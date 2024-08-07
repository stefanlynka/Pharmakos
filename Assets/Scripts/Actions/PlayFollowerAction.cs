using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayFollowerAction : GameAction
{
    public Follower Follower;
    public int Index;
    public PlayFollowerAction(Follower follower, int index)
    {
        Follower = follower;
        Index = index;
    }

    public override void Execute(bool simulated = false)
    {
        Follower.Owner.PlayCard(Follower);

        GameAction newAction = new SummonFollowerAction(Follower, Index);
        Follower.GameState.ActionHandler.AddAction(newAction);
        Follower.GameState.ActionHandler.StartEvaluating();

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>(AnimationActions)
        {
            //new PlayFollowerAnimation(this)
        };
        return animationActions;
    }
}
