using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class PlayFollowerAction : GameAction
{
    public Follower Follower;
    public int Index;
    public PlayFollowerAction(Follower follower, int index)
    {
        Follower = follower;
        Index = index;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        PlayFollowerAction copy = (PlayFollowerAction)MemberwiseClone();
        copy.Follower = newOwner.GameState.GetTargetByID<Follower>(Follower.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Follower.Owner.PlayCard(Follower);

        GameAction newAction = new SummonFollowerAction(Follower, Index);
        Follower.GameState.ActionHandler.AddAction(newAction);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            //new PlayFollowerAnimation(this)
        };
        return animationActions;
    }

    public override void LogAction()
    {
        Debug.LogWarning(Follower.Owner.GetName() + " played " + Follower.GetName());
    }
}
