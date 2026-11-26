using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overworld status screen with two tabs:
///  - Current: the player's current deck, rituals, and trinkets. With Gates Beyond,
///    a ritual or trinket can be selected and then sacrificed with the Hammer button.
///  - Styx: the Styx deck, removed rituals, and removed trinkets that await at the river.
/// Shears (Strings of Fate) and Hammer (Gates Beyond) in BottomInfo are shown when the
/// player has that boon this run. The Hammer sacrifice button is only active on Current.
/// The static layout is authored in Prefabs/StatusScreen.prefab. ContentScrollView renders
/// the deck into DeckContent, and the rituals and trinkets into framed images built in SideContent.
/// The Styx tab tints the backdrop cold (StyxBackdropTint) so the two tabs read as different places.
/// </summary>
public class StatusScreenHandler : MonoBehaviour
{
    [Header("Scene References")]
    public Button CurrentTabButton;
    public Button StyxTabButton;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI SubtitleText;
    public TextMeshProUGUI HeartstringsText;
    [Tooltip("Row the heartstring pips are built into: one per heartstring, lit for each one the player holds.")]
    public RectTransform HeartstringPips;
    [Tooltip("Shows the deck. Found by name under this screen if left empty.")]
    public RawImage DeckContent;
    public RectTransform SideContentRoot;

    [Header("Boss Boons")]
    public GameObject ShearsSection;
    public GameObject HammerSection;
    public Button SacrificeButton;

    [Header("Templates (inactive in scene, cloned into SideContent)")]
    public TextMeshProUGUI SectionHeaderTemplate;
    public TextMeshProUGUI EmptyRowTemplate;
    [Tooltip("Plaque frame laid over each side section (Image, Fill Center off).")]
    public Image SectionFrameTemplate;
    [Tooltip("One heartstring pip, cloned into HeartstringPips.")]
    public Image HeartstringPipTemplate;

    [Header("Text")]
    public string CurrentTitle = "Your Possessions";
    [Tooltip("{0} is the number of cards in the deck.")]
    public string CurrentSubtitle = "{0} cards in your deck";
    public string StyxTitle = "What Waits at the Styx";
    [Tooltip("{0} is the number of cards in the Styx deck.")]
    public string StyxSubtitle = "What you give up drifts here. Cross the Styx to trade all you carry for it.";

    [Header("Styx Tab")]
    [Tooltip("Multiplies the carpet backdrop while the Styx tab is shown.")]
    public Color StyxBackdropTint = new Color(0.5f, 0.56f, 0.6f, 1f);

    [Header("Heartstrings")]
    public Color HeartstringHeldColor = new Color(0.851f, 0.706f, 0.353f, 1f);
    public Color HeartstringMissingColor = new Color(0.32f, 0.28f, 0.24f, 0.6f);

    [Header("Side Sections")]
    [Tooltip("Height of the trinket section in SideContent; the ritual section gets the rest. Applied live while the screen is open.")]
    public float TrinketSectionHeight = 382f;

    [Header("Tab Colors")]
    public Color TabColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color TabSelectedColor = Color.white;

    const float HeaderTopPadding = 6f;
    const float HeaderSpacing = 12f;
    const float SectionSpacing = 10f;

    bool isOpen;
    bool styxTabActive;
    bool canSelectForSacrifice;
    Ritual selectedRitual;
    Trinket selectedTrinket;

    void Awake()
    {
        ResolveBoonReferences();
        CurrentTabButton.onClick.AddListener(() => ShowTab(false));
        StyxTabButton.onClick.AddListener(() => ShowTab(true));
        if (SacrificeButton != null)
            SacrificeButton.onClick.AddListener(SacrificeSelected);

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
        if (!isOpen || !canSelectForSacrifice || !Input.GetMouseButtonDown(0) || TutorialPresenter.BlocksGameInput)
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
        // Once crossed, nothing more waits at the Styx this run. The Current tab would then be a lone tab
        // with nothing to switch to, so the whole tab row goes.
        bool crossed = Controller.Instance.StyxRunState.HasCrossed;
        StyxTabButton.gameObject.SetActive(!crossed);
        CurrentTabButton.gameObject.SetActive(!crossed);
        ShowTab(false);
    }

    public void Close()
    {
        isOpen = false;
        selectedRitual = null;
        selectedTrinket = null;
        ContentScrollView.HighlightRitual(null);
        ContentScrollView.HighlightTrinket(null);
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
        List<Card> deck = styxTabActive
            ? Controller.Instance.StyxRunState.GetStyxDeck()
            : Controller.Instance.HumanPlayerDetails.DeckBlueprint[0];
        TitleText.text = styxTabActive ? StyxTitle : CurrentTitle;
        if (SubtitleText != null)
            SubtitleText.text = string.Format(styxTabActive ? StyxSubtitle : CurrentSubtitle, deck.Count);
        RefreshHeartstrings();

        selectedRitual = null;
        selectedTrinket = null;
        canSelectForSacrifice = !styxTabActive && HasGatesBeyond();

        ContentScrollView.ShowCards(GetSortedDeck(deck), DeckContent != null ? DeckContent.rectTransform : null);

        StyxUI.Clear(SideContentRoot);
        if (styxTabActive) PopulateStyxSide();
        else PopulateCurrentSide();
        ContentScrollView.SetBackdropTint(styxTabActive ? StyxBackdropTint : Color.white);

        RefreshBoonSections();
        RefreshSacrificeSelection();
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

        List<Ritual> rituals = new List<Ritual>();
        if (details.MajorRituals[0] != null) rituals.Add(details.MajorRituals[0]);
        if (details.MinorRituals[0] != null) rituals.Add(details.MinorRituals[0]);

        PopulateSections("Rituals", rituals, "Trinkets", details.Trinkets[0], "None", 0f);
    }

    void PopulateStyxSide()
    {
        StyxRunState styxState = Controller.Instance.StyxRunState;
        PopulateSections("Lost Rituals", styxState.RemovedRituals, "Lost Trinkets", styxState.RemovedTrinkets, "Nothing yet", 0f);
    }

    // Splits the side panel into a header and scrolling section for the rituals, then the trinkets.
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

        y = AddSectionHeader(y, trinketLabel, trinkets.Count);
        y = AddSection(y, trinketHeight, trinkets.Count > 0 ? null : emptyLabel,
            image => ContentScrollView.ShowTrinkets(trinkets, image));
        return y;
    }

    float AddSectionHeader(float y, string label, int count = 0)
    {
        TextMeshProUGUI header = CloneTemplate(SectionHeaderTemplate);
        header.gameObject.name = "Header_" + label;
        header.text = count > 0 ? label + "<alpha=#80>  " + count : label;
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

        if (SectionFrameTemplate != null)
        {
            Image frame = Instantiate(SectionFrameTemplate, rect);
            frame.gameObject.name = "Frame";
            frame.gameObject.SetActive(true);
            frame.raycastTarget = false;
            StyxUI.Stretch(frame.rectTransform);
        }

        if (emptyLabel != null)
        {
            TextMeshProUGUI text = CloneTemplate(EmptyRowTemplate);
            text.text = emptyLabel;
            text.rectTransform.SetParent(rect, false);
            StyxUI.Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.Center;
        }

        return y - height - SectionSpacing;
    }

    void RefreshSacrificeSelection()
    {
        ContentScrollView.HighlightRitual(selectedRitual);
        ContentScrollView.HighlightTrinket(selectedTrinket);

        if (SacrificeButton == null)
            return;

        bool showButton = HasGatesBeyond() && !styxTabActive;
        SacrificeButton.gameObject.SetActive(showButton);
        SacrificeButton.interactable = showButton && (selectedRitual != null || selectedTrinket != null);
    }

    void SacrificeSelected()
    {
        if (styxTabActive)
            return;

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

        if (!sacrificed)
            return;

        if (View.Instance != null && View.Instance.AudioHandler != null)
            View.Instance.AudioHandler.PlayHeartstringStrum();

        Rebuild();
    }

    // The count, plus a row of pips (strings): lit for each heartstring held, dim for each still to win
    void RefreshHeartstrings()
    {
        int held = Controller.Instance.RunHeartStrings;
        int max = Player.MaxHeartStrings;
        HeartstringsText.text = held + " / " + max;

        if (HeartstringPips == null || HeartstringPipTemplate == null)
            return;

        StyxUI.Clear(HeartstringPips);
        for (int i = 0; i < max; i++)
        {
            Image pip = Instantiate(HeartstringPipTemplate, HeartstringPips);
            pip.gameObject.name = "Pip" + i;
            pip.gameObject.SetActive(true);
            pip.color = i < held ? HeartstringHeldColor : HeartstringMissingColor;
        }
    }

    void ResolveBoonReferences()
    {
        if (ShearsSection == null)
        {
            Transform shears = transform.Find("StatusUI/BottomInfo/Shears");
            if (shears != null)
                ShearsSection = shears.gameObject;
        }

        if (HammerSection == null)
        {
            Transform hammer = transform.Find("StatusUI/BottomInfo/Hammer");
            if (hammer != null)
                HammerSection = hammer.gameObject;
        }

        if (SacrificeButton == null && HammerSection != null)
            SacrificeButton = HammerSection.GetComponentInChildren<Button>(true);
    }

    void RefreshBoonSections()
    {
        if (ShearsSection != null)
            ShearsSection.SetActive(HasStringsOfFate());
        if (HammerSection != null)
            HammerSection.SetActive(HasGatesBeyond());
    }

    static bool HasStringsOfFate()
    {
        return HasTrinket(trinket => trinket is StringsOfFateTrinket);
    }

    static bool HasGatesBeyond()
    {
        return HasTrinket(trinket => trinket is GatesBeyondTrinket);
    }

    static bool HasTrinket(System.Predicate<Trinket> match)
    {
        List<Trinket> trinkets = Controller.Instance.HumanPlayerDetails.Trinkets[0];
        return trinkets != null && trinkets.Exists(match);
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
