using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class ZeusMinor : Ritual
{
    public ZeusMinor()
    {
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
        copy.Owner = Owner;
        copy.CurrentAttack = 1;
        copy.CurrentHealth = 1;
        int index = Owner.BattleRow.Followers.Count;

        GameAction newAction = new SummonFollowerAction(copy, index);
        Owner.GameState.ActionHandler.AddAction(newAction);
        //Owner.SummonFollower(copy, index);
    }
}

public class ZeusMajor : Ritual
{
    private int damage = 5;
    public ZeusMajor()
    {
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

public class HadesMinor : Ritual
{
    public HadesMinor()
    {
        Description = "Summon a 2/2 copy of a Follower";

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
        copy.Owner = Owner;
        copy.CurrentAttack = 2;
        copy.CurrentHealth = 2;
        int index = Owner.BattleRow.Followers.Count;

        GameAction newAction = new SummonFollowerAction(copy, index);
        Owner.GameState.ActionHandler.AddAction(newAction);
        //Owner.SummonFollower(copy, index);
    }
}

public class HadesMajor : Ritual
{
    private int damage = 5;
    public HadesMajor()
    {
        Description = "Deal 10 Damage";

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
    }
}

public class DemeterMajor : Ritual
{
    private int attackGain = 0;
    private int healthGain = 1;
    public DemeterMajor()
    {
        Description = "When a Follower enters Target Player's BattleRow, your Followers gain +0/+1";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
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

public class DemeterMinor : Ritual
{
    private int attackGain = 1;
    private int healthGain = 0;
    public DemeterMinor()
    {
        Description = "When one of Target Player's Followers dies, your Followers gain +1/+0";

        Costs = new Dictionary<OfferingType, int>()
            {
                {OfferingType.Blood, 0 },
                {OfferingType.Bone, 0 },
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
        DemeterMajorEffectDef copy = (DemeterMajorEffectDef)MemberwiseClone();
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
