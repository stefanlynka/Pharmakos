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

        //GameState.CurrentPlayer.ChangeOffering(OfferingType.Scroll, 1);

        if (target is Follower followerTarget)
        {
            followerTarget.ApplyOnTargetedEffects(this);
        }

        // Trigger all "OnSpellPlayed" triggers
        GameState.FireSpellPlayed(this);

        GameAction newAction = new PlaySpellAction(Owner, this, target);
        Owner.GameState.ActionHandler.AddAction(newAction);

        GameAction offeringAction = new CreateOfferingAction(Owner, OfferingType.Scroll, 1, Owner.ITargetID, GameState.CurrentPlayer.ITargetID);
        GameState.ActionHandler.AddAction(offeringAction);

    }

    public virtual VfxEffect GetPlayVfx(ITarget target)
    {
        return null;
    }

    // Thrown from this spell's owner to the target while the spell is shown.
    public virtual ArcProjectileStyle GetThrowStyle()
    {
        return null;
    }

    public bool HasPlayableTargets()
    {
        return !HasTargets || GetTargets().Count > 0;
    }

}
