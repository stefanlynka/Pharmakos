using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeResourceAction : GameAction
{
    Player owner;
    OfferingType offeringType;
    int amount = 0;

    public ChangeResourceAction(Player owner, OfferingType offeringType, int amount)
    {
        this.owner = owner;
        this.offeringType = offeringType;
        this.amount = amount;
    }

    public override void Execute(bool simulated = false)
    {
        owner.ChangeOffering(offeringType, amount);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>(AnimationActions)
        {
            //new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
