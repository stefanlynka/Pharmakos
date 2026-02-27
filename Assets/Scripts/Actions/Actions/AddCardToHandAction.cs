using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using static UnityEngine.GraphicsBuffer;

public class AddCardCopyToHandAction : GameAction
{
    public Card Card;

    // Card should already be Init()'d 
    public AddCardCopyToHandAction(Card newCard)
    {
        Card = newCard;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        AddCardCopyToHandAction copy = (AddCardCopyToHandAction)MemberwiseClone();
        copy.Card = Card.DeepCopy(newOwner);

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        bool success = Card.Owner.AddCardToHand(Card);

        base.Execute(simulated, success);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new MoveCardAnimation(this, Card, Card.Owner, GameZone.BattleRow, Card.Owner, GameZone.Hand, 0.25f)
        };
        return animationActions;
    }
}
