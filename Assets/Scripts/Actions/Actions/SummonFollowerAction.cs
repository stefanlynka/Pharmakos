using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.UI.GridLayoutGroup;

public class SummonFollowerAction : GameAction
{
    public Follower Follower;
    public int Index;
    public SummonFollowerAction(Follower follower, int index = -1)
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
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        SummonFollowerAction copy = (SummonFollowerAction)MemberwiseClone();
        copy.Follower = newOwner.GameState.GetTargetByID<Follower>(Follower.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false)
    {
        Follower.Owner.SummonFollower(Follower, Index);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
