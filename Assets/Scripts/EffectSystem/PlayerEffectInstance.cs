using System;

public class PlayerEffectInstance
{
    public FollowerEffect EffectDef;
    public Follower AffectedFollower; // The follower this EffectInstance is applied to
    public int OffsetFromOwner = 0; // -1: Affected Follower is one space left of EffectDef Owner. 0: Affected Follower is EffectDef Owner. Etc...
    public int EffectNum = 0; // This is the nth effect added

    public PlayerEffectInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0)
    {
        EffectDef = def;
        AffectedFollower = affectedFollower;
        OffsetFromOwner = offsetFromOwner;
        EffectNum = effectNum;
    }


    // Applies static effects and sets up listeners
    protected virtual void Apply(){}

    // Removes static effects and listeners
    public virtual void Unapply(){}
}
/*
public class StaticFollowerEffectInstance : FollowerEffectInstance
{
    public StaticEffect StaticEffectName = StaticEffect.None;
    public StaticFollowerEffectInstance(EffectDef def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, StaticEffect staticEffectName = StaticEffect.None) : base(def, affectedFollower, offsetFromOwner, effectNum)
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

public class TriggeredFollowerEffectInstance: FollowerEffectInstance
{
    public EffectTrigger effectTrigger = EffectTrigger.None;
    public TriggeredFollowerEffectInstance(EffectDef def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum)
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

public class SummonPeltastInstance : TriggeredFollowerEffectInstance
{
    public int SummonPositionOffset = 0;
    public SummonPeltastInstance(EffectDef def, Follower owner, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, owner, offsetFromOwner, effectNum, effectTrigger) { }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        Peltast peltast = new Peltast();
        peltast.Init(AffectedFollower.Owner);

        int index = AffectedFollower.Owner.BattleRow.GetIndexOfFollower(AffectedFollower) + SummonPositionOffset;

        GameAction newAction = new SummonFollowerAction(peltast, index);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

public class SummonFollowerInstance : TriggeredFollowerEffectInstance
{
    private int summonPositionOffset = 0;
    private Type followerType = null;
    public SummonFollowerInstance(EffectDef def, Follower owner, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, owner, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(Type followerType, int summonPositionOffset = 0)
    {
        this.followerType = followerType;
        this.summonPositionOffset = summonPositionOffset;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        if (followerType == null) return;

        Follower follower = (Follower)Activator.CreateInstance(followerType);
        if (follower == null) return;

        follower.Init(AffectedFollower.Owner);

        int index = AffectedFollower.Owner.BattleRow.GetIndexOfFollower(AffectedFollower) + summonPositionOffset;

        GameAction newAction = new SummonFollowerAction(follower, index);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

public class DamageTargetInstance : TriggeredFollowerEffectInstance
{
    private int damage = 0;
    private bool useAttackForDamage = false;
    public DamageTargetInstance(EffectDef def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }
    public void Init(int damage, bool useAttackForDamage = false)
    {
        this.damage = damage;
        this.useAttackForDamage = useAttackForDamage;
    }
    public override void Trigger(ITarget target = null, int amount = 0)
    {
        int damageToDeal = useAttackForDamage ? AffectedFollower.GetCurrentAttack() : damage;
        DealDamageAction damageAction = new DealDamageAction(AffectedFollower, target, damageToDeal);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(damageAction, true, true);
    }
}

public class ChangeResourceInstance : TriggeredFollowerEffectInstance
{
    public OfferingType OfferingType = OfferingType.Gold;
    public int OfferingAmount = 1;
    public ChangeResourceInstance(EffectDef def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) {}

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

public class ChangeStatsInstance : TriggeredFollowerEffectInstance
{
    public int AttackChange = 0;
    public int HealthChange = 0;
    public ChangeStatsInstance(EffectDef def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

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

public class AddCardCopyToHandInstance : TriggeredFollowerEffectInstance
{
    public Card CardToCopy;
    public AddCardCopyToHandInstance(EffectDef def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

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

public class ChangePlayerHealthInstance : TriggeredFollowerEffectInstance
{
    public int ConstantAmount = 1;
    public bool TargetOwner = false;
    public bool UseTriggerAmount = false;
    public bool InvertDamageAmount = false;
    public ChangePlayerHealthInstance(EffectDef def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int amount, bool targetOwner = true, bool useTriggerAmount = false, bool invert = false)
    {
        ConstantAmount = amount;
        TargetOwner = targetOwner;
        UseTriggerAmount = useTriggerAmount;
        InvertDamageAmount = invert;
    }

    public override void Trigger(ITarget target = null, int triggerAmount = 0)
    {
        Player targetPlayer = TargetOwner ? AffectedFollower.Owner : AffectedFollower.Owner.GetOtherPlayer();
        int healthChange = UseTriggerAmount ? triggerAmount : ConstantAmount;
        if (InvertDamageAmount) healthChange *= -1;
        ChangePlayerHealthAction action = new ChangePlayerHealthAction(targetPlayer, AffectedFollower, healthChange);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
    }
}
*/