using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakeControlOfFollowerAction : GameAction
{
    public Player OldOwner;
    public Player NewOwner;
    public Follower Follower;

    private int targetIndex = -1;

    public TakeControlOfFollowerAction(Player newOwner, Follower follower)
    {
        NewOwner = newOwner;
        Follower = follower;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        TakeControlOfFollowerAction copy = (TakeControlOfFollowerAction)MemberwiseClone();
        copy.Follower = newOwner.GameState.GetTargetByID<Follower>(Follower.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        if (Follower.Owner == NewOwner) return;

        // Remove Follower from current owner
        Follower.Owner.BattleRow.RemoveFollower(Follower);
        //Follower.Owner.BattleRow.ReapplyAllEffects();

        // Add Follower to new Owner's Battlerow
        OldOwner = Follower.Owner;
        Follower.Owner = NewOwner;
        targetIndex = Mathf.Min(Follower.Owner.BattleRow.Followers.Count);
        NewOwner.BattleRow.AddFollower(Follower);
        //NewOwner.BattleRow.ReapplyAllEffects();

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new MoveCardAnimation(this, Follower, OldOwner, GameZone.BattleRow, NewOwner, GameZone.BattleRow, 0.5f, targetIndex),
        };
        return animationActions;
    }
}
