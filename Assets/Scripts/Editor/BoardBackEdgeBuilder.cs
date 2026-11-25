using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Boards → Build Back Edges: turns the back edges painted and modelled by Tools/Boards (backedge.py and each
/// recipe's back_edge()) into Unity assets. For every board with a Claude/Boards/backedge_json/&lt;Name&gt;.json it builds
/// &lt;Name&gt;_BackEdge_Mesh.asset (one submesh per material), a URP Lit material per submesh from the BE_&lt;material&gt;_*
/// textures in the board's folder, and &lt;Name&gt;_BackEdge.prefab, and sets the board's BoardStyle.BackEdge.
/// The mesh is in board world space (View.ShowBoard places the prefab at the world origin). See Docs/Boards.md.
/// Existing materials keep any colour / smoothness / emission strength tuned in the inspector; only their textures and
/// keywords are rewritten.
/// </summary>
public static class BoardBackEdgeBuilder
{
    const string JsonFolder = "Claude/Boards/backedge_json";

    [System.Serializable]
    class SubData
    {
        public string material;
        public int[] triangles;
        public float selfLight;
        public bool cutout;
        public float emission;
    }

    [System.Serializable]
    class EdgeData
    {
        public string name;
        public float[] vertices;
        public float[] normals;
        public float[] uvs;
        public SubData[] submeshes;
    }

    [MenuItem("Tools/Boards/Build Back Edges")]
    public static void BuildAll()
    {
        if (!Directory.Exists(JsonFolder))
        {
            Debug.LogError("No " + JsonFolder + ": run python Tools/Boards/boards.py first.");
            return;
        }
        foreach (string file in Directory.GetFiles(JsonFolder, "*.json"))
            Build(file);
        AssetDatabase.SaveAssets();
    }

    static void Build(string file)
    {
        EdgeData data = JsonUtility.FromJson<EdgeData>(File.ReadAllText(file));
        string folder = BoardStyleBuilder.Root + "/" + data.name;
        BoardStyle style = AssetDatabase.LoadAssetAtPath<BoardStyle>(folder + "/" + data.name + ".asset");
        if (style == null)
        {
            Debug.LogWarning("Back edge " + data.name + ": no BoardStyle at " + folder);
            return;
        }

        int count = data.vertices.Length / 3;
        var verts = new Vector3[count];
        var norms = new Vector3[count];
        var uvs = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            verts[i] = new Vector3(data.vertices[i * 3], data.vertices[i * 3 + 1], data.vertices[i * 3 + 2]);
            norms[i] = new Vector3(data.normals[i * 3], data.normals[i * 3 + 1], data.normals[i * 3 + 2]);
            uvs[i] = new Vector2(data.uvs[i * 2], data.uvs[i * 2 + 1]);
        }

        Mesh mesh = new Mesh { name = data.name + "_BackEdge" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.normals = norms;
        mesh.uv = uvs;
        mesh.subMeshCount = data.submeshes.Length;
        var materials = new List<Material>();
        for (int s = 0; s < data.submeshes.Length; s++)
        {
            mesh.SetTriangles(data.submeshes[s].triangles, s);
            materials.Add(MakeMaterial(folder, data.submeshes[s]));
        }
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        string meshPath = folder + "/" + data.name + "_BackEdge_Mesh.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            mesh = existing;
        }
        else
            AssetDatabase.CreateAsset(mesh, meshPath);

        GameObject go = new GameObject(data.name + "_BackEdge");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = materials.ToArray();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, folder + "/" + data.name + "_BackEdge.prefab");
        Object.DestroyImmediate(go);

        style.BackEdge = prefab;
        EditorUtility.SetDirty(style);
        Debug.Log("Back edge built: " + data.name + " (" + count + " vertices, " + data.submeshes.Length + " materials)");
    }

    static Material MakeMaterial(string folder, SubData sub)
    {
        string prefix = folder + "/BE_" + sub.material;
        string path = prefix + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetFloat("_Smoothness", 1f);                                  // scales the mask's smoothness
            m.SetColor("_EmissionColor", Color.white * Mathf.Max(sub.emission, 0.01f));
            AssetDatabase.CreateAsset(m, path);
        }
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_Albedo.png");
        if (albedo == null)
            albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_Albedo.jpg");
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_Normal.jpg");
        Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_Mask.png");
        Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "_Emission.jpg");

        m.SetTexture("_BaseMap", albedo);
        m.SetTexture("_BumpMap", normal);
        m.SetTexture("_MetallicGlossMap", mask);
        m.SetTexture("_EmissionMap", emission);
        Keyword(m, "_NORMALMAP", normal != null);
        Keyword(m, "_METALLICSPECGLOSSMAP", mask != null);
        Keyword(m, "_EMISSION", emission != null);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

        m.SetFloat("_AlphaClip", sub.cutout ? 1f : 0f);
        m.SetFloat("_Cutoff", 0.5f);
        Keyword(m, "_ALPHATEST_ON", sub.cutout);
        m.SetFloat("_Cull", sub.cutout ? 0f : 2f);                       // leaves and threads show both sides
        m.renderQueue = sub.cutout ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Keyword(Material m, string keyword, bool on)
    {
        if (on) m.EnableKeyword(keyword);
        else m.DisableKeyword(keyword);
    }
}
