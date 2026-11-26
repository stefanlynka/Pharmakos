using System.Collections.Generic;

/// <summary>
/// Every spell's cast cue: its sound (Resources/Audio/SFX/Cues/Cast*.wav), and for some a VFX or a projectile thrown
/// from the caster. A spell's own GetPlayVfx / GetThrowStyle (Smite, Lightning, Throw Stone) win over these, and spells
/// whose own VFX already makes a sound have no cast sound here. Spells without an entry play Default.
/// </summary>
public static class SpellCues
{
    public static readonly SpellCueDef Default = new SpellCueDef("CastSpell");

    const string Blood = "Images/Particles/Blood";
    const string Gold = "Images/Particles/Gold";
    const string Ink = "Images/Particles/Ink";
    const string Water = "Images/Particles/Water";

    public static Dictionary<string, SpellCueDef> Build()
    {
        Dictionary<string, SpellCueDef> cues = new Dictionary<string, SpellCueDef>
        {
            // Own VFX with sound already
            { nameof(Smite), new SpellCueDef("") },
            { nameof(Lightning), new SpellCueDef("") },
            { nameof(ThrowStone), new SpellCueDef("") },

            // Summons (their followers' entrances are animated)
            { nameof(DragonTeeth), new SpellCueDef("CastEarth") },
            { nameof(ReleasePrey), new SpellCueDef("CastHunt") },
            { nameof(CreateFilth), new SpellCueDef("CastFilth") },
            { nameof(EyesInTheDark), new SpellCueDef("CastEyes") },
            { nameof(MacabreFestivities), new SpellCueDef("CastMacabre") },

            // Damage
            { nameof(Haunt), new SpellCueDef("CastHaunt") { Projectile = StrokeProjectiles.Ink } },
            { nameof(FirstFlame), new SpellCueDef("CastFlame") { Projectile = StrokeProjectiles.Fire } },
            { nameof(Vengeance), new SpellCueDef("CastVengeance") { Projectile = StrokeProjectiles.Blood } },
            { nameof(RiverStyx), new SpellCueDef("CastStyx") },
            { nameof(Titanomachy), new SpellCueDef("CastTitans") },
            { nameof(Drown), new SpellCueDef("CastDrown") { Vfx = (spell, target) => DrownFlurry() } },
            { nameof(Gnaw), new SpellCueDef("CastGnaw") { Vfx = (spell, target) => new BiteVfx() } },

            // Buffs and debuffs (their stat changes are animated)
            { nameof(Blessing), new SpellCueDef("CastBless") { Vfx = (spell, target) => Burst(Gold, 7, 0.8f) } },
            { nameof(Contraption), new SpellCueDef("CastGears") },
            { nameof(Confusion), new SpellCueDef("CastConfuse") { Vfx = (spell, target) => Burst(Ink, 7, 0.75f) } },
            { nameof(Talaria), new SpellCueDef("CastWings") },
            { nameof(HarpeOfPerseus), new SpellCueDef("CastBlade") },
            { nameof(ShieldOfAjax), new SpellCueDef("CastShield") },
            { nameof(StygianPact), new SpellCueDef("CastPact") { Vfx = (spell, target) => Burst(Blood, 7, 0.8f) } },
            { nameof(Reverie), new SpellCueDef("CastDream") },
            { nameof(Restoration), new SpellCueDef("CastRestore") },
            { nameof(Reflection), new SpellCueDef("CastMirror") },
            { nameof(Roar), new SpellCueDef("CastRoar") { Vfx = (spell, target) => new RoarVfx { Caster = spell.Owner } } },
            { nameof(Panic), new SpellCueDef("CastPanic") { Vfx = (spell, target) => new FearVfx() } },

            // Cards and offerings
            { nameof(Scry), new SpellCueDef("CastScry") },
            { nameof(Herald), new SpellCueDef("CastHerald") },
            { nameof(PropheticDraught), new SpellCueDef("CastDraught") },
            { nameof(FinalRites), new SpellCueDef("CastRites") },
            { nameof(LastingGift), new SpellCueDef("CastHarvest") },

            // The Price Of... sacrifices: a heartbeat under every one, a different coda each
            { nameof(PriceOfProfit), Price("CastPriceProfit") },
            { nameof(PriceOfKnowledge), Price("CastPriceKnowledge") },
            { nameof(PriceOfInspiration), Price("CastPriceInspiration") },
            { nameof(PriceOfWealth), Price("CastPriceWealth") },
            { nameof(PriceOfReprisal), Price("CastPriceReprisal") },
            { nameof(PriceOfRenewal), Price("CastPriceRenewal") },
            { nameof(PriceOfLegacy), Price("CastPriceLegacy") },

            // The gods favour the foe
            { nameof(TwistOfFate), new SpellCueDef("CastFate") { Vfx = (spell, target) => new TwistOfFateVfx() } },
        };
        return cues;
    }

    static SpellCueDef Price(string sound)
    {
        return new SpellCueDef(sound) { Vfx = (spell, target) => new HeartbeatVfx() };
    }

    static StrokeBurstVfx Burst(string folder, int count, float scale)
    {
        return new StrokeBurstVfx { Folder = folder, Count = count, Scale = scale, Delay = 0.05f };
    }

    static VfxEffect DrownFlurry()
    {
        BrushStrokeFlurryVfx flood = VfxLibrary.WaterFlood();
        flood.SweepDirection = UnityEngine.Vector2.down;
        flood.SweepDuration = 0.35f;
        flood.SweepOutDelay = 0.55f;
        flood.PeakRate = 120f;
        flood.ScaleMin = 0.5f;
        flood.ScaleMax = 0.8f;
        flood.HoldDuration = 0.2f;
        flood.Duration = flood.SweepOutDelay + flood.SweepDuration;
        flood.QueueHold = 0.6f;
        flood.SortingOrder = 400;
        return flood;
    }
}
