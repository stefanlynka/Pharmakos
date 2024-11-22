using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;

public class EffectInstance : IComparable<EffectInstance>
{
    public EffectDef EffectDef;
    public Follower AffectedFollower; // The follower this EffectInstance is applied to
    public int OffsetFromOwner = 0; // -1: Affected Follower is one space left of EffectDef Owner. 0: Affected Follower is EffectDef Owner. Etc...
    public int EffectNum = 0; // This is the nth effect added

    public EffectInstance(EffectDef def, Follower affectedFollower, int offset = 0, int effectNum = 0)
    {
        EffectDef = def;
        AffectedFollower = affectedFollower;
        OffsetFromOwner = offset;
        EffectNum = effectNum;
    }

    // Required So Followers can have SortedSet<EffectInstance>'s
    public int CompareTo(EffectInstance other)
    {
        if (OffsetFromOwner == other.OffsetFromOwner) return EffectNum - other.EffectNum;

        return OffsetFromOwner - other.OffsetFromOwner;
    }

    // Applies static effects and sets up listeners
    protected virtual void Apply(){}

    // Removes static effects and listeners
    public virtual void Unapply(){}
}

public class StaticEffectInstance : EffectInstance
{
    public StaticEffect StaticEffectName = StaticEffect.None;
    public StaticEffectInstance(EffectDef def, Follower affectedFollower, int offset = 0, int effectNum = 0, StaticEffect staticEffectName = StaticEffect.None) : base(def, affectedFollower, offset, effectNum)
    {
        StaticEffectName = staticEffectName;

        Apply();
    }

    protected override void Apply()
    {
        base.Apply();

        if (AffectedFollower != null)
        {
            AffectedFollower.StaticEffects.AddToList(StaticEffectName, this);
        }
    }

    public override void Unapply()
    {
        base.Unapply();

        if (AffectedFollower != null)
        {
            if (AffectedFollower.StaticEffects.ContainsKey(StaticEffectName))
            {
                AffectedFollower.StaticEffects[StaticEffectName].Remove(this);
            }
        }
    }
}

public class TriggeredEffectInstance: EffectInstance
{
    public EffectTrigger effectTrigger = EffectTrigger.None;
    public TriggeredEffectInstance(EffectDef def, Follower affectedFollower, int offset = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offset, effectNum)
    {
        this.effectTrigger = effectTrigger;

        Apply();
    }

    protected override void Apply()
    {
        base.Apply();

        if (AffectedFollower == null) return;

        switch (effectTrigger)
        {
            case EffectTrigger.OnEnter:
                AffectedFollower.OnEnterEffects.Add(this);
                break;
            case EffectTrigger.OnDeath:
                AffectedFollower.OnDeathEffects.Add(this);
                break;
            case EffectTrigger.OnAttack:
                AffectedFollower.OnAttackEffects.Add(this);
                break;
            case EffectTrigger.OnAttacked:
                AffectedFollower.OnAttackedEffects.Add(this);
                break;
            case EffectTrigger.OnDamage:
                AffectedFollower.OnDamageEffects.Add(this);
                break;
            case EffectTrigger.OnDamaged:
                AffectedFollower.OnDamagedEffects.Add(this);
                break;
            case EffectTrigger.OnDrawBlood:
                AffectedFollower.OnDrawBloodEffects.Add(this);
                break;
            case EffectTrigger.OnKill:
                AffectedFollower.OnKillEffects.Add(this);
                break;
        }
    }

    public override void Unapply()
    {
        base.Unapply();

        if (AffectedFollower == null) return;

        switch (effectTrigger)
        {
            case EffectTrigger.OnEnter:
                AffectedFollower.OnEnterEffects.Remove(this);
                break;
            case EffectTrigger.OnDeath:
                AffectedFollower.OnDeathEffects.Remove(this);
                break;
            case EffectTrigger.OnAttack:
                AffectedFollower.OnAttackEffects.Remove(this);
                break;
            case EffectTrigger.OnAttacked:
                AffectedFollower.OnAttackedEffects.Remove(this);
                break;
            case EffectTrigger.OnDamage:
                AffectedFollower.OnDamageEffects.Remove(this);
                break;
            case EffectTrigger.OnDamaged:
                AffectedFollower.OnDamagedEffects.Remove(this);
                break;
            case EffectTrigger.OnDrawBlood:
                AffectedFollower.OnDrawBloodEffects.Remove(this);
                break;
            case EffectTrigger.OnKill:
                AffectedFollower.OnKillEffects.Remove(this);
                break;
        }
    }

    public virtual void Trigger(ITarget target = null, int amount = 0){}
}

public class SummonPeltastInstance : TriggeredEffectInstance
{
    public int SummonPositionOffset = 0;
    public SummonPeltastInstance(EffectDef def, Follower owner, int offset = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, owner, offset, effectNum, effectTrigger) { }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        Peltast peltast = new Peltast();
        peltast.Init(AffectedFollower.Owner);

        //peltast.CurrentAttack = 1;
        //peltast.CurrentHealth = 1;
        int index = AffectedFollower.Owner.BattleRow.GetIndexOfFollower(AffectedFollower) + SummonPositionOffset;

        GameAction newAction = new SummonFollowerAction(peltast, index);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

public class DamageTargetInstance : TriggeredEffectInstance
{
    public DamageTargetInstance(EffectDef def, Follower affectedFollower, int offset = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offset, effectNum, effectTrigger) { }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        DealDamageAction damageAction = new DealDamageAction(AffectedFollower, target, AffectedFollower.GetCurrentAttack());
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(damageAction, true, true);
    }
}

public class ChangeResourceInstance : TriggeredEffectInstance
{
    public OfferingType OfferingType = OfferingType.Gold;
    public int OfferingAmount = 1;
    public ChangeResourceInstance(EffectDef def, Follower affectedFollower, int offset = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offset, effectNum, effectTrigger) {}

    public void Init(OfferingType offeringType = OfferingType.Gold, int offeringAmount = 1)
    {
        OfferingType = offeringType;
        OfferingAmount = offeringAmount;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        ChangeResourceAction action = new ChangeResourceAction(AffectedFollower.Owner, OfferingType.Gold, OfferingAmount);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
    }
}

public class ChangeStatsInstance : TriggeredEffectInstance
{
    public int AttackChange = 0;
    public int HealthChange = 0;
    public ChangeStatsInstance(EffectDef def, Follower affectedFollower, int offset = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offset, effectNum, effectTrigger) { }

    public void Init(int attackChange = 0, int healthChange = 0)
    {
        AttackChange = attackChange;
        HealthChange = healthChange;
    }
    public override void Trigger(ITarget target = null, int amount = 0)
    {
        ChangeStatsAction action = new ChangeStatsAction(AffectedFollower, AttackChange, HealthChange);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
    }
}

public class AddCardCopyToHandInstance : TriggeredEffectInstance
{
    public Card CardToCopy;
    public AddCardCopyToHandInstance(EffectDef def, Follower affectedFollower, int offset = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offset, effectNum, effectTrigger) { }

    // Pass in a card with all the properties you want
    public void Init(Card cardToCopy)
    {
        CardToCopy = cardToCopy;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        Card newCard = (Card)CardToCopy.Clone();
        newCard.Init(AffectedFollower.Owner);
        AddCardCopyToHandAction action = new AddCardCopyToHandAction(newCard);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
    }
}