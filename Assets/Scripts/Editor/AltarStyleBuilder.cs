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

    [MenuItem("Tools/Altar Kit/Build Missing Altar Styles")]
    public static void BuildAll()
    {
        foreach (string folder in Directory.GetDirectories(Root))
            Build(folder.Replace('\\', '/'));
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Re-picks every style's rules text colour (black or cream) from its panel texture. Run it after regenerating
    /// the textures with a different panel setting; it overwrites the TextColor you see on each style asset.
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
            Color wanted = TextColourFor(folder);
            if (style.TextColor == wanted)
                continue;
            Undo.RecordObject(style, "Reset Altar Text Colour");
            style.TextColor = wanted;
            EditorUtility.SetDirty(style);
        }
        AssetDatabase.SaveAssets();
    }

    // Pure black, not a dark tint: the altar text's TMP material renders dark tints washed out.
    static Color TextColourFor(string folder) => PanelIsDark(folder) ? new Color(0.93f, 0.89f, 0.80f) : Color.black;

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
        if (style.Panel == null) style.Panel = MakeMaterial(folder, god, "Panel", "Panel_Albedo", "Stone_Normal", "Stone_Mask", 0.35f, 0.6f);
        if (style.Trim == null) style.Trim = MakeMaterial(folder, god, "Trim", "Trim_Albedo", "Trim_Normal", "Trim_Mask", 1f, 0.9f);
        if (style.Corner == null) style.Corner = MakeMaterial(folder, god, "Corner", "Corner_Albedo", "Corner_Normal", "Corner_Mask", 1f, 0.9f);

        if (isNew)
        {
            style.TextColor = TextColourFor(folder);
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

    static bool PanelIsDark(string folder)
    {
        string path = folder + "/Panel_Albedo.png";
        if (!File.Exists(path))
            return false;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(path));
        float sum = 0f;
        for (int i = 0; i < 16; i++)
            sum += texture.GetPixelBilinear((i % 4 + 0.5f) / 4f, (i / 4 + 0.5f) / 4f).grayscale;
        Object.DestroyImmediate(texture);
        return sum / 16f < 0.45f;
    }
}
