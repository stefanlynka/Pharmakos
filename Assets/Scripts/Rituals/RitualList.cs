using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

// Target Follower switches sides
public class AphroditeMajor : Ritual
{
    public AphroditeMajor()
    {
        Name = "Aphrodite\nMajor";
        Description = "Target Follower switches sides";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 7 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 10 }, // 10
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllFollowers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        Follower follower = target as Follower;
        if (follower == null) return;
        Player newOwner = follower.Owner.GetOtherPlayer();
        GameAction newAction = new TakeControlOfFollowerAction(newOwner, follower);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// Target Follower runs away with its closest enemy
public class AphroditeMinor : Ritual
{
    public AphroditeMinor()
    {
        Name = "Aphrodite\nMinor";
        Description = "Target Follower runs away with its closest enemy";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 2 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 3 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        var potentialTargets = ITarget.GetAllFollowers(Owner);
        foreach (ITarget target in potentialTargets)
        {
            Follower follower = target as Follower;
            if (follower == null) continue;
            Follower elopeTarget = follower.GetClosestEnemy();
            if (elopeTarget == null) continue;

            targets.Add(follower);
        }

        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        Follower follower = target as Follower;
        if (follower == null) return;
        GameAction newAction = new FollowerElopesAction(follower);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// Next Turn: Gain 2 Gold, Draw 2 Cards
public class ApolloMinor : Ritual
{
    public ApolloMinor()
    {
        Name = "Apollo\nMinor";
        Description = "Next Turn: Gain 2 Gold, Draw 2 Cards";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 2 },
                {OfferingType.Scroll, 2 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 5},
                {OfferingType.Scroll, 3 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllPlayers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;

        ChangeResourceAction goldAction = new ChangeResourceAction(targetPlayer, OfferingType.Gold, 2);
        targetPlayer.StartOfTurnActions.Add(new DelayedGameAction(goldAction));

        DrawCardAction drawAction = new DrawCardAction(Owner, targetPlayer, 2);
        targetPlayer.StartOfTurnActions.Add(new DelayedGameAction(drawAction));
    }
}

// When you play a Spell, your next Follower costs 1 less (stacks)
public class ApolloMajor : Ritual
{
    public ApolloMajor()
    {
        Name = "Apollo\nMajor";
        Description = "When you play a Spell, your next Follower costs 1 less (stacks)";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 3 },
                {OfferingType.Scroll, 3 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 5 },
                {OfferingType.Scroll, 5 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        //targets.Add(Owner.GetOtherPlayer());
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;
        ApolloMajorEffectDef majorEffectDef = new ApolloMajorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(majorEffectDef);
    }
}

public class ApolloMajorEffectDef : StaticPlayerEffect
{
    public ApolloMajorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        TargetPlayer = target;
    }

    public override void Apply()
    {
        Owner.GameState.SpellPlayed += SpellPlayed;
    }

    private void SpellPlayed(Spell spell)
    {
        if (spell.Owner != TargetPlayer) return;
        Owner.FollowerCostReductions[OfferingType.Gold]++;
    }

    public override void Unapply()
    {
        Owner.GameState.SpellPlayed -= SpellPlayed;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        ApolloMajorEffectDef copy = (ApolloMajorEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        string myTarget = TargetPlayer.IsHuman ? "you play" : "your opponent plays";
        string owner = Owner.IsHuman ? "your" : "their";
        return "When " + myTarget + " a Spell, " + owner + " next Follower costs 1 less";
    }
}

// Your Followers have Sprint
public class AresMinor : Ritual
{
    public AresMinor()
    {
        Name = "Ares\nMinor";
        Description = "Your Followers have Sprint";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 2 },
                {OfferingType.Bone, 2 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 6 },
                {OfferingType.Bone, 3 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllPlayers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;
        AresMinorEffectDef aresMinorEffectDef = new AresMinorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(aresMinorEffectDef);
    }
}

public class AresMinorEffectDef : StaticPlayerEffect
{
    public AresMinorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        TargetPlayer = target;
    }
    public AresMinorEffectDef(Player owner)
    {
        Owner = owner;
        TargetPlayer = owner;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += FollowerEnters;

        foreach (Follower follower in Owner.BattleRow.Followers)
        {
            StaticEffectDef sprintEffectDef = new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint);
            follower.InnateEffects.Add(sprintEffectDef);
            sprintEffectDef.Apply(follower);
        }
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= FollowerEnters;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        AresMinorEffectDef copy = (AresMinorEffectDef)MemberwiseClone();
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
        StaticEffectDef sprintEffectDef = new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint);
        follower.InnateEffects.Add(sprintEffectDef);
        sprintEffectDef.Apply(follower);
    }
    protected override string GetDescription()
    {
        string myTarget = TargetPlayer.IsHuman ? "Your" : "Your opponent's";
        return myTarget + " Followers have sprint";
    }
}

// When one of your Followers kills an enemy, it gains +1/+1 and can attack again
public class AresMajor : Ritual
{
    private int attackGain = 1;
    private int healthGain = 1;
    public AresMajor()
    {
        Name = "Ares\nMajor";
        Description = "When one of your Followers kills an enemy, it gains +1/+1 and can attack again";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 3 },
                {OfferingType.Bone, 3 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 6 }, // 6
                {OfferingType.Bone, 6 }, // 6
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;
        AresMajorEffectDef aresMajorEffectDef = new AresMajorEffectDef(Owner, targetPlayer, attackGain, healthGain);
        Owner.AddPlayerEffect(aresMajorEffectDef);
    }
}

public class AresMajorEffectDef : StaticPlayerEffect
{
    private int attackGain = 0;
    private int healthGain = 0;

    public AresMajorEffectDef(Player owner, Player target, int attackGain, int healthGain)
    {
        Owner = owner;
        TargetPlayer = target;
        this.attackGain = attackGain;
        this.healthGain = healthGain;
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
        AresMajorEffectDef copy = (AresMajorEffectDef)MemberwiseClone();
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
        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        follower.InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance gainStatsEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnKill);
        gainStatsEffectInstance.Init(attackGain, healthGain);
        effectDef.EffectInstances.Add(gainStatsEffectInstance);

        RefreshFollowerAttackInstance refreshAttackEffectInstance = new RefreshFollowerAttackInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnKill);
        refreshAttackEffectInstance.Init(1);
        effectDef.EffectInstances.Add(refreshAttackEffectInstance);
    }
    protected override string GetDescription()
    {
        string myTarget = TargetPlayer.IsHuman ? "your" : "your opponent's";
        return "When one of " + myTarget + " Followers kills an enemy, it gains +1/+1 and can attack again";
    }
}

// When one of your Followers draws blood, get a free spell with cost equal to the damage dealt
public class AthenaMajor : Ritual
{
    public AthenaMajor()
    {
        Name = "Athena\nMajor";
        Description = "When one of your Followers draws blood, get a free spell with cost equal to the damage dealt";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 3 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 3 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 6 }, // 6
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 4 }, // 4
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;
        AthenaMajorEffectDef athenaMajorEffectDef = new AthenaMajorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(athenaMajorEffectDef);
    }
}

public class AthenaMajorEffectDef : StaticPlayerEffect
{
    public AthenaMajorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        TargetPlayer = target;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += FollowerEnters;

        foreach (Follower follower in TargetPlayer.BattleRow.Followers)
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
        AthenaMajorEffectDef copy = (AthenaMajorEffectDef)MemberwiseClone();
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
        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        follower.InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        AddRandomFreeSpellToHandInstance gainSpellEffectInstance = new AddRandomFreeSpellToHandInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDrawBlood);
        gainSpellEffectInstance.Init(TargetPlayer);
        effectDef.EffectInstances.Add(gainSpellEffectInstance);
    }
    protected override string GetDescription()
    {
        string myTarget = TargetPlayer.IsHuman ? "your" : "your opponent's";
        string myOwner = Owner.IsHuman ? "you" : "they";
        return "When one of " + myTarget + " Followers draws blood, " + myOwner + " get a free spell with cost equal to the damage dealt";
    }
}

// When a Follower enters Target Player's BattleRow, gain 1 life and draw a card
public class DemeterMajor : Ritual
{
    private int attackGain = 0;
    private int healthGain = 1;
    public DemeterMajor()
    {
        Name = "Demeter\nMajor";
        Description = "When a Follower enters Target Player's BattleRow, gain 1 life and draw a card";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 4 }, // 4
                {OfferingType.Crop, 4 }, // 4
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 6 },
                {OfferingType.Crop, 8 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllPlayers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;
        DemeterMajorEffectDef demeterMajorEffectDef = new DemeterMajorEffectDef(Owner, targetPlayer, attackGain, healthGain);
        Owner.AddPlayerEffect(demeterMajorEffectDef);
    }
}

public class DemeterMajorEffectDef : StaticPlayerEffect
{
    private int attackGain = 0;
    private int healthGain = 0;

    public DemeterMajorEffectDef(Player owner, Player target, int attackGain, int healthGain)
    {
        Owner = owner;
        TargetPlayer = target;
        this.attackGain = attackGain;
        this.healthGain = healthGain;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += FollowerEnters;
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerEnters -= FollowerEnters;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        DemeterMajorEffectDef copy = (DemeterMajorEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected void FollowerEnters(Follower follower)
    {
        if (follower.Owner != TargetPlayer) return;

        DrawCardAction drawCardAction = new DrawCardAction(Owner, Owner, 1);
        Owner.GameState.ActionHandler.AddAction(drawCardAction, true);

        ChangePlayerHealthAction gainHealthAction = new ChangePlayerHealthAction(Owner, Owner, 1);
        Owner.GameState.ActionHandler.AddAction(gainHealthAction, true);

    }
    protected override string GetDescription()
    {
        string myTarget = TargetPlayer.IsHuman ? "your" : "your opponent's";
        string myOwner = Owner.IsHuman ? "you gain 1 life and draw 1 card" : "your opponent gains 1 life and draws 1 card";
        return "When a Follower enters " + myTarget + " BattleRow, " + myOwner;
    }
}

// When one of Target Player's Followers dies, one of your Followers gains its attack
public class DemeterMinor : Ritual
{
    public DemeterMinor()
    {
        Name = "Demeter\nMinor";
        Description = "When one of Target Player's Followers dies, one of your Followers gains its attack";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 }, // 4
                {OfferingType.Crop, 0 }, // 4
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 8 },
                {OfferingType.Crop, 6 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllPlayers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;
        DemeterMinorEffectDef demeterMinorEffectDef = new DemeterMinorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(demeterMinorEffectDef);
    }
}

public class DemeterMinorEffectDef : StaticPlayerEffect
{
    public DemeterMinorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        TargetPlayer = target;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerDies += FollowerDies;
    }

    public override void Unapply()
    {
        Owner.GameState.FollowerDies -= FollowerDies;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        DemeterMinorEffectDef copy = (DemeterMinorEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected void FollowerDies(Follower follower)
    {
        if (follower.Owner != TargetPlayer) return;

        // Get the attack value of the dead follower
        int attackGain = follower.GetCurrentAttack();
        if (attackGain <= 0) return;

        // Get all of owner's followers
        List<Follower> alliedFollowers = new List<Follower>(Owner.BattleRow.Followers);
        if (alliedFollowers.Count == 0) return;

        // Select one random follower
        int idx = Owner.GameState.RNG.Next(0, alliedFollowers.Count);
        Follower targetFollower = alliedFollowers[idx];

        // Give that follower the attack value
        ChangeStatsAction action = new ChangeStatsAction(targetFollower, attackGain, 0);
        Owner.GameState.ActionHandler.AddAction(action, true);
    }
    protected override string GetDescription()
    {
        string myTarget = TargetPlayer.IsHuman ? "your" : "your opponent's";
        string myOwner = Owner.IsHuman ? "one of your" : "one of your opponent's";
        return "When one of " + myTarget + " Followers dies, " + myOwner + " Followers gains its attack";
    }
}
// Sacrifice a Follower: Gain 1 Gold, Draw 2 Cards, Gain 3 Life
public class DionysusMinor : Ritual
{
    public DionysusMinor()
    {
        Name = "Dionysus\nMinor";
        Description = "Sacrifice a Follower: Gain 1 Gold, Draw 2 Cards, Gain 3 Life";
        
        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 4 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 2 },
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 6 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 3 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetOwnFollowers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        KillFollowerAction killAction = new KillFollowerAction(Owner, target, true);
        Owner.GameState.ActionHandler.AddAction(killAction);

        ChangeResourceAction gainGoldAction = new ChangeResourceAction(Owner, OfferingType.Gold, 1);
        Owner.GameState.ActionHandler.AddAction(gainGoldAction, true);

        DrawCardAction drawAction = new DrawCardAction(Owner, Owner, 2);
        Owner.GameState.ActionHandler.AddAction(drawAction, true);

        ChangePlayerHealthAction gainLifeAction = new ChangePlayerHealthAction(Owner, Owner, 3);
        Owner.GameState.ActionHandler.AddAction(gainLifeAction, true);
    }
}

// Summon a copy of the last Follower that died
public class HadesMinor : Ritual
{
    public HadesMinor()
    {
        Name = "Hades\nMinor";
        Description = "Summon a copy of the last Follower that died";
        ReminderText = "Last Follower that died:\n[LastFollowerThatDied]";


        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 2 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 4 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllPlayers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player playerTarget) return;
        SummonLastDeadFollowerAction summonAction = new SummonLastDeadFollowerAction(playerTarget);
        Owner.GameState.ActionHandler.AddAction(summonAction);
    }
}

// At the end of each of target Player's turns, they summon a copy of the last Follower that died
public class HadesMajor : Ritual
{
    public HadesMajor()
    {
        Name = "Hades\nMajor";
        Description = "At the end of each of your turns, summon a copy of the last Follower that died";
        ReminderText = "Last Follower that died:\n[LastFollowerThatDied]";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 5 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 10 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player playerTarget) return;
        HadesMajorEffectDef hadesMajorEffectDef = new HadesMajorEffectDef(Owner, playerTarget);
        Owner.AddPlayerEffect(hadesMajorEffectDef);
    }
}

public class HadesMajorEffectDef : StaticPlayerEffect
{
    SummonLastDeadFollowerAction summonAction;
    DelayedGameAction delayedAction;

    public HadesMajorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        TargetPlayer = target;
    }

    public override void Apply()
    {
        summonAction = new SummonLastDeadFollowerAction(TargetPlayer);
        delayedAction = new DelayedGameAction(summonAction, false);
        TargetPlayer.EndOfTurnActions.Add(delayedAction);
    }

    public override void Unapply()
    {
        TargetPlayer.EndOfTurnActions.Remove(delayedAction);
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        HadesMajorEffectDef copy = (HadesMajorEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    protected override string GetDescription()
    {
        string myTarget = TargetPlayer.IsHuman ? "your" : "your opponent's";
        string myTargetSingular = TargetPlayer.IsHuman ? "you" : "they";
        return "At the end of each of " + myTarget + " turns, " + myTargetSingular + " summon a copy of the last Follower that died";
    }
}

// When one of target Player's Followers dies, get a copy of every spell that Player cast on on it. The copies costs 1 less.
public class HephaestusMajor : Ritual
{
    public HephaestusMajor()
    {
        Name = "Hephaestus\nMajor";
        Description = "When one of your Followers dies, get a copy of every spell you cast on on it. The copies costs 1 less.";
        //Description = "When one of target Player's Followers dies, get a copy of every spell that Player cast on on it. The copies costs 1 less.";
        
        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 3 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 3 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 4 }, // 4
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 4 }, // 4
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;
        HephaestusMajorEffectDef aresMajorEffectDef = new HephaestusMajorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(aresMajorEffectDef);
    }
}

public class HephaestusMajorEffectDef : StaticPlayerEffect
{
    public HephaestusMajorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        TargetPlayer = target;
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
        HephaestusMajorEffectDef copy = (HephaestusMajorEffectDef)MemberwiseClone();
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
        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        follower.InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        AddSpellsCastOnThisToHandInstance gainSpellEffectInstance = new AddSpellsCastOnThisToHandInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        gainSpellEffectInstance.Init(TargetPlayer);
        effectDef.EffectInstances.Add(gainSpellEffectInstance);
    }
    protected override string GetDescription()
    {
        string myTarget = TargetPlayer.IsHuman ? "your" : "your opponent's";
        string myOwner = Owner.IsHuman ? "you get" : "they get";
        string singularTarget = TargetPlayer.IsHuman ? "you" : "they";
        return "When one of " + myTarget + " Followers dies, " + myOwner + " a copy of every spell " + singularTarget + " cast on on it. The copies costs 1 less.";
    }
}

// Steal a card from your opponent's deck. It costs 1 less.
public class HermesMinor : Ritual
{
    public HermesMinor()
    {
        Name = "Hermes\nMinor";
        Description = "Steal a card from your opponent's deck. It costs 1 less.";
        
        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 2 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 3 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner.GetOtherPlayer());
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        Player playerTarget = target as Player;
        if (playerTarget == null) return;
        GameAction newAction = new StealCardAction(Owner, playerTarget);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}
// Draw a card
public class MuseMinor : Ritual
{
    public MuseMinor()
    {
        Name = "Muse\nMinor";
        Description = "Draw a card.";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 2 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 2 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        Player playerTarget = target as Player;
        if (playerTarget == null) return;

        GameAction newAction = new DrawCardAction(Owner, Owner, 1);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}
// Give all Followers you summoned this turn +1/+1
public class HestiaMinor : Ritual
{
    public HestiaMinor()
    {
        Name = "Hestia\nMinor";
        if (Controller.BlitzMode) Description = "Give all Followers you summoned this turn +1/+1";
        else Description = "Give all Followers you summoned this turn +1/+1";
        
        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 5 },
                {OfferingType.Scroll, 0 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 5 },
                {OfferingType.Scroll, 0 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        Player playerTarget = target as Player;
        if (playerTarget == null) return;
        foreach (Follower follower in playerTarget.BattleRow.Followers)
        {
            if (follower.PlayedThisTurn)
            {
                int healthGained = Controller.BlitzMode ? 1 : 1;
                ChangeStatsAction newAction = new ChangeStatsAction(follower, 1, healthGained);
                Owner.GameState.ActionHandler.AddAction(newAction);
            }
        }
    }
}


// When your life total changes, deal 2 damage to a random enemy
public class HeraMajor : Ritual
{
    public HeraMajor()
    {
        Name = "Hera\nMajor";
        Description = "When your life total changes, deal 2 damage to a random enemy";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 4 }, // 4
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 3 }, // 3
                {OfferingType.Scroll, 1 }, // 1
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 3 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 5 },
                {OfferingType.Scroll, 3 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        //targets.Add(Owner.GetOtherPlayer());
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        if (target is not Player targetPlayer) return;
        HeraMajorEffectDef majorEffectDef = new HeraMajorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(majorEffectDef);
    }
}

public class HeraMajorEffectDef : StaticPlayerEffect
{
    public HeraMajorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        TargetPlayer = target;
    }

    public override void Apply()
    {
        TargetPlayer.OnHealthChange -= OnTargetPlayerHealthChanged;
        TargetPlayer.OnHealthChange += OnTargetPlayerHealthChanged;
    }

    public override void Unapply()
    {
        TargetPlayer.OnHealthChange -= OnTargetPlayerHealthChanged;
    }

    public override PlayerEffect DeepCopy(Player newOwner)
    {
        HeraMajorEffectDef copy = (HeraMajorEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.TargetPlayer = newOwner.GameState.GetTargetByID<Player>(TargetPlayer.GetID());
        return copy;
    }

    private void OnTargetPlayerHealthChanged(int healthChange)
    {
        if (healthChange == 0) return;

        Player enemy = TargetPlayer.GetOtherPlayer();

        List<ITarget> possibleTargets = new List<ITarget>();
        possibleTargets.Add(enemy);
        possibleTargets.AddRange(enemy.BattleRow.Followers);

        if (possibleTargets.Count == 0) return;

        int idx = TargetPlayer.GameState.RNG.Next(0, possibleTargets.Count);
        ITarget randomEnemy = possibleTargets[idx];

        DealDamageAction damageAction = new DealDamageAction(Owner, randomEnemy, 2);
        TargetPlayer.GameState.ActionHandler.AddAction(damageAction);
    }
    protected override string GetDescription()
    {
        string owner = Owner.IsHuman ? "your" : "your opponent's";
        return "When " + owner + " life total changes, deal 2 damage to a random enemy";
    }
}

// Summon Typhon and Echidna
public class OldOnesMinor : Ritual
{
    public OldOnesMinor()
    {
        Name = "Old\nOnes";
        Description = "Summon Typhon and Echidna\nGive them Sprint";
        
        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 3 },
                {OfferingType.Bone, 3 },
                {OfferingType.Crop, 3 },
                {OfferingType.Scroll, 3 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 8 },
                {OfferingType.Bone, 8 },
                {OfferingType.Crop, 8 },
                {OfferingType.Scroll, 8 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllPlayers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        Player targetPlayer = target as Player;
        if (targetPlayer == null) return;

        Follower typhon = new Typhon();
        typhon.Init(targetPlayer);
        GameAction summonTyphon = new SummonFollowerAction(typhon);
        Owner.GameState.ActionHandler.AddAction(summonTyphon);

        Follower echidna = new Echidna();
        echidna.Init(targetPlayer);
        GameAction summonEchidna = new SummonFollowerAction(echidna);
        Owner.GameState.ActionHandler.AddAction(summonEchidna);

        GiveFollowerStaticEffectAction newAction = new GiveFollowerStaticEffectAction(typhon, StaticEffect.Sprint);
        Owner.GameState.ActionHandler.AddAction(newAction);

        GiveFollowerStaticEffectAction newAction2 = new GiveFollowerStaticEffectAction(echidna, StaticEffect.Sprint);
        Owner.GameState.ActionHandler.AddAction(newAction2);
    }
}

// Destroy all Followers with the least attack
public class PoseidonMinor : Ritual
{
    public PoseidonMinor()
    {
        Name = "Poseidon\nMinor";
        Description = "Destroy all Followers with the least attack";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 1 },
                {OfferingType.Bone, 1 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 1 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 3 },
                {OfferingType.Bone, 3 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 3 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.Add(Owner);
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        KillLowestAttackFollowersAction killAction = new KillLowestAttackFollowersAction(Owner);
        Owner.GameState.ActionHandler.AddAction(killAction);
    }
}

// Summon a 1/1 copy of a Follower
public class ZeusMinor : Ritual
{
    public ZeusMinor()
    {
        Name = "Zeus\nMinor";

        if (Controller.BlitzMode) Description = "Summon a copy of a Follower";
        else Description = "Summon a 1/1 copy of a Follower";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 1 },
                {OfferingType.Bone, 1 },
                {OfferingType.Crop, 1 },
                {OfferingType.Scroll, 1 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 1 }, // 1
                {OfferingType.Bone, 1 }, // 1
                {OfferingType.Crop, 2 }, // 2
                {OfferingType.Scroll, 1 }, // 1
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllFollowers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        Follower follower = target as Follower;
        if (follower == null) return;

        Follower copy = (Follower)follower.MakeBaseCopy();
        copy.Init(Owner);
        if (!Controller.BlitzMode) copy.SetBaseStats(1, 1);
        int index = Owner.BattleRow.Followers.Count;

        GameAction newAction = new SummonFollowerAction(copy, index);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// Deal 4 damage to a target
public class ZeusMajor : Ritual
{
    private int damage = 4;
    public ZeusMajor()
    {
        Name = "Zeus\nMajor";
        Description = "Deal 4 Damage";

        if (Controller.BlitzMode)
        {
            // BlitzMode costs - reduced non-gold costs since they reset each turn
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 1 },
                {OfferingType.Bone, 1 },
                {OfferingType.Crop, 1 },
                {OfferingType.Scroll, 1 },
            };
        }
        else
        {
            // Normal mode costs
            Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 2 },
                {OfferingType.Bone, 2 },
                {OfferingType.Crop, 2 },
                {OfferingType.Scroll, 2 },
            };
        }
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();
        targets.AddRange(ITarget.GetAllFollowers(Owner));
        targets.AddRange(ITarget.GetAllPlayers(Owner));
        return targets;
    }

    public override void ExecuteEffect(ITarget target)
    {
        DealDamageAction damageAction = new DealDamageAction(Owner, target, damage);
        Owner.GameState.ActionHandler.AddAction(damageAction, true, true);
    }
}