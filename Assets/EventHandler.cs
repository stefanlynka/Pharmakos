using System.Collections;
using System.Collections.Generic;
using Pharmakos.Events;
using UnityEngine;

public class EventHandler : MonoBehaviour
{
    public SpriteRenderer EventBackground;
    public Camera EventCamera;
    public EventScreenHandler EventScreenHandler;
    public List<EventDefinition> Events = new List<EventDefinition>();

    [Header("Camera sequence")]
    public float FadeInDuration = 2.0f;
    public float HoldBeforeZoomOutDuration = 3.0f;
    public float ZoomOutDuration = 6.0f;
    public float ZoomedInOrthographicSize = 1.6f;
    public float ZoomedOutOrthographicSize = 5.0f;

    [Tooltip("Optional transform that marks the close-up focus point.")]
    public Transform ZoomFocusPoint;

    EventDefinition _currentDefinition;
    Coroutine _sequenceRoutine;
    System.Action _onEventComplete;

    public void BeginRandomEvent(System.Action onEventComplete)
    {
        if (Events == null || Events.Count == 0)
        {
            Debug.LogError("EventHandler: no events configured. Add Event Definition assets to the Events list.");
            onEventComplete?.Invoke();
            return;
        }

        int idx = Controller.Instance.MetaRNG.Next(0, Events.Count);
        var def = Events[idx];
        if (def == null)
        {
            Debug.LogError($"EventHandler: event at index {idx} is null.");
            onEventComplete?.Invoke();
            return;
        }

        BeginEvent(def, onEventComplete);
    }

    public void BeginEvent(EventDefinition eventDefinition, System.Action onEventComplete)
    {
        _currentDefinition = eventDefinition;
        _onEventComplete = onEventComplete;
        if (EventCamera != null)
            EventCamera.gameObject.SetActive(true);

        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);
        _sequenceRoutine = StartCoroutine(RunEventSequence());
    }

    IEnumerator RunEventSequence()
    {
        if (_currentDefinition == null)
        {
            CompleteAndReturn();
            yield break;
        }

        if (EventBackground != null)
            EventBackground.sprite = _currentDefinition.BackgroundImage;
        if (EventScreenHandler != null)
        {
            EventScreenHandler.LoadEventSprite(_currentDefinition.BackgroundImage);
            EventScreenHandler.HideAllOptions();
            EventScreenHandler.SetOptionsInteractable(false);
            EventScreenHandler.ClearEventText();

            var options = _currentDefinition.Options;
            var optionTexts = new List<string>();
            if (options != null)
            {
                for (int i = 0; i < options.Count; i++)
                    optionTexts.Add(options[i].OptionText);
            }

            EventScreenHandler.LoadOptions(optionTexts, OnOptionSelected);
            EventScreenHandler.SetOptionsInteractable(true);
        }

        if (EventCamera != null)
        {
            EventCamera.orthographicSize = ZoomedInOrthographicSize;
            if (ZoomFocusPoint != null)
            {
                var p = EventCamera.transform.position;
                EventCamera.transform.position = new Vector3(ZoomFocusPoint.position.x, ZoomFocusPoint.position.y, p.z);
            }
        }

        float fade = Mathf.Max(0.01f, FadeInDuration);
        for (float t = 0f; t < fade; t += Time.deltaTime)
        {
            float progress = t / fade;
            yield return null;
        }

        if (EventScreenHandler != null)
            yield return StartCoroutine(EventScreenHandler.PlayEventBodyTyping(_currentDefinition.EventText));

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, HoldBeforeZoomOutDuration));

        if (EventCamera != null)
        {
            float startSize = ZoomedInOrthographicSize;
            float endSize = Mathf.Max(startSize, ZoomedOutOrthographicSize);
            float zoom = Mathf.Max(0.01f, ZoomOutDuration);
            for (float t = 0f; t < zoom; t += Time.deltaTime)
            {
                float progress = t / zoom;
                EventCamera.orthographicSize = Mathf.Lerp(startSize, endSize, progress);
                yield return null;
            }
            EventCamera.orthographicSize = endSize;
        }
    }

    void OnOptionSelected(int optionIndex)
    {
        var options = _currentDefinition?.Options;
        if (options == null || optionIndex < 0 || optionIndex >= options.Count)
        {
            CompleteAndReturn();
            return;
        }

        ApplyOutcome(options[optionIndex].Outcome);
        CompleteAndReturn();
    }

    void ApplyOutcome(EventOutcomeData outcome)
    {
        if (outcome == null) return;

        switch (outcome.OutcomeType)
        {
            case EventOutcomeType.None:
                break;
            case EventOutcomeType.AddRandomTrinket:
                var trinkets = Controller.Instance.ProgressionHandler.GetRandomTrinkets();
                if (trinkets != null && trinkets.Count > 0)
                    Controller.Instance.AddTrinket(trinkets[0]);
                break;
            case EventOutcomeType.RemoveRandomCardFromDeck:
                RemoveRandomCardsFromDeck(Mathf.Max(1, outcome.CardCount));
                break;
            case EventOutcomeType.DuplicateRandomCardInDeck:
                DuplicateRandomCardsInDeck(Mathf.Max(1, outcome.CardCount));
                break;
            default:
                break;
        }
    }

    void RemoveRandomCardsFromDeck(int count)
    {
        var deck = Controller.Instance.HumanPlayerDetails.DeckBlueprint[0];
        if (deck == null || deck.Count == 0) return;

        var removed = new List<Card>();
        var selectable = new List<Card>(deck);
        int target = Mathf.Min(count, selectable.Count);
        for (int i = 0; i < target; i++)
        {
            int idx = Controller.Instance.MetaRNG.Next(0, selectable.Count);
            removed.Add(selectable[idx]);
            selectable.RemoveAt(idx);
        }

        if (removed.Count > 0)
            Controller.Instance.RemoveCardsFromPlayerDeck(removed);
    }

    void DuplicateRandomCardsInDeck(int count)
    {
        var deck = Controller.Instance.HumanPlayerDetails.DeckBlueprint[0];
        if (deck == null || deck.Count == 0) return;

        var toAdd = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            int idx = Controller.Instance.MetaRNG.Next(0, deck.Count);
            toAdd.Add(deck[idx].MakeBaseCopy());
        }

        if (toAdd.Count > 0)
            Controller.Instance.AddCardsToPlayerDeck(toAdd);
    }

    void CompleteAndReturn()
    {
        var complete = _onEventComplete;
        _onEventComplete = null;
        _currentDefinition = null;
        complete?.Invoke();
    }
}
