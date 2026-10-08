using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Chars a card: fades in a flat-coloured copy of its body mesh (Pharmakos/CardChar) drawn over its art, text, and icons.
/// The overlay is a child of the card's CardHolder, so it hides along with the card's contents, and it's removed after
/// Lifetime or as soon as it's no longer visible (e.g. the card is hidden or pooled).
/// Parts of the card that stick out past the body mesh aren't covered.
/// </summary>
public class CharCardVfx : VfxEffect
{
    private const string MaterialPath = "VFX/Materials/CardChar";
    private const string CardBodyName = "CardBasic";
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static Material material;

    public Color CharColor = Color.black;
    public float Delay = 0f;
    public float FadeDuration = 0.9f;
    // Seconds from the start of the effect until the overlay is removed
    public float Lifetime = 3f;
    public string SortingLayerName = "CardLayer";
    // Above the card's own sprites and text (up to 15), below brushstroke flurries (100+)
    public int SortingOrder = 50;

    public override void Play(VfxContext context, Action onComplete)
    {
        MeshRenderer overlay = CreateOverlay(context);
        if (overlay != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            SetAlpha(overlay, block, 0f);

            Sequence sequence = new Sequence();
            sequence.Add(new Tween(elapsed => Step(overlay, block, elapsed), 0f, Lifetime, Lifetime));
            sequence.Add(new SequenceAction(() => DestroyOverlay(overlay)));
            sequence.Start();
        }

        CompleteAfterHold(onComplete);
    }

    private void Step(MeshRenderer overlay, MaterialPropertyBlock block, float elapsed)
    {
        if (overlay == null) return;
        if (!overlay.gameObject.activeInHierarchy)
        {
            DestroyOverlay(overlay);
            return;
        }

        float progress = FadeDuration > 0f
            ? Mathf.Clamp01((elapsed - Delay) / FadeDuration)
            : (elapsed >= Delay ? 1f : 0f);
        SetAlpha(overlay, block, progress * progress);
    }

    private void SetAlpha(MeshRenderer overlay, MaterialPropertyBlock block, float alpha)
    {
        block.SetColor(ColorId, new Color(CharColor.r, CharColor.g, CharColor.b, CharColor.a * alpha));
        overlay.SetPropertyBlock(block);
    }

    private static void DestroyOverlay(MeshRenderer overlay)
    {
        if (overlay != null) UnityEngine.Object.Destroy(overlay.gameObject);
    }

    private MeshRenderer CreateOverlay(VfxContext context)
    {
        Transform anchor = context.GetAnchorTransform(Anchor);
        ViewCard card = anchor != null ? anchor.GetComponent<ViewCard>() : null;
        Transform body = card != null && card.CardHolder != null ? card.CardHolder.transform.Find(CardBodyName) : null;
        MeshRenderer bodyRenderer = body != null ? body.GetComponentInChildren<MeshRenderer>() : null;
        MeshFilter bodyFilter = bodyRenderer != null ? bodyRenderer.GetComponent<MeshFilter>() : null;
        if (bodyFilter == null || bodyFilter.sharedMesh == null)
        {
            Debug.LogWarning("CharCardVfx: anchor isn't a card with a " + CardBodyName + " body mesh.");
            return null;
        }

        Material charMaterial = GetMaterial();
        if (charMaterial == null)
        {
            Debug.LogWarning("CharCardVfx: missing Resources/" + MaterialPath);
            return null;
        }

        GameObject overlayObject = new GameObject("CharOverlay");
        overlayObject.layer = bodyRenderer.gameObject.layer;
        overlayObject.transform.SetParent(bodyRenderer.transform, false);

        Mesh mesh = bodyFilter.sharedMesh;
        overlayObject.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer overlay = overlayObject.AddComponent<MeshRenderer>();
        Material[] materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
        for (int i = 0; i < materials.Length; i++) materials[i] = charMaterial;
        overlay.sharedMaterials = materials;
        overlay.shadowCastingMode = ShadowCastingMode.Off;
        overlay.receiveShadows = false;

        int layerID = SortingLayer.NameToID(SortingLayerName);
        if (SortingLayer.IsValid(layerID)) overlay.sortingLayerID = layerID;
        overlay.sortingOrder = SortingOrder;

        return overlay;
    }

    private static Material GetMaterial()
    {
        if (material == null) material = Resources.Load<Material>(MaterialPath);
        return material;
    }
}
