using System.Collections;
using System.Collections.Generic;
using System.Text;
using Pharmakos.Events;
using Pharmakos.Events.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventScreenHandler : MonoBehaviour
{
    public List<Button> OptionButtons = new List<Button>();
    public float CharacterRevealDelay = 0.1f;

    public List<EventSummaryView> EventSummaryPositions = new List<EventSummaryView>();

    public GameObject Popup1;
    public GameObject Popup2;

    TextMeshProUGUI _eventText;

    Coroutine _typewriterRoutine;

    /// <summary>
    /// Shows the summary UI for <paramref name="position"/> and routes body text / typewriter to that view's <c>SummaryText</c>.
    /// All other entries in <see cref="EventSummaryPositions"/> have their containers hidden.
    /// </summary>
    public void ApplySummaryPosition(EventSummaryPosition position)
    {
        _eventText = null;
        if (EventSummaryPositions == null)
            return;

        for (int i = 0; i < EventSummaryPositions.Count; i++)
        {
            var view = EventSummaryPositions[i];
            if (view == null) continue;
            if (view.Container != null)
                view.Container.SetActive(false);
        }

        for (int i = 0; i < EventSummaryPositions.Count; i++)
        {
            var view = EventSummaryPositions[i];
            if (view == null || view.Position != position) continue;

            if (view.Container != null)
                view.Container.SetActive(true);
            _eventText = view.SummaryText;
            break;
        }

        if (_eventText == null)
            Debug.LogWarning($"EventScreenHandler: no EventSummaryView for summary position {position}. Assign one in EventSummaryPositions.");
    }

    public void ShowTypingText(string fullText)
    {
        if (_typewriterRoutine != null)
            StopCoroutine(_typewriterRoutine);
        _typewriterRoutine = StartCoroutine(TypeTextRoutine(fullText));
    }

    /// <summary>Runs the typewriter on the active summary's <c>SummaryText</c> and waits until it finishes (or immediately if <paramref name="fullText"/> is empty).</summary>
    public IEnumerator PlayEventBodyTyping(string fullText)
    {
        if (_typewriterRoutine != null)
        {
            StopCoroutine(_typewriterRoutine);
            _typewriterRoutine = null;
        }
        yield return TypeTextRoutine(fullText);
    }

    public void HideAllOptions()
    {
        HideOutcomePreview();

        for (int i = 0; i < OptionButtons.Count; i++)
        {
            var button = OptionButtons[i];
            if (button == null) continue;

            var optionRow = button.GetComponent<EventOptionButtonComponent>();
            if (optionRow != null)
                optionRow.ResetOption();
            else
            {
                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }
    }

    public void LoadOptions(List<EventOptionData> options, System.Action<int> onOptionClicked)
    {
        HideAllOptions();
        if (options == null) return;

        int count = Mathf.Min(options.Count, OptionButtons.Count);
        for (int i = 0; i < count; i++)
        {
            int captured = i;
            var button = OptionButtons[i];
            if (button == null) continue;

            var option = options[i];
            string optionText = option?.OptionText ?? string.Empty;
            var outcome = option?.Outcome;

            var optionRow = button.GetComponent<EventOptionButtonComponent>();
            if (optionRow != null)
            {
                optionRow.Load(optionText, outcome, this, () => onOptionClicked?.Invoke(captured));
                continue;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = optionText;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onOptionClicked?.Invoke(captured));
            button.interactable = true;
            button.gameObject.SetActive(true);
        }
    }

    public void ShowOutcomePreview(EventOutcomeData outcome)
    {
        HideOutcomePreview();

        if (!EventOutcomePreview.SupportsPreview(outcome))
            return;

        EventOutcomePreview.PopupVisibility visibility = EventOutcomePreview.Populate(outcome);

        if (Popup1 != null)
            Popup1.SetActive(visibility.Popup1);
        if (Popup2 != null)
            Popup2.SetActive(visibility.Popup2);
    }

    public void HideOutcomePreview()
    {
        PopupHandler.ClearAll();

        if (Popup1 != null)
            Popup1.SetActive(false);
        if (Popup2 != null)
            Popup2.SetActive(false);
    }

    public void SetOptionsInteractable(bool isInteractable)
    {
        for (int i = 0; i < OptionButtons.Count; i++)
        {
            var button = OptionButtons[i];
            if (button == null || !button.gameObject.activeSelf) continue;

            var optionRow = button.GetComponent<EventOptionButtonComponent>();
            if (optionRow != null)
                optionRow.SetInteractable(isInteractable);
            else
                button.interactable = isInteractable;
        }
    }

    IEnumerator TypeTextRoutine(string fullText)
    {
        if (_eventText == null)
        {
            Debug.LogWarning("EventScreenHandler: no active summary text. Call ApplySummaryPosition first and ensure a matching EventSummaryView has SummaryText assigned.");
            yield break;
        }

        _eventText.text = string.Empty;
        if (string.IsNullOrEmpty(fullText))
        {
            Debug.LogWarning("EventScreenHandler: event body text is empty. Set Event Text on the Event Definition asset.");
            yield break;
        }

        var sb = new StringBuilder(fullText.Length);
        float delay = Mathf.Max(0.001f, CharacterRevealDelay);
        for (int i = 0; i < fullText.Length; i++)
        {
            sb.Append(fullText[i]);
            _eventText.text = sb.ToString();
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    public void ClearEventText()
    {
        if (_eventText != null)
            _eventText.text = string.Empty;
    }
}
