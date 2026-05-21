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

    [Header("Camera (intro)")]
    public float FadeInDuration = 2.0f;
    public float ZoomedInOrthographicSize = 1.6f;

    [Tooltip("Optional transform that marks the close-up focus point.")]
    public Transform ZoomFocusPoint;

    [Header("Event image intro")]
    [Tooltip("Realtime wait after the intro fade finishes, before the event image begins moving and scaling toward the end pose (runs in parallel with body text typing).")]
    public float HoldBeforeZoomOutDuration = 3.0f;
    [Tooltip("How long the event image takes to reach the end local position and scale.")]
    public float ZoomOutDuration = 6.0f;
    public Vector3 EventImageEndLocalPosition = Vector3.zero;
    public Vector3 EventImageEndLocalScale = new Vector3(6f, 6f, 1f);

    EventDefinition _currentDefinition;
    Coroutine _sequenceRoutine;
    Coroutine _eventImageIntroRoutine;
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
        idx = 0; // TODO: Remove this
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

        if (_eventImageIntroRoutine != null)
        {
            StopCoroutine(_eventImageIntroRoutine);
            _eventImageIntroRoutine = null;
        }

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
        {
            EventBackground.sprite = _currentDefinition.BackgroundImage;
            EventBackground.transform.localPosition = _currentDefinition.ImageStartingPosition;
            EventBackground.transform.localScale = _currentDefinition.ImageStartingScale;
        }
        if (EventScreenHandler != null)
        {
            EventScreenHandler.ApplySummaryPosition(_currentDefinition.SummaryPosition);
            EventScreenHandler.HideAllOptions();
            EventScreenHandler.SetOptionsInteractable(false);
            EventScreenHandler.ClearEventText();

            EventScreenHandler.LoadOptions(_currentDefinition.Options, OnOptionSelected);
            EventScreenHandler.HideOutcomePreview();
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

        _eventImageIntroRoutine = StartCoroutine(RunEventImageIntroCoroutine());

        if (EventScreenHandler != null)
            yield return StartCoroutine(EventScreenHandler.PlayEventBodyTyping(_currentDefinition.EventText));

        yield return _eventImageIntroRoutine;
        _eventImageIntroRoutine = null;
    }

    IEnumerator RunEventImageIntroCoroutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, HoldBeforeZoomOutDuration));

        if (EventBackground == null)
            yield break;

        Transform tform = EventBackground.transform;
        Vector3 startPos = tform.localPosition;
        Vector3 startScale = tform.localScale;
        Vector3 endPos = EventImageEndLocalPosition;
        Vector3 endScale = EventImageEndLocalScale;
        float duration = Mathf.Max(0.01f, ZoomOutDuration);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            tform.localPosition = Vector3.Lerp(startPos, endPos, progress);
            tform.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null;
        }

        tform.localPosition = endPos;
        tform.localScale = endScale;
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
            case EventOutcomeType.FightEagle:
                break;
            case EventOutcomeType.IncreaseMaxHealth:
                break;
            case EventOutcomeType.CopyCardInDeck:
                break;
            case EventOutcomeType.GainCard:
            case EventOutcomeType.GainTrinket:
            case EventOutcomeType.GainHeartstring:
                EventOutcomePreview.Apply(outcome);
                break;
            case EventOutcomeType.RemoveCardFromDeck:
                break;
            case EventOutcomeType.BecomeLastEnemy:
                break;
            case EventOutcomeType.GoldenFleeceFight:
                break;
            case EventOutcomeType.GainRituals:
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
