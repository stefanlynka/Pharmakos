using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class SummonLastDeadFollowerAction : GameAction
{
    public Player Player;
    public SummonLastDeadFollowerAction(Player player)
    {
        Player = player;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        SummonLastDeadFollowerAction copy = (SummonLastDeadFollowerAction)MemberwiseClone();
        copy.Player = newOwner.GameState.GetTargetByID<Player>(Player.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        if (Player.GameState.LastFollowerThatDied == null) return;
        Follower followerCopy = Player.GameState.LastFollowerThatDied.MakeBaseCopy() as Follower;
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
            //new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
