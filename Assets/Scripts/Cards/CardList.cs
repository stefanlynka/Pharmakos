using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;



public class Hoplite : Follower
{
    public Hoplite() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(2, 3);

        Text = "Hop to it";
    }
}

public class Chariot : Follower
{
    public Chariot() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(1, 5);

        Text = "Adjacent Followers have Sprint";
    }
}

public class DragonsTeeth : Spell
{
    public DragonsTeeth()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 3},
            { OfferingType.Crop, 3},
            { OfferingType.Scroll, 0},
        };

    }
}

public class Sphinx : Follower
{
    public Sphinx()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(2, 6);
    }
}

public class Cyclops : Follower
{
    public Cyclops()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 3},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(5, 6);
    }
}

public class Smite : Spell
{
    private int damage = 3;
    public Smite()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Deal 3 Damage";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));
        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        DealDamageAction damageAction = new DealDamageAction(Owner, target, damage);
        Owner.GameState.ActionHandler.AddAction(damageAction);
        Owner.GameState.ActionHandler.StartEvaluating();
    }

}