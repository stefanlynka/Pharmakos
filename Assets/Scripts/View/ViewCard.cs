using System;
using TMPro;
using UnityEngine;

public class ViewCard : ViewTarget
{
    public SpriteRenderer ArtRenderer;
    public ViewOfferingCost ViewOfferingCost;
    public TextMeshPro NameText;
    public TextMeshPro NameTextWithoutCost;

    public Card Card;

    public SpriteRenderer Highlight;
    [Header("Highlight Glow")]
    [SerializeField] bool useGlowHighlight = true;
    [SerializeField] bool useMistShader = true;
    [SerializeField] Color glowCoreColor = new Color(0.68f, 0.9f, 1f, 0.78f);
    [SerializeField] Color glowOuterColor = new Color(0.42f, 0.75f, 1f, 0.65f);
    [SerializeField] Color glowSecondaryColor = new Color(0.9f, 0.97f, 1f, 0.8f);
    [SerializeField] float glowPulseSpeed = 0.75f;
    [SerializeField] float glowPulseMin = 0.62f;
    [SerializeField] float glowPulseMax = 0.95f;
    [SerializeField] float glowCoreIntensityMultiplier = 0.9f;
    [SerializeField] float glowOuterIntensityMultiplier = 0.7f;
    [SerializeField] float glowMistSpeed = 0.85f;
    [SerializeField] float glowMistScale = 3f;
    [SerializeField] float glowMistDetailScale = 8.5f;
    [SerializeField] float glowFlickerSpeed = 2.4f;
    [SerializeField] float glowFlickerStrength = 0.2f;
    [SerializeField] float glowCoreEmissionStrength = 0.9f;
    [SerializeField] float glowOuterEmissionStrength = 0.55f;
    [SerializeField] float glowDrift = 0.24f;

    public GameObject CardHolder;
    public BoxCollider CardCollider;

    public GameObject FullTextBox;

    public RectTransform NameTextTransform;

    protected bool inDescriptiveMode = false;
    bool isCostShown = true;
    bool isHighlightActive = false;

    public bool IsInDescriptiveMode => inDescriptiveMode;
    public bool IsCostShown => isCostShown;
    MaterialPropertyBlock artPropertyBlock;
    MaterialPropertyBlock highlightCorePropertyBlock;
    MaterialPropertyBlock highlightOuterPropertyBlock;
    SpriteRenderer highlightOuterRenderer;
    float highlightCoreSeed;
    float highlightOuterSeed;
    static Material glowSharedMaterial;
    static bool attemptedToLoadGlowShader;
    const string GlowShaderName = "Pharmakos/CardEdgeMistGlow";
    static readonly int ShaderColorId = Shader.PropertyToID("_Color");
    static readonly int ShaderBaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ShaderEmissionColorId = Shader.PropertyToID("_EmissionColor");
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

    //public Action<ViewCard> OnClick = null;


    public virtual void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        EnsureHighlightGlowSetup();
        EnsureNameTextReferences();
        Card = cardData;
        ViewOfferingCost.Load(Card.GetCosts());

        // Show ViewOfferingCost when card is created
        SetCostShown(true);

        ArtRenderer.sprite = CardHandler.GetSprite(cardData);
        SetNameText(cardData.GetName());

        OnClick = onClick;

        CardCollider.enabled = true;

        SetDescriptiveMode(false);
    }

    public void SetHighlight(bool highlight)
    {
        EnsureHighlightGlowSetup();

        isHighlightActive = highlight;
        if (Highlight != null)
            Highlight.enabled = highlight;

        if (highlightOuterRenderer != null)
            highlightOuterRenderer.enabled = highlight;

        if (highlight)
            ApplyHighlightGlow(1f);
    }
    public bool IsHighlighted()
    {
        return Highlight.enabled;
    }

    public void UpdateCost()
    {
        ViewOfferingCost.Load(Card.GetCosts());
    }

    public virtual void SetDescriptiveMode(bool value)
    {
        //if (inDescriptiveMode == value) return;
        if (Card == null)
        {
            Debug.LogError("ViewCard: Card is null");
            return;
        }
        if (Card.Text == string.Empty) value = false;

        inDescriptiveMode = value;

        FullTextBox.SetActive(value);
        ApplyArtLayout(value);
    }

    protected virtual CardArtLayoutAsset GetArtLayoutAsset(bool descriptiveMode) => null;

    protected CardArtClipRect GetArtLayoutForMode(bool descriptiveMode)
    {
        CardArtLayoutAsset asset = GetArtLayoutAsset(descriptiveMode);
        return asset != null ? asset.Layout : CardArtClipRect.Full;
    }

    protected void ApplyArtLayout(bool descriptiveMode)
    {
        if (ArtRenderer == null)
        {
            return;
        }

        CardArtClipRect layout = GetArtLayoutForMode(descriptiveMode);
        if (!isCostShown)
            layout.TopLeftCutout = CardArtCornerCutout.None;

        artPropertyBlock ??= new MaterialPropertyBlock();
        CardArtClipUtility.Apply(ArtRenderer, layout, artPropertyBlock);
    }

    public virtual void SetCostShown(bool value)
    {
        EnsureNameTextReferences();
        isCostShown = value;
        ViewOfferingCost.gameObject.SetActive(value);
        SetNameVisibility(value);
        float newWidth = value ? 7.4f : 8.8f;
        NameTextTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        ApplyArtLayout(inDescriptiveMode);
    }

    void SetNameText(string text)
    {
        if (NameText != null)
            NameText.text = text;
        if (NameTextWithoutCost != null)
            NameTextWithoutCost.text = text;
    }

    void SetNameVisibility(bool showTextWithCost)
    {
        if (NameText != null)
            NameText.gameObject.SetActive(showTextWithCost);

        if (NameTextWithoutCost != null)
            NameTextWithoutCost.gameObject.SetActive(!showTextWithCost);
        else if (!showTextWithCost && NameText != null)
        {
            // Fallback for prefabs that have not assigned TextWithoutCost yet.
            NameText.gameObject.SetActive(true);
        }
    }

    void EnsureNameTextReferences()
    {
        if (NameText == null)
            NameText = transform.Find("TextWithCost")?.GetComponent<TextMeshPro>();

        if (NameTextWithoutCost == null)
            NameTextWithoutCost = transform.Find("TextWithoutCost")?.GetComponent<TextMeshPro>();
    }

    void Awake()
    {
        EnsureHighlightGlowSetup();
        SetHighlight(false);
    }

    void LateUpdate()
    {
        if (!isHighlightActive || Highlight == null || !useGlowHighlight)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * glowPulseSpeed * Mathf.PI * 2f);
        float glowIntensity = Mathf.Lerp(glowPulseMin, glowPulseMax, pulse);
        ApplyHighlightGlow(glowIntensity);
    }

    void EnsureHighlightGlowSetup()
    {
        if (!useGlowHighlight || Highlight == null)
            return;

        if (highlightCorePropertyBlock == null)
            highlightCorePropertyBlock = new MaterialPropertyBlock();
        if (highlightOuterPropertyBlock == null)
            highlightOuterPropertyBlock = new MaterialPropertyBlock();

        if (highlightOuterRenderer != null)
            return;

        Transform existingOuter = Highlight.transform.parent.Find($"{Highlight.name}_OuterGlow");
        if (existingOuter != null)
        {
            highlightOuterRenderer = existingOuter.GetComponent<SpriteRenderer>();
        }
        else
        {
            GameObject outerGlowObject = new GameObject($"{Highlight.name}_OuterGlow");
            outerGlowObject.transform.SetParent(Highlight.transform.parent, false);
            outerGlowObject.transform.localPosition = Highlight.transform.localPosition;
            outerGlowObject.transform.localRotation = Highlight.transform.localRotation;
            outerGlowObject.transform.localScale = Highlight.transform.localScale;

            highlightOuterRenderer = outerGlowObject.AddComponent<SpriteRenderer>();
            CopyHighlightRendererSettings(Highlight, highlightOuterRenderer);
            highlightOuterRenderer.enabled = false;
        }

        highlightCoreSeed = GenerateStableSeed(0.137f);
        highlightOuterSeed = GenerateStableSeed(0.713f);
        AssignHighlightMaterial(Highlight);
        AssignHighlightMaterial(highlightOuterRenderer);
    }

    void CopyHighlightRendererSettings(SpriteRenderer source, SpriteRenderer destination)
    {
        destination.sprite = source.sprite;
        destination.drawMode = source.drawMode;
        destination.size = source.size;
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder;
        destination.maskInteraction = source.maskInteraction;
        destination.sharedMaterial = source.sharedMaterial;
        destination.flipX = source.flipX;
        destination.flipY = source.flipY;
        destination.color = source.color;
    }

    void ApplyHighlightGlow(float glowIntensity)
    {
        ApplyGlowToRenderer(
            Highlight,
            highlightCorePropertyBlock,
            glowCoreColor,
            glowIntensity * glowCoreIntensityMultiplier,
            glowCoreEmissionStrength,
            highlightCoreSeed);

        if (highlightOuterRenderer == null)
            return;

        ApplyGlowToRenderer(
            highlightOuterRenderer,
            highlightOuterPropertyBlock,
            glowOuterColor,
            glowIntensity * glowOuterIntensityMultiplier,
            glowOuterEmissionStrength,
            highlightOuterSeed);
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
        bool usingMistMaterial = useMistShader && glowSharedMaterial != null && renderer.sharedMaterial == glowSharedMaterial;

        if (usingMistMaterial)
        {
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
        }
        else
        {
            Color color = glowColor * intensity;
            Color emissionColor = glowColor * (intensity * emissionStrength);
            propertyBlock.SetColor(ShaderColorId, color);
            propertyBlock.SetColor(ShaderBaseColorId, color);
            propertyBlock.SetColor(ShaderEmissionColorId, emissionColor);
        }

        renderer.SetPropertyBlock(propertyBlock);
    }

    void AssignHighlightMaterial(SpriteRenderer renderer)
    {
        if (renderer == null || !useMistShader)
            return;

        Material glowMaterial = GetOrCreateGlowMaterial();
        if (glowMaterial != null && renderer.sharedMaterial != glowMaterial)
            renderer.sharedMaterial = glowMaterial;
    }

    static Material GetOrCreateGlowMaterial()
    {
        if (glowSharedMaterial != null)
            return glowSharedMaterial;

        if (attemptedToLoadGlowShader)
            return null;

        attemptedToLoadGlowShader = true;
        Shader glowShader = Shader.Find(GlowShaderName);
        if (glowShader == null)
            return null;

        glowSharedMaterial = new Material(glowShader)
        {
            name = "CardEdgeMistGlow_Runtime"
        };

        return glowSharedMaterial;
    }

    float GenerateStableSeed(float salt)
    {
        int hash = GetInstanceID();
        float normalized = Mathf.Abs(Mathf.Sin((hash * 0.1234567f) + salt));
        return normalized * 100f;
    }
}
