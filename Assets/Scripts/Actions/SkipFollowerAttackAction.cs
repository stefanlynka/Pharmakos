using System.Collections.Generic;
using UnityEngine;

public class SkipFollowerAttackAction : GameAction
{
    public Follower Attacker;

    public SkipFollowerAttackAction(Follower attacker)
    {
        Attacker = attacker;
    }

    public override void Execute(bool simulated = false)
    {
        Attacker.SkippedAttack = true;

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        return new List<AnimationAction>();
    }

}

