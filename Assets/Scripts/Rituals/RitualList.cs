using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ZeusMinor : Ritual
{
    public ZeusMinor(Player player) : base(player)
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
        Owner.GameState.ActionManager.AddAction(newAction);
        Owner.GameState.ActionManager.StartEvaluating();
        //Owner.SummonFollower(copy, index);
    }
}

public class ZeusMajor : Ritual
{
    private int damage = 5;
    public ZeusMajor(Player player) : base(player)
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
        Follower follower = target as Follower;
        Player player = target as Player;
        if (follower != null)
        {
            follower.ChangeHealth(-damage);
        }
        else if (player != null)
        {
            player.ChangeHealth(-damage);
        }
    }
}