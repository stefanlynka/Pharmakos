using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TheLostReturnTrinket;
using static Unity.VisualScripting.Member;


// Ares Whetstone: Your Rituals cost 1 less Blood
public class AresWhetstoneTrinket : Trinket<AresWhetstoneTrinketEffectDef>
{
    public AresWhetstoneTrinket()
    {
        Name = "Ares Whetstone";
        Description = "Your Rituals cost 1 less Blood";
        RelevantOffering = OfferingType.Blood;
    }
}
public class AresWhetstoneTrinketEffectDef : TrinketPlayerEffect
{
    public AresWhetstoneTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.RitualCostReductions[OfferingType.Blood]++;
    }

    public override void Unapply()
    {
        Owner.RitualCostReductions[OfferingType.Blood]--;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        AresWhetstoneTrinketEffectDef copy = (AresWhetstoneTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "Your Rituals cost 1 less Blood";
    }
}

// Funeral Amphora: Your Rituals cost 1 less Bone
public class FuneralAmphoraTrinket : Trinket<FuneralAmphoraTrinketEffectDef>
{
    public FuneralAmphoraTrinket()
    {
        Name = "Funeral Amphora";
        Description = "Your Rituals cost 1 less Bone";
        RelevantOffering = OfferingType.Bone;
    }
}
public class FuneralAmphoraTrinketEffectDef : TrinketPlayerEffect
{
    public FuneralAmphoraTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.RitualCostReductions[OfferingType.Bone]++;
    }

    public override void Unapply()
    {
        Owner.RitualCostReductions[OfferingType.Bone]--;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        FuneralAmphoraTrinketEffectDef copy = (FuneralAmphoraTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "Your Rituals cost 1 less Bone";
    }
}

// Demeter's Sickle: Your Rituals cost 1 less Crop
public class DemetersSickleTrinket : Trinket<DemetersSickleTrinketEffectDef>
{
    public DemetersSickleTrinket()
    {
        Name = "Demeter's Sickle";
        Description = "Your Rituals cost 1 less Crop";
        RelevantOffering = OfferingType.Crop;
    }
}
public class DemetersSickleTrinketEffectDef : TrinketPlayerEffect
{
    public DemetersSickleTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.RitualCostReductions[OfferingType.Crop]++;
    }

    public override void Unapply()
    {
        Owner.RitualCostReductions[OfferingType.Crop]--;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        DemetersSickleTrinketEffectDef copy = (DemetersSickleTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "Your Rituals cost 1 less Crop";
    }
}

// Athena's Quill: Your Rituals cost 1 less Scroll
public class AthenasQuillTrinket : Trinket<AthenasQuillTrinketEffectDef>
{
    public AthenasQuillTrinket()
    {
        Name = "Athena's Quill";
        Description = "Your Rituals cost 1 less Scroll";
        RelevantOffering = OfferingType.Scroll;
    }
}
public class AthenasQuillTrinketEffectDef : TrinketPlayerEffect
{
    public AthenasQuillTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.RitualCostReductions[OfferingType.Scroll]++;
    }

    public override void Unapply()
    {
        Owner.RitualCostReductions[OfferingType.Scroll]--;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        AthenasQuillTrinketEffectDef copy = (AthenasQuillTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "Your Rituals cost 1 less Scroll";
    }
}

// Peltast Trumpet: When you use a ritual: summon a 1/1 peltast
public class PeltastTrumpetTrinket : Trinket<PeltastTrumpetTrinketEffectDef>
{
    public PeltastTrumpetTrinket()
    {
        Name = "Peltast Trumpet";
        Description = "When you use a ritual: summon a 1/1 peltast";
    }
}
public class PeltastTrumpetTrinketEffectDef : TrinketPlayerEffect
{
    public PeltastTrumpetTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.RitualUsed += OnRitualUsed;
    }

    public override void Unapply()
    {
        Owner.GameState.RitualUsed -= OnRitualUsed;
    }

    private void OnRitualUsed(Ritual ritual)
    {
        if (ritual.Owner == TargetPlayer)
        {
            Peltast peltast = new Peltast();
            peltast.Init(TargetPlayer);
            SummonFollowerAction summonAction = new SummonFollowerAction(peltast);
            TargetPlayer.GameState.ActionHandler.AddAction(summonAction, true);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        PeltastTrumpetTrinketEffectDef copy = (PeltastTrumpetTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "When you use a ritual: summon a 1/1 peltast";
    }
}

// Golden Fleece Tuft: When you use a ritual: gain 1 gold
public class GoldenFleeceTuftTrinket : Trinket<GoldenFleeceTuftTrinketEffectDef>
{
    public GoldenFleeceTuftTrinket()
    {
        Name = "Golden Fleece Tuft";
        Description = "When you use a ritual: gain 1 gold";
    }
}
public class GoldenFleeceTuftTrinketEffectDef : TrinketPlayerEffect
{
    public GoldenFleeceTuftTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.RitualUsed += OnRitualUsed;
    }

    public override void Unapply()
    {
        Owner.GameState.RitualUsed -= OnRitualUsed;
    }

    private void OnRitualUsed(Ritual ritual)
    {
        if (ritual.Owner == TargetPlayer)
        {
            TargetPlayer.ChangeOffering(OfferingType.Gold, 1);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        GoldenFleeceTuftTrinketEffectDef copy = (GoldenFleeceTuftTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "When you use a ritual: gain 1 gold";
    }
}

// Lyre of Apollo: When you use a ritual: draw 2 cards
public class LyreOfApolloTrinket : Trinket<LyreOfApolloTrinketEffectDef>
{
    public LyreOfApolloTrinket()
    {
        Name = "Lyre of Apollo";
        Description = "When you use a ritual: draw 2 cards";
    }
}
public class LyreOfApolloTrinketEffectDef : TrinketPlayerEffect
{
    public LyreOfApolloTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.RitualUsed += OnRitualUsed;
    }

    public override void Unapply()
    {
        Owner.GameState.RitualUsed -= OnRitualUsed;
    }

    private void OnRitualUsed(Ritual ritual)
    {
        if (ritual.Owner == TargetPlayer)
        {
            DrawCardAction drawAction = new DrawCardAction(TargetPlayer, TargetPlayer, 2);
            TargetPlayer.GameState.ActionHandler.AddAction(drawAction, true);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        LyreOfApolloTrinketEffectDef copy = (LyreOfApolloTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "When you use a ritual: draw 2 cards";
    }
}

// Vial of Ambrosia: When you use a ritual: gain 3 life
public class VialOfAmbrosiaTrinket : Trinket<VialOfAmbrosiaTrinketEffectDef>
{
    public VialOfAmbrosiaTrinket()
    {
        Name = "Vial of Ambrosia";
        Description = "When you use a ritual: gain 3 life";
    }
}
public class VialOfAmbrosiaTrinketEffectDef : TrinketPlayerEffect
{
    public VialOfAmbrosiaTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.RitualUsed += OnRitualUsed;
    }

    public override void Unapply()
    {
        Owner.GameState.RitualUsed -= OnRitualUsed;
    }

    private void OnRitualUsed(Ritual ritual)
    {
        if (ritual.Owner == TargetPlayer)
        {
            ChangePlayerHealthAction healthAction = new ChangePlayerHealthAction(TargetPlayer, null, 3);
            TargetPlayer.GameState.ActionHandler.AddAction(healthAction, true);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        VialOfAmbrosiaTrinketEffectDef copy = (VialOfAmbrosiaTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "When you use a ritual: gain 3 life";
    }
}



// Cyclops Eye: When one of your Followers attacks an enemy directly in front of it, it gains +1/+0
public class CyclopsEyeTrinket : Trinket<CyclopsEyeTrinketEffectDef>
{
    public CyclopsEyeTrinket()
    {
        Name = "Cyclops Eye";
        Description = "When one of your Followers attacks directly in front of them, they gain +1 attack";
    }
}
public class CyclopsEyeTrinketEffectDef : TrinketPlayerEffect
{
    public CyclopsEyeTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        // Hook into follower attacks to check if attacking directly in front
        // We'll need to modify AttackWithFollowerAction or hook into OnAttackEffects
        // For now, hook into FollowerEnters and add an effect to each follower
        Owner.GameState.FollowerEnters += OnFollowerEnters;
        // Also apply to existing followers
        foreach (Follower follower in Owner.BattleRow.Followers)
        {
            CustomEffectDef customEffectDef = ApplyEffectToFollower(follower);
            customEffectDef.Apply(follower);
        }
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= OnFollowerEnters;
    }

    private void OnFollowerEnters(Follower follower)
    {
        if (follower.Owner == TargetPlayer)
        {
            ApplyEffectToFollower(follower);
        }
    }

    private CustomEffectDef ApplyEffectToFollower(Follower follower)
    {
        // Add an OnAttack effect that checks if attacking directly in front
        CustomEffectDef customEffect = new CustomEffectDef(EffectTarget.Self);
        customEffect.ApplyInstanceAction = CustomEffectAction;
        follower.InnateEffects.Add(customEffect);
        customEffect.Apply(follower);

        return customEffect;
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        CyclopsEyeAttackInstance attackInstance = new CyclopsEyeAttackInstance(effectDef, instanceTarget, offset, 0);
        instanceTarget.OnAttackEffects.Add(attackInstance);
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        CyclopsEyeTrinketEffectDef copy = (CyclopsEyeTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "When one of your Followers attacks directly in front of them, they gain +1 attack";
    }
}
// Custom effect instance for CyclopsEye trinket
public class CyclopsEyeAttackInstance : TriggeredFollowerEffectInstance
{
    public CyclopsEyeAttackInstance(FollowerEffect def, Follower affectedFollower, int offsetFromOwner = 0, int effectNum = 0)
        : base(def, affectedFollower, offsetFromOwner, effectNum, EffectTrigger.OnAttack) { }

    public override void Trigger(ITarget target = null, int amount = 0)
    {
        if (target is Follower targetFollower)
        {
            // Check if target is directly in front (using LowVision range)
            float attackerPos = AffectedFollower.GetPositionInBattleRow();
            List<ITarget> targetsInFront = targetFollower.Owner.BattleRow.GetTargetsInLowVisionRange(attackerPos);
            if (targetsInFront.Contains(targetFollower))
            {
                ChangeStatsAction statsAction = new ChangeStatsAction(AffectedFollower, 1, 0);
                AffectedFollower.Owner.GameState.ActionHandler.AddAction(statsAction, true, true);
            }
        }
    }
}


// Pan's Flute: Followers have frenzy and +1/+0 the turn they're summoned
public class PansFluteTrinket : Trinket<PansFluteTrinketEffectDef>
{
    public PansFluteTrinket()
    {
        Name = "Pan's Flute";
        Description = "Followers have frenzy and +1/+0 the turn they're summoned";
        RepeatTrinket = false;
    }
}
public class PansFluteTrinketEffectDef : TrinketPlayerEffect
{
    public PansFluteTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += OnFollowerEnters;
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= OnFollowerEnters;
    }

    private void OnFollowerEnters(Follower follower)
    {
        if (follower.Owner == TargetPlayer)
        {
            // Give frenzy and +1/+0
            StaticEffectDef frenzyEffect = new StaticEffectDef(EffectTarget.Self, StaticEffect.Frenzy);
            follower.InnateEffects.Add(frenzyEffect);
            frenzyEffect.Apply(follower);
            
            ChangeStatsAction statsAction = new ChangeStatsAction(follower, 1, 0);
            TargetPlayer.GameState.ActionHandler.AddAction(statsAction, true, true);

            ChangeStatsAction reduceStatsAction = new ChangeStatsAction(follower, -1, 0);
            follower.Owner.EndOfTurnActions.Add(new DelayedGameAction(reduceStatsAction));

            RemoveFollowerStaticEffectAction removeFrenzyAction = new RemoveFollowerStaticEffectAction(follower, StaticEffect.Frenzy);
            follower.Owner.EndOfTurnActions.Add(new DelayedGameAction(removeFrenzyAction));
            
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        PansFluteTrinketEffectDef copy = (PansFluteTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "Followers have frenzy and +1/+0 the turn they're summoned";
    }
}

// Hermes Sandals: The first follower you play each turn has sprint
public class HermesSandalsTrinket : Trinket<HermesSandalsTrinketEffectDef>
{
    public HermesSandalsTrinket()
    {
        Name = "Hermes Sandals";
        Description = "The first follower you play each turn has sprint";
        RepeatTrinket = false;
    }
}
public class HermesSandalsTrinketEffectDef : TrinketPlayerEffect
{
    private bool playedFollowerThisTurn = false;

    public HermesSandalsTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += OnFollowerEnters;
        // Reset flag at start of turn using StartOfTurnActions
        // Reset flag at start of each turn
        // We'll create a simple action to reset the flag
        TargetPlayer.StartOfTurnActions.Add(new DelayedGameAction(new ResetHermesSandalsFlagAction(this), false));
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= OnFollowerEnters;
    }

    public void ResetFlag()
    {
        playedFollowerThisTurn = false;
    }

    private void OnFollowerEnters(Follower follower)
    {
        if (follower.Owner == TargetPlayer && !playedFollowerThisTurn)
        {
            playedFollowerThisTurn = true;
            StaticEffectDef sprintEffect = new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint);
            follower.InnateEffects.Add(sprintEffect);
            sprintEffect.Apply(follower);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        HermesSandalsTrinketEffectDef copy = (HermesSandalsTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "The first follower you play each turn has sprint";
    }
}
// Helper action to reset HermesSandals flag
public class ResetHermesSandalsFlagAction : GameAction
{
    private HermesSandalsTrinketEffectDef effectDef;

    public ResetHermesSandalsFlagAction(HermesSandalsTrinketEffectDef effectDef)
    {
        this.effectDef = effectDef;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        ResetHermesSandalsFlagAction copy = (ResetHermesSandalsFlagAction)MemberwiseClone();
        // Find the corresponding effect def in the new owner
        foreach (PlayerEffect effect in newOwner.PlayerEffects)
        {
            if (effect is HermesSandalsTrinketEffectDef hermesEffect)
            {
                copy.effectDef = hermesEffect;
                break;
            }
        }
        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        if (effectDef != null)
        {
            effectDef.ResetFlag();
        }
        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        return new List<AnimationAction>();
    }
}

// Aegis: Your followers with taunt have shield 2 (reduce incoming damage by 2)
public class TheAegisTrinket : Trinket<TheAegisTrinketEffectDef>
{
    public TheAegisTrinket()
    {
        Name = "Aegis";
        Description = "Your followers with taunt have shield 2 (reduce incoming damage by 2)";
    }
}
public class TheAegisTrinketEffectDef : TrinketPlayerEffect
{
    public TheAegisTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += OnFollowerEnters;
        // Also apply to existing followers
        foreach (Follower follower in Owner.BattleRow.Followers)
        {
            if (follower.HasStaticEffect(StaticEffect.Taunt))
            {
                ApplyShield(follower);
            }
        }
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= OnFollowerEnters;
    }

    private void OnFollowerEnters(Follower follower)
    {
        if (follower.Owner == TargetPlayer && follower.HasStaticEffect(StaticEffect.Taunt))
        {
            ApplyShield(follower);
        }
    }

    private void ApplyShield(Follower follower)
    {
        if (!follower.HasStaticEffect(StaticEffect.Shield))
        {
            StaticEffectDef shieldEffect = new StaticEffectDef(EffectTarget.Self, StaticEffect.Shield);
            follower.InnateEffects.Add(shieldEffect);
            shieldEffect.Apply(follower);
            StaticEffectDef shieldEffect2 = new StaticEffectDef(EffectTarget.Self, StaticEffect.Shield);
            follower.InnateEffects.Add(shieldEffect2);
            shieldEffect2.Apply(follower);

        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        TheAegisTrinketEffectDef copy = (TheAegisTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "Your followers with taunt have shield 2 (reduce incoming damage by 2)";
    }
}

// Rod of Asclepius: The first time one of your followers dies on your turn, revive it with 1 health
public class RodOfAsclepiusTrinket : Trinket<RodOfAsclepiusTrinketEffectDef>
{
    public RodOfAsclepiusTrinket()
    {
        Name = "Rod of Asclepius";
        Description = "The first time one of your followers dies on your turn, revive it with 1 health";
    }
}
public class RodOfAsclepiusTrinketEffectDef : TrinketPlayerEffect
{
    private bool usedThisTurn = false;
    private Follower lastFollowerThatDied = null;

    public RodOfAsclepiusTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerDies += OnFollowerDies;
        // Reset flag at start of each turn
        TargetPlayer.StartOfTurnActions.Add(new DelayedGameAction(new ResetRodOfAsclepiusFlagAction(this), false));
    }

    public void ResetFlag()
    {
        usedThisTurn = false;
        lastFollowerThatDied = null;
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerDies -= OnFollowerDies;
    }

    private void OnFollowerDies(Follower follower)
    {
        if (follower.Owner == TargetPlayer && !usedThisTurn && TargetPlayer.IsMyTurn)
        {
            usedThisTurn = true;
            lastFollowerThatDied = follower;
            // Revive with 1 health
            Follower revived = (Follower)follower.MakeBaseCopy();
            revived.Init(TargetPlayer);
            revived.SetBaseStats(revived.BaseAttack, 1);
            revived.CurrentHealth = 1;
            SummonFollowerAction summonAction = new SummonFollowerAction(revived);
            TargetPlayer.GameState.ActionHandler.AddAction(summonAction);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        RodOfAsclepiusTrinketEffectDef copy = (RodOfAsclepiusTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "The first time one of your followers dies on your turn, revive it with 1 health";
    }
}
// Helper action to reset RodOfAsclepius flag
public class ResetRodOfAsclepiusFlagAction : GameAction
{
    private RodOfAsclepiusTrinketEffectDef effectDef;

    public ResetRodOfAsclepiusFlagAction(RodOfAsclepiusTrinketEffectDef effectDef)
    {
        this.effectDef = effectDef;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        ResetRodOfAsclepiusFlagAction copy = (ResetRodOfAsclepiusFlagAction)MemberwiseClone();
        foreach (PlayerEffect effect in newOwner.PlayerEffects)
        {
            if (effect is RodOfAsclepiusTrinketEffectDef rodEffect)
            {
                copy.effectDef = rodEffect;
                break;
            }
        }
        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        if (effectDef != null)
        {
            effectDef.ResetFlag();
        }
        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        return new List<AnimationAction>();
    }
}


// Hydra's Scale: When you summon a follower that costs 5 or more, summon a copy of it.
public class HydrasScaleTrinket : Trinket<HydrasScaleTrinketEffectDef>
{
    public HydrasScaleTrinket()
    {
        Name = "Hydra's Scale";
        Description = "When you summon a follower that costs 5 or more, summon a copy of it.";
    }
}
public class HydrasScaleTrinketEffectDef : TrinketPlayerEffect
{
    public HydrasScaleTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += OnFollowerEnters;
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= OnFollowerEnters;
    }

    private void OnFollowerEnters(Follower follower)
    {
        if (follower.Owner == TargetPlayer)
        {
            // Calculate total cost
            int totalCost = 0;
            foreach (var cost in follower.Costs)
            {
                totalCost += cost.Value;
            }
            
            if (totalCost >= 5 && !follower.IsClone)
            {
                Follower copy = (Follower)follower.MakeBaseCopy();
                copy.Init(TargetPlayer);
                copy.IsClone = true;
                int index = TargetPlayer.BattleRow.GetIndexOfFollower(follower) + 1;
                SummonFollowerAction summonAction = new SummonFollowerAction(copy, index);
                TargetPlayer.GameState.ActionHandler.AddAction(summonAction, true, true);
            }
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        HydrasScaleTrinketEffectDef copy = (HydrasScaleTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "When you summon a follower that costs 5 or more, summon a copy of it.";
    }
}

// Tunic of Nessus: When you sacrifice a follower, deal 2 damage to a random enemy
public class TunicOfNessusTrinket : Trinket<TunicOfNessusTrinketEffectDef>
{
    public TunicOfNessusTrinket()
    {
        Name = "Tunic of Nessus";
        Description = "When you sacrifice a follower, deal 2 damage to a random enemy";
    }
}
public class TunicOfNessusTrinketEffectDef : TrinketPlayerEffect
{
    public TunicOfNessusTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerSacrificed += OnFollowerSacrificed;
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerSacrificed -= OnFollowerSacrificed;
    }

    private void OnFollowerSacrificed(Follower follower)
    {
        // Check if it was sacrificed
        if (follower.Owner == TargetPlayer && TargetPlayer.IsMyTurn)
        {
            Player enemy = TargetPlayer.GetOtherPlayer();

            List<ITarget> possibleTargets = new List<ITarget>();
            possibleTargets.Add(enemy);
            possibleTargets.AddRange(enemy.BattleRow.Followers);

            ITarget randomEnemy = possibleTargets[TargetPlayer.GameState.RNG.Next(0, possibleTargets.Count)];
            DealDamageAction damageAction = new DealDamageAction(Owner, randomEnemy, 2);
            TargetPlayer.GameState.ActionHandler.AddAction(damageAction);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        TunicOfNessusTrinketEffectDef copy = (TunicOfNessusTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "When you sacrifice a follower, deal 2 damage to a random enemy";
    }
}

// Medea's Potion: When you sacrifice a follower, add a copy of it to your hand
public class MedeasPotionTrinket : Trinket<MedeasPotionTrinketEffectDef>
{
    public MedeasPotionTrinket()
    {
        Name = "Medea's Potion";
        Description = "When you sacrifice a follower, add a copy of it to your hand";
    }
}
public class MedeasPotionTrinketEffectDef : TrinketPlayerEffect
{
    public MedeasPotionTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerSacrificed += OnFollowerSacrificed;
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerSacrificed -= OnFollowerSacrificed;
    }

    private void OnFollowerSacrificed(Follower follower)
    {
        // Check if it was sacrificed (died on owner's turn)
        if (follower.Owner == TargetPlayer && TargetPlayer.IsMyTurn)
        {
            Card copy = follower.MakeBaseCopy();
            copy.Init(TargetPlayer);
            AddCardCopyToHandAction addAction = new AddCardCopyToHandAction(copy);
            TargetPlayer.GameState.ActionHandler.AddAction(addAction);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        MedeasPotionTrinketEffectDef copy = (MedeasPotionTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "When you sacrifice a follower, add a copy of it to your hand";
    }
}

// Wings of Icarus: Your damaged followers have +1/+0
public class WingsOfIcarusTrinket : Trinket<WingsOfIcarusTrinketEffectDef>
{
    public WingsOfIcarusTrinket()
    {
        Name = "Wings of Icarus";
        Description = "Your damaged followers have +1/+0";
    }
}
public class WingsOfIcarusTrinketEffectDef : TrinketPlayerEffect
{
    private Dictionary<Follower, int> appliedFollowers = new Dictionary<Follower, int>();

    public WingsOfIcarusTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += CheckStats;
        Owner.GameState.FollowerHealthChanges += CheckStats;

        // Apply to existing followers
        foreach (Follower follower in Owner.BattleRow.Followers)
        {
            CheckAndApply(follower);
        }
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= CheckStats;
        Owner.GameState.FollowerHealthChanges -= CheckStats;

        // Remove from all followers
        foreach (var kvp in appliedFollowers)
        {
            ChangeStatsAction statsAction = new ChangeStatsAction(kvp.Key, -1, 0);
            TargetPlayer.GameState.ActionHandler.AddAction(statsAction, true, true);
        }
        appliedFollowers.Clear();
    }

    private void CheckStats(Follower follower)
    {
        if (follower.Owner == TargetPlayer)
        {
            CheckAndApply(follower);
        }
    }

    private void CheckAndApply(Follower follower)
    {
        bool isDamaged = follower.CurrentHealth < follower.MaxHealth;
        bool hasBonus = appliedFollowers.ContainsKey(follower);
        
        if (isDamaged && !hasBonus)
        {
            appliedFollowers[follower] = 1;
            ChangeStatsAction statsAction = new ChangeStatsAction(follower, 1, 0);
            TargetPlayer.GameState.ActionHandler.AddAction(statsAction, true, true);
        }
        else if (!isDamaged && hasBonus)
        {
            appliedFollowers.Remove(follower);
            ChangeStatsAction statsAction = new ChangeStatsAction(follower, -1, 0);
            TargetPlayer.GameState.ActionHandler.AddAction(statsAction, true, true);
        }
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        WingsOfIcarusTrinketEffectDef copy = (WingsOfIcarusTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        copy.appliedFollowers = new Dictionary<Follower, int>();
        return copy;
    }

    protected override string GetDescription()
    {
        return "Your damaged followers have +1/+0";
    }
}

// Pandora's Box: At the start of your turn, play a random spell with a random target
public class PandorasBoxTrinket : Trinket<PandorasBoxTrinketEffectDef>
{
    public PandorasBoxTrinket()
    {
        Name = "Pandora's Box";
        Description = "At the start of your turn, play a random spell with a random target";
    }
}
public class PandorasBoxTrinketEffectDef : TrinketPlayerEffect
{
    public PandorasBoxTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        TargetPlayer.StartOfTurnActions.Add(new DelayedGameAction(new PlayRandomSpellAction(Owner), false));
    }

    public override void Unapply()
    {
        
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        PandorasBoxTrinketEffectDef copy = (PandorasBoxTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "At the start of your turn, play a random spell with a random target";
    }
}


// A trinket that grants the effect of the AresMinor Ritual
public class AresMinorTrinket : Trinket<AresMinorEffectDef>
{
    public AresMinorTrinket()
    {
        Name = "Ares Minor";
        Description = "Your Followers have Sprint";
    }
}


// A trinket that grants the effect of the AresMinor Ritual
public class AresMajorTrinket : Trinket<AresMajorEffectDef>
{
    public AresMajorTrinket()
    {
        Name = "Ares Major";
        Description = "When one of your Followers kills an enemy, it gains +1/+1 and can attack again";
    }
}

// Twisting Corridors: At the end of your turn, summon a Corridor
public class TwistingCorridorsTrinket : Trinket<TwistingCorridorsTrinketEffectDef>
{
    public TwistingCorridorsTrinket()
    {
        Name = "Twisting Corridors";
        Description = "At the end of your turn, summon a Corridor";
    }
}
public class TwistingCorridorsTrinketEffectDef : TrinketPlayerEffect
{
    SummonFollowerCopyAction summonAction;
    DelayedGameAction delayedAction;

    public TwistingCorridorsTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        summonAction = new SummonFollowerCopyAction(TargetPlayer, new Corridor());
        delayedAction = new DelayedGameAction(summonAction, false);
        TargetPlayer.EndOfTurnActions.Add(delayedAction);
    }

    public override void Unapply()
    {
        TargetPlayer.EndOfTurnActions.Remove(delayedAction);
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        TwistingCorridorsTrinketEffectDef copy = (TwistingCorridorsTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "At the end of your turn, summon a Corridor";
    }
}

// Twisting Corridors: Your Corridors can summon larger monsters
public class EverDeeperTrinket : Trinket<EverDeeperTrinketEffectDef>
{
    public EverDeeperTrinket()
    {
        Name = "Twisting Corridors Buff";
        Description = "Your Corridors can summon larger monsters";
    }
}
public class EverDeeperTrinketEffectDef : TrinketPlayerEffect
{

    public EverDeeperTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        //summonAction = new SummonFollowerCopyAction(TargetPlayer, new Corridor());
        //delayedAction = new DelayedGameAction(summonAction, false);
        //TargetPlayer.EndOfTurnActions.Add(delayedAction);
    }

    public override void Unapply()
    {
        //TargetPlayer.EndOfTurnActions.Remove(delayedAction);
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        TwistingCorridorsTrinketEffectDef copy = (TwistingCorridorsTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "Your Corridors can summon larger monsters";
    }
}




// TheGreatHunt: At the start of your turn, summon a Prey for both players
public class TheGreatHuntTrinket : Trinket<TheGreatHuntTrinketEffectDef>
{
    public TheGreatHuntTrinket()
    {
        Name = "The Great Hunt";
        Description = "At the start of your turn, summon a Prey for both players";
    }
}
public class TheGreatHuntTrinketEffectDef : TrinketPlayerEffect
{
    SummonFollowerCopyAction summonAction;
    SummonFollowerCopyAction summonAction2;
    DelayedGameAction delayedAction;
    DelayedGameAction delayedAction2;

    public TheGreatHuntTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Player otherPlayer = TargetPlayer.GetOtherPlayer();
        summonAction = new SummonFollowerCopyAction(TargetPlayer, new Prey2());
        delayedAction = new DelayedGameAction(summonAction, false);
        TargetPlayer.StartOfTurnActions.Add(delayedAction);

        summonAction2 = new SummonFollowerCopyAction(otherPlayer, new Prey2());
        delayedAction2 = new DelayedGameAction(summonAction2, false);
        TargetPlayer.StartOfTurnActions.Add(delayedAction2);
    }

    public override void Unapply()
    {
        Player otherPlayer = TargetPlayer.GetOtherPlayer();

        TargetPlayer.StartOfTurnActions.Remove(delayedAction);
        otherPlayer.StartOfTurnActions.Remove(delayedAction2);
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        TheGreatHuntTrinketEffectDef copy = (TheGreatHuntTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "At the start of your turn, summon a Prey for both players";
    }
}

// Growing Bounties: Your opponent's Prey deal them more damage
public class GrowingBountiesTrinket : Trinket<GrowingBountiesTrinketEffectDef>
{
    public GrowingBountiesTrinket()
    {
        Name = "Growing Bounties";
        Description = "Your opponent's Prey deal them more damage";
    }
}
public class GrowingBountiesTrinketEffectDef : TrinketPlayerEffect
{
    public GrowingBountiesTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        
    }

    public override void Unapply()
    {
        
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        GrowingBountiesTrinketEffectDef copy = (GrowingBountiesTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "Your opponent's Prey deal them more damage";
    }
}

// TheEndlessWall: At the end of your turn, summon a Wall of Troy
public class TheEndlessWallTrinket : Trinket<TheEndlessWallTrinketEffectDef>
{
    public TheEndlessWallTrinket()
    {
        Name = "Twisting Corridors";
        Description = "At the end of your turn, summon a Corridor";
    }
}
public class TheEndlessWallTrinketEffectDef : TrinketPlayerEffect
{
    SummonFollowerCopyAction summonAction;
    DelayedGameAction delayedAction;

    public TheEndlessWallTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        summonAction = new SummonFollowerCopyAction(TargetPlayer, new WallOfTroy());
        delayedAction = new DelayedGameAction(summonAction, false);
        TargetPlayer.EndOfTurnActions.Add(delayedAction);
    }

    public override void Unapply()
    {
        TargetPlayer.EndOfTurnActions.Remove(delayedAction);
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        TheEndlessWallTrinketEffectDef copy = (TheEndlessWallTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "At the end of your turn, summon a Wall of Troy";
    }
}

// Call of the Sea: When a Siren kills a follower, add a free Drown to hand
public class CallOfTheSeaTrinket : Trinket<CallOfTheSeaTrinketEffectDef>
{
    public CallOfTheSeaTrinket()
    {
        Name = "Call of the Sea";
        Description = "When a Siren kills a follower, add a free Drown to hand";
    }
}
public class CallOfTheSeaTrinketEffectDef : TrinketPlayerEffect
{
    public CallOfTheSeaTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += FollowerEnters;

        foreach (Follower follower in Owner.BattleRow.Followers)
        {
            CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
            customEffectDef.ApplyInstanceAction = CustomEffectAction;
            follower.InnateEffects.Add(customEffectDef);
            customEffectDef.Apply(follower);
        }
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= FollowerEnters;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        CallOfTheSeaTrinketEffectDef copy = (CallOfTheSeaTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected void FollowerEnters(Follower follower)
    {
        if (follower.Owner != TargetPlayer) return;

        ApplyEffectToFollower(follower);
    }

    private void ApplyEffectToFollower(Follower follower)
    {
        if (follower is not Siren) return;

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        follower.InnateEffects.Add(customEffectDef);
        customEffectDef.Apply(follower);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnKill);
        Drown newCard = new Drown();
        newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);
        effectDef.EffectInstances.Add(newEffectInstance);
    }

    protected override string GetDescription()
    {
        return "When a Siren kills a follower, add a free Drown to hand";
    }
}

// ReturnOfTheFallen: At the start of your turn, summon a Follower that's been Offered to the Gods
public class TheLostReturnTrinket : Trinket<TheLostReturnTrinketEffectDef>
{
    public TheLostReturnTrinket()
    {
        Name = "The Lost Return";
        Description = "At the start of your turn, summon a Follower that's been Offered to the Gods";
    }
}
public class TheLostReturnTrinketEffectDef : TrinketPlayerEffect
{
    SummonSacrificedFollowerAction summonAction;
    DelayedGameAction delayedAction;

    public TheLostReturnTrinketEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        summonAction = new SummonSacrificedFollowerAction(TargetPlayer);
        delayedAction = new DelayedGameAction(summonAction, false);
        TargetPlayer.StartOfTurnActions.Add(delayedAction);
    }

    public override void Unapply()
    {
        TargetPlayer.EndOfTurnActions.Remove(delayedAction);
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        TheLostReturnTrinketEffectDef copy = (TheLostReturnTrinketEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        return "At the start of your turn, summon a Follower that's been Offered to the Gods";
    }
}