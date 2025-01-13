using System;
using System.Collections.Generic;
using UnityEngine;

public class FollowerEffectInstance : IComparable<FollowerEffectInstance>
{
    public FollowerEffect EffectDef;
    public Follower AffectedFollower; // The follower this EffectInstance is applied to
    public int OffsetFromOwner = 0; // -1: Affected Follower is one space left of EffectDef Owner. 0: Affected Follower is EffectDef Owner. Etc...
    public int EffectNum = 0; // This is the nth effect added

    public FollowerEffectInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0)
    {
        EffectDef = def;
        AffectedFollower = affectedFollower;
        OffsetFromOwner = offsetFromOwner;
        EffectNum = effectNum;
    }

    // Required So Followers can have SortedSet<EffectInstance>'s
    public int CompareTo(FollowerEffectInstance other)
    {
        if (OffsetFromOwner == other.OffsetFromOwner) return EffectNum - other.EffectNum;

        return OffsetFromOwner - other.OffsetFromOwner;
    }

    // Applies static effects and sets up listeners
    protected virtual void Apply(){}

    // Removes static effects and listeners
    public virtual void Unapply(){}
}

public class StaticFollowerEffectInstance : FollowerEffectInstance
{
    public StaticEffect StaticEffectName = StaticEffect.None;
    public StaticFollowerEffectInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, StaticEffect staticEffectName = StaticEffect.None) : base(def, affectedFollower, offsetFromOwner, effectNum)
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
    public TriggeredFollowerEffectInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum)
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
            case EffectTrigger.OnTargeted:
                AffectedFollower.OnTargetedEffects.Add(this);
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
            case EffectTrigger.OnTargeted:
                AffectedFollower.OnTargetedEffects.Remove(this);
                break;
        }
    }

    public virtual void Trigger(ITarget target = null, int amount = 0){}
}

public class SummonPeltastInstance : TriggeredFollowerEffectInstance
{
    public int SummonPositionOffset = 0;
    public SummonPeltastInstance(FollowerEffect def, Follower owner, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, owner, offsetFromOwner, effectNum, effectTrigger) { }

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
    public SummonFollowerInstance(FollowerEffect def, Follower owner, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, owner, offsetFromOwner, effectNum, effectTrigger) { }

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
    public DamageTargetInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }
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
    public ChangeResourceInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) {}

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

public class ChangeResourceDelayedInstance : TriggeredFollowerEffectInstance
{
    public OfferingType OfferingType = OfferingType.Gold;
    public int OfferingAmount = 1;
    public ChangeResourceDelayedInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(OfferingType offeringType = OfferingType.Gold, int offeringAmount = 1)
    {
        OfferingType = offeringType;
        OfferingAmount = offeringAmount;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        ChangeResourceAction action = new ChangeResourceAction(AffectedFollower.Owner, OfferingType.Gold, OfferingAmount);
        AffectedFollower.Owner.StartOfTurnActions.Add(new DelayedGameAction(action));
        //AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
    }
}

public class ChangeStatsInstance : TriggeredFollowerEffectInstance
{
    public int AttackChange = 0;
    public int HealthChange = 0;
    public EffectTarget TargetType;

    public ChangeStatsInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int attackChange = 0, int healthChange = 0, EffectTarget targetType = EffectTarget.Self)
    {
        AttackChange = attackChange;
        HealthChange = healthChange;
        TargetType = targetType;
    }
    public override void Trigger(ITarget target = null, int amount = 0)
    {
        List<Follower> targetFollowers = FollowerEffect.GetTargets(AffectedFollower, TargetType, EffectDef);

        foreach (Follower targetFollower in targetFollowers)
        {
            ChangeStatsAction action = new ChangeStatsAction(targetFollower, AttackChange, HealthChange);
            AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
        }
    }
}

public class AddCardCopyToHandInstance : TriggeredFollowerEffectInstance
{
    public Card CardToCopy;
    public AddCardCopyToHandInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

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
    public ChangePlayerHealthInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

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

public class HealFollowersInstance : TriggeredFollowerEffectInstance
{
    public int ConstantAmount = 1;
    public EffectTarget TargetType;
    public bool UseTriggerAmount = false;
    public HealFollowersInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int amount, EffectTarget targetType, bool useTriggerAmount = false)
    {
        ConstantAmount = amount;
        TargetType = targetType;
        UseTriggerAmount = useTriggerAmount;
    }

    public override void Trigger(ITarget target = null, int triggerAmount = 0)
    {
        int healAmount = UseTriggerAmount ? triggerAmount : ConstantAmount;

        List<Follower> targetFollowers = FollowerEffect.GetTargets(AffectedFollower, TargetType, EffectDef);
        foreach (Follower targetFollower in targetFollowers)
        {
            HealAction action = new HealAction(targetFollower, AffectedFollower, healAmount);
            AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
        }
    }
}

public class GiveFollowerStaticEffectInstance : TriggeredFollowerEffectInstance
{
    public EffectTarget TargetType;
    public StaticEffect StaticEffectName;
    public GiveFollowerStaticEffectInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(EffectTarget targetType = EffectTarget.Self, StaticEffect staticEffectName = StaticEffect.None)
    {
        TargetType = targetType;
        StaticEffectName = staticEffectName;
    }
    public override void Trigger(ITarget target = null, int amount = 0)
    {
        List<Follower> targetFollowers = FollowerEffect.GetTargets(AffectedFollower, TargetType, EffectDef);

        foreach (Follower targetFollower in targetFollowers)
        {
            targetFollower.StaticEffects.AddToList(StaticEffectName, this);
        }
    }
}

public class RefreshFollowerAttackInstance : TriggeredFollowerEffectInstance
{
    public int ConstantAmount = 1;
    public RefreshFollowerAttackInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int amount)
    {
        ConstantAmount = amount;
    }

    public override void Trigger(ITarget target = null, int triggerAmount = 0)
    {
        RefreshFollowerAttackAction action = new RefreshFollowerAttackAction(AffectedFollower, AffectedFollower, ConstantAmount);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
    }
}

public class SummonRandomMonsterInstance : TriggeredFollowerEffectInstance
{
    public int SummonPositionOffset = 0;
    public SummonRandomMonsterInstance(FollowerEffect def, Follower owner, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, owner, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int summonOffset)
    {
        SummonPositionOffset = summonOffset;
    }
    public override void Trigger(ITarget target = null, int amount = 0)
    {
        if (CardHandler.AllMonsters.Count == 0) return;

        int randomMonsterIndex = UnityEngine.Random.Range(0, CardHandler.AllMonsters.Count);
        Follower monster = (Follower)CardHandler.AllMonsters[randomMonsterIndex].Clone(); //CardHandler.AllMonsters.
        //Peltast peltast = new Peltast();
        monster.Init(AffectedFollower.Owner);

        int index = AffectedFollower.Owner.BattleRow.GetIndexOfFollower(AffectedFollower) + SummonPositionOffset;

        GameAction newAction = new SummonFollowerAction(monster, index);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

public class AddRandomFreeSpellToHandInstance : TriggeredFollowerEffectInstance
{
    Player EffectOwner;
    public AddRandomFreeSpellToHandInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    // The EffectOwner is the player who gets the free spell
    public void Init(Player effectOwner)
    {
        EffectOwner = effectOwner;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        amount = Mathf.Min(CardHandler.HighestSpellCost, amount);
        if (!CardHandler.SpellsByCost.ContainsKey(amount)) return;

        Spell randomSpell = CardHandler.SpellsByCost[amount][UnityEngine.Random.Range(0, CardHandler.SpellsByCost[amount].Count - 1)];
        if (randomSpell == null) return;

        randomSpell.Costs[OfferingType.Gold] = 0;
        randomSpell.Init(AffectedFollower.Owner);
        AddCardCopyToHandAction action = new AddCardCopyToHandAction(randomSpell);
        EffectOwner.GameState.ActionHandler.AddAction(action, true, true);
    }
}

public class AddSpellsCastOnThisToHandInstance : TriggeredFollowerEffectInstance
{
    Player SpellReceivingPlayer;
    public AddSpellsCastOnThisToHandInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(Player spellReceivingPlayer)
    {
        SpellReceivingPlayer = spellReceivingPlayer;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        amount = Mathf.Min(CardHandler.HighestSpellCost, amount);
        if (!CardHandler.SpellsByCost.ContainsKey(amount)) return;

        foreach (Spell spell in AffectedFollower.SpellsCastOnThisByOwner)
        {
            Spell spellCopy = spell.MakeBaseCopy() as Spell;
            spellCopy.Init(SpellReceivingPlayer);
            spellCopy.Costs[OfferingType.Gold] = Mathf.Max(spellCopy.Costs[OfferingType.Gold] - 1, 0);
            AddCardCopyToHandAction action = new AddCardCopyToHandAction(spellCopy);
            SpellReceivingPlayer.GameState.ActionHandler.AddAction(action);
        }
    }
}