using UnityEngine;

public class OverworldScreen : Screen
{
    public OverworldMapController MapController;
    [Tooltip("Game camera to disable when overworld is shown. Assign Main Camera.")]
    public Camera MainCamera;

    [Tooltip("Overworld camera depth while the map is viewed from another screen. Must be above the other screens' cameras (Main, Event, Altar, Shop...) and below the Status camera, so the map covers the current screen and Status still opens over it.")]
    public float MapViewCameraDepth = 5f;

    const string LegendObjectName = "OverworldLegend";

    CanvasGroup _legendGroup;
    bool _inMapView;
    float _mapViewSavedDepth;
    bool _mapViewCameraWasActive;
    float _legendAlpha = 1f;
    float _legendTarget = 1f;
    float _legendDelay;

    public override void Init()
    {
        CacheLegendGroup();
    }

    public override void Enter(bool instant = false)
    {
        if (_inMapView)
        {
            ExitMapView();
            OverworldMapView.NotifyClosedExternally();
        }

        if (MainCamera != null) MainCamera.gameObject.SetActive(false);
        var overworldCam = Camera != null ? Camera : MapController?.OverworldCamera;
        if (overworldCam != null) overworldCam.gameObject.SetActive(true);
        base.Enter(instant);
        if (MapController != null)
            MapController.ShowMap();
        BeginLegendFadeIn();
    }

    public override void Exit(bool instant = false)
    {
        if (_inMapView)
        {
            // Something hid every screen while the map view was open: close the view, then exit as usual.
            ExitMapView();
            OverworldMapView.NotifyClosedExternally();
        }

        if (MapController != null)
            MapController.HideMap();
        base.Exit(instant);
        var overworldCam = Camera != null ? Camera : MapController?.OverworldCamera;
        if (overworldCam != null) overworldCam.gameObject.SetActive(false);
        if (MainCamera != null) MainCamera.gameObject.SetActive(true);
    }

    Camera OverworldCamera => Camera != null ? Camera : MapController?.OverworldCamera;

    public bool IsInMapView => _inMapView;

    /// <summary>
    /// Shows the map over the current screen for viewing only. Unlike <see cref="Enter"/> this
    /// leaves the current screen's cameras alone: the overworld camera renders above them for
    /// as long as the view is open (<see cref="MapViewCameraDepth"/>).
    /// </summary>
    public void EnterMapView(OverworldMapNode focusNode)
    {
        if (_inMapView) return;
        _inMapView = true;

        Camera overworldCam = OverworldCamera;
        if (overworldCam != null)
        {
            _mapViewCameraWasActive = overworldCam.gameObject.activeSelf;
            _mapViewSavedDepth = overworldCam.depth;
            overworldCam.depth = MapViewCameraDepth;
            overworldCam.gameObject.SetActive(true);
        }

        base.Enter(false);
        if (MapController != null)
            MapController.ShowMapViewOnly(focusNode);

        CacheLegendGroup();
        _legendDelay = 0f;
        _legendTarget = 1f;
    }

    /// <summary>Closes the map view and gives the screen underneath its cameras back.</summary>
    public void ExitMapView()
    {
        if (!_inMapView) return;
        _inMapView = false;

        if (MapController != null)
            MapController.HideMap();
        base.Exit(true);

        Camera overworldCam = OverworldCamera;
        if (overworldCam != null)
        {
            overworldCam.depth = _mapViewSavedDepth;
            overworldCam.gameObject.SetActive(_mapViewCameraWasActive);
        }

        if (_legendGroup != null)
        {
            _legendAlpha = 0f;
            _legendTarget = 0f;
            _legendGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Fades the node legend out with the fade to black. Called when a node transition starts,
    /// which is earlier than <see cref="Exit"/> (that runs only once the screen is already black).
    /// </summary>
    public void FadeLegendOut()
    {
        CacheLegendGroup();
        _legendDelay = 0f;
        _legendTarget = 0f;
    }

    void BeginLegendFadeIn()
    {
        CacheLegendGroup();
        _legendAlpha = 0f;
        _legendTarget = 1f;
        // Enter runs while the fade-to-black is still held. Wait until it lifts, then fade in with it.
        _legendDelay = ScreenTransitionAnimation.HalfwayPauseDuration;
        if (_legendGroup != null)
            _legendGroup.alpha = 0f;
    }

    protected override void Update()
    {
        base.Update();

        if (_legendGroup == null)
            return;

        if (_legendDelay > 0f)
        {
            _legendDelay -= Time.deltaTime;
            return;
        }

        if (Mathf.Approximately(_legendAlpha, _legendTarget))
            return;

        float duration = Mathf.Max(0.01f, ScreenChangeDuration);
        _legendAlpha = Mathf.MoveTowards(_legendAlpha, _legendTarget, Time.deltaTime / duration);
        _legendGroup.alpha = _legendAlpha;
    }

    void CacheLegendGroup()
    {
        if (_legendGroup != null)
            return;

        foreach (CanvasGroup group in GetComponentsInChildren<CanvasGroup>(true))
        {
            if (group != UIHolder && group.gameObject.name == LegendObjectName)
            {
                _legendGroup = group;
                _legendAlpha = group.alpha;
                return;
            }
        }
    }
}
