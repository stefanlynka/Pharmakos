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

    private float duration = 0.25f;


    public MoveCardAnimation(GameAction gameAction, Card card, Player previousOwner, GameZone previousZone, Player newOwner, GameZone newZone, int newIndex = -1) : base(gameAction)
    {
        this.card = card;
        this.previousOwner = previousOwner;
        this.previousZone = previousZone;
        this.newOwner = newOwner;
        this.newZone = newZone;
        this.newIndex = newIndex;
    }

    public override void Play(Action onFinish = null)
    {
        OnFinish = onFinish;

        // If we can't find a ViewCard for this Card
        if (!View.Instance.TryGetViewCard(card, out viewCard))
        {
            // Make a new ViewCard
            viewCard = View.Instance.MakeNewViewCard(card);
            // and put it offscreen to the right
            switch (previousZone)
            {
                case GameZone.Deck:
                    viewCard.transform.position = new Vector3(-40, -20, 10);
                    break;
                case GameZone.Discard:
                    viewCard.transform.position = new Vector3(22, -20, 10);
                    break;
                default:
                    viewCard.transform.position = new Vector3(50, 0, 10);
                    break;
            }
        }
        else
        {
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

        startPos = viewCard.transform.position;

        switch (newZone)
        {
            case GameZone.BattleRow:
                endPos = View.Instance.GetViewPlayer(newOwner).BattleRow.GetPotentialFollowerPosition(newIndex);
                break;
            case GameZone.Hand:
                endPos = View.Instance.GetViewPlayer(newOwner).HandHandler.GetPotentialCardPosition(newIndex);
                break;
            case GameZone.Discard:
                endPos = new Vector3(22, -20, 10);
                break;
            case GameZone.Deck:
                endPos = new Vector3(-40, -20, 10);
                break;
            default:
                endPos = new Vector3(50, 0, 10);
                break;
        }
        

        Sequence moveSequence = new Sequence();
        moveSequence.Add(new Tween(TweenProgress, 0, 1, duration));
        moveSequence.Add(new SequenceAction(Complete));
        moveSequence.Start();
    }

    private void TweenProgress(float progress)
    {
        viewCard.transform.position = startPos + (endPos - startPos) * progress;
    }

    private void Complete()
    {
        switch (newZone)
        {
            case GameZone.BattleRow:
                if (viewCard is ViewFollower viewFollower)
                {
                    View.Instance.MoveFollowerToBattleRow(viewFollower.Follower, newIndex);
                    //if (previousOwner == View.Instance.Player1.Player)
                    //{
                    //    View.Instance.Player1.BattleRow.TryRemoveFollower(viewFollower);
                    //}
                    //else
                    //{
                    //    View.Instance.Player2.BattleRow.TryRemoveFollower(viewFollower);
                    //}
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
                break;
        }

        OnFinish?.Invoke();
    }
}
