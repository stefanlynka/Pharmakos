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

    public MeshHighlightEffect MeshHighlightEffect;
    [SerializeField] Transform altarRoot;

    public Light Spotlight;

    [Header("Spotlight")]
    [SerializeField] float unusableInnerAngle = 40f;
    [SerializeField] float unusableOuterAngle = 55f;
    [SerializeField] float unusableIntensity = 150f;
    [SerializeField] float flareInnerAngle = 50f;
    [SerializeField] float flareOuterAngle = 70f;
    [SerializeField] float flareIntensity = 500f;
    [SerializeField] float usableInnerAngle = 40f;
    [SerializeField] float usableOuterAngle = 55f;
    [SerializeField] float usableIntensity = 250f;
    [Tooltip("While usable and not selected, intensity oscillates between Usable Intensity and this value.")]
    [SerializeField] float usablePulseIntensity = 350f;
    [Tooltip("Full oscillation cycles per second while usable and not selected.")]
    [SerializeField] float usablePulseSpeed = 0.5f;
    [SerializeField] float flareUpDuration = 0.18f;
    [SerializeField] float flareSettleDuration = 0.5f;
    [SerializeField] float dimDuration = 0.35f;

    [Header("Hover")]
    [SerializeField] float hoverScaleMultiplier = 1.1f;
    [Tooltip("Which side of this ritual the hover summary popup appears on.")]
    public PopupPosition SummaryPosition = PopupPosition.Above;

    [Header("Idle Rattle")]
    [SerializeField] float rattleIntervalMin = 4f;
    [SerializeField] float rattleIntervalMax = 6f;
    [SerializeField] float rattleDuration = 0.1f;
    [SerializeField] float rattleAngleDegrees = 1.75f;
    [Tooltip("Multiplier for rattle angle and shake speed. 0 = none, 1 = default.")]
    [SerializeField] float rattleIntensity = 1f;

    bool isHighlightActive;
    Vector3 defaultLocalScale;
    bool isHoverSummaryShown;

    public Transform rattleTarget;
    Quaternion defaultRattleLocalRotation;
    bool hasDefaultRattleRotation;
    bool isRattling;
    float nextRattleAt = -1f;
    float rattleEndTime;

    enum SpotlightPhase
    {
        Hold,
        FlareUp,
        Settle,
        Dim,
    }

    struct SpotlightProfile
    {
        public float Inner;
        public float Outer;
        public float Intensity;

        public static SpotlightProfile Lerp(SpotlightProfile a, SpotlightProfile b, float t)
        {
            return new SpotlightProfile
            {
                Inner = Mathf.Lerp(a.Inner, b.Inner, t),
                Outer = Mathf.Lerp(a.Outer, b.Outer, t),
                Intensity = Mathf.Lerp(a.Intensity, b.Intensity, t),
            };
        }
    }

    SpotlightPhase spotlightPhase = SpotlightPhase.Hold;
    bool spotlightIsUsable;
    SpotlightProfile spotlightFrom;
    float spotlightElapsed;
    float pulseTime;
    float pulseWeight;
    const float PulseBlendDuration = 0.25f;

    void Awake()
    {
        defaultLocalScale = transform.localScale;
        EnsureMeshHighlight();
        SetHighlight(false);
        ApplySpotlight(UnusableSpotlight());
    }

    void OnEnable()
    {
        spotlightIsUsable = false;
        spotlightPhase = SpotlightPhase.Hold;
        ApplySpotlight(UnusableSpotlight());
    }

    void LateUpdate()
    {
        UpdateHoverScale();
        UpdateHoverSummary();
        UpdateIdleRattle();
        UpdateSpotlight();
    }

    void OnDisable()
    {
        if (isHighlightActive)
            SetHighlight(false);

        HideHoverSummary();
        StopIdleRattle(resetSchedule: true);
        if (spotlightIsUsable)
            NotifyRitualFlame(false);
        spotlightIsUsable = false;
        spotlightPhase = SpotlightPhase.Hold;
    }

    public void Init(Ritual ritual, bool clickable = true)
    {
        HideHoverSummary();
        EnsureMeshHighlight();
        Ritual = ritual;
        Target = Ritual;

        OnClick = RitualClicked;
        Refresh();

        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null) collider.enabled = clickable;
    }

    public void SetDisplayScale(Vector3 scale)
    {
        defaultLocalScale = scale;
        transform.localScale = scale;
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
        EnsureMeshHighlight();

        if (MeshHighlightEffect != null)
            MeshHighlightEffect.SetActive(active);
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
        else if (Ritual != null && Ritual.Owner.IsHuman && CanUseFromDisplayedOfferings() && View.Instance.IsInteractible)
        {
            highlight = true;
        }

        SetHighlight(highlight);
    }

    public void SyncOfferingReadyState()
    {
        UpdateSpotlight();
    }

    public bool HasDisplayedOfferings()
    {
        if (Ritual == null || Ritual.Owner == null)
            return false;

        if (View.Instance == null)
            return Ritual.Owner.CanPayForRitual(Ritual);

        ViewPlayer viewPlayer = View.Instance.GetViewPlayer(Ritual.Owner);
        if (viewPlayer == null || viewPlayer.ViewResources == null)
            return Ritual.Owner.CanPayForRitual(Ritual);

        return viewPlayer.ViewResources.HasDisplayedOfferingsFor(Ritual);
    }

    bool CanUseFromDisplayedOfferings()
    {
        return HasDisplayedOfferings() && Ritual != null && Ritual.CanPlay();
    }

    bool IsAffordableByPlayer()
    {
        return Ritual != null
            && Ritual.Owner != null
            && Ritual.Owner.IsHuman
            && HasDisplayedOfferings();
    }

    SpotlightProfile UnusableSpotlight()
    {
        return new SpotlightProfile
        {
            Inner = unusableInnerAngle,
            Outer = unusableOuterAngle,
            Intensity = unusableIntensity,
        };
    }

    SpotlightProfile FlareSpotlight()
    {
        return new SpotlightProfile
        {
            Inner = flareInnerAngle,
            Outer = flareOuterAngle,
            Intensity = flareIntensity,
        };
    }

    SpotlightProfile UsableSpotlight()
    {
        return new SpotlightProfile
        {
            Inner = usableInnerAngle,
            Outer = usableOuterAngle,
            Intensity = usableIntensity,
        };
    }

    SpotlightProfile CurrentSpotlight()
    {
        if (Spotlight == null)
            return UnusableSpotlight();

        return new SpotlightProfile
        {
            Inner = Spotlight.innerSpotAngle,
            Outer = Spotlight.spotAngle,
            Intensity = Spotlight.intensity,
        };
    }

    void ApplySpotlight(SpotlightProfile profile)
    {
        if (Spotlight == null)
            return;

        Spotlight.spotAngle = profile.Outer;
        Spotlight.innerSpotAngle = Mathf.Min(profile.Inner, profile.Outer);
        Spotlight.intensity = profile.Intensity;
    }

    void UpdateSpotlight()
    {
        if (Spotlight == null)
            return;

        bool usable = IsAffordableByPlayer();
        if (usable != spotlightIsUsable)
        {
            spotlightIsUsable = usable;
            spotlightFrom = CurrentSpotlight();
            spotlightElapsed = 0f;
            spotlightPhase = usable ? SpotlightPhase.FlareUp : SpotlightPhase.Dim;
            NotifyRitualFlame(usable);
        }

        if (spotlightPhase == SpotlightPhase.Hold)
        {
            if (spotlightIsUsable)
                UpdateUsablePulse();
            return;
        }

        float duration = dimDuration;
        SpotlightProfile target = UnusableSpotlight();
        if (spotlightPhase == SpotlightPhase.FlareUp)
        {
            duration = flareUpDuration;
            target = FlareSpotlight();
        }
        else if (spotlightPhase == SpotlightPhase.Settle)
        {
            duration = flareSettleDuration;
            target = UsableSpotlight();
        }

        spotlightElapsed += Time.deltaTime;
        float t = duration <= 0f ? 1f : Mathf.Clamp01(spotlightElapsed / duration);
        float eased = spotlightPhase == SpotlightPhase.FlareUp ? EaseOutQuad(t) : EaseOutSine(t);
        ApplySpotlight(SpotlightProfile.Lerp(spotlightFrom, target, eased));

        if (t < 1f)
            return;

        if (spotlightPhase == SpotlightPhase.FlareUp && spotlightIsUsable)
        {
            spotlightPhase = SpotlightPhase.Settle;
            spotlightFrom = FlareSpotlight();
            spotlightElapsed = 0f;
            return;
        }

        spotlightPhase = SpotlightPhase.Hold;
        ApplySpotlight(spotlightIsUsable ? UsableSpotlight() : UnusableSpotlight());

        // Wave starts at 0 so the pulse begins exactly at usableIntensity with no pop.
        pulseTime = 0f;
        pulseWeight = IsSelected() ? 0f : 1f;
    }

    void UpdateUsablePulse()
    {
        float targetWeight = IsSelected() ? 0f : 1f;
        pulseWeight = Mathf.MoveTowards(pulseWeight, targetWeight, Time.deltaTime / PulseBlendDuration);
        pulseTime += Time.deltaTime * Mathf.Max(0f, usablePulseSpeed);

        float wave = 0.5f - 0.5f * Mathf.Cos(pulseTime * Mathf.PI * 2f);
        Spotlight.intensity = Mathf.Lerp(usableIntensity, usablePulseIntensity, wave * pulseWeight);
    }

    bool IsSelected()
    {
        return View.Instance != null
            && View.Instance.SelectionHandler != null
            && View.Instance.SelectionHandler.SelectedRitual == this;
    }

    void NotifyRitualFlame(bool ready)
    {
        if (View.Instance == null || View.Instance.AudioHandler == null)
            return;

        if (ready)
            View.Instance.AudioHandler.NotifyRitualReady(this);
        else
            View.Instance.AudioHandler.NotifyRitualUnready(this);
    }

    static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    static float EaseOutSine(float t)
    {
        return Mathf.Sin(t * Mathf.PI * 0.5f);
    }

    void EnsureMeshHighlight()
    {
        if (MeshHighlightEffect != null)
            return;

        Transform root = ResolveAltarRoot();
        if (root != null)
            MeshHighlightEffect = root.GetComponent<MeshHighlightEffect>();
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

    void UpdateHoverScale()
    {
        float scaleFactor = IsHoveredAndUsable() ? hoverScaleMultiplier : 1f;
        transform.localScale = defaultLocalScale * scaleFactor;
    }

    bool IsHoveredAndUsable()
    {
        if (Ritual == null || View.Instance == null)
            return false;

        if (Ritual.Owner == null || !Ritual.Owner.IsHuman || !CanUseFromDisplayedOfferings() || !View.Instance.IsInteractible)
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

    bool IsReadyToUse()
    {
        if (Ritual == null || View.Instance == null)
            return false;

        if (View.Instance.SelectionHandler.SelectedRitual != null
            && View.Instance.SelectionHandler.SelectedRitual != this)
            return false;

        return Ritual.Owner != null
            && Ritual.Owner.IsHuman
            && CanUseFromDisplayedOfferings()
            && View.Instance.IsInteractible;
    }

    bool ShouldIdleRattle()
    {
        return IsReadyToUse()
            && !IsHovered()
            && View.Instance.SelectionHandler.SelectedRitual != this;
    }

    void EnsureRattleTarget()
    {
        // Prefab may wire rattleTarget to this transform; prefer the altar mesh.
        if (rattleTarget == null || rattleTarget == transform)
        {
            Transform altar = ResolveAltarRoot();
            if (altar != null)
                rattleTarget = altar;
        }

        if (rattleTarget == null)
            return;

        if (!hasDefaultRattleRotation)
        {
            defaultRattleLocalRotation = rattleTarget.localRotation;
            hasDefaultRattleRotation = true;
        }
    }

    void UpdateIdleRattle()
    {
        if (!ShouldIdleRattle())
        {
            StopIdleRattle(resetSchedule: true);
            return;
        }

        EnsureRattleTarget();
        if (rattleTarget == null)
            return;

        if (isRattling)
        {
            ApplyIdleRattle();
            if (Time.unscaledTime >= rattleEndTime)
                StopIdleRattle(resetSchedule: false);
            return;
        }

        if (nextRattleAt < 0f)
            ScheduleNextRattle();

        if (Time.unscaledTime >= nextRattleAt)
            StartIdleRattle();
    }

    void ScheduleNextRattle()
    {
        float interval = Random.Range(rattleIntervalMin, rattleIntervalMax);
        nextRattleAt = Time.unscaledTime + interval;
    }

    void StartIdleRattle()
    {
        EnsureRattleTarget();
        if (rattleTarget == null)
            return;

        isRattling = true;
        rattleEndTime = Time.unscaledTime + rattleDuration;
        ApplyIdleRattle();
    }

    void ApplyIdleRattle()
    {
        if (rattleTarget == null || !hasDefaultRattleRotation)
            return;

        float elapsed = rattleDuration - (rattleEndTime - Time.unscaledTime);
        float t = Mathf.Clamp01(elapsed / rattleDuration);
        float dampen = 1f - t;
        float intensity = Mathf.Max(0f, rattleIntensity);
        float wobble = dampen * rattleAngleDegrees * intensity;
        float x = Mathf.Sin(t * 50f * intensity) * wobble;
        float z = Mathf.Cos(t * 43f * intensity) * wobble * 0.75f;
        rattleTarget.localRotation = defaultRattleLocalRotation * Quaternion.Euler(x, 0f, z);
    }

    void StopIdleRattle(bool resetSchedule)
    {
        if (isRattling && rattleTarget != null && hasDefaultRattleRotation)
            rattleTarget.localRotation = defaultRattleLocalRotation;

        isRattling = false;

        if (resetSchedule)
            nextRattleAt = -1f;
        else
            ScheduleNextRattle();
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
}
