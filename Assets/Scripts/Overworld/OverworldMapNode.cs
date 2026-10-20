using System.Collections.Generic;
using UnityEngine;

public class OverworldMapNode : MonoBehaviour
{
    /// <summary>DEV: when true, the player can move to any node on the overworld map, ignoring adjacency and cleared state.</summary>
    public static bool DevMoveAnywhere = true;

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
        { EncounterType.BossFate, new List<string> { "OverworldHex", "Green", "Green" } },
        { EncounterType.BossGate, new List<string> { "OverworldHex", "Purple", "Purple" } },
        { EncounterType.BossThrone, new List<string> { "OverworldHex", "Black", "Black" } },
        { EncounterType.Styx, new List<string> { "OverworldHex", "Blue", "Blue" } },
        { EncounterType.TutorialCombat1, new List<string> { "OverworldHex", "Red", "Red" } },
        { EncounterType.TutorialCombat2, new List<string> { "OverworldHex", "Red", "Red" } },
        { EncounterType.TutorialTemple, new List<string> { "OverworldHex", "White", "White" } },
        { EncounterType.TutorialMarket, new List<string> { "OverworldHex", "Yellow", "Yellow" } },
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

    [HideInInspector]
    public EncounterType AuthoredTutorialType;

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
    static string GetMeshKeyForEncounter(EncounterType type)
    {
        switch (type)
        {
            case EncounterType.BossFate:
            case EncounterType.BossGate:
            case EncounterType.BossThrone:
                return "Boss";
            case EncounterType.TutorialCombat1:
            case EncounterType.TutorialCombat2:
                return "Combat";
            case EncounterType.TutorialTemple:
                return "Temple";
            case EncounterType.TutorialMarket:
                return "Market";
            default:
                return type.ToString();
        }
    }

    public void ApplyEncounter(EncounterType type)
    {
        if (OverworldEncounters.IsTutorial(type))
            AuthoredTutorialType = type;
        else if (OverworldEncounters.IsTutorial(EncounterType) && AuthoredTutorialType == EncounterType.None)
            AuthoredTutorialType = EncounterType;

        EncounterType = type;
        string key = GetMeshKeyForEncounter(type);

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
    public static void AssignEncountersByFloorRules(OverworldMapNode[] nodes, CustomRandom rng)
    {
        if (nodes == null || rng == null) return;

        var ordered = new List<OverworldMapNode>(nodes);
        ordered.Sort(CompareByFloorThenColumn);

        var byFloor = new Dictionary<int, List<OverworldMapNode>>();
        foreach (var node in ordered)
        {
            if (node == null) continue;
            if (!byFloor.TryGetValue(node.R, out var list))
            {
                list = new List<OverworldMapNode>();
                byFloor[node.R] = list;
            }
            list.Add(node);
        }

        var floors = new List<int>(byFloor.Keys);
        floors.Sort();
        foreach (int floor in floors)
            AssignEncountersOnFloor(byFloor[floor], floor, rng);
    }

    static int CompareByFloorThenColumn(OverworldMapNode a, OverworldMapNode b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int floor = a.R.CompareTo(b.R);
        if (floor != 0) return floor;
        return a.Q.CompareTo(b.Q);
    }

    /// 
    /// Assigns encounters to the nodes on a given floor.
    /// 
    static void AssignEncountersOnFloor(List<OverworldMapNode> floorNodes, int floor, CustomRandom rng)
    {
        var pool = new List<OverworldMapNode>();
        foreach (var node in floorNodes)
        {
            if (node == null) continue;
            if (node.EncounterType != EncounterType.None)
            {
                node.ApplyEncounter(node.EncounterType);
                continue;
            }
            pool.Add(node);
        }

        int nTotal = pool.Count;
        if (nTotal == 0) return;

        pool.Sort(CompareByFloorThenColumn);
        ShuffleInPlace(pool, rng);

        if (floor == 0 && pool.Count > 0)
        {
            pool[0].ApplyEncounter(EncounterType.Market);
            pool.RemoveAt(0);
        }

        int shopCount = 0;
        if (floor == 4) shopCount = 2;
        else if (floor == 7) shopCount = 2;
        shopCount = Mathf.Min(shopCount, pool.Count);

        for (int i = 0; i < shopCount; i++)
            pool[i].ApplyEncounter(EncounterType.Market);
        pool.RemoveRange(0, shopCount);

        int templeTarget = 0;
        if (floor >= 2 && floor != 7)
        {
            templeTarget = 1;
            
            // bool oddFloor = (r & 1) != 0;
            // templeTarget = oddFloor
            //     ? Mathf.CeilToInt(nTotal / 3f)
            //     : Mathf.FloorToInt(nTotal / 3f);
        }
        templeTarget = Mathf.Clamp(templeTarget, 0, pool.Count);
        ShuffleInPlace(pool, rng);
        for (int i = 0; i < templeTarget; i++)
            pool[i].ApplyEncounter(EncounterType.Temple);
        pool.RemoveRange(0, templeTarget);

        int eventTarget = 1; //Mathf.CeilToInt(pool.Count / 4f);
        if (floor == 4 || floor == 4 || floor == 7) eventTarget = 0;
        eventTarget = Mathf.Min(eventTarget, pool.Count);
        ShuffleInPlace(pool, rng);
        for (int i = 0; i < eventTarget; i++)
            pool[i].ApplyEncounter(EncounterType.Event);
        pool.RemoveRange(0, eventTarget);

        foreach (var node in pool)
            node.ApplyEncounter(EncounterType.Combat);
    }

    static void ShuffleInPlace(List<OverworldMapNode> list, CustomRandom rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
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

    [SerializeField, HideInInspector] int _validatedQ;
    [SerializeField, HideInInspector] int _validatedR;
    [SerializeField, HideInInspector] Vector3 _validatedLocalPosition;
    [SerializeField, HideInInspector] bool _hasValidatedAxial;

    private void OnValidate()
    {
        if (Grid == null) return;

        bool axialChanged = !_hasValidatedAxial || Q != _validatedQ || R != _validatedR;
        bool positionChanged = _hasValidatedAxial && transform.localPosition != _validatedLocalPosition;

        if (!axialChanged && !positionChanged)
            return;

        if (axialChanged)
            SnapPositionFromAxial();
        else
        {
            CaptureAxialFromPosition();
            SnapPositionFromAxial();
        }

        _validatedQ = Q;
        _validatedR = R;
        _validatedLocalPosition = transform.localPosition;
        _hasValidatedAxial = true;
    }
}

public static class OverworldEncounters
{
    public static bool IsTutorial(EncounterType type)
    {
        switch (type)
        {
            case EncounterType.TutorialCombat1:
            case EncounterType.TutorialCombat2:
            case EncounterType.TutorialTemple:
            case EncounterType.TutorialMarket:
                return true;
            default:
                return false;
        }
    }
}

public enum EncounterType
{
    None,
    Combat,
    Temple,
    Event,
    Market,
    BossFate,
    BossGate,
    BossThrone,
    Styx,
    TutorialCombat1,
    TutorialCombat2,
    TutorialTemple,
    TutorialMarket,
}