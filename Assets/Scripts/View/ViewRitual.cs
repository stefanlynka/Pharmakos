using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewRitual : ViewTarget
{
    public Ritual Ritual;
    public TextMeshPro BloodText;
    public TextMeshPro BonesText;
    public TextMeshPro CropsText;
    public TextMeshPro ScrollsText;

    public TextMeshPro Title;
    public TextMeshPro Description;

    [Header("Legacy 2D Highlight")]
    public GameObject HighlightHolder;
    public MeshRenderer Highlight;

    [Header("3D Highlight")]
    [SerializeField] Transform altarRoot;
    [SerializeField] bool useMeshHighlight = true;
    [SerializeField] bool useOutlineHighlight = true;
    [SerializeField] Color outlinePulseColorA = new Color(0.35f, 0.82f, 1f, 1f);
    [SerializeField] Color outlinePulseColorB = new Color(0.68f, 0.95f, 1f, 1f);
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
    [SerializeField] float coreShellExtrusion = 0.012f;
    [SerializeField] float outerShellExtrusion = 0.022f;
    [SerializeField] float coreRimPower = 2.5f;
    [SerializeField] float outerRimPower = 1.35f;

    [Header("Hover")]
    [SerializeField] float hoverScaleMultiplier = 1.1f;
    [Tooltip("Which side of this ritual the hover summary popup appears on.")]
    public PopupPosition SummaryPosition = PopupPosition.Above;

    const uint RenderingLayerRitualHighlight = 1u << 3;
    const string GlowShaderName = "Pharmakos/MeshEdgeMistGlow";
    const string GlowShellRootName = "RitualGlowShells";
    const string CoreGlowSuffix = "_CoreGlow";
    const string OuterGlowSuffix = "_OuterGlow";

    static Material glowSharedMaterial;
    static bool attemptedToLoadGlowShader;

    static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    static readonly int GlowSecondaryColorId = Shader.PropertyToID("_SecondaryColor");
    static readonly int GlowIntensityId = Shader.PropertyToID("_Intensity");
    static readonly int GlowEmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
    static readonly int GlowRimPowerId = Shader.PropertyToID("_RimPower");
    static readonly int GlowShellExtrusionId = Shader.PropertyToID("_ShellExtrusion");
    static readonly int GlowMistSpeedId = Shader.PropertyToID("_MistSpeed");
    static readonly int GlowMistScaleId = Shader.PropertyToID("_MistScale");
    static readonly int GlowMistDetailScaleId = Shader.PropertyToID("_DetailMistScale");
    static readonly int GlowFlickerSpeedId = Shader.PropertyToID("_FlickerSpeed");
    static readonly int GlowFlickerStrengthId = Shader.PropertyToID("_FlickerStrength");
    static readonly int GlowDriftId = Shader.PropertyToID("_Drift");
    static readonly int GlowSeedId = Shader.PropertyToID("_Seed");

    bool isHighlightActive;
    bool usesMeshHighlight;
    Vector3 defaultLocalScale;
    readonly List<Renderer> altarRenderers = new List<Renderer>();
    readonly List<uint> altarDefaultRenderingLayerMasks = new List<uint>();
    readonly List<RitualGlowShell> glowShells = new List<RitualGlowShell>();
    Transform glowShellRoot;
    bool isHoverSummaryShown;

    sealed class RitualGlowShell
    {
        public Renderer Renderer;
        public MaterialPropertyBlock PropertyBlock;
        public bool IsOuter;
        public float Seed;
    }

    void Awake()
    {
        defaultLocalScale = transform.localScale;
        EnsureMeshHighlightSetup();
        SetHighlight(false);
    }

    void LateUpdate()
    {
        UpdateHoverScale();
        UpdateHoverSummary();

        if (!isHighlightActive)
            return;

        float pulseT = GetPulseT();
        if (useOutlineHighlight)
            RitualOutlineHighlight.ApplyPulse(outlinePulseColorA, outlinePulseColorB, pulseT);

        if (!usesMeshHighlight || glowShells.Count == 0)
            return;

        float glowIntensity = Mathf.Lerp(glowPulseMin, glowPulseMax, pulseT);
        ApplyMeshHighlightGlow(glowIntensity);
    }

    float GetPulseT()
    {
        return 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * glowPulseSpeed * Mathf.PI * 2f);
    }

    void OnDisable()
    {
        HideHoverSummary();
    }

    public void Init(Ritual ritual, bool clickable = true)
    {
        HideHoverSummary();
        EnsureMeshHighlightSetup();
        Ritual = ritual;
        Target = Ritual;

        OnClick = RitualClicked;
        Refresh();

        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null) collider.enabled = clickable;
    }

    public void Refresh()
    {
        gameObject.SetActive(Ritual != null);

        if (Ritual == null)
        {
            Title.text = "";
            Description.text = "";
            transform.localScale = defaultLocalScale;
            HideHoverSummary();
            return;
        }

        Title.text = Ritual.Name;
        Description.text = Ritual.Description;

        BloodText.text = Ritual.GetCost(OfferingType.Blood).ToString();
        BonesText.text = Ritual.GetCost(OfferingType.Bone).ToString();
        CropsText.text = Ritual.GetCost(OfferingType.Crop).ToString();
        ScrollsText.text = Ritual.GetCost(OfferingType.Scroll).ToString();
    }

    public void SetHighlight(bool active)
    {
        if (isHighlightActive == active)
            return;

        isHighlightActive = active;

        if (useOutlineHighlight)
            RitualOutlineHighlight.SetActive(active);

        if (usesMeshHighlight)
        {
            SetMeshHighlightActive(active);
            return;
        }

        if (HighlightHolder != null)
            HighlightHolder.SetActive(active);
    }

    public void RitualClicked(ViewTarget viewTarget)
    {
        ViewEventHandler.Instance.FireRitualClicked(viewTarget);
    }

    public void UpdateRitual()
    {
        bool highlight = false;
        if (View.Instance.SelectionHandler.SelectedRitual != null)
        {
            if (View.Instance.SelectionHandler.SelectedRitual == this)
                highlight = true;
        }
        else if (Ritual != null && Ritual.Owner.IsHuman && Ritual.CanPlay() && View.Instance.IsInteractible)
        {
            highlight = true;
        }

        SetHighlight(highlight);
    }

    void UpdateHoverScale()
    {
        float scaleFactor = IsHoveredAndUsable() ? hoverScaleMultiplier : 1f;
        transform.localScale = defaultLocalScale * scaleFactor;
    }

    bool IsHoveredAndUsable()
    {
        if (Ritual == null || View.Instance == null)
            return false;

        if (Ritual.Owner == null || !Ritual.Owner.IsHuman || !Ritual.CanPlay() || !View.Instance.IsInteractible)
            return false;

        if (View.Instance.SelectionHandler.SelectedRitual != null)
            return false;

        return IsHovered();
    }

    bool IsHovered()
    {
        if (View.Instance == null)
            return false;

        if (View.Instance.SelectionHandler.CurrentHover == this)
            return true;

        return MenuSelectionHandler.Instance != null
            && MenuSelectionHandler.Instance.IsActive
            && MenuSelectionHandler.Instance.CurrentHover == this;
    }

    void UpdateHoverSummary()
    {
        if (IsHovered())
            ShowHoverSummary();
        else if (isHoverSummaryShown)
            HideHoverSummary();
    }

    string BuildSummaryText()
    {
        if (Ritual == null)
            return string.Empty;

        string name = Ritual.Name ?? string.Empty;
        if (string.IsNullOrEmpty(Ritual.ReminderText))
            return name;

        string reminder = Controller.Instance != null && Controller.Instance.TextHandler != null
            ? Controller.Instance.TextHandler.ProcessText(Ritual.ReminderText)
            : Ritual.ReminderText;

        return string.IsNullOrEmpty(reminder) ? name : name + "\n" + reminder;
    }

    void ShowHoverSummary()
    {
        string summary = BuildSummaryText();
        if (string.IsNullOrEmpty(summary))
        {
            if (isHoverSummaryShown)
                HideHoverSummary();
            return;
        }

        PopupScreenHandler.Instance?.ShowTextPopup(summary, transform, SummaryPosition);
        isHoverSummaryShown = true;
    }

    void HideHoverSummary()
    {
        PopupScreenHandler.Instance?.HideTextPopup(transform);
        isHoverSummaryShown = false;
    }

    void EnsureMeshHighlightSetup()
    {
        if (!useMeshHighlight || usesMeshHighlight)
            return;

        Transform root = ResolveAltarRoot();
        if (root == null)
            return;

        CollectAltarRenderers(root);
        if (altarRenderers.Count == 0)
            return;

        Material glowMaterial = GetOrCreateGlowMaterial();
        if (glowMaterial == null)
            return;

        glowShellRoot = CreateOrFindGlowShellRoot(root);
        BuildGlowShells(glowMaterial);
        usesMeshHighlight = glowShells.Count > 0;
    }

    Transform ResolveAltarRoot()
    {
        if (altarRoot != null)
            return altarRoot;

        Transform namedRoot = transform.Find("Altar2");
        if (namedRoot != null)
            return namedRoot;

        foreach (Transform child in transform)
        {
            if (child.name.Contains("Altar") && child.GetComponentInChildren<MeshRenderer>() != null)
                return child;
        }

        return null;
    }

    void CollectAltarRenderers(Transform root)
    {
        altarRenderers.Clear();
        altarDefaultRenderingLayerMasks.Clear();

        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer == null || IsGlowShellRenderer(renderer))
                continue;

            altarRenderers.Add(renderer);
            altarDefaultRenderingLayerMasks.Add(renderer.renderingLayerMask);
        }
    }

    static Transform CreateOrFindGlowShellRoot(Transform altarTransform)
    {
        Transform existing = altarTransform.Find(GlowShellRootName);
        if (existing != null)
            return existing;

        var rootObject = new GameObject(GlowShellRootName);
        rootObject.transform.SetParent(altarTransform, false);
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

        foreach (Renderer sourceRenderer in altarRenderers)
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

    RitualGlowShell CreateGlowShell(Transform sourceTransform, Mesh mesh, Material glowMaterial, bool isOuter, float seed)
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

            Transform altarTransform = glowShellRoot.parent;
            shellObject.transform.localPosition = altarTransform.InverseTransformPoint(sourceTransform.position);
            shellObject.transform.localRotation = Quaternion.Inverse(altarTransform.rotation) * sourceTransform.rotation;
            Vector3 altarLossyScale = altarTransform.lossyScale;
            Vector3 sourceLossyScale = sourceTransform.lossyScale;
            shellObject.transform.localScale = new Vector3(
                altarLossyScale.x > 0f ? sourceLossyScale.x / altarLossyScale.x : 1f,
                altarLossyScale.y > 0f ? sourceLossyScale.y / altarLossyScale.y : 1f,
                altarLossyScale.z > 0f ? sourceLossyScale.z / altarLossyScale.z : 1f);

            var meshFilter = shellObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = shellObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = glowMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            renderer = meshRenderer;
        }

        renderer.enabled = false;

        return new RitualGlowShell
        {
            Renderer = renderer,
            PropertyBlock = new MaterialPropertyBlock(),
            IsOuter = isOuter,
            Seed = seed
        };
    }

    void SetMeshHighlightActive(bool active)
    {
        foreach (RitualGlowShell shell in glowShells)
        {
            if (shell.Renderer != null)
                shell.Renderer.enabled = active;
        }

        for (int i = 0; i < altarRenderers.Count; i++)
        {
            Renderer renderer = altarRenderers[i];
            if (renderer == null)
                continue;

            renderer.renderingLayerMask = active && useOutlineHighlight
                ? RenderingLayerRitualHighlight
                : altarDefaultRenderingLayerMasks[i];
        }

        if (active)
            ApplyMeshHighlightGlow(1f);
    }

    void ApplyMeshHighlightGlow(float glowIntensity)
    {
        foreach (RitualGlowShell shell in glowShells)
        {
            if (shell.Renderer == null || shell.PropertyBlock == null)
                continue;

            Color glowColor = shell.IsOuter ? glowOuterColor : glowCoreColor;
            float intensityMultiplier = shell.IsOuter ? glowOuterIntensityMultiplier : glowCoreIntensityMultiplier;
            float emissionStrength = shell.IsOuter ? glowOuterEmissionStrength : glowCoreEmissionStrength;
            float rimPower = shell.IsOuter ? outerRimPower : coreRimPower;
            float shellExtrusion = shell.IsOuter ? outerShellExtrusion : coreShellExtrusion;

            ApplyGlowToRenderer(
                shell.Renderer,
                shell.PropertyBlock,
                glowColor,
                glowIntensity * intensityMultiplier,
                emissionStrength,
                rimPower,
                shellExtrusion,
                shell.Seed);
        }
    }

    void ApplyGlowToRenderer(
        Renderer renderer,
        MaterialPropertyBlock propertyBlock,
        Color glowColor,
        float intensity,
        float emissionStrength,
        float rimPower,
        float shellExtrusion,
        float seed)
    {
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(GlowColorId, glowColor);
        propertyBlock.SetColor(GlowSecondaryColorId, glowSecondaryColor);
        propertyBlock.SetFloat(GlowIntensityId, intensity);
        propertyBlock.SetFloat(GlowEmissionStrengthId, emissionStrength);
        propertyBlock.SetFloat(GlowRimPowerId, rimPower);
        propertyBlock.SetFloat(GlowShellExtrusionId, shellExtrusion);
        propertyBlock.SetFloat(GlowMistSpeedId, glowMistSpeed);
        propertyBlock.SetFloat(GlowMistScaleId, glowMistScale);
        propertyBlock.SetFloat(GlowMistDetailScaleId, glowMistDetailScale);
        propertyBlock.SetFloat(GlowFlickerSpeedId, glowFlickerSpeed);
        propertyBlock.SetFloat(GlowFlickerStrengthId, glowFlickerStrength);
        propertyBlock.SetFloat(GlowDriftId, glowDrift);
        propertyBlock.SetFloat(GlowSeedId, seed);
        renderer.SetPropertyBlock(propertyBlock);
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
            name = "MeshEdgeMistGlow_Runtime"
        };

        return glowSharedMaterial;
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
