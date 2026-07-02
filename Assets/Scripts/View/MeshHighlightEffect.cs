using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MeshHighlightEffect : MonoBehaviour
{
    const string GlowShaderName = "Pharmakos/MeshFlameHighlight";
    const string GlowShellRootName = "FlameHighlightShells";
    const string CoreGlowSuffix = "_CoreFlame";
    const string OuterGlowSuffix = "_OuterFlame";

    [SerializeField] Transform targetRoot;
    [SerializeField] Material glowMaterialTemplate;
    [SerializeField] MeshHighlightSettings settings;
    [SerializeField] List<Renderer> explicitRenderers = new List<Renderer>();

    [Header("Overrides")]
    [SerializeField] Color glowColor = new Color(0.15f, 0.95f, 0.28f, 1f);
    [SerializeField] Color hotColor = new Color(1f, 1f, 0.82f, 1f);
    [SerializeField] Color outerColor = new Color(0.02f, 0.45f, 0.08f, 1f);
    [SerializeField] Color outerShellGlowColor = new Color(0.1f, 0.78f, 0.18f, 0.9f);
    [SerializeField] Color outerShellOuterColor = new Color(0.01f, 0.28f, 0.05f, 0.75f);
    [SerializeField] float coreFlameExtent = 0.012f;
    [SerializeField] float coreFlameExtentMin = 0.001f;
    [SerializeField] float outerFlameExtent = 0.032f;
    [SerializeField] float outerFlameExtentMin = 0.004f;
    [SerializeField] float edgePower = 3.8f;
    [SerializeField] float outerEdgePower = 2.6f;
    [SerializeField] float edgeMin = 0.35f;
    [SerializeField] float riseStrength = 1.1f;
    [SerializeField] float coreTongueThreshold = 0.46f;
    [SerializeField] float outerTongueThreshold = 0.58f;
    [SerializeField] float tongueSharpness = 0.11f;
    [SerializeField] float tipFalloff = 1.35f;
    [SerializeField] float outerTipFalloff = 1.05f;
    [SerializeField] float noiseScale = 11f;
    [SerializeField] float noiseSpeed = 2.2f;
    [SerializeField] float coreNoiseStrength = 1.15f;
    [SerializeField] float outerNoiseStrength = 1.35f;
    [SerializeField] float flowBias = 1.35f;
    [SerializeField] float intensity = 2.2f;
    [SerializeField] float coreIntensityMultiplier = 1f;
    [SerializeField] float outerIntensityMultiplier = 0.55f;
    [SerializeField] float emissionBoost = 3.2f;
    [SerializeField] float outerEmissionBoost = 2.1f;
    [SerializeField] float flickerSpeed = 4.5f;
    [SerializeField] float flickerStrength = 0.38f;
    [SerializeField] float pulseSpeed = 1.2f;
    [SerializeField] float pulseMin = 0.88f;
    [SerializeField] float pulseMax = 1f;

    static Material sharedGlowMaterial;
    static bool attemptedToLoadGlowShader;

    static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    static readonly int HotColorId = Shader.PropertyToID("_HotColor");
    static readonly int OuterColorId = Shader.PropertyToID("_OuterColor");
    static readonly int FlameExtentId = Shader.PropertyToID("_FlameExtent");
    static readonly int FlameExtentMinId = Shader.PropertyToID("_FlameExtentMin");
    static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    static readonly int NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
    static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    static readonly int EdgePowerId = Shader.PropertyToID("_EdgePower");
    static readonly int EdgeMinId = Shader.PropertyToID("_EdgeMin");
    static readonly int RiseStrengthId = Shader.PropertyToID("_RiseStrength");
    static readonly int TongueThresholdId = Shader.PropertyToID("_TongueThreshold");
    static readonly int TongueSharpnessId = Shader.PropertyToID("_TongueSharpness");
    static readonly int TipFalloffId = Shader.PropertyToID("_TipFalloff");
    static readonly int FlickerSpeedId = Shader.PropertyToID("_FlickerSpeed");
    static readonly int FlickerStrengthId = Shader.PropertyToID("_FlickerStrength");
    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int EmissionBoostId = Shader.PropertyToID("_EmissionBoost");
    static readonly int FlowBiasId = Shader.PropertyToID("_FlowBias");
    static readonly int ShellLayerId = Shader.PropertyToID("_ShellLayer");
    static readonly int SeedId = Shader.PropertyToID("_Seed");

    readonly List<Renderer> sourceRenderers = new List<Renderer>();
    readonly List<FlameGlowShell> glowShells = new List<FlameGlowShell>();
    Transform glowShellRoot;
    bool isActive;
    bool isInitialized;

    sealed class FlameGlowShell
    {
        public Renderer Renderer;
        public MaterialPropertyBlock PropertyBlock;
        public bool IsOuter;
        public float Seed;
    }

    void Awake()
    {
        EnsureInitialized();
        SetActive(false);
    }

    void LateUpdate()
    {
        if (!isActive || glowShells.Count == 0)
            return;

        float pulseT = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
        float glowIntensity = Mathf.Lerp(pulseMin, pulseMax, pulseT);
        ApplyGlow(glowIntensity);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying || !isActive || glowShells.Count == 0)
            return;

        float pulseT = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
        ApplyGlow(Mathf.Lerp(pulseMin, pulseMax, pulseT));
    }
#endif

    public void SetActive(bool active)
    {
        EnsureInitialized();

        if (isActive == active)
            return;

        isActive = active;

        foreach (FlameGlowShell shell in glowShells)
        {
            if (shell.Renderer != null)
                shell.Renderer.enabled = active;
        }

        if (active)
            ApplyGlow(Mathf.Lerp(pulseMin, pulseMax, 0.5f));
    }

    public void ApplySettings(MeshHighlightSettings newSettings)
    {
        if (newSettings == null)
            return;

        settings = newSettings;
        CopySettingsToFields(newSettings);
        if (isActive)
            ApplyGlow(Mathf.Lerp(pulseMin, pulseMax, 0.5f));
    }

    public void CopyFieldsToSettings(MeshHighlightSettings target)
    {
        if (target == null)
            return;

        target.glowColor = glowColor;
        target.hotColor = hotColor;
        target.outerColor = outerColor;
        target.outerShellGlowColor = outerShellGlowColor;
        target.outerShellOuterColor = outerShellOuterColor;
        target.coreFlameExtent = coreFlameExtent;
        target.coreFlameExtentMin = coreFlameExtentMin;
        target.outerFlameExtent = outerFlameExtent;
        target.outerFlameExtentMin = outerFlameExtentMin;
        target.edgePower = edgePower;
        target.outerEdgePower = outerEdgePower;
        target.edgeMin = edgeMin;
        target.riseStrength = riseStrength;
        target.coreTongueThreshold = coreTongueThreshold;
        target.outerTongueThreshold = outerTongueThreshold;
        target.tongueSharpness = tongueSharpness;
        target.tipFalloff = tipFalloff;
        target.outerTipFalloff = outerTipFalloff;
        target.noiseScale = noiseScale;
        target.noiseSpeed = noiseSpeed;
        target.coreNoiseStrength = coreNoiseStrength;
        target.outerNoiseStrength = outerNoiseStrength;
        target.flowBias = flowBias;
        target.intensity = intensity;
        target.coreIntensityMultiplier = coreIntensityMultiplier;
        target.outerIntensityMultiplier = outerIntensityMultiplier;
        target.emissionBoost = emissionBoost;
        target.outerEmissionBoost = outerEmissionBoost;
        target.flickerSpeed = flickerSpeed;
        target.flickerStrength = flickerStrength;
        target.pulseSpeed = pulseSpeed;
        target.pulseMin = pulseMin;
        target.pulseMax = pulseMax;
    }

    void EnsureInitialized()
    {
        if (isInitialized)
            return;

        Transform root = targetRoot != null ? targetRoot : transform;
        CollectSourceRenderers(root);

        Material glowMaterial = GetOrCreateGlowMaterial();
        if (glowMaterial == null || sourceRenderers.Count == 0)
            return;

        glowShellRoot = CreateOrFindGlowShellRoot(root);
        BuildGlowShells(glowMaterial);
        isInitialized = glowShells.Count > 0;
    }

    void CopySettingsToFields(MeshHighlightSettings source)
    {
        glowColor = source.glowColor;
        hotColor = source.hotColor;
        outerColor = source.outerColor;
        outerShellGlowColor = source.outerShellGlowColor;
        outerShellOuterColor = source.outerShellOuterColor;
        coreFlameExtent = source.coreFlameExtent;
        coreFlameExtentMin = source.coreFlameExtentMin;
        outerFlameExtent = source.outerFlameExtent;
        outerFlameExtentMin = source.outerFlameExtentMin;
        edgePower = source.edgePower;
        outerEdgePower = source.outerEdgePower;
        edgeMin = source.edgeMin;
        riseStrength = source.riseStrength;
        coreTongueThreshold = source.coreTongueThreshold;
        outerTongueThreshold = source.outerTongueThreshold;
        tongueSharpness = source.tongueSharpness;
        tipFalloff = source.tipFalloff;
        outerTipFalloff = source.outerTipFalloff;
        noiseScale = source.noiseScale;
        noiseSpeed = source.noiseSpeed;
        coreNoiseStrength = source.coreNoiseStrength;
        outerNoiseStrength = source.outerNoiseStrength;
        flowBias = source.flowBias;
        intensity = source.intensity;
        coreIntensityMultiplier = source.coreIntensityMultiplier;
        outerIntensityMultiplier = source.outerIntensityMultiplier;
        emissionBoost = source.emissionBoost;
        outerEmissionBoost = source.outerEmissionBoost;
        flickerSpeed = source.flickerSpeed;
        flickerStrength = source.flickerStrength;
        pulseSpeed = source.pulseSpeed;
        pulseMin = source.pulseMin;
        pulseMax = source.pulseMax;
    }

    void CollectSourceRenderers(Transform root)
    {
        sourceRenderers.Clear();

        if (explicitRenderers.Count > 0)
        {
            foreach (Renderer renderer in explicitRenderers)
            {
                if (renderer != null && !IsGlowShellRenderer(renderer))
                    sourceRenderers.Add(renderer);
            }

            return;
        }

        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer == null || IsGlowShellRenderer(renderer))
                continue;

            sourceRenderers.Add(renderer);
        }
    }

    static Transform CreateOrFindGlowShellRoot(Transform rootTransform)
    {
        Transform existing = rootTransform.Find(GlowShellRootName);
        if (existing != null)
            return existing;

        var rootObject = new GameObject(GlowShellRootName);
        rootObject.transform.SetParent(rootTransform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        return rootObject.transform;
    }

    void BuildGlowShells(Material glowMaterial)
    {
        glowShells.Clear();

        float coreSeed = GenerateStableSeed(0.137f);
        float outerSeed = GenerateStableSeed(0.713f);

        foreach (Renderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer is not MeshRenderer meshRenderer)
                continue;

            MeshFilter sourceFilter = meshRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            glowShells.Add(CreateGlowShell(sourceRenderer.transform, sourceFilter.sharedMesh, glowMaterial, false, coreSeed));
            glowShells.Add(CreateGlowShell(sourceRenderer.transform, sourceFilter.sharedMesh, glowMaterial, true, outerSeed));
        }
    }

    FlameGlowShell CreateGlowShell(Transform sourceTransform, Mesh mesh, Material glowMaterial, bool isOuter, float seed)
    {
        string suffix = isOuter ? OuterGlowSuffix : CoreGlowSuffix;
        Transform existing = glowShellRoot.Find(sourceTransform.name + suffix);
        Renderer renderer;

        if (existing != null)
        {
            renderer = existing.GetComponent<Renderer>();
        }
        else
        {
            var shellObject = new GameObject(sourceTransform.name + suffix);
            shellObject.transform.SetParent(glowShellRoot, false);

            Transform parentTransform = glowShellRoot.parent;
            shellObject.transform.localPosition = parentTransform.InverseTransformPoint(sourceTransform.position);
            shellObject.transform.localRotation = Quaternion.Inverse(parentTransform.rotation) * sourceTransform.rotation;
            Vector3 parentLossyScale = parentTransform.lossyScale;
            Vector3 sourceLossyScale = sourceTransform.lossyScale;
            shellObject.transform.localScale = new Vector3(
                parentLossyScale.x > 0f ? sourceLossyScale.x / parentLossyScale.x : 1f,
                parentLossyScale.y > 0f ? sourceLossyScale.y / parentLossyScale.y : 1f,
                parentLossyScale.z > 0f ? sourceLossyScale.z / parentLossyScale.z : 1f);

            var meshFilter = shellObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = shellObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = glowMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            renderer = meshRenderer;
        }

        renderer.enabled = false;

        return new FlameGlowShell
        {
            Renderer = renderer,
            PropertyBlock = new MaterialPropertyBlock(),
            IsOuter = isOuter,
            Seed = seed
        };
    }

    void ApplyGlow(float glowIntensity)
    {
        foreach (FlameGlowShell shell in glowShells)
        {
            if (shell.Renderer == null || shell.PropertyBlock == null)
                continue;

            bool isOuter = shell.IsOuter;
            Color primaryGlow = isOuter ? outerShellGlowColor : glowColor;
            Color primaryOuter = isOuter ? outerShellOuterColor : outerColor;
            float intensityMultiplier = isOuter ? outerIntensityMultiplier : coreIntensityMultiplier;
            float emission = isOuter ? outerEmissionBoost : emissionBoost;
            float flameExtent = isOuter ? outerFlameExtent : coreFlameExtent;
            float flameExtentMin = isOuter ? outerFlameExtentMin : coreFlameExtentMin;
            float noiseStrength = isOuter ? outerNoiseStrength : coreNoiseStrength;
            float shellEdgePower = isOuter ? outerEdgePower : edgePower;
            float tongueThreshold = isOuter ? outerTongueThreshold : coreTongueThreshold;
            float shellTipFalloff = isOuter ? outerTipFalloff : tipFalloff;

            ApplyGlowToRenderer(
                shell.Renderer,
                shell.PropertyBlock,
                primaryGlow,
                hotColor,
                primaryOuter,
                glowIntensity * intensityMultiplier,
                emission,
                flameExtent,
                flameExtentMin,
                noiseStrength,
                shellEdgePower,
                tongueThreshold,
                shellTipFalloff,
                isOuter ? 1f : 0f,
                shell.Seed);
        }
    }

    void ApplyGlowToRenderer(
        Renderer renderer,
        MaterialPropertyBlock propertyBlock,
        Color primaryGlow,
        Color primaryHot,
        Color primaryOuter,
        float glowIntensity,
        float emission,
        float flameExtent,
        float flameExtentMin,
        float noiseStrength,
        float shellEdgePower,
        float tongueThreshold,
        float shellTipFalloff,
        float shellLayer,
        float seed)
    {
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(GlowColorId, primaryGlow);
        propertyBlock.SetColor(HotColorId, primaryHot);
        propertyBlock.SetColor(OuterColorId, primaryOuter);
        propertyBlock.SetFloat(IntensityId, intensity * glowIntensity);
        propertyBlock.SetFloat(EmissionBoostId, emission);
        propertyBlock.SetFloat(FlameExtentId, flameExtent);
        propertyBlock.SetFloat(FlameExtentMinId, flameExtentMin);
        propertyBlock.SetFloat(NoiseScaleId, noiseScale);
        propertyBlock.SetFloat(NoiseSpeedId, noiseSpeed);
        propertyBlock.SetFloat(NoiseStrengthId, noiseStrength);
        propertyBlock.SetFloat(EdgePowerId, shellEdgePower);
        propertyBlock.SetFloat(EdgeMinId, edgeMin);
        propertyBlock.SetFloat(RiseStrengthId, riseStrength);
        propertyBlock.SetFloat(TongueThresholdId, tongueThreshold);
        propertyBlock.SetFloat(TongueSharpnessId, tongueSharpness);
        propertyBlock.SetFloat(TipFalloffId, shellTipFalloff);
        propertyBlock.SetFloat(FlickerSpeedId, flickerSpeed);
        propertyBlock.SetFloat(FlickerStrengthId, flickerStrength);
        propertyBlock.SetFloat(FlowBiasId, flowBias);
        propertyBlock.SetFloat(ShellLayerId, shellLayer);
        propertyBlock.SetFloat(SeedId, seed);
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
                name = "MeshFlameHighlight_Runtime"
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
            name = "MeshFlameHighlight_Runtime"
        };

        return sharedGlowMaterial;
    }

    static bool IsGlowShellRenderer(Renderer renderer)
    {
        if (renderer == null)
            return false;

        string objectName = renderer.gameObject.name;
        return objectName.EndsWith(CoreGlowSuffix) || objectName.EndsWith(OuterGlowSuffix);
    }

    float GenerateStableSeed(float salt)
    {
        int hash = GetInstanceID();
        return Mathf.Abs(Mathf.Sin((hash * 0.1234567f) + salt)) * 100f;
    }
}
