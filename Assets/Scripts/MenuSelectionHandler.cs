using System;
using UnityEngine;

/// <summary>
/// Raycast-based click handling for non-combat screens (starter bundle, temple sacrifice, rewards, etc.).
/// Activate while a menu screen is showing; combat uses <see cref="SelectionHandler"/> instead.
/// </summary>
public class MenuSelectionHandler : MonoBehaviour
{
    public static MenuSelectionHandler Instance { get; private set; }

    const int TargetLayerIndex = 6;
    readonly LayerMask targetLayer = 1 << TargetLayerIndex;

    Camera _selectionCamera;
    Func<bool> _blockInput;

    public bool IsActive { get; private set; }
    public ViewTarget CurrentHover { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        enabled = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <param name="camera">Camera used for screen-point rays (e.g. altar / event camera). Falls back to Camera.main.</param>
    /// <param name="blockInput">When this returns true, clicks are ignored (e.g. during animations).</param>
    public void Activate(Camera camera = null, Func<bool> blockInput = null)
    {
        IsActive = true;
        _selectionCamera = camera;
        _blockInput = blockInput;
        enabled = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        _selectionCamera = null;
        _blockInput = null;
        CurrentHover = null;
        enabled = false;
    }

    void Update()
    {
        if (!IsActive) return;
        UpdateSelections();
    }

    public void UpdateSelections()
    {
        if (!IsActive) return;
        if (Controller.Instance != null && Controller.Instance.GamePaused) return;

        UpdateTargetUnderMouse();
        HandleMouseInputs();
    }

    void UpdateTargetUnderMouse()
    {
        CurrentHover = null;

        Camera camera = GetSelectionCamera();
        if (camera == null) return;

        Physics.SyncTransforms();

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitData, 1000f, targetLayer)
            && hitData.collider.gameObject.TryGetComponent(out ViewTarget viewTarget))
        {
            CurrentHover = viewTarget;
        }
    }

    void HandleMouseInputs()
    {
        if (_blockInput != null && _blockInput()) return;
        if (!Input.GetMouseButtonDown(0)) return;

        if (CurrentHover != null)
            CurrentHover.OnClick?.Invoke(CurrentHover);
    }

    /// <summary>Camera used for menu screen-point rays; falls back to <see cref="Camera.main"/>.</summary>
    public Camera GetSelectionCamera()
    {
        if (_selectionCamera != null && _selectionCamera.enabled)
            return _selectionCamera;

        return Camera.main;
    }
}
