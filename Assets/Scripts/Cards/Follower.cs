using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : Card, ITarget
{
    public int BaseAttack;
    public int BaseHealth;

    public int CurrentAttack;
    public int CurrentHealth;

    public Action OnEnter;
    public Action OnDeath;

    public static int IDIndex = 0;
    public int Index = 0;

    public Follower()
    {
        Index = IDIndex;
        IDIndex++;
    }

    public override List<ITarget> GetPlayableTargets()
    {
        return new List<ITarget>();
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

    public virtual void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
    }

    public virtual void ResolveDamage()
    {
        if (CurrentHealth <= 0) Die();
    }

    public virtual void Die()
    {
        Owner.FollowerDied(this);
    }
}
