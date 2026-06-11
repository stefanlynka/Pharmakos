using TMPro;
using UnityEngine;

public class OfferingLabel : MonoBehaviour
{
    public TextMeshPro OfferingName;
    public TextMeshPro OfferingAmount;
    public SpriteRenderer Icon;

    [Tooltip("Which side of this offering the hover summary popup appears on.")]
    public PopupPosition SummaryPosition = PopupPosition.Above;

    string summaryDescription = string.Empty;

    public void SetSummaryText(string text)
    {
        summaryDescription = text ?? string.Empty;
    }

    void OnMouseEnter()
    {
        if (ContentScrollView.BlocksGameInput)
            return;

        ShowHoverSummary();
    }

    void OnMouseExit()
    {
        HideHoverSummary();
    }

    void OnDisable()
    {
        HideHoverSummary();
    }

    void ShowHoverSummary()
    {
        if (ContentScrollView.BlocksGameInput)
            return;

        if (string.IsNullOrEmpty(summaryDescription))
            return;

        PopupScreenHandler.Instance?.ShowTextPopup(summaryDescription, transform, SummaryPosition);
    }

    void HideHoverSummary()
    {
        PopupScreenHandler.Instance?.HideTextPopup(transform);
    }
}
