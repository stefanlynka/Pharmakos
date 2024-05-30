using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Follower : Card, ITarget
{
    public int BaseAttack;
    public int BaseHealth;

    public int CurrentAttack;
    public int CurrentHealth;

    public Action OnEnter;
    public Action OnDeath;

    //public static int IDIndex = 0;
    //public int Index = 0;

    public bool HasAttacked = false;
    public bool PlayedThisTurn = true;
    public bool HasSprint = false;


    public override List<ITarget> GetTargets()
    {
        return new List<ITarget>();
    }

    // Position Explanation:
    // Cards are 1.0 apart. Center = 0.
    // Odd number of Followers:  Center position is 0. Left of that is -1, another one left is -2, etc
    // Even number of Followers: Slightly left of center is -0.5, left of that is -1.5, etc
    public virtual List<ITarget> GetAttackTargets()
    {
        List<ITarget> targets = new List<ITarget>();
        if (!CanAttack()) return targets;

        int index = Owner.BattleRow.GetIndexOfFollower(this);
        if (index < 0) return targets;
        float position = -0.5f * (Owner.BattleRow.Followers.Count - 1) + index;

        Player otherPlayer = GameState.GetOtherPlayer(Owner.PlayerID);
        if (otherPlayer == null) return targets;

        targets = otherPlayer.BattleRow.GetTargetsInRange(position);
        

        return targets;
    }

    public virtual void DoEndOfTurnEffects()
    {
        PlayedThisTurn = false;
        HasAttacked = false;
    }

    public override void Reset()
    {
        CurrentAttack = BaseAttack;
        CurrentHealth = BaseHealth;
    }


    public void SetBaseStats(int attack, int health)
    {
        BaseAttack = attack;
        CurrentAttack = attack;

        BaseHealth = health;
        CurrentHealth = health;
    }

    public virtual void ChangeHealth(int value)
    {
        CurrentHealth += value;

        ResolveDamage();

        OnChange?.Invoke();
    }

    public virtual void ResolveDamage()
    {
        if (CurrentHealth <= 0) Die();
    }

    public virtual void Die()
    {
        Owner.FollowerDied(this);

        if (!Owner.GameState.IsSimulated)
        {
            OnRemove?.Invoke();
        }

        GameState.CurrentPlayer.ChangeOffering(OfferingType.Bone, 1);
    }

    public bool CanAttack()
    {
        return !HasAttacked && (!PlayedThisTurn || HasSprint);
    }

    public void AttackFollower(Follower defender)
    {
        HasAttacked = true;

        ChangeHealth(-defender.CurrentAttack);
        defender.ChangeHealth(-CurrentAttack);
    }

    public void AttackPlayer(Player player)
    {
        HasAttacked = true;

        player.ChangeHealth(-CurrentAttack);
    }
}
