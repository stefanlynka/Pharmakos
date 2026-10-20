using UnityEngine;

[DisallowMultipleComponent]
public class CardHighlightEffect : MonoBehaviour
{
    const string GlowShaderName = "Pharmakos/CardEdgeMistGlow";
    const string OuterGlowSuffix = "_OuterGlow";

    [SerializeField] SpriteRenderer coreHighlight;
    [SerializeField] SpriteRenderer outerHighlight;
    [SerializeField] Material glowMaterialTemplate;
    [SerializeField] MeshHighlightSettings settings;

    [Header("Colors")]
    [SerializeField] Color glowCoreColor = new Color(0.68f, 0.9f, 1f, 0.78f);
    [SerializeField] Color glowOuterColor = new Color(0.42f, 0.75f, 1f, 0.65f);
    [SerializeField] Color glowSecondaryColor = new Color(0.9f, 0.97f, 1f, 0.8f);

    [Header("Intensity")]
    [SerializeField] float glowCoreIntensityMultiplier = 0.9f;
    [SerializeField] float glowOuterIntensityMultiplier = 0.7f;
    [SerializeField] float glowCoreEmissionStrength = 0.9f;
    [SerializeField] float glowOuterEmissionStrength = 0.55f;

    [Header("Motion")]
    [SerializeField] float glowPulseSpeed = 0.75f;
    [SerializeField] float glowPulseMin = 0.62f;
    [SerializeField] float glowPulseMax = 0.95f;
    [SerializeField] float glowMistSpeed = 0.85f;
    [SerializeField] float glowMistScale = 3f;
    [SerializeField] float glowMistDetailScale = 8.5f;
    [SerializeField] float glowFlickerSpeed = 2.4f;
    [SerializeField] float glowFlickerStrength = 0.2f;
    [SerializeField] float glowDrift = 0.24f;

    [Header("Flare")]
    [SerializeField] float flareDuration = 0.5f;
    [SerializeField] float flarePeakMultiplier = 2.1f;

    static Material sharedGlowMaterial;
    static bool attemptedToLoadGlowShader;

    static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    static readonly int GlowSecondaryColorId = Shader.PropertyToID("_SecondaryColor");
    static readonly int GlowIntensityId = Shader.PropertyToID("_Intensity");
    static readonly int GlowEmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
    static readonly int GlowMistSpeedId = Shader.PropertyToID("_MistSpeed");
    static readonly int GlowMistScaleId = Shader.PropertyToID("_MistScale");
    static readonly int GlowMistDetailScaleId = Shader.PropertyToID("_DetailMistScale");
    static readonly int GlowFlickerSpeedId = Shader.PropertyToID("_FlickerSpeed");
    static readonly int GlowFlickerStrengthId = Shader.PropertyToID("_FlickerStrength");
    static readonly int GlowDriftId = Shader.PropertyToID("_Drift");
    static readonly int GlowSeedId = Shader.PropertyToID("_Seed");

    MaterialPropertyBlock corePropertyBlock;
    MaterialPropertyBlock outerPropertyBlock;
    float coreSeed;
    float outerSeed;
    bool isActive;
    bool isInitialized;
    bool flarePlaying;
    float flareElapsed;

    void Awake()
    {
        EnsureInitialized();
        SetActive(false);
    }

    void LateUpdate()
    {
        if (!isActive && !flarePlaying)
            return;

        float resting = isActive ? RestingIntensity() : 0f;
        float intensity = resting;
        float emissionMultiplier = 1f;

        if (flarePlaying)
        {
            flareElapsed += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.01f, flareDuration);
            float t = flareElapsed / duration;
            if (t >= 1f)
            {
                flarePlaying = false;
                RefreshRendererState();
            }
            else
            {
                float envelope = FlareEnvelope(t);
                float peak = Mathf.Max(glowPulseMax * flarePeakMultiplier, glowPulseMax);
                intensity = Mathf.Lerp(resting, peak, envelope);
                emissionMultiplier = Mathf.Lerp(1f, 1.45f, envelope);
            }
        }

        ApplyGlow(intensity, emissionMultiplier);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying || !isActive)
            return;

        ApplyGlow(RestingIntensity());
    }
#endif

    public void SetActive(bool active)
    {
        EnsureInitialized();

        if (isActive == active)
        {
            RefreshRendererState();
            return;
        }

        isActive = active;
        RefreshRendererState();

        if (active || flarePlaying)
            ApplyGlow(isActive ? RestingIntensity() : 0f);
    }

    public void Flare()
    {
        EnsureInitialized();
        flarePlaying = true;
        flareElapsed = 0f;
        RefreshRendererState();
        ApplyGlow(isActive ? RestingIntensity() : 0f);
    }

    public void ApplySettings(MeshHighlightSettings newSettings)
    {
        if (newSettings == null)
            return;

        settings = newSettings;
        CopySettingsToFields(newSettings);

        if (isActive || flarePlaying)
            ApplyGlow(isActive ? RestingIntensity() : 0f);
    }

    public void CopyFieldsToSettings(MeshHighlightSettings target)
    {
        if (target == null)
            return;

        target.glowColor = glowCoreColor;
        target.hotColor = glowSecondaryColor;
        target.outerShellGlowColor = glowOuterColor;
        target.intensity = glowCoreIntensityMultiplier;
        target.coreIntensityMultiplier = 1f;
        target.outerIntensityMultiplier = glowOuterIntensityMultiplier / Mathf.Max(glowCoreIntensityMultiplier, 0.001f);
        target.emissionBoost = glowCoreEmissionStrength;
        target.outerEmissionBoost = glowOuterEmissionStrength;
        target.pulseSpeed = glowPulseSpeed;
        target.pulseMin = glowPulseMin;
        target.pulseMax = glowPulseMax;
        target.noiseSpeed = glowMistSpeed;
        target.noiseScale = glowMistScale;
        target.flickerSpeed = glowFlickerSpeed;
        target.flickerStrength = glowFlickerStrength;
        target.flowBias = glowDrift;
    }

    void CopySettingsToFields(MeshHighlightSettings source)
    {
        glowCoreColor = source.glowColor;
        glowSecondaryColor = source.hotColor;
        glowOuterColor = source.outerShellGlowColor.a > 0.01f ? source.outerShellGlowColor : source.outerColor;
        glowCoreIntensityMultiplier = source.intensity * source.coreIntensityMultiplier;
        glowOuterIntensityMultiplier = source.intensity * source.outerIntensityMultiplier;
        glowCoreEmissionStrength = source.emissionBoost;
        glowOuterEmissionStrength = source.outerEmissionBoost;
        glowPulseSpeed = source.pulseSpeed;
        glowPulseMin = source.pulseMin;
        glowPulseMax = source.pulseMax;
        glowMistSpeed = source.noiseSpeed > 0.01f ? source.noiseSpeed : glowMistSpeed;
        glowMistScale = source.noiseScale > 0.01f ? source.noiseScale : glowMistScale;
        glowFlickerSpeed = source.flickerSpeed;
        glowFlickerStrength = source.flickerStrength;
        glowDrift = Mathf.Abs(source.flowBias) > 0.01f ? Mathf.Clamp(source.flowBias, 0f, 2f) : glowDrift;
    }

    void EnsureInitialized()
    {
        if (isInitialized)
            return;

        if (coreHighlight == null)
        {
            ViewCard viewCard = GetComponent<ViewCard>();
            if (viewCard != null)
                coreHighlight = viewCard.Highlight;
        }

        if (coreHighlight == null)
            return;

        corePropertyBlock ??= new MaterialPropertyBlock();
        outerPropertyBlock ??= new MaterialPropertyBlock();

        if (outerHighlight == null)
        {
            Transform existingOuter = coreHighlight.transform.parent.Find($"{coreHighlight.name}{OuterGlowSuffix}");
            if (existingOuter != null)
                outerHighlight = existingOuter.GetComponent<SpriteRenderer>();
        }

        Material glowMaterial = GetOrCreateGlowMaterial();
        if (glowMaterial != null)
        {
            if (coreHighlight.sharedMaterial != glowMaterial)
                coreHighlight.sharedMaterial = glowMaterial;

            if (outerHighlight != null && outerHighlight.sharedMaterial != glowMaterial)
                outerHighlight.sharedMaterial = glowMaterial;
        }

        if (coreHighlight != null)
            coreHighlight.gameObject.SetActive(true);

        if (outerHighlight != null)
            outerHighlight.gameObject.SetActive(true);

        coreSeed = GenerateStableSeed(0.137f);
        outerSeed = GenerateStableSeed(0.713f);
        isInitialized = true;
    }

    float RestingIntensity()
    {
        float pulseT = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * glowPulseSpeed * Mathf.PI * 2f);
        return Mathf.Lerp(glowPulseMin, glowPulseMax, pulseT);
    }

    static float FlareEnvelope(float t)
    {
        const float risePortion = 0.28f;
        if (t < risePortion)
        {
            float rise = t / risePortion;
            return 1f - (1f - rise) * (1f - rise);
        }

        float fall = (t - risePortion) / (1f - risePortion);
        return 1f - fall * fall;
    }

    void RefreshRendererState()
    {
        bool show = isActive || flarePlaying;

        if (coreHighlight != null)
            coreHighlight.enabled = show;

        if (outerHighlight != null)
            outerHighlight.enabled = show;
    }

    void ApplyGlow(float glowIntensity, float emissionMultiplier = 1f)
    {
        ApplyGlowToRenderer(coreHighlight, corePropertyBlock, glowCoreColor, glowIntensity * glowCoreIntensityMultiplier, glowCoreEmissionStrength * emissionMultiplier, coreSeed);

        if (outerHighlight != null)
            ApplyGlowToRenderer(outerHighlight, outerPropertyBlock, glowOuterColor, glowIntensity * glowOuterIntensityMultiplier, glowOuterEmissionStrength * emissionMultiplier, outerSeed);
    }

    void ApplyGlowToRenderer(
        SpriteRenderer renderer,
        MaterialPropertyBlock propertyBlock,
        Color glowColor,
        float intensity,
        float emissionStrength,
        float seed)
    {
        if (renderer == null || propertyBlock == null)
            return;

        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(GlowColorId, glowColor);
        propertyBlock.SetColor(GlowSecondaryColorId, glowSecondaryColor);
        propertyBlock.SetFloat(GlowIntensityId, intensity);
        propertyBlock.SetFloat(GlowEmissionStrengthId, emissionStrength);
        propertyBlock.SetFloat(GlowMistSpeedId, glowMistSpeed);
        propertyBlock.SetFloat(GlowMistScaleId, glowMistScale);
        propertyBlock.SetFloat(GlowMistDetailScaleId, glowMistDetailScale);
        propertyBlock.SetFloat(GlowFlickerSpeedId, glowFlickerSpeed);
        propertyBlock.SetFloat(GlowFlickerStrengthId, glowFlickerStrength);
        propertyBlock.SetFloat(GlowDriftId, glowDrift);
        propertyBlock.SetFloat(GlowSeedId, seed);
        renderer.SetPropertyBlock(propertyBlock);
    }

    Material GetOrCreateGlowMaterial()
    {
        if (sharedGlowMaterial != null)
            return sharedGlowMaterial;

        if (glowMaterialTemplate != null)
        {
            sharedGlowMaterial = new Material(glowMaterialTemplate)
            {
                name = "CardEdgeMistGlow_Runtime"
            };
            return sharedGlowMaterial;
        }

        if (attemptedToLoadGlowShader)
            return null;

        attemptedToLoadGlowShader = true;
        Shader glowShader = Shader.Find(GlowShaderName);
        if (glowShader == null)
            return null;

        sharedGlowMaterial = new Material(glowShader)
        {
            name = "CardEdgeMistGlow_Runtime"
        };

        return sharedGlowMaterial;
    }

    float GenerateStableSeed(float salt)
    {
        int hash = GetInstanceID();
        return Mathf.Abs(Mathf.Sin((hash * 0.1234567f) + salt)) * 100f;
    }
}
