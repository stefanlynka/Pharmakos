using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class SummonSacrificedFollowerAction : GameAction
{
    public Player Player;
    public SummonSacrificedFollowerAction(Player player)
    {
        Player = player;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        SummonSacrificedFollowerAction copy = (SummonSacrificedFollowerAction)MemberwiseClone();
        copy.Player = newOwner.GameState.GetTargetByID<Player>(Player.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        List<Follower> sacrificedFollowers = Controller.Instance.SacrificedFollowers;
        if (sacrificedFollowers.Count == 0) return;

        Follower followerCopy = (Follower)sacrificedFollowers[Player.GameState.RNG.Next(0, sacrificedFollowers.Count - 1)].MakeBaseCopy();
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
