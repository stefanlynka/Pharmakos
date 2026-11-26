using UnityEngine;

/// <summary>
/// A temporary card back on a ViewCard, so a card drawn from a <see cref="DeckPile"/> can leave it face-down and
/// turn over on its way to the hand. The back is a quad on the card's reverse (local +Z, facing away from the face),
/// so it shows only while the card is turned over and covers the face then. Cards are pooled, so whoever attaches
/// one must <see cref="Detach"/> it.
/// </summary>
public static class CardBackFace
{
    const string ObjectName = "CardBackFace";
    static Mesh quad;

    public static GameObject Attach(ViewCard viewCard, Material material)
    {
        if (viewCard == null || material == null)
            return null;

        Detach(viewCard);
        if (quad == null)
            quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        BoxCollider bounds = viewCard.CardCollider;
        Vector3 centre = bounds != null ? bounds.center : Vector3.zero;
        Vector3 size = bounds != null ? bounds.size : new Vector3(5.4f, 7.59f, 1f);
        Transform parent = bounds != null ? bounds.transform : viewCard.transform;

        var back = new GameObject(ObjectName);
        back.layer = viewCard.gameObject.layer;
        back.transform.SetParent(parent, false);
        // Just behind everything on the card, facing out of its back (the built-in quad faces -Z).
        back.transform.localPosition = centre + new Vector3(0f, 0f, size.z * 0.5f + 0.02f);
        back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        // A touch larger than the collider: the card frame reaches slightly past it.
        back.transform.localScale = new Vector3(size.x * 1.04f, size.y * 1.03f, 1f);
        back.AddComponent<MeshFilter>().sharedMesh = quad;
        var renderer = back.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return back;
    }

    public static void Detach(ViewCard viewCard)
    {
        if (viewCard == null)
            return;
        Transform parent = viewCard.CardCollider != null ? viewCard.CardCollider.transform : viewCard.transform;
        Transform back = parent.Find(ObjectName);
        if (back != null)
            Object.Destroy(back.gameObject);
    }
}
