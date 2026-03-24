using System.Collections.Generic;
using UnityEngine;

public class OverworldMapController : MonoBehaviour
{
    public OverworldHexGrid Grid;
    public Transform PlayerMarker;
    public Camera OverworldCamera;

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
        if (_currentNode == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            var cam = OverworldCamera != null ? OverworldCamera : Camera.main;
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

    private bool CanMoveTo(OverworldMapNode target)
    {
        if (target.Cleared) return false;
        if (target.IsStart && _clearedNodes.Count > 0) return false;
        return OverworldHexGrid.AreAdjacent(_currentNode.Q, _currentNode.R, target.Q, target.R);
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
