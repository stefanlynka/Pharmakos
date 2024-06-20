using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonFollowerAction : GameAction
{
    public Follower Follower;
    public int Index;
    public SummonFollowerAction(Follower follower, int index)
    {
        Follower = follower;
        Index = index;
    }

    public override void Execute(bool simulated = false)
    {
        Follower.Owner.SummonFollower(Follower, Index);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>(AnimationActions)
        {
            new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
