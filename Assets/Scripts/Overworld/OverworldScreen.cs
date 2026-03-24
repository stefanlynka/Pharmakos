using UnityEngine;

public class OverworldScreen : Screen
{
    public OverworldMapController MapController;
    [Tooltip("Game camera to disable when overworld is shown. Assign Main Camera.")]
    public Camera MainCamera;

    public override void Enter(bool instant = false)
    {
        if (MainCamera != null) MainCamera.gameObject.SetActive(false);
        var overworldCam = Camera != null ? Camera : MapController?.OverworldCamera;
        if (overworldCam != null) overworldCam.gameObject.SetActive(true);
        base.Enter(instant);
        if (MapController != null)
            MapController.ShowMap();
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
}
