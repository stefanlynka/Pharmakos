using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FollowerDeathAction : GameAction
{
    public Follower Follower;
    public FollowerDeathAction(Follower follower)
    {
        Follower = follower;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        SummonFollowerAction copy = (SummonFollowerAction)MemberwiseClone();
        copy.Follower = newOwner.GameState.GetTargetByID<Follower>(Follower.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false)
    {
        Player Owner = Follower.Owner;
        Owner.FollowerDied(Follower);

        Owner.GameState.CurrentPlayer.ChangeOffering(OfferingType.Bone, 1);
        Owner.GameState.TargetsByID.Remove(Follower.ID);

        Owner.GameState.FireFollowerDies(Follower);
        Owner.GameState.FollowerDeathsThisTurn++;
        Owner.GameState.LastFollowerThatDied = Follower.MakeBaseCopy() as Follower;

        //if (!simulated) Debug.LogError("Follower Death Action");

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new FollowerDeathAnimation(this)
        };
        return animationActions;
    }
}
