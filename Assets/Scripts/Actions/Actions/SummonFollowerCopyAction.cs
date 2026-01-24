using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonFollowerCopyAction : GameAction
{
    public Player Player;
    public Follower Follower;
    public SummonFollowerCopyAction(Player player, Follower follower)
    {
        Player = player;
        Follower = follower;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        SummonFollowerCopyAction copy = (SummonFollowerCopyAction)MemberwiseClone();
        copy.Player = newOwner.GameState.GetTargetByID<Player>(Player.GetID());
        copy.Follower = (Follower)Follower.MakeBaseCopy();

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Follower followerCopy = (Follower)Follower.MakeBaseCopy();
        if (followerCopy == null) return;
        followerCopy.Init(Player);

        GameAction newAction = new SummonFollowerAction(followerCopy);
        Player.GameState.ActionHandler.AddAction(newAction);

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
