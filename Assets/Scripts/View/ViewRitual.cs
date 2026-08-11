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

    [Header("Hover")]
    [SerializeField] float hoverScaleMultiplier = 1.1f;
    [Tooltip("Which side of this ritual the hover summary popup appears on.")]
    public PopupPosition SummaryPosition = PopupPosition.Above;

    [Header("Idle Rattle")]
    [SerializeField] float rattleIntervalMin = 4f;
    [SerializeField] float rattleIntervalMax = 6f;
    [SerializeField] float rattleDuration = 0.1f;
    [SerializeField] float rattleAngleDegrees = 1.75f;

    bool isHighlightActive;
    Vector3 defaultLocalScale;
    bool isHoverSummaryShown;

    public Transform rattleTarget;
    Quaternion defaultRattleLocalRotation;
    bool hasDefaultRattleRotation;
    bool isRattling;
    float nextRattleAt = -1f;
    float rattleEndTime;

    void Awake()
    {
        defaultLocalScale = transform.localScale;
        EnsureMeshHighlight();
        SetHighlight(false);
    }

    void LateUpdate()
    {
        UpdateHoverScale();
        UpdateHoverSummary();
        UpdateIdleRattle();
    }

    void OnDisable()
    {
        if (isHighlightActive)
            SetHighlight(false);

        HideHoverSummary();
        StopIdleRattle(resetSchedule: true);
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
        else if (Ritual != null && Ritual.Owner.IsHuman && Ritual.CanPlay() && View.Instance.IsInteractible)
        {
            highlight = true;
        }

        SetHighlight(highlight);
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

    bool IsReadyToUse()
    {
        if (Ritual == null || View.Instance == null)
            return false;

        if (View.Instance.SelectionHandler.SelectedRitual != null
            && View.Instance.SelectionHandler.SelectedRitual != this)
            return false;

        return Ritual.Owner != null
            && Ritual.Owner.IsHuman
            && Ritual.CanPlay()
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
        float wobble = dampen * rattleAngleDegrees;
        float x = Mathf.Sin(t * 50f) * wobble;
        float z = Mathf.Cos(t * 43f) * wobble * 0.75f;
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
