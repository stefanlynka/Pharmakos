using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug panel on the pause menu for toggling which boss kills are considered unlocked
/// (Strings of Fate / Gates Beyond) when testing Styx relics.
/// </summary>
public class PauseStyxUnlocksHandler : MonoBehaviour
{
    class ToggleEntry
    {
        public string BossName;
        public string TrinketName;
        public System.Func<bool> IsUnlocked;
        public System.Action<bool> SetUnlocked;
        public TextMeshProUGUI StateLabel;
        public Image Background;
    }

    readonly ToggleEntry[] entries = new ToggleEntry[2];

    RectTransform uiRoot;
    bool pausePanelVisible;

    void Start()
    {
        entries[0] = new ToggleEntry
        {
            BossName = "Fates",
            TrinketName = "Strings of Fate",
            IsUnlocked = () => StyxUnlocks.StringsOfFateUnlocked,
            SetUnlocked = StyxUnlocks.SetStringsOfFateUnlocked,
        };
        entries[1] = new ToggleEntry
        {
            BossName = "The Gate",
            TrinketName = "Gates Beyond",
            IsUnlocked = () => StyxUnlocks.GatesBeyondUnlocked,
            SetUnlocked = StyxUnlocks.SetGatesBeyondUnlocked,
        };

        BuildUIIfNeeded();
    }

    void Update()
    {
        bool shouldShow = Controller.Instance != null
            && Controller.Instance.GamePaused
            && ScreenHandler.Instance != null
            && ScreenHandler.Instance.CurrentScreen != null
            && ScreenHandler.Instance.CurrentScreen.Name == ScreenName.Pause;

        if (shouldShow && !pausePanelVisible)
            Refresh();

        pausePanelVisible = shouldShow;
    }

    void BuildUIIfNeeded()
    {
        if (uiRoot != null) return;

        uiRoot = StyxUI.CreateRect(transform, "StyxUnlocksPanel");
        StyxUI.SetAnchored(uiRoot, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(720, 220));

        Image panel = StyxUI.CreatePanel(uiRoot, "Background", StyxUI.PanelColor);
        StyxUI.Stretch(panel.rectTransform);

        TextMeshProUGUI header = StyxUI.CreateText(uiRoot, "Header", "Styx Boss Unlocks (Test)", 22, TextAlignmentOptions.Center);
        header.color = StyxUI.DimTextColor;
        StyxUI.SetAnchored(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(680, 32));

        const float rowHeight = 72f;
        const float rowSpacing = 12f;
        float y = -56f - rowHeight * 0.5f;

        for (int i = 0; i < entries.Length; i++)
        {
            ToggleEntry entry = entries[i];

            Image row = StyxUI.CreatePanel(uiRoot, "Row_" + entry.BossName, StyxUI.RowColor);
            StyxUI.SetAnchored(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, y), new Vector2(680, rowHeight));
            entry.Background = row;

            TextMeshProUGUI bossText = StyxUI.CreateText(row.transform, "Boss", entry.BossName, 24, TextAlignmentOptions.Left);
            StyxUI.SetAnchored(bossText.rectTransform, new Vector2(0f, 0.5f), new Vector2(20, 14), new Vector2(200, 34));

            TextMeshProUGUI trinketText = StyxUI.CreateText(row.transform, "Trinket", entry.TrinketName, 19, TextAlignmentOptions.Left);
            trinketText.color = StyxUI.DimTextColor;
            StyxUI.SetAnchored(trinketText.rectTransform, new Vector2(0f, 0.5f), new Vector2(20, -18), new Vector2(320, 28));

            Button toggleButton = StyxUI.CreateButton(row.transform, "Toggle", "", 20, () => ToggleAnEntry(entry));
            StyxUI.SetAnchored((RectTransform)toggleButton.transform, new Vector2(1f, 0.5f), new Vector2(-20, 0), new Vector2(200, 52));
            entry.StateLabel = toggleButton.GetComponentInChildren<TextMeshProUGUI>();

            y -= rowHeight + rowSpacing;
        }

        Refresh();
    }

    void ToggleAnEntry(ToggleEntry entry)
    {
        entry.SetUnlocked(!entry.IsUnlocked());
        Refresh();
    }

    void Refresh()
    {
        foreach (ToggleEntry entry in entries)
        {
            if (entry.StateLabel == null) continue;

            bool unlocked = entry.IsUnlocked();
            entry.StateLabel.text = unlocked ? "Defeated" : "Not Defeated";
            if (entry.Background != null)
                entry.Background.color = unlocked ? StyxUI.ButtonSelectedColor : StyxUI.RowColor;
        }
    }
}
