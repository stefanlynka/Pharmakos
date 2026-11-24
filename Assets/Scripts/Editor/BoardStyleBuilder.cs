using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Boards → Build Board Styles: for every folder in Assets/Resources/Content/Boards that holds textures painted by
/// Tools/Boards/boards.py, sets the texture import settings and creates (or updates) the folder's materials and
/// BoardStyle asset. Run it with GameScene open: the UV mapping is measured from NewBackgroundHolder's Background and
/// PlayerBattleRow meshes, so the painted ground lines up with the lanes. See Docs/Boards.md.
/// Only textures and UV tiling are written on existing materials; colours, smoothness and emission strength you tune in
/// the inspector are kept.
/// </summary>
public static class BoardStyleBuilder
{
    public const string Root = "Assets/Resources/Content/Boards";

    // The world rectangles the painter covers (keep in sync with Tools/Boards/boardgen.py).
    static readonly Rect GroundRect = Rect.MinMaxRect(-68f, -24f, 68f, 44f);

    [MenuItem("Tools/Boards/Build Board Styles")]
    public static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Root))
        {
            Debug.LogError("No " + Root + " folder.");
            return;
        }

        if (!TryMeasureMeshes(out Vector4 groundST, out Vector4 playerLaneST, out Vector4 enemyLaneST))
            return;

        foreach (string folder in AssetDatabase.GetSubFolders(Root))
            Build(folder, groundST, playerLaneST, enemyLaneST);
        AssetDatabase.SaveAssets();
    }

    static void Build(string folder, Vector4 groundST, Vector4 playerLaneST, Vector4 enemyLaneST)
    {
        string name = Path.GetFileName(folder);
        if (!File.Exists(folder + "/Ground_Albedo.jpg"))
            return;

        Material ground = MakeMaterial(folder, name + "_Ground", "Ground", groundST);
        Material playerLane = MakeMaterial(folder, name + "_Lane", "Lane", playerLaneST);
        Material enemyLane = MakeMaterial(folder, name + "_EnemyLane", "Lane", enemyLaneST);

        string stylePath = folder + "/" + name + ".asset";
        BoardStyle style = AssetDatabase.LoadAssetAtPath<BoardStyle>(stylePath);
        if (style == null)
        {
            style = ScriptableObject.CreateInstance<BoardStyle>();
            AssetDatabase.CreateAsset(style, stylePath);
        }
        style.Ground = ground;
        style.PlayerLane = playerLane;
        style.EnemyLane = enemyLane;
        EditorUtility.SetDirty(style);
        Debug.Log("Board style built: " + name);
    }

    static Material MakeMaterial(string folder, string matName, string prefix, Vector4 st)
    {
        string path = folder + "/" + matName + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetFloat("_Smoothness", 1f);     // scales the mask's smoothness
            m.SetColor("_BaseColor", Color.white);
            m.SetColor("_EmissionColor", Color.white);
            AssetDatabase.CreateAsset(m, path);
        }

        Texture2D albedo = Load(folder, prefix, "Albedo.jpg");
        Texture2D normal = Load(folder, prefix, "Normal.jpg");
        Texture2D mask = Load(folder, prefix, "Mask.png");
        Texture2D emission = Load(folder, prefix, "Emission.jpg");

        m.SetTexture("_BaseMap", albedo);
        m.SetTexture("_BumpMap", normal);
        m.SetTexture("_MetallicGlossMap", mask);
        m.SetTexture("_EmissionMap", emission);
        SetKeyword(m, "_NORMALMAP", normal != null);
        SetKeyword(m, "_METALLICSPECGLOSSMAP", mask != null);
        SetKeyword(m, "_EMISSION", emission != null);
        m.globalIlluminationFlags = emission != null ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;

        foreach (string tex in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" })
        {
            m.SetTextureScale(tex, new Vector2(st.x, st.y));
            m.SetTextureOffset(tex, new Vector2(st.z, st.w));
        }
        m.SetVector("_BaseMap_ST", st);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D Load(string folder, string prefix, string suffix)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + prefix + "_" + suffix);
    }

    static void SetKeyword(Material m, string keyword, bool on)
    {
        if (on)
            m.EnableKeyword(keyword);
        else
            m.DisableKeyword(keyword);
    }

    /// <summary>Tiling/offset that put the painted ground rect and the lane canvas onto the scene meshes.</summary>
    static bool TryMeasureMeshes(out Vector4 groundST, out Vector4 playerLaneST, out Vector4 enemyLaneST)
    {
        groundST = playerLaneST = enemyLaneST = Vector4.zero;
        GameObject holder = GameObject.Find("NewBackgroundHolder");
        Transform ground = holder != null ? holder.transform.Find("Background") : null;
        Transform lane = holder != null ? holder.transform.Find("PlayerBattleRow") : null;
        if (ground == null || lane == null)
        {
            Debug.LogError("Build Board Styles: open GameScene (needs NewBackgroundHolder/Background and PlayerBattleRow).");
            return false;
        }

        // Ground: the painter saves the ground turned 180 degrees, so texture u runs from the rect's right edge to its
        // left and v from its top to its bottom, which matches the Plane mesh's own UV directions (-x, -y).
        Mesh gm = ground.GetComponent<MeshFilter>().sharedMesh;
        Vector3 p00 = Vector3.zero, p10 = Vector3.zero, p01 = Vector3.zero;
        Vector3[] gv = gm.vertices;
        Vector2[] guv = gm.uv;
        for (int i = 0; i < gv.Length; i++)
        {
            Vector3 w = ground.TransformPoint(gv[i]);
            if (guv[i] == new Vector2(0, 0)) p00 = w;
            if (guv[i] == new Vector2(1, 0)) p10 = w;
            if (guv[i] == new Vector2(0, 1)) p01 = w;
        }
        float tx = -(p10.x - p00.x) / GroundRect.width;
        float ox = (GroundRect.xMax - p00.x) / GroundRect.width;
        float ty = -(p01.y - p00.y) / GroundRect.height;
        float oy = (GroundRect.yMax - p00.y) / GroundRect.height;
        if (tx <= 0 || ty <= 0)
            Debug.LogWarning("Build Board Styles: the Background plane's UVs no longer run toward -x/-y; the ground will be mirrored.");
        groundST = new Vector4(tx, ty, ox, oy);

        // Lanes: the top face's UV square (u along +x over 60 units, v along +y over the lane's 10 units) maps to one
        // half of the lane canvas: the bottom half is the player lane, the top half the enemy lane.
        // The lane mesh isn't Read/Write enabled, so its top face's UV square is recorded here (measured in play mode:
        // u 0.625..0.875 along x, v 0.4964..0.7536 across the lane). Re-measure if BattleRow is re-exported.
        Mesh lm = lane.GetComponent<MeshFilter>().sharedMesh;
        float umin = 0.625f, umax = 0.875f, vmin = 0.4964f, vmax = 0.7536f;
        if (lm.isReadable)
        {
            Vector3[] lv = lm.vertices;
            Vector2[] luv = lm.uv;
            Vector3[] ln = lm.normals;
            umin = vmin = 9;
            umax = vmax = -9;
            for (int i = 0; i < lv.Length; i++)
            {
                if (ln[i].y < 0.99f) continue;
                umin = Mathf.Min(umin, luv[i].x);
                umax = Mathf.Max(umax, luv[i].x);
                vmin = Mathf.Min(vmin, luv[i].y);
                vmax = Mathf.Max(vmax, luv[i].y);
            }
        }
        float lx = 1f / (umax - umin);
        float ly = 0.5f / (vmax - vmin);
        playerLaneST = new Vector4(lx, ly, -umin * lx, -vmin * ly);
        enemyLaneST = new Vector4(lx, ly, -umin * lx, -vmin * ly + 0.5f);
        return true;
    }
}

/// <summary>Import settings for the painted board textures (Assets/Resources/Content/Boards/*), applied on import so
/// regenerating them only needs a refresh: clamped, mipmapped, high-quality compression, normals as normal maps and
/// the smoothness mask linear.</summary>
public class BoardTexturePostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(BoardStyleBuilder.Root + "/"))
            return;
        TextureImporter ti = (TextureImporter)assetImporter;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        ti.textureType = file.EndsWith("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = !file.EndsWith("_Mask");
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.mipmapEnabled = true;
        ti.anisoLevel = 8;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
