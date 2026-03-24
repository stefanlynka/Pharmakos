using System.Collections.Generic;
using UnityEngine;

public class OverworldMapNode : MonoBehaviour
{
    [Tooltip("Axial hex coordinates. Use context menu to sync from position.")]
    public int Q;
    [Tooltip("Axial hex coordinates. Use context menu to sync from position.")]
    public int R;

    [Tooltip("Player starts on this node. Exactly one per map.")]
    public bool IsStart;

    [HideInInspector]
    public bool Cleared;

    public Vector2Int Axial => new Vector2Int(Q, R);

    private OverworldHexGrid _grid;

    public OverworldHexGrid Grid
    {
        get
        {
            if (_grid == null)
                _grid = GetComponentInParent<OverworldHexGrid>();
            return _grid;
        }
    }

    public void SnapPositionFromAxial()
    {
        var grid = Grid;
        if (grid == null) return;

        transform.localPosition = grid.AxialToLocal(Q, R);
    }

    public void CaptureAxialFromPosition()
    {
        var grid = Grid;
        if (grid == null) return;

        var axial = grid.LocalToAxial(transform.localPosition);
        Q = axial.x;
        R = axial.y;
    }

    public void SnapPositionFromAxialAndCapture()
    {
        CaptureAxialFromPosition();
        SnapPositionFromAxial();
    }

    public List<OverworldMapNode> GetNeighborNodes()
    {
        if (Grid == null) return new List<OverworldMapNode>();
        var neighbors = new List<OverworldMapNode>();
        var coords = OverworldHexGrid.GetNeighbors(Q, R);
        foreach (var node in Grid.GetComponentsInChildren<OverworldMapNode>())
        {
            if (node == this) continue;
            if (coords.Exists(c => c.x == node.Q && c.y == node.R))
                neighbors.Add(node);
        }
        return neighbors;
    }

    private void Start()
    {
        EnsureCollider();
    }

    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        var grid = Grid;
        var hexExtent = grid != null ? grid.HexSize * 2f : 2f;

        var box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(hexExtent * 1.8f, 1f, hexExtent * 1.8f);
        box.center = new Vector3(0, 0.5f, 0);
    }

    private void OnValidate()
    {
        if (Grid != null)
            SnapPositionFromAxial();
    }
}
