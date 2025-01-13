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

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        ChangeResourceInstance newEffectInstance = new ChangeResourceInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDeath);
        newEffectInstance.Init(OfferingType.Gold, 1);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

/// MORTALS
/// 

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

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        SummonPeltastInstance newEffectInstance1 = new SummonPeltastInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDeath);
        effectDef.EffectInstances.Add(newEffectInstance1);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(effectDef, instanceTarget, 0, 1, EffectTrigger.OnDeath);
        effectDef.EffectInstances.Add(newEffectInstance2);

        SummonPeltastInstance newEffectInstance3 = new SummonPeltastInstance(effectDef, instanceTarget, 0, 2, EffectTrigger.OnDeath);
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

// OnEnter: Summon 2 Sheep
public class Endymion : Follower
{
    public Endymion() : base()
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

        SetBaseStats(2, 1);

        Text = "On Enter: Summon 2 Sheep";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        SummonFollowerInstance newEffectInstance = new SummonFollowerInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnEnter);
        newEffectInstance.Init(typeof(Sheep));
        effectDef.EffectInstances.Add(newEffectInstance);

        SummonFollowerInstance newEffectInstance2 = new SummonFollowerInstance(effectDef, instanceTarget, 0, 1, EffectTrigger.OnEnter);
        newEffectInstance2.Init(typeof(Sheep), 1);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 6);

        Text = "When an adjacent Follower enters: Gain 1 gold next turn";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        ChangeResourceDelayedInstance newEffectInstance = new ChangeResourceDelayedInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnEnter);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 6);

        Text = "When an adjacent Follower dies: Gain 1 Gold";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        ChangeResourceInstance newEffectInstance = new ChangeResourceInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDeath);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 6);

        Text = "When an adjacent Follower kills: Add the spell Scry to your hand";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnKill);
        Scry newCard = new Scry();
        newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// OnDamage: Heal adjacent Followers the amount dealt
public class Podalirius : Follower
{
    public Podalirius() : base()
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

        SetBaseStats(1, 6);

        Text = "OnDamage: Heal adjacent Followers the amount dealt";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        HealFollowersInstance newEffectInstance = new HealFollowersInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDamage);
        newEffectInstance.Init(0, EffectTarget.AdjacentAllies, true);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// When an adjacent Follower kills: Heal that Follower to full health
public class Medea : Follower
{
    public Medea() : base()
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

        SetBaseStats(1, 6);

        Text = "When an adjacent Follower kills: Heal that Follower to full health";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        HealFollowersInstance newEffectInstance = new HealFollowersInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnKill);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 1);

        Text = "OnDeath: Grant adjacent Followers +2 attack and Frenzy";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        GiveFollowerStaticEffectInstance newEffectInstance2 = new GiveFollowerStaticEffectInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDeath);
        newEffectInstance2.Init(EffectTarget.AdjacentAllies, StaticEffect.Frenzy);
        effectDef.EffectInstances.Add(newEffectInstance2);

        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, 0, 1, EffectTrigger.OnDeath);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(1, 1);

        Text = "OnEnter: Grant adjacent Followers +2 attack and Sprint";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        GiveFollowerStaticEffectInstance newEffectInstance2 = new GiveFollowerStaticEffectInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnEnter);
        newEffectInstance2.Init(EffectTarget.AdjacentAllies, StaticEffect.Sprint);
        effectDef.EffectInstances.Add(newEffectInstance2);

        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, 0, 1, EffectTrigger.OnEnter);
        newEffectInstance.Init(2, 0, EffectTarget.AdjacentAllies);
        effectDef.EffectInstances.Add(newEffectInstance);

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

        Type = FollowerType.Mortal;

        SetBaseStats(5, 3);

        Text = "King 1";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }

    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnAttack);
        effectDef.EffectInstances.Add(newEffectInstance);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(effectDef, instanceTarget, 0, 1, EffectTrigger.OnAttack);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(3, 3);

        Text = "King 2";

        SetupInnateEffects();
    }
    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        SummonPeltastInstance newEffectInstance = new SummonPeltastInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDamaged);
        effectDef.EffectInstances.Add(newEffectInstance);

        SummonPeltastInstance newEffectInstance2 = new SummonPeltastInstance(effectDef, instanceTarget, 0, 1, EffectTrigger.OnDamaged);
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
            { OfferingType.Gold, 0},
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

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        ChangeResourceInstance newEffectInstance = new ChangeResourceInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnKill);
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
            { OfferingType.Gold, 0},
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

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDrawBlood);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(0, 7);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
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

        Type = FollowerType.Mortal;

        SetBaseStats(2, 4);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnKill);
        Smite newCard = new Smite();
        newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);

        effectDef.EffectInstances.Add(newEffectInstance);
    }
}

// When an adjacent Follower is targeted by a spell: Heal 2 health
public class Asclepius : Follower
{
    public Asclepius() : base()
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

        SetBaseStats(2, 4);

        Text = "When an adjacent Follower is targeted by a spell: Heal 2 health";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        ChangePlayerHealthInstance newEffectInstance = new ChangePlayerHealthInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnTargeted);
        newEffectInstance.Init(2);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Mortal;

        SetBaseStats(2, 4);

        Text = "When an adjacent Follower dies: Add the spell Blessing to your hand";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        AddCardCopyToHandInstance newEffectInstance = new AddCardCopyToHandInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDeath);
        Blessing newCard = new Blessing();
        newCard.Costs[OfferingType.Gold] = 0;
        newEffectInstance.Init(newCard);
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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Divine;

        SetBaseStats(1, 5);

        Text = "Heal damage at end of each turn and Gain that much attack";

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


// MONSTERS
// 

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

        Type = FollowerType.Monster;

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

// Heal damage at end of each turn and Gain that much attack
public class Hydra : Follower
{
    public Hydra() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(1, 5);

        Text = "Heal damage at end of each turn and Gain that much attack";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();
    }

    public override void DoEndOfEachTurnEffects()
    {
        base.DoEndOfEachTurnEffects();

        int healthHealed = Mathf.Max(BaseHealth - CurrentHealth, 0);
        CurrentHealth += healthHealed;
        CurrentAttack += healthHealed;

        ApplyOnChanged();
    }
}

// Cleave
public class Cerberus : Follower
{
    public Cerberus() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(3, 9);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new StaticEffectDef(EffectTarget.Self, StaticEffect.Cleave));
    }
}

// OnAttacked: Deal 3 damage 
public class Siren : Follower
{
    public Siren() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(1, 3);

        Text = "";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        InnateEffects.Add(new DamageOnAttackedEffectDef(EffectTarget.Self, 3));
    }
}

// Life Siphon
public class Lamia : Follower
{
    public Lamia() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(1, 3);

        Text = "";

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
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(3, 3);

        Text = "Hiss!";

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

        Type = FollowerType.Monster;

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

        Type = FollowerType.Monster;

        SetBaseStats(5, 6);
    }
}

// Adjacent Followers have Life Siphon
public class Empusa : Follower
{
    public Empusa() : base()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(2, 4);

        Text = "Adjacent Followers have Life Siphon";

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
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(0, 5);

        Text = "When an adjacent Follower dies: Gain +2 attack";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.AdjacentAllies);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        ChangeStatsInstance newEffectInstance = new ChangeStatsInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnDeath);
        newEffectInstance.Init(2, 0, EffectTarget.EffectSource);
        effectDef.EffectInstances.Add(newEffectInstance);
    }
}


// Adjacent Followers and Typhon have OnKill: Can attack again
// 
public class Typhon : Follower
{
    public Typhon() : base()
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

        SetBaseStats(4, 6);

        Text = "Adjacent Followers and Typhon have OnKill: Can attack again";

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
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        RefreshFollowerAttackInstance newEffectInstance = new RefreshFollowerAttackInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnKill);
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
            { OfferingType.Gold, 1},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Type = FollowerType.Monster;

        SetBaseStats(4, 6);

        Text = "OnKill: Summon a Monster";

        SetupInnateEffects();
    }

    public override void SetupInnateEffects()
    {
        base.SetupInnateEffects();

        CustomEffectDef customEffectDef = new CustomEffectDef(EffectTarget.Self);
        customEffectDef.ApplyInstanceAction = CustomEffectAction;
        InnateEffects.Add(customEffectDef);
    }
    private void CustomEffectAction(FollowerEffect effectDef, Follower instanceTarget)
    {
        SummonRandomMonsterInstance newEffectInstance = new SummonRandomMonsterInstance(effectDef, instanceTarget, 0, 0, EffectTrigger.OnKill);
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

        DrawCardAction damageAction = new DrawCardAction(Owner, target, cardsDrawn);
        Owner.GameState.ActionHandler.AddAction(damageAction);
    }
}

public class Blessing : Spell
{
    private int attackGained = 1;
    private int healthGained = 1;
    public Blessing()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower +1/+1";
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
            { OfferingType.Gold, 0},
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

// Give a Follower +1 Health and Taunt
public class ShieldOfAjax : Spell
{
    private int attackGained = 0;
    private int healthGained = 1;

    public ShieldOfAjax()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "Give a Follower +1 Health and Taunt";
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
            { OfferingType.Gold, 0},
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

// Heal a Follower to full health. Gain that much health
public class Restoration : Spell
{
    private int attackGained = 0;
    private int healthGained = 0;

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

        Text = "Heal a Follower to full health. Gain that much health";
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
            int healthToHeal = targetFollower.BaseHealth - targetFollower.CurrentHealth;

            HealAction healFollowerAction = new HealAction(targetFollower, Owner, healthToHeal);
            Owner.GameState.ActionHandler.AddAction(healFollowerAction, true, true);

            HealAction healPlayerAction = new HealAction(Owner, Owner, healthToHeal);
            Owner.GameState.ActionHandler.AddAction(healPlayerAction, true, true);
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

        Text = "Deal 4 Damage";
        HasTargets = true;
    }

    public override Dictionary<OfferingType, int> GetCosts()
    {
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
    private int attackGained = 0;
    private int healthGained = 0;

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

// Unimplemented
// Deal 1 damage to all Characters for each Follower that has died this turn
public class RiverStyx : Spell
{
    public RiverStyx()
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

// If there are two or more Followers with at least 5 Attack, destroy all Followers
public class Titanomachy : Spell
{
    public Titanomachy()
    {
        Costs = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 2},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        Text = "If there are two or more Followers with at least 5 Attack, destroy all Followers";
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
            if (follower.CurrentAttack >= 5) giantFollowerCount++;
        }

        Player otherPlayer = Owner.GetOtherPlayer();

        foreach (Follower follower in otherPlayer.BattleRow.Followers)
        {
            if (follower.CurrentAttack >= 5) giantFollowerCount++;
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

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget);
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

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget);
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

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget);
        Owner.GameState.ActionHandler.AddAction(killAction);

        DrawCardAction action = new DrawCardAction(Owner, Owner, followerHealth);
        Owner.GameState.ActionHandler.AddAction(action, true);
    }
}

// Sacrifice a Follower and lose life equal to its attack to Gain half that much Gold
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

        Text = "Sacrifice a Follower and lose life equal to its attack to Gain half that much Gold";
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

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget);
        Owner.GameState.ActionHandler.AddAction(killAction);

        ChangeResourceAction action = new ChangeResourceAction(Owner, OfferingType.Gold, Mathf.CeilToInt((float)followerAttack/2));
        Owner.GameState.ActionHandler.AddAction(action, true);
    }
}


// Sacrifice a Follower and damage opponent equal to its attack
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

        Text = "Sacrifice a Follower and damage opponent equal to its attack";
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

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget);
        Owner.GameState.ActionHandler.AddAction(killAction);

        int followerAttack = followerTarget.CurrentAttack;
        ChangePlayerHealthAction payLifeAction = new ChangePlayerHealthAction(Owner.GetOtherPlayer(), Owner, -followerAttack);
        Owner.GameState.ActionHandler.AddAction(payLifeAction, true);
    }
}

// Sacrifice a Follower and Gain health equal to its health
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

        Text = "Sacrifice a Follower and Gain health equal to its health";
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

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget);
        Owner.GameState.ActionHandler.AddAction(killAction);

        int followerHealth = followerTarget.CurrentHealth;
        ChangePlayerHealthAction payLifeAction = new ChangePlayerHealthAction(Owner, Owner, followerHealth);
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

        KillFollowerAction killAction = new KillFollowerAction(Owner, followerTarget);
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


