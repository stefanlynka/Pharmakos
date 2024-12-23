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

        GameState.CurrentPlayer.ChangeOffering(OfferingType.Scroll, 1);

        if (target is Follower followerTarget)
        {
            followerTarget.ApplyOnTargetedEffects();
        }
    }

    public bool HasPlayableTargets()
    {
        return !HasTargets || GetTargets().Count > 0;
    }

}
