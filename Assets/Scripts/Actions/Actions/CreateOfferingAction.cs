using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class CreateOfferingAction : GameAction
{
    public Player Owner;
    public OfferingType Offering;
    public int Amount;
    public int SourceID;
    public int DestinationID;

    public CreateOfferingAction(Player owner, OfferingType offering, int amount, int sourceID, int destinationID)
    {
        Owner = owner;
        Offering = offering;
        Amount = amount;
        SourceID = sourceID;
        DestinationID = destinationID;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        CreateOfferingAction copy = (CreateOfferingAction)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.Offering = Offering;
        copy.Amount = Amount;
        copy.SourceID = SourceID;
        copy.DestinationID = DestinationID;

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        if (Owner != null)
        {
            Owner.ChangeOffering(Offering, Amount);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new CreateOfferingAnimation(this, Owner, Offering, Amount, SourceID, DestinationID)
        };
        return animationActions;
    }
}
