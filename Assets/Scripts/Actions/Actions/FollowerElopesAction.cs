using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowerElopesAction : GameAction
{
    public Follower Follower;

    public FollowerElopesAction(Follower follower)
    {
        Follower = follower;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        FollowerElopesAction copy = (FollowerElopesAction)MemberwiseClone();
        copy.Follower = newOwner.GameState.GetTargetByID<Follower>(Follower.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        // Get target
        Follower target = Follower.GetClosestEnemy();

        if (target != null)
        {
            // Remove Follower and target
            // TODO: This should be exile, not die
            Follower.Die();
            target.Die();
        }

        base.Execute(simulated);
    }


    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
        };
        return animationActions;
    }
}
