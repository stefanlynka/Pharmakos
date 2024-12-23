using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

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

    public override GameAction DeepCopy(Player newOwner)
    {
        ChangeResourceAction copy = (ChangeResourceAction)MemberwiseClone();
        copy.owner = newOwner.GameState.GetTargetByID<Player>(owner.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false)
    {
        owner.ChangeOffering(offeringType, amount);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            //new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
