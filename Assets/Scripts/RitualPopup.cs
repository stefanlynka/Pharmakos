using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RitualPopup : MonoBehaviour
{
    const string PortraitResourceFolder = "Images/Rituals";

    public TextMeshProUGUI Text;
    public Image RitualImage;

    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

    RectTransform _rectTransform;
    static Dictionary<string, Sprite> _portraits;

    public void SetRitual(Ritual ritual)
    {
        if (Text != null)
            Text.text = BuildSummaryText(ritual);

        if (RitualImage != null)
            RitualImage.sprite = LoadPortrait(ritual);
    }

    public void SetRaycastTargets(bool enabled)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = enabled;
    }

    static string BuildSummaryText(Ritual ritual)
    {
        if (ritual == null)
            return string.Empty;

        string name = ritual.Name ?? string.Empty;
        if (string.IsNullOrEmpty(ritual.ReminderText))
            return name;

        string reminder = Controller.Instance != null && Controller.Instance.TextHandler != null
            ? Controller.Instance.TextHandler.ProcessText(ritual.ReminderText)
            : ritual.ReminderText;

        return string.IsNullOrEmpty(reminder) ? name : name + "\n" + reminder;
    }

    static Sprite LoadPortrait(Ritual ritual)
    {
        if (ritual == null)
            return null;

        string typeName = ritual.GetType().Name;
        EnsurePortraits();
        if (_portraits.TryGetValue(typeName, out Sprite sprite))
            return sprite;

        Debug.LogWarning($"RitualPopup: no image at Resources/{PortraitResourceFolder}/{typeName}");
        _portraits[typeName] = null;
        return null;
    }

    static void EnsurePortraits()
    {
        if (_portraits != null)
            return;

        _portraits = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        Sprite[] sprites = Resources.LoadAll<Sprite>(PortraitResourceFolder);
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null)
                _portraits[sprite.name] = sprite;
        }
    }
}
