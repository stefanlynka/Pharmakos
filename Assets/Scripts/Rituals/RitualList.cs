using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

// Summon a 1/1 copy of a Follower
public class ZeusMinor : Ritual
{
    public ZeusMinor()
    {
        Name = "Zeus\nMinor";
        Description = "Summon a 1/1 copy of a Follower";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 1 },
                {OfferingType.Bone, 1 },
                {OfferingType.Crop, 2 },
                {OfferingType.Scroll, 1 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
        Follower follower = target as Follower;
        if (follower == null) return;

        //Type parentType = Follower;
        //Type childType = Assembly.GetAssembly(parentType)
        //                 .GetType(parentType.FullName.Replace("ParentClass", "Child"));

        //// Create an instance of the child type
        //ParentClass child = (ParentClass)Activator.CreateInstance(childType);

        Follower copy = (Follower)follower.MakeBaseCopy();
        copy.Init(Owner);
        copy.CurrentAttack = 1;
        copy.CurrentHealth = 1;
        int index = Owner.BattleRow.Followers.Count;

        GameAction newAction = new SummonFollowerAction(copy, index);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// Deal 5 damage to a target
public class ZeusMajor : Ritual
{
    private int damage = 5;
    public ZeusMajor()
    {
        Name = "Zeus\nMajor";
        Description = "Deal 5 Damage";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 2 },
                {OfferingType.Bone, 2 },
                {OfferingType.Crop, 2 },
                {OfferingType.Scroll, 2 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));
        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        DealDamageAction damageAction = new DealDamageAction(Owner, target, damage);
        Owner.GameState.ActionHandler.AddAction(damageAction, true, true);

        //Follower follower = target as Follower;
        //Player player = target as Player;
        //if (follower != null)
        //{
        //    follower.ChangeHealth(-damage);
        //}
        //else if (player != null)
        //{
        //    player.ChangeHealth(-damage);
        //}
    }
}

// Destroy all Followers with the least attack
public class PoseidonMinor : Ritual
{
    public PoseidonMinor()
    {
        Name = "Poseidon\nMinor";
        Description = "Destroy all Followers with the least attack";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 3 },
                {OfferingType.Bone, 3 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 3 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.Add(Owner);

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        KillLowestAttackFollowersAction killAction = new KillLowestAttackFollowersAction(Owner);
        Owner.GameState.ActionHandler.AddAction(killAction);
    }
}

// Sacrifice a Follower: Gain 1 Gold, Draw 2 Cards, Gain 3 Life
public class DionysusMinor : Ritual
{
    public DionysusMinor()
    {
        Name = "Dionysus\nMinor";
        Description = "Sacrifice a Follower: Gain 1 Gold, Draw 2 Cards, Gain 3 Life";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 6 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 3 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        KillFollowerAction killAction = new KillFollowerAction(Owner, target);
        Owner.GameState.ActionHandler.AddAction(killAction);

        ChangeResourceAction gainGoldAction = new ChangeResourceAction(Owner, OfferingType.Gold, 1);
        Owner.GameState.ActionHandler.AddAction(gainGoldAction, true);

        DrawCardAction drawAction = new DrawCardAction(Owner, Owner, 2);
        Owner.GameState.ActionHandler.AddAction(drawAction, true);

        ChangePlayerHealthAction gainLifeAction = new ChangePlayerHealthAction(Owner, Owner, 3);
        Owner.GameState.ActionHandler.AddAction(gainLifeAction, true);
    }
}

// Next Turn: Gain 2 Gold, Draw 2 Cards
public class ApolloMinor : Ritual
{
    public ApolloMinor()
    {
        Name = "Apollo\nMinor";
        Description = "Next Turn: Gain 2 Gold, Draw 2 Cards";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 3 },
                {OfferingType.Scroll, 3 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Player targetPlayer) return;

        ChangeResourceAction goldAction = new ChangeResourceAction(targetPlayer, OfferingType.Gold, 2);
        targetPlayer.StartOfTurnActions.Add(new DelayedGameAction(goldAction));

        DrawCardAction drawAction = new DrawCardAction(Owner, targetPlayer, 2);
        targetPlayer.StartOfTurnActions.Add(new DelayedGameAction(drawAction));
    }
}

// At the end of each of target Player's turns, they summon a copy of the last Follower that died
public class HadesMajor : Ritual
{
    public HadesMajor()
    {
        Name = "Hades\nMajor";
        Description = "At the end of each of target Player's turns, they summon a copy of the last Follower that died";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 12 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
        if (target is not Player playerTarget) return;

        SummonLastDeadFollowerAction summonAction = new SummonLastDeadFollowerAction(playerTarget);
        playerTarget.EndOfTurnActions.Add(new DelayedGameAction(summonAction, false));
    }
}
// Summon a copy of the last Follower that died
public class HadesMinor : Ritual
{
    public HadesMinor()
    {
        Name = "Hades\nMinor";
        Description = "Summon a copy of the last Follower that died";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 4 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
        if (target is not Player playerTarget) return;

        SummonLastDeadFollowerAction summonAction = new SummonLastDeadFollowerAction(playerTarget);
        Owner.GameState.ActionHandler.AddAction(summonAction);
    }
}


// When a Follower enters Target Player's BattleRow, your Followers gain +0/+1
public class DemeterMajor : Ritual
{
    private int attackGain = 0;
    private int healthGain = 1;
    public DemeterMajor()
    {
        Name = "Demeter\nMajor";
        Description = "When a Follower enters Target Player's BattleRow, your Followers gain +0/+1";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 4 },
                {OfferingType.Crop, 8 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Player targetPlayer) return;

        DemeterMajorEffectDef demeterMajorEffectDef = new DemeterMajorEffectDef(Owner, targetPlayer, attackGain, healthGain);
        Owner.AddPlayerEffect(demeterMajorEffectDef);
    }
}

public class DemeterMajorEffectDef : PlayerEffect
{
    public Player Owner;
    public Player Target;
    private int attackGain = 0;
    private int healthGain = 0;

    public DemeterMajorEffectDef(Player owner, Player target, int attackGain, int healthGain)
    {
        Owner = owner; 
        Target = target;
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
        copy.Target = newOwner.GameState.GetTargetByID<Player>(Target.GetID());

        return copy;
    }

    protected void FollowerEnters(Follower follower)
    {
        if (follower.Owner != Target) return;

        foreach (Follower alliedFollower in Owner.BattleRow.Followers)
        {
            if (alliedFollower == follower) continue; 

            ChangeStatsAction action = new ChangeStatsAction(alliedFollower, attackGain, healthGain);
            Owner.GameState.ActionHandler.AddAction(action, true);
        }
    }
}


// When one of Target Player's Followers dies, your Followers gain +1/+0
public class DemeterMinor : Ritual
{
    private int attackGain = 1;
    private int healthGain = 0;
    public DemeterMinor()
    {
        Name = "Demeter\nMinor";
        Description = "When one of Target Player's Followers dies, your Followers gain +1/+0";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 8 },
                {OfferingType.Crop, 4 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Player targetPlayer) return;

        DemeterMinorEffectDef demeterMajorEffectDef = new DemeterMinorEffectDef(Owner, targetPlayer, attackGain, healthGain);
        Owner.AddPlayerEffect(demeterMajorEffectDef);
    }
}

public class DemeterMinorEffectDef : PlayerEffect
{
    public Player Owner;
    public Player Target;
    private int attackGain = 0;
    private int healthGain = 0;

    public DemeterMinorEffectDef(Player owner, Player target, int attackGain, int healthGain)
    {
        Owner = owner;
        Target = target;
        this.attackGain = attackGain;
        this.healthGain = healthGain;
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
        copy.Target = newOwner.GameState.GetTargetByID<Player>(Target.GetID());

        return copy;
    }

    protected void FollowerDies(Follower follower)
    {
        if (follower.Owner != Target) return;

        foreach (Follower alliedFollower in Owner.BattleRow.Followers)
        {
            if (alliedFollower == follower) continue;

            ChangeStatsAction action = new ChangeStatsAction(alliedFollower, attackGain, healthGain);
            Owner.GameState.ActionHandler.AddAction(action, true);
        }
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

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 6 },
                {OfferingType.Bone, 6 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.Add(Owner);

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Player targetPlayer) return;

        AresMajorEffectDef aresMajorEffectDef = new AresMajorEffectDef(Owner, targetPlayer, attackGain, healthGain);
        Owner.AddPlayerEffect(aresMajorEffectDef);
    }
}
public class AresMajorEffectDef : PlayerEffect
{
    public Player Owner;
    public Player Target;
    private int attackGain = 0;
    private int healthGain = 0;

    public AresMajorEffectDef(Player owner, Player target, int attackGain, int healthGain)
    {
        Owner = owner;
        Target = target;
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
        copy.Target = newOwner.GameState.GetTargetByID<Player>(Target.GetID());

        return copy;
    }

    protected void FollowerEnters(Follower follower)
    {
        if (follower.Owner != Target) return;

        ApplyEffectToFollower(follower);
    }

    private void ApplyEffectToFollower(Follower follower)
    {
        // Grant the Follower an innate effect
        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        follower.InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        ChangeStatsInstance gainStatsEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnKill);
        gainStatsEffectInstance.Init(attackGain, healthGain);
        effectDef.EffectInstances.Add(gainStatsEffectInstance);

        RefreshFollowerAttackInstance refreshAttackEffectInstance = new RefreshFollowerAttackInstance(effectDef, instanceTarget, 0, 1, EffectTrigger.OnKill);
        refreshAttackEffectInstance.Init(1);
        effectDef.EffectInstances.Add(refreshAttackEffectInstance);
    }

}

// Your Followers have Sprint
public class AresMinor : Ritual
{
    public AresMinor()
    {
        Name = "Ares\nMinor";
        Description = "Your Followers have Sprint";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 6 },
                {OfferingType.Bone, 3 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Player targetPlayer) return;

        AresMinorEffectDef aresMinorEffectDef = new AresMinorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(aresMinorEffectDef);
    }
}
public class AresMinorEffectDef : PlayerEffect
{
    public Player Owner;
    public Player Target;

    public AresMinorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        Target = target;
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
        AresMajorEffectDef copy = (AresMajorEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<Player>(Target.GetID());

        return copy;
    }

    protected void FollowerEnters(Follower follower)
    {
        if (follower.Owner != Target) return;

        ApplyEffectToFollower(follower);
    }

    private void ApplyEffectToFollower(Follower follower)
    {
        // Grant the Follower an innate effect
        StaticEffectDef sprintEffectDef = new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint);
        follower.InnateEffects.Add(sprintEffectDef);
    }
}

// Target Follower switches sides
public class AphroditeMajor : Ritual
{
    public AphroditeMajor()
    {
        Name = "Aphrodite\nMajor";
        Description = "Target Follower switches sides";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 10 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
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

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 5 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
        Follower follower = target as Follower;
        if (follower == null) return;

        Player newOwner = follower.Owner.GetOtherPlayer();

        GameAction newAction = new FollowerElopesAction(follower);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// When one of target Player's Followers draws blood, get a free spell with cost equal to the damage dealt
public class AthenaMajor : Ritual
{
    public AthenaMajor()
    {
        Name = "Athena\nMajor";
        Description = "When one of target Player's Followers draws blood, get a free spell with cost equal to the damage dealt";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 8 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 5 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.Add(Owner);

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Player targetPlayer) return;

        AthenaMajorEffectDef aresMajorEffectDef = new AthenaMajorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(aresMajorEffectDef);
    }
}
public class AthenaMajorEffectDef : PlayerEffect
{
    public Player Owner;
    public Player Target;

    public AthenaMajorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        Target = target;
    }

    public override void Apply()
    {
        Owner.GameState.FollowerEnters += FollowerEnters;

        foreach (Follower follower in Target.BattleRow.Followers)
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
        copy.Target = newOwner.GameState.GetTargetByID<Player>(Target.GetID());

        return copy;
    }

    protected void FollowerEnters(Follower follower)
    {
        if (follower.Owner != Target) return;

        ApplyEffectToFollower(follower);
    }

    private void ApplyEffectToFollower(Follower follower)
    {
        // Grant the Follower an innate effect
        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        follower.InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        AddRandomFreeSpellToHandInstance gainSpellEffectInstance = new AddRandomFreeSpellToHandInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDrawBlood);
        gainSpellEffectInstance.Init(Target);
        effectDef.EffectInstances.Add(gainSpellEffectInstance);
    }
}

// When one of target Player's Followers dies, get a copy of every spell that Player cast on on it. The copies costs 1 less.
public class HephaestusMajor : Ritual
{
    public HephaestusMajor()
    {
        Name = "Hephaestus\nMajor";
        Description = "When one of target Player's Followers dies, get a copy of every spell that Player cast on on it. The copies costs 1 less.";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 5 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 5 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        //targets.Add(Owner);
        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Player targetPlayer) return;

        HephaestusMajorEffectDef aresMajorEffectDef = new HephaestusMajorEffectDef(Owner, targetPlayer);
        Owner.AddPlayerEffect(aresMajorEffectDef);
    }
}
public class HephaestusMajorEffectDef : PlayerEffect
{
    public Player Owner;
    public Player Target;

    public HephaestusMajorEffectDef(Player owner, Player target)
    {
        Owner = owner;
        Target = target;
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
        AthenaMajorEffectDef copy = (AthenaMajorEffectDef)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<Player>(Target.GetID());

        return copy;
    }

    protected void FollowerEnters(Follower follower)
    {
        if (follower.Owner != Target) return;

        ApplyEffectToFollower(follower);
    }

    private void ApplyEffectToFollower(Follower follower)
    {
        // Grant the Follower an innate effect
        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        follower.InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        AddSpellsCastOnThisToHandInstance gainSpellEffectInstance = new AddSpellsCastOnThisToHandInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDeath);
        gainSpellEffectInstance.Init(Target);
        effectDef.EffectInstances.Add(gainSpellEffectInstance);
    }
}

// Steal a card from your opponent's hand. It costs 0 Gold.
public class HermesMinor : Ritual
{
    public HermesMinor()
    {
        Name = "Hermes\nMinor";
        Description = "Steal a card from your opponent's hand. It costs 0 Gold.";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 0 },
                {OfferingType.Scroll, 4 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.Add(Owner.GetOtherPlayer());

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
        Player playerTarget = target as Player;
        if (playerTarget == null) return;

        GameAction newAction = new StealCardAction(Owner, playerTarget);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// Give all Followers you summoned this turn +1/+1
public class HestiaMinor : Ritual
{
    public HestiaMinor()
    {
        Name = "Hestia\nMinor";
        Description = "Give all Followers you summoned this turn +1/+1";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
                {OfferingType.Crop, 5 },
                {OfferingType.Scroll, 0 },
            };
    }

    public override List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        targets.Add(Owner);

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
        Player playerTarget = target as Player;
        if (playerTarget == null) return;

        foreach (Follower follower in playerTarget.BattleRow.Followers)
        {
            if (follower.PlayedThisTurn)
            {
                ChangeStatsAction newAction = new ChangeStatsAction(follower, 1, 1);
                Owner.GameState.ActionHandler.AddAction(newAction);
            }
        }
    }
}
