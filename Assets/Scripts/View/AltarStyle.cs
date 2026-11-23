using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One god's look for the ritual altar. The altar mesh (Models/Meshes/AltarKit.fbx) has five material slots,
/// and a style is just the materials for them plus the colour of the rules text on the altar.
/// Styles live at Resources/Content/AltarStyles/&lt;God&gt;/&lt;God&gt;.asset and are picked by <see cref="Ritual.God"/>;
/// rituals whose god has no style use the Default style.
/// </summary>
[CreateAssetMenu(menuName = "Hecatomb/Altar Style", fileName = "AltarStyle")]
public class AltarStyle : ScriptableObject
{
    const string Folder = "Content/AltarStyles/";
    const string DefaultName = "Default";

    [Tooltip("Body, steps and slab sides (material slot 0).")]
    public Material Stone;
    [Tooltip("The table top that carries the rules text (slot 1). Usually the Stone material.")]
    public Material Panel;
    [Tooltip("Raised border strips round the text faces (slot 2). Texture tiles along U; V = 0 is the outer edge.")]
    public Material Trim;
    [Tooltip("Raised corner blocks (slot 3).")]
    public Material Corner;
    [Tooltip("The sloped face that carries the costs (slot 4). Leave empty to use Panel.")]
    public Material Ramp;

    [Tooltip("Colour of the rules text printed on the altar. Light on dark stone, dark on pale stone.")]
    public Color TextColor = Color.black;
    [Tooltip("Optional material for the rules text; empty uses the altar prefab's. Dark text reads thinner than light " +
             "text on the rough stone, so dark-text styles use the heavier 'AlegreyaSans-Regular Altar Dark'.")]
    public Material TextMaterial;

    static readonly Dictionary<string, AltarStyle> cache = new Dictionary<string, AltarStyle>();
    Material[] materials;

    /// <summary>The materials in the altar mesh's slot order (Ramp falls back to Panel).</summary>
    public Material[] Materials
    {
        get
        {
            if (materials == null || materials.Length != 5)
                materials = new[] { Stone, Panel, Trim, Corner, Ramp != null ? Ramp : Panel };
            return materials;
        }
    }

    void OnValidate()
    {
        materials = null;
    }

    /// <summary>The style for a ritual's god, falling back to Default. Null only if no Default style exists.</summary>
    public static AltarStyle For(Ritual ritual)
    {
        if (ritual != null)
        {
            AltarStyle style = Load(ritual.God);
            if (style != null)
                return style;
        }
        return Load(DefaultName);
    }

    static AltarStyle Load(string god)
    {
        if (string.IsNullOrEmpty(god))
            return null;
        if (!cache.TryGetValue(god, out AltarStyle style))
        {
            style = Resources.Load<AltarStyle>(Folder + god + "/" + god);
            cache[god] = style;
        }
        return style;
    }
}
