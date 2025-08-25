using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class StealCardAction : GameAction
{
    public Player Thief;
    public Player Target;

    private Card stolenCard;

    private int targetIndex = 0;

    public StealCardAction(Player thief, Player target)
    {
        Thief = thief;
        Target = target;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        StealCardAction copy = (StealCardAction)MemberwiseClone();
        copy.Thief = newOwner.GameState.GetTargetByID<Player>(Thief.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<Player>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        if (Thief == Target || Target.Hand.Count == 0) 
        {
            base.Execute(simulated, false);
            return;
        }

        stolenCard = Target.Hand[targetIndex];
        Target.Hand.Remove(stolenCard);
        stolenCard.Costs[OfferingType.Gold] = 0;
        stolenCard.Owner = Thief;
        Thief.Hand.Add(stolenCard);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new MoveCardAnimation(this, stolenCard, Target, GameZone.Hand, Thief, GameZone.Hand)
        };
        return animationActions;
    }
}
