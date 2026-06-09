using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shown at the start of a run (after the starting devotion is chosen) when the player
/// has unlocked at least one Styx trinket. Lets them pick which unlocked trinkets
/// (both, one, or neither) to start the run with. Lives on the StyxTrinketSelectScreen
/// GameObject; UI is built at runtime under the screen's CanvasGroup.
/// </summary>
public class StyxTrinketSelectHandler : MonoBehaviour
{
    class TrinketEntry
    {
        public Trinket Trinket;
        public bool Selected;
        public Image Background;
        public TextMeshProUGUI StateLabel;
    }

    readonly List<TrinketEntry> entries = new List<TrinketEntry>();

    RectTransform uiRoot;
    RectTransform listRoot;

    public void Show()
    {
        BuildUIIfNeeded();
        PopulateList();
    }

    void BuildUIIfNeeded()
    {
        if (uiRoot != null) return;

        uiRoot = StyxUI.CreateStretchedRect(transform, "StyxTrinketSelectUI");

        Image background = StyxUI.CreatePanel(uiRoot, "Background", StyxUI.BackgroundColor);
        StyxUI.Stretch(background.rectTransform);

        TextMeshProUGUI header = StyxUI.CreateText(uiRoot, "Header", "Gifts Of The Styx", 44, TextAlignmentOptions.Center);
        StyxUI.SetAnchored(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(1200, 70));

        TextMeshProUGUI subtitle = StyxUI.CreateText(uiRoot, "Subtitle",
            "Choose which unlocked Styx trinkets to begin this run with. You may take both, one, or neither.",
            24, TextAlignmentOptions.Center);
        subtitle.color = StyxUI.DimTextColor;
        StyxUI.SetAnchored(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(1200, 50));

        listRoot = StyxUI.CreateRect(uiRoot, "TrinketList");
        StyxUI.SetAnchored(listRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(820, 360));

        Button continueButton = StyxUI.CreateButton(uiRoot, "ContinueButton", "Begin Run", 28, Continue);
        StyxUI.SetAnchored((RectTransform)continueButton.transform, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(260, 70));
    }

    void PopulateList()
    {
        StyxUI.Clear(listRoot);
        entries.Clear();

        List<Trinket> unlockedTrinkets = StyxUnlocks.GetUnlockedTrinkets();
        const float rowHeight = 150f;
        const float rowSpacing = 20f;
        float totalHeight = unlockedTrinkets.Count * rowHeight + Mathf.Max(0, unlockedTrinkets.Count - 1) * rowSpacing;
        float y = totalHeight * 0.5f - rowHeight * 0.5f;

        foreach (Trinket trinket in unlockedTrinkets)
        {
            TrinketEntry entry = new TrinketEntry { Trinket = trinket };

            Image row = StyxUI.CreatePanel(listRoot, "Row_" + trinket.Name, StyxUI.RowColor);
            StyxUI.SetAnchored(row.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(820, rowHeight));
            entry.Background = row;

            Button rowButton = row.gameObject.AddComponent<Button>();
            rowButton.targetGraphic = row;
            rowButton.onClick.AddListener(() => ToggleEntry(entry));

            PlayerEffectDescriptionData descriptionData = trinket.GetDescriptionData();
            Image icon = StyxUI.CreateIcon(row.transform, "Icon", descriptionData != null ? descriptionData.Icon : null);
            StyxUI.SetAnchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(20, 0), new Vector2(110, 110));

            TextMeshProUGUI nameText = StyxUI.CreateText(row.transform, "Name", trinket.Name, 28, TextAlignmentOptions.Left);
            StyxUI.SetAnchored(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(150, -18), new Vector2(480, 40));

            TextMeshProUGUI descriptionText = StyxUI.CreateText(row.transform, "Description", trinket.Description, 21, TextAlignmentOptions.TopLeft);
            descriptionText.color = StyxUI.DimTextColor;
            StyxUI.SetAnchored(descriptionText.rectTransform, new Vector2(0f, 1f), new Vector2(150, -62), new Vector2(480, 80));

            TextMeshProUGUI stateLabel = StyxUI.CreateText(row.transform, "State", "Take?", 24, TextAlignmentOptions.Center);
            StyxUI.SetAnchored(stateLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(140, 40));
            entry.StateLabel = stateLabel;

            entries.Add(entry);
            y -= rowHeight + rowSpacing;
        }

        RefreshVisuals();
    }

    void ToggleEntry(TrinketEntry entry)
    {
        entry.Selected = !entry.Selected;
        RefreshVisuals();
    }

    void RefreshVisuals()
    {
        foreach (TrinketEntry entry in entries)
        {
            entry.Background.color = entry.Selected ? StyxUI.ButtonSelectedColor : StyxUI.RowColor;
            entry.StateLabel.text = entry.Selected ? "Taken" : "Take?";
        }
    }

    public void Continue()
    {
        foreach (TrinketEntry entry in entries)
        {
            if (entry.Selected)
                Controller.Instance.AddTrinket(entry.Trinket);
        }

        ScreenHandler.Instance.HideScreen(ScreenName.StyxTrinketSelect, true);
        Controller.Instance.StartGame();
    }
}
