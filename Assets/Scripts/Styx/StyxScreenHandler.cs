using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The River Styx node screen. Replaces the player's deck with their Styx deck,
/// replaces their trinkets with the trinkets removed during the run, and lets them
/// choose up to two removed rituals (first pick = Major, second pick = Minor).
/// Lives on the StyxScreen GameObject; the 3D card grid lives in the StyxArea.
/// </summary>
public class StyxScreenHandler : MonoBehaviour
{
    [Tooltip("Root of the 3D area that shows the Styx deck (activated while the screen is open).")]
    public GameObject StyxArea;
    [Tooltip("Card grid inside the StyxArea used to display the Styx deck.")]
    public ViewCardScroller CardScroller;

    class RitualEntry
    {
        public Ritual Ritual;
        public Image Background;
        public TextMeshProUGUI SlotLabel;
    }

    readonly List<RitualEntry> ritualEntries = new List<RitualEntry>();
    readonly List<Ritual> selectedRituals = new List<Ritual>();

    RectTransform uiRoot;
    RectTransform ritualListRoot;
    RectTransform trinketListRoot;
    TextMeshProUGUI ritualsHeader;
    TextMeshProUGUI trinketsHeader;

    public void BeginStyx()
    {
        if (StyxArea != null) StyxArea.SetActive(true);

        BuildUIIfNeeded();
        selectedRituals.Clear();

        if (CardScroller != null)
            CardScroller.Load(Controller.Instance.StyxRunState.GetStyxDeck());

        PopulateRituals();
        PopulateTrinkets();
    }

    void BuildUIIfNeeded()
    {
        if (uiRoot != null) return;

        uiRoot = StyxUI.CreateStretchedRect(transform, "StyxUI");

        TextMeshProUGUI header = StyxUI.CreateText(uiRoot, "Header", "The River Styx", 44, TextAlignmentOptions.Center);
        StyxUI.SetAnchored(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(-160, -45), new Vector2(900, 60));

        TextMeshProUGUI subtitle = StyxUI.CreateText(uiRoot, "Subtitle",
            "All you have cast aside returns to you, and all you carry is washed away.\nThis becomes your deck. The trinkets below become your trinkets. Choose up to two rituals.",
            21, TextAlignmentOptions.Center);
        subtitle.color = StyxUI.DimTextColor;
        StyxUI.SetAnchored(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(-160, -105), new Vector2(1100, 60));

        // Right-side ritual selection panel.
        Image sidePanel = StyxUI.CreatePanel(uiRoot, "RitualPanel", StyxUI.PanelColor);
        sidePanel.rectTransform.anchorMin = new Vector2(1f, 0f);
        sidePanel.rectTransform.anchorMax = new Vector2(1f, 1f);
        sidePanel.rectTransform.pivot = new Vector2(1f, 0.5f);
        sidePanel.rectTransform.anchoredPosition = Vector2.zero;
        sidePanel.rectTransform.sizeDelta = new Vector2(380, 0);

        ritualsHeader = StyxUI.CreateText(sidePanel.transform, "RitualsHeader", "Choose Rituals", 28, TextAlignmentOptions.Center);
        StyxUI.SetAnchored(ritualsHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(340, 40));

        ritualListRoot = StyxUI.CreateRect(sidePanel.transform, "RitualList");
        ritualListRoot.anchorMin = new Vector2(0f, 0f);
        ritualListRoot.anchorMax = new Vector2(1f, 1f);
        ritualListRoot.offsetMin = new Vector2(15, 100);
        ritualListRoot.offsetMax = new Vector2(-15, -70);

        Button continueButton = StyxUI.CreateButton(sidePanel.transform, "ContinueButton", "Cross The Styx", 24, Continue);
        StyxUI.SetAnchored((RectTransform)continueButton.transform, new Vector2(0.5f, 0f), new Vector2(0, 25), new Vector2(280, 60));

        // Bottom strip listing the trinkets the player will receive.
        Image bottomPanel = StyxUI.CreatePanel(uiRoot, "TrinketPanel", StyxUI.PanelColor);
        bottomPanel.rectTransform.anchorMin = new Vector2(0f, 0f);
        bottomPanel.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottomPanel.rectTransform.pivot = new Vector2(0.5f, 0f);
        bottomPanel.rectTransform.offsetMin = new Vector2(0, 0);
        bottomPanel.rectTransform.offsetMax = new Vector2(-380, 130);

        trinketsHeader = StyxUI.CreateText(bottomPanel.transform, "TrinketsHeader", "Your New Trinkets", 22, TextAlignmentOptions.Left);
        StyxUI.SetAnchored(trinketsHeader.rectTransform, new Vector2(0f, 1f), new Vector2(20, -8), new Vector2(400, 30));

        trinketListRoot = StyxUI.CreateRect(bottomPanel.transform, "TrinketList");
        trinketListRoot.anchorMin = new Vector2(0f, 0f);
        trinketListRoot.anchorMax = new Vector2(1f, 1f);
        trinketListRoot.offsetMin = new Vector2(20, 8);
        trinketListRoot.offsetMax = new Vector2(-20, -40);
    }

    void PopulateRituals()
    {
        StyxUI.Clear(ritualListRoot);
        ritualEntries.Clear();

        List<Ritual> removedRituals = Controller.Instance.StyxRunState.RemovedRituals;
        if (removedRituals.Count == 0)
        {
            TextMeshProUGUI empty = StyxUI.CreateText(ritualListRoot, "Empty",
                "You removed no rituals this run.\nYou will cross with none.", 22, TextAlignmentOptions.Center);
            empty.color = StyxUI.DimTextColor;
            StyxUI.SetAnchored(empty.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(330, 80));
            return;
        }

        const float rowHeight = 92f;
        const float rowSpacing = 12f;
        float y = -rowHeight * 0.5f;

        foreach (Ritual ritual in removedRituals)
        {
            RitualEntry entry = new RitualEntry { Ritual = ritual };

            Image row = StyxUI.CreatePanel(ritualListRoot, "Ritual_" + ritual.Name, StyxUI.RowColor);
            row.rectTransform.anchorMin = new Vector2(0f, 1f);
            row.rectTransform.anchorMax = new Vector2(1f, 1f);
            row.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            row.rectTransform.anchoredPosition = new Vector2(0, y);
            row.rectTransform.sizeDelta = new Vector2(0, rowHeight);
            entry.Background = row;

            Button rowButton = row.gameObject.AddComponent<Button>();
            rowButton.targetGraphic = row;
            rowButton.onClick.AddListener(() => RitualClicked(entry));

            TextMeshProUGUI nameText = StyxUI.CreateText(row.transform, "Name", ritual.Name, 23, TextAlignmentOptions.Left);
            StyxUI.SetAnchored(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(12, -6), new Vector2(240, 30));

            TextMeshProUGUI descriptionText = StyxUI.CreateText(row.transform, "Description", ritual.Description, 17, TextAlignmentOptions.TopLeft);
            descriptionText.color = StyxUI.DimTextColor;
            StyxUI.SetAnchored(descriptionText.rectTransform, new Vector2(0f, 1f), new Vector2(12, -38), new Vector2(250, 50));

            TextMeshProUGUI slotLabel = StyxUI.CreateText(row.transform, "Slot", "", 19, TextAlignmentOptions.Center);
            StyxUI.SetAnchored(slotLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-8, 0), new Vector2(80, 30));
            entry.SlotLabel = slotLabel;

            ritualEntries.Add(entry);
            y -= rowHeight + rowSpacing;
        }

        RefreshRitualVisuals();
    }

    void RitualClicked(RitualEntry entry)
    {
        if (selectedRituals.Contains(entry.Ritual))
        {
            selectedRituals.Remove(entry.Ritual);
        }
        else
        {
            if (selectedRituals.Count >= 2)
                selectedRituals.RemoveAt(0);
            selectedRituals.Add(entry.Ritual);
        }

        RefreshRitualVisuals();
    }

    void RefreshRitualVisuals()
    {
        foreach (RitualEntry entry in ritualEntries)
        {
            int index = selectedRituals.IndexOf(entry.Ritual);
            bool selected = index >= 0;
            entry.Background.color = selected ? StyxUI.ButtonSelectedColor : StyxUI.RowColor;
            entry.SlotLabel.text = index == 0 ? "Major" : index == 1 ? "Minor" : "";
        }
    }

    void PopulateTrinkets()
    {
        StyxUI.Clear(trinketListRoot);

        List<Trinket> removedTrinkets = Controller.Instance.StyxRunState.RemovedTrinkets;
        if (removedTrinkets.Count == 0)
        {
            TextMeshProUGUI empty = StyxUI.CreateText(trinketListRoot, "Empty",
                "You removed no trinkets this run. You will cross with none.", 20, TextAlignmentOptions.Left);
            empty.color = StyxUI.DimTextColor;
            StyxUI.SetAnchored(empty.rectTransform, new Vector2(0f, 0.5f), new Vector2(0, 0), new Vector2(800, 40));
            return;
        }

        float x = 35f;
        foreach (Trinket trinket in removedTrinkets)
        {
            PlayerEffectDescriptionData descriptionData = trinket.GetDescriptionData();
            Image icon = StyxUI.CreateIcon(trinketListRoot, "Trinket_" + trinket.Name, descriptionData != null ? descriptionData.Icon : null);
            StyxUI.SetAnchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(x - 35f, 8), new Vector2(70, 70));

            TextMeshProUGUI nameText = StyxUI.CreateText(trinketListRoot, "Name_" + trinket.Name, trinket.Name, 14, TextAlignmentOptions.Center);
            StyxUI.SetAnchored(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(x - 55f, -38), new Vector2(110, 30));

            x += 120f;
        }
    }

    public void Continue()
    {
        Controller.Instance.ApplyStyxExchange(new List<Ritual>(selectedRituals));

        if (CardScroller != null) CardScroller.Exit();
        if (StyxArea != null) StyxArea.SetActive(false);

        Controller.Instance.StartNextLevel();
    }
}
