using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Boards → Build Troy Wall: generates the 3D wall of Troy that stands along the far edge of the Troy board
/// (mesh, material, prefab in Resources/Content/Boards/Troy) and plugs it into the Troy BoardStyle's BackEdge.
/// The mesh is built in board world space (x across, y up the board, height toward the camera = -z from the board plane
/// at z = 33), so View.ShowBoard places the prefab at the world origin. Textures come from Tools/Boards/recipes/troy.py
/// (paint_wall); the constants below must match WALL_Y, WALL_HALF, WALL_H, BASE_H, TOWER_X, TOWER_W there.
/// </summary>
public static class BoardBackEdgeBuilder
{
    const string Folder = BoardStyleBuilder.Root + "/Troy";

    const float BoardZ = 33f;
    const float WallY = 34f;         // the foot of the wall on the board
    const float Half = 75f;
    const float TexH = 300f / (4096f / 150f);   // heights the elevation texture covers (about 11)
    const float BaseH = 2.4f;        // battered limestone base
    const float Batter = 0.45f;      // how far the base slopes back per unit of height
    const float WallH = 4.6f;         // top of the walkway (low enough that the crenellations stay in frame)
    const float Thick = 2.0f;
    const float TowerX = 44f;
    const float TowerW = 9f;
    const float TowerFwd = 1.6f;     // towers stand this far forward of the wall's foot
    const float TowerH = 7.5f;    // towers rise out of frame
    const float MerlonW = 0.9f, MerlonGap = 0.65f, MerlonH = 1.3f, MerlonD = 0.55f;

    // texture layout (see troy.py WALL_TEX): elevation in v 0..ElevV for heights 0..TexH, tops in v ElevV..1
    const float ElevV = 300f / 384f;
    const float TopDepth = 84f / (4096f / (2f * Half));

    [MenuItem("Tools/Boards/Build Troy Wall")]
    public static void Build()
    {
        Mesh mesh = BuildMesh();
        string meshPath = Folder + "/TroyWall_Mesh.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            mesh = existing;
        }
        else
            AssetDatabase.CreateAsset(mesh, meshPath);

        string matPath = Folder + "/Troy_Wall.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetFloat("_Smoothness", 1f);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Wall_Albedo.jpg"));
        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Wall_Normal.jpg"));
        mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Wall_Mask.png"));
        mat.EnableKeyword("_NORMALMAP");
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        // The board's spotlights reach the wall's face only at a grazing angle and the scene has no ambient light, so
        // the wall carries a little self-light from its own albedo (stand-in for bounce light). Created once; tune
        // _EmissionColor on the material.
        if (!mat.IsKeywordEnabled("_EMISSION"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.45f, 0.4f, 0.35f));
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        mat.SetTexture("_EmissionMap", mat.GetTexture("_BaseMap"));
        EditorUtility.SetDirty(mat);

        string prefabPath = Folder + "/TroyWall.prefab";
        GameObject go = new GameObject("TroyWall");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        BoardStyle style = AssetDatabase.LoadAssetAtPath<BoardStyle>(Folder + "/Troy.asset");
        if (style != null)
        {
            style.BackEdge = prefab;
            EditorUtility.SetDirty(style);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Troy wall built: " + mesh.vertexCount + " vertices");
    }

    // ------------------------------------------------------------------ mesh ---
    static List<Vector3> verts;
    static List<Vector3> norms;
    static List<Vector2> uvs;
    static List<int> tris;

    // board point (x, y, height) -> world
    static Vector3 P(float x, float y, float h) => new Vector3(x, y, BoardZ - h);

    static Vector2 Elev(Vector3 w, float along)
    {
        float h = BoardZ - w.z;
        return new Vector2((along + Half) / (2f * Half), Mathf.Clamp01(h / TexH) * ElevV);
    }

    static Vector2 Top(Vector3 w)
    {
        float d = Mathf.Repeat(w.y - WallY, TopDepth);
        return new Vector2((w.x + Half) / (2f * Half), ElevV + (1f - ElevV) * d / TopDepth);
    }

    /// <summary>Quad a-b-c-d (any winding); faces it toward 'outward'. Picks its UV mapping from its normal.</summary>
    static void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        if (Vector3.Dot(n, outward) < 0)
        {
            Vector3 t = b; b = d; d = t;
            n = -n;
        }
        int i = verts.Count;
        foreach (Vector3 v in new[] { a, b, c, d })
        {
            verts.Add(v);
            norms.Add(n);
            if (Mathf.Abs(n.z) > 0.8f)
                uvs.Add(Top(v));
            else if (Mathf.Abs(n.x) > 0.7f)
                uvs.Add(Elev(v, v.x + (v.y - WallY)));       // side faces: run the elevation sideways
            else
                uvs.Add(Elev(v, v.x));
        }
        tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });     // cross(b - a, c - a) is the front side in Unity
    }

    /// <summary>A block whose front face is battered: foot at y0 (height h0), front at y0 + batter*(h - h0) higher up.</summary>
    static void Block(float x0, float x1, float y0, float depth, float h0, float h1, float batter, bool sides)
    {
        float yt = y0 + batter * (h1 - h0);
        float yb = y0 + depth;
        Quad(P(x0, y0, h0), P(x1, y0, h0), P(x1, yt, h1), P(x0, yt, h1), new Vector3(0, -1, -batter));   // front
        Quad(P(x0, yt, h1), P(x1, yt, h1), P(x1, yb, h1), P(x0, yb, h1), Vector3.back);                    // top
        Quad(P(x0, yb, h0), P(x1, yb, h0), P(x1, yb, h1), P(x0, yb, h1), Vector3.up);                      // back
        if (sides)
        {
            Quad(P(x0, y0, h0), P(x0, yt, h1), P(x0, yb, h1), P(x0, yb, h0), Vector3.left);
            Quad(P(x1, y0, h0), P(x1, yt, h1), P(x1, yb, h1), P(x1, yb, h0), Vector3.right);
        }
    }

    static void Merlons(float x0, float x1, float yFront, float h)
    {
        float span = x1 - x0;
        int count = Mathf.Max(1, Mathf.FloorToInt((span + MerlonGap) / (MerlonW + MerlonGap)));
        float used = count * MerlonW + (count - 1) * MerlonGap;
        float start = x0 + (span - used) / 2f;
        for (int i = 0; i < count; i++)
        {
            float mx = start + i * (MerlonW + MerlonGap);
            Block(mx, mx + MerlonW, yFront, MerlonD, h, h + MerlonH, 0f, true);
        }
    }

    static Mesh BuildMesh()
    {
        verts = new List<Vector3>();
        norms = new List<Vector3>();
        uvs = new List<Vector2>();
        tris = new List<int>();

        float wallFront = WallY + Batter * BaseH;           // where the vertical mudbrick wall stands
        // wall runs between and beyond the towers
        float[] xs = { -Half, -TowerX - TowerW / 2, -TowerX + TowerW / 2, TowerX - TowerW / 2, TowerX + TowerW / 2, Half };
        for (int k = 0; k < xs.Length - 1; k++)
        {
            bool isTower = k == 1 || k == 3;
            float x0 = xs[k], x1 = xs[k + 1];
            if (isTower)
            {
                float ty = WallY - TowerFwd;
                float tFront = ty + Batter * BaseH;
                Block(x0, x1, ty, Thick + TowerFwd + Batter * BaseH, 0f, BaseH, Batter, true);
                Block(x0, x1, tFront, Thick + TowerFwd, BaseH, TowerH, 0f, true);
                Merlons(x0 + 0.2f, x1 - 0.2f, tFront, TowerH);
            }
            else
            {
                Block(x0, x1, WallY, Thick + Batter * BaseH, 0f, BaseH, Batter, false);
                Block(x0, x1, wallFront, Thick, BaseH, WallH, 0f, false);
                Merlons(x0 + 0.4f, x1 - 0.4f, wallFront, WallH);
            }
        }

        Mesh mesh = new Mesh { name = "TroyWall" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }
}
