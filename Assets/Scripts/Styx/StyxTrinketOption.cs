using UnityEngine;

/// <summary>
/// A 3D Styx trinket on the select altar (shears or hammer). Click toggles whether it
/// is taken this run. Only a selected object uses the altar mesh highlight.
/// Hover shows the trinket name and effect in the shared text popup.
/// </summary>
public class StyxTrinketOption : ViewTarget
{
    public enum Kind
    {
        StringsOfFate,
        GatesBeyond,
    }

    const int TargetLayer = 6;

    public Kind TrinketKind;
    public MeshHighlightEffect MeshHighlightEffect;

    [Tooltip("Click toggles whether this trinket is taken. Off for the unlock reveal, which is hover only.")]
    public bool Selectable = true;

    [Tooltip("Which side of this object the effect popup appears on.")]
    public PopupPosition SummaryPosition = PopupPosition.Above;

    public Trinket Trinket { get; private set; }
    public bool Selected { get; private set; }

    bool isHighlightActive;
    bool isHoverSummaryShown;

    void Awake()
    {
        gameObject.layer = TargetLayer;
        FitColliderToMesh();
        OnClick = HandleClick;
        SetHighlight(false);
    }

    void LateUpdate()
    {
        if (Trinket == null)
            return;

        if (Selectable)
            SetHighlight(Selected);
        UpdateHoverSummary();
    }

    void OnDisable()
    {
        SetHighlight(false);
        HideHoverSummary();
    }

    public void Configure(Trinket trinket)
    {
        Trinket = trinket;
        Selected = false;
        HideHoverSummary();
        SetHighlight(false);
        gameObject.SetActive(trinket != null);
    }

    public void Hide()
    {
        Selected = false;
        HideHoverSummary();
        SetHighlight(false);
    }

    void HandleClick(ViewTarget viewTarget)
    {
        if (!Selectable || Trinket == null)
            return;

        Selected = !Selected;
        SetHighlight(Selected);
    }

    public void SetHighlight(bool active)
    {
        if (isHighlightActive == active)
            return;

        isHighlightActive = active;
        if (MeshHighlightEffect == null)
            MeshHighlightEffect = GetComponent<MeshHighlightEffect>();

        if (MeshHighlightEffect != null)
            MeshHighlightEffect.SetActive(active);
    }

    bool IsHovered()
    {
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
        if (Trinket == null)
            return string.Empty;

        string name = Trinket.Name ?? string.Empty;
        string description = Trinket.Description ?? string.Empty;
        if (string.IsNullOrEmpty(description))
            return name;
        if (string.IsNullOrEmpty(name))
            return description;

        return name + "\n" + description;
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

        Camera camera = null;
        StyxTrinketArea area = GetComponentInParent<StyxTrinketArea>();
        if (area != null)
            camera = area.AreaCamera;
        else
        {
            StyxTrinketUnlockArea unlockArea = GetComponentInParent<StyxTrinketUnlockArea>();
            if (unlockArea != null)
                camera = unlockArea.AreaCamera;
        }

        PopupScreenHandler.Instance?.ShowTextPopup(summary, transform, SummaryPosition, camera);
        isHoverSummaryShown = true;
    }

    void HideHoverSummary()
    {
        PopupScreenHandler.Instance?.HideTextPopup(transform);
        isHoverSummaryShown = false;
    }

    void FitColliderToMesh()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            return;

        bool hasBounds = false;
        Bounds localBounds = new Bounds();
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter.sharedMesh == null || IsHighlightShell(filter.transform))
                continue;

            EncapsulateMesh(filter, ref localBounds, ref hasBounds);
        }

        if (!hasBounds)
            return;

        Vector3 size = localBounds.size;
        size.x = Mathf.Max(size.x, 0.05f);
        size.y = Mathf.Max(size.y, 0.05f);
        size.z = Mathf.Max(size.z, 0.05f);
        box.center = localBounds.center;
        box.size = size;
    }

    void EncapsulateMesh(MeshFilter filter, ref Bounds localBounds, ref bool hasBounds)
    {
        Bounds meshBounds = filter.sharedMesh.bounds;
        Vector3 center = meshBounds.center;
        Vector3 extents = meshBounds.extents;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 meshCorner = center + new Vector3(
                (corner & 1) == 0 ? -extents.x : extents.x,
                (corner & 2) == 0 ? -extents.y : extents.y,
                (corner & 4) == 0 ? -extents.z : extents.z);
            Vector3 local = transform.InverseTransformPoint(filter.transform.TransformPoint(meshCorner));
            if (!hasBounds)
            {
                localBounds = new Bounds(local, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                localBounds.Encapsulate(local);
            }
        }
    }

    bool IsHighlightShell(Transform target)
    {
        Transform current = target;
        while (current != null && current != transform)
        {
            if (current.name == "FlameHighlightShells")
                return true;
            current = current.parent;
        }

        return target.name.EndsWith("_CoreFlame") || target.name.EndsWith("_OuterFlame");
    }
}
