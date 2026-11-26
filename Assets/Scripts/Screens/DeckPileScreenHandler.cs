using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The menu a <see cref="DeckPile"/> opens in combat. For your own pile it has two tabs: Draw Pile (the cards left
/// to draw, sorted so the order gives nothing away) and Full Deck (every card you brought to the fight). For the
/// enemy's pile it shows their whole deck only, so you get a sense of what they can do without seeing what's next.
///
/// Laid out in <c>Resources/Prefabs/UI/DeckPileScreen.prefab</c> (the status screen's look: tabs top left, title
/// and subtitle centred). It isn't in the scene: the first <see cref="Open"/> instantiates it beside
/// DeckViewerScreen and registers it with ScreenHandler. ContentScrollView renders the cards into DeckContent.
/// Closing goes through <see cref="Controller.HideContentScrollView"/>, so Escape and Back behave like the other
/// viewers.
/// </summary>
public class DeckPileScreenHandler : MonoBehaviour
{
    const string PrefabPath = "Prefabs/UI/DeckPileScreen";
    const string RenderTexturePath = "RenderTextures/DeckViewer";

    static DeckPileScreenHandler instance;

    [Header("References")]
    public Button DrawPileTab;
    public Button FullDeckTab;
    public Button BackButton;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI SubtitleText;
    public RawImage DeckContent;

    [Header("Text")]
    public string PlayerTitle = "Your Deck";
    [Tooltip("{0} is the number of cards left to draw.")]
    public string DrawPileSubtitle = "{0} cards left to draw, in no particular order";
    public string DrawPileSingularSubtitle = "1 card left to draw";
    public string DrawPileEmptySubtitle = "No cards left to draw. Your next draw reshuffles your whole deck.";
    [Tooltip("{0} is the number of cards in the deck.")]
    public string FullDeckSubtitle = "{0} cards in your deck";
    public string EnemyTitle = "Enemy's Deck";
    [Tooltip("{0} is the number of cards in the enemy's deck.")]
    public string EnemySubtitle = "{0} cards they might draw";

    [Header("Tab Colors")]
    public Color TabColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color TabSelectedColor = Color.white;

    Player shownPlayer;
    bool fullDeckTab;
    bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    /// <summary>Opens the deck menu for <paramref name="player"/>'s pile.</summary>
    public static void Open(Player player)
    {
        if (player == null || !Ensure())
            return;
        instance.Show(player);
    }

    static bool Ensure()
    {
        if (instance != null)
            return true;
        if (ScreenHandler.Instance == null)
            return false;

        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("DeckPileScreenHandler: prefab missing at Resources/" + PrefabPath);
            return false;
        }

        // Beside the other full-screen viewers, so it sorts with them (above the board UI, below popups).
        Transform parent = null;
        int sibling = -1;
        if (ScreenHandler.Instance.TryGetScreen(ScreenName.DeckViewerScreen, out Screen deckViewer))
        {
            parent = deckViewer.transform.parent;
            sibling = deckViewer.transform.GetSiblingIndex() + 1;
        }
        if (parent == null)
            return false;

        GameObject screenObject = Instantiate(prefab, parent, false);
        screenObject.name = prefab.name;
        if (sibling >= 0)
            screenObject.transform.SetSiblingIndex(sibling);

        Screen screen = screenObject.GetComponent<Screen>();
        ScreenHandler.Instance.Screens.Add(screen);
        screen.Init();
        screen.Exit(true);

        instance = screenObject.GetComponent<DeckPileScreenHandler>();
        return instance != null;
    }

    void Awake()
    {
        if (DrawPileTab != null) DrawPileTab.onClick.AddListener(() => ShowTab(false));
        if (FullDeckTab != null) FullDeckTab.onClick.AddListener(() => ShowTab(true));
        if (BackButton != null) BackButton.onClick.AddListener(Close);

        if (DeckContent != null)
        {
            RenderTexture texture = Resources.Load<RenderTexture>(RenderTexturePath);
            if (texture != null)
                DeckContent.texture = texture;
        }

        Screen screen = GetComponent<Screen>();
        if (screen != null)
        {
            screen.InstantEnter = true;
            screen.InstantExit = true;
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Update()
    {
        if (!isOpen)
            return;

        // Something else closed the viewer (Escape, another screen), or the fight ended under it.
        if (!ContentScrollView.IsActive || Controller.Instance == null || !Controller.Instance.GameRunning)
            Close();
    }

    void Show(Player player)
    {
        shownPlayer = player;
        fullDeckTab = !player.IsHuman;

        ScreenHandler.Instance.HideScreen(ScreenName.Game, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.DeckPileScreen, true, false);
        isOpen = true;

        bool tabs = player.IsHuman;
        if (DrawPileTab != null) DrawPileTab.gameObject.SetActive(tabs);
        if (FullDeckTab != null) FullDeckTab.gameObject.SetActive(tabs);
        Rebuild();
    }

    void ShowTab(bool fullDeck)
    {
        if (!isOpen)
            return;
        fullDeckTab = fullDeck;
        Rebuild();
    }

    void Rebuild()
    {
        if (shownPlayer == null)
            return;

        List<Card> cards;
        if (!shownPlayer.IsHuman)
        {
            cards = shownPlayer.DeckBlueprint;
            TitleText.text = EnemyTitle;
            SubtitleText.text = string.Format(EnemySubtitle, cards.Count);
        }
        else
        {
            cards = fullDeckTab ? shownPlayer.DeckBlueprint : shownPlayer.Deck;
            TitleText.text = PlayerTitle;
            SubtitleText.text = fullDeckTab ? string.Format(FullDeckSubtitle, cards.Count)
                : cards.Count == 0 ? DrawPileEmptySubtitle
                : cards.Count == 1 ? DrawPileSingularSubtitle
                : string.Format(DrawPileSubtitle, cards.Count);

            if (DrawPileTab != null) DrawPileTab.targetGraphic.color = fullDeckTab ? TabColor : TabSelectedColor;
            if (FullDeckTab != null) FullDeckTab.targetGraphic.color = fullDeckTab ? TabSelectedColor : TabColor;
        }

        ContentScrollView.ShowCards(Sorted(cards), DeckContent != null ? DeckContent.rectTransform : null);
    }

    // Cost, then followers before spells, then name: the draw pile's real order stays hidden.
    static List<Card> Sorted(List<Card> source)
    {
        var cards = new List<Card>(source);
        cards.Sort((a, b) =>
        {
            int cost = a.Costs[OfferingType.Gold].CompareTo(b.Costs[OfferingType.Gold]);
            if (cost != 0) return cost;
            bool aFollower = a is Follower, bFollower = b is Follower;
            if (aFollower != bFollower) return aFollower ? -1 : 1;
            return string.CompareOrdinal(a.GetName(), b.GetName());
        });
        return cards;
    }

    void Close()
    {
        if (!isOpen)
            return;
        isOpen = false;
        shownPlayer = null;

        ScreenHandler.Instance.HideScreen(ScreenName.DeckPileScreen, true);
        if (ContentScrollView.IsActive)
            Controller.Instance.HideContentScrollView();
    }
}
