using UnityEngine;

/// <summary>
/// The marble cylinder a trinket's art sits on (ViewBuff prefab → Base). It sizes itself from the art so it fits
/// everywhere ViewBuff is used, whatever scale an instance has (the event screen uses 50x50x1, starter bundles scale
/// the art up): its Transform scale is driven by this component, tune the fields here instead.
/// It also takes the art's layer, so cameras that render only that layer (popup captures) see it too.
/// Runs in play mode only, so it never writes overrides into scenes or prefabs; the prefab's own Base scale is the
/// default fit (art radius 1.745 → scale 1.806, z 0.01) for previewing in the editor.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class TrinketBase : MonoBehaviour
{
    [Tooltip("The trinket art this base sits under.")]
    public SpriteRenderer Icon;

    [Tooltip("Cylinder height as a fraction of its diameter.")]
    [Range(0.02f, 1f)]
    public float Height = 0.15f;

    [Tooltip("Base radius relative to the art's radius. Above 1 the rounded rim shows round the art's edge.")]
    [Range(0.8f, 1.2f)]
    public float RadiusFit = 1.035f;

    [Tooltip("Gap between the art and the cylinder's top face, in the art's units.")]
    public float Gap = 0.01f;

    // The mesh (TrinketBase.fbx) has radius 1 and its height along local +Y; this object is rotated -90 on X so the
    // top faces the art (-Z) and the body runs away from it (+Z).
    const float MeshHeight = 0.3f;

    Vector3 lastParentScale, lastIconScale;
    Sprite lastSprite;
    float lastHeight, lastFit, lastGap;

    void OnEnable()
    {
        Fit(true);
    }

    void LateUpdate()
    {
        Fit(false);
    }

    void Fit(bool force)
    {
        Transform parent = transform.parent;
        if (Icon == null || Icon.sprite == null || parent == null)
            return;

        if (gameObject.layer != Icon.gameObject.layer)
            gameObject.layer = Icon.gameObject.layer;

        Vector3 parentScale = parent.lossyScale;
        Vector3 iconScale = Icon.transform.lossyScale;
        if (!force && parentScale == lastParentScale && iconScale == lastIconScale && Icon.sprite == lastSprite
            && Height == lastHeight && RadiusFit == lastFit && Gap == lastGap)
            return;
        lastParentScale = parentScale;
        lastIconScale = iconScale;
        lastSprite = Icon.sprite;
        lastHeight = Height;
        lastFit = RadiusFit;
        lastGap = Gap;

        if (parentScale.x == 0f || parentScale.y == 0f || parentScale.z == 0f)
            return;

        // world-space radius of the art along x and y, and the height that keeps the cylinder's proportions
        Vector3 extents = Icon.sprite.bounds.extents;
        float radiusX = extents.x * Mathf.Abs(iconScale.x) * RadiusFit;
        float radiusY = extents.y * Mathf.Abs(iconScale.y) * RadiusFit;
        float height = (radiusX + radiusY) * Height;

        // local x stays x, mesh height (local y) runs along the parent's z, mesh z along the parent's y
        transform.localScale = new Vector3(radiusX / parentScale.x, height / MeshHeight / parentScale.z, radiusY / parentScale.y);
        Vector3 position = transform.localPosition;
        position.z = Icon.transform.localPosition.z + Gap * Mathf.Abs(iconScale.z) / parentScale.z;
        transform.localPosition = position;
    }
}
