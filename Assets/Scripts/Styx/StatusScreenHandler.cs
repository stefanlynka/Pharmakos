using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overworld status screen with two tabs:
///  - Current: the player's current deck, rituals, and trinkets. With the Gates Beyond
///    trinket, a ritual or trinket can be clicked and sacrificed to the Styx in exchange for a heartstring.
///  - Styx: the Styx deck, removed rituals, and removed trinkets that await at the river.
/// The static layout is authored in the scene under the StatusScreen GameObject. ContentScrollView renders the deck
/// into the scene-authored DeckContent image, and the rituals and trinkets into images built in SideContent at
/// runtime; each section only scrolls while the mouse is over it.
/// </summary>
public class StatusScreenHandler : MonoBehaviour
{
    [Header("Scene References")]
    public Button CurrentTabButton;
    public Button StyxTabButton;
    public Button CloseButton;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI HeartstringsText;
    [Tooltip("Shows the deck. Found by name under this screen if left empty.")]
    public RawImage DeckContent;
    public RectTransform SideContentRoot;

    [Header("Templates (inactive in scene, cloned into SideContent)")]
    public TextMeshProUGUI SectionHeaderTemplate;
    public TextMeshProUGUI EmptyRowTemplate;
    public TextMeshProUGUI SacrificeNoteTemplate;

    [Header("Side Sections")]
    [Tooltip("Height of the trinket section in SideContent; the ritual section gets the rest. Applied live while the screen is open.")]
    public float TrinketSectionHeight = 382f;

    [Header("Tab Colors")]
    public Color TabColor = new Color(0.23f, 0.27f, 0.37f, 1f);
    public Color TabSelectedColor = new Color(0.62f, 0.5f, 0.16f, 1f);

    const float HeaderTopPadding = 6f;
    const float HeaderSpacing = 12f;
    const float SectionSpacing = 10f;
    const float EmptyLabelPadding = 8f;
    const float NoteTopPadding = 8f;
    const float SacrificeButtonSpacing = 8f;
    const float SacrificeButtonFontSize = 24f;
    static readonly Vector2 SacrificeButtonSize = new Vector2(200f, 48f);

    bool isOpen;
    bool styxTabActive;
    bool canSelectForSacrifice;
    Ritual selectedRitual;
    Trinket selectedTrinket;
    Button sacrificeButton;

    void Awake()
    {
        CurrentTabButton.onClick.AddListener(() => ShowTab(false));
        StyxTabButton.onClick.AddListener(() => ShowTab(true));
        CloseButton.onClick.AddListener(() => Controller.Instance.CloseStatusScreen());

        if (DeckContent == null)
            DeckContent = FindDeckContent();

        CreateClickBlocker();
    }

    void OnValidate()
    {
        if (Application.isPlaying && isOpen)
            Rebuild();
    }

    // The screen stays active while hidden, so clicks only count while it's open
    void Update()
    {
        if (!isOpen || !canSelectForSacrifice || !Input.GetMouseButtonDown(0))
            return;

        Ritual ritual = ContentScrollView.GetRitualUnderMouse();
        Trinket trinket = ritual == null ? ContentScrollView.GetTrinketUnderMouse() : null;
        if (ritual == null && trinket == null)
            return;

        // Clicking the selection again deselects it
        selectedRitual = ritual != null && ritual != selectedRitual ? ritual : null;
        selectedTrinket = trinket != null && trinket != selectedTrinket ? trinket : null;
        RefreshSacrificeSelection();
    }

    public void Open()
    {
        isOpen = true;
        // Once crossed, nothing more waits at the Styx this run
        StyxTabButton.gameObject.SetActive(!Controller.Instance.StyxRunState.HasCrossed);
        ShowTab(false);
    }

    public void Close()
    {
        isOpen = false;
        ContentScrollView.Hide();
    }

    void ShowTab(bool styxTab)
    {
        styxTabActive = styxTab && !Controller.Instance.StyxRunState.HasCrossed;
        Rebuild();
    }

    void Rebuild()
    {
        CurrentTabButton.targetGraphic.color = styxTabActive ? TabColor : TabSelectedColor;
        StyxTabButton.targetGraphic.color = styxTabActive ? TabSelectedColor : TabColor;
        TitleText.text = styxTabActive ? "What Waits At The Styx" : "Your Current Possessions";
        HeartstringsText.text = "Heartstrings: " + Controller.Instance.RunHeartStrings + " / " + Player.MaxHeartStrings;

        selectedRitual = null;
        selectedTrinket = null;
        canSelectForSacrifice = false;
        sacrificeButton = null;

        ContentScrollView.ShowCards(GetSortedDeck(styxTabActive
            ? Controller.Instance.StyxRunState.GetStyxDeck()
            : Controller.Instance.HumanPlayerDetails.DeckBlueprint[0]),
            DeckContent != null ? DeckContent.rectTransform : null);

        StyxUI.Clear(SideContentRoot);
        if (styxTabActive) PopulateStyxSide();
        else PopulateCurrentSide();
    }

    static List<Card> GetSortedDeck(List<Card> source)
    {
        List<Card> deck = new List<Card>(source);
        deck.Sort((a, b) =>
        {
            int costCompare = a.Costs[OfferingType.Gold].CompareTo(b.Costs[OfferingType.Gold]);
            if (costCompare != 0) return costCompare;

            bool aIsFollower = a is Follower;
            bool bIsFollower = b is Follower;
            if (aIsFollower && !bIsFollower) return -1;
            if (!aIsFollower && bIsFollower) return 1;
            return 0;
        });
        return deck;
    }

    void PopulateCurrentSide()
    {
        PlayerDetails details = Controller.Instance.HumanPlayerDetails;
        bool canSacrifice = details.CanSacrificeForHeartstrings;
        bool heartstringsFull = Controller.Instance.RunHeartStrings >= Player.MaxHeartStrings;

        List<Ritual> rituals = new List<Ritual>();
        if (details.MajorRituals[0] != null) rituals.Add(details.MajorRituals[0]);
        if (details.MinorRituals[0] != null) rituals.Add(details.MinorRituals[0]);

        float footerHeight = canSacrifice ? GetSacrificeFooterHeight() : 0f;
        float y = PopulateSections("Rituals", rituals, "Trinkets", details.Trinkets[0], "None", footerHeight);

        if (!canSacrifice) return;

        TextMeshProUGUI note = CloneTemplate(SacrificeNoteTemplate);
        note.text = heartstringsFull
            ? "Your heartstrings are full. The Styx offers nothing more."
            : "Gates Beyond: click a ritual or trinket, then sacrifice it to the Styx for a heartstring.";
        y -= NoteTopPadding;
        SetY(note.rectTransform, y);
        y -= note.rectTransform.sizeDelta.y + SacrificeButtonSpacing;

        if (heartstringsFull) return;

        canSelectForSacrifice = true;
        sacrificeButton = StyxUI.CreateButton(SideContentRoot, "SacrificeButton", "Sacrifice", SacrificeButtonFontSize, SacrificeSelected);
        StyxUI.SetAnchored((RectTransform)sacrificeButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), SacrificeButtonSize);
        RefreshSacrificeSelection();
    }

    void PopulateStyxSide()
    {
        StyxRunState styxState = Controller.Instance.StyxRunState;
        PopulateSections("Removed Rituals", styxState.RemovedRituals, "Removed Trinkets", styxState.RemovedTrinkets, "None yet", 0f);
    }

    // Splits the side panel (above the footer) into a header and scrolling section for the rituals, then the trinkets.
    // Returns the y below the trinket section.
    float PopulateSections(string ritualLabel, List<Ritual> rituals, string trinketLabel, List<Trinket> trinkets,
        string emptyLabel, float footerHeight)
    {
        float headerHeight = HeaderTopPadding + SectionHeaderTemplate.rectTransform.sizeDelta.y + HeaderSpacing;
        float sectionsHeight = Mathf.Max(0f, SideContentRoot.rect.height - headerHeight * 2f - SectionSpacing * 2f - footerHeight);
        float trinketHeight = Mathf.Clamp(TrinketSectionHeight, 0f, sectionsHeight);
        float ritualHeight = sectionsHeight - trinketHeight;

        float y = 0f;
        y = AddSectionHeader(y, ritualLabel);
        y = AddSection(y, ritualHeight, rituals.Exists(ritual => ritual != null) ? null : emptyLabel,
            image => ContentScrollView.ShowRituals(rituals, image));

        y = AddSectionHeader(y, trinketLabel);
        y = AddSection(y, trinketHeight, trinkets.Count > 0 ? null : emptyLabel,
            image => ContentScrollView.ShowTrinkets(trinkets, image));
        return y;
    }

    float AddSectionHeader(float y, string label)
    {
        TextMeshProUGUI header = CloneTemplate(SectionHeaderTemplate);
        header.gameObject.name = "Header_" + label;
        header.text = label;
        SetY(header.rectTransform, y - HeaderTopPadding);
        return y - HeaderTopPadding - header.rectTransform.sizeDelta.y - HeaderSpacing;
    }

    // A full-width image of the given height for ContentScrollView to render a section into, labelled if empty
    float AddSection(float y, float height, string emptyLabel, System.Action<RawImage> show)
    {
        RectTransform rect = StyxUI.CreateRect(SideContentRoot, "Section");
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(0f, height);

        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        show(image);

        if (emptyLabel != null)
        {
            TextMeshProUGUI text = CloneTemplate(EmptyRowTemplate);
            text.text = emptyLabel;
            SetY(text.rectTransform, y - EmptyLabelPadding);
        }

        return y - height - SectionSpacing;
    }

    float GetSacrificeFooterHeight()
    {
        return NoteTopPadding + SacrificeNoteTemplate.rectTransform.sizeDelta.y + SacrificeButtonSpacing + SacrificeButtonSize.y;
    }

    void RefreshSacrificeSelection()
    {
        ContentScrollView.HighlightRitual(selectedRitual);
        ContentScrollView.HighlightTrinket(selectedTrinket);

        if (sacrificeButton != null)
            sacrificeButton.interactable = selectedRitual != null || selectedTrinket != null;
    }

    void SacrificeSelected()
    {
        bool sacrificed = false;
        if (selectedRitual != null)
        {
            bool major = selectedRitual == Controller.Instance.HumanPlayerDetails.MajorRituals[0];
            sacrificed = Controller.Instance.TrySacrificeRitualForHeartstring(major);
        }
        else if (selectedTrinket != null)
        {
            sacrificed = Controller.Instance.TrySacrificeTrinketForHeartstring(selectedTrinket);
        }

        if (sacrificed)
            Rebuild();
    }

    // The screen only covers part of the view, so an invisible full-screen image behind it stops clicks reaching UI below
    void CreateClickBlocker()
    {
        Image blocker = StyxUI.CreatePanel(transform, "ClickBlocker", Color.clear);
        StyxUI.Stretch(blocker.rectTransform);
        blocker.rectTransform.SetAsFirstSibling();
    }

    RawImage FindDeckContent()
    {
        foreach (RawImage image in GetComponentsInChildren<RawImage>(true))
        {
            if (image.name == "DeckContent")
                return image;
        }
        return null;
    }

    T CloneTemplate<T>(T template) where T : Component
    {
        T clone = Instantiate(template, SideContentRoot);
        clone.gameObject.SetActive(true);
        return clone;
    }

    static void SetY(RectTransform rect, float y)
    {
        Vector2 position = rect.anchoredPosition;
        position.y = y;
        rect.anchoredPosition = position;
    }
}
