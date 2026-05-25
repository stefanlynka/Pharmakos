using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class OverworldMapController : MonoBehaviour
{
    public OverworldHexGrid Grid;
    public Transform PlayerMarker;
    public Camera OverworldCamera;
    // public DynamicSmokeSystem.DynamicSmokeSystemController DynamicSmokeSystem;
    public GameObject LightingParent;
    public OverworldLightController OverworldLightController;
    public Vector3 SmokePosition;
    
    [Header("Overworld camera")]
    [Tooltip("Parent-local X/Z: max units per second when WASD is held.")]
    [FormerlySerializedAs("CameraPanSpeed")]
    public float CameraPanMaxSpeed = 20f;

    [Tooltip("How quickly pan speed ramps up toward max (local X/Z per second²).")]
    public float CameraPanAcceleration = 45f;

    [Tooltip("How quickly pan speed ramps down when WASD is released.")]
    public float CameraPanDeceleration = 55f;

    [Tooltip("Multiplier for mouse wheel movement on local Y (per scroll unit).")]
    public float CameraScrollZoomSpeed = 30f;

    [Tooltip("Approximate time to ease toward the scroll target height (seconds). Smaller = snappier.")]
    public float CameraScrollSmoothTime = 0.12f;

    [Tooltip("Minimum local X for the camera (relative to its parent).")]
    public float CameraMinX = -80f;

    [Tooltip("Maximum local X for the camera (relative to its parent).")]
    public float CameraMaxX = 80f;

    [Tooltip("Minimum local Y for the camera (relative to its parent).")]
    [FormerlySerializedAs("CameraMinHeight")]
    public float CameraMinY = 5f;

    [Tooltip("Maximum local Y for the camera (relative to its parent).")]
    [FormerlySerializedAs("CameraMaxHeight")]
    public float CameraMaxY = 80f;

    [Tooltip("Minimum local Z for the camera (relative to its parent).")]
    public float CameraMinZ = -80f;

    [Tooltip("Maximum local Z for the camera (relative to its parent).")]
    public float CameraMaxZ = 80f;

    [Tooltip("Height of the player icon above the node.")]
    public float PlayerIconHeight = 0.5f;

    [Tooltip("Uniform scale applied to an accessible hex while the mouse hovers it.")]
    public float AccessibleHexHoverScale = 1.1f;

    public PlayerLyre PlayerLyre;

    [Header("Overworld lights")]
    [Tooltip("World-space offset along the row axis from the leftmost/rightmost hex center for side lights (grid local ±X).")]
    public float SideLightRowInset = 2.5f;

    private Vector2 _cameraPanVelocityLocal;
    private float _cameraScrollTargetY;
    private float _cameraScrollSmoothVelocity;
    private bool _cameraScrollInitialized;
    private OverworldMapNode _currentNode;
    private HashSet<Vector2Int> _clearedNodes = new HashSet<Vector2Int>();
    private OverworldMapNode[] _allNodes;
    private OverworldMapNode _hoveredAccessibleNode;

    public OverworldMapNode CurrentNode => _currentNode;
    public bool IsActive { get; private set; }

    private void Awake()
    {
        _allNodes = Grid != null ? Grid.GetAllNodes() : new OverworldMapNode[0];
    }

    /// <summary>
    /// New run: floor-based encounters (hex visuals follow encounter), reset cleared/progress from <see cref="OverworldMapNode.IsStart"/>.
    /// </summary>
    public void SetupOverworldForNewRun()
    {
        _allNodes = Grid != null ? Grid.GetAllNodes() : new OverworldMapNode[0];
        _currentNode = FindStartNode();
        _clearedNodes.Clear();

        var nodesToAssign = new List<OverworldMapNode>(_allNodes.Length);
        foreach (var node in _allNodes)
        {
            if (node == null || node == _currentNode) continue;
            nodesToAssign.Add(node);
        }
        OverworldMapNode.AssignEncountersByFloorRules(nodesToAssign.ToArray());

        if (_currentNode != null)
            _currentNode.ApplyEncounter(EncounterType.None);
        else
            Debug.LogError("Overworld: No node with IsStart=true found.");

        foreach (var node in _allNodes)
        {
            if (node == null) continue;
            node.Cleared = false;
        }

        RefreshHexInteractionLayers();
        ResetHexVisualScales();
        RefreshPlayerLyre();

        // // Setup smoke
        // if (DynamicSmokeSystem.isActiveAndEnabled)
        // {
        //     DynamicSmokeSystem.Disperse();
        //     DynamicSmokeSystem.Emit(SmokePosition);
        // }

    }

    public void ShowMap()
    {
        gameObject.SetActive(true);
        IsActive = true;

        if (_currentNode == null)
        {
            var startNode = FindStartNode();
            if (startNode != null)
            {
                _currentNode = startNode;
                _clearedNodes.Clear();
            }
            else
            {
                Debug.LogError("Overworld: No node with IsStart=true found.");
            }
        }

        if (_currentNode != null)
            _currentNode.ApplyEncounter(EncounterType.None);

        UpdatePlayerMarker();
        RefreshHexInteractionLayers();
        ResetHexVisualScales();
        PositionSideLightsForCurrentRow();
        SetLights(true);
        RefreshPlayerLyre();
    }

    public void HideMap()
    {
        IsActive = false;
        _cameraPanVelocityLocal = Vector2.zero;
        _cameraScrollInitialized = false;
        _cameraScrollSmoothVelocity = 0f;
        ResetHexVisualScales();
        gameObject.SetActive(false);
        SetLights(false);
    }

    public void ReturnToMap(OverworldMapNode completedNode)
    {
        if (completedNode != null)
        {
            _currentNode = completedNode;
            _clearedNodes.Add(completedNode.Axial);
            completedNode.Cleared = true;
        }

        ShowMap();
    }

    private OverworldMapNode FindStartNode()
    {
        foreach (var node in _allNodes)
        {
            if (node != null && node.IsStart)
                return node;
        }
        return null;
    }

    /// <summary>
    /// URP rendering layer bits — order matches <c>UniversalRenderPipelineGlobalSettings</c> (Default, GreenHighlight, YellowHighlight).
    /// </summary>
    const uint RenderingLayerDefault = 1u << 0;
    const uint RenderingLayerGreenHighlight = 1u << 1;
    const uint RenderingLayerYellowHighlight = 1u << 2;

    /// <summary>
    /// Default layer + Default rendering, AccessibleHexes + GreenHighlight, CurrentHex + YellowHighlight.
    /// </summary>
    void RefreshHexInteractionLayers()
    {
        int layerDefault = LayerMask.NameToLayer("Default");
        int layerAccessible = LayerMask.NameToLayer("AccessibleHexes");
        int layerCurrent = LayerMask.NameToLayer("CurrentHex");
        if (layerDefault < 0) layerDefault = 0;
        if (layerAccessible < 0) layerAccessible = 0;
        if (layerCurrent < 0) layerCurrent = 0;

        foreach (var node in _allNodes)
        {
            if (node == null) continue;

            if (_currentNode != null && node == _currentNode)
                node.ApplyHexInteractionLayers(layerCurrent, RenderingLayerYellowHighlight);
            else if (_currentNode != null && IsAccessibleNeighbor(node))
                node.ApplyHexInteractionLayers(layerAccessible, RenderingLayerGreenHighlight);
            else
                node.ApplyHexInteractionLayers(layerDefault, RenderingLayerDefault);
        }
    }

    bool IsAccessibleNeighbor(OverworldMapNode target)
    {
        if (_currentNode == null) return false;
        if (target.Cleared) return false;
        if (target.IsStart && _clearedNodes.Count > 0) return false;
        return OverworldHexGrid.IsAheadAdjacent(_currentNode.Q, _currentNode.R, target.Q, target.R);
    }

    /// <summary>
    /// Sets every hex node's local scale to 1 and clears hover state.
    /// </summary>
    void ResetHexVisualScales()
    {
        foreach (var node in _allNodes)
        {
            if (node != null)
                node.transform.localScale = Vector3.one;
        }

        _hoveredAccessibleNode = null;
    }

    void UpdateAccessibleHexHover()
    {
        var cam = GetOverworldCamera();
        OverworldMapNode newHover = null;
        if (cam != null)
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                var node = hit.collider.GetComponentInParent<OverworldMapNode>();
                if (node != null && CanMoveTo(node))
                    newHover = node;
            }
        }

        if (newHover == _hoveredAccessibleNode)
            return;

        if (_hoveredAccessibleNode != null)
            _hoveredAccessibleNode.transform.localScale = Vector3.one;

        _hoveredAccessibleNode = newHover;

        if (_hoveredAccessibleNode != null)
        {
            float s = Mathf.Max(0.01f, AccessibleHexHoverScale);
            _hoveredAccessibleNode.transform.localScale = new Vector3(s, s, s);
        }
    }

    private void UpdatePlayerMarker()
    {
        if (PlayerMarker == null || _currentNode == null) return;

        PlayerMarker.position = _currentNode.transform.position + Vector3.up * PlayerIconHeight;
        PlayerMarker.gameObject.SetActive(true);
    }

    void RefreshPlayerLyre()
    {
        if (PlayerLyre == null || Controller.Instance == null)
            return;

        PlayerLyre.SetHeartstrings(Controller.Instance.RunHeartStrings, 0);
    }

    private void Update()
    {
        if (!IsActive) return;

        UpdateOverworldCamera();

        if (_currentNode == null) return;

        UpdateAccessibleHexHover();

        if (Input.GetMouseButtonDown(0))
        {
            var cam = GetOverworldCamera();
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                var node = hit.collider.GetComponentInParent<OverworldMapNode>();
                if (node != null && CanMoveTo(node))
                {
                    TryEnterNode(node);
                }
            }
        }
    }

    private Camera GetOverworldCamera()
    {
        return OverworldCamera != null ? OverworldCamera : Camera.main;
    }

    private void UpdateOverworldCamera()
    {
        var cam = GetOverworldCamera();
        if (cam == null) return;

        var t = cam.transform;
        var local = t.localPosition;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        var input = new Vector2(h, v);
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        bool hasInput = input.sqrMagnitude > 1e-6f;
        var targetVel = hasInput ? input * CameraPanMaxSpeed : Vector2.zero;
        float rate = hasInput ? CameraPanAcceleration : CameraPanDeceleration;
        _cameraPanVelocityLocal = Vector2.MoveTowards(_cameraPanVelocityLocal, targetVel, rate * Time.deltaTime);

        local.x += _cameraPanVelocityLocal.x * Time.deltaTime;
        local.z += _cameraPanVelocityLocal.y * Time.deltaTime;

        if (!_cameraScrollInitialized)
        {
            _cameraScrollTargetY = local.y;
            _cameraScrollSmoothVelocity = 0f;
            _cameraScrollInitialized = true;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            _cameraScrollTargetY -= scroll * CameraScrollZoomSpeed;
            _cameraScrollTargetY = Mathf.Clamp(_cameraScrollTargetY, CameraMinY, CameraMaxY);
        }

        float smooth = Mathf.Max(0.0001f, CameraScrollSmoothTime);
        local.y = Mathf.SmoothDamp(local.y, _cameraScrollTargetY, ref _cameraScrollSmoothVelocity, smooth, Mathf.Infinity, Time.deltaTime);
        local.y = Mathf.Clamp(local.y, CameraMinY, CameraMaxY);

        local.x = Mathf.Clamp(local.x, CameraMinX, CameraMaxX);
        local.z = Mathf.Clamp(local.z, CameraMinZ, CameraMaxZ);

        if (local.x <= CameraMinX && _cameraPanVelocityLocal.x < 0f)
            _cameraPanVelocityLocal.x = 0f;
        if (local.x >= CameraMaxX && _cameraPanVelocityLocal.x > 0f)
            _cameraPanVelocityLocal.x = 0f;
        if (local.z <= CameraMinZ && _cameraPanVelocityLocal.y < 0f)
            _cameraPanVelocityLocal.y = 0f;
        if (local.z >= CameraMaxZ && _cameraPanVelocityLocal.y > 0f)
            _cameraPanVelocityLocal.y = 0f;

        t.localPosition = local;
    }

    private bool CanMoveTo(OverworldMapNode target)
    {
        if (target.Cleared) return false;
        if (target.IsStart && _clearedNodes.Count > 0) return false;
        return OverworldHexGrid.IsAheadAdjacent(_currentNode.Q, _currentNode.R, target.Q, target.R);
    }

    private void TryEnterNode(OverworldMapNode node)
    {
        if (Controller.Instance == null) return;
        if (Controller.Instance.IsTestChamber) return;

        if (node.IsStart && _clearedNodes.Count == 0)
        {
            Debug.Log("Already at start.");
            return;
        }

        Controller.Instance.BeginEncounterFromOverworldNode(node);
    }

    public void SetCompleted(OverworldMapNode node)
    {
        if (node != null)
            _clearedNodes.Add(node.Axial);
    }

    public void SetLights(bool active)
    {
        if (LightingParent != null)
            LightingParent.SetActive(active);

        if (OverworldLightController != null)
            OverworldLightController.SetAllLightsEnabled(active);
    }

    /// <summary>
    /// Places <see cref="OverworldLightController.LeftLightGameObject"/> / <see cref="OverworldLightController.RightLightGameObject"/>
    /// at <see cref="SideLightRowInset"/> along grid local ±X from the leftmost/rightmost hex on the player's
    /// current row (same axial <c>R</c> as <see cref="_currentNode"/>).
    /// </summary>
    private void PositionSideLightsForCurrentRow()
    {
        if (OverworldLightController == null || Grid == null || _currentNode == null)
            return;

        var leftGo = OverworldLightController.LeftLightGameObject;
        var rightGo = OverworldLightController.RightLightGameObject;
        if (leftGo == null || rightGo == null)
            return;

        int rowR = _currentNode.R;
        OverworldMapNode leftMost = null;
        OverworldMapNode rightMost = null;
        float minLocalX = float.MaxValue;
        float maxLocalX = float.MinValue;

        var gridTx = Grid.transform;
        foreach (var node in _allNodes)
        {
            if (node == null || node.R != rowR)
                continue;

            float lx = gridTx.InverseTransformPoint(node.transform.position).x;
            if (lx < minLocalX)
            {
                minLocalX = lx;
                leftMost = node;
            }

            if (lx > maxLocalX)
            {
                maxLocalX = lx;
                rightMost = node;
            }
        }

        if (leftMost == null || rightMost == null)
            return;

        float inset = Mathf.Max(0f, SideLightRowInset);

        Vector3 leftLocal = gridTx.InverseTransformPoint(leftMost.transform.position);
        leftLocal.x -= inset;
        leftGo.transform.position = gridTx.TransformPoint(leftLocal);
        leftGo.transform.SetLocalPositionAndRotation(new Vector3(leftGo.transform.localPosition.x, 0, leftGo.transform.localPosition.z), Quaternion.identity);

        Vector3 rightLocal = gridTx.InverseTransformPoint(rightMost.transform.position);
        rightLocal.x += inset;
        rightGo.transform.position = gridTx.TransformPoint(rightLocal);
        rightGo.transform.SetLocalPositionAndRotation(new Vector3(rightGo.transform.localPosition.x, 0, rightGo.transform.localPosition.z), Quaternion.identity);
    }
}
