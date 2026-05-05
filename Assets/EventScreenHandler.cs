using System.Collections;
using System.Collections.Generic;
using System.Text;
using Pharmakos.Events.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventScreenHandler : MonoBehaviour
{
    public Image EventImage;
    public TextMeshProUGUI EventText;
    public List<Button> OptionButtons = new List<Button>();
    public float CharacterRevealDelay = 0.1f;

    Coroutine _typewriterRoutine;

    public void LoadEventSprite(Sprite sprite)
    {
        if (EventImage != null)
            EventImage.sprite = sprite;
    }

    public void ShowTypingText(string fullText)
    {
        if (_typewriterRoutine != null)
            StopCoroutine(_typewriterRoutine);
        _typewriterRoutine = StartCoroutine(TypeTextRoutine(fullText));
    }

    /// <summary>Runs the typewriter on <see cref="EventText"/> and waits until it finishes (or immediately if <paramref name="fullText"/> is empty).</summary>
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

    public void LoadOptions(List<string> options, System.Action<int> onOptionClicked)
    {
        HideAllOptions();
        if (options == null) return;

        int count = Mathf.Min(options.Count, OptionButtons.Count);
        for (int i = 0; i < count; i++)
        {
            int captured = i;
            var button = OptionButtons[i];
            if (button == null) continue;

            var optionRow = button.GetComponent<EventOptionButtonComponent>();
            if (optionRow != null)
            {
                optionRow.Load(options[i], () => onOptionClicked?.Invoke(captured));
                continue;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = options[i];

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onOptionClicked?.Invoke(captured));
            button.interactable = true;
            button.gameObject.SetActive(true);
        }
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
        if (EventText == null)
        {
            Debug.LogWarning("EventScreenHandler: EventText is not assigned.");
            yield break;
        }

        EventText.text = string.Empty;
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
            EventText.text = sb.ToString();
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    public void ClearEventText()
    {
        if (EventText != null)
            EventText.text = string.Empty;
    }
}
