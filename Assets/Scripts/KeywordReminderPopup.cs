using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeywordReminderPopup : MonoBehaviour
{
    public const float FrameInset = 35f;

    public TextMeshProUGUI Text;
    public float MaxTextWidth = 250f;

    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

    RectTransform _rectTransform;

    public void SetText(string text)
    {
        if (Text == null)
            return;

        Text.text = text ?? string.Empty;
        Text.enableAutoSizing = true;

        float measureSize = Text.fontSizeMax > 0f ? Text.fontSizeMax : Text.fontSize;
        Text.enableAutoSizing = false;
        Text.fontSize = measureSize;
        Text.ForceMeshUpdate();

        float textWidth = Mathf.Min(Text.GetPreferredValues(Text.text).x, MaxTextWidth);
        float textHeight = Text.GetPreferredValues(Text.text, textWidth, 0f).y;

        RectTransform.sizeDelta = new Vector2(
            Mathf.Ceil(textWidth + FrameInset * 2f),
            Mathf.Ceil(textHeight + FrameInset * 2f));

        Text.enableAutoSizing = true;
        Text.ForceMeshUpdate();
    }

    public void SetRaycastTargets(bool enabled)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = enabled;
    }
}
