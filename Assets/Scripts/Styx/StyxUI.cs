using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime UGUI factory for the Styx feature screens (Styx node, trinket unlock,
/// run-start trinket select, status screen). Builds rough functional layouts so the
/// scene only needs minimal hand-authored objects; visuals can be polished in-editor later.
/// </summary>
public static class StyxUI
{
    public static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.1f, 0.92f);
    public static readonly Color BackgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
    public static readonly Color ButtonColor = new Color(0.23f, 0.27f, 0.37f, 1f);
    public static readonly Color ButtonSelectedColor = new Color(0.62f, 0.5f, 0.16f, 1f);
    public static readonly Color RowColor = new Color(0.14f, 0.16f, 0.22f, 0.9f);
    public static readonly Color TextColor = new Color(0.92f, 0.89f, 0.8f, 1f);
    public static readonly Color DimTextColor = new Color(0.7f, 0.68f, 0.6f, 1f);
    public static readonly Color IconFallbackColor = new Color(0.45f, 0.35f, 0.55f, 1f);

    public static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI layer
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static RectTransform CreateStretchedRect(Transform parent, string name)
    {
        RectTransform rect = CreateRect(parent, name);
        Stretch(rect);
        return rect;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>Anchors the rect to a single point (pivot = anchor) with the given offset and size.</summary>
    public static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static Image CreatePanel(Transform parent, string name, Color color)
    {
        RectTransform rect = CreateRect(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(parent, name);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = TextColor;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    public static Button CreateButton(Transform parent, string name, string label, float fontSize, Action onClick)
    {
        Image image = CreatePanel(parent, name, ButtonColor);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
            button.onClick.AddListener(() => onClick());

        TextMeshProUGUI text = CreateText(image.transform, "Label", label, fontSize, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    public static Image CreateIcon(Transform parent, string name, Sprite sprite)
    {
        Image image = CreatePanel(parent, name, Color.white);
        image.raycastTarget = false;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
        }
        else
        {
            image.color = IconFallbackColor;
        }
        return image;
    }

    public static void Clear(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }
}
