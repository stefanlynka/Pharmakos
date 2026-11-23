using UnityEngine;

/// <summary>
/// A soft shadow on the ground round a ritual altar, so it sits on the board (or the temple rug) instead of
/// floating over it. Drawn with Graphics.DrawMesh as a flat ring hugging the altar's footprint; the footprint
/// comes from the AltarFlame on the same object, so the two always match. Darkness and softness live on the
/// material (AltarContactShadow.mat); size here.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(AltarFlame))]
public class AltarContactShadow : MonoBehaviour
{
    [Tooltip("Alpha-blended material using the Pharmakos/AltarContactShadow shader.")]
    public Material Material;

    [Tooltip("How far the shadow spreads out from the altar's base, in local units (the altar is about 1.6 wide).")]
    public float Spread = 0.22f;
    [Tooltip("How far under the altar's edge the shadow starts, so there's no gap at the base.")]
    public float Inset = 0.03f;
    [Tooltip("Lift above the altar's base height, to stay clear of the ground it rests on.")]
    public float Lift = 0.002f;

    AltarFlame flame;
    Mesh mesh;
    int builtHash;

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
        if (flame == null)
            flame = GetComponent<AltarFlame>();
        if (Material == null || flame == null || flame.Footprint == null || flame.Footprint.Length < 3)
            return;

        int hash = ShapeHash();
        if (mesh == null || hash != builtHash)
        {
            // Not ??=: after a domain reload the field can hold Unity's fake null, which ?? doesn't catch.
            if (mesh == null)
                mesh = new Mesh { name = "AltarContactShadow", hideFlags = HideFlags.DontSave };
            float height = flame.BaseHeight + Lift;
            AltarFlame.BuildRing(mesh, flame.Footprint, flame.SegmentLength, -Inset, height, Spread, height);
            builtHash = hash;
        }
        Graphics.DrawMesh(mesh, transform.localToWorldMatrix, Material, gameObject.layer, null, 0, null, false, false);
    }

    int ShapeHash()
    {
        int h = flame.Footprint.Length;
        foreach (Vector2 p in flame.Footprint)
            h = h * 31 + p.GetHashCode();
        h = h * 31 + flame.BaseHeight.GetHashCode();
        h = h * 31 + flame.SegmentLength.GetHashCode();
        h = h * 31 + Spread.GetHashCode();
        h = h * 31 + Inset.GetHashCode();
        return h * 31 + Lift.GetHashCode();
    }
}
