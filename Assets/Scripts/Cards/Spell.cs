using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spell : Card, ITarget
{
    public bool HasTargets = true;
    public override List<ITarget> GetTargets()
    {
        return new List<ITarget>();
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        GameAction offeringAction = new CreateOfferingAction(Owner, OfferingType.Scroll, 1, Owner.ITargetID, GameState.CurrentPlayer.ITargetID);
        GameState.ActionHandler.AddAction(offeringAction);
        //GameState.CurrentPlayer.ChangeOffering(OfferingType.Scroll, 1);

        if (target is Follower followerTarget)
        {
            followerTarget.ApplyOnTargetedEffects(this);
        }

        // Trigger all "OnSpellPlayed" triggers
        GameState.FireSpellPlayed(this);

        GameAction newAction = new PlaySpellAction(Owner, this);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }

    public bool HasPlayableTargets()
    {
        return !HasTargets || GetTargets().Count > 0;
    }

}
