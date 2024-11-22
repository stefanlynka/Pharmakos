using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Follower : Card, ITarget
{
    public int BaseAttack;
    public int BaseHealth;

    public int CurrentAttack;
    public int CurrentHealth;

    public Action OnEnter;
    public Action OnDeath;

    public List<EffectDef> InnateEffects = new List<EffectDef>();
    public Dictionary<StaticEffect, List<EffectInstance>> StaticEffects = new Dictionary<StaticEffect, List<EffectInstance>>();
    public SortedSet<TriggeredEffectInstance> OnEnterEffects = new SortedSet<TriggeredEffectInstance>();
    public SortedSet<TriggeredEffectInstance> OnDeathEffects = new SortedSet<TriggeredEffectInstance>();
    public SortedSet<TriggeredEffectInstance> OnAttackEffects = new SortedSet<TriggeredEffectInstance>();
    public SortedSet<TriggeredEffectInstance> OnAttackedEffects = new SortedSet<TriggeredEffectInstance>();
    public SortedSet<TriggeredEffectInstance> OnDamageEffects = new SortedSet<TriggeredEffectInstance>();
    public SortedSet<TriggeredEffectInstance> OnDamagedEffects = new SortedSet<TriggeredEffectInstance>();
    public SortedSet<TriggeredEffectInstance> OnDrawBloodEffects = new SortedSet<TriggeredEffectInstance>();
    public SortedSet<TriggeredEffectInstance> OnKillEffects = new SortedSet<TriggeredEffectInstance>();

    //public static int IDIndex = 0;
    //public int Index = 0;

    public bool HasAttacked = false;
    public bool PlayedThisTurn = true;
    public bool SkippedAttack = false;
    public bool HasSprint { get { return StaticEffects.ContainsKey(StaticEffect.Sprint) && StaticEffects[StaticEffect.Sprint].Count > 0; } }






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
        SkippedAttack = false;
    }

    public override void Reset()
    {
        CurrentAttack = BaseAttack;
        CurrentHealth = BaseHealth;
    }

    // Called when deep copying
    protected override void HandleCloned(Card original)
    {
        // These are all reference type so they need to be made anew
        InnateEffects = new List<EffectDef>();
        StaticEffects = new Dictionary<StaticEffect, List<EffectInstance>>();
        OnEnterEffects = new SortedSet<TriggeredEffectInstance>();
        OnDeathEffects = new SortedSet<TriggeredEffectInstance>();
        OnAttackEffects = new SortedSet<TriggeredEffectInstance>();
        OnAttackedEffects = new SortedSet<TriggeredEffectInstance>();
        OnDamageEffects = new SortedSet<TriggeredEffectInstance>();
        OnDamagedEffects = new SortedSet<TriggeredEffectInstance>();
        OnDrawBloodEffects = new SortedSet<TriggeredEffectInstance>();
        OnKillEffects = new SortedSet<TriggeredEffectInstance>();
    }

    // This assumes 
    public virtual void ReapplyEffects()
    {
        SetupInnateEffects();
        ApplyInnateEffects();
    }

    // Each card overrides this and adds its its Innate Effects after this clears them
    public virtual void SetupInnateEffects()
    {
        InnateEffects.Clear();
    }

    // Should be called only once when a follower is summoned (Or when re-applying effects)
    // Applies a follower's innate effects that affect itself or its neighbours
    public void ApplyInnateEffects()
    {
        foreach (EffectDef innateEffect in InnateEffects)
        {
            innateEffect.Apply(this);
        }
    }

    public void ApplyOnEnterEffects()
    {
        foreach (TriggeredEffectInstance effect in OnEnterEffects)
        {
            effect.Trigger();
        }
    }

    public void SetBaseStats(int attack, int health)
    {
        BaseAttack = attack;
        CurrentAttack = attack;

        BaseHealth = health;
        CurrentHealth = health;
    }

    public virtual int GetCurrentAttack()
    {
        return CurrentAttack;
    }

    public void ChangeStats(int attackChange, int healthChange)
    {
        BaseAttack += attackChange;
        CurrentAttack += attackChange;

        BaseHealth += healthChange;
        CurrentHealth += healthChange;

        ResolveDamage();

        ApplyOnChanged();
    }

    public virtual void ChangeHealth(ITarget source, int value)
    {
        CurrentHealth += value;

        Follower sourceFollower = source as Follower;
        if (sourceFollower != null)
        {
            if (value < 0) sourceFollower.ApplyOnDamageEffects(this, -value);

            if (CurrentHealth <= 0) sourceFollower.ApplyOnKillEffects(this);
        }

        ApplyOnDamagedEffects(source, -value);

        ResolveDamage();

        ApplyOnChanged();
    }

    public virtual void ResolveDamage()
    {
        if (CurrentHealth <= 0) Die();
    }

    public virtual void Die()
    {
        GameAction newAction = new FollowerDeathAction(this);
        GameState.ActionHandler.AddAction(newAction);
    }

    public void ApplyOnDamageEffects(ITarget target, int amount)
    {
        foreach (TriggeredEffectInstance effect in OnDamageEffects)
        {
            effect.Trigger(target, amount);
        }
    }

    public void ApplyOnDamagedEffects(ITarget damageSource, int amount)
    {
        foreach (TriggeredEffectInstance effect in OnDamagedEffects)
        {
            effect.Trigger(damageSource, amount);
        }
    }

    public void ApplyOnKillEffects(ITarget target)
    {
        foreach (TriggeredEffectInstance effect in OnKillEffects)
        {
            effect.Trigger(target);
        }
    }

    public void ApplyOnDrawBloodEffects(ITarget target, int amount)
    {
        foreach (TriggeredEffectInstance effect in OnDrawBloodEffects)
        {
            effect.Trigger(target, amount);
        }
    }
    //public void ApplyOnKillEffects(ITarget source)
    //{
    //    Follower followerSource = source as Follower;
    //    if (followerSource != null)
    //    {
    //        foreach (TriggeredEffectInstance effect in followerSource.OnKillEffects)
    //        {
    //            effect.Trigger(this);
    //        }
    //    }
    //}

    public bool CanAttack()
    {
        return !HasAttacked && (!PlayedThisTurn || HasSprint) && !SkippedAttack;
    }

    // Return true if attack happened
    public bool AttackTarget(ITarget target)
    {
        ApplyOnAttackEffects(target);

        Follower defendingFollower = target as Follower;
        Player defendingPlayer = target as Player;

        if (defendingFollower != null)
        {
            defendingFollower.ApplyOnAttackedEffects(this);

            if (CurrentHealth > 0 && defendingFollower.CurrentHealth > 0)
            {
                AttackFollower(defendingFollower);
                return true;
            }
        }
        else if (defendingPlayer != null && CurrentHealth > 0 && defendingPlayer.Health > 0)
        {
            AttackPlayer(defendingPlayer);
            return true;
        }

        HasAttacked = true;
        return false;
    }
    public void ApplyOnAttackEffects(ITarget target)
    {
        foreach (TriggeredEffectInstance effect in OnAttackEffects)
        {
            effect.Trigger(target);
        }
    }
    public void ApplyOnAttackedEffects(ITarget attacker)
    {
        foreach (TriggeredEffectInstance effect in OnAttackedEffects)
        {
            effect.Trigger(attacker);
        }
    }
    
    public void AttackFollower(Follower defender)
    {
        HasAttacked = true;

        int defenderAttack = defender.GetCurrentAttack();
        int attackerAttack = GetCurrentAttack();

        ChangeHealth(defender, -defenderAttack);
        defender.ChangeHealth(this, -attackerAttack);
    }

    public void AttackPlayer(Player player)
    {
        HasAttacked = true;

        player.ChangeHealth(this, -GetCurrentAttack());
    }

    public void ApplyOnChanged()
    {
        if (GameState.IsSimulated) return;

        OnChange?.Invoke();
    }
}
