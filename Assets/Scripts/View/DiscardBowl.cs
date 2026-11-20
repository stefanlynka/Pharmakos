using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Drop-target glow for the Discard bowl: an ember ring along the rim (same material, colour and pulse as the
/// lane edges) and a brighter label while the cursor is over the bowl. <see cref="ViewDiscard"/> drives it.
/// The ring is drawn with Graphics.DrawMesh in this object's local space, so it adds no child objects.
/// </summary>
[ExecuteAlways]
public class DiscardBowl : MonoBehaviour
{
    [Header("Rim glow (bowl-local units)")]
    [Tooltip("Additive glow material; its texture is a profile across the ring (v = 0 outer edge, v = 1 inner edge). Use LaneEdgeGlow.mat.")]
    public Material HighlightMaterial;

    [Tooltip("Colour and pulse speed. Use the Target preset so the bowl matches the lanes and other valid targets.")]
    public MeshHighlightSettings HighlightPreset;

    public float OuterRadius = 1.15f;
    public float InnerRadius = 0.8f;
    [Tooltip("Height of the glow ring along the bowl's local Z (its opening faces +Z).")]
    public float Height = 0.24f;
    [Range(16, 256)] public int Segments = 128;

    [Tooltip("Glow strength range while pulsing (a card is held), and when the cursor is over the bowl.")]
    public float PulseMin = 0.6f;
    public float PulseMax = 0.8f;
    public float HoverStrength = 1.2f;
    [Tooltip("How fast the glow fades in and out, per second.")]
    public float FadeSpeed = 6f;

    [Header("Label")]
    public TMP_Text Label;
    [Tooltip("Label brightness multiplier at full hover strength (1 = unchanged).")]
    public float LabelHoverBrightness = 1.6f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");

    readonly DropGlow glow = new DropGlow();
    MaterialPropertyBlock ringBlock, labelBlock;
    Mesh mesh;
    float builtOuter = -1f, builtInner = -1f, builtHeight = -1f;
    int builtSegments = -1;
    bool labelTinted;

    /// <summary>active = a card is held (the bowl will take it); hovered = the cursor is over the bowl.</summary>
    public void SetHighlight(bool active, bool hovered)
    {
        glow.Set(active, hovered);
    }

    void OnDisable()
    {
        ClearLabelTint();
        if (mesh != null)
        {
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
            mesh = null;
        }
    }

    void LateUpdate()
    {
        float speed = HighlightPreset != null ? HighlightPreset.pulseSpeed : 0.3f;
        float strength = glow.Update(speed, PulseMin, PulseMax, HoverStrength, FadeSpeed);
        UpdateLabel(strength);
        if (!glow.IsVisible || HighlightMaterial == null)
            return;

        if (mesh == null || OuterRadius != builtOuter || InnerRadius != builtInner || Height != builtHeight || Segments != builtSegments)
            Rebuild();

        Color color = HighlightPreset != null ? HighlightPreset.glowColor : new Color(1f, 0.46f, 0.14f);
        color.a = 1f;
        ringBlock ??= new MaterialPropertyBlock();
        ringBlock.SetColor(BaseColorId, color * strength);
        Graphics.DrawMesh(mesh, transform.localToWorldMatrix, HighlightMaterial, gameObject.layer, null, 0, ringBlock, false, false);
    }

    // Brightens the label in step with the hover part of the glow; the label's own colour stays as authored.
    void UpdateLabel(float strength)
    {
        if (Label == null)
            return;

        float hover = HoverStrength > PulseMax ? Mathf.InverseLerp(PulseMax, HoverStrength, strength) : 0f;
        if (hover <= 0.001f)
        {
            ClearLabelTint();
            return;
        }

        var renderer = Label.GetComponent<Renderer>();
        labelBlock ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(labelBlock);
        float brightness = Mathf.Lerp(1f, LabelHoverBrightness, hover);
        labelBlock.SetColor(FaceColorId, new Color(brightness, brightness, brightness, 1f));
        renderer.SetPropertyBlock(labelBlock);
        labelTinted = true;
    }

    void ClearLabelTint()
    {
        if (!labelTinted || Label == null)
            return;
        Label.GetComponent<Renderer>().SetPropertyBlock(null);
        labelTinted = false;
    }

    void Rebuild()
    {
        if (mesh == null)
            mesh = new Mesh { name = "DiscardBowlGlow", hideFlags = HideFlags.DontSave };

        var vertices = new List<Vector3>((Segments + 1) * 2);
        var uvs = new List<Vector2>((Segments + 1) * 2);
        var triangles = new List<int>(Segments * 6);
        for (int i = 0; i <= Segments; i++)
        {
            float a = i / (float)Segments * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            vertices.Add(dir * OuterRadius + new Vector3(0f, 0f, Height));
            vertices.Add(dir * InnerRadius + new Vector3(0f, 0f, Height));
            uvs.Add(new Vector2(i / (float)Segments, 0f));
            uvs.Add(new Vector2(i / (float)Segments, 1f));
            if (i == Segments)
                continue;
            int o = i * 2;
            // Faces +Z, out of the bowl's opening.
            triangles.Add(o); triangles.Add(o + 2); triangles.Add(o + 1);
            triangles.Add(o + 1); triangles.Add(o + 2); triangles.Add(o + 3);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        if (mesh.normals.Length > 0 && mesh.normals[0].z < 0f)
        {
            triangles.Reverse();
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
        }
        mesh.RecalculateBounds();

        builtOuter = OuterRadius;
        builtInner = InnerRadius;
        builtHeight = Height;
        builtSegments = Segments;
    }
}
