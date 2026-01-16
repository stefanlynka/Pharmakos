using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

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
        EffectNum = affectedFollower.GetEffectCounter(); // effectNum + affectedFollower.GetEffectCounter();
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
            case EffectTrigger.OnAnyFollowerDeath:
                AffectedFollower.Owner.GameState.FollowerDies += OnDeath;
                break;
            case EffectTrigger.OnAnySpellCast:
                AffectedFollower.Owner.GameState.SpellPlayed += OnSpellPlayed;
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
            case EffectTrigger.OnAnyFollowerDeath:
                AffectedFollower.Owner.GameState.FollowerDies -= OnDeath;
                break;
            case EffectTrigger.OnAnySpellCast:
                AffectedFollower.Owner.GameState.SpellPlayed -= OnSpellPlayed;
                break;
        }
    }

    public virtual void Trigger(ITarget target = null, int amount = 0){}

    private void OnDeath(Follower follower)
    {
        Trigger(follower);
    }
    private void OnSpellPlayed(Spell spell)
    {
        if (spell.Owner == AffectedFollower.Owner) Trigger(spell);
    }
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
        DealDamageAction damageAction = new DealDamageAction(AffectedFollower, target, damageToDeal, true);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(damageAction);
    }
}

public class DamageAllEnemiesInstance : TriggeredFollowerEffectInstance
{
    private int damage = 0;
    private bool useAttackForDamage = false;
    public DamageAllEnemiesInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int damage, bool useAttackForDamage = false)
    {
        this.damage = damage;
        this.useAttackForDamage = useAttackForDamage;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        int damageToDeal = useAttackForDamage ? AffectedFollower.GetCurrentAttack() : damage;

        Player otherPlayer = AffectedFollower.Owner.GetOtherPlayer();

        List<Follower> enemyFollowers = new List<Follower>(otherPlayer.BattleRow.Followers);
        foreach (Follower follower in enemyFollowers)
        {
            DealDamageAction action = new DealDamageAction(AffectedFollower.Owner, follower, damageToDeal);
            AffectedFollower.Owner.GameState.ActionHandler.AddAction(action);
        }

        DealDamageAction actionOnEnemyPlayer = new DealDamageAction(AffectedFollower.Owner, otherPlayer, damageToDeal);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(actionOnEnemyPlayer);
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

public class DrawCardsDelayedInstance : TriggeredFollowerEffectInstance
{
    public int CardAmount = 1;
    public DrawCardsDelayedInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int cardAmount = 1)
    {
        CardAmount = cardAmount;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        DrawCardAction action = new DrawCardAction(AffectedFollower.Owner, AffectedFollower.Owner, CardAmount);
        AffectedFollower.Owner.StartOfTurnActions.Add(new DelayedGameAction(action));
    }
}

public class ChangeStatsInstance : TriggeredFollowerEffectInstance
{
    public int AttackChange = 0;
    public int HealthChange = 0;
    public EffectTarget TargetType;
    public bool UseTargetInsteadOfType;
    public bool UseAmountForHealth;

    public ChangeStatsInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int attackChange = 0, int healthChange = 0, EffectTarget targetType = EffectTarget.Self, bool useTargetInsteadOfType = false, bool useAmountForHealth = false)
    {
        AttackChange = attackChange;
        HealthChange = healthChange;
        TargetType = targetType;
        UseTargetInsteadOfType = useTargetInsteadOfType;
        UseAmountForHealth = useAmountForHealth;
    }
    public override void Trigger(ITarget target = null, int amount = 0)
    {
        if (UseAmountForHealth) HealthChange = amount;

        List<Follower> targetFollowers;
        if (UseTargetInsteadOfType)
        {
            targetFollowers = new List<Follower> { target as Follower};
        }
        else
        {
            targetFollowers = FollowerEffect.GetTargets(AffectedFollower, TargetType, EffectDef);
        }

        foreach (Follower targetFollower in targetFollowers)
        {
            if (targetFollower == null)
            {
                continue;
            }
            ChangeStatsAction action = new ChangeStatsAction(targetFollower, AttackChange, HealthChange);
            AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
        }
    }
}

public class SetStatsInstance : TriggeredFollowerEffectInstance
{
    public int NewAttack = 0;
    public int NewHealth = 0;
    public EffectTarget TargetType;
    public bool UseTargetInsteadOfType;

    public SetStatsInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(int newAttack = -1, int newHealth = -1, EffectTarget targetType = EffectTarget.Self, bool useTargetInsteadOfType = false)
    {
        NewAttack = newAttack;
        NewHealth = newHealth;
        TargetType = targetType;
        UseTargetInsteadOfType = useTargetInsteadOfType;
    }
    public override void Trigger(ITarget target = null, int amount = 0)
    {
        List<Follower> targetFollowers;
        if (UseTargetInsteadOfType)
        {
            targetFollowers = new List<Follower> { target as Follower };
        }
        else
        {
            targetFollowers = FollowerEffect.GetTargets(AffectedFollower, TargetType, EffectDef);
        }

        foreach (Follower targetFollower in targetFollowers)
        {
            SetStatsAction action = new SetStatsAction(targetFollower, NewAttack, NewHealth);
            AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
        }
    }
}

public class TurnToStoneInstance : TriggeredFollowerEffectInstance
{
    public EffectTarget TargetType;
    public bool UseTargetInsteadOfType;

    public TurnToStoneInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    public void Init(EffectTarget targetType = EffectTarget.Self, bool useTargetInsteadOfType = false)
    {
        TargetType = targetType;
        UseTargetInsteadOfType = useTargetInsteadOfType;
    }
    public override void Trigger(ITarget target = null, int amount = 0)
    {
        List<Follower> targetFollowers;
        if (UseTargetInsteadOfType)
        {
            targetFollowers = new List<Follower> { target as Follower };
        }
        else
        {
            targetFollowers = FollowerEffect.GetTargets(AffectedFollower, TargetType, EffectDef);
        }

        foreach (Follower targetFollower in targetFollowers)
        {
            if (targetFollower.GetCurrentAttack() < AffectedFollower.GetCurrentAttack())
            {
                SetStatsAction action = new SetStatsAction(targetFollower, 0, -1);
                AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
            }
        }
    }
}

public class DrawCardsInstance : TriggeredFollowerEffectInstance
{
    public int CardsToDraw;
    public DrawCardsInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    // Pass in a card with all the properties you want
    public void Init(int cardsToDraw)
    {
        CardsToDraw = cardsToDraw;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        DrawCardAction action = new DrawCardAction(AffectedFollower.Owner, AffectedFollower.Owner, CardsToDraw);
        AffectedFollower.Owner.GameState.ActionHandler.AddAction(action, true, true);
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
        if (CardHandler.SmallMonsters.Count == 0) return;

        int randomMonsterIndex = AffectedFollower.Owner.GameState.RNG.Next(0, CardHandler.SmallMonsters.Count);
        Follower monster = (Follower)CardHandler.SmallMonsters[randomMonsterIndex].Clone(); 
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

        Spell randomSpell = CardHandler.SpellsByCost[amount][EffectOwner.GameState.RNG.Next(0, CardHandler.SpellsByCost[amount].Count - 1)];
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

public class TakeDamageWhenAttackingPlayerInstance : TriggeredFollowerEffectInstance
{
    Player EffectOwner;
    public TakeDamageWhenAttackingPlayerInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0, EffectTrigger effectTrigger = EffectTrigger.None) : base(def, affectedFollower, offsetFromOwner, effectNum, effectTrigger) { }

    // The EffectOwner is the player who gets the free spell
    public void Init(Player effectOwner)
    {
        EffectOwner = effectOwner;
    }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        Player playerTarget = target as Player;
        if (playerTarget == null) return;

        DealDamageAction dealDamageAction = new DealDamageAction(EffectOwner, AffectedFollower, 1);
        EffectOwner.GameState.ActionHandler.AddAction(dealDamageAction, true, true);
    }
}