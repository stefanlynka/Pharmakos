using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws a carved edge around a battle lane: a soft drop shadow on the ground outside it and a bevel just inside
/// its rim. The edge follows the lane mesh's bounds, so it fits every board; its look comes entirely from
/// <see cref="Material"/> (a profile texture across the band: v = 0 outer edge, v = 1 inner edge).
/// Drawn with Graphics.DrawMesh, so it creates no scene objects and shows in the editor too.
/// </summary>
[ExecuteAlways]
public class LaneEdge : MonoBehaviour
{
    [Tooltip("Lane whose bounds the edge follows. Defaults to the MeshFilter on this object.")]
    public MeshFilter Lane;

    [Tooltip("Transparent material whose texture is the edge profile (see LaneEdgeSlab.png).")]
    public Material Material;

    [Tooltip("How far the edge reaches out over the ground (drop shadow), in lane-local units.")]
    public float OuterWidth = 0.9f;

    [Tooltip("How far the edge reaches in over the lane (bevel and inner shade), in lane-local units.")]
    public float InnerWidth = 0.7f;

    [Tooltip("Height above the lane's top face, to avoid z-fighting.")]
    public float HeightOffset = 0.012f;

    [Header("Drop highlight")]
    [Tooltip("Additive glow drawn along the bevel while this lane is a valid drop target (see LaneEdgeGlow.mat).")]
    public Material HighlightMaterial;

    [Tooltip("Colour and pulse speed for the glow. Use the Target preset so the lane matches other valid targets.")]
    public MeshHighlightSettings HighlightPreset;

    [Tooltip("Glow strength range while pulsing (a playable follower is held), and when the cursor is over the lane.")]
    public float PulseMin = 0.6f;
    public float PulseMax = 0.8f;
    public float HoverStrength = 1.2f;

    [Tooltip("How fast the glow fades in and out, per second.")]
    public float FadeSpeed = 6f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    Mesh mesh;
    MaterialPropertyBlock highlightBlock;
    readonly DropGlow glow = new DropGlow();
    Bounds builtFor;
    float builtOuter = -1f, builtInner = -1f, builtHeight = -1f;

    void OnEnable()
    {
        if (Lane == null)
            Lane = GetComponent<MeshFilter>();
    }

    /// <summary>Lights the lane's bevel: active = it's a valid drop target; hovered = the cursor is over it.</summary>
    public void SetHighlight(bool active, bool hovered)
    {
        glow.Set(active, hovered);
    }

    void OnDisable()
    {
        if (mesh != null)
        {
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
            mesh = null;
        }
    }

    void LateUpdate()
    {
        if (Lane == null || Lane.sharedMesh == null || Material == null)
            return;

        Bounds bounds = Lane.sharedMesh.bounds;
        if (mesh == null || bounds != builtFor || OuterWidth != builtOuter || InnerWidth != builtInner || HeightOffset != builtHeight)
            Rebuild(bounds);

        Matrix4x4 matrix = Lane.transform.localToWorldMatrix;
        Graphics.DrawMesh(mesh, matrix, Material, gameObject.layer, null, 0, null, false, false);
        DrawHighlight(matrix);
    }

    void DrawHighlight(Matrix4x4 matrix)
    {
        float speed = HighlightPreset != null ? HighlightPreset.pulseSpeed : 0.3f;
        float strength = glow.Update(speed, PulseMin, PulseMax, HoverStrength, FadeSpeed);
        if (!glow.IsVisible || HighlightMaterial == null)
            return;

        Color color = HighlightPreset != null ? HighlightPreset.glowColor : new Color(1f, 0.46f, 0.14f);
        color.a = 1f;
        highlightBlock ??= new MaterialPropertyBlock();
        highlightBlock.SetColor(BaseColorId, color * strength);
        Graphics.DrawMesh(mesh, matrix, HighlightMaterial, gameObject.layer, null, 0, highlightBlock, false, false);
    }

    void Rebuild(Bounds b)
    {
        if (mesh == null)
            mesh = new Mesh { name = "LaneEdge", hideFlags = HideFlags.DontSave };

        float y = b.max.y + HeightOffset;
        Vector3[] outer =
        {
            new Vector3(b.min.x - OuterWidth, y, b.min.z - OuterWidth),
            new Vector3(b.max.x + OuterWidth, y, b.min.z - OuterWidth),
            new Vector3(b.max.x + OuterWidth, y, b.max.z + OuterWidth),
            new Vector3(b.min.x - OuterWidth, y, b.max.z + OuterWidth),
        };
        Vector3[] inner =
        {
            new Vector3(b.min.x + InnerWidth, y, b.min.z + InnerWidth),
            new Vector3(b.max.x - InnerWidth, y, b.min.z + InnerWidth),
            new Vector3(b.max.x - InnerWidth, y, b.max.z - InnerWidth),
            new Vector3(b.min.x + InnerWidth, y, b.max.z - InnerWidth),
        };

        // Four mitred strips. u runs along the edge (the profile texture is uniform along it), v across the band.
        var vertices = new List<Vector3>(16);
        var uvs = new List<Vector2>(16);
        var triangles = new List<int>(24);
        for (int side = 0; side < 4; side++)
        {
            int next = (side + 1) % 4;
            int start = vertices.Count;
            vertices.Add(outer[side]);
            vertices.Add(outer[next]);
            vertices.Add(inner[next]);
            vertices.Add(inner[side]);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));
            // Wound so the faces point up (+y) towards the camera.
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
            triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        if (mesh.normals.Length > 0 && mesh.normals[0].y < 0f)
        {
            triangles.Reverse();
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
        }
        mesh.RecalculateBounds();

        builtFor = b;
        builtOuter = OuterWidth;
        builtInner = InnerWidth;
        builtHeight = HeightOffset;
    }
}
