using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextPopup : MonoBehaviour
{
    public TextMeshProUGUI Text;

    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

    RectTransform _rectTransform;

    public void SetText(string text)
    {
        if (Text != null)
            Text.text = text ?? string.Empty;
    }

    public void SetRaycastTargets(bool enabled)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = enabled;
    }
}
