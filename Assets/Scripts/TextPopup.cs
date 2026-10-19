using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextPopup : MonoBehaviour
{
    public const float FrameInset = 35f;

    public TextMeshProUGUI Text;

    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

    RectTransform _rectTransform;

    public void SetText(string text)
    {
        if (Text == null)
            return;

        Text.text = text ?? string.Empty;

        RectTransform textRect = Text.rectTransform;
        textRect.offsetMin = new Vector2(FrameInset, FrameInset);
        textRect.offsetMax = new Vector2(-FrameInset, -FrameInset);
    }

    public void SetRaycastTargets(bool enabled)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = enabled;
    }
}
