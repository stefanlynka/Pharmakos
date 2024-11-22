using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class Peltast : Follower
{
    public Peltast() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(1, 1);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }
}

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

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        //InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));
    }
}

// On Attack, summon two 1/1s
public class Agamemnon : Follower
{
    public Agamemnon() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(3, 3);

        Text = "King 1";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new AgamemnonSummonEffectDef(EffectTarget.Self));
    }
}

// When Damaged, summon two 1/1s
public class Menelaus : Follower
{
    public Menelaus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(3, 3);

        Text = "King 2";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new MenelausSummonEffectDef(EffectTarget.Self));
    }
}

// On Kill: Gain 1 gold
public class Pyrrhus : Follower
{
    public Pyrrhus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(3, 2);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        //InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));
        InnateEffects.Add(new PyrrhusEffectDef(EffectTarget.Self));
    }
}

// On Draw Blood: Gain +1/+1
public class Diomedes : Follower
{
    public Diomedes() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(3, 2);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        //InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));
        InnateEffects.Add(new DiomedesEffectDef(EffectTarget.Self));
    }
}

// Attack = Missing Health
public class Icarus : Follower
{
    public Icarus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(0, 7);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        //InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));
    }

    public override int GetCurrentAttack()
    {
        return CurrentAttack + (BaseHealth - CurrentHealth);
    }
}

// On Kill: Add free Smite to hand
public class Atalanta : Follower
{
    public Atalanta() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(2, 4);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new AtalantaEffectDef(EffectTarget.Self));
        //InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));
    }
}

// +5 Attack when in middle of formation
public class Pytho : Follower
{
    public Pytho() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(0, 5);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override int GetCurrentAttack()
    {
        if (Owner != null)
        {
            int index = Owner.BattleRow.GetIndexOfFollower(this);
            if (index * 2 + 1 == Owner.BattleRow.Followers.Count)
            {
                return CurrentAttack + 5;
            }
        }
        return CurrentAttack;
    }
}













public class TrojanHorse : Follower
{
    public TrojanHorse() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(0, 2);

        Text = "Surprise!";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new TrojanHorseSummonEffectDef(EffectTarget.Self));
    }
}

public class Chariot : Follower
{
    public Chariot() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(1, 5);

        Text = "Adjacent Followers have Sprint";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.AdjacentAllies, StaticEffect.Sprint));
    }
}

public class Chimera : Follower
{
    public Chimera() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        SetBaseStats(3, 3);

        Text = "Hiss!";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));

        InnateEffects.Add(new DamageOnAttackEffectDef(EffectTarget.Self));
        InnateEffects.Add(new DamageOnAttackedEffectDef(EffectTarget.Self));
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
    }

}