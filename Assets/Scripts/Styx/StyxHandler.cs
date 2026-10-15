using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The River Styx 3D area. The player's current deck, rituals, and trinkets are laid out on the altar; crossing floods
/// the area with water and, while it is covered, swaps them for the Styx content (the Styx deck, removed rituals, and
/// removed trinkets). The player then chooses up to two of the removed rituals (first pick = Major, second = Minor)
/// before the exchange is applied.
/// The scene objects are built by Pharmakos/Setup Styx Area; the UI text and button live in StyxScreenHandler.
/// </summary>
public class StyxHandler : MonoBehaviour
{
    public enum StyxPhase
    {
        Inactive,
        Offering,
        Flooding,
        Choosing,
        Leaving,
    }

    const string RitualPrefabPath = "Prefabs/View/ViewRitual3";
    const string TrinketPrefabPath = "Prefabs/View/ViewBuff";
    const int MaxChosenRituals = 2;

    public Camera StyxCamera;
    public StyxScreenHandler StyxScreenHandler;
    public ViewCardScroller CardScroller;
    public Transform RitualContainer;
    public Transform TrinketContainer;
    [Tooltip("Faces the camera (local -Z towards it); the water flood is painted on its local XY plane.")]
    public ViewTarget FloodAnchor;

    [Header("Rituals")]
    public float RitualScale = 0.55f;
    public int RitualColumns = 2;
    public Vector2 RitualSpacing = new Vector2(16f, 14f);
    [Tooltip("Rows shown before the rituals need scrolling.")]
    public int RitualVisibleRows = 3;
    [Tooltip("Ritual rows scrolled per second.")]
    public float RitualScrollSpeed = 8f;

    [Header("Scrolling")]
    [Tooltip("Viewport X (0-1) splitting the screen: the mouse wheel scrolls the cards left of it and the rituals right of it.")]
    public float ScrollSplit = 0.5f;

    [Header("Trinkets")]
    public float TrinketScale = 2.5f;
    public int TrinketColumns = 2;
    public Vector2 TrinketSpacing = new Vector2(7f, 7f);

    [Header("Flood")]
    [Tooltip("How much larger than the camera's view (at the anchor's distance) the flooded area is.")]
    public float FloodCoverageMargin = 1.15f;
    [Tooltip("Coverage cells across the flooded area; fewer cells means larger strokes.")]
    public int FloodColumns = 9;
    [Tooltip("Stroke size in coverage cells.")]
    public float FloodStrokeCells = 2f;
    [Tooltip("Strokes kept alive on each covered cell at once; higher fills gaps as strokes paint in and wipe out.")]
    public float FloodStrokesPerCell = 2f;

    public StyxPhase Phase { get; private set; } = StyxPhase.Inactive;

    GameObject ritualPrefab;
    GameObject trinketPrefab;

    readonly List<ViewRitual> ritualViews = new List<ViewRitual>();
    readonly List<Transform> ritualTransforms = new List<Transform>();
    readonly List<ViewBuff> trinketViews = new List<ViewBuff>();
    readonly List<Ritual> chosenRituals = new List<Ritual>();

    float ritualScrollOffset;
    float ritualScrollTarget;
    float ritualMaxScroll;

    void Update()
    {
        if (Phase == StyxPhase.Inactive) return;
        UpdateRitualScroll();
    }

    public void BeginStyx()
    {
        if (Controller.Instance == null || View.Instance == null)
        {
            Debug.LogError("StyxHandler: Controller or View is missing.");
            return;
        }

        StopAllCoroutines();
        gameObject.SetActive(true);
        SetCameraActive(true);

        chosenRituals.Clear();
        if (CardScroller != null)
            CardScroller.ScrollRegion = new Rect(0f, 0f, ScrollSplit, 1f);
        ShowPlayerContent();
        Phase = StyxPhase.Offering;

        if (View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Activate(StyxCamera, () => Phase != StyxPhase.Choosing);

        if (StyxScreenHandler != null)
            StyxScreenHandler.ShowOffering();
    }

    public void EndStyx()
    {
        StopAllCoroutines();
        ClearContent();
        chosenRituals.Clear();
        Phase = StyxPhase.Inactive;

        SetCameraActive(false);
        gameObject.SetActive(false);

        if (View.Instance != null && View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Deactivate();
    }

    public void CrossStyx()
    {
        if (Phase != StyxPhase.Offering) return;
        StartCoroutine(CrossStyxRoutine());
    }

    public void Continue()
    {
        if (Phase != StyxPhase.Choosing) return;
        Phase = StyxPhase.Leaving;

        if (StyxScreenHandler != null)
            StyxScreenHandler.ShowLeaving();

        Controller.Instance.ApplyStyxExchange(new List<Ritual>(chosenRituals));
        Controller.Instance.StartNextLevel();
    }

    IEnumerator CrossStyxRoutine()
    {
        Phase = StyxPhase.Flooding;
        if (StyxScreenHandler != null)
            StyxScreenHandler.ShowFlooding();

        if (FloodAnchor != null)
        {
            BrushStrokeFlurryVfx flood = BuildFlood();
            bool floodDone = false;
            flood.Play(new VfxContext(null, FloodAnchor), () => floodDone = true);

            // The whole area is covered once the sweep has crossed it and the last strokes have painted in
            yield return new WaitForSeconds(flood.SweepDuration + flood.PaintInDuration);
            ShowStyxContent();

            while (!floodDone) yield return null;
        }
        else
        {
            ShowStyxContent();
        }

        Phase = StyxPhase.Choosing;
        if (StyxScreenHandler != null)
            StyxScreenHandler.ShowChoosing();
    }

    BrushStrokeFlurryVfx BuildFlood()
    {
        BrushStrokeFlurryVfx flood = VfxLibrary.WaterFlood();
        if (StyxCamera == null) return flood;

        Transform anchor = FloodAnchor.transform;
        Transform cameraTransform = StyxCamera.transform;
        float distance = Mathf.Max(StyxCamera.nearClipPlane, Vector3.Dot(anchor.position - cameraTransform.position, cameraTransform.forward));
        float viewHeight = StyxCamera.orthographic
            ? 2f * StyxCamera.orthographicSize
            : 2f * distance * Mathf.Tan(StyxCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        Vector3 anchorScale = anchor.lossyScale;
        Vector2 area = new Vector2(
            viewHeight * StyxCamera.aspect / Mathf.Max(0.0001f, Mathf.Abs(anchorScale.x)),
            viewHeight / Mathf.Max(0.0001f, Mathf.Abs(anchorScale.y))) * FloodCoverageMargin;

        Vector3 viewCenter = anchor.InverseTransformPoint(cameraTransform.position + cameraTransform.forward * distance);
        flood.AreaCenter = new Vector2(viewCenter.x, viewCenter.y);
        flood.AreaSize = area;

        float cell = area.x / Mathf.Max(1, FloodColumns);
        flood.CoverageCellSize = new Vector2(cell, cell);
        int cellCount = Mathf.Max(1, FloodColumns) * Mathf.Max(1, Mathf.RoundToInt(area.y / cell));

        float spriteSize = GetFloodSpriteSize(flood.SpriteFolder);
        float scale = FloodStrokeCells * cell / spriteSize;
        flood.ScaleMin = scale * 0.8f;
        flood.ScaleMax = scale * 1.25f;

        // Enough to replace each covered cell's strokes as they expire
        flood.PeakRate = cellCount * FloodStrokesPerCell / Mathf.Max(0.05f, flood.StrokeLifetime);
        return flood;
    }

    static float GetFloodSpriteSize(string folder)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(folder);
        if (sprites.Length == 0) return 1f;
        Vector3 size = sprites[0].bounds.size;
        return Mathf.Max(0.0001f, Mathf.Max(size.x, size.y));
    }

    void ShowPlayerContent()
    {
        PlayerDetails details = Controller.Instance.HumanPlayerDetails;

        details.DeckBlueprint.TryGetValue(0, out List<Card> deck);
        details.MajorRituals.TryGetValue(0, out Ritual major);
        details.MinorRituals.TryGetValue(0, out Ritual minor);
        details.Trinkets.TryGetValue(0, out List<Trinket> trinkets);

        List<Ritual> rituals = new List<Ritual>();
        if (major != null) rituals.Add(major);
        if (minor != null) rituals.Add(minor);

        ShowContent(deck ?? new List<Card>(), rituals, false, trinkets ?? new List<Trinket>());
    }

    void ShowStyxContent()
    {
        StyxRunState styx = Controller.Instance.StyxRunState;
        ShowContent(styx.GetStyxDeck(), styx.RemovedRituals, true, styx.RemovedTrinkets);

        chosenRituals.Clear();
        for (int i = 0; i < ritualViews.Count && chosenRituals.Count < MaxChosenRituals; i++)
            chosenRituals.Add(ritualViews[i].Ritual);
        RefreshRitualHighlights();
    }

    void ShowContent(List<Card> deck, List<Ritual> rituals, bool ritualsSelectable, List<Trinket> trinkets)
    {
        ClearContent();

        if (CardScroller != null)
            CardScroller.Load(deck);

        ShowRituals(rituals, ritualsSelectable);
        ShowTrinkets(trinkets);
    }

    void ShowRituals(List<Ritual> rituals, bool selectable)
    {
        if (RitualContainer == null) return;
        if (ritualPrefab == null) ritualPrefab = Resources.Load<GameObject>(RitualPrefabPath);
        if (ritualPrefab == null)
        {
            Debug.LogError("StyxHandler: missing Resources/" + RitualPrefabPath);
            return;
        }

        foreach (Ritual ritual in rituals)
        {
            if (ritual == null) continue;

            GameObject ritualObject = Instantiate(ritualPrefab, RitualContainer, false);
            ritualObject.name = "Ritual_" + ritual.Name;
            ViewRitual view = ritualObject.GetComponent<ViewRitual>();
            view.SetDisplayScale(Vector3.one * RitualScale);
            // The collider stays on either way so the hover summary can be read
            view.Init(ritual, true);
            view.OnClick = selectable ? OnRitualClicked : (System.Action<ViewTarget>)null;
            view.SetHighlight(false);

            ritualViews.Add(view);
            ritualTransforms.Add(ritualObject.transform);
        }

        ritualScrollOffset = 0f;
        ritualScrollTarget = 0f;
        ritualMaxScroll = LayoutGrid(ritualTransforms, RitualColumns, RitualSpacing, RitualVisibleRows, 0f);
    }

    void UpdateRitualScroll()
    {
        if (ritualTransforms.Count == 0 || ritualMaxScroll <= 0f) return;

        float wheel = Input.mouseScrollDelta.y;
        if (wheel != 0f && UnityEngine.Screen.width > 0 && Input.mousePosition.x / UnityEngine.Screen.width >= ScrollSplit)
        {
            // Wheel down moves further down the list, matching the card grid
            ritualScrollTarget = Mathf.Clamp(ritualScrollTarget - Mathf.Sign(wheel) * RitualSpacing.y, 0f, ritualMaxScroll);
        }

        if (Mathf.Approximately(ritualScrollOffset, ritualScrollTarget)) return;

        ritualScrollOffset = Mathf.MoveTowards(ritualScrollOffset, ritualScrollTarget, RitualScrollSpeed * RitualSpacing.y * Time.deltaTime);
        LayoutGrid(ritualTransforms, RitualColumns, RitualSpacing, RitualVisibleRows, ritualScrollOffset);
    }

    void ShowTrinkets(List<Trinket> trinkets)
    {
        if (TrinketContainer == null) return;
        if (trinketPrefab == null) trinketPrefab = Resources.Load<GameObject>(TrinketPrefabPath);
        if (trinketPrefab == null)
        {
            Debug.LogError("StyxHandler: missing Resources/" + TrinketPrefabPath);
            return;
        }

        List<Transform> placed = new List<Transform>();
        foreach (Trinket trinket in trinkets)
        {
            PlayerEffectDescriptionData descriptionData = trinket != null ? trinket.GetDescriptionData() : null;
            if (descriptionData == null) continue;

            GameObject trinketObject = Instantiate(trinketPrefab, TrinketContainer, false);
            trinketObject.name = "Trinket_" + trinket.Name;
            trinketObject.transform.localScale = Vector3.one * TrinketScale;
            ViewBuff view = trinketObject.GetComponent<ViewBuff>();
            view.SetBuffData(trinket.MyEffect, descriptionData);
            view.SetVisible(true);

            trinketViews.Add(view);
            placed.Add(trinketObject.transform);
        }

        LayoutGrid(placed, TrinketColumns, TrinketSpacing);
    }

    // Lays the items out from the container's origin (the first item's position) in rows of up to `columns`, left to
    // right then top to bottom, shifted up by scrollOffset. Returns how far it can scroll to show rows past visibleRows.
    static float LayoutGrid(List<Transform> items, int columns, Vector2 spacing, int visibleRows = 0, float scrollOffset = 0f)
    {
        int count = items.Count;
        if (count == 0) return 0f;

        int columnCount = Mathf.Max(1, columns);
        int rowCount = Mathf.CeilToInt(count / (float)columnCount);

        for (int i = 0; i < count; i++)
        {
            Transform item = items[i];
            item.localPosition = new Vector3(
                (i % columnCount) * spacing.x,
                -(i / columnCount) * spacing.y + scrollOffset,
                0f);
            item.localRotation = Quaternion.identity;
        }

        return visibleRows > 0 ? Mathf.Max(0, rowCount - visibleRows) * spacing.y : 0f;
    }

    void OnRitualClicked(ViewTarget target)
    {
        if (Phase != StyxPhase.Choosing) return;

        ViewRitual view = target as ViewRitual;
        if (view == null || view.Ritual == null) return;

        if (chosenRituals.Contains(view.Ritual))
        {
            chosenRituals.Remove(view.Ritual);
        }
        else
        {
            if (chosenRituals.Count >= MaxChosenRituals)
                chosenRituals.RemoveAt(0);
            chosenRituals.Add(view.Ritual);
        }

        RefreshRitualHighlights();
    }

    void RefreshRitualHighlights()
    {
        foreach (ViewRitual ritualView in ritualViews)
            ritualView.SetHighlight(chosenRituals.Contains(ritualView.Ritual));
    }

    void ClearContent()
    {
        if (CardScroller != null)
            CardScroller.Exit();

        foreach (ViewRitual view in ritualViews)
            if (view != null) Destroy(view.gameObject);
        ritualViews.Clear();
        ritualTransforms.Clear();
        ritualMaxScroll = 0f;

        foreach (ViewBuff view in trinketViews)
            if (view != null) Destroy(view.gameObject);
        trinketViews.Clear();
    }

    void SetCameraActive(bool active)
    {
        if (StyxCamera == null) return;

        StyxCamera.gameObject.SetActive(active);
        StyxCamera.enabled = active;
    }
}
