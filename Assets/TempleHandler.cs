using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TempleCardPose
{
    public Vector3 LocalPosition;
    public Vector3 LocalEulerAngles;
    public Vector3 LocalScale = new Vector3(0.15f, 0.15f, 0.15f);
}

public class TempleHandler : MonoBehaviour
{
    public static TempleHandler Instance;

    public Camera TempleCamera;
    public TempleScreenHandler TempleScreenHandler;
    public Transform CardHolder;
    [Tooltip("Three transforms marking on-screen card positions (children of CardHolder).")]
    public Transform[] CardSlotAnchors = new Transform[3];
    [Tooltip("Added to each slot's on-screen local position for the off-screen start/end pose.")]
    public Vector3 OffScreenLocalOffset = new Vector3(0f, 6f, 2f);

    public float CardMoveDuration = 0.55f;
    public float CardMoveStagger = 0.12f;
    public EaseType CardMoveEase = EaseType.EaseOutQuad;

    const int CardsPerBatch = 3;
    const int BatchCount = 3;

    readonly List<Card> _sacrificePool = new List<Card>();
    readonly List<List<Card>> _batches = new List<List<Card>>();
    readonly List<ViewCard> _activeViewCards = new List<ViewCard>();
    TempleCardPose[] _onScreenPoses = new TempleCardPose[CardsPerBatch];
    TempleCardPose[] _offScreenPoses = new TempleCardPose[CardsPerBatch];

    int _currentBatchIndex;
    int _selectedCardIndex;
    bool _busy;
    Coroutine _animationRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CacheSlotPoses();
        HideAnchorPlaceholders();
        SetTempleAreaActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void BeginTempleSacrifice()
    {
        if (Controller.Instance == null || View.Instance == null)
        {
            Debug.LogError("TempleHandler: Controller or View is missing.");
            return;
        }

        BuildSacrificePool();
        if (_sacrificePool.Count == 0)
        {
            Debug.LogWarning("TempleHandler: deck has no cards to sacrifice.");
            FinishToRitualRewards();
            return;
        }

        _currentBatchIndex = 0;
        _busy = false;
        SetTempleAreaActive(true);
        if (TempleCamera != null) TempleCamera.gameObject.SetActive(true);

        if (TempleScreenHandler != null)
        {
            TempleScreenHandler.ResetForTemple();
            TempleScreenHandler.SetSacrificeButtonsInteractable(true);
        }

        if (View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Activate(TempleCamera, () => _busy);

        ShowCurrentBatch();
    }

    public void RefuseSacrifice()
    {
        if (_busy) return;
        EndTemple();
        Controller.Instance.StartNextLevel();
    }

    public void AcceptSacrifice()
    {
        if (_busy || _activeViewCards.Count == 0) return;
        if (_selectedCardIndex < 0 || _selectedCardIndex >= _activeViewCards.Count) return;

        _busy = true;
        if (TempleScreenHandler != null)
            TempleScreenHandler.SetSacrificeButtonsInteractable(false);

        StartCoroutine(AcceptSacrificeRoutine());
    }

    public void EndTemple()
    {
        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }

        ClearActiveCards();
        _sacrificePool.Clear();
        _batches.Clear();
        _busy = false;

        if (TempleCamera != null) TempleCamera.gameObject.SetActive(false);
        SetTempleAreaActive(false);

        if (View.Instance != null && View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Deactivate();
    }

    void BuildSacrificePool()
    {
        _sacrificePool.Clear();
        _batches.Clear();

        var deck = Controller.Instance.HumanPlayerDetails.DeckBlueprint[0];
        if (deck == null || deck.Count == 0) return;

        var selectable = new List<Card>(deck);
        int pickCount = Mathf.Min(BatchCount * CardsPerBatch, selectable.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int idx = Controller.Instance.MetaRNG.Next(0, selectable.Count);
            _sacrificePool.Add(selectable[idx]);
            selectable.RemoveAt(idx);
        }

        for (int batch = 0; batch < BatchCount; batch++)
        {
            var batchCards = new List<Card>();
            for (int j = 0; j < CardsPerBatch; j++)
            {
                int flatIndex = batch * CardsPerBatch + j;
                if (flatIndex >= _sacrificePool.Count) break;
                batchCards.Add(_sacrificePool[flatIndex]);
            }
            if (batchCards.Count > 0) _batches.Add(batchCards);
        }
    }

    void ShowCurrentBatch()
    {
        ClearActiveCards();

        if (_currentBatchIndex >= _batches.Count)
        {
            FinishToRitualRewards();
            return;
        }

        if (TempleScreenHandler != null)
            TempleScreenHandler.SetInstructionIndex(_currentBatchIndex);

        List<Card> batch = _batches[_currentBatchIndex];
        for (int i = 0; i < batch.Count; i++)
        {
            ViewCard viewCard = View.Instance.MakeNewViewCard(batch[i], false);
            viewCard.transform.SetParent(CardHolder, false);
            viewCard.transform.localPosition = _offScreenPoses[i].LocalPosition;
            viewCard.transform.localEulerAngles = _offScreenPoses[i].LocalEulerAngles;
            viewCard.transform.localScale = _offScreenPoses[i].LocalScale;
            viewCard.SetDescriptiveMode(true);
            viewCard.Load(batch[i], OnSacrificeCardClicked);
            if (viewCard.CardCollider != null) viewCard.CardCollider.enabled = true;
            viewCard.SetHighlight(false);
            viewCard.gameObject.SetActive(true);
            _activeViewCards.Add(viewCard);
        }

        _selectedCardIndex = 0;
        if (_activeViewCards.Count > 0)
            _activeViewCards[0].SetHighlight(true);

        _animationRoutine = StartCoroutine(AnimateCardsToOnScreen());
    }

    IEnumerator AnimateCardsToOnScreen()
    {
        _busy = true;
        yield return AnimateCards(_offScreenPoses, _onScreenPoses);
        _busy = false;
        _animationRoutine = null;
        if (TempleScreenHandler != null)
            TempleScreenHandler.SetSacrificeButtonsInteractable(true);
    }

    IEnumerator AcceptSacrificeRoutine()
    {
        ViewCard selected = _activeViewCards[_selectedCardIndex];
        Card sacrificedCard = selected.Card;
        var others = new List<ViewCard>();
        foreach (ViewCard viewCard in _activeViewCards)
        {
            if (viewCard != selected) others.Add(viewCard);
        }

        bool othersFinished = false;
        bool removalFinished = false;
        StartCoroutine(RunSacrificeParallel(
            AnimateCardsToOffScreen(others),
            () => othersFinished = true));
        StartCoroutine(RunSacrificeParallel(
            PlaySacrificeRemovalAnimation(selected),
            () => removalFinished = true));
        while (!othersFinished || !removalFinished)
            yield return null;

        Controller.Instance.RemoveCardsFromPlayerDeck(new List<Card> { sacrificedCard });
        ReleaseViewCard(selected);
        _activeViewCards.Clear();

        _currentBatchIndex++;
        if (_currentBatchIndex >= _batches.Count)
        {
            _busy = false;
            FinishToRitualRewards();
            yield break;
        }

        if (TempleScreenHandler != null)
            TempleScreenHandler.SetSacrificeButtonsInteractable(true);

        _busy = false;
        ShowCurrentBatch();
    }

    IEnumerator RunSacrificeParallel(IEnumerator routine, Action onComplete)
    {
        yield return routine;
        onComplete?.Invoke();
    }

    IEnumerator AnimateCardsToOffScreen(List<ViewCard> viewCards)
    {
        if (viewCards.Count == 0) yield break;

        var startPoses = new TempleCardPose[CardsPerBatch];
        var endPoses = new TempleCardPose[CardsPerBatch];
        var moving = new List<ViewCard>();

        for (int i = 0; i < viewCards.Count; i++)
        {
            ViewCard viewCard = viewCards[i];
            int slot = _activeViewCards.IndexOf(viewCard);
            if (slot < 0) slot = Mathf.Min(i, CardsPerBatch - 1);

            startPoses[i] = CapturePose(viewCard.transform);
            endPoses[i] = _offScreenPoses[slot];
            moving.Add(viewCard);
        }

        yield return AnimateCardSubset(moving, startPoses, endPoses);
        foreach (ViewCard viewCard in moving)
            ReleaseViewCard(viewCard);
    }

    IEnumerator PlaySacrificeRemovalAnimation(ViewCard viewCard)
    {
        if (viewCard is ViewFollower viewFollower && viewFollower.SkullRenderer != null)
        {
            bool done = false;
            Sequence skullSequence = new Sequence();
            skullSequence.Add(new Tween(p => SetSkullAlpha(viewFollower, p), 0, 1, 0.2f));
            skullSequence.Add(new Tween(p => SetSkullAlpha(viewFollower, p), 1, 0, 0.4f));
            skullSequence.Add(new SequenceAction(() => done = true));
            skullSequence.Start();
            while (!done) yield return null;
            yield break;
        }

        bool fadeDone = false;
        Sequence fadeSequence = new Sequence();
        // fadeSequence.Add(new Tween(p => SetCardAlpha(viewCard, 1f - p), 0, 1, 0.35f, EaseType.EaseInQuad));
        fadeSequence.Add(new SequenceAction(() => fadeDone = true));
        fadeSequence.Start();
        while (!fadeDone) yield return null;
    }

    IEnumerator AnimateCards(TempleCardPose[] fromPoses, TempleCardPose[] toPoses)
    {
        yield return AnimateCardSubset(_activeViewCards, fromPoses, toPoses);
    }

    IEnumerator AnimateCardSubset(List<ViewCard> viewCards, TempleCardPose[] fromPoses, TempleCardPose[] toPoses)
    {
        int count = viewCards.Count;
        if (count == 0) yield break;

        float maxEndTime = 0f;
        var sequences = new Sequence[count];

        for (int i = 0; i < count; i++)
        {
            ViewCard viewCard = viewCards[i];
            TempleCardPose from = fromPoses[Mathf.Min(i, fromPoses.Length - 1)];
            TempleCardPose to = toPoses[Mathf.Min(i, toPoses.Length - 1)];
            float delay = i * CardMoveStagger;
            float endTime = delay + CardMoveDuration;
            if (endTime > maxEndTime) maxEndTime = endTime;

            sequences[i] = BuildCardMoveSequence(viewCard, from, to, delay);
            sequences[i].Start();
        }

        yield return new WaitForSeconds(maxEndTime);
    }

    Sequence BuildCardMoveSequence(ViewCard viewCard, TempleCardPose from, TempleCardPose to, float delay)
    {
        Sequence sequence = new Sequence();
        if (delay > 0f)
            sequence.Add(new Tween(_ => { }, 0, 0, delay));

        sequence.Add(new Tween(progress =>
        {
            viewCard.transform.localPosition = Vector3.Lerp(from.LocalPosition, to.LocalPosition, progress);
            viewCard.transform.localEulerAngles = Vector3.Lerp(from.LocalEulerAngles, to.LocalEulerAngles, progress);
            viewCard.transform.localScale = Vector3.Lerp(from.LocalScale, to.LocalScale, progress);
        }, 0, 1, CardMoveDuration, CardMoveEase));

        return sequence;
    }

    void OnSacrificeCardClicked(ViewTarget viewTarget)
    {
        if (_busy) return;

        ViewCard clicked = viewTarget as ViewCard;
        if (clicked == null) return;

        int index = _activeViewCards.IndexOf(clicked);
        if (index < 0) return;

        SelectCard(index);
    }

    void SelectCard(int index)
    {
        if (index < 0 || index >= _activeViewCards.Count) return;
        if (_selectedCardIndex == index) return;

        for (int i = 0; i < _activeViewCards.Count; i++)
            _activeViewCards[i].SetHighlight(i == index);

        _selectedCardIndex = index;
    }

    void FinishToRitualRewards()
    {
        EndTemple();
        if (TempleScreenHandler != null)
            TempleScreenHandler.ProgressToRitualScreen();
        else
            Controller.Instance.GoToRitualRewardScreen();
    }

    void ClearActiveCards()
    {
        foreach (ViewCard viewCard in _activeViewCards)
            ReleaseViewCard(viewCard);
        _activeViewCards.Clear();
    }

    void ReleaseViewCard(ViewCard viewCard)
    {
        if (viewCard == null) return;
        viewCard.SetHighlight(false);
        if (viewCard.CardCollider != null) viewCard.CardCollider.enabled = false;
        View.Instance.ReleaseCard(viewCard);
    }

    void CacheSlotPoses()
    {
        if (CardSlotAnchors == null || CardSlotAnchors.Length < CardsPerBatch)
        {
            if (CardHolder != null && CardHolder.childCount >= CardsPerBatch)
            {
                CardSlotAnchors = new Transform[CardsPerBatch];
                for (int i = 0; i < CardsPerBatch; i++)
                    CardSlotAnchors[i] = CardHolder.GetChild(i);
            }
        }

        for (int i = 0; i < CardsPerBatch; i++)
        {
            if (i < CardSlotAnchors.Length && CardSlotAnchors[i] != null)
            {
                _onScreenPoses[i] = CapturePose(CardSlotAnchors[i]);
                _offScreenPoses[i] = new TempleCardPose
                {
                    LocalPosition = _onScreenPoses[i].LocalPosition + OffScreenLocalOffset,
                    LocalEulerAngles = _onScreenPoses[i].LocalEulerAngles,
                    LocalScale = _onScreenPoses[i].LocalScale,
                };
            }
        }
    }

    static TempleCardPose CapturePose(Transform t)
    {
        return new TempleCardPose
        {
            LocalPosition = t.localPosition,
            LocalEulerAngles = t.localEulerAngles,
            LocalScale = t.localScale,
        };
    }

    void HideAnchorPlaceholders()
    {
        if (CardSlotAnchors == null) return;
        foreach (Transform anchor in CardSlotAnchors)
        {
            if (anchor == null) continue;
            foreach (Renderer renderer in anchor.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }
    }

    void SetTempleAreaActive(bool active)
    {
        gameObject.SetActive(active);
    }

    static void SetSkullAlpha(ViewFollower viewFollower, float alpha)
    {
        if (viewFollower?.SkullRenderer == null) return;
        Color c = viewFollower.SkullRenderer.color;
        c.a = alpha;
        viewFollower.SkullRenderer.color = c;
    }

    static void SetCardAlpha(ViewCard viewCard, float alpha)
    {
        if (viewCard == null) return;
        foreach (SpriteRenderer spriteRenderer in viewCard.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
}
