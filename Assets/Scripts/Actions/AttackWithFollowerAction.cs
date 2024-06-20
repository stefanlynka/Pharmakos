using System.Collections.Generic;
using UnityEngine;

public class AttackWithFollowerAction : GameAction
{
    public Follower Attacker;
    public ITarget Target;

    public AttackWithFollowerAction(Follower attacker, ITarget target)
    {
        Attacker = attacker;
        Target = target;
    }

    public override void Execute(bool simulated = false)
    {
        base.Execute(simulated);

        Attacker.AttackTarget(Target);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>(AnimationActions)
        {
            new AttackWithFollowerAnimation(this)
        };

        return animationActions;
    }
}
