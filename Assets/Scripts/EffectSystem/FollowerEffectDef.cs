using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectTarget
{
    Self,
    Global,
    AdjacentAllies,
    EffectSource,
}

public enum EffectTrigger
{
    None,
    OnEnter,
    OnDeath,
    OnAttack,
    OnAttacked,
    OnDamage,
    OnDamaged,
    OnDrawBlood,
    OnKill,
    OnTargeted,
    OnAnyFollowerDeath,
    OnAnyFollowerEnter,
    OnAnySpellCast,
}

public enum StaticEffect
{
    None,
    Sprint, // Can attack immediately on entering
    Shield, // Reduce damage taken by 1
    ImmuneWhileAttacking,
    Cleave, // Attack hits adjacent enemies
    Taunt, // Must be attacked first
    RangedAttacker, // Inverted attacking and takes no damage while attacking
    Frenzy, // 2 attacks per turn
    CantAttack,
    LowVision, // Can only attack directly in front
}

/**
 * A Follower may have an EffectDef that applies an EffectInstance to itself, its neighbours, or others
 */
public abstract class FollowerEffect
{
    public List<FollowerEffectInstance> EffectInstances = new List<FollowerEffectInstance>();
    public Follower EffectSource; // The follower that owns+creates the effect.
    public EffectTarget TargetType;

    public FollowerEffect(EffectTarget target)
    {
        TargetType = target;
    }

    // Let's Clone
    public FollowerEffect Clone()
    {
        FollowerEffect newEffect = (FollowerEffect)MemberwiseClone();
        newEffect.EffectInstances = new List<FollowerEffectInstance>();

        return newEffect;
    }

    public virtual void Apply(Follower source)
    {
        EffectSource = source;

        // Listeners for reapplying the effect
        switch (TargetType)
        {
            case EffectTarget.Self:
                // Apply effect instance to self
                ApplyInstance(EffectSource, 0);

                break;
            case EffectTarget.AdjacentAllies:
                // Listen to retarget when the battlerow changes
                source.GameState.FollowerEnters += Reapply;
                source.GameState.FollowerDies += Reapply;

                // Apply effect instances to neighbours
                int sourceIndex = source.Owner.BattleRow.GetIndexOfFollower(source);
                int followersInBattleRow = source.Owner.BattleRow.Followers.Count;
                if (followersInBattleRow <= 1) break;

                if (sourceIndex > 0)
                {
                    Follower leftNeighbour = source.Owner.BattleRow.Followers[sourceIndex - 1];
                    if (leftNeighbour != null)
                    {
                        ApplyInstance(leftNeighbour, -1);
                    }
                }
                if (sourceIndex < followersInBattleRow - 1)
                {
                    Follower rightNeighbour = source.Owner.BattleRow.Followers[sourceIndex + 1];
                    if (rightNeighbour != null)
                    {
                        ApplyInstance(rightNeighbour, 1);
                    }
                }

                break;
        }
    }

    protected virtual void Reapply(Follower follower)
    {
        Reapply();
    }

    protected virtual void Reapply()
    {
        Unapply();
        Apply(EffectSource);
    }

    public virtual void Unapply()
    {
        switch (TargetType)
        {
            case EffectTarget.AdjacentAllies:
                EffectSource.GameState.FollowerEnters -= Reapply;
                EffectSource.GameState.FollowerDies -= Reapply;
                break;
        }

        foreach (FollowerEffectInstance effectInstance in EffectInstances)
        {
            effectInstance.Unapply();
        }

        EffectInstances.Clear();
    }

    protected virtual void ApplyInstance(Follower instanceTarget, int offset){}

    public static List<Follower> GetTargets(Follower source, EffectTarget targetType, FollowerEffect effectDef)
    {
        List<Follower> targets = new List<Follower>();

        switch(targetType)
        {
            case EffectTarget.Self:
                targets.Add(source);
                break;
            case EffectTarget.AdjacentAllies:
                targets.AddRange(source.Owner.BattleRow.GetAdjacentFollowers(source));
                break;
            case EffectTarget.Global:
                targets.AddRange(source.Owner.BattleRow.Followers);
                break;
            case EffectTarget.EffectSource:
                targets.Add(effectDef.EffectSource);
                break;
        }

        return targets;
    }
}

public class CustomEffectDef : FollowerEffect
{
    public Action<FollowerEffect, Follower, int> ApplyInstanceAction = null;
    public CustomEffectDef(EffectTarget target) : base(target){}

    protected override void ApplyInstance(Follower source, int offset)
    {
        base.ApplyInstance(source, offset);

        ApplyInstanceAction?.Invoke(this, source, offset);
    }
}

public class StaticEffectDef : FollowerEffect
{
    public StaticEffect StaticEffectName = StaticEffect.None;
    public StaticEffectDef(EffectTarget target, StaticEffect staticEffectName) : base(target)
    {
        this.StaticEffectName = staticEffectName;
    }

    public override void Apply(Follower source)
    {
        base.Apply(source);
    }

    protected override void ApplyInstance(Follower instanceTarget, int offset)
    {
        StaticFollowerEffectInstance newEffectInstance = new StaticFollowerEffectInstance(this, instanceTarget, 0, 0, StaticEffectName);
        EffectInstances.Add(newEffectInstance);
    }
}

// Heal player per damage dealt
public class LifeSiphonEffectDef : FollowerEffect
{
    public LifeSiphonEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget, int offset)
    {
        base.ApplyInstance(instanceTarget, 0);

        ChangePlayerHealthInstance newEffectInstance = new ChangePlayerHealthInstance(this, instanceTarget, 0, 0, EffectTrigger.OnDamage);
        newEffectInstance.Init(0, true, true);

        EffectInstances.Add(newEffectInstance);
    }
}

public class DealAttackDamageOnAttackEffectDef : FollowerEffect
{
    public DealAttackDamageOnAttackEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget, int offset)
    {
        base.ApplyInstance(instanceTarget, 0);

        DamageTargetInstance newEffectInstance = new DamageTargetInstance(this, instanceTarget, 0, 0, EffectTrigger.OnAttack);
        newEffectInstance.Init(0, true);

        EffectInstances.Add(newEffectInstance);
    }
}

// Deal this Follower's attack damage to attacker when attacked
public class DealAttackDamageOnAttackedEffectDef : FollowerEffect
{
    public DealAttackDamageOnAttackedEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget, int offset)
    {
        base.ApplyInstance(instanceTarget, 0);

        DamageTargetInstance newEffectInstance = new DamageTargetInstance(this, instanceTarget, 0, 0, EffectTrigger.OnAttacked);
        newEffectInstance.Init(0, true);

        EffectInstances.Add(newEffectInstance);
    }
}

// Deal a constant amount of damage to attacker when attacked
public class DamageOnAttackedEffectDef : FollowerEffect
{
    private int damage = 0;
    public DamageOnAttackedEffectDef(EffectTarget target, int damage) : base(target) 
    {
        this.damage = damage;
    }

    protected override void ApplyInstance(Follower instanceTarget, int offset)
    {
        base.ApplyInstance(instanceTarget, 0);

        DamageTargetInstance newEffectInstance = new DamageTargetInstance(this, instanceTarget, 0, 0, EffectTrigger.OnAttacked);
        newEffectInstance.Init(damage);

        EffectInstances.Add(newEffectInstance);
    }
}

