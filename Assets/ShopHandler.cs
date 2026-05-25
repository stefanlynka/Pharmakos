using System.Collections.Generic;
using UnityEngine;

public class ShopHandler : MonoBehaviour
{
    public static ShopHandler Instance;

    public Camera ShopCamera;
    public ShopScreenHandler ShopScreenHandler;
    public PlayerLyre PlayerLyre;
    public Transform CardRow1;
    public Transform CardRow2;
    public Transform TrinketContainer;

    public int CardCost = 1;
    public int TrinketCost = 2;

    const int CardSlotCount = 8;
    const int TrinketSlotCount = 3;

    struct ShopCardSlotPose
    {
        public Transform RowParent;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }

    readonly ShopCardSlotPose[] _cardSlotPoses = new ShopCardSlotPose[CardSlotCount];
    ViewBuff[] _trinketSlots = new ViewBuff[TrinketSlotCount];

    readonly List<Card> _shopCards = new List<Card>();
    readonly List<Trinket> _shopTrinkets = new List<Trinket>();
    readonly List<ViewCard> _cardViews = new List<ViewCard>();
    readonly Dictionary<ViewCard, int> _cardViewToSlot = new Dictionary<ViewCard, int>();
    readonly HashSet<int> _soldCardSlots = new HashSet<int>();
    readonly HashSet<int> _soldTrinketSlots = new HashSet<int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CacheSlotsFromEditorMarkers();
        if (ShopCamera != null) ShopCamera.gameObject.SetActive(false);
        SetShopAreaActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        UpdateShopHoverHighlights();
    }

    public void BeginShop()
    {
        if (Controller.Instance == null || View.Instance == null)
        {
            Debug.LogError("ShopHandler: Controller or View is missing.");
            return;
        }

        _soldCardSlots.Clear();
        _soldTrinketSlots.Clear();
        BuildShopStock();

        SetShopAreaActive(true);
        if (ShopCamera != null) ShopCamera.gameObject.SetActive(true);

        if (ShopScreenHandler != null)
        {
            ShopScreenHandler.ResetForShop();
            ShopScreenHandler.RefreshHeartstringsDisplay();
        }

        RefreshPlayerLyre();

        if (View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Activate(ShopCamera, () => false);

        DisplayStock();
    }

    public void EndShop()
    {
        ClearDisplayedStock();
        _shopCards.Clear();
        _shopTrinkets.Clear();
        _soldCardSlots.Clear();
        _soldTrinketSlots.Clear();

        if (ShopCamera != null) ShopCamera.gameObject.SetActive(false);
        SetShopAreaActive(false);

        if (View.Instance != null && View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Deactivate();
    }

    void BuildShopStock()
    {
        _shopCards.Clear();
        _shopTrinkets.Clear();

        List<Card> cardPool = Controller.Instance.ProgressionHandler.GetShopCardPool();
        int cardCount = Mathf.Min(CardSlotCount, cardPool.Count);
        for (int i = 0; i < cardCount; i++)
        {
            int idx = Controller.Instance.MetaRNG.Next(0, cardPool.Count);
            _shopCards.Add(cardPool[idx]);
            cardPool.RemoveAt(idx);
        }

        _shopTrinkets.AddRange(Controller.Instance.ProgressionHandler.GetRandomShopTrinkets(TrinketSlotCount));
    }

    void DisplayStock()
    {
        ClearDisplayedStock();

        for (int i = 0; i < _shopCards.Count; i++)
        {
            if (!TryGetCardSlotPose(i, out ShopCardSlotPose slotPose))
                continue;

            ViewCard viewCard = View.Instance.MakeNewViewCard(_shopCards[i], false);
            viewCard.transform.SetParent(slotPose.RowParent, false);
            viewCard.transform.localPosition = slotPose.LocalPosition;
            viewCard.transform.localRotation = slotPose.LocalRotation;
            viewCard.transform.localScale = slotPose.LocalScale;
            viewCard.Load(_shopCards[i], OnCardClicked);
            viewCard.SetDescriptiveMode(true);
            if (viewCard.CardCollider != null) viewCard.CardCollider.enabled = true;
            viewCard.SetHighlight(false);
            viewCard.gameObject.SetActive(true);
            _cardViews.Add(viewCard);
            _cardViewToSlot[viewCard] = i;
        }

        for (int i = 0; i < _shopTrinkets.Count && i < _trinketSlots.Length; i++)
        {
            ViewBuff viewBuff = _trinketSlots[i];
            if (viewBuff == null) continue;

            Trinket trinket = _shopTrinkets[i];
            viewBuff.SetBuffData(trinket.MyEffect, trinket.GetDescriptionData());
            viewBuff.SetOnClick(OnTrinketClicked);
            viewBuff.SetHighlight(false);
            viewBuff.SetVisible(true);
            // viewBuff.SetSummaryForced(true);
        }
    }

    void OnCardClicked(ViewTarget viewTarget)
    {
        ViewCard clicked = viewTarget as ViewCard;
        if (clicked == null) return;

        if (!_cardViewToSlot.TryGetValue(clicked, out int slot) || _soldCardSlots.Contains(slot)) return;

        TryPurchaseCard(slot, clicked);
    }

    void OnTrinketClicked(ViewBuff clicked)
    {
        if (clicked == null) return;

        int slot = System.Array.IndexOf(_trinketSlots, clicked);
        if (slot < 0 || _soldTrinketSlots.Contains(slot)) return;

        TryPurchaseTrinket(slot);
    }

    void TryPurchaseCard(int slot, ViewCard clicked)
    {
        if (slot < 0 || slot >= _shopCards.Count) return;
        if (!Controller.Instance.TrySpendHeartstrings(CardCost)) return;

        Controller.Instance.AddCardsToPlayerDeck(new List<Card> { _shopCards[slot].MakeBaseCopy() });
        MarkCardSold(slot, clicked);
        ShopScreenHandler?.RefreshHeartstringsDisplay();
        RefreshPlayerLyre();
    }

    void TryPurchaseTrinket(int slot)
    {
        if (slot < 0 || slot >= _shopTrinkets.Count) return;
        if (!Controller.Instance.TrySpendHeartstrings(TrinketCost)) return;

        Controller.Instance.AddTrinket(_shopTrinkets[slot].MakeBaseCopy());
        MarkTrinketSold(slot);
        ShopScreenHandler?.RefreshHeartstringsDisplay();
        RefreshPlayerLyre();
    }

    void MarkCardSold(int slot, ViewCard clicked)
    {
        _soldCardSlots.Add(slot);
        ReleaseViewCard(clicked);
        _cardViewToSlot.Remove(clicked);
        _cardViews.Remove(clicked);
    }

    void MarkTrinketSold(int slot)
    {
        _soldTrinketSlots.Add(slot);
        ViewBuff viewBuff = _trinketSlots[slot];
        if (viewBuff != null)
        {
            viewBuff.SetOnClick(null);
            viewBuff.SetVisible(false);
            // viewBuff.SetSummaryForced(false);
        }
    }

    void ClearDisplayedStock()
    {
        foreach (ViewCard viewCard in _cardViews)
            ReleaseViewCard(viewCard);
        _cardViews.Clear();
        _cardViewToSlot.Clear();

        if (_trinketSlots == null) return;
        foreach (ViewBuff viewBuff in _trinketSlots)
        {
            if (viewBuff == null) continue;
            viewBuff.SetOnClick(null);
            viewBuff.SetVisible(false);
            // viewBuff.SetSummaryForced(false);
        }
    }

    void ReleaseViewCard(ViewCard viewCard)
    {
        if (viewCard == null) return;
        viewCard.SetHighlight(false);
        if (viewCard.CardCollider != null) viewCard.CardCollider.enabled = false;
        View.Instance.ReleaseCard(viewCard);
    }

    void CacheSlotsFromEditorMarkers()
    {
        int slot = 0;
        CacheRowSlotPoses(CardRow1, ref slot);
        CacheRowSlotPoses(CardRow2, ref slot);

        if (TrinketContainer != null)
        {
            ViewBuff[] buffs = TrinketContainer.GetComponentsInChildren<ViewBuff>(true);
            for (int i = 0; i < TrinketSlotCount && i < buffs.Length; i++)
                _trinketSlots[i] = buffs[i];
        }
    }

    void CacheRowSlotPoses(Transform row, ref int slotIndex)
    {
        if (row == null)
            return;

        int childCount = row.childCount;
        var placeholders = new Transform[Mathf.Min(childCount, CardSlotCount - slotIndex)];
        for (int i = 0; i < placeholders.Length; i++)
            placeholders[i] = row.GetChild(i);

        for (int i = 0; i < placeholders.Length; i++)
        {
            Transform placeholder = placeholders[i];
            _cardSlotPoses[slotIndex] = new ShopCardSlotPose
            {
                RowParent = row,
                LocalPosition = placeholder.localPosition,
                LocalRotation = placeholder.localRotation,
                LocalScale = placeholder.localScale,
            };
            slotIndex++;
            Destroy(placeholder.gameObject);
        }
    }

    bool TryGetCardSlotPose(int slotIndex, out ShopCardSlotPose pose)
    {
        if (slotIndex < 0 || slotIndex >= _cardSlotPoses.Length || _cardSlotPoses[slotIndex].RowParent == null)
        {
            pose = default;
            return false;
        }

        pose = _cardSlotPoses[slotIndex];
        return true;
    }

    void SetShopAreaActive(bool active)
    {
        gameObject.SetActive(active);
    }

    void UpdateShopHoverHighlights()
    {
        if (Controller.Instance == null)
            return;

        ViewCard hoveredCard = GetHoveredShopCard();
        bool canAffordCard = CanAfford(CardCost);

        for (int i = 0; i < _cardViews.Count; i++)
        {
            ViewCard viewCard = _cardViews[i];
            bool highlight = viewCard == hoveredCard && canAffordCard;
            viewCard.SetHighlight(highlight);
        }

        ViewBuff hoveredTrinket = GetHoveredTrinket();
        bool canAffordTrinket = CanAfford(TrinketCost);

        for (int i = 0; i < _trinketSlots.Length; i++)
        {
            ViewBuff viewBuff = _trinketSlots[i];
            if (viewBuff == null || !viewBuff.gameObject.activeInHierarchy)
                continue;

            if (_soldTrinketSlots.Contains(i))
            {
                viewBuff.SetHighlight(false);
                continue;
            }

            viewBuff.SetHighlight(viewBuff == hoveredTrinket && canAffordTrinket);
        }

        int hoverHighlightCost = 0;
        if (hoveredCard != null && canAffordCard)
            hoverHighlightCost = CardCost;
        else if (hoveredTrinket != null && canAffordTrinket)
            hoverHighlightCost = TrinketCost;

        RefreshPlayerLyre(hoverHighlightCost);
    }

    void RefreshPlayerLyre(int highlightedCount = 0)
    {
        if (PlayerLyre == null || Controller.Instance == null)
            return;

        PlayerLyre.SetHeartstrings(Controller.Instance.RunHeartStrings, highlightedCount);
    }

    ViewCard GetHoveredShopCard()
    {
        if (MenuSelectionHandler.Instance == null || !MenuSelectionHandler.Instance.IsActive)
            return null;

        ViewCard hoveredCard = MenuSelectionHandler.Instance.CurrentHover as ViewCard;
        if (hoveredCard == null || !_cardViewToSlot.ContainsKey(hoveredCard))
            return null;

        if (_cardViewToSlot.TryGetValue(hoveredCard, out int slot) && _soldCardSlots.Contains(slot))
            return null;

        return hoveredCard;
    }

    ViewBuff GetHoveredTrinket()
    {
        Camera camera = ShopCamera;
        if (camera == null && MenuSelectionHandler.Instance != null)
            camera = MenuSelectionHandler.Instance.GetSelectionCamera();
        if (camera == null)
            return null;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
            return null;

        ViewBuff viewBuff = hit.collider.GetComponentInParent<ViewBuff>();
        if (viewBuff == null)
            return null;

        for (int i = 0; i < _trinketSlots.Length; i++)
        {
            if (_trinketSlots[i] != viewBuff || _soldTrinketSlots.Contains(i))
                continue;

            return viewBuff;
        }

        return null;
    }

    bool CanAfford(int cost)
    {
        return Controller.Instance != null && Controller.Instance.RunHeartStrings >= cost;
    }
}
