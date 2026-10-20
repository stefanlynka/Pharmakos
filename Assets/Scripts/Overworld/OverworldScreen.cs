using UnityEngine;

public class OverworldScreen : Screen
{
    public OverworldMapController MapController;
    [Tooltip("Game camera to disable when overworld is shown. Assign Main Camera.")]
    public Camera MainCamera;

    const string LegendObjectName = "OverworldLegend";

    CanvasGroup _legendGroup;
    float _legendAlpha = 1f;
    float _legendTarget = 1f;
    float _legendDelay;

    public override void Init()
    {
        CacheLegendGroup();
    }

    public override void Enter(bool instant = false)
    {
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
        if (MapController != null)
            MapController.HideMap();
        base.Exit(instant);
        var overworldCam = Camera != null ? Camera : MapController?.OverworldCamera;
        if (overworldCam != null) overworldCam.gameObject.SetActive(false);
        if (MainCamera != null) MainCamera.gameObject.SetActive(true);
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
