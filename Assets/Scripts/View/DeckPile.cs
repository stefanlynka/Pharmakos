using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A player's draw pile on the combat board: a stack of face-down cards beside the portrait whose height follows the
/// cards left to draw, with the count on the board on its lane side. Clicking it opens <see cref="DeckPileScreenHandler"/>
/// (your own pile: what's left to draw and your whole deck; the enemy's: their whole deck).
///
/// The stack is drawn with Graphics.DrawMesh in this object's local space (cards lie in local XY, the stack rises
/// toward local -Z, out of the board), so it adds no scene objects and previews in the editor at
/// <see cref="PreviewCount"/>. It is built from layers of a few cards, each nudged and turned a little (seeded, so it
/// never changes between frames) so it reads as a hand-stacked pile rather than a box.
///
/// The count shown lags the model on purpose: it drops as each draw animation leaves the pile
/// (<see cref="NotifyCardLeft"/>) and resyncs with the model whenever no animation is playing.
/// </summary>
[ExecuteAlways]
public class DeckPile : MonoBehaviour
{
    static readonly List<DeckPile> Piles = new List<DeckPile>();

    [Header("Materials")]
    [Tooltip("Side of the stack (CardBack_Edges): one texture row per 8 cards' worth of edges.")]
    public Material SideMaterial;
    public Material PlayerBackMaterial;
    public Material EnemyBackMaterial;

    [Header("Shape (local units)")]
    [Tooltip("Keep the cards' own aspect (ViewCard's collider is 5.4 x 7.59) so a drawn card matches the pile.")]
    public float CardWidth = 4.6f;
    public float CardHeight = 6.47f;
    public float CornerRadius = 0.32f;
    [Range(1, 12)] public int CornerSegments = 5;
    [Tooltip("Thickness of one card. The pile is never taller than MaxHeight.")]
    public float CardThickness = 0.04f;
    public float MaxHeight = 1.6f;
    [Tooltip("Cards per layer; each layer is nudged and turned a little to make the pile look hand-stacked.")]
    [Range(1, 10)] public int CardsPerLayer = 3;
    public float LayerJitter = 0.07f;
    public float LayerTwist = 1.6f;
    [Tooltip("Turn of the whole pile in the board plane, in degrees.")]
    public float PileTwist = -4f;
    [Tooltip("How many cards the side texture shows per V tile (CardBack_Edges has 16 rows).")]
    public float CardsPerSideTile = 16f;

    [Header("Board")]
    [Tooltip("World Z of the board's surface. The pile is drawn sitting on it whatever this object's own depth, " +
             "because the two players' ViewPlayer objects sit at different depths.")]
    public float BoardWorldZ = 33f;

    [Header("Contact shadow")]
    [Tooltip("Pharmakos/AltarContactShadow material: a soft skirt round the pile, and the dark slot left when it's empty.")]
    public Material ShadowMaterial;
    public float ShadowSpread = 0.5f;
    [Tooltip("Strength of the slot (the shadow's inside) once the pile is empty, relative to the material's.")]
    [Range(0f, 1f)] public float EmptySlotStrength = 0.55f;

    [Header("Count label")]
    public TMP_Text CountLabel;
    [Tooltip("Gap between the pile and its count, on the lane side (above your pile, below the enemy's).")]
    public float LabelGap = 0.6f;

    [Header("Hover")]
    public float HoverLift = 0.35f;
    public float HoverBrightness = 1.35f;
    public float LabelHoverBrightness = 1.5f;
    [Tooltip("How fast the hover lift and brightness ease in and out, per second.")]
    public float HoverSpeed = 10f;

    [Header("Editor preview")]
    public int PreviewCount = 30;
    public bool PreviewAsEnemy;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");
    const int ClickLayerMask = 1 << 6;

    Mesh mesh, shadowMesh;
    MaterialPropertyBlock shadowBlock;
    int builtCount = -1;
    string builtKey;
    MaterialPropertyBlock block, labelBlock;
    Color sideBaseColor = Color.white, backBaseColor = Color.white;
    float hover;
    int shownCount = -1;
    ViewPlayer viewPlayer;
    BoxCollider hitBox;
    bool popupShown;

    public Player Player => viewPlayer != null ? viewPlayer.Player : null;
    // In the editor (no Player yet) the AI's ViewPlayer is named ViewPlayerAI.
    bool IsEnemy => Player != null ? !Player.IsHuman
        : PreviewAsEnemy || (viewPlayer != null && viewPlayer.name.Contains("AI"));

    /// <summary>A draw animation took the top card off <paramref name="owner"/>'s pile.</summary>
    public static void NotifyCardLeft(Player owner)
    {
        DeckPile pile = For(owner);
        if (pile != null && pile.shownCount > 0)
            pile.shownCount--;
    }

    /// <summary>The pile shown for <paramref name="owner"/>, or null.</summary>
    public static DeckPile For(Player owner)
    {
        if (owner == null)
            return null;
        foreach (DeckPile pile in Piles)
            if (pile.Player == owner)
                return pile;
        return null;
    }

    public Material BackMaterial => IsEnemy ? EnemyBackMaterial : PlayerBackMaterial;

    /// <summary>World width of one card on the pile.</summary>
    public float WorldCardWidth => CardWidth * transform.lossyScale.x;

    /// <summary>World position of the top card's centre: where drawn cards leave from.</summary>
    public Vector3 TopPosition => BoardMatrix().MultiplyPoint3x4(new Vector3(0f, 0f, -StackHeight(Mathf.Max(0, shownCount)) - HoverLift * hover));

    void OnEnable()
    {
        Piles.Add(this);
        viewPlayer = GetComponentInParent<ViewPlayer>();
        hitBox = GetComponent<BoxCollider>();
    }

    void OnDisable()
    {
        Piles.Remove(this);
        HidePopup();
        DestroyMesh(ref mesh);
        DestroyMesh(ref shadowMesh);
        builtCount = -1;
    }

    void LateUpdate()
    {
        int count = CurrentCount();
        bool hovered = Application.isPlaying && UpdateInput();
        hover = Mathf.MoveTowards(hover, hovered ? 1f : 0f, Time.unscaledDeltaTime * HoverSpeed);

        string key = $"{CardWidth}|{CardHeight}|{CornerRadius}|{CornerSegments}|{CardThickness}|{MaxHeight}|{CardsPerLayer}|{LayerJitter}|{LayerTwist}|{PileTwist}|{CardsPerSideTile}";
        if (mesh == null || count != builtCount || key != builtKey)
            Rebuild(count, key);

        UpdateLabel(count);
        if (Application.isPlaying)
            UpdateHitBox(count);
        Draw(count);
    }

    static void DestroyMesh(ref Mesh m)
    {
        if (m == null)
            return;
        if (Application.isPlaying) Destroy(m);
        else DestroyImmediate(m);
        m = null;
    }

    // This object's transform, moved along Z so its origin lies on the board.
    Matrix4x4 BoardMatrix()
    {
        Vector3 position = transform.position;
        position.z = BoardWorldZ;
        return Matrix4x4.TRS(position, transform.rotation, transform.lossyScale);
    }

    int CurrentCount()
    {
        if (!Application.isPlaying)
            return PreviewCount;

        Player player = Player;
        if (player == null || player.Deck == null)
            return 0;

        int modelCount = player.Deck.Count;
        if (shownCount < 0 || !AnimationHandler.IsAnimating)
            shownCount = modelCount;
        return shownCount;
    }

    float StackHeight(int count) => Mathf.Min(MaxHeight, count * CardThickness);

    // ---- Input ----

    // Returns whether the cursor is over the pile (and it can be clicked); opens the viewer on a click.
    bool UpdateInput()
    {
        bool over = CanInteract() && IsMouseOver();
        if (over && Input.GetMouseButtonDown(0) && !InputGuard.IsBlocked)
        {
            HidePopup();
            DeckPileScreenHandler.Open(Player);
            return false;
        }

        if (over) ShowPopup();
        else HidePopup();
        return over;
    }

    bool CanInteract()
    {
        Controller controller = Controller.Instance;
        View view = View.Instance;
        if (controller == null || view == null || Player == null || !controller.GameRunning || controller.GamePaused)
            return false;
        if (ContentScrollView.BlocksGameInput || TutorialPresenter.BlocksGameInput || OverworldMapView.BlocksGameInput)
            return false;
        // The Game screen is shown (not hidden under a viewer or the map). Not "current": overlays like the
        // play-history button become ScreenHandler's current screen while combat is on.
        if (ScreenHandler.Instance == null || !ScreenHandler.Instance.TryGetScreen(ScreenName.Game, out Screen screen)
            || !screen.IsActive || screen.IsSuppressed)
            return false;
        // Not while a card, attacker or ritual is picked up: the click belongs to that.
        if (view.SelectionHandler != null && view.SelectionHandler.IsSelectingTarget())
            return false;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;
        return true;
    }

    bool IsMouseOver()
    {
        Camera camera = Camera.main;
        if (camera == null || hitBox == null)
            return false;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit, 1000f, ClickLayerMask) && hit.collider == hitBox;
    }

    void ShowPopup()
    {
        if (popupShown || PopupScreenHandler.Instance == null)
            return;

        string text = IsEnemy
            ? "<b>Enemy's Deck</b>\nClick to see every card they might draw."
            : "<b>Your Deck</b>\nClick to see the cards left to draw, and your whole deck.";
        PopupScreenHandler.Instance.ShowTextPopup(GameFonts.WithDisplayTitle(text), transform, PopupPosition.Left);
        popupShown = true;
    }

    void HidePopup()
    {
        if (!popupShown)
            return;
        PopupScreenHandler.Instance?.HideTextPopup(transform);
        popupShown = false;
    }

    // The collider covers the pile as it stands, so the click target shrinks with it.
    void UpdateHitBox(int count)
    {
        if (hitBox == null)
            return;
        float height = Mathf.Max(StackHeight(count), CardThickness) + HoverLift;
        // Local Z of the board under this object (the pile is drawn there, see BoardMatrix).
        float boardZ = transform.InverseTransformPoint(new Vector3(transform.position.x, transform.position.y, BoardWorldZ)).z;
        hitBox.center = new Vector3(0f, 0f, boardZ - height * 0.5f);
        hitBox.size = new Vector3(CardWidth + LayerJitter * 2f, CardHeight + LayerJitter * 2f, height);
    }

    // ---- Label ----

    void UpdateLabel(int count)
    {
        if (CountLabel == null)
            return;

        // On the lane side: the board's up (local +Y) for your pile, down for the enemy's, which faces you across it.
        float side = IsEnemy ? -1f : 1f;
        Vector3 position = CountLabel.transform.localPosition;
        position.y = side * (CardHeight * 0.5f + LabelGap);
        CountLabel.transform.localPosition = position;

        string text = count.ToString();
        if (CountLabel.text != text)
            CountLabel.text = text;

        var renderer = CountLabel.GetComponent<Renderer>();
        if (renderer == null)
            return;
        if (hover <= 0.001f)
        {
            renderer.SetPropertyBlock(null);
            return;
        }
        labelBlock ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(labelBlock);
        float b = Mathf.Lerp(1f, LabelHoverBrightness, hover);
        labelBlock.SetColor(FaceColorId, new Color(b, b, b, 1f));
        renderer.SetPropertyBlock(labelBlock);
    }

    // ---- Drawing ----

    void Draw(int count)
    {
        Matrix4x4 board = BoardMatrix();
        DrawShadow(board, count);

        Material back = IsEnemy ? EnemyBackMaterial : PlayerBackMaterial;
        if (mesh == null || count <= 0 || SideMaterial == null || back == null)
            return;

        Matrix4x4 matrix = board * Matrix4x4.Translate(new Vector3(0f, 0f, -HoverLift * hover));
        block ??= new MaterialPropertyBlock();
        float brightness = Mathf.Lerp(1f, HoverBrightness, hover);

        block.Clear();
        block.SetColor(BaseColorId, MaterialColor(SideMaterial) * brightness);
        Graphics.DrawMesh(mesh, matrix, SideMaterial, gameObject.layer, null, 0, block, true, true);
        block.Clear();
        block.SetColor(BaseColorId, MaterialColor(back) * brightness);
        Graphics.DrawMesh(mesh, matrix, back, gameObject.layer, null, 1, block, true, true);
    }

    void DrawShadow(Matrix4x4 board, int count)
    {
        if (ShadowMaterial == null)
            return;
        if (shadowMesh == null)
            shadowMesh = BuildShadow();

        shadowBlock ??= new MaterialPropertyBlock();
        float strength = ShadowMaterial.HasProperty("_Strength") ? ShadowMaterial.GetFloat("_Strength") : 0.7f;
        // A lifted pile casts a softer, fainter shadow; an empty one leaves a fainter slot.
        float scale = (count > 0 ? 1f : EmptySlotStrength) * Mathf.Lerp(1f, 0.75f, hover);
        shadowBlock.SetFloat("_Strength", strength * scale);
        Graphics.DrawMesh(shadowMesh, board * Matrix4x4.Translate(new Vector3(0f, 0f, -0.01f)), ShadowMaterial,
            gameObject.layer, null, 0, shadowBlock, false, false);
    }

    // The bottom card's outline filled (uv.y = 0, full strength) plus a skirt fading out over ShadowSpread.
    Mesh BuildShadow()
    {
        var m = new Mesh { name = "DeckPileShadow", hideFlags = HideFlags.DontSave };
        List<Vector2> outline = BuildOutline();
        Quaternion rotation = Quaternion.Euler(0f, 0f, PileTwist);
        var vertices = new List<Vector3> { Vector3.zero };
        var uvs = new List<Vector2> { Vector2.zero };
        var tris = new List<int>();
        int n = outline.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 p = outline[i];
            Vector2 outward = p.normalized;
            // Push out along the corner's own direction for the round corners, the side's normal on the flats.
            Vector2 prev = outline[(i - 1 + n) % n], next = outline[(i + 1) % n];
            Vector2 tangent = (next - prev).normalized;
            Vector2 normal = new Vector2(tangent.y, -tangent.x);
            if (Vector2.Dot(normal, outward) < 0f) normal = -normal;
            vertices.Add(rotation * (Vector3)p);
            vertices.Add(rotation * (Vector3)(p + normal * ShadowSpread));
            uvs.Add(Vector2.zero);
            uvs.Add(Vector2.up);
        }
        for (int i = 0; i < n; i++)
        {
            int a = 1 + i * 2, b = 1 + ((i + 1) % n) * 2;
            tris.Add(0); tris.Add(a); tris.Add(b);
            tris.Add(a); tris.Add(a + 1); tris.Add(b);
            tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
        }
        m.SetVertices(vertices);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    static Color MaterialColor(Material material)
    {
        Color c = material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
        c.a = 1f;
        return c;
    }

    void Rebuild(int count, string key)
    {
        if (key != builtKey)
            DestroyMesh(ref shadowMesh);
        builtCount = count;
        builtKey = key;
        if (mesh == null)
            mesh = new Mesh { name = "DeckPile", hideFlags = HideFlags.DontSave };
        mesh.Clear();
        if (count <= 0)
            return;

        List<Vector2> outline = BuildOutline();
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var sides = new List<int>();
        var tops = new List<int>();

        float total = StackHeight(count);
        float perCard = total / count;
        int layers = Mathf.CeilToInt(count / (float)CardsPerLayer);
        float perimeter = 0f;
        for (int i = 0; i < outline.Count; i++)
            perimeter += Vector2.Distance(outline[i], outline[(i + 1) % outline.Count]);

        int cardsBelow = 0;
        for (int layer = 0; layer < layers; layer++)
        {
            int cards = Mathf.Min(CardsPerLayer, count - cardsBelow);
            float z0 = -cardsBelow * perCard;
            float z1 = -(cardsBelow + cards) * perCard;

            // Seeded per layer, so a layer keeps its place as the pile shrinks.
            var random = new System.Random(layer * 7919 + 17);
            Vector2 offset = new Vector2((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f) * LayerJitter;
            float twist = PileTwist + ((float)random.NextDouble() * 2f - 1f) * LayerTwist;
            Quaternion rotation = Quaternion.Euler(0f, 0f, twist);

            // Sides: one quad per outline segment, V counting cards so every card shows its own edge.
            float u = 0f;
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 a2 = outline[i], b2 = outline[(i + 1) % outline.Count];
                Vector3 a = rotation * new Vector3(a2.x, a2.y, 0f) + (Vector3)offset;
                Vector3 b = rotation * new Vector3(b2.x, b2.y, 0f) + (Vector3)offset;
                Vector3 edge = b - a;
                Vector3 normal = new Vector3(edge.y, -edge.x, 0f).normalized;
                float segment = edge.magnitude;
                float v0 = cardsBelow / CardsPerSideTile, v1 = (cardsBelow + cards) / CardsPerSideTile;
                float ua = u / perimeter * 4f, ub = (u + segment) / perimeter * 4f;
                u += segment;

                int start = vertices.Count;
                vertices.Add(new Vector3(a.x, a.y, z0)); uvs.Add(new Vector2(ua, v0));
                vertices.Add(new Vector3(b.x, b.y, z0)); uvs.Add(new Vector2(ub, v0));
                vertices.Add(new Vector3(a.x, a.y, z1)); uvs.Add(new Vector2(ua, v1));
                vertices.Add(new Vector3(b.x, b.y, z1)); uvs.Add(new Vector2(ub, v1));
                for (int k = 0; k < 4; k++) normals.Add(normal);
                sides.Add(start); sides.Add(start + 2); sides.Add(start + 1);
                sides.Add(start + 1); sides.Add(start + 2); sides.Add(start + 3);
            }

            // Top: the card back, facing out of the board (-Z). Every layer has one; only the top layer's shows
            // in full, the others peek out where the layers above are nudged aside.
            int centre = vertices.Count;
            vertices.Add(new Vector3(offset.x, offset.y, z1));
            uvs.Add(new Vector2(0.5f, 0.5f));
            normals.Add(Vector3.back);
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 p = outline[i];
                Vector3 q = rotation * new Vector3(p.x, p.y, 0f) + (Vector3)offset;
                vertices.Add(new Vector3(q.x, q.y, z1));
                uvs.Add(new Vector2(p.x / CardWidth + 0.5f, p.y / CardHeight + 0.5f));
                normals.Add(Vector3.back);
            }
            for (int i = 0; i < outline.Count; i++)
            {
                tops.Add(centre);
                tops.Add(centre + 1 + (i + 1) % outline.Count);
                tops.Add(centre + 1 + i);
            }

            cardsBelow += cards;
        }

        mesh.subMeshCount = 2;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(sides, 0);
        mesh.SetTriangles(tops, 1);
        FixWinding(mesh, 0, vertices, normals);
        FixWinding(mesh, 1, vertices, normals);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }

    // Makes every triangle in the submesh face along its vertex normal (the outline's direction isn't assumed).
    static void FixWinding(Mesh mesh, int submesh, List<Vector3> vertices, List<Vector3> normals)
    {
        int[] tris = mesh.GetTriangles(submesh);
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = vertices[tris[i]], b = vertices[tris[i + 1]], c = vertices[tris[i + 2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normals[tris[i]]) < 0f)
                (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
        }
        mesh.SetTriangles(tris, submesh);
    }

    // Rounded rectangle, counter-clockwise, centred on the origin.
    List<Vector2> BuildOutline()
    {
        float hw = CardWidth * 0.5f, hh = CardHeight * 0.5f;
        float r = Mathf.Clamp(CornerRadius, 0.001f, Mathf.Min(hw, hh));
        var points = new List<Vector2>();
        Vector2[] centres =
        {
            new Vector2(hw - r, hh - r), new Vector2(-hw + r, hh - r),
            new Vector2(-hw + r, -hh + r), new Vector2(hw - r, -hh + r),
        };
        for (int corner = 0; corner < 4; corner++)
        {
            for (int s = 0; s <= CornerSegments; s++)
            {
                float a = (corner * 90f + s * 90f / CornerSegments) * Mathf.Deg2Rad;
                points.Add(centres[corner] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
        }
        return points;
    }
}
