using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class HealAction : GameAction
{
    ITarget target;
    ITarget source;
    int amount = 0;

    public HealAction(ITarget target, ITarget source, int amount)
    {
        this.target = target;
        this.source = source;
        this.amount = amount;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        HealAction copy = (HealAction)MemberwiseClone();
        copy.target = newOwner.GameState.GetTargetByID<ITarget>(target.GetID());
        copy.source = newOwner.GameState.GetTargetByID<ITarget>(source.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false)
    {
        Follower followerTarget = target as Follower;
        Player playerTarget = target as Player;

        if (followerTarget != null)
        {
            followerTarget.Heal(amount);
        }
        else if (playerTarget != null)
        {
            playerTarget.ChangeHealth(source, amount);
        }

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
