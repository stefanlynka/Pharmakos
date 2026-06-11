using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overworld status screen with two tabs:
///  - Current: the player's current deck, rituals, and trinkets. With the Gates Beyond
///    trinket, rituals/trinkets can be sacrificed to the Styx in exchange for a heartstring.
///  - Styx: the Styx deck, removed rituals, and removed trinkets that await at the river.
/// The static layout is authored in the scene under the StatusScreen GameObject; the
/// ritual/trinket rows are cloned from inactive scene templates at runtime.
/// The deck is rendered via ContentScrollView into the scene-authored DeckContent image.
/// </summary>
public class StatusScreenHandler : MonoBehaviour
{
    [Header("Scene References")]
    public Button CurrentTabButton;
    public Button StyxTabButton;
    public Button CloseButton;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI HeartstringsText;
    public RectTransform SideContentRoot;

    [Header("Templates (inactive in scene, cloned into SideContent)")]
    public TextMeshProUGUI SectionHeaderTemplate;
    public TextMeshProUGUI EmptyRowTemplate;
    public TextMeshProUGUI SacrificeNoteTemplate;
    public StatusRowView RitualRowTemplate;
    public StatusRowView TrinketRowTemplate;

    [Header("Tab Colors")]
    public Color TabColor = new Color(0.23f, 0.27f, 0.37f, 1f);
    public Color TabSelectedColor = new Color(0.62f, 0.5f, 0.16f, 1f);

    const float HeaderTopPadding = 6f;
    const float HeaderSpacing = 12f;
    const float EmptyRowSpacing = 8f;
    const float RowSpacing = 10f;
    const float NoteTopPadding = 8f;

    bool styxTabActive;

    void Awake()
    {
        CurrentTabButton.onClick.AddListener(() => ShowTab(false));
        StyxTabButton.onClick.AddListener(() => ShowTab(true));
        CloseButton.onClick.AddListener(() => Controller.Instance.CloseStatusScreen());
    }

    public void Open()
    {
        ShowTab(false);
    }

    public void Close()
    {
        ContentScrollView.Hide();
    }

    void ShowTab(bool styxTab)
    {
        styxTabActive = styxTab;
        Rebuild();
    }

    void Rebuild()
    {
        CurrentTabButton.targetGraphic.color = styxTabActive ? TabColor : TabSelectedColor;
        StyxTabButton.targetGraphic.color = styxTabActive ? TabSelectedColor : TabColor;
        TitleText.text = styxTabActive ? "What Waits At The Styx" : "Your Current Possessions";
        HeartstringsText.text = "Heartstrings: " + Controller.Instance.RunHeartStrings + " / " + Player.MaxHeartStrings;

        ContentScrollView.ShowCards(GetSortedDeck(styxTabActive
            ? Controller.Instance.StyxRunState.GetStyxDeck()
            : Controller.Instance.HumanPlayerDetails.DeckBlueprint[0]));

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

        float y = 0f;
        y = AddSectionHeader(y, "Rituals");
        System.Action sacrificeMajor = null;
        System.Action sacrificeMinor = null;
        if (canSacrifice)
        {
            sacrificeMajor = () => SacrificeRitual(true);
            sacrificeMinor = () => SacrificeRitual(false);
        }
        y = AddRitualRow(y, details.MajorRituals[0], sacrificeMajor, heartstringsFull);
        y = AddRitualRow(y, details.MinorRituals[0], sacrificeMinor, heartstringsFull);

        y = AddSectionHeader(y, "Trinkets");
        List<Trinket> trinkets = details.Trinkets[0];
        if (trinkets.Count == 0)
            y = AddEmptyRow(y, "None");
        foreach (Trinket trinket in trinkets)
        {
            Trinket captured = trinket;
            System.Action sacrificeTrinket = null;
            if (canSacrifice)
                sacrificeTrinket = () => SacrificeTrinket(captured);
            y = AddTrinketRow(y, trinket, sacrificeTrinket, heartstringsFull);
        }

        if (canSacrifice)
        {
            TextMeshProUGUI note = CloneTemplate(SacrificeNoteTemplate);
            note.text = heartstringsFull
                ? "Your heartstrings are full. The Styx offers nothing more."
                : "Gates Beyond: sacrifice a ritual or trinket to the Styx for a heartstring.";
            SetY(note.rectTransform, y - NoteTopPadding);
        }
    }

    void PopulateStyxSide()
    {
        StyxRunState styxState = Controller.Instance.StyxRunState;

        float y = 0f;
        y = AddSectionHeader(y, "Removed Rituals");
        if (styxState.RemovedRituals.Count == 0)
            y = AddEmptyRow(y, "None yet");
        foreach (Ritual ritual in styxState.RemovedRituals)
            y = AddRitualRow(y, ritual, null, false);

        y = AddSectionHeader(y, "Removed Trinkets");
        if (styxState.RemovedTrinkets.Count == 0)
            y = AddEmptyRow(y, "None yet");
        foreach (Trinket trinket in styxState.RemovedTrinkets)
            y = AddTrinketRow(y, trinket, null, false);
    }

    float AddSectionHeader(float y, string label)
    {
        TextMeshProUGUI header = CloneTemplate(SectionHeaderTemplate);
        header.gameObject.name = "Header_" + label;
        header.text = label;
        SetY(header.rectTransform, y - HeaderTopPadding);
        return y - HeaderTopPadding - header.rectTransform.sizeDelta.y - HeaderSpacing;
    }

    float AddEmptyRow(float y, string label)
    {
        TextMeshProUGUI text = CloneTemplate(EmptyRowTemplate);
        text.text = label;
        SetY(text.rectTransform, y);
        return y - text.rectTransform.sizeDelta.y - EmptyRowSpacing;
    }

    float AddRitualRow(float y, Ritual ritual, System.Action onSacrifice, bool sacrificeDisabled)
    {
        StatusRowView row = CloneTemplate(RitualRowTemplate);
        RectTransform rowRect = (RectTransform)row.transform;
        SetY(rowRect, y);

        row.NameText.text = ritual != null ? ritual.Name.Replace("\n", " ") : "(empty)";

        if (ritual != null)
            row.DescriptionText.text = ritual.Description;
        else
            row.DescriptionText.gameObject.SetActive(false);

        SetupSacrificeButton(row, ritual != null ? onSacrifice : null, sacrificeDisabled);

        return y - rowRect.sizeDelta.y - RowSpacing;
    }

    float AddTrinketRow(float y, Trinket trinket, System.Action onSacrifice, bool sacrificeDisabled)
    {
        StatusRowView row = CloneTemplate(TrinketRowTemplate);
        RectTransform rowRect = (RectTransform)row.transform;
        SetY(rowRect, y);

        PlayerEffectDescriptionData descriptionData = trinket.GetDescriptionData();
        Sprite icon = descriptionData != null ? descriptionData.Icon : null;
        if (icon != null)
            row.Icon.sprite = icon;
        else
            row.Icon.color = StyxUI.IconFallbackColor;

        row.NameText.text = trinket.Name;
        row.DescriptionText.text = trinket.Description;

        SetupSacrificeButton(row, onSacrifice, sacrificeDisabled);

        return y - rowRect.sizeDelta.y - RowSpacing;
    }

    void SetupSacrificeButton(StatusRowView row, System.Action onSacrifice, bool disabled)
    {
        if (row.SacrificeButton == null) return;

        if (onSacrifice == null)
        {
            row.SacrificeButton.gameObject.SetActive(false);
            return;
        }

        row.SacrificeButton.interactable = !disabled;
        row.SacrificeButton.onClick.AddListener(() => onSacrifice());
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

    void SacrificeRitual(bool major)
    {
        if (Controller.Instance.TrySacrificeRitualForHeartstring(major))
            Rebuild();
    }

    void SacrificeTrinket(Trinket trinket)
    {
        if (Controller.Instance.TrySacrificeTrinketForHeartstring(trinket))
            Rebuild();
    }
}
