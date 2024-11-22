using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class AddCardCopyToHandAction : GameAction
{
    public Card Card;

    // Card should already be Init()'d 
    public AddCardCopyToHandAction(Card newCard)
    {
        Card = newCard;
    }

    public override void Execute(bool simulated = false)
    {
        Card.Owner.Hand.Add(Card);
        View.Instance.DrawCard(Card);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>(AnimationActions)
        {
            new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
