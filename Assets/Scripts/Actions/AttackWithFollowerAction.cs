using System.Collections.Generic;
using UnityEngine;

public class AttackWithFollowerAction : GameAction
{
    public Follower Attacker;
    public ITarget Target;

    private bool attackHappened = false;

    public AttackWithFollowerAction(Follower attacker, ITarget target)
    {
        Attacker = attacker;
        Target = target;
    }

    public override void Execute(bool simulated = false)
    {
        attackHappened = Attacker.AttackTarget(Target);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        if (!attackHappened) return new List<AnimationAction>();

        List<AnimationAction> animationActions = new List<AnimationAction>(AnimationActions)
        {
            new AttackWithFollowerAnimation(this)
        };

        return animationActions;
    }
}
