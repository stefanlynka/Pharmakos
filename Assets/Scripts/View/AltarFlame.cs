using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The ready glow on a ritual altar: a skirt of flame rising from the altar's footprint, leaning outward so the
/// tongues lick up past every side. Built for the altar kit mesh (AltarKit.fbx): the footprint and heights are in
/// that mesh's local space (x across, y up, z depth with the ramp at -z). Drawn with Graphics.DrawMesh, so it adds
/// no scene objects. Look (colours, tongue shape, speed) lives on the material (AltarFlame.mat); size and timing here.
/// </summary>
[ExecuteAlways]
public class AltarFlame : MonoBehaviour
{
    [Tooltip("Additive flame material using the Pharmakos/AltarFlame shader.")]
    public Material Material;

    [Tooltip("Outline of the altar's base in local x/z, in order round the edge.")]
    public Vector2[] Footprint =
    {
        new Vector2(-0.79f, 0.876f), new Vector2(0.79f, 0.876f), new Vector2(0.79f, -0.25f), new Vector2(0.53f, -0.25f),
        new Vector2(0.53f, -0.95f), new Vector2(-0.53f, -0.95f), new Vector2(-0.53f, -0.25f), new Vector2(-0.79f, -0.25f),
    };

    [Tooltip("Local height of the altar's base, where the flames start.")]
    public float BaseHeight = -0.186f;
    [Tooltip("How tall the flames reach, in local units (the altar is about 0.36 tall).")]
    public float Height = 0.42f;
    [Tooltip("How far the flame tips lean out from the altar's sides.")]
    public float Lean = 0.22f;
    [Tooltip("How far inside the footprint the flames start, so they grow from under the altar's edge.")]
    public float Inset = 0.02f;
    [Tooltip("Spacing of the ring's vertices along the footprint.")]
    public float SegmentLength = 0.05f;

    [Header("Timing")]
    [Tooltip("Flame strength while lit.")]
    public float Intensity = 1f;
    [Tooltip("How fast the flames fade in and out, per second.")]
    public float FadeSpeed = 3f;
    [Tooltip("Shows the flames in the editor while not playing, to tune them.")]
    public bool PreviewInEditor;

    static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    static readonly int SeedId = Shader.PropertyToID("_Seed");

    Mesh mesh;
    MaterialPropertyBlock block;
    bool active;
    float level;
    int builtHash;

    public bool IsActive => active;

    /// <summary>Lights or puts out the flames (they fade).</summary>
    public void SetActive(bool isActive)
    {
        active = isActive;
    }

    void OnDisable()
    {
        level = 0f;
        if (mesh != null)
        {
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
            mesh = null;
        }
    }

    void LateUpdate()
    {
        bool lit = active || (!Application.isPlaying && PreviewInEditor);
        float step = Application.isPlaying ? Time.deltaTime * FadeSpeed : 1f;
        level = Mathf.MoveTowards(level, lit ? 1f : 0f, step);
        if (level <= 0.001f || Material == null || Footprint == null || Footprint.Length < 3)
            return;

        int hash = ShapeHash();
        if (mesh == null || hash != builtHash)
        {
            Rebuild();
            builtHash = hash;
        }

        block ??= new MaterialPropertyBlock();
        block.SetFloat(IntensityId, Intensity * Mathf.SmoothStep(0f, 1f, level));
        block.SetFloat(SeedId, (GetInstanceID() & 0xff) * 0.37f);
        Graphics.DrawMesh(mesh, transform.localToWorldMatrix, Material, gameObject.layer, null, 0, block, false, false);
    }

    int ShapeHash()
    {
        int h = Footprint.Length;
        foreach (Vector2 p in Footprint)
            h = h * 31 + p.GetHashCode();
        h = h * 31 + BaseHeight.GetHashCode();
        h = h * 31 + Height.GetHashCode();
        h = h * 31 + Lean.GetHashCode();
        h = h * 31 + Inset.GetHashCode();
        return h * 31 + SegmentLength.GetHashCode();
    }

    void Rebuild()
    {
        if (mesh == null)
            mesh = new Mesh { name = "AltarFlame", hideFlags = HideFlags.DontSave };
        BuildRing(mesh, Footprint, SegmentLength, -Inset, BaseHeight, Lean, BaseHeight + Height);
    }

    /// <summary>
    /// Builds a ring of quads round a footprint outline (local x/z): the inner edge sits at <paramref name="innerOffset"/>
    /// outward from the outline at height <paramref name="innerHeight"/>, the outer edge at <paramref name="outerOffset"/>
    /// and <paramref name="outerHeight"/>. Corners are mitred so neighbouring sides meet without gaps.
    /// uv.x runs along the outline in local units, uv.y is 0 on the inner edge and 1 on the outer edge.
    /// </summary>
    public static void BuildRing(Mesh mesh, Vector2[] footprint, float segmentLength, float innerOffset, float innerHeight,
        float outerOffset, float outerHeight)
    {
        int n = footprint.Length;
        // Outward normal per edge; flip all of them if the outline runs the other way round.
        float area = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = footprint[i], b = footprint[(i + 1) % n];
            area += a.x * b.y - b.x * a.y;
        }
        float side = area > 0f ? 1f : -1f;
        var edgeNormals = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 d = (footprint[(i + 1) % n] - footprint[i]).normalized;
            edgeNormals[i] = new Vector2(d.y, -d.x) * side;
        }
        // Mitred normal at each corner, so neighbouring sides meet without gaps.
        var cornerNormals = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 a = edgeNormals[(i + n - 1) % n], b = edgeNormals[i];
            Vector2 m = (a + b).normalized;
            cornerNormals[i] = m / Mathf.Max(Vector2.Dot(m, b), 0.5f);
        }

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        float u = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = footprint[i], b = footprint[(i + 1) % n];
            float length = Vector2.Distance(a, b);
            int segments = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(segmentLength, 0.005f)));
            for (int s = 0; s <= segments; s++)
            {
                float t = s / (float)segments;
                Vector2 p = Vector2.Lerp(a, b, t);
                // Corners use the mitre; along the edge, blend toward the edge's own normal.
                float cornerWeight = Mathf.Max(1f - t * 4f, (t - 0.75f) * 4f, 0f);
                Vector2 corner = t < 0.5f ? cornerNormals[i] : cornerNormals[(i + 1) % n];
                Vector2 normal = Vector2.Lerp(edgeNormals[i], corner, cornerWeight);
                Vector2 basePoint = p + normal * innerOffset;
                Vector2 tipPoint = p + normal * outerOffset;
                vertices.Add(new Vector3(basePoint.x, innerHeight, basePoint.y));
                vertices.Add(new Vector3(tipPoint.x, outerHeight, tipPoint.y));
                float uu = u + length * t;
                uvs.Add(new Vector2(uu, 0f));
                uvs.Add(new Vector2(uu, 1f));
                if (s > 0)
                {
                    int v = vertices.Count - 4;
                    triangles.Add(v); triangles.Add(v + 1); triangles.Add(v + 3);
                    triangles.Add(v); triangles.Add(v + 3); triangles.Add(v + 2);
                }
            }
            u += length;
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }
}
