using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The River Styx node screen. Replaces the player's deck with their Styx deck,
/// replaces their trinkets with the trinkets removed during the run, and lets them
/// choose up to two removed rituals (first pick = Major, second pick = Minor).
/// The static layout is authored in the scene under the StyxScreen GameObject;
/// ritual rows and trinket entries are cloned from inactive scene templates.
/// The 3D card grid lives in the StyxArea.
/// </summary>
public class StyxScreenHandler : MonoBehaviour
{
    [Tooltip("Root of the 3D area that shows the Styx deck (activated while the screen is open).")]
    public GameObject StyxArea;
    [Tooltip("Card grid inside the StyxArea used to display the Styx deck.")]
    public ViewCardScroller CardScroller;

    [Header("Scene References")]
    public RectTransform RitualListRoot;
    public RectTransform TrinketListRoot;
    public Button ContinueButton;
    public TextMeshProUGUI EmptyRitualsText;
    public TextMeshProUGUI EmptyTrinketsText;

    [Header("Templates (inactive in scene, cloned at runtime)")]
    public StatusRowView RitualRowTemplate;
    public StatusRowView TrinketEntryTemplate;

    [Header("Row Colors")]
    public Color RowColor = new Color(0.14f, 0.16f, 0.22f, 0.9f);
    public Color RowSelectedColor = new Color(0.62f, 0.5f, 0.16f, 1f);

    const float RitualRowSpacing = 12f;

    class RitualEntry
    {
        public Ritual Ritual;
        public Image Background;
        public TextMeshProUGUI SlotLabel;
    }

    readonly List<RitualEntry> ritualEntries = new List<RitualEntry>();
    readonly List<Ritual> selectedRituals = new List<Ritual>();

    void Awake()
    {
        ContinueButton.onClick.AddListener(Continue);
    }

    public void BeginStyx()
    {
        if (StyxArea != null) StyxArea.SetActive(true);

        selectedRituals.Clear();

        if (CardScroller != null)
            CardScroller.Load(Controller.Instance.StyxRunState.GetStyxDeck());

        PopulateRituals();
        PopulateTrinkets();
    }

    void PopulateRituals()
    {
        StyxUI.Clear(RitualListRoot);
        ritualEntries.Clear();

        List<Ritual> removedRituals = Controller.Instance.StyxRunState.RemovedRituals;
        EmptyRitualsText.gameObject.SetActive(removedRituals.Count == 0);
        if (removedRituals.Count == 0) return;

        float rowHeight = ((RectTransform)RitualRowTemplate.transform).sizeDelta.y;
        float y = -rowHeight * 0.5f;

        foreach (Ritual ritual in removedRituals)
        {
            StatusRowView row = CloneTemplate(RitualRowTemplate, RitualListRoot);
            row.gameObject.name = "Ritual_" + ritual.Name;

            RectTransform rowRect = (RectTransform)row.transform;
            rowRect.anchoredPosition = new Vector2(rowRect.anchoredPosition.x, y);

            row.NameText.text = ritual.Name;
            row.DescriptionText.text = ritual.Description;

            RitualEntry entry = new RitualEntry
            {
                Ritual = ritual,
                Background = row.Background,
                SlotLabel = row.TagText
            };
            row.RowButton.onClick.AddListener(() => RitualClicked(entry));

            ritualEntries.Add(entry);
            y -= rowHeight + RitualRowSpacing;
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
            entry.Background.color = selected ? RowSelectedColor : RowColor;
            entry.SlotLabel.text = index == 0 ? "Major" : index == 1 ? "Minor" : "";
        }
    }

    void PopulateTrinkets()
    {
        StyxUI.Clear(TrinketListRoot);

        List<Trinket> removedTrinkets = Controller.Instance.StyxRunState.RemovedTrinkets;
        EmptyTrinketsText.gameObject.SetActive(removedTrinkets.Count == 0);
        if (removedTrinkets.Count == 0) return;

        RectTransform templateRect = (RectTransform)TrinketEntryTemplate.transform;
        float x = templateRect.anchoredPosition.x;
        float step = templateRect.sizeDelta.x;

        foreach (Trinket trinket in removedTrinkets)
        {
            StatusRowView entry = CloneTemplate(TrinketEntryTemplate, TrinketListRoot);
            entry.gameObject.name = "Trinket_" + trinket.Name;

            RectTransform rect = (RectTransform)entry.transform;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);

            PlayerEffectDescriptionData descriptionData = trinket.GetDescriptionData();
            Sprite icon = descriptionData != null ? descriptionData.Icon : null;
            if (icon != null)
                entry.Icon.sprite = icon;
            else
                entry.Icon.color = StyxUI.IconFallbackColor;

            entry.NameText.text = trinket.Name;

            x += step;
        }
    }

    static StatusRowView CloneTemplate(StatusRowView template, RectTransform parent)
    {
        StatusRowView clone = Object.Instantiate(template, parent);
        clone.gameObject.SetActive(true);
        return clone;
    }

    public void Continue()
    {
        Controller.Instance.ApplyStyxExchange(new List<Ritual>(selectedRituals));

        if (CardScroller != null) CardScroller.Exit();
        if (StyxArea != null) StyxArea.SetActive(false);

        Controller.Instance.StartNextLevel();
    }
}
