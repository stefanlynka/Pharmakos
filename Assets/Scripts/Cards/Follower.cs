using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
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

    public List<FollowerEffect> InnateEffects = new List<FollowerEffect>();
    public Dictionary<StaticEffect, List<FollowerEffectInstance>> StaticEffects = new Dictionary<StaticEffect, List<FollowerEffectInstance>>();
    public SortedSet<TriggeredFollowerEffectInstance> OnEnterEffects = new SortedSet<TriggeredFollowerEffectInstance>();
    public SortedSet<TriggeredFollowerEffectInstance> OnDeathEffects = new SortedSet<TriggeredFollowerEffectInstance>();
    public SortedSet<TriggeredFollowerEffectInstance> OnAttackEffects = new SortedSet<TriggeredFollowerEffectInstance>();
    public SortedSet<TriggeredFollowerEffectInstance> OnAttackedEffects = new SortedSet<TriggeredFollowerEffectInstance>();
    public SortedSet<TriggeredFollowerEffectInstance> OnDamageEffects = new SortedSet<TriggeredFollowerEffectInstance>();
    public SortedSet<TriggeredFollowerEffectInstance> OnDamagedEffects = new SortedSet<TriggeredFollowerEffectInstance>();
    public SortedSet<TriggeredFollowerEffectInstance> OnDrawBloodEffects = new SortedSet<TriggeredFollowerEffectInstance>();
    public SortedSet<TriggeredFollowerEffectInstance> OnKillEffects = new SortedSet<TriggeredFollowerEffectInstance>();
    public SortedSet<TriggeredFollowerEffectInstance> OnTargetedEffects = new SortedSet<TriggeredFollowerEffectInstance>();

    //public static int IDIndex = 0;
    //public int Index = 0;

    public int TimesThisAttackedThisTurn = 0;
    public bool PlayedThisTurn = true;
    public bool SkippedAttack = false;
    public bool HasSprint { get { return StaticEffects.ContainsKey(StaticEffect.Sprint) && StaticEffects[StaticEffect.Sprint].Count > 0; } }
    public FollowerType Type = FollowerType.Mortal;

    public enum FollowerType
    {
        Monster,
        Mortal,
        Beast,
        Divine
    }




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

        if (HasStaticEffect(StaticEffect.RangedAttacker))
        {
            targets = otherPlayer.BattleRow.GetTargetsInRangeOfRangedAttack(position);
        }
        else
        {
            targets = otherPlayer.BattleRow.GetTargetsInRange(position);
        }
        

        return targets;
    }

    // Only trigger on your turn
    public virtual void DoStartOfMyTurnEffects()
    {
    }

    // Trigger on own and opponent's turn
    public virtual void DoStartOfEachTurnEffects()
    {
    }

    // Only trigger on your turn
    public virtual void DoEndOfMyTurnEffects()
    {
        PlayedThisTurn = false;
        TimesThisAttackedThisTurn = 0;
        SkippedAttack = false;
    }

    // Trigger on own and opponent's turn
    public virtual void DoEndOfEachTurnEffects()
    {

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
        StaticEffects = new Dictionary<StaticEffect, List<FollowerEffectInstance>>();
        OnEnterEffects = new SortedSet<TriggeredFollowerEffectInstance>();
        OnDeathEffects = new SortedSet<TriggeredFollowerEffectInstance>();
        OnAttackEffects = new SortedSet<TriggeredFollowerEffectInstance>();
        OnAttackedEffects = new SortedSet<TriggeredFollowerEffectInstance>();
        OnDamageEffects = new SortedSet<TriggeredFollowerEffectInstance>();
        OnDamagedEffects = new SortedSet<TriggeredFollowerEffectInstance>();
        OnDrawBloodEffects = new SortedSet<TriggeredFollowerEffectInstance>();
        OnKillEffects = new SortedSet<TriggeredFollowerEffectInstance>();
        OnTargetedEffects = new SortedSet<TriggeredFollowerEffectInstance>();

        InnateEffects = new List<FollowerEffect>();

        // When cloning a Follower, we clone each EffectDef and apply them all later once all Followers have been cloned
        if (original is Follower followerOriginal)
        {
            foreach (FollowerEffect effectDef in followerOriginal.InnateEffects)
            {
                FollowerEffect clonedEffectDef = effectDef.Clone();
                clonedEffectDef.EffectSource = this;
                InnateEffects.Add(clonedEffectDef);
            }
        }
    }

    // This assumes all InnateEffects are not yet applied
    public virtual void ReapplyEffects()
    {
        //SetupInnateEffects();
        ApplyInnateEffects();
    }

    // Each card overrides this and adds its its Innate Effects after this clears them
    public virtual void SetupInnateEffects()
    {
        //InnateEffects.Clear(); // Let's try not cleaning up InnateEffects for cloning purposes.
    }

    // Should be called only once when a follower is summoned (Or when re-applying effects)
    // Applies a follower's innate effects that affect itself or its neighbours
    public void ApplyInnateEffects()
    {
        foreach (FollowerEffect innateEffect in InnateEffects)
        {
            innateEffect.Apply(this);
        }
    }

    public void ApplyOnEnterEffects()
    {
        foreach (TriggeredFollowerEffectInstance effect in OnEnterEffects)
        {
            effect.Trigger();
        }
    }

    public bool HasStaticEffect(StaticEffect effect)
    {
        return StaticEffects.ContainsKey(effect) && StaticEffects[effect].Count > 0;
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
            if (value < 0)
            {
                sourceFollower.ApplyOnDamageEffects(this, -value);

                if (CurrentHealth <= 0) sourceFollower.ApplyOnKillEffects(this);
            }
        }

        ApplyOnDamagedEffects(source, -value);

        ResolveDamage();

        ApplyOnChanged();
    }

    public virtual void Heal(int value)
    {
        CurrentHealth = Math.Min(CurrentHealth + value, BaseHealth);

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
        foreach (TriggeredFollowerEffectInstance effect in OnDamageEffects)
        {
            effect.Trigger(target, amount);
        }
    }

    public void ApplyOnDamagedEffects(ITarget damageSource, int amount)
    {
        foreach (TriggeredFollowerEffectInstance effect in OnDamagedEffects)
        {
            effect.Trigger(damageSource, amount);
        }
    }

    public void ApplyOnKillEffects(ITarget target)
    {
        foreach (TriggeredFollowerEffectInstance effect in OnKillEffects)
        {
            effect.Trigger(target);
        }
    }

    public void ApplyOnDrawBloodEffects(ITarget target, int amount)
    {
        foreach (TriggeredFollowerEffectInstance effect in OnDrawBloodEffects)
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
        return (TimesThisAttackedThisTurn == 0 || (TimesThisAttackedThisTurn <= 1 && HasStaticEffect(StaticEffect.Frenzy))) && (!PlayedThisTurn || HasStaticEffect(StaticEffect.Sprint)) && !SkippedAttack;
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

        TimesThisAttackedThisTurn++;
        return false;
    }
    public void ApplyOnAttackEffects(ITarget target)
    {
        foreach (TriggeredFollowerEffectInstance effect in OnAttackEffects)
        {
            effect.Trigger(target);
        }
    }
    public void ApplyOnAttackedEffects(ITarget attacker)
    {
        foreach (TriggeredFollowerEffectInstance effect in OnAttackedEffects)
        {
            effect.Trigger(attacker);
        }
    }
    
    public void AttackFollower(Follower defender)
    {
        TimesThisAttackedThisTurn++;

        int defenderAttack = defender.GetCurrentAttack();
        int attackerAttack = GetCurrentAttack();

        Follower rightOfTarget = null;
        if (HasStaticEffect(StaticEffect.Cleave))
        {
            Follower leftOfTarget = null;
            int targetIndex = defender.Owner.BattleRow.GetIndexOfFollower(defender);
            if (targetIndex > 0)
            {
                leftOfTarget = defender.Owner.BattleRow.Followers[targetIndex - 1];
            }
            if (targetIndex < defender.Owner.BattleRow.Followers.Count - 1)
            {
                rightOfTarget = defender.Owner.BattleRow.Followers[targetIndex + 1];
            }

            leftOfTarget?.ChangeHealth(this, -attackerAttack);
        }

        if (!HasStaticEffect(StaticEffect.RangedAttacker)) ChangeHealth(defender, -defenderAttack);
        defender.ChangeHealth(this, -attackerAttack);

        rightOfTarget?.ChangeHealth(this, -attackerAttack);
    }

    public void AttackPlayer(Player player)
    {
        TimesThisAttackedThisTurn++;

        player.ChangeHealth(this, -GetCurrentAttack());
    }

    public void ApplyOnChanged()
    {
        if (GameState.IsSimulated) return;

        OnChange?.Invoke();
    }

    public void ApplyOnTargetedEffects()
    {
        foreach (TriggeredFollowerEffectInstance effect in OnTargetedEffects)
        {
            effect.Trigger(null);
        }
    }
}
