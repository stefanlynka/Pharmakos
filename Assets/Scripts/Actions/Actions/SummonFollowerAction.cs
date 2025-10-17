using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonFollowerAction : GameAction
{
    public Follower Follower;
    public int Index;
    public bool CreateCrops = true;
    public SummonFollowerAction(Follower follower, int index = -1, bool createCrops = true)
    {
        Follower = follower;
        if (index == -1) 
        {
            Index = Follower.Owner.BattleRow.Followers.Count;
        }
        else
        {
            Index = index;
        }

        CreateCrops = createCrops;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        SummonFollowerAction copy = (SummonFollowerAction)MemberwiseClone();
        copy.Follower = newOwner.GameState.GetTargetByID<Follower>(Follower.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        bool success = Follower.Owner.SummonFollower(Follower, Index, CreateCrops);

        base.Execute(simulated, success);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new MoveCardAnimation(this, Follower, Follower.Owner, GameZone.Hand, Follower.Owner, GameZone.BattleRow, 0.25f, Index),
            //new SummonFollowerAnimation(this)
        };
        return animationActions;
    }

    public override void LogAction()
    {
        Debug.LogWarning("SummonFollowerAction: " + Follower.Owner.GetName() + " summons " + Follower.GetName());
    }
}
