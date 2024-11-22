using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectTarget
{
    Self,
    Global,
    AdjacentAllies
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
}

public enum StaticEffect
{
    None,
    Sprint
}

/**
 * A Follower may have an EffectDef that applies an EffectInstance to itself, its neighbours, or others
 */
public abstract class EffectDef
{
    public List<EffectInstance> EffectInstances = new List<EffectInstance>();
    public Follower EffectSource; // The follower that owns+creates the effect.
    public EffectTarget TargetType;

    public EffectDef(EffectTarget target)
    {
        TargetType = target;
    }

    public virtual void Apply(Follower source)
    {
        EffectSource = source;

        // Listeners for reapplying the effect
        switch (TargetType)
        {
            case EffectTarget.Self:
                // Apply effect instance to self
                ApplyInstance(EffectSource);

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
                        ApplyInstance(leftNeighbour);
                    }
                }
                if (sourceIndex < followersInBattleRow - 1)
                {
                    Follower rightNeighbour = source.Owner.BattleRow.Followers[sourceIndex + 1];
                    if (rightNeighbour != null)
                    {
                        ApplyInstance(rightNeighbour);
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

    protected virtual void Unapply()
    {
        switch (TargetType)
        {
            case EffectTarget.AdjacentAllies:
                EffectSource.GameState.FollowerEnters -= Reapply;
                EffectSource.GameState.FollowerDies -= Reapply;
                break;
        }

        foreach (EffectInstance effectInstance in EffectInstances)
        {
            effectInstance.Unapply();
        }

        EffectInstances.Clear();
    }

    protected virtual void ApplyInstance(Follower instanceTarget){}
}

public class TriggeredEffectDef : EffectDef
{
    public TriggeredEffectDef(EffectTarget target) : base(target){}
}

public class StaticEffectDef : EffectDef
{
    StaticEffect staticEffectName = StaticEffect.None;
    public StaticEffectDef(EffectTarget target, StaticEffect staticEffectName) : base(target)
    {
        this.staticEffectName = staticEffectName;
    }

    public override void Apply(Follower source)
    {
        base.Apply(source);
    }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        StaticEffectInstance newEffectInstance = new StaticEffectInstance(this, instanceTarget, 0, 0, staticEffectName);
        EffectInstances.Add(newEffectInstance);
    }
}

// On Attack, summon two 1/1s
public class AgamemnonSummonEffectDef : EffectDef
{
    public AgamemnonSummonEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(this, instanceTarget, 0, 0, EffectTrigger.OnAttack);
        EffectInstances.Add(newEffectInstance);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(this, instanceTarget, 0, 1, EffectTrigger.OnAttack);
        newEffectInstance2.SummonPositionOffset = 1;
        EffectInstances.Add(newEffectInstance2);
    }
}

// When Damaged, summon two 1/1s
public class MenelausSummonEffectDef : EffectDef
{
    public MenelausSummonEffectDef(EffectTarget target) : base(target){}

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(this, instanceTarget, 0, 0, EffectTrigger.OnDamaged);
        EffectInstances.Add(newEffectInstance);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(this, instanceTarget, 0, 1, EffectTrigger.OnDamaged);
        newEffectInstance2.SummonPositionOffset = 1;
        EffectInstances.Add(newEffectInstance2);
    }
}

// On Kill: Gain 1 Gold
public class PyrrhusEffectDef : EffectDef
{
    public PyrrhusEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        ChangeResourceInstance newEffectInstance = new ChangeResourceInstance(this, instanceTarget, 0, 0, EffectTrigger.OnKill);
        newEffectInstance.Init(OfferingType.Gold, 1);

        EffectInstances.Add(newEffectInstance);
    }
}

// On Draw Blood: Gain +1/+1
public class DiomedesEffectDef : EffectDef
{
    public DiomedesEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(this, instanceTarget, 0, 0, EffectTrigger.OnDrawBlood);
        newEffectInstance.Init(1, 1);

        EffectInstances.Add(newEffectInstance);
    }
}

// On Kill: Add free Smite to hand
public class AtalantaEffectDef : EffectDef
{
    public AtalantaEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(this, instanceTarget, 0, 0, EffectTrigger.OnKill);
        Smite newCard = new Smite();
        newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);

        EffectInstances.Add(newEffectInstance);
    }
}






















public class TrojanHorseSummonEffectDef : EffectDef
{
    public TrojanHorseSummonEffectDef(EffectTarget target) : base(target){}

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        SummonPeltastInstance newEffectInstance1 = new SummonPeltastInstance(this, instanceTarget, 0, 0, EffectTrigger.OnDeath);
        EffectInstances.Add(newEffectInstance1);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(this, instanceTarget, 0, 1, EffectTrigger.OnDeath);
        EffectInstances.Add(newEffectInstance2);

        SummonPeltastInstance newEffectInstance3 = new SummonPeltastInstance(this, instanceTarget, 0, 2, EffectTrigger.OnDeath);
        EffectInstances.Add(newEffectInstance3);
    }
}

public class DamageOnAttackEffectDef : EffectDef
{
    public DamageOnAttackEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        DamageTargetInstance newEffectInstance = new DamageTargetInstance(this, instanceTarget, 0, 0, EffectTrigger.OnAttack);
        EffectInstances.Add(newEffectInstance);
    }
}

public class DamageOnAttackedEffectDef : EffectDef
{
    public DamageOnAttackedEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        DamageTargetInstance newEffectInstance = new DamageTargetInstance(this, instanceTarget, 0, 0, EffectTrigger.OnAttacked);

        EffectInstances.Add(newEffectInstance);
    }
}

public class SummonOnDamageEffectDef : EffectDef
{
    public SummonOnDamageEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(this, instanceTarget, 0, 0, EffectTrigger.OnDamage);
        newEffectInstance.SummonPositionOffset = 1;

        EffectInstances.Add(newEffectInstance);
    }
}

public class SummonOnDamagedEffectDef : EffectDef
{
    public SummonOnDamagedEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(this, instanceTarget, 0, 0, EffectTrigger.OnDamaged);
        newEffectInstance.SummonPositionOffset = 1;

        EffectInstances.Add(newEffectInstance);
    }
}

public class SummonOnDrawBloodEffectDef : EffectDef
{
    public SummonOnDrawBloodEffectDef(EffectTarget target) : base(target) { }

    protected override void ApplyInstance(Follower instanceTarget)
    {
        base.ApplyInstance(instanceTarget);

        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(this, instanceTarget, 0, 0, EffectTrigger.OnDrawBlood);
        newEffectInstance.SummonPositionOffset = 1;

        EffectInstances.Add(newEffectInstance);
    }
}