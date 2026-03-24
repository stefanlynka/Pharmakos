using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pointy-top hex grid. Place this GameObject at (0, 100, 0) for overworld height.
/// </summary>
public class OverworldHexGrid : MonoBehaviour
{
    [Tooltip("Distance from hex center to vertex (pointy-top). Tune to match Pasture prefab scale.")]
    public float HexSize = 1f;

    [Tooltip("Y offset for hex positions in local space (added to grid base at Y=100).")]
    public float LocalY;

    private static readonly float Sqrt3 = Mathf.Sqrt(3f);

    /// <summary>
    /// Pointy-top axial to local position. X/Z in grid plane; Y uses LocalY.
    /// </summary>
    public Vector3 AxialToLocal(int q, int r)
    {
        float x = HexSize * Sqrt3 * (q + r / 2f);
        float z = HexSize * 1.5f * r;
        return new Vector3(x, LocalY, z);
    }

    public Vector3 AxialToWorld(int q, int r)
    {
        return transform.TransformPoint(AxialToLocal(q, r));
    }

    /// <summary>
    /// World position to nearest axial coordinates.
    /// </summary>
    public Vector2Int WorldToAxial(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return LocalToAxial(local);
    }

    public Vector2Int LocalToAxial(Vector3 local)
    {
        float q = (local.x / (HexSize * Sqrt3)) - (local.z / (HexSize * 3f));
        float r = local.z / (HexSize * 1.5f);
        return RoundAxial(q, r);
    }

    /// <summary>
    /// Cube rounding for axial (q, r) with s = -q - r.
    /// </summary>
    private static Vector2Int RoundAxial(float q, float r)
    {
        float s = -q - r;
        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float qDiff = Mathf.Abs(rq - q);
        float rDiff = Mathf.Abs(rr - r);
        float sDiff = Mathf.Abs(rs - s);

        if (qDiff > rDiff && qDiff > sDiff)
            rq = -rr - rs;
        else if (rDiff > sDiff)
            rr = -rq - rs;
        else
            rs = -rq - rr;

        return new Vector2Int(rq, rr);
    }

    public static int AxialDistance(int q1, int r1, int q2, int r2)
    {
        int s1 = -q1 - r1;
        int s2 = -q2 - r2;
        return (Mathf.Abs(q1 - q2) + Mathf.Abs(r1 - r2) + Mathf.Abs(s1 - s2)) / 2;
    }

    /// <summary>
    /// Pointy-top axial neighbors in order: E, SE, SW, W, NW, NE.
    /// </summary>
    public static readonly (int dq, int dr)[] PointyTopNeighbors = new[]
    {
        (1, 0), (1, 1), (0, 1), (-1, 0), (-1, -1), (0, -1)
    };

    public static List<Vector2Int> GetNeighbors(int q, int r)
    {
        var list = new List<Vector2Int>(6);
        foreach (var (dq, dr) in PointyTopNeighbors)
            list.Add(new Vector2Int(q + dq, r + dr));
        return list;
    }

    public static bool AreAdjacent(int q1, int r1, int q2, int r2)
    {
        return AxialDistance(q1, r1, q2, r2) == 1;
    }

    public OverworldMapNode[] GetAllNodes()
    {
        return GetComponentsInChildren<OverworldMapNode>();
    }
}
