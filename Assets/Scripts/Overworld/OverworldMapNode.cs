using System.Collections.Generic;
using UnityEngine;

public class OverworldMapNode : MonoBehaviour
{
    const string HexMeshResourcesPath = "Overworld/Hexes/Meshes/";
    const string HexMaterialResourcesPath = "Overworld/Hexes/Materials/";
    const string FallbackHexMaterialName = "OverworldHex";
    /// <summary>Types eligible for random assignment on a new run (excludes <see cref="EncounterType.None"/> and <see cref="EncounterType.Boss"/>).</summary>
    static readonly EncounterType[] RollableEncounterTypes =
    {
        EncounterType.Combat,
        EncounterType.Temple,
        EncounterType.Event,
        EncounterType.Market,
    };

    static readonly Dictionary<EncounterType, List<string>> MaterialsByEncounterType = new Dictionary<EncounterType, List<string>>
    {
        { EncounterType.None, new List<string> { FallbackHexMaterialName } },
        { EncounterType.Combat, new List<string> { "OverworldHex", "Red", "Red"} },
        { EncounterType.Temple, new List<string> { "OverworldHex", "White", "White" } },
        { EncounterType.Event, new List<string> { "Event" } },
        { EncounterType.Market, new List<string> { "OverworldHex", "Yellow", "Yellow" } },
        { EncounterType.Boss, new List<string> { "OverworldHex", "Black", "Black" } },
    };

    [Tooltip("Axial hex coordinates. Use context menu to sync from position.")]
    public int Q;
    [Tooltip("Axial hex coordinates. Use context menu to sync from position.")]
    public int R;

    [Tooltip("Player starts on this node. Exactly one per map.")]
    public bool IsStart;

    public MeshRenderer HexMeshRenderer;

    [HideInInspector]
    public bool Cleared;

    public Vector2Int Axial => new Vector2Int(Q, R);

    private OverworldHexGrid _grid;

    public EncounterType EncounterType;

    public OverworldHexGrid Grid
    {
        get
        {
            if (_grid == null)
                _grid = GetComponentInParent<OverworldHexGrid>();
            return _grid;
        }
    }

    public void SnapPositionFromAxial()
    {
        var grid = Grid;
        if (grid == null) return;

        transform.localPosition = grid.AxialToLocal(Q, R);
    }

    public void CaptureAxialFromPosition()
    {
        var grid = Grid;
        if (grid == null) return;

        var axial = grid.LocalToAxial(transform.localPosition);
        Q = axial.x;
        R = axial.y;
    }

    public void SnapPositionFromAxialAndCapture()
    {
        CaptureAxialFromPosition();
        SnapPositionFromAxial();
    }

    public List<OverworldMapNode> GetNeighborNodes()
    {
        if (Grid == null) return new List<OverworldMapNode>();
        var neighbors = new List<OverworldMapNode>();
        var coords = OverworldHexGrid.GetNeighbors(Q, R);
        foreach (var node in Grid.GetComponentsInChildren<OverworldMapNode>())
        {
            if (node == this) continue;
            if (coords.Exists(c => c.x == node.Q && c.y == node.R))
                neighbors.Add(node);
        }
        return neighbors;
    }

    /// <summary>
    /// Sets <see cref="EncounterType"/> and loads the hex base mesh from Resources/Overworld/Hexes/Meshes.
    /// Applies materials named in <see cref="MaterialsByEncounterType"/>, loaded from <c>Resources/Overworld/Hexes/Materials/</c>.
    /// </summary>
    public void ApplyEncounter(EncounterType type)
    {
        EncounterType = type;
        string key = type.ToString();

        if (HexMeshRenderer == null)
            HexMeshRenderer = GetComponentInChildren<MeshRenderer>();
        if (HexMeshRenderer == null)
            Debug.LogError($"OverworldMapNode: No HexMeshRenderer on {name}.");
        else
        {
            Mesh hexMesh = LoadMeshAtResourcesPath(HexMeshResourcesPath + key);
            if (hexMesh == null)
                Debug.LogError($"OverworldMapNode: Missing hex mesh at Resources/{HexMeshResourcesPath}{key}");

            Material[] hexMaterials = BuildHexMaterialsForEncounter(type, hexMesh);
            ApplyMeshAndMaterialsToRenderer(HexMeshRenderer, hexMesh, hexMaterials);
        }

    }

    static Material[] BuildHexMaterialsForEncounter(EncounterType type, Mesh mesh)
    {
        int slotCount = mesh != null ? Mathf.Max(1, mesh.subMeshCount) : 1;
        if (!MaterialsByEncounterType.TryGetValue(type, out var names) || names == null || names.Count == 0)
            names = new List<string> { FallbackHexMaterialName };

        var materials = new Material[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            string materialName = i < names.Count ? names[i] : names[names.Count - 1];
            var mat = Resources.Load<Material>(HexMaterialResourcesPath + materialName);
            if (mat == null)
            {
                Debug.LogError($"OverworldMapNode: Missing hex material at Resources/{HexMaterialResourcesPath}{materialName}");
                mat = Resources.Load<Material>(HexMaterialResourcesPath + FallbackHexMaterialName);
            }
            materials[i] = mat;
        }

        return materials;
    }

    /// <summary>
    /// Sets the Unity layer on this node (recursively) and the URP rendering layer mask on all renderers under this node.
    /// </summary>
    public void ApplyHexInteractionLayers(int unityLayer, uint renderingLayerMask)
    {
        SetLayerRecursively(gameObject, unityLayer);
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.renderingLayerMask = renderingLayerMask;
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        var t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }

    static void ApplyMeshAndMaterialsToRenderer(MeshRenderer renderer, Mesh mesh, Material[] materials)
    {
        if (renderer == null) return;

        var filter = renderer.GetComponent<MeshFilter>();
        if (filter != null && mesh != null)
            filter.sharedMesh = mesh;

        if (materials != null && materials.Length > 0)
            renderer.sharedMaterials = materials;

        var meshCollider = renderer.GetComponent<MeshCollider>();
        if (meshCollider != null && mesh != null)
            meshCollider.sharedMesh = mesh;
    }

    static Mesh LoadMeshAtResourcesPath(string resourcesPath)
    {
        var mesh = Resources.Load<Mesh>(resourcesPath);
        if (mesh != null) return mesh;
        foreach (var obj in Resources.LoadAll(resourcesPath))
        {
            if (obj is Mesh m)
                return m;
        }
        return null;
    }

    public static EncounterType GetRandomRollableEncounterType()
    {
        return RollableEncounterTypes[Random.Range(0, RollableEncounterTypes.Length)];
    }

    /// <summary>
    /// Floor = axial <see cref="R"/>: 0,1,2,….
    /// Shops on R=4 (1) and R=7 (3). From R≥2, temples = ⌈n/3⌉ on odd floors and ⌊n/3⌋ on even (n = nodes on that floor).
    /// Among remaining nodes per floor, ⌈remaining/4⌉ are events; the rest combat.
    /// </summary>
    public static void AssignEncountersByFloorRules(OverworldMapNode[] nodes)
    {
        if (nodes == null) return;

        var byFloor = new Dictionary<int, List<OverworldMapNode>>();
        foreach (var node in nodes)
        {
            if (node == null) continue;
            if (!byFloor.TryGetValue(node.R, out var list))
            {
                list = new List<OverworldMapNode>();
                byFloor[node.R] = list;
            }
            list.Add(node);
        }

        foreach (var kv in byFloor)
            AssignEncountersOnFloor(kv.Value, kv.Key);
    }

    static void AssignEncountersOnFloor(List<OverworldMapNode> floorNodes, int r)
    {
        int nTotal = floorNodes.Count;
        if (nTotal == 0) return;

        if (r >= 10 && r <= 12)
        {
            foreach (var node in floorNodes)
                node.ApplyEncounter(EncounterType.Boss);
            return;
        }

        var pool = new List<OverworldMapNode>(floorNodes);
        ShuffleInPlace(pool);

        int shopCount = 0;
        if (r == 4) shopCount = 1;
        else if (r == 7) shopCount = 3;
        shopCount = Mathf.Min(shopCount, pool.Count);

        for (int i = 0; i < shopCount; i++)
            pool[i].ApplyEncounter(EncounterType.Market);
        pool.RemoveRange(0, shopCount);

        int templeTarget = 0;
        if (r >= 2)
        {
            bool oddFloor = (r & 1) != 0;
            templeTarget = oddFloor
                ? Mathf.CeilToInt(nTotal / 3f)
                : Mathf.FloorToInt(nTotal / 3f);
        }
        templeTarget = Mathf.Clamp(templeTarget, 0, pool.Count);
        ShuffleInPlace(pool);
        for (int i = 0; i < templeTarget; i++)
            pool[i].ApplyEncounter(EncounterType.Temple);
        pool.RemoveRange(0, templeTarget);

        int eventTarget = Mathf.CeilToInt(pool.Count / 4f);
        eventTarget = Mathf.Min(eventTarget, pool.Count);
        ShuffleInPlace(pool);
        for (int i = 0; i < eventTarget; i++)
            pool[i].ApplyEncounter(EncounterType.Event);
        pool.RemoveRange(0, eventTarget);

        foreach (var node in pool)
            node.ApplyEncounter(EncounterType.Combat);
    }

    static void ShuffleInPlace(List<OverworldMapNode> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private void Start()
    {
        EnsureCollider();
    }

    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        var grid = Grid;
        var hexExtent = grid != null ? grid.HexSize * 2f : 2f;

        var box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(hexExtent * 1.8f, 1f, hexExtent * 1.8f);
        box.center = new Vector3(0, 0.5f, 0);
    }

    private void OnValidate()
    {
        if (Grid != null)
            SnapPositionFromAxial();
    }
}

public enum EncounterType
{
    None,
    Combat,
    Temple,
    Event,
    Boss,
    Market,
}