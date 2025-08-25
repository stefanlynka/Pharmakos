using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class DealDamageToAllCharactersAction : GameAction
{
    public ITarget Source;
    public int Damage;

    public DealDamageToAllCharactersAction(ITarget source, int damage)
    {
        Source = source;
        Damage = damage;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        DealDamageToAllCharactersAction copy = (DealDamageToAllCharactersAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Player playerSource = Source as Player;
        if (playerSource == null)
        {
            Follower followerSource = Source as Follower;
            if (followerSource == null) return;

            playerSource = followerSource.Owner;
        }

        // Owner Followers
        List<Follower> myFollowers = new List<Follower>(playerSource.BattleRow.Followers);
        foreach (Follower follower in myFollowers)
        {
            DealDamageAction damageAction = new DealDamageAction(Source, follower, Damage);
            playerSource.GameState.ActionHandler.AddAction(damageAction);
        }

        Player otherPlayer = playerSource.GetOtherPlayer();
        // Enemy Followers
        List<Follower> enemyFollowers = new List<Follower>(otherPlayer.BattleRow.Followers);
        foreach (Follower follower in enemyFollowers)
        {
            DealDamageAction damageAction = new DealDamageAction(Source, follower, Damage);
            playerSource.GameState.ActionHandler.AddAction(damageAction);
        }

        // Owner
        DealDamageAction damagePlayerAction = new DealDamageAction(Source, playerSource, Damage);
        playerSource.GameState.ActionHandler.AddAction(damagePlayerAction);

        // Enemy
        DealDamageAction damageOtherAction = new DealDamageAction(Source, otherPlayer, Damage);
        playerSource.GameState.ActionHandler.AddAction(damageOtherAction);

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
