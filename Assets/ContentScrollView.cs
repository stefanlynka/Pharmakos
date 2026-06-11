using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ContentScrollView : MonoBehaviour
{
    public static ContentScrollView Instance { get; private set; }
    public static bool IsActive => Instance != null && Instance._isActive;
    public static bool CapturesScrollWheel => IsActive;
    public static bool BlocksGameInput => IsActive;

    const string ContentLayerName = "DeckViewer";

    [Header("Capture")]
    public Camera ScrollCamera;
    public Transform ContentContainer;
    [Tooltip("Legacy UI canvas previously used for play history capture. Play history now renders in ContentContainer.")]
    public RectTransform CaptureCanvas;

    [Header("Layout")]
    public ContentScrollLayout CardLayout = new ContentScrollLayout
    {
        ViewWidth = 37.4f,
        ViewDepth = 34f,
        ItemWidth = 5f,
        ItemDepth = 6.84f,
        ItemMargin = 0.5f,
        ItemScale = 0.9f,
    };
    public ContentScrollLayout PlayHistoryLayout = new ContentScrollLayout
    {
        ViewWidth = 60f,
        ViewDepth = 34f,
        ItemWidth = 58f,
        ItemDepth = 10f,
        ItemMargin = 1f,
        ItemScale = 1f,
        Columns = 1,
    };

    [Header("Scroll")]
    public float ScrollIncrement = 5f;
    public float ScrollSpeed = 40f;

    ContentScrollMode _mode = ContentScrollMode.None;
    ContentScrollLayout _activeLayout;
    bool _isActive;

    float _cameraBaseZ;
    float _currentScrollOffset;
    float _desiredScrollOffset;
    float _maxScrollOffset;

    readonly List<ViewCard> _viewCards = new List<ViewCard>();
    readonly List<ViewPlayHistoryItemNew> _playHistoryItems = new List<ViewPlayHistoryItemNew>();
    ObjectPool<GameObject> _playHistoryItemPool;
    static GameObject _playHistoryItemPrefab;
    int _contentLayer = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoResolveReferences();

        if (ScrollCamera != null)
            _cameraBaseZ = ScrollCamera.transform.localPosition.z;

        _contentLayer = LayerMask.NameToLayer(ContentLayerName);
        gameObject.SetActive(true);
        SetCaptureActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!_isActive)
            return;

        HandleScrollInput();
        ApplyScroll();
    }

    public static void ShowCards(List<Card> cards) => Instance?.ShowCardsInternal(cards);

    public static void ShowPlayHistory(List<PlayHistoryItem> items) =>
        Instance?.ShowPlayHistoryInternal(items);

    public static void Hide() => Instance?.HideInternal();

    public static void Clear() => Instance?.ClearInternal();

    void ShowCardsInternal(List<Card> cards)
    {
        ClearInternal();
        _mode = ContentScrollMode.Cards;
        _activeLayout = CardLayout;
        SetCaptureActive(true);
        CreateCardViews(cards ?? new List<Card>());
        ResetScroll();
        LayoutContent();
    }

    void ShowPlayHistoryInternal(List<PlayHistoryItem> items)
    {
        ClearInternal();
        _mode = ContentScrollMode.PlayHistory;
        _activeLayout = PlayHistoryLayout;
        SetCaptureActive(true);
        CreatePlayHistoryViews(items ?? new List<PlayHistoryItem>());
        ResetScroll();
        LayoutContent();
    }

    void HideInternal()
    {
        ClearInternal();
        SetCaptureActive(false);
        _mode = ContentScrollMode.None;
        _isActive = false;
    }

    void ClearInternal()
    {
        ClearCardViews();
        ClearPlayHistoryViews();
    }

    void SetCaptureActive(bool active)
    {
        _isActive = active;

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
            viewCard.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            viewCard.SetHighlight(false);
            viewCard.SetDescriptiveMode(true);
            if (viewCard.CardCollider != null)
                viewCard.CardCollider.enabled = false;

            SetLayerRecursively(viewCard.gameObject, _contentLayer);
            _viewCards.Add(viewCard);
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

    void LayoutContent()
    {
        if (_activeLayout == null)
            return;

        int itemCount = _mode == ContentScrollMode.Cards ? _viewCards.Count : _playHistoryItems.Count;
        if (itemCount == 0)
        {
            _maxScrollOffset = 0f;
            ApplyCameraScroll();
            return;
        }

        int columns = _activeLayout.GetColumnCount(itemCount);
        int rows = Mathf.CeilToInt(itemCount / (float)columns);
        float rowStride = _activeLayout.ItemDepth + _activeLayout.ItemMargin;
        float colStride = _activeLayout.ItemWidth + _activeLayout.ItemMargin;
        float totalWidth = columns * _activeLayout.ItemWidth + Mathf.Max(0, columns - 1) * _activeLayout.ItemMargin;
        float totalDepth = rows * _activeLayout.ItemDepth + Mathf.Max(0, rows - 1) * _activeLayout.ItemMargin;
        float startX = -totalWidth * 0.5f + _activeLayout.ItemWidth * 0.5f;
        float startZ = (_activeLayout.ViewDepth - _activeLayout.ItemDepth) * 0.5f;

        _maxScrollOffset = Mathf.Max(0f, totalDepth - _activeLayout.ViewDepth);

        for (int i = 0; i < itemCount; i++)
        {
            int col = i % columns;
            int row = i / columns;
            float x = startX + col * colStride;
            float z = startZ - row * rowStride;

            if (_mode == ContentScrollMode.Cards)
            {
                ViewCard viewCard = _viewCards[i];
                viewCard.transform.localPosition = new Vector3(x, 0f, z);
            }
            else
            {
                ViewPlayHistoryItemNew viewItem = _playHistoryItems[i];
                viewItem.transform.localPosition = new Vector3(x, 0f, z);
            }
        }

        ApplyCameraScroll();
    }

    void ResetScroll()
    {
        _currentScrollOffset = 0f;
        _desiredScrollOffset = 0f;
        _maxScrollOffset = 0f;
        ApplyCameraScroll();
    }

    void HandleScrollInput()
    {
        float scrollVector = Input.mouseScrollDelta.y;
        if (scrollVector == 0f)
            return;

        if (scrollVector < 0f)
            _desiredScrollOffset += ScrollIncrement;
        else
            _desiredScrollOffset -= ScrollIncrement;

        _desiredScrollOffset = Mathf.Clamp(_desiredScrollOffset, 0f, _maxScrollOffset);
    }

    void ApplyScroll()
    {
        if (Mathf.Approximately(_currentScrollOffset, _desiredScrollOffset))
            return;

        float direction = _desiredScrollOffset > _currentScrollOffset ? 1f : -1f;
        float delta = direction * Time.deltaTime * ScrollSpeed;
        if (Mathf.Abs(delta) > Mathf.Abs(_desiredScrollOffset - _currentScrollOffset))
            _currentScrollOffset = _desiredScrollOffset;
        else
            _currentScrollOffset += delta;

        ApplyCameraScroll();
    }

    void ApplyCameraScroll()
    {
        if (ScrollCamera == null)
            return;

        Transform cameraTransform = ScrollCamera.transform;
        Vector3 localPosition = cameraTransform.localPosition;
        localPosition.z = _cameraBaseZ - _currentScrollOffset;
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
    public float ViewWidth = 40f;
    public float ViewDepth = 34f;
    public float ItemWidth = 5f;
    public float ItemDepth = 7f;
    public float ItemMargin = 0.5f;
    public float ItemScale = 1f;
    [Tooltip("0 = auto-fit as many columns as ViewWidth allows.")]
    public int Columns = 0;

    public int GetColumnCount(int itemCount)
    {
        if (itemCount <= 0)
            return 1;

        if (Columns > 0)
            return Columns;

        int columns = 1;
        float widthUsed = ItemWidth;
        while (widthUsed + ItemMargin + ItemWidth <= ViewWidth + 0.001f)
        {
            columns++;
            widthUsed += ItemMargin + ItemWidth;
        }

        return Mathf.Max(1, columns);
    }
}

public enum ContentScrollMode
{
    None,
    Cards,
    PlayHistory,
}
