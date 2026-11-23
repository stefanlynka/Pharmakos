using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns a folder of altar textures (Resources/Content/AltarStyles/&lt;God&gt;/, as written by
/// Tools/Blender/altar_textures.py or replaced by hand) into the four URP Lit materials and the AltarStyle asset.
/// Only creates what's missing: existing materials and styles are left exactly as you tuned them.
/// To start a style over, delete its materials/asset and run it again.
/// </summary>
public static class AltarStyleBuilder
{
    const string Root = "Assets/Resources/Content/AltarStyles";
    const string DarkTextMaterialPath = "Assets/Fonts/AlegreyaSans/AlegreyaSans-Regular Altar Dark.mat";

    [MenuItem("Tools/Altar Kit/Build Missing Altar Styles")]
    public static void BuildAll()
    {
        foreach (string folder in Directory.GetDirectories(Root))
            Build(folder.Replace('\\', '/'));
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Re-picks every style's rules text colour (black or cream) from the texture of the material in its Panel slot.
    /// Run it after regenerating textures or swapping the panel material; it overwrites the TextColor you see on each
    /// style asset, so re-apply any hand-picked colours (e.g. Aphrodite's red, #3F0000, with the Dark text material)
    /// afterwards. Black text also gets the heavier "Altar Dark" text material.
    /// </summary>
    [MenuItem("Tools/Altar Kit/Reset Text Colours From Panels")]
    public static void ResetTextColours()
    {
        foreach (string dir in Directory.GetDirectories(Root))
        {
            string folder = dir.Replace('\\', '/');
            string god = Path.GetFileName(folder);
            var style = AssetDatabase.LoadAssetAtPath<AltarStyle>(folder + "/" + god + ".asset");
            if (style == null)
                continue;
            Color wanted = TextColourFor(folder, style);
            // Dark text reads thinner than light text on the rough stone, so it gets the heavier text material.
            var darkText = AssetDatabase.LoadAssetAtPath<Material>(DarkTextMaterialPath);
            Material wantedMaterial = wanted == Color.black ? darkText : (style.TextMaterial == darkText ? null : style.TextMaterial);
            if (style.TextColor == wanted && style.TextMaterial == wantedMaterial)
                continue;
            Undo.RecordObject(style, "Reset Altar Text Colour");
            style.TextColor = wanted;
            style.TextMaterial = wantedMaterial;
            EditorUtility.SetDirty(style);
        }
        AssetDatabase.SaveAssets();
    }

    // Light text unless the panel is genuinely pale: on the rough stone, black only reads on the lightest stones.
    // Pure black, not a dark tint: the altar text's TMP material renders dark tints washed out.
    const float DarkTextMinBrightness = 0.62f;
    static Color TextColourFor(string folder, AltarStyle style) =>
        PanelBrightness(folder, style) < DarkTextMinBrightness ? new Color(0.93f, 0.89f, 0.80f) : Color.black;

    public static AltarStyle Build(string folder)
    {
        string god = Path.GetFileName(folder);
        ConfigureTextures(folder);

        string stylePath = folder + "/" + god + ".asset";
        var style = AssetDatabase.LoadAssetAtPath<AltarStyle>(stylePath);
        bool isNew = style == null;
        if (isNew)
            style = ScriptableObject.CreateInstance<AltarStyle>();

        if (style.Stone == null) style.Stone = MakeMaterial(folder, god, "Stone", "Stone_Albedo", "Stone_Normal", "Stone_Mask", 1f, 0.8f);
        // The text panel uses the Stone material too: the calmer Panel material read too smooth next to the rough
        // stone. It is still created, so a style can switch back by dragging it into the Panel slot.
        Material panel = MakeMaterial(folder, god, "Panel", "Panel_Albedo", "Stone_Normal", "Stone_Mask", 0.35f, 0.6f);
        if (style.Panel == null) style.Panel = style.Stone != null ? style.Stone : panel;
        // Styles with stains that mustn't run under the rules text (Aphrodite's blood) ship a Tablet texture set:
        // the same stone without them. Its material copies Stone and takes over the Panel slot from Stone.
        Material tablet = MakeTablet(folder, god, style.Stone);
        if (tablet != null && style.Panel == style.Stone) style.Panel = tablet;
        if (style.Trim == null) style.Trim = MakeMaterial(folder, god, "Trim", "Trim_Albedo", "Trim_Normal", "Trim_Mask", 1f, 0.9f);
        if (style.Corner == null) style.Corner = MakeMaterial(folder, god, "Corner", "Corner_Albedo", "Corner_Normal", "Corner_Mask", 1f, 0.9f);

        if (isNew)
        {
            style.TextColor = TextColourFor(folder, style);
            AssetDatabase.CreateAsset(style, stylePath);
        }
        EditorUtility.SetDirty(style);
        return style;
    }

    static Material MakeMaterial(string folder, string god, string slot, string albedo, string normal, string mask, float bumpScale, float smoothness)
    {
        string path = folder + "/" + god + "_" + slot + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = god + "_" + slot };
        material.SetTexture("_BaseMap", Load(folder, albedo));
        Texture2D normalMap = Load(folder, normal);
        if (normalMap != null)
        {
            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_BumpScale", bumpScale);
            material.EnableKeyword("_NORMALMAP");
        }
        Texture2D maskMap = Load(folder, mask);
        if (maskMap != null)
        {
            material.SetTexture("_MetallicGlossMap", maskMap);
            material.SetFloat("_Metallic", 1f);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static Material MakeTablet(string folder, string god, Material stone)
    {
        Texture2D albedo = Load(folder, "Tablet_Albedo");
        if (albedo == null || stone == null)
            return null;
        string path = folder + "/" + god + "_Tablet.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;
        var material = new Material(stone) { name = god + "_Tablet" };
        material.SetTexture("_BaseMap", albedo);
        Texture2D maskMap = Load(folder, "Tablet_Mask");
        if (maskMap != null)
            material.SetTexture("_MetallicGlossMap", maskMap);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static Texture2D Load(string folder, string name)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name + ".png");
    }

    // Normal maps as normal maps, masks linear; trims tile along U only, corners don't tile.
    static void ConfigureTextures(string folder)
    {
        foreach (string file in Directory.GetFiles(folder, "*.png"))
        {
            string path = file.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            string name = Path.GetFileNameWithoutExtension(path);
            bool changed = false;

            if (name.EndsWith("_Normal") && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                changed = true;
            }
            if (name.EndsWith("_Mask") && importer.sRGBTexture)
            {
                importer.sRGBTexture = false;
                changed = true;
            }
            if (name.StartsWith("Trim_") && (importer.wrapModeU != TextureWrapMode.Repeat || importer.wrapModeV != TextureWrapMode.Clamp))
            {
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
                changed = true;
            }
            if (name.StartsWith("Corner_") && importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }
            if (changed)
                importer.SaveAndReimport();
        }
    }

    /// <summary>Average brightness of the panel: the Panel material's base map times its colour (Panel_Albedo.png if
    /// there is no material yet).</summary>
    public static float PanelBrightness(string folder, AltarStyle style)
    {
        string path = folder + "/Panel_Albedo.png";
        Color tint = Color.white;
        if (style != null && style.Panel != null)
        {
            Texture baseMap = style.Panel.GetTexture("_BaseMap");
            if (baseMap != null)
                path = AssetDatabase.GetAssetPath(baseMap);
            tint = style.Panel.GetColor("_BaseColor");
        }
        if (!File.Exists(path))
            return 1f;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(path));
        float sum = 0f;
        for (int i = 0; i < 64; i++)
            sum += (texture.GetPixelBilinear((i % 8 + 0.5f) / 8f, (i / 8 + 0.5f) / 8f) * tint).grayscale;
        Object.DestroyImmediate(texture);
        return sum / 64f;
    }
}
