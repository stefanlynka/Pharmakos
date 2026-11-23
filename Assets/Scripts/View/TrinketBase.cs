using UnityEngine;

/// <summary>
/// The marble cylinder a trinket is shown on (ViewBuff prefab → Base). In play mode the trinket's art is drawn on the
/// model, in the sunken disc inside its rim, so the art is lit and shaded with the stone; the flat Icon sprite stays
/// in the prefab (code sets its sprite everywhere) but isn't rendered.
/// The model sizes itself from the art so it fits everywhere ViewBuff is used, whatever scale an instance has (the
/// event screen uses 50x50x1, starter bundles scale the art up): its Transform scale is driven by this component, tune
/// the fields here instead. It also takes the art's layer, so cameras that render only that layer (popup captures)
/// see it too.
/// Only the scale is driven: the model's position is the prefab's (its top at the ViewBuff's origin), whatever the
/// sprite's position.
/// Runs in play mode only, so it never writes overrides into scenes or prefabs; in the editor the prefab shows the
/// sprite over the model at the default fit.
/// How bright the art is: TrinketInlay.mat (Emission colour lifts it on dark boards).
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class TrinketBase : MonoBehaviour
{
    [Tooltip("The trinket art: its sprite is drawn on the model and the sprite itself is hidden.")]
    public SpriteRenderer Icon;

    [Tooltip("Cylinder height as a fraction of its diameter.")]
    [Range(0.02f, 1f)]
    public float Height = 0.15f;

    [Tooltip("Model radius relative to the art's radius.")]
    [Range(0.8f, 1.2f)]
    public float RadiusFit = 1f;

    // The mesh (TrinketBase.fbx) has radius 1 and its height along local +Y; this object is rotated -90 on X so the
    // top faces the viewer (-Z) and the body runs away (+Z). Submesh 2 is the inlay that shows the art.
    const float MeshHeight = 0.3f;
    const int InlaySlot = 2;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
    static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

    MeshRenderer meshRenderer;
    MaterialPropertyBlock block;
    Vector3 lastParentScale, lastIconScale;
    Sprite lastSprite, shownSprite;
    float lastHeight, lastFit;

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
        if (!Icon.forceRenderingOff)
            Icon.forceRenderingOff = true;
        if (Icon.sprite != shownSprite)
            ShowArt(Icon.sprite);

        Vector3 parentScale = parent.lossyScale;
        Vector3 iconScale = Icon.transform.lossyScale;
        if (!force && parentScale == lastParentScale && iconScale == lastIconScale && Icon.sprite == lastSprite
            && Height == lastHeight && RadiusFit == lastFit)
            return;
        lastParentScale = parentScale;
        lastIconScale = iconScale;
        lastSprite = Icon.sprite;
        lastHeight = Height;
        lastFit = RadiusFit;

        if (parentScale.x == 0f || parentScale.y == 0f || parentScale.z == 0f)
            return;

        // world-space radius of the art along x and y, and the height that keeps the cylinder's proportions
        Vector3 extents = Icon.sprite.bounds.extents;
        float radiusX = extents.x * Mathf.Abs(iconScale.x) * RadiusFit;
        float radiusY = extents.y * Mathf.Abs(iconScale.y) * RadiusFit;
        float height = (radiusX + radiusY) * Height;

        // local x stays x, mesh height (local y) runs along the parent's z, mesh z along the parent's y
        transform.localScale = new Vector3(radiusX / parentScale.x, height / MeshHeight / parentScale.z, radiusY / parentScale.y);
    }

    // The inlay's UVs cover the art's full 0..1 square; the sprite's rect in its texture (atlases) becomes the tiling.
    void ShowArt(Sprite sprite)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (block == null)
            block = new MaterialPropertyBlock();
        if (meshRenderer.sharedMaterials.Length <= InlaySlot)
            return;

        Texture2D texture = sprite.texture;
        Rect rect = sprite.textureRect;
        Vector4 st = new Vector4(rect.width / texture.width, rect.height / texture.height,
            rect.x / texture.width, rect.y / texture.height);

        meshRenderer.GetPropertyBlock(block, InlaySlot);
        block.SetTexture(BaseMapId, texture);
        block.SetTexture(EmissionMapId, texture);
        block.SetVector(BaseMapStId, st);
        meshRenderer.SetPropertyBlock(block, InlaySlot);
        shownSprite = sprite;
    }
}
