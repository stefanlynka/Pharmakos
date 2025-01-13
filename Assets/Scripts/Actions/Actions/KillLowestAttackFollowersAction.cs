using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;

public class KillLowestAttackFollowersAction : GameAction
{
    public ITarget Source;

    public KillLowestAttackFollowersAction(ITarget source)
    {
        Source = source;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        KillLowestAttackFollowersAction copy = (KillLowestAttackFollowersAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false)
    {
        Player playerSource = Source as Player;
        if (playerSource == null)
        {
            Follower followerSource = Source as Follower;
            if (followerSource == null) return;

            playerSource = followerSource.Owner;
        }

        int lowestAttack = 9999;
        List<Follower> lowestAttackFollowers = new List<Follower>();

        List<Follower> allFollowers = ITarget.GetAllFollowers(playerSource);

        foreach (Follower follower in allFollowers)
        {
            int currentAttack = follower.GetCurrentAttack();
            if (currentAttack == lowestAttack)
            {
                lowestAttackFollowers.Add(follower);
            }
            else if (currentAttack < lowestAttack)
            {
                lowestAttackFollowers.Clear();
                lowestAttackFollowers.Add(follower);
                lowestAttack = currentAttack;
            }
        }

        foreach (Follower follower in lowestAttackFollowers)
        {
            KillFollowerAction killAction = new KillFollowerAction(playerSource, follower);
            playerSource.GameState.ActionHandler.AddAction(killAction);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new DamageAnimation(this)
        };
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }

}
