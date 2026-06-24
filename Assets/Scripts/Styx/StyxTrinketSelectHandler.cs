using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shown at the start of a run (after the starting devotion is chosen) when the player
/// has unlocked at least one Styx trinket. Lets them pick which unlocked trinkets
/// (both, one, or neither) to start the run with. The static layout is authored in the
/// scene under the StyxTrinketSelectScreen GameObject; trinket rows are cloned from an
/// inactive scene template.
/// </summary>
public class StyxTrinketSelectHandler : MonoBehaviour
{
    [Header("Scene References")]
    public RectTransform TrinketListRoot;
    public Button ContinueButton;

    [Header("Template (inactive in scene, cloned at runtime)")]
    public StatusRowView TrinketRowTemplate;

    [Header("Row Colors")]
    public Color RowColor = new Color(0.14f, 0.16f, 0.22f, 0.9f);
    public Color RowSelectedColor = new Color(0.62f, 0.5f, 0.16f, 1f);

    const float RowSpacing = 20f;

    class TrinketEntry
    {
        public Trinket Trinket;
        public bool Selected;
        public Image Background;
        public TextMeshProUGUI StateLabel;
    }

    readonly List<TrinketEntry> entries = new List<TrinketEntry>();

    void Awake()
    {
        ContinueButton.onClick.AddListener(Continue);
    }

    public void Show()
    {
        PopulateList();
    }

    void PopulateList()
    {
        StyxUI.Clear(TrinketListRoot);
        entries.Clear();

        List<Trinket> unlockedTrinkets = StyxUnlocks.GetUnlockedTrinkets();
        float rowHeight = ((RectTransform)TrinketRowTemplate.transform).sizeDelta.y;
        float totalHeight = unlockedTrinkets.Count * rowHeight + Mathf.Max(0, unlockedTrinkets.Count - 1) * RowSpacing;
        float y = totalHeight * 0.5f - rowHeight * 0.5f;

        foreach (Trinket trinket in unlockedTrinkets)
        {
            StatusRowView row = Instantiate(TrinketRowTemplate, TrinketListRoot);
            row.gameObject.SetActive(true);
            row.gameObject.name = "Row_" + trinket.Name;

            RectTransform rowRect = (RectTransform)row.transform;
            rowRect.anchoredPosition = new Vector2(rowRect.anchoredPosition.x, y);

            PlayerEffectDescriptionData descriptionData = trinket.GetDescriptionData();
            Sprite icon = descriptionData != null ? descriptionData.Icon : null;
            if (icon != null)
                row.Icon.sprite = icon;
            else
                row.Icon.color = StyxUI.IconFallbackColor;

            row.NameText.text = trinket.Name;
            row.DescriptionText.text = trinket.Description;

            TrinketEntry entry = new TrinketEntry
            {
                Trinket = trinket,
                Background = row.Background,
                StateLabel = row.TagText
            };
            row.RowButton.onClick.AddListener(() => ToggleEntry(entry));

            entries.Add(entry);
            y -= rowHeight + RowSpacing;
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
            entry.Background.color = entry.Selected ? RowSelectedColor : RowColor;
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

        Controller.Instance.StartGame();
    }
}
