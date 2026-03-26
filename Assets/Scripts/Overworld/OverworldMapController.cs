using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class OverworldMapController : MonoBehaviour
{
    public OverworldHexGrid Grid;
    public Transform PlayerMarker;
    public Camera OverworldCamera;

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

    private Vector2 _cameraPanVelocityLocal;
    private float _cameraScrollTargetY;
    private float _cameraScrollSmoothVelocity;
    private bool _cameraScrollInitialized;
    private OverworldMapNode _currentNode;
    private HashSet<Vector2Int> _clearedNodes = new HashSet<Vector2Int>();
    private OverworldMapNode[] _allNodes;

    public OverworldMapNode CurrentNode => _currentNode;
    public bool IsActive { get; private set; }

    private void Awake()
    {
        _allNodes = Grid != null ? Grid.GetAllNodes() : new OverworldMapNode[0];
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

        UpdatePlayerMarker();
    }

    public void HideMap()
    {
        IsActive = false;
        _cameraPanVelocityLocal = Vector2.zero;
        _cameraScrollInitialized = false;
        _cameraScrollSmoothVelocity = 0f;
        gameObject.SetActive(false);
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

    private void UpdatePlayerMarker()
    {
        if (PlayerMarker == null || _currentNode == null) return;

        PlayerMarker.position = _currentNode.transform.position + Vector3.up * 2f;
        PlayerMarker.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!IsActive) return;

        UpdateOverworldCamera();

        if (_currentNode == null) return;

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
        HideMap();
    }

    public void SetCompleted(OverworldMapNode node)
    {
        if (node != null)
            _clearedNodes.Add(node.Axial);
    }
}
