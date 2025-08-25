using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefreshFollowerAttackAction : GameAction
{
    ITarget target;
    ITarget source;
    int amount = 0;

    public RefreshFollowerAttackAction(ITarget target, ITarget source, int amount)
    {
        this.target = target;
        this.source = source;
        this.amount = amount;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        RefreshFollowerAttackAction copy = (RefreshFollowerAttackAction)MemberwiseClone();
        copy.target = newOwner.GameState.GetTargetByID<ITarget>(target.GetID());
        copy.source = newOwner.GameState.GetTargetByID<ITarget>(source.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        Follower followerTarget = target as Follower;
        if (followerTarget == null) return;

        followerTarget.TimesThisAttackedThisTurn -= amount;

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
