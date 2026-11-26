using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MoveCardAnimation : AnimationAction
{
    private Tween animationTween;

    private Player previousOwner;
    private GameZone previousZone;
    private Player newOwner;
    private GameZone newZone;
    private int newIndex = -1;

    private Card card;
    private ViewCard viewCard;
    private Vector3 startPos;
    private Vector3 endPos;

    private float duration = 0.5f; // 0.25f

    private bool useCurrentScale = false;
    private float startScale = 1.2f;
    private float endScale = 1.2f;

    private bool useCurrentRotation = false;
    private Vector3 startRotation = new Vector3(0, 0, 0);
    private Vector3 endRotation = new Vector3(0, 0, 0);
    private bool tweenRotation = false;
    private bool useQuaternionRotation = false;
    private Quaternion startRotationQuat = Quaternion.identity;
    private Quaternion endRotationQuat = Quaternion.identity;

    private bool forceDescriptive = false;

    // Drawn from a DeckPile: the card lifts off the pile face-down and turns over on the way (stays face-down
    // for the enemy's hand), on a small arc toward the camera.
    private DeckPile sourcePile;
    private Quaternion restRotation;
    private float flipFromTwist;
    private bool endsFaceDown;
    private const float PileArcHeight = 2.5f;

    public MoveCardAnimation(GameAction gameAction, Card card, Player previousOwner, GameZone previousZone, Player newOwner, GameZone newZone, float duration = 0.5f, int newIndex = -1) : base(gameAction)
    {
        this.card = card;
        this.previousOwner = previousOwner;
        this.previousZone = previousZone;
        this.newOwner = newOwner;
        this.newZone = newZone;
        this.duration = duration;
        this.newIndex = newIndex;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        // If we can't find a ViewCard for this Card
        if (!View.Instance.TryGetViewCard(card, out viewCard))
        {
            // Make a new ViewCard
            viewCard = View.Instance.MakeNewViewCard(card);

            if (useCurrentScale)
            {
                startScale = viewCard.transform.localScale.x;
            }

            if (useCurrentRotation)
            {
                startRotation = viewCard.transform.eulerAngles;
            }

            // Put it roughly where it should have been
            switch (previousZone)
            {
                case GameZone.BattleRow:
                    if (previousOwner.IsHuman)
                    {
                        viewCard.transform.position = new Vector3(0, -2, 10);
                    }
                    else
                    {
                        viewCard.transform.position = new Vector3(0, 9, 10);
                    }
                    break;
                case GameZone.Deck when DeckPile.For(previousOwner) != null:
                    sourcePile = DeckPile.For(previousOwner);
                    viewCard.transform.position = sourcePile.TopPosition;
                    // A steal copies a card rather than taking one off the pile.
                    if (previousOwner == newOwner)
                        DeckPile.NotifyCardLeft(previousOwner);
                    break;
                case GameZone.Deck:
                case GameZone.Discard:
                    if (previousOwner.IsHuman)
                    {
                        viewCard.transform.position = new Vector3(0, -32, 10);
                    }
                    else
                    {
                        viewCard.transform.position = new Vector3(0, 50, 10);
                    }
                    break;
                default:
                    viewCard.transform.position = new Vector3(50, 0, 10);
                    break;
                }
        }
        else
        {
            // If we do have the card, remove it from its current location (hand or battlerow)
            switch (previousZone)
            {
                case GameZone.BattleRow:
                    if (viewCard is ViewFollower viewFollower)
                    {
                        if (previousOwner == View.Instance.Player1.Player)
                        {
                            View.Instance.Player1.BattleRow.TryRemoveFollower(viewFollower);
                        }
                        else
                        {
                            View.Instance.Player2.BattleRow.TryRemoveFollower(viewFollower);
                        }
                    }
                    break;
                case GameZone.Hand:
                    if (previousOwner == View.Instance.Player1.Player)
                    {
                        View.Instance.Player1.HandHandler.RemoveCard(viewCard);
                    }
                    else
                    {
                        View.Instance.Player2.HandHandler.RemoveCard(viewCard);
                    }
                    break;
            }
        }

        if (newZone == GameZone.BattleRow)
        {
            viewCard.transform.SetParent(null, true);
            SetupBattleRowRotationTween();
        }

        startPos = viewCard.transform.position;

        // Determine the card's destination
        switch (newZone)
        {
            case GameZone.BattleRow:
                endPos = View.Instance.GetViewPlayer(newOwner).BattleRow.GetPotentialFollowerPosition(newIndex);
                break;
            case GameZone.Hand:
                endPos = View.Instance.GetViewPlayer(newOwner).HandHandler.GetPotentialCardPosition(newIndex);
                break;
            case GameZone.Deck:
            case GameZone.Discard:
                if (newOwner.IsHuman)
                {
                    endPos = new Vector3(0, -32, 10);
                }
                else
                {
                    endPos = new Vector3(0, 32, 10);
                }
                break;
            case GameZone.PlayZone:
                endPos = new Vector3(-27, 4.4f, 20);
                break;
            default:
                endPos = new Vector3(50, 0, 10);
                break;
        }

        if (sourcePile != null)
            SetupDrawFromPile();

        if (forceDescriptive) viewCard.SetDescriptiveMode(true);


        if (newOwner.IsHuman)
        {
            switch (newZone)
            {
                case GameZone.Hand:
                case GameZone.Discard:
                    View.Instance.AudioHandler.PlaySoundEffect(AudioHandler.SoundEffectType.CardDraw);
                    break;
                case GameZone.PlayZone:
                    View.Instance.AudioHandler.PlaySoundEffect(AudioHandler.SoundEffectType.Scroll);
                    break;
            }
        }

        Sequence moveSequence = new Sequence();
        moveSequence.Add(new Tween(TweenProgress, 0, 1, duration));
        moveSequence.Add(new SequenceAction(Complete));
        moveSequence.Start();
    }


    void SetupDrawFromPile()
    {
        restRotation = viewCard.transform.rotation;
        float width = viewCard.CardCollider != null ? viewCard.CardCollider.size.x : 5.4f;
        startScale = sourcePile.WorldCardWidth / Mathf.Max(0.01f, width);
        flipFromTwist = sourcePile.PileTwist;
        endsFaceDown = !newOwner.IsHuman;
        tweenRotation = false;
        CardBackFace.Attach(viewCard, sourcePile.BackMaterial);
        ApplyPileFlip(0f);
        viewCard.transform.localScale = Vector3.one * startScale;
    }

    // Turned over (180° about the card's vertical axis) at the start; the turn happens mostly mid-flight.
    void ApplyPileFlip(float progress)
    {
        float turn = endsFaceDown ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.85f, progress));
        float yaw = 180f * (1f - turn);
        float twist = flipFromTwist * (1f - progress);
        viewCard.transform.rotation = restRotation * Quaternion.Euler(0f, yaw, twist);
    }

    public void SetScale(float startScale, float endScale, bool useCurrentScale = false)
    {
        this.useCurrentScale = useCurrentScale;
        this.startScale = startScale;
        this.endScale = endScale;
    }
    public void SetRotation(Vector3 startRotation, Vector3 endRotation, bool useCurrentRotation = false)
    {
        this.useCurrentRotation = useCurrentRotation;
        this.startRotation = startRotation;
        this.endRotation = endRotation;
        tweenRotation = true;
        useQuaternionRotation = false;
    }

    void SetupBattleRowRotationTween()
    {
        ViewBattleRow battleRow = View.Instance.GetViewPlayer(newOwner).BattleRow;
        if (battleRow?.UnitHolderTransform == null)
            return;

        startRotationQuat = viewCard.transform.rotation;
        endRotationQuat = battleRow.UnitHolderTransform.rotation;
        tweenRotation = true;
        useQuaternionRotation = true;
    }
    public void ForceDescriptive(bool value)
    {
        forceDescriptive = value;
    }

    private void TweenProgress(float progress)
    {
        viewCard.transform.position = startPos + (endPos - startPos) * progress;
        if (sourcePile != null)
        {
            viewCard.transform.position += Vector3.back * Mathf.Sin(progress * Mathf.PI) * PileArcHeight;
            ApplyPileFlip(progress);
        }
        float newScale = startScale + (endScale - startScale) * progress;
        viewCard.transform.localScale = new Vector3(newScale, newScale, newScale);

        if (tweenRotation)
        {
            if (useQuaternionRotation)
            {
                viewCard.transform.rotation = Quaternion.Slerp(startRotationQuat, endRotationQuat, progress);
            }
            else
            {
                viewCard.transform.eulerAngles = startRotation + (endRotation - startRotation) * progress;
            }
        }
    }

    private void Complete()
    {
        if (sourcePile != null && viewCard != null)
        {
            CardBackFace.Detach(viewCard);
            viewCard.transform.rotation = restRotation;
        }

        // The fight may already have cleared this card once the screen went black.
        if (!View.Instance.TryGetViewCard(card, out ViewCard current) || current != viewCard)
        {
            CallCallback();
            return;
        }

        switch (newZone)
        {
            case GameZone.BattleRow:
                if (viewCard is ViewFollower viewFollower)
                {
                    View.Instance.MoveFollowerToBattleRow(viewFollower.Follower, newIndex);
                    View.ApplyCombatCardScale(viewCard.transform);
                    View.Instance.AudioHandler.PlaySoundEffect(AudioHandler.SoundEffectType.Crop);
                }
                break;
            case GameZone.Hand:
                if (newOwner == View.Instance.Player1.Player)
                {
                    View.Instance.Player1.HandHandler.MoveCardToHand(viewCard);
                }
                else
                {
                    View.Instance.Player2.HandHandler.MoveCardToHand(viewCard);
                }
                View.ApplyCombatCardScale(viewCard.transform);
                TutorialCombatScript.OnCardEnteredHand(newOwner);
                break;
            case GameZone.Discard:
                View.Instance.DiscardCard(viewCard.Card);
                break;
        }

        CallCallback();
    }
}
