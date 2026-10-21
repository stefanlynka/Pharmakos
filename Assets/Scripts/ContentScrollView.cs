using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

public class ContentScrollView : MonoBehaviour
{
    public static ContentScrollView Instance { get; private set; }
    static ContentScrollView _selectable;
    public static bool IsActive =>
        (Instance != null && Instance._isActive) || (_selectable != null && _selectable._isActive);
    public static bool CapturesScrollWheel => IsActive;
    public static bool BlocksGameInput => IsActive;

    [Tooltip("Second copy used to pick a card. It does not replace the deck viewer.")]
    public bool IsSelectableCopy;

    const string ContentLayerName = "DeckViewer";
    const string RitualPrefabPath = "Prefabs/View/ViewRitual3";
    const string TrinketPrefabPath = "Prefabs/View/ViewBuff";

    static readonly Quaternion FlatRotation = Quaternion.Euler(90f, 0f, 0f);
    static readonly Vector3[] RegionCorners = new Vector3[4];

    [Header("Capture")]
    public Camera ScrollCamera;
    public Transform ContentContainer;
    [Tooltip("Legacy UI canvas previously used for play history capture. Play history now renders in ContentContainer.")]
    public RectTransform CaptureCanvas;

    [Header("Layout")]
    public ContentScrollLayout CardLayout = new ContentScrollLayout
    {
        ItemWidth = 5f,
        ItemDepth = 6.84f,
        ItemMargin = 0.5f,
        ItemScale = 0.9f,
    };
    public ContentScrollLayout PlayHistoryLayout = new ContentScrollLayout
    {
        ItemWidth = 58f,
        ItemDepth = 10f,
        ItemMargin = 1f,
        ItemScale = 1f,
        Columns = 1,
    };

    [Header("Side Sections")]
    [Tooltip("Rituals and trinkets each get their own camera, render texture, and copy of this area's backdrop, built this far apart so their cameras and lights don't reach each other.")]
    public float SectionSpacing = 400f;
    public ContentScrollSectionLayout RitualLayout = new ContentScrollSectionLayout
    {
        ItemScale = 0.55f,
        Columns = 2,
        ItemMargin = 1.5f,
        ViewPaddingX = 1.5f,
        ViewPaddingY = 1.5f,
    };
    [Tooltip("UI pixels per world unit in the ritual section. 0 zooms so the layout's columns fill the section.")]
    public float RitualPixelsPerUnit = 0f;
    public ContentScrollSectionLayout TrinketLayout = new ContentScrollSectionLayout
    {
        ItemScale = 2.5f,
        Columns = 2,
        ItemMargin = 2f,
        ViewPaddingX = 2f,
        ViewPaddingY = 2f,
    };
    [Tooltip("UI pixels per world unit in the trinket section. 0 zooms so the layout's columns fill the section.")]
    public float TrinketPixelsPerUnit = 6.3f;
    [Tooltip("Size of a trinket's icon relative to its summary, which stays at the layout's Item Scale.")]
    public float TrinketIconScale = 2f;
    [Tooltip("World units between a trinket's icon and the summary beside it.")]
    public float TrinketSummaryGap = 1f;
    [Tooltip("Local Z of each trinket's Summary object.")]
    public float TrinketSummaryZ = -0.2f;
    [Tooltip("Local Z of the trinket section's copy of the Carpets backdrop.")]
    public float TrinketCarpetZ = 5f;

    [Header("Scroll")]
    public float ScrollIncrement = 5f;
    public float ScrollSpeed = 40f;
    [Tooltip("Rows a side section scrolls per mouse wheel step.")]
    public float SideScrollRows = 0.5f;
    [Tooltip("Rows per second a side section scrolls at.")]
    public float SideScrollSpeed = 4f;

    // One scrolling view: a camera looking down at a container of items laid out in a grid. Scrolling moves the camera.
    sealed class Section
    {
        public GameObject Root;
        public Camera Camera;
        public Transform Container;
        public RenderTexture Texture;
        // Width of the image the section renders into, in UI units
        public float ImageWidth;
        // The mouse wheel only scrolls this section while over this rect; null scrolls it anywhere
        public RectTransform ScrollRegion;
        public bool Shown;

        public float CameraBaseZ;
        public float BaseFieldOfView;
        public float ViewCenterZ;
        public float ViewDepth;
        public int Columns = 1;
        public Vector2 CellSize;
        public float Margin;
        public float ScrollIncrement;
        public float ScrollSpeed;

        public float CurrentScrollOffset;
        public float DesiredScrollOffset;
        public float MaxScrollOffset;

        public readonly List<Transform> Items = new List<Transform>();
        // From each item's origin to the centre of its footprint, in container space
        public readonly List<Vector3> ItemOffsets = new List<Vector3>();

        public void ClearItems()
        {
            Items.Clear();
            ItemOffsets.Clear();
        }
    }

    ContentScrollLayout _activeLayout;
    bool _isActive;

    readonly Section _mainSection = new Section();
    Section _ritualSection;
    Section _trinketSection;

    readonly List<ViewCard> _viewCards = new List<ViewCard>();
    readonly List<ViewPlayHistoryItemNew> _playHistoryItems = new List<ViewPlayHistoryItemNew>();
    readonly List<ViewRitual> _ritualViews = new List<ViewRitual>();
    readonly List<ViewBuff> _trinketViews = new List<ViewBuff>();
    readonly List<Trinket> _shownTrinkets = new List<Trinket>();
    ObjectPool<GameObject> _playHistoryItemPool;
    static GameObject _playHistoryItemPrefab;
    static GameObject _ritualPrefab;
    static GameObject _trinketPrefab;
    int _contentLayer = -1;
    bool _selectionEnabled;
    Card _selectedCard;
    readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    public Card SelectedCard => _selectedCard;

    void Awake()
    {
        AutoResolveReferences();

        _mainSection.Camera = ScrollCamera;
        _mainSection.Container = ContentContainer;
        if (ScrollCamera != null)
            _mainSection.CameraBaseZ = ScrollCamera.transform.localPosition.z;

        _contentLayer = LayerMask.NameToLayer(ContentLayerName);

        if (IsSelectableCopy)
        {
            _selectable = this;
            gameObject.SetActive(true);
            SetCaptureActive(false);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.SetActive(true);
        SetCaptureActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (_selectable == this)
            _selectable = null;

        ReleaseTexture(_ritualSection);
        ReleaseTexture(_trinketSection);
    }

    void Update()
    {
        if (!_isActive)
            return;

        UpdateSection(_mainSection);
        UpdateSection(_ritualSection);
        UpdateSection(_trinketSection);

        if (_selectionEnabled && Input.GetMouseButtonDown(0) && !IsPointerOverButton())
            ToggleCardUnderMouse();
    }

    /// <summary>Shows the deck so one card can be selected. Clicking the selected card clears it.</summary>
    public void ShowSelectableCards(List<Card> cards, RectTransform scrollRegion)
    {
        _selectionEnabled = true;
        _selectedCard = null;
        ShowCardsInternal(cards, scrollRegion);
    }

    public void HideView()
    {
        _selectionEnabled = false;
        _selectedCard = null;
        HideInternal();
    }

    /// <param name="scrollRegion">If set, the mouse wheel only scrolls the cards while the mouse is over it.</param>
    public static void ShowCards(List<Card> cards, RectTransform scrollRegion = null) =>
        Instance?.ShowCardsInternal(cards, scrollRegion);

    public static void ShowPlayHistory(List<PlayHistoryItem> items) =>
        Instance?.ShowPlayHistoryInternal(items);

    /// <summary>
    /// Shows the rituals in their own section, rendered into <paramref name="target"/> and scrolled while the mouse is
    /// over it. Side sections accompany the main content: show the cards first; hiding or replacing them clears these.
    /// </summary>
    public static void ShowRituals(List<Ritual> rituals, RawImage target) =>
        Instance?.ShowRitualsInternal(rituals, target);

    /// <summary>Shows the trinkets in their own section, like <see cref="ShowRituals"/>.</summary>
    public static void ShowTrinkets(List<Trinket> trinkets, RawImage target) =>
        Instance?.ShowTrinketsInternal(trinkets, target);

    public static Ritual GetRitualUnderMouse() => Instance?.RitualUnderMouse();

    public static Trinket GetTrinketUnderMouse() => Instance?.TrinketUnderMouse();

    /// <summary>Highlights the shown ritual matching <paramref name="ritual"/>, or none if null.</summary>
    public static void HighlightRitual(Ritual ritual) => Instance?.HighlightRitualInternal(ritual);

    /// <summary>Highlights the shown trinket matching <paramref name="trinket"/>, or none if null.</summary>
    public static void HighlightTrinket(Trinket trinket) => Instance?.HighlightTrinketInternal(trinket);

    public static void Hide() => Instance?.HideInternal();

    public static void Clear() => Instance?.ClearInternal();

    void ShowCardsInternal(List<Card> cards, RectTransform scrollRegion)
    {
        ClearInternal();
        _activeLayout = CardLayout;
        _mainSection.ScrollRegion = scrollRegion;
        SetCaptureActive(true);
        CreateCardViews(cards ?? new List<Card>());
        ResetScroll(_mainSection);
        LayoutContent();
    }

    void ShowPlayHistoryInternal(List<PlayHistoryItem> items)
    {
        ClearInternal();
        _activeLayout = PlayHistoryLayout;
        _mainSection.ScrollRegion = null;
        SetCaptureActive(true);
        CreatePlayHistoryViews(items ?? new List<PlayHistoryItem>());
        ResetScroll(_mainSection);
        LayoutContent();
    }

    void ShowRitualsInternal(List<Ritual> rituals, RawImage target)
    {
        ClearRitualViews();
        Section section = BeginSideSection(ref _ritualSection, "RitualSection", 1, target);
        if (section == null)
            return;

        GameObject prefab = LoadPrefab(ref _ritualPrefab, RitualPrefabPath);
        if (prefab != null && rituals != null)
        {
            foreach (Ritual ritual in rituals)
            {
                if (ritual == null)
                    continue;

                GameObject ritualObject = Instantiate(prefab, section.Container, false);
                ritualObject.name = "Ritual_" + ritual.Name;
                ViewRitual view = ritualObject.GetComponent<ViewRitual>();
                view.SetDisplayScale(Vector3.one * RitualLayout.ItemScale);
                view.Init(ritual, false);
                view.SetHighlight(false);

                _ritualViews.Add(view);
                AddSideItem(section, ritualObject.transform);
            }
        }

        FinishSideSection(section, RitualLayout, RitualPixelsPerUnit);
    }

    void ShowTrinketsInternal(List<Trinket> trinkets, RawImage target)
    {
        ClearTrinketViews();
        Section section = BeginSideSection(ref _trinketSection, "TrinketSection", 2, target);
        if (section == null)
            return;

        Transform carpets = section.Root.transform.Find("Carpets");
        if (carpets != null)
        {
            Vector3 carpetPosition = carpets.localPosition;
            carpetPosition.z = TrinketCarpetZ;
            carpets.localPosition = carpetPosition;
        }

        GameObject prefab = LoadPrefab(ref _trinketPrefab, TrinketPrefabPath);
        if (prefab != null && trinkets != null)
        {
            foreach (Trinket trinket in trinkets)
            {
                PlayerEffectDescriptionData descriptionData = trinket != null ? trinket.GetDescriptionData() : null;
                if (descriptionData == null)
                    continue;

                GameObject trinketObject = Instantiate(prefab, section.Container, false);
                trinketObject.name = "Trinket_" + trinket.Name;
                trinketObject.transform.localScale = Vector3.one * (TrinketLayout.ItemScale * TrinketIconScale);
                ViewBuff view = trinketObject.GetComponent<ViewBuff>();
                view.SetBuffData(trinket.MyEffect, descriptionData);
                view.SetVisible(true);
                // Hover popups can't reach into a render texture, so the description is always shown, like the cards
                view.SetSummaryForced(true);
                PlaceSummaryBesideIcon(view);

                _trinketViews.Add(view);
                _shownTrinkets.Add(trinket);
                AddSideItem(section, trinketObject.transform);
            }
        }

        FinishSideSection(section, TrinketLayout, TrinketPixelsPerUnit);
    }

    // Moves the summary from above the icon to its right, scaled back down so its text keeps the layout's Item Scale
    void PlaceSummaryBesideIcon(ViewBuff view)
    {
        if (view.SummaryObject == null)
            return;

        Transform root = view.transform;
        Transform summary = view.SummaryObject.transform;
        summary.localScale = Vector3.one / Mathf.Max(0.01f, TrinketIconScale);

        Bounds icon = MeasureLocalBounds(root, root, summary);
        Bounds box = MeasureLocalBounds(summary, root);
        float gap = TrinketSummaryGap / Mathf.Max(0.01f, root.localScale.x);
        Vector3 summaryPosition = summary.localPosition + new Vector3(icon.max.x + gap - box.min.x, icon.center.y - box.center.y, 0f);
        summaryPosition.z = TrinketSummaryZ;
        summary.localPosition = summaryPosition;
    }

    void HideInternal()
    {
        ClearInternal();
        SetCaptureActive(false);
        _isActive = false;
    }

    void ClearInternal()
    {
        ClearCardViews();
        ClearPlayHistoryViews();
        _mainSection.ClearItems();
        HideSideSections();
    }

    void SetCaptureActive(bool active)
    {
        _isActive = active;
        _mainSection.Shown = active;

        if (ScrollCamera != null)
            ScrollCamera.enabled = active;

        if (active)
            DismissHoverPopups();
    }

    static void DismissHoverPopups()
    {
        PopupScreenHandler.Instance?.HideTextPopup();
        PopupHandler.ClearAll();
    }

    void AutoResolveReferences()
    {
        if (ScrollCamera == null)
            ScrollCamera = GetComponentInChildren<Camera>(true);

        if (ContentContainer == null)
        {
            Transform container = transform.Find("CardContainer");
            if (container != null)
                ContentContainer = container;
        }

        if (CaptureCanvas == null)
        {
            Transform captureCanvas = transform.Find("CaptureCanvas");
            if (captureCanvas != null)
                CaptureCanvas = captureCanvas as RectTransform;
        }

        BindScreenRenderTextures();
    }

    static void BindScreenRenderTextures()
    {
        if (ScreenHandler.Instance == null)
            return;

        BindScreenRenderTexture(ScreenName.DeckViewerScreen);
        BindScreenRenderTexture(ScreenName.PlayHistoryScreen);
        BindScreenRenderTexture(ScreenName.SelectableDeckViewerScreen);
    }

    static void BindScreenRenderTexture(ScreenName screenName)
    {
        if (!ScreenHandler.Instance.TryGetScreen(screenName, out Screen screen))
            return;

        ContentScrollScreenHandler handler = screen.GetComponent<ContentScrollScreenHandler>();
        handler?.BindRenderTexture();
    }

    void CreateCardViews(List<Card> cards)
    {
        if (ContentContainer == null || View.Instance == null)
            return;

        foreach (Card card in cards)
        {
            ViewCard viewCard = View.Instance.MakeNewViewCard(card, addToCardMap: false);
            if (viewCard == null)
                continue;

            viewCard.transform.SetParent(ContentContainer, false);
            viewCard.transform.localRotation = FlatRotation;
            viewCard.SetHighlight(false);
            viewCard.SetDescriptiveMode(true);
            if (viewCard.CardCollider != null)
                viewCard.CardCollider.enabled = false;

            SetLayerRecursively(viewCard.gameObject, _contentLayer);
            _viewCards.Add(viewCard);
            _mainSection.Items.Add(viewCard.transform);
            _mainSection.ItemOffsets.Add(Vector3.zero);
        }

        foreach (ViewCard viewCard in _viewCards)
            ApplyItemScale(viewCard.transform, _activeLayout.ItemScale);
    }

    void CreatePlayHistoryViews(List<PlayHistoryItem> playHistoryItems)
    {
        if (ContentContainer == null)
        {
            Debug.LogError("ContentScrollView: ContentContainer is not assigned.");
            return;
        }

        if (_playHistoryItemPool == null)
        {
            _playHistoryItemPool = new ObjectPool<GameObject>(
                CreatePlayHistoryItem,
                OnPlayHistoryItemGet,
                OnPlayHistoryItemRelease,
                null,
                false);
        }

        foreach (PlayHistoryItem playHistoryItem in playHistoryItems)
        {
            GameObject itemObject = _playHistoryItemPool.Get();
            itemObject.transform.SetParent(ContentContainer, false);
            itemObject.transform.localRotation = Quaternion.identity;

            ViewPlayHistoryItemNew viewItem = itemObject.GetComponent<ViewPlayHistoryItemNew>();
            if (viewItem == null)
                continue;

            viewItem.Load(playHistoryItem);
            ApplyItemScale(itemObject.transform, _activeLayout.ItemScale);
            SetLayerRecursively(itemObject, _contentLayer);
            _playHistoryItems.Add(viewItem);
            _mainSection.Items.Add(itemObject.transform);
            _mainSection.ItemOffsets.Add(Vector3.zero);
        }
    }

    void ClearCardViews()
    {
        if (View.Instance == null)
        {
            _viewCards.Clear();
            return;
        }

        foreach (ViewCard viewCard in _viewCards)
            View.Instance.ReleaseCard(viewCard);

        _viewCards.Clear();
    }

    void ClearPlayHistoryViews()
    {
        if (_playHistoryItemPool == null)
        {
            _playHistoryItems.Clear();
            return;
        }

        foreach (ViewPlayHistoryItemNew viewItem in _playHistoryItems)
        {
            if (viewItem != null)
                viewItem.Clear();

            _playHistoryItemPool.Release(viewItem.gameObject);
        }

        _playHistoryItems.Clear();
    }

    void ClearRitualViews()
    {
        foreach (ViewRitual view in _ritualViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        _ritualViews.Clear();
        _ritualSection?.ClearItems();
    }

    void ClearTrinketViews()
    {
        foreach (ViewBuff view in _trinketViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        _trinketViews.Clear();
        _shownTrinkets.Clear();
        _trinketSection?.ClearItems();
    }

    void HideSideSections()
    {
        ClearRitualViews();
        ClearTrinketViews();
        HideSideSection(_ritualSection);
        HideSideSection(_trinketSection);
    }

    static void HideSideSection(Section section)
    {
        if (section == null)
            return;

        section.Shown = false;
        section.ScrollRegion = null;
        if (section.Root != null)
            section.Root.SetActive(false);
    }

    void LayoutContent()
    {
        if (_activeLayout == null)
            return;

        FrameMainSection(_mainSection, _activeLayout);
        _mainSection.CellSize = new Vector2(_activeLayout.ItemWidth, _activeLayout.ItemDepth);
        _mainSection.Margin = _activeLayout.ItemMargin;
        _mainSection.ScrollIncrement = ScrollIncrement;
        _mainSection.ScrollSpeed = ScrollSpeed;
        LayoutSection(_mainSection);
    }

    // Visible width/depth and centre come from the capture camera so layout and scroll stop match what it shows.
    void FrameMainSection(Section section, ContentScrollLayout layout)
    {
        if (section.Camera == null || section.Container == null)
        {
            section.ViewCenterZ = 0f;
            section.ViewDepth = 0f;
            section.Columns = 1;
            return;
        }

        section.ViewCenterZ = section.CameraBaseZ - section.Container.localPosition.z;
        GetCameraViewSize(
            section.Camera,
            section.Container,
            layout.ViewPaddingX,
            layout.ViewPaddingY,
            out float viewWidth,
            out float viewDepth);
        section.ViewDepth = viewDepth;
        section.Columns = layout.GetColumnCount(section.Items.Count, viewWidth);
    }

    // World-space size of the camera's view on the content plane, inset by horizontal and vertical padding.
    static void GetCameraViewSize(
        Camera camera,
        Transform content,
        float paddingX,
        float paddingY,
        out float viewWidth,
        out float viewDepth)
    {
        float aspect = Mathf.Max(0.01f, camera.aspect);
        float distance = Mathf.Max(0.01f, Vector3.Dot(
            content.position - camera.transform.position, camera.transform.forward));

        if (camera.orthographic)
            viewDepth = camera.orthographicSize * 2f;
        else
            viewDepth = 2f * distance * Mathf.Tan(0.5f * camera.fieldOfView * Mathf.Deg2Rad);

        viewWidth = viewDepth * aspect;
        viewDepth = Mathf.Max(0f, viewDepth - paddingY * 2f);
        viewWidth = Mathf.Max(0f, viewWidth - paddingX * 2f);
    }

    // Lays the items out in rows from the top of the section's view, centred horizontally on the container
    static void LayoutSection(Section section)
    {
        int itemCount = section.Items.Count;
        if (itemCount == 0)
        {
            section.MaxScrollOffset = 0f;
            ApplyCameraScroll(section);
            return;
        }

        int columns = Mathf.Max(1, section.Columns);
        int rows = Mathf.CeilToInt(itemCount / (float)columns);
        Vector2 cell = section.CellSize;
        float margin = section.Margin;
        float rowStride = cell.y + margin;
        float colStride = cell.x + margin;
        float totalWidth = columns * cell.x + Mathf.Max(0, columns - 1) * margin;
        float totalDepth = rows * cell.y + Mathf.Max(0, rows - 1) * margin;
        float startX = -totalWidth * 0.5f + cell.x * 0.5f;
        float startZ = section.ViewCenterZ + (section.ViewDepth - cell.y) * 0.5f;

        section.MaxScrollOffset = Mathf.Max(0f, totalDepth - section.ViewDepth);

        for (int i = 0; i < itemCount; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Vector3 cellCenter = new Vector3(startX + col * colStride, 0f, startZ - row * rowStride);
            section.Items[i].localPosition = cellCenter - section.ItemOffsets[i];
        }

        ApplyCameraScroll(section);
    }

    Section BeginSideSection(ref Section section, string name, int index, RawImage target)
    {
        if (target == null)
            return null;

        if (!_isActive)
        {
            Debug.LogWarning("ContentScrollView: show the cards before the " + name + "; side sections accompany them.");
            return null;
        }

        if (section == null)
            section = CreateSideSection(name, index);
        if (section == null)
            return null;

        BindSectionTexture(section, target);
        section.Root.SetActive(true);
        section.Shown = true;
        section.CellSize = Vector2.zero;
        return section;
    }

    // A copy of this area translated along X: the backdrop, a camera like ScrollCamera, and an empty container
    Section CreateSideSection(string name, int index)
    {
        if (ScrollCamera == null || ContentContainer == null)
            return null;

        List<Transform> backdrop = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (IsBackdrop(child))
                backdrop.Add(child);
        }

        GameObject root = new GameObject(name);
        root.SetActive(false);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.right * (SectionSpacing * index);

        foreach (Transform source in backdrop)
            Instantiate(source.gameObject, root.transform, false).name = source.name;

        Camera sectionCamera = Instantiate(ScrollCamera, root.transform, false);
        sectionCamera.name = ScrollCamera.name;
        sectionCamera.tag = "Untagged";
        AudioListener listener = sectionCamera.GetComponent<AudioListener>();
        if (listener != null)
            DestroyImmediate(listener);
        sectionCamera.enabled = true;

        Vector3 cameraPosition = ScrollCamera.transform.localPosition;
        cameraPosition.z = _mainSection.CameraBaseZ;
        sectionCamera.transform.localPosition = cameraPosition;

        Transform container = new GameObject("Container").transform;
        container.SetParent(root.transform, false);
        container.localPosition = ContentContainer.localPosition;
        container.localRotation = ContentContainer.localRotation;

        return new Section
        {
            Root = root,
            Camera = sectionCamera,
            Container = container,
            CameraBaseZ = cameraPosition.z,
            BaseFieldOfView = sectionCamera.fieldOfView,
        };
    }

    bool IsBackdrop(Transform child)
    {
        if (ScrollCamera != null && child == ScrollCamera.transform)
            return false;
        if (child == ContentContainer || child == CaptureCanvas)
            return false;

        return !IsSectionRoot(_ritualSection, child) && !IsSectionRoot(_trinketSection, child);
    }

    static bool IsSectionRoot(Section section, Transform child) =>
        section != null && section.Root != null && section.Root.transform == child;

    void BindSectionTexture(Section section, RawImage target)
    {
        Rect screenRect = GetScreenRect(target.rectTransform);
        int width = Mathf.Max(16, Mathf.RoundToInt(screenRect.width));
        int height = Mathf.Max(16, Mathf.RoundToInt(screenRect.height));

        if (section.Texture == null || section.Texture.width != width || section.Texture.height != height)
        {
            ReleaseTexture(section);
            section.Texture = CreateSectionTexture(width, height);
            section.Texture.name = section.Root.name;
        }

        section.Camera.targetTexture = section.Texture;
        section.Camera.aspect = width / (float)height;
        section.ImageWidth = target.rectTransform.rect.width;
        target.texture = section.Texture;
        section.ScrollRegion = target.rectTransform;
    }

    RenderTexture CreateSectionTexture(int width, int height)
    {
        RenderTexture source = ScrollCamera.targetTexture;
        if (source == null)
            return new RenderTexture(width, height, 24);

        RenderTextureDescriptor descriptor = source.descriptor;
        descriptor.width = width;
        descriptor.height = height;
        return new RenderTexture(descriptor);
    }

    static void ReleaseTexture(Section section)
    {
        if (section == null || section.Texture == null)
            return;

        if (section.Camera != null)
            section.Camera.targetTexture = null;

        section.Texture.Release();
        Destroy(section.Texture);
        section.Texture = null;
    }

    void AddSideItem(Section section, Transform item)
    {
        item.localPosition = Vector3.zero;
        item.localRotation = FlatRotation;
        SetLayerRecursively(item.gameObject, _contentLayer);

        Rect footprint = MeasureFootprint(item, section.Container);

        // Only this section's camera sees the item, so nothing should be able to hover or click it directly
        foreach (Collider itemCollider in item.GetComponentsInChildren<Collider>(true))
            itemCollider.enabled = false;
        foreach (Collider2D itemCollider in item.GetComponentsInChildren<Collider2D>(true))
            itemCollider.enabled = false;

        section.Items.Add(item);
        section.ItemOffsets.Add(new Vector3(footprint.center.x, 0f, footprint.center.y));
        section.CellSize = Vector2.Max(section.CellSize, footprint.size);
    }

    void FinishSideSection(Section section, ContentScrollSectionLayout layout, float pixelsPerUnit)
    {
        section.Margin = layout.ItemMargin;
        FrameSideSection(section, layout, pixelsPerUnit);

        float rowStride = section.CellSize.y + section.Margin;
        section.ScrollIncrement = rowStride * SideScrollRows;
        section.ScrollSpeed = rowStride * SideScrollSpeed;

        ResetScroll(section);
        LayoutSection(section);
    }

    // Sets the camera's field of view either to show pixelsPerUnit UI pixels per world unit, fitting as many columns
    // as there's room for (up to the layout's), or, at 0, so the layout's columns fill the image. Either way it zooms
    // out if a single item wouldn't fit.
    static void FrameSideSection(Section section, ContentScrollSectionLayout layout, float pixelsPerUnit)
    {
        Camera sectionCamera = section.Camera;
        section.ViewCenterZ = section.CameraBaseZ - section.Container.localPosition.z;
        section.Columns = Mathf.Max(1, layout.Columns);

        if (section.Items.Count == 0)
        {
            sectionCamera.fieldOfView = section.BaseFieldOfView;
            section.ViewDepth = 0f;
            return;
        }

        float paddingX = layout.ViewPaddingX;
        float paddingY = layout.ViewPaddingY;
        float aspect = Mathf.Max(0.01f, sectionCamera.aspect);
        float distance = Mathf.Max(0.01f, Vector3.Dot(
            section.Container.position - sectionCamera.transform.position, sectionCamera.transform.forward));

        float viewWidth;
        if (pixelsPerUnit > 0f)
        {
            viewWidth = section.ImageWidth / pixelsPerUnit;
            float columnsThatFit = (viewWidth - paddingX * 2f + section.Margin) / (section.CellSize.x + section.Margin);
            section.Columns = Mathf.Clamp(Mathf.FloorToInt(columnsThatFit + 0.001f), 1, section.Columns);
        }
        else
        {
            viewWidth = section.Columns * section.CellSize.x + (section.Columns - 1) * section.Margin + paddingX * 2f;
        }

        viewWidth = Mathf.Max(
            viewWidth,
            section.CellSize.x + paddingX * 2f,
            (section.CellSize.y + paddingY * 2f) * aspect);
        float viewDepth = viewWidth / aspect;

        sectionCamera.fieldOfView = 2f * Mathf.Atan(viewDepth * 0.5f / distance) * Mathf.Rad2Deg;
        section.ViewDepth = viewDepth - paddingY * 2f;
    }

    // The item's extent on the container's XZ plane
    static Rect MeasureFootprint(Transform item, Transform space)
    {
        Bounds bounds = MeasureLocalBounds(item, space);
        return Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
    }

    // The extent of the target's visible renderers and box colliders in space's local axes, skipping anything under exclude
    static Bounds MeasureLocalBounds(Transform target, Transform space, Transform exclude = null)
    {
        Bounds bounds = default;
        bool found = false;

        foreach (Renderer targetRenderer in target.GetComponentsInChildren<Renderer>())
        {
            if (!targetRenderer.enabled || targetRenderer is ParticleSystemRenderer)
                continue;
            if (exclude != null && targetRenderer.transform.IsChildOf(exclude))
                continue;
            // Text meshes may not be built yet; the backgrounds behind them are measured instead
            if (targetRenderer.GetComponent<TMP_Text>() != null || targetRenderer.GetComponent<TMP_SubMesh>() != null)
                continue;

            Bounds rendererBounds = targetRenderer.bounds;
            if (rendererBounds.size.sqrMagnitude <= 0f)
                continue;

            EncapsulateBox(space, Matrix4x4.identity, rendererBounds.center, rendererBounds.size, ref bounds, ref found);
        }

        foreach (BoxCollider box in target.GetComponentsInChildren<BoxCollider>())
        {
            if (exclude != null && box.transform.IsChildOf(exclude))
                continue;

            EncapsulateBox(space, box.transform.localToWorldMatrix, box.center, box.size, ref bounds, ref found);
        }

        return found ? bounds : new Bounds(Vector3.zero, Vector3.one);
    }

    static void EncapsulateBox(Transform space, Matrix4x4 toWorld, Vector3 center, Vector3 size, ref Bounds bounds, ref bool found)
    {
        Vector3 half = size * 0.5f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = center + new Vector3(
                (i & 1) == 0 ? -half.x : half.x,
                (i & 2) == 0 ? -half.y : half.y,
                (i & 4) == 0 ? -half.z : half.z);
            Vector3 local = space.InverseTransformPoint(toWorld.MultiplyPoint3x4(corner));

            if (found)
            {
                bounds.Encapsulate(local);
            }
            else
            {
                bounds = new Bounds(local, Vector3.zero);
                found = true;
            }
        }
    }

    Ritual RitualUnderMouse()
    {
        int index = GetItemIndexUnderMouse(_ritualSection);
        return index >= 0 && index < _ritualViews.Count ? _ritualViews[index].Ritual : null;
    }

    Trinket TrinketUnderMouse()
    {
        int index = GetItemIndexUnderMouse(_trinketSection);
        return index >= 0 && index < _shownTrinkets.Count ? _shownTrinkets[index] : null;
    }

    int GetItemIndexUnderMouse(Section section)
    {
        if (!_isActive || section == null || !section.Shown)
            return -1;
        if (!TryGetMouseViewport(section.ScrollRegion, out Vector2 viewport))
            return -1;

        Ray ray = section.Camera.ViewportPointToRay(viewport);
        Plane plane = new Plane(section.Container.up, section.Container.position);
        if (!plane.Raycast(ray, out float distance))
            return -1;

        Vector3 point = section.Container.InverseTransformPoint(ray.GetPoint(distance));
        Vector2 half = section.CellSize * 0.5f;
        for (int i = 0; i < section.Items.Count; i++)
        {
            Vector3 center = section.Items[i].localPosition + section.ItemOffsets[i];
            if (Mathf.Abs(point.x - center.x) <= half.x && Mathf.Abs(point.z - center.z) <= half.y)
                return i;
        }

        return -1;
    }

    void ToggleCardUnderMouse()
    {
        int index = GetItemIndexUnderMouse(_mainSection);
        if (index < 0 || index >= _viewCards.Count)
            return;

        Card card = _viewCards[index].Card;
        _selectedCard = _selectedCard == card ? null : card;

        foreach (ViewCard viewCard in _viewCards)
        {
            if (viewCard != null)
                viewCard.SetHighlight(viewCard.Card == _selectedCard);
        }
    }

    bool IsPointerOverButton()
    {
        if (EventSystem.current == null)
            return false;

        _uiHits.Clear();
        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        EventSystem.current.RaycastAll(pointer, _uiHits);
        if (_uiHits.Count == 0)
            return false;

        return _uiHits[0].gameObject.GetComponentInParent<Button>() != null;
    }

    void HighlightRitualInternal(Ritual ritual)
    {
        foreach (ViewRitual view in _ritualViews)
        {
            if (view != null)
                view.SetHighlight(ritual != null && view.Ritual == ritual);
        }
    }

    void HighlightTrinketInternal(Trinket trinket)
    {
        for (int i = 0; i < _trinketViews.Count; i++)
        {
            if (_trinketViews[i] != null)
                _trinketViews[i].SetHighlight(trinket != null && _shownTrinkets[i] == trinket);
        }
    }

    static bool TryGetMouseViewport(RectTransform region, out Vector2 viewport)
    {
        viewport = Vector2.zero;
        if (region == null || !region.gameObject.activeInHierarchy)
            return false;

        Rect screenRect = GetScreenRect(region);
        if (screenRect.width <= 0f || screenRect.height <= 0f)
            return false;

        Vector2 mouse = Input.mousePosition;
        viewport = new Vector2((mouse.x - screenRect.xMin) / screenRect.width, (mouse.y - screenRect.yMin) / screenRect.height);
        return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }

    static Rect GetScreenRect(RectTransform rect)
    {
        rect.GetWorldCorners(RegionCorners);
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, RegionCorners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, RegionCorners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static void UpdateSection(Section section)
    {
        if (section == null || !section.Shown)
            return;

        HandleScrollInput(section);
        ApplyScroll(section);
    }

    static void ResetScroll(Section section)
    {
        section.CurrentScrollOffset = 0f;
        section.DesiredScrollOffset = 0f;
        section.MaxScrollOffset = 0f;
        ApplyCameraScroll(section);
    }

    static void HandleScrollInput(Section section)
    {
        float scrollVector = Input.mouseScrollDelta.y;
        if (scrollVector == 0f)
            return;

        if (section.ScrollRegion != null && !TryGetMouseViewport(section.ScrollRegion, out _))
            return;

        if (scrollVector < 0f)
            section.DesiredScrollOffset += section.ScrollIncrement;
        else
            section.DesiredScrollOffset -= section.ScrollIncrement;

        section.DesiredScrollOffset = Mathf.Clamp(section.DesiredScrollOffset, 0f, section.MaxScrollOffset);
    }

    static void ApplyScroll(Section section)
    {
        if (Mathf.Approximately(section.CurrentScrollOffset, section.DesiredScrollOffset))
            return;

        float direction = section.DesiredScrollOffset > section.CurrentScrollOffset ? 1f : -1f;
        float delta = direction * Time.deltaTime * section.ScrollSpeed;
        if (Mathf.Abs(delta) > Mathf.Abs(section.DesiredScrollOffset - section.CurrentScrollOffset))
            section.CurrentScrollOffset = section.DesiredScrollOffset;
        else
            section.CurrentScrollOffset += delta;

        ApplyCameraScroll(section);
    }

    static void ApplyCameraScroll(Section section)
    {
        if (section.Camera == null)
            return;

        Transform cameraTransform = section.Camera.transform;
        Vector3 localPosition = cameraTransform.localPosition;
        localPosition.z = section.CameraBaseZ - section.CurrentScrollOffset;
        cameraTransform.localPosition = localPosition;
    }

    static void ApplyItemScale(Transform target, float scale)
    {
        target.localScale = new Vector3(scale, scale, 1f);
    }

    static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    static GameObject LoadPrefab(ref GameObject cached, string resourcesPath)
    {
        if (cached == null)
            cached = Resources.Load<GameObject>(resourcesPath);

        if (cached == null)
            Debug.LogError("ContentScrollView: missing Resources/" + resourcesPath);

        return cached;
    }

    GameObject CreatePlayHistoryItem()
    {
        if (_playHistoryItemPrefab == null)
            _playHistoryItemPrefab = Resources.Load<GameObject>("Prefabs/PlayHistory/PlayHistoryItem");

        if (_playHistoryItemPrefab != null)
            return Instantiate(_playHistoryItemPrefab);

        Debug.LogError("ContentScrollView: could not load play history item prefab.");
        return null;
    }

    static void OnPlayHistoryItemGet(GameObject item)
    {
        item.SetActive(true);
        item.transform.localScale = Vector3.one;
    }

    static void OnPlayHistoryItemRelease(GameObject item)
    {
        ViewPlayHistoryItemNew viewItem = item.GetComponent<ViewPlayHistoryItemNew>();
        if (viewItem != null)
            viewItem.Clear();

        item.SetActive(false);
        item.transform.SetParent(null);
    }
}

[System.Serializable]
public class ContentScrollLayout
{
    public float ItemWidth = 5f;
    public float ItemDepth = 7f;
    public float ItemMargin = 0.5f;
    public float ItemScale = 1f;
    [Tooltip("Horizontal space kept between the items and the left/right edges of the camera view.")]
    public float ViewPaddingX = 0f;
    [Tooltip("Vertical space kept between the items and the top/bottom edges of the camera view.")]
    public float ViewPaddingY = 0f;
    [Tooltip("0 = auto-fit as many columns as the camera's visible width allows.")]
    public int Columns = 0;

    public int GetColumnCount(int itemCount, float viewWidth)
    {
        if (itemCount <= 0)
            return 1;

        if (Columns > 0)
            return Columns;

        int columns = 1;
        float widthUsed = ItemWidth;
        while (widthUsed + ItemMargin + ItemWidth <= viewWidth + 0.001f)
        {
            columns++;
            widthUsed += ItemMargin + ItemWidth;
        }

        return Mathf.Max(1, columns);
    }
}

/// <summary>
/// Layout for a side section (rituals, trinkets). Cell sizes are measured from the items; how far the section's camera
/// zooms is set by its pixels-per-unit on ContentScrollView.
/// </summary>
[System.Serializable]
public class ContentScrollSectionLayout
{
    public float ItemScale = 1f;
    [Tooltip("Most columns to lay out; fewer if they don't fit at the section's pixels per unit.")]
    [Min(1)] public int Columns = 2;
    public float ItemMargin = 1f;
    [Tooltip("Horizontal space kept between the items and the left/right edges of the section's image.")]
    public float ViewPaddingX = 1f;
    [Tooltip("Vertical space kept between the items and the top/bottom edges of the section's image.")]
    public float ViewPaddingY = 1f;
}
