using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

// BEAST
//

// OnDeath: Gain 1 Gold
public class Sheep : Follower
{
    public Sheep() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Beast;

        SetBaseStats(0, 1);

        Text = "On Death: Gain 1 Gold";
        Icon = IconType.Skull;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeResourceInstance newEffectInstance = new ChangeResourceInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        newEffectInstance.Init(OfferingType.Gold, 1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// OnDeath: Gain 3 Health
public class Boar : Follower
{
    public Boar() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Beast;

        SetBaseStats(2, 1);

        Text = "On Death: Gain 3 Health";
        Icon = IconType.Skull;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangePlayerHealthInstance changePlayerHealthInstance = new ChangePlayerHealthInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        changePlayerHealthInstance.Init(3);
        effectDef.EffectInstances.Add(changePlayerHealthInstance);
    }
}

// OnDeath: Lose 1 Health
public class Prey1 : Follower
{
    public Prey1() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Beast;

        SetBaseStats(0, 1);
        inherentValue = -2;

        OverrideName = "Prey";
        Text = "On Death: Lose 1 Health";
        Icon = IconType.Skull;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangePlayerHealthInstance changePlayerHealthInstance = new ChangePlayerHealthInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        changePlayerHealthInstance.Init(-1);
        effectDef.EffectInstances.Add(changePlayerHealthInstance);
    }
}
// OnDeath: Lose 1 Health
public class Prey2 : Prey1
{
}
// OnDeath: Lose 1 Health
public class Prey3 : Prey1
{
}

// OnAttack: Damage all enemies
public class GoldenHind : Follower
{
    public GoldenHind() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Beast;

        SetBaseStats(2, 4);

        Text = "On Attack: Damage all enemies equal to Golden Hind's Attack";
        Icon = IconType.Sword;
        inherentValue = 5;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        DamageAllEnemiesInstance instance = new DamageAllEnemiesInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnAttack);
        instance.Init(0, true);
        effectDef.EffectInstances.Add(instance);
    }
}

// Only takes 1 damage at a time
public class NemeanLion : Follower
{
    public NemeanLion() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Beast;

        SetBaseStats(3, 4);

        Text = "Only takes 1 damage at a time";
        Icon = IconType.Shield;
        inherentValue = 3;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override void ChangeHealth(ITarget source, int value)
    {
        if (value < -1) value = -1;

        base.ChangeHealth(source, value);
        ResolveDamage();
        ApplyOnChanged();
    }
}

// Sprint 3/1
public class MareOfDiomedes : Follower
{
    public MareOfDiomedes() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Beast;

        SetBaseStats(3, 1);

        OverrideName = "Wild Mares";
        Text = "Sprint";
        Icon = IconType.Bolt;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));
    }
}

// 5/1
public class StymphalianBird : Follower
{
    public StymphalianBird() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Beast;

        SetBaseStats(5, 1);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }
}

// OnDamage: Reduce target's Attack by 1
public class Rat : Follower
{
    public Rat() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Beast;

        SetBaseStats(1, 1);

        Text = "On Damage: Reduce target's Attack by 1";
        Icon = IconType.Fangs;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnDamage);
        newEffectInstance.Init(-1, 0, EffectTarget.Self, true);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}








/// OBJECTS
/// 

// OnAttacked: Reduce attacker's Attack by 1
public class Filth : Follower
{
    public Filth() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Object;

        SetBaseStats(0, 1);

        Text = "Taunt\nOn Attacked: Reduce attacker's Attack by 1";
        Icon = IconType.Target;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Taunt));
        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.CantAttack));

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnAttacked);
        newEffectInstance.Init(-1, 0, EffectTarget.Self, true);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// Taunt
public class WallOfTroy : Follower
{
    public WallOfTroy() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Object;

        SetBaseStats(0, 3);

        Text = "Taunt";
        Icon = IconType.Target;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Taunt));
        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.CantAttack));
    }
}

// Taunt\nOn Death: Summon a Monster
public class Corridor : Follower
{
    public Corridor() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Object;

        SetBaseStats(0, 1);

        Text = "Taunt\nCan't Attack\nOn Death: Summon a Monster";
        Icon = IconType.Target;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Taunt));

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.CantAttack));
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        SummonRandomMonsterInstance newEffectInstance = new SummonRandomMonsterInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnDeath);
        newEffectInstance.Init(0);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}






/// MORTALS
/// 

// 1/1
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
        Type = FollowerType.Mortal;
        SetBaseStats(1, 1);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }
}

// 1/3
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
        Type = FollowerType.Mortal;
        SetBaseStats(1, 3);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }
}

// 2/4
public class Hippeis : Follower
{
    public Hippeis() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };
        Type = FollowerType.Mortal;
        SetBaseStats(2, 4);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }
}

// 3/5
public class Myrmidon : Follower
{
    public Myrmidon() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };
        Type = FollowerType.Mortal;

        SetBaseStats(3, 5);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }
}

// Sprint
public class Ekdromos : Follower
{
    public Ekdromos() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 1);

        Text = "Sprint";
        Icon = IconType.Bolt;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));
    }
}

// Taunt
public class Thureophoros : Follower
{
    public Thureophoros() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 4);

        Text = "Taunt";
        Icon = IconType.Target;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Taunt));
    }
}

// Archer
public class Toxotes : Follower
{
    public Toxotes() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 2);

        Text = "Ranged attacker.\nImmune while attacking";
        Icon = IconType.Bow;
        inherentValue = 2;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.RangedAttacker));
    }
}

// OnDeath: Summon 3 1/1's
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

        Type = FollowerType.Mortal;

        SetBaseStats(0, 2);

        Text = "On Death: Summon 3 1/1's";
        Icon = IconType.Skull;
        inherentValue = 2;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        SummonPeltastInstance newEffectInstance1 = new SummonPeltastInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        effectDef.EffectInstances.Add(newEffectInstance1);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnDeath);
        effectDef.EffectInstances.Add(newEffectInstance2);

        SummonPeltastInstance newEffectInstance3 = new SummonPeltastInstance(effectDef, instanceTarget, offset, 2, EffectTrigger.OnDeath);
        effectDef.EffectInstances.Add(newEffectInstance3);
    }
}

// Adjacent Followers have Sprint
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

        Type = FollowerType.Mortal;

        SetBaseStats(0, 4);

        Text = "Adjacent Followers have Sprint";
        Icon = IconType.Bolt;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.AdjacentAllies, StaticEffect.Sprint));
    }
}
// Adjacent Followers take 1 less damage
public class Phalangite : Follower
{
    public Phalangite() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 3);

        Text = "Adjacent Followers take 1 less damage";
        Icon = IconType.Shield;
        AffectsAdjacent = true;
        inherentValue = 2;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.AdjacentAllies, StaticEffect.Shield));
    }
}

// OnDeath: Draw a card
public class Satyr : Follower
{
    public Satyr() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 1);

        Text = "On Death: Draw a card";
        Icon = IconType.Skull;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        DrawCardsInstance newEffectInstance = new DrawCardsInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        newEffectInstance.Init(1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}
// When an adjacent Follower attacks: give it +1/+0
public class Maenad : Follower
{
    public Maenad() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(3, 2);

        Text = "When an adjacent Follower attacks: give it +1/+0";
        Icon = IconType.Sword;
        AffectsAdjacent = true;
        inherentValue = 2;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnAttack);
        newEffectInstance.Init(1, 0);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}
// When you play a spell: this gains 2 health
public class Nereid : Follower
{
    public Nereid() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(3, 1);

        Text = "When you play a spell: this gains 2 health";
        Icon = IconType.Scroll;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnAnySpellCast);
        newEffectInstance.Init(0, 2);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}



// MYTHIC

// OnEnter: Summon 2 Sheep
public class Endymion : Follower
{
    public Endymion() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 1);

        Text = "On Enter: Summon 2 Sheep";
        Icon = IconType.Horn;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        SummonFollowerInstance newEffectInstance = new SummonFollowerInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnEnter);
        newEffectInstance.Init(typeof(Sheep));
        effectDef.EffectInstances.Add(newEffectInstance);

        SummonFollowerInstance newEffectInstance2 = new SummonFollowerInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnEnter);
        newEffectInstance2.Init(typeof(Sheep), 2);
        effectDef.EffectInstances.Add(newEffectInstance2);
    }
}

// When an adjacent Follower enters: Gain 1 gold next turn
public class OracleOfDelphi : Follower
{
    public OracleOfDelphi() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 4);

        Text = "When an adjacent Follower enters: Gain 1 gold next turn";
        Icon = IconType.Horn;
        AffectsAdjacent = true;
        inherentValue = 3;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeResourceDelayedInstance newEffectInstance = new ChangeResourceDelayedInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnEnter);
        newEffectInstance.Init(OfferingType.Gold, 1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// When an adjacent Follower dies: Gain 1 gold
public class Charon : Follower
{
    public Charon() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 4);

        Text = "When an adjacent Follower dies: Gain 1 Gold";
        Icon = IconType.Skull;
        AffectsAdjacent = true;
        inherentValue = 3;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeResourceInstance newEffectInstance = new ChangeResourceInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        newEffectInstance.Init(OfferingType.Gold, 1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// When an adjacent Follower kills: Add Scry card to your hand
public class Calchas : Follower
{
    public Calchas() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 6);
        inherentValue = 1;

        Text = "When an adjacent Follower kills: Add the spell Scry to your hand";
        Icon = IconType.Sickle;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnKill);
        Scry newCard = new Scry();
        newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// OnDamage: Increase Health of adjacent Followers by the amount dealt
public class Podalirius : Follower
{
    public Podalirius() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 3);
        inherentValue = 2;

        Text = "OnDamage: Increase Health of adjacent Followers by the amount dealt";
        Icon = IconType.Fangs;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDamage);
        newEffectInstance.Init(0, 0, EffectTarget.AdjacentAllies, false, true);
        effectDef.EffectInstances.Add(newEffectInstance);

        //HealFollowersInstance newEffectInstance = new HealFollowersInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDamage);
        //newEffectInstance.Init(0, EffectTarget.AdjacentAllies, true);
        //effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// When an adjacent Follower kills: Heal that Follower to full health
public class Medea : Follower
{
    public Medea() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 5);
        inherentValue = 1;

        Text = "When an adjacent Follower kills: Heal that Follower to full health";
        Icon = IconType.Sickle;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        HealFollowersInstance newEffectInstance = new HealFollowersInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnKill);
        newEffectInstance.Init(999, EffectTarget.Self);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// OnDeath: Grant adjacent Followers +2 attack and Frenzy
public class Patroclus : Follower
{
    public Patroclus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 1);
        inherentValue = 3;

        Text = "On Death: Grant adjacent Followers +2 attack and Frenzy";
        Icon = IconType.Skull;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        GiveFollowerStaticEffectInstance newEffectInstance2 = new GiveFollowerStaticEffectInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        newEffectInstance2.Init(EffectTarget.AdjacentAllies, StaticEffect.Frenzy);
        effectDef.EffectInstances.Add(newEffectInstance2);

        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnDeath);
        newEffectInstance.Init(2, 0, EffectTarget.AdjacentAllies);
        effectDef.EffectInstances.Add(newEffectInstance);

    }
}

// OnEnter: Grant adjacent Followers +2 attack and Sprint
public class Hippolyta : Follower
{
    public Hippolyta() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(3, 3);

        Text = "OnEnter: Grant adjacent Followers +2 attack and Sprint";
        Icon = IconType.Horn;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        GiveFollowerStaticEffectInstance newEffectInstance2 = new GiveFollowerStaticEffectInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnEnter);
        newEffectInstance2.Init(EffectTarget.AdjacentAllies, StaticEffect.Sprint);
        effectDef.EffectInstances.Add(newEffectInstance2);

        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnEnter);
        newEffectInstance.Init(2, 0, EffectTarget.AdjacentAllies);
        effectDef.EffectInstances.Add(newEffectInstance);

    }
}
// 1: 3/2
public class Amazon : Follower
{
    public Amazon() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(3, 2);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {


    }
}

// On Attack, summon two 1/1s
public class Agamemnon : Follower
{
    public Agamemnon() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(5, 3);

        Text = "On Attack: summon two 1/1s";
        Icon = IconType.Sword;
        inherentValue = 2;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnAttack);
        effectDef.EffectInstances.Add(newEffectInstance);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnAttack);
        newEffectInstance2.SummonPositionOffset = 1;
        effectDef.EffectInstances.Add(newEffectInstance2);
    }
}

// When Damaged, summon two 1/1s
public class Menelaus : Follower
{
    public Menelaus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 4);
        inherentValue = 4;

        Text = "On Damaged: Summon two 1/1s";
        Icon = IconType.Shield;

        SetupInnateEffects();
    }
    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDamaged);
        effectDef.EffectInstances.Add(newEffectInstance);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnDamaged);
        newEffectInstance2.SummonPositionOffset = 1;
        effectDef.EffectInstances.Add(newEffectInstance2);
    }
}

// On Kill: Gain 1 gold
public class Pyrrhus : Follower
{
    public Pyrrhus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(3, 3);
        inherentValue = 2;

        Text = "On Kill: Gain 1 Gold";
        Icon = IconType.Sickle;


        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeResourceInstance newEffectInstance = new ChangeResourceInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnKill);
        newEffectInstance.Init(OfferingType.Gold, 1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// On Draw Blood: Gain +1/+1
public class Diomedes : Follower
{
    public Diomedes() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(4, 4);
        inherentValue = 2;

        Text = "On Draw Blood: Gain +1/+1";
        Icon = IconType.Blood;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDrawBlood);
        newEffectInstance.Init(1, 1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// Attack = Missing Health
public class Icarus : Follower
{
    public Icarus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(0, 7);

        Text = "Attack equals Icarus' missing health";
        Icon = IconType.Star;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override int GetCurrentAttack()
    {
        return CurrentAttack + (MaxHealth - CurrentHealth);
    }
}

// On Kill: Add free Smite to hand
public class Atalanta : Follower
{
    public Atalanta() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 4);
        inherentValue = 2;

        Text = "On Kill: Add free Smite to hand";
        Icon = IconType.Sickle;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnKill);
        Smite newCard = new Smite();
        newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);

        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// When an adjacent Follower is targeted by a spell: You gain 3 life
public class Asclepius : Follower
{
    public Asclepius() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 4);
        inherentValue = 2;

        Text = "When an adjacent Follower is targeted by a spell: You gain 3 life";
        Icon = IconType.Scroll;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangePlayerHealthInstance newEffectInstance = new ChangePlayerHealthInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnTargeted);
        newEffectInstance.Init(3);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// When an adjacent Follower dies: Add the spell Blessing to your hand
public class Melpomene : Follower
{
    public Melpomene() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 4);
        inherentValue = 2;

        Text = "When an adjacent Follower dies:\nAdd a free Blessing spell to your hand";
        Icon = IconType.Skull;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        Blessing newCard = new Blessing();
        newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// Sprint + immune while attacking
public class Achilles : Follower
{
    public Achilles() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(3, 3);
        inherentValue = 5;

        Text = "Sprint\nFrenzy\nImmune while attacking";
        Icon = IconType.Bolt;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));
        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Frenzy));
        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.ImmuneWhileAttacking));
    }
}
// Archer 3/2
public class Paris : Follower
{
    public Paris() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(3, 2);
        inherentValue = 3;

        Text = "Ranged attacker.\nImmune while attacking";
        Icon = IconType.Bow;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.RangedAttacker));
    }
}

// Reduce damage taken by 1
public class Hector : Follower
{
    public Hector() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(5, 5);
        inherentValue = 1;

        Text = "Reduce damage taken by 1";
        Icon = IconType.Shield;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects(); 
        
        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Shield));
        //InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Shield));
    }
}

// OnAttack: Give Adjacent Followers +1/+1
public class Sarpedon : Follower
{
    public Sarpedon() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 5);
        inherentValue = 3;

        Text = "On Attack: Give Adjacent Followers +1/+1";
        Icon = IconType.Sword;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnAttack);
        newEffectInstance.Init(1, 1, EffectTarget.AdjacentAllies);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// When an adjacent Follower enters: Draw 1 card next turn
public class Cassandra : Follower
{
    public Cassandra() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 4);
        inherentValue = 2;

        Text = "When an adjacent Follower enters: Draw 1 card next turn";
        Icon = IconType.Horn;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        DrawCardsDelayedInstance newEffectInstance = new DrawCardsDelayedInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnEnter);
        newEffectInstance.Init(1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}



// GODS
//

// At the start of your turn kill your opponent's right-most Follower
// At the end of your turn kill your opponent's left-most Follower
public class Helios : Follower
{
    public Helios() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 6},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Divine;

        SetBaseStats(6, 6);
        inherentValue = 16;

        Text = "Start of turn: Destroy rightmost enemy\nEnd of turn: Destroy leftmost enemy";
        Icon = IconType.Sundial;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override void DoStartOfMyTurnEffects()
    {
        base.DoStartOfMyTurnEffects();

        Player enemyPlayer = Owner.GetOtherPlayer();
        if (enemyPlayer.BattleRow.Followers.Count == 0) return;

        Follower rightMostFollower = enemyPlayer.BattleRow.Followers[enemyPlayer.BattleRow.Followers.Count - 1];

        KillFollowerAction damageAction = new KillFollowerAction(Owner, rightMostFollower);
        Owner.GameState.ActionHandler.AddAction(damageAction);
    }

    public override void DoEndOfMyTurnEffects()
    {
        base.DoEndOfMyTurnEffects();

        Player enemyPlayer = Owner.GetOtherPlayer();
        if (enemyPlayer.BattleRow.Followers.Count == 0) return;

        Follower leftMostFollower = enemyPlayer.BattleRow.Followers[0];

        KillFollowerAction damageAction = new KillFollowerAction(Owner, leftMostFollower);
        Owner.GameState.ActionHandler.AddAction(damageAction);
    }
}

// Sprint + OnAttack: Draw 2 cards + Gain 3 health
public class Pan : Follower
{
    public Pan() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 5},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Divine;

        SetBaseStats(5, 5);
        inherentValue = 6;

        Text = "Sprint\nOn Attack: Draw 2 cards + Gain 3 Life\n";
        Icon = IconType.Sword;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        DrawCardsInstance drawCardInstance = new DrawCardsInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnAttack);
        drawCardInstance.Init(2);
        effectDef.EffectInstances.Add(drawCardInstance);

        ChangePlayerHealthInstance changePlayerHealthInstance = new ChangePlayerHealthInstance(effectDef, instanceTarget, offset, 1, EffectTrigger.OnAttack);
        changePlayerHealthInstance.Init(3);
        effectDef.EffectInstances.Add(changePlayerHealthInstance);
    }
}




// MONSTERS
// 

// +5 Attack when in middle of formation
public class Pytho : Follower
{
    public Pytho() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(0, 5);

        Text = "+5 Attack when in middle of formation";
        Icon = IconType.Star;

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

// Heal damage at end of each turn and Gain that much attack
public class Hydra : Follower
{
    public Hydra() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(1, 6);
        inherentValue = 5;

        Text = "Heal damage at end of each turn and Gain that much attack";
        Icon = IconType.Sundial;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override void DoEndOfEachTurnEffects()
    {
        base.DoEndOfEachTurnEffects();

        int healthHealed = Mathf.Max(MaxHealth - CurrentHealth, 0);

        HealAction healFollowerAction = new HealAction(this, this, healthHealed);
        Owner.GameState.ActionHandler.AddAction(healFollowerAction, true, true);

        ChangeStatsAction newAction = new ChangeStatsAction(this, healthHealed, 0);
        Owner.GameState.ActionHandler.AddAction(newAction);
        //CurrentHealth += healthHealed;
        //CurrentAttack += healthHealed;

        ApplyOnChanged();
    }
}

// At the end of your turn, if this has two neighbours: This heals 2 health
public class Minotaur : Follower
{
    public Minotaur() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(3, 6);
        inherentValue = 1;

        Text = "At the end of your turn, if this has two neighbours: This heals 2 health";
        Icon = IconType.Sundial;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override void DoEndOfMyTurnEffects()
    {
        base.DoEndOfMyTurnEffects();

        List<Follower> neighbours = Owner.BattleRow.GetAdjacentFollowers(this);
        if (neighbours.Count >= 2)
        {
            HealAction healFollowerAction = new HealAction(this, this, 2);
            Owner.GameState.ActionHandler.AddAction(healFollowerAction, true, true);
            //Heal(2);
        }
    }
}

// At the end of your turn, Deal 1 damage to an adjacent enemy 6 times
public class Scylla : Follower
{
    public Scylla() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(0, 6);
        inherentValue = 5;

        Text = "At the end of your turn, Deal 1 damage to an adjacent enemy 6 times";
        Icon = IconType.Sundial;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override void DoEndOfMyTurnEffects()
    {
        base.DoEndOfMyTurnEffects();

        for (int i = 0; i < 6; i++)
        {
            DealDamageToAdjacentEnemyAction dealDamageToAdjacentEnemyAction = new DealDamageToAdjacentEnemyAction(this, 1);
            Owner.GameState.ActionHandler.AddAction(dealDamageToAdjacentEnemyAction, true, true, true);
        }
    }
}

// At the end of your turn, Destroy all adjacent followers with less attack
public class Charybdis : Follower
{
    public Charybdis() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(2, 5);
        inherentValue = 3;

        Text = "At the end of your turn, Destroy all adjacent followers with less attack";
        Icon = IconType.Sundial;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override void DoEndOfMyTurnEffects()
    {
        base.DoEndOfMyTurnEffects();

        List<Follower> adjacentFollowers = GetAllAdjacentFollowers();

        foreach (Follower follower in adjacentFollowers)
        {
            if (follower.GetCurrentAttack() < GetCurrentAttack())
            {
                KillFollowerAction killFollowerAction = new KillFollowerAction(this, follower);
                Owner.GameState.ActionHandler.AddAction(killFollowerAction);
            }
        }
    }
}

// OnAttack: Turn the target to stone if it has less attack
public class Gorgon : Follower
{
    public Gorgon() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(3, 5);
        inherentValue = 2;

        Text = "OnAttack: Turn the target to stone if it has less attack";
        Icon = IconType.Sword;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        TurnToStoneInstance setStatsInstance = new TurnToStoneInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnAttack);
        setStatsInstance.Init(useTargetInsteadOfType:true);
        effectDef.EffectInstances.Add(setStatsInstance);
    }
}

// Cleave
public class Cerberus : Follower
{
    public Cerberus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(3, 9);
        inherentValue = 3;

        Text = "OnAttack: Also damage the target's neighbours";
        Icon = IconType.Sword;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Cleave));
    }
}

// OnAttacked: Deal 2 damage 
public class Siren : Follower
{
    public Siren() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(1, 1);
        inherentValue = 2;

        Text = "On Attacked:\nDeal 2 damage";
        Icon = IconType.Shield;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new DamageOnAttackedEffectDef(EffectTarget.Self, 2));
    }
}

// Life Siphon
public class Lamia : Follower
{
    public Lamia() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(1, 3);

        Text = "Life Siphon";
        Icon = IconType.Fangs;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new LifeSiphonEffectDef(EffectTarget.Self));
    }
}

// OnAttack + OnAttacked: Deal damage equal to its attack
public class Chimera : Follower
{
    public Chimera() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(2, 3);
        inherentValue = 4;

        Text = "Sprint\nOn Attack or Attacked: Deal damage equal to its Attack\n";
        Icon = IconType.Sword;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Sprint));

        InnateEffects.Add(new DealAttackDamageOnAttackEffectDef(EffectTarget.Self));
        InnateEffects.Add(new DealAttackDamageOnAttackedEffectDef(EffectTarget.Self));
    }
}

// When an adjacent Follower enters: Get a Confusion spell
public class Sphinx : Follower
{
    public Sphinx() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(2, 5);
        inherentValue = 1;

        Text = "When an adjacent Follower enters: Get a Confusion spell";
        Icon = IconType.Horn;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnEnter);
        Confusion newCard = new Confusion();
        //newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// Low Vision
public class Cyclops : Follower
{
    public Cyclops()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(3, 4);

        Text = "Can only attack Followers directly in front of it";
        Icon = IconType.Star;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.LowVision));
    }
}

// Adjacent Followers have Life Siphon
public class Empusa : Follower
{
    public Empusa() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(2, 4);
        inherentValue = 1;

        Text = "Adjacent Followers have Life Siphon";
        Icon = IconType.Fangs;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new LifeSiphonEffectDef(EffectTarget.AdjacentAllies));
    }
}

// When an adjacent Follower dies: Gain +2 attack
public class Keres : Follower
{
    public Keres() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(0, 4);

        Text = "When an adjacent Follower dies: Gain +2 attack";
        Icon = IconType.Skull;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnDeath);
        newEffectInstance.Init(2, 0, EffectTarget.EffectSource);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}
// When any Follower dies: Deal 2 damage to the opponent
public class Erinyes : Follower
{
    public Erinyes() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(1, 2);
        inherentValue = 4;

        Text = "When any Follower dies: Deal 2 damage to the opponent";
        Icon = IconType.Skull;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        ChangePlayerHealthInstance changePlayerHealthInstance = new ChangePlayerHealthInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnAnyFollowerDeath);
        changePlayerHealthInstance.Init(-2, false);
        effectDef.EffectInstances.Add(changePlayerHealthInstance);
    }
}

// Adjacent Followers and Typhon have OnKill: Can attack again
public class Typhon : Follower
{
    public Typhon() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 5},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(4, 6);
        inherentValue = 8;

        Text = "Adjacent Followers and Typhon have 'On Kill: Can attack again'";
        Icon = IconType.Sickle;
        AffectsAdjacent = true;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);

        CustomEffectDef adjacentCustomEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        adjacentCustomEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(adjacentCustomEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        RefreshFollowerAttackInstance newEffectInstance = new RefreshFollowerAttackInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnKill);
        newEffectInstance.Init(1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// OnKill: Summon a Monster
public class Echidna : Follower
{
    public Echidna() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 5},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(4, 6);
        inherentValue = 8;

        Text = "OnKill: Summon a Monster";
        Icon = IconType.Sickle;

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget, int offset)
    {
        SummonRandomMonsterInstance newEffectInstance = new SummonRandomMonsterInstance(effectDef, instanceTarget, offset, 0, EffectTrigger.OnKill);
        newEffectInstance.Init(1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}





/// SPELLS
///

// Summon 3 1/1s for target player
public class DragonsTeeth : Spell
{
    public DragonsTeeth()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Summon 3 1/1s for target player";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Player playerTarget)
        {
            for (int i = 0; i < 3; i++)
            {
                int index = playerTarget.BattleRow.Followers.Count;
                Peltast peltast = new Peltast();
                peltast.Init(playerTarget);
                GameAction newAction = new SummonFollowerAction(peltast, index);
                playerTarget.GameState.ActionHandler.AddAction(newAction);
            }
        }
    }
}
// Summon 3 0/1 prey for target player
public class ReleasePrey : Spell
{
    public ReleasePrey()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Release Prey";
        Text = "Summon 3 0/1 Prey for target player";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Player playerTarget)
        {
            int index = playerTarget.BattleRow.Followers.Count;
            Prey1 follower = new Prey1();
            follower.Init(playerTarget);
            GameAction newAction = new SummonFollowerAction(follower, index);
            playerTarget.GameState.ActionHandler.AddAction(newAction);

            index = playerTarget.BattleRow.Followers.Count;
            Prey2 follower2 = new Prey2();
            follower2.Init(playerTarget);
            GameAction newAction2 = new SummonFollowerAction(follower2, index);
            playerTarget.GameState.ActionHandler.AddAction(newAction2);

            index = playerTarget.BattleRow.Followers.Count;
            Prey3 follower3 = new Prey3();
            follower3.Init(playerTarget);
            GameAction newAction3 = new SummonFollowerAction(follower3, index);
            playerTarget.GameState.ActionHandler.AddAction(newAction3);
        }
    }
}
// Summon 3 0/1 filth for target player
public class CreateFilth : Spell
{
    public CreateFilth()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Create Filth";
        Text = "Summon 3 0/1 Filth for target player";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Player playerTarget)
        {
            int index = playerTarget.BattleRow.Followers.Count;
            Filth follower = new Filth();
            follower.Init(playerTarget);
            GameAction newAction = new SummonFollowerAction(follower, index);
            playerTarget.GameState.ActionHandler.AddAction(newAction);

            index = playerTarget.BattleRow.Followers.Count;
            Filth follower2 = new Filth();
            follower2.Init(playerTarget);
            GameAction newAction2 = new SummonFollowerAction(follower2, index);
            playerTarget.GameState.ActionHandler.AddAction(newAction2);

            index = playerTarget.BattleRow.Followers.Count;
            Filth follower3 = new Filth();
            follower3.Init(playerTarget);
            GameAction newAction3 = new SummonFollowerAction(follower3, index);
            playerTarget.GameState.ActionHandler.AddAction(newAction3);
        }
    }
}

// Deal 3 damage to a Follower
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

        Text = "Deal 3 Damage to a Follower";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        //targets.AddRange(ITarget.GetAllPlayers(Owner));
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

// Deal 2 damage
public class ThrowStone : Spell
{
    private int damage = 2;
    public ThrowStone()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Throw Stone";
        Text = "Deal 2 Damage";
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

// Deal 2 damage to all enemies
public class Lightning : Spell
{
    private int damage = 2;
    public Lightning()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Deal 2 Damage to all enemies";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.Add(Owner.GetOtherPlayer());

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        Player otherPlayer = Owner.GetOtherPlayer();

        List<Follower> enemyFollowers = new List<Follower>(otherPlayer.BattleRow.Followers);
        foreach (Follower follower in enemyFollowers)
        {
            DealDamageAction action = new DealDamageAction(Owner, follower, damage);
            Owner.GameState.ActionHandler.AddAction(action);
        }

        DealDamageAction actionOnEnemyPlayer = new DealDamageAction(Owner, otherPlayer, damage);
        Owner.GameState.ActionHandler.AddAction(actionOnEnemyPlayer);
    }
}

// Draw 2 Cards
public class Scry : Spell
{
    private int cardsDrawn = 2;
    public Scry()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Draw 2 Cards";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.Add(Owner);

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        DrawCardAction drawAction = new DrawCardAction(Owner, target, cardsDrawn);
        Owner.GameState.ActionHandler.AddAction(drawAction);
    }
}

// Give a Follower +2/+0
public class Blessing : Spell
{
    private int attackGained = 2;
    private int healthGained = 0;
    public Blessing()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower +2 Attack";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        ChangeStatsAction newAction = new ChangeStatsAction(target, attackGained, healthGained);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}


// Give a Follower +1/+0
public class Contraption : Spell
{
    private int attackGained = 1;
    private int healthGained = 0;
    public Contraption()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower +1/+0";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        ChangeStatsAction newAction = new ChangeStatsAction(target, attackGained, healthGained);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// Give a Follower -1/-0
public class Confusion : Spell
{
    private int attackGained = -1;
    private int healthGained = 0;
    public Confusion()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower \n-1/-0";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        ChangeStatsAction newAction = new ChangeStatsAction(target, attackGained, healthGained);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// Give a Follower Sprint
public class Talaria : Spell
{
    public Talaria()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower Sprint";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Follower targetFollower)
        {
            GiveFollowerStaticEffectAction newAction = new GiveFollowerStaticEffectAction(target, StaticEffect.Sprint);
            Owner.GameState.ActionHandler.AddAction(newAction);
        }
    }
}

// Give a Follower +1 Attack and Frenzy
public class HarpeOfPerseus : Spell
{
    private int attackGained = 1;
    private int healthGained = 0;

    public HarpeOfPerseus()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower +1 Attack and Frenzy";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Follower targetFollower)
        {
            GiveFollowerStaticEffectAction newAction = new GiveFollowerStaticEffectAction(target, StaticEffect.Frenzy);
            Owner.GameState.ActionHandler.AddAction(newAction);

            ChangeStatsAction newAction2 = new ChangeStatsAction(target, attackGained, healthGained);
            Owner.GameState.ActionHandler.AddAction(newAction2);
        }
    }
}

// Give a Follower +2 Health and Taunt
public class ShieldOfAjax : Spell
{
    private int attackGained = 0;
    private int healthGained = 2;

    public ShieldOfAjax()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Shield of Ajax";
        Text = "Give a Follower +2 Health and Taunt";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Follower targetFollower)
        {
            GiveFollowerStaticEffectAction newAction = new GiveFollowerStaticEffectAction(target, StaticEffect.Taunt);
            Owner.GameState.ActionHandler.AddAction(newAction);

            ChangeStatsAction newAction2 = new ChangeStatsAction(target, attackGained, healthGained);
            Owner.GameState.ActionHandler.AddAction(newAction2);
        }
    }
}

// Give a Follower +1 Attack and Life Siphon
public class StygianPact : Spell
{
    private int attackGained = 1;
    private int healthGained = 0;

    public StygianPact()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower +1 Attack and Life Siphon";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Follower targetFollower)
        {
            LifeSiphonEffectDef newEffectDef = new LifeSiphonEffectDef(EffectTarget.Self);
            targetFollower.InnateEffects.Add(newEffectDef);
            newEffectDef.Apply(targetFollower);

            ChangeStatsAction newAction2 = new ChangeStatsAction(target, attackGained, healthGained);
            Owner.GameState.ActionHandler.AddAction(newAction2);
        }
    }
}


// Give a Follower +3 Health and Restore 3 Life
public class Reverie : Spell
{
    private int attackGained = 0;
    private int healthGained = 3;

    public Reverie()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower +3 Health and gain 3 Life";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Follower targetFollower)
        {
            ChangeStatsAction newAction = new ChangeStatsAction(target, attackGained, healthGained);
            Owner.GameState.ActionHandler.AddAction(newAction);

            ChangePlayerHealthAction gainLifeAction = new ChangePlayerHealthAction(Owner, Owner, healthGained);
            Owner.GameState.ActionHandler.AddAction(gainLifeAction, true);
        }
    }
}

// Make a Follower attack
public class Panic : Spell
{
    public Panic()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Make a Follower attack";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
        Follower attackingFollower = target as Follower;
        if (attackingFollower == null) return;

        List<ITarget> possibleTargets = attackingFollower.GetAttackTargets(true);
        if (possibleTargets.Count == 0) return;

        ITarget attackTarget = possibleTargets[Owner.GameState.RNG.Next(0, possibleTargets.Count)];
        GameAction newAction = new PreAttackWithFollowerAction(attackingFollower, attackTarget);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// Heal a Follower to full health. Gain that much Life
public class Restoration : Spell
{
    public Restoration()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Heal a Follower to full health. You gain that much Life";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is Follower targetFollower)
        {
            int healthToHeal = targetFollower.MaxHealth - targetFollower.CurrentHealth;

            HealAction healFollowerAction = new HealAction(targetFollower, Owner, healthToHeal);
            Owner.GameState.ActionHandler.AddAction(healFollowerAction, true);

            ChangePlayerHealthAction gainLifeAction = new ChangePlayerHealthAction(Owner, Owner, healthToHeal);
            Owner.GameState.ActionHandler.AddAction(gainLifeAction, true);
        }
    }
}

// Deal 4 Damage (costs 1 less per Follower that died this turn)
public class Vengeance : Spell
{
    private int damage = 4;
    public Vengeance()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 3},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Deal 4 Damage (costs 1 less per Follower that died this turn)";
        HasTargets = true;
    }

    public override Dictionary<OfferingType, int> GetCosts()
    {
        if (GameState == null)
        {
            return Costs;
        }
        Dictionary<OfferingType, int> reducedCosts = new Dictionary<OfferingType, int>(Costs);
        foreach (var offeringType in Costs)
        {
            if (offeringType.Key == OfferingType.Gold)
            {
                reducedCosts[offeringType.Key] = Mathf.Max(0, offeringType.Value - GameState.FollowerDeathsThisTurn);
            }
        }
        return reducedCosts;
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

// Spend all your Crops: Gain that much health
public class LastingGift : Spell
{
    //private int attackGained = 0;
    //private int healthGained = 0;

    public LastingGift()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Spend all your Crops: Gain that much health";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllPlayers(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        int healthToHeal = Owner.Offerings[OfferingType.Crop];
        Owner.Offerings[OfferingType.Crop] = 0;

        HealAction healPlayerAction = new HealAction(Owner, Owner, healthToHeal);
        Owner.GameState.ActionHandler.AddAction(healPlayerAction, true, true);
    }
}

// Deal 1 damage to all Characters for each Follower that has died this turn
public class RiverStyx : Spell
{
    public RiverStyx()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = " Deal 1 damage to all Characters for each Follower that has died this turn";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.Add(Owner);

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        int followerDeathsThisTurn = Owner.GameState.FollowerDeathsThisTurn;

        DealDamageToAllCharactersAction damageAllCharactersAction = new DealDamageToAllCharactersAction(Owner, followerDeathsThisTurn);
        Owner.GameState.ActionHandler.AddAction(damageAllCharactersAction, true, true);
    }
}

// Create a copy of a Follower with exactly 1 neighbour
public class Reflection : Spell
{
    public Reflection()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Create a copy of a Follower with exactly 1 neighbour";
        HasTargets = true;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        return GetTargets().Count > 0;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetAllFollowersOnEdges(Owner));

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);


        if (target is not Follower followerTarget) return;

        if (followerTarget.Owner.BattleRow.Followers.Count == 0) return;

        bool targetOnLeftEdge = followerTarget.Owner.BattleRow.GetIndexOfFollower(followerTarget) == 0;
        bool targetOnRightEdge = followerTarget.Owner.BattleRow.GetIndexOfFollower(followerTarget) == followerTarget.Owner.BattleRow.Followers.Count-1;

        if (!targetOnLeftEdge && !targetOnRightEdge) return;

        int cloneIndex = targetOnLeftEdge ? followerTarget.Owner.BattleRow.Followers.Count : 0;

        Follower newFollower = (Follower)followerTarget.Clone();
        newFollower.Init(followerTarget.Owner);
        newFollower.RefreshAttacks();

        //Follower copy = (Follower)followerTarget.MakeBaseCopy();
        //copy.Owner = Owner;
        //copy.CurrentAttack = 1;
        //copy.CurrentHealth = 1;
        //int index = Owner.BattleRow.Followers.Count;

        GameAction newAction = new SummonFollowerAction(newFollower, cloneIndex);
        Owner.GameState.ActionHandler.AddAction(newAction);
    }
}

// If there are two or more Followers with at least 3 Attack, destroy all Followers
public class Titanomachy : Spell
{
    public Titanomachy()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "If there are two or more Followers with at least 3 Attack, destroy all Followers";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.Add(Owner);

        return targets;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        int giantFollowerCount = 0;

        foreach (Follower follower in Owner.BattleRow.Followers)
        {
            if (follower.CurrentAttack >= 5) giantFollowerCount++;
        }

        Player otherPlayer = Owner.GetOtherPlayer();

        foreach (Follower follower in otherPlayer.BattleRow.Followers)
        {
            if (follower.CurrentAttack >= 5) giantFollowerCount++;
        }

        return giantFollowerCount > 1;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        int giantFollowerCount = 0;

        foreach (Follower follower in Owner.BattleRow.Followers)
        {
            if (follower.CurrentAttack >= 3) giantFollowerCount++;
        }

        Player otherPlayer = Owner.GetOtherPlayer();

        foreach (Follower follower in otherPlayer.BattleRow.Followers)
        {
            if (follower.CurrentAttack >= 3) giantFollowerCount++;
        }
        
        if (giantFollowerCount > 1)
        {
            List<Follower> ownerFollowers = new List<Follower>(Owner.BattleRow.Followers);
            foreach (Follower follower in ownerFollowers)
            {
                KillFollowerAction killAction = new KillFollowerAction(Owner, follower);
                Owner.GameState.ActionHandler.AddAction(killAction);
            }

            List<Follower> enemyFollowers = new List<Follower>(otherPlayer.BattleRow.Followers);
            foreach (Follower follower in enemyFollowers)
            {
                KillFollowerAction killAction = new KillFollowerAction(Owner, follower);
                Owner.GameState.ActionHandler.AddAction(killAction);
            }
        }
    }
}

// Destroy target Follower with the lowest Attack among its allies
public class Drown : Spell
{
    public Drown()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Destroy target Follower with the lowest Attack among its allies";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        // Get targets for Owner
        int lowestAttack = 9999;
        List<Follower> lowestAttackFollowers = new List<Follower>();
        foreach (Follower follower in Owner.BattleRow.Followers)
        {
            int currentAttack = follower.GetCurrentAttack();
            if (currentAttack == lowestAttack)
            {
                lowestAttackFollowers.Add(follower);
            }
            else if (currentAttack < lowestAttack)
            {
                lowestAttackFollowers.Clear();
                lowestAttackFollowers.Add(follower);
                lowestAttack = currentAttack;
            }
        }
        targets.AddRange(lowestAttackFollowers);

        // Get targets for Opponent
        lowestAttack = 9999;
        lowestAttackFollowers.Clear();

        foreach (Follower follower in Owner.GetOtherPlayer().BattleRow.Followers)
        {
            int currentAttack = follower.GetCurrentAttack();
            if (currentAttack == lowestAttack)
            {
                lowestAttackFollowers.Add(follower);
            }
            else if (currentAttack < lowestAttack)
            {
                lowestAttackFollowers.Clear();
                lowestAttackFollowers.Add(follower);
                lowestAttack = currentAttack;
            }
        }
        targets.AddRange(lowestAttackFollowers);

        return targets;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);
        Follower targetFollower = target as Follower;
        if (targetFollower == null) return;

        KillFollowerAction killAction = new KillFollowerAction(Owner, targetFollower);
        Owner.GameState.ActionHandler.AddAction(killAction);
    }
}





/// Sacrifices
/// 

// Sacrifice a Follower to Gain 1 Gold
public class PriceOfProfit : Spell
{
    public PriceOfProfit()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Price Of Profit";
        Text = "Sacrifice a Follower to Gain 1 Gold";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        return Owner.BattleRow.Followers.Count > 0;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Follower followerTarget) return;

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget, true);
        Owner.GameState.ActionHandler.AddAction(killAction);

        ChangeResourceAction action = new ChangeResourceAction(Owner, OfferingType.Gold, 1);
        Owner.GameState.ActionHandler.AddAction(action, true);
    }
}

// Sacrifice a Follower to Draw 2 Cards
public class PriceOfKnowledge : Spell
{
    public PriceOfKnowledge()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Price of Knowledge";
        Text = "Sacrifice a Follower to Draw 2 Cards";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        return Owner.BattleRow.Followers.Count > 0;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Follower followerTarget) return;

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget, true);
        Owner.GameState.ActionHandler.AddAction(killAction);

        DrawCardAction action = new DrawCardAction(Owner, Owner, 2);
        Owner.GameState.ActionHandler.AddAction(action, true);
    }
}

// Sacrifice a Follower and lose life equal to its health to Draw that many Cards
public class PriceOfInspiration : Spell
{
    public PriceOfInspiration()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Price of Inspiration";
        Text = "Sacrifice a Follower and lose life equal to its health to Draw that many Cards";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        return Owner.BattleRow.Followers.Count > 0;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Follower followerTarget) return;

        int followerHealth = followerTarget.CurrentHealth;
        ChangePlayerHealthAction payLifeAction = new ChangePlayerHealthAction(Owner, Owner, -followerHealth);
        Owner.GameState.ActionHandler.AddAction(payLifeAction, true);

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget, true);
        Owner.GameState.ActionHandler.AddAction(killAction);

        DrawCardAction action = new DrawCardAction(Owner, Owner, followerHealth);
        Owner.GameState.ActionHandler.AddAction(action, true);
    }
}

// Sacrifice a Follower and lose life equal to its attack to Gain that much Gold
public class PriceOfWealth : Spell
{
    public PriceOfWealth()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Price of Wealth";
        Text = "Sacrifice a Follower and lose life equal to its attack to Gain that much Gold";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        return Owner.BattleRow.Followers.Count > 0;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Follower followerTarget) return;

        int followerAttack = followerTarget.CurrentAttack;
        ChangePlayerHealthAction payLifeAction = new ChangePlayerHealthAction(Owner, Owner, -followerAttack);
        Owner.GameState.ActionHandler.AddAction(payLifeAction, true);

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget, true);
        Owner.GameState.ActionHandler.AddAction(killAction);

        ChangeResourceAction action = new ChangeResourceAction(Owner, OfferingType.Gold, followerAttack);
        Owner.GameState.ActionHandler.AddAction(action, true);
    }
}

// Sacrifice a Follower and deal damage to your opponent equal to twice its attack
public class PriceOfReprisal : Spell
{
    public PriceOfReprisal()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Price of Reprisal";
        Text = "Sacrifice a Follower and deal damage to your opponent equal to twice its attack";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        return Owner.BattleRow.Followers.Count > 0;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Follower followerTarget) return;

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget, true);
        Owner.GameState.ActionHandler.AddAction(killAction);

        int followerAttack = followerTarget.CurrentAttack;
        ChangePlayerHealthAction dealDamageAction = new ChangePlayerHealthAction(Owner.GetOtherPlayer(), Owner, -(followerAttack * 2));
        Owner.GameState.ActionHandler.AddAction(dealDamageAction, true);
    }
}

// Sacrifice a Follower and gain life equal to its attack and health
public class PriceOfRenewal : Spell
{
    public PriceOfRenewal()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Price of Renewal";
        Text = "Sacrifice a Follower and gain life equal to its attack and health";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        return Owner.BattleRow.Followers.Count > 0;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Follower followerTarget) return;

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget, true);
        Owner.GameState.ActionHandler.AddAction(killAction);

        int lifeGain = followerTarget.GetCurrentAttack() + followerTarget.CurrentHealth;
        ChangePlayerHealthAction payLifeAction = new ChangePlayerHealthAction(Owner, Owner, lifeGain);
        Owner.GameState.ActionHandler.AddAction(payLifeAction, true);
    }
}

// Sacrifice a Follower and lose life equal to its attack to Summon that many 1/1s
public class PriceOfLegacy : Spell
{
    public PriceOfLegacy()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Price of Legacy";
        Text = "Sacrifice a Follower and lose life equal to its attack to Summon that many 1/1s";
        HasTargets = true;
    }

    public override List<ITarget> GetTargets()
    {
        List<ITarget> targets = new List<ITarget>();

        targets.AddRange(ITarget.GetOwnFollowers(Owner));

        return targets;
    }

    public override bool CanPlay()
    {
        if (!base.CanPlay()) return false;

        return Owner.BattleRow.Followers.Count > 0;
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        if (target is not Follower followerTarget) return;

        int followerAttack = followerTarget.CurrentAttack;
        ChangePlayerHealthAction payLifeAction = new ChangePlayerHealthAction(Owner, Owner, -followerAttack);
        Owner.GameState.ActionHandler.AddAction(payLifeAction, true);

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget, true);
        Owner.GameState.ActionHandler.AddAction(killAction);

        for (int i = 0; i < followerAttack; i++)
        {
            int index = Owner.BattleRow.Followers.Count;
            Peltast peltast = new Peltast();
            peltast.Init(Owner);
            GameAction newAction = new SummonFollowerAction(peltast, index);
            Owner.GameState.ActionHandler.AddAction(newAction);
        }
    }
}


// Deal 100 damage
public class DevKill : Spell
{
    private int damage = 100;
    public DevKill()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OverrideName = "Dev Kill";
        Text = "Deal 100 Damage";
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