using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overworld status screen with two tabs:
///  - Current: the player's current deck, rituals, and trinkets. With the Gates Beyond
///    trinket, rituals/trinkets can be sacrificed to the Styx in exchange for a heartstring.
///  - Styx: the Styx deck, removed rituals, and removed trinkets that await at the river.
/// Lives on the StatusScreen GameObject; the deck is rendered via ContentScrollView.
/// </summary>
public class StatusScreenHandler : MonoBehaviour
{
    const string DeckViewerRenderTexturePath = "RenderTextures/DeckViewer";

    bool styxTabActive;

    RectTransform uiRoot;
    RectTransform sideContentRoot;
    TextMeshProUGUI titleText;
    TextMeshProUGUI heartstringsText;
    Image currentTabBackground;
    Image styxTabBackground;

    void Awake()
    {
        Screen screen = GetComponent<Screen>();
        if (screen != null)
            screen.Camera = null;
    }

    public void Open()
    {
        BuildUIIfNeeded();
        ShowTab(false);
    }

    public void Close()
    {
        ContentScrollView.Hide();
    }

    void BuildUIIfNeeded()
    {
        if (uiRoot != null) return;

        uiRoot = StyxUI.CreateStretchedRect(transform, "StatusUI");

        CreateDeckContentImage(uiRoot).transform.SetAsFirstSibling();

        currentTabBackground = (Image)StyxUI.CreateButton(uiRoot, "CurrentTab", "Current", 24, () => ShowTab(false)).targetGraphic;
        StyxUI.SetAnchored(currentTabBackground.rectTransform, new Vector2(0f, 1f), new Vector2(30, -25), new Vector2(180, 55));

        styxTabBackground = (Image)StyxUI.CreateButton(uiRoot, "StyxTab", "Styx", 24, () => ShowTab(true)).targetGraphic;
        StyxUI.SetAnchored(styxTabBackground.rectTransform, new Vector2(0f, 1f), new Vector2(225, -25), new Vector2(180, 55));

        titleText = StyxUI.CreateText(uiRoot, "Title", "", 36, TextAlignmentOptions.Center);
        StyxUI.SetAnchored(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(-100, -35), new Vector2(700, 55));

        Button closeButton = StyxUI.CreateButton(uiRoot, "CloseButton", "Close", 24, () => Controller.Instance.CloseStatusScreen());
        StyxUI.SetAnchored((RectTransform)closeButton.transform, new Vector2(1f, 0f), new Vector2(-415, 25), new Vector2(170, 55));

        // Right-side panel for rituals and trinkets; the deck render texture shows on the left.
        Image sidePanel = StyxUI.CreatePanel(uiRoot, "SidePanel", StyxUI.PanelColor);
        sidePanel.rectTransform.anchorMin = new Vector2(1f, 0f);
        sidePanel.rectTransform.anchorMax = new Vector2(1f, 1f);
        sidePanel.rectTransform.pivot = new Vector2(1f, 0.5f);
        sidePanel.rectTransform.anchoredPosition = Vector2.zero;
        sidePanel.rectTransform.sizeDelta = new Vector2(400, 0);

        sideContentRoot = StyxUI.CreateRect(sidePanel.transform, "SideContent");
        sideContentRoot.anchorMin = new Vector2(0f, 0f);
        sideContentRoot.anchorMax = new Vector2(1f, 1f);
        sideContentRoot.offsetMin = new Vector2(15, 15);
        sideContentRoot.offsetMax = new Vector2(-15, -15);

        heartstringsText = StyxUI.CreateText(uiRoot, "Heartstrings", "", 24, TextAlignmentOptions.Left);
        StyxUI.SetAnchored(heartstringsText.rectTransform, new Vector2(0f, 0f), new Vector2(30, 25), new Vector2(500, 40));
    }

    void ShowTab(bool styxTab)
    {
        styxTabActive = styxTab;
        Rebuild();
    }

    void Rebuild()
    {
        currentTabBackground.color = styxTabActive ? StyxUI.ButtonColor : StyxUI.ButtonSelectedColor;
        styxTabBackground.color = styxTabActive ? StyxUI.ButtonSelectedColor : StyxUI.ButtonColor;
        titleText.text = styxTabActive ? "What Waits At The Styx" : "Your Current Possessions";
        heartstringsText.text = "Heartstrings: " + Controller.Instance.RunHeartStrings + " / " + Player.MaxHeartStrings;

        ContentScrollView.ShowCards(GetSortedDeck(styxTabActive
            ? Controller.Instance.StyxRunState.GetStyxDeck()
            : Controller.Instance.HumanPlayerDetails.DeckBlueprint[0]));

        StyxUI.Clear(sideContentRoot);
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
        y = AddRitualRow(y, details.MajorRituals[0], "Major", sacrificeMajor, heartstringsFull);
        y = AddRitualRow(y, details.MinorRituals[0], "Minor", sacrificeMinor, heartstringsFull);

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
            TextMeshProUGUI note = StyxUI.CreateText(sideContentRoot, "SacrificeNote",
                heartstringsFull
                    ? "Your heartstrings are full. The Styx offers nothing more."
                    : "Gates Beyond: sacrifice a ritual or trinket to the Styx for a heartstring.",
                17, TextAlignmentOptions.TopLeft);
            note.color = StyxUI.DimTextColor;
            StyxUI.SetAnchored(note.rectTransform, new Vector2(0f, 1f), new Vector2(0, y - 8), new Vector2(370, 60));
        }
    }

    void PopulateStyxSide()
    {
        StyxRunState styxState = Controller.Instance.StyxRunState;

        float y = 0f;
        y = AddSectionHeader(y, "Removed Rituals"); // (choose 2 at the Styx)");
        if (styxState.RemovedRituals.Count == 0)
            y = AddEmptyRow(y, "None yet");
        foreach (Ritual ritual in styxState.RemovedRituals)
            y = AddRitualRow(y, ritual, "", null, false);

        y = AddSectionHeader(y, "Removed Trinkets");
        if (styxState.RemovedTrinkets.Count == 0)
            y = AddEmptyRow(y, "None yet");
        foreach (Trinket trinket in styxState.RemovedTrinkets)
            y = AddTrinketRow(y, trinket, null, false);
    }

    float AddSectionHeader(float y, string label)
    {
        TextMeshProUGUI header = StyxUI.CreateText(sideContentRoot, "Header_" + label, label, 23, TextAlignmentOptions.Left);
        StyxUI.SetAnchored(header.rectTransform, new Vector2(0f, 1f), new Vector2(0, y - 6), new Vector2(370, 32));
        return y - 44f;
    }

    float AddEmptyRow(float y, string label)
    {
        TextMeshProUGUI text = StyxUI.CreateText(sideContentRoot, "Empty", label, 19, TextAlignmentOptions.Left);
        text.color = StyxUI.DimTextColor;
        StyxUI.SetAnchored(text.rectTransform, new Vector2(0f, 1f), new Vector2(10, y), new Vector2(350, 28));
        return y - 36f;
    }

    float AddRitualRow(float y, Ritual ritual, string slotLabel, System.Action onSacrifice, bool sacrificeDisabled)
    {
        const float rowHeight = 64f;
        Image row = StyxUI.CreatePanel(sideContentRoot, "RitualRow", StyxUI.RowColor);
        row.rectTransform.anchorMin = new Vector2(0f, 1f);
        row.rectTransform.anchorMax = new Vector2(1f, 1f);
        row.rectTransform.pivot = new Vector2(0.5f, 1f);
        row.rectTransform.anchoredPosition = new Vector2(0, y);
        row.rectTransform.sizeDelta = new Vector2(0, rowHeight);

        string name = ritual != null ? ritual.Name : "(empty)";
        name = name.Replace("\n", " ");
        string prefix = string.IsNullOrEmpty(slotLabel) ? "" : slotLabel + ": ";
        TextMeshProUGUI nameText = StyxUI.CreateText(row.transform, "Name", name, 19, TextAlignmentOptions.Left);
        StyxUI.SetAnchored(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(10, -4), new Vector2(355, 26));

        if (ritual != null)
        {
            TextMeshProUGUI descriptionText = StyxUI.CreateText(row.transform, "Description", ritual.Description, 14, TextAlignmentOptions.TopLeft);
            descriptionText.color = StyxUI.DimTextColor;
            StyxUI.SetAnchored(descriptionText.rectTransform, new Vector2(0f, 1f), new Vector2(10, -30), new Vector2(255, 32));
        }

        if (ritual != null && onSacrifice != null)
            AddSacrificeButton(row.transform, onSacrifice, sacrificeDisabled);

        return y - rowHeight - 10f;
    }

    float AddTrinketRow(float y, Trinket trinket, System.Action onSacrifice, bool sacrificeDisabled)
    {
        const float rowHeight = 64f;
        Image row = StyxUI.CreatePanel(sideContentRoot, "TrinketRow", StyxUI.RowColor);
        row.rectTransform.anchorMin = new Vector2(0f, 1f);
        row.rectTransform.anchorMax = new Vector2(1f, 1f);
        row.rectTransform.pivot = new Vector2(0.5f, 1f);
        row.rectTransform.anchoredPosition = new Vector2(0, y);
        row.rectTransform.sizeDelta = new Vector2(0, rowHeight);

        PlayerEffectDescriptionData descriptionData = trinket.GetDescriptionData();
        Image icon = StyxUI.CreateIcon(row.transform, "Icon", descriptionData != null ? descriptionData.Icon : null);
        StyxUI.SetAnchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(8, 0), new Vector2(48, 48));

        TextMeshProUGUI nameText = StyxUI.CreateText(row.transform, "Name", trinket.Name, 19, TextAlignmentOptions.Left);
        StyxUI.SetAnchored(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(64, -4), new Vector2(300, 26));

        TextMeshProUGUI descriptionText = StyxUI.CreateText(row.transform, "Description", trinket.Description, 14, TextAlignmentOptions.TopLeft);
        descriptionText.color = StyxUI.DimTextColor;
        // descriptionText.autoSizeTextContainer = true;
        descriptionText.fontSizeMin = 6;
        descriptionText.enableAutoSizing = true;
        StyxUI.SetAnchored(descriptionText.rectTransform, new Vector2(0f, 1f), new Vector2(64, -30), new Vector2(200, 32));

        if (onSacrifice != null)
            AddSacrificeButton(row.transform, onSacrifice, sacrificeDisabled);

        return y - rowHeight - 10f;
    }

    void AddSacrificeButton(Transform row, System.Action onSacrifice, bool disabled)
    {
        Button button = StyxUI.CreateButton(row, "SacrificeButton", "Sacrifice", 16, onSacrifice);
        StyxUI.SetAnchored((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-8, 0), new Vector2(95, 42));
        button.interactable = !disabled;
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

    static RawImage CreateDeckContentImage(Transform parent)
    {
        RectTransform rect = StyxUI.CreateRect(parent, "DeckContent");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(15f, 60f);
        rect.offsetMax = new Vector2(-415f, -80f);

        RawImage rawImage = rect.gameObject.AddComponent<RawImage>();
        rawImage.raycastTarget = false;

        RenderTexture texture = Resources.Load<RenderTexture>(DeckViewerRenderTexturePath);
        if (texture != null)
            rawImage.texture = texture;

        return rawImage;
    }
}
