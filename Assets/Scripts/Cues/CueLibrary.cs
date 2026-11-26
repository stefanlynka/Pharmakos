using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What a triggered ability plays when it fires.</summary>
public class CueDef
{
    /// <summary>Sound id under Resources/Audio/SFX/Cues (CuePlayer). Empty for silence.</summary>
    public string Sound;
    /// <summary>The source card flashes and pulses (TriggerFlash).</summary>
    public bool Flash = true;
    /// <summary>Multiplies the beat the cue holds the queue for.</summary>
    public float BeatScale = 1f;
    /// <summary>Damage this ability deals directly flies as this projectile (unless its action already throws one).</summary>
    public ArcProjectileStyle Projectile;
    /// <summary>Extra presentation when the cue plays (source view may be null). Returns seconds to hold the queue.</summary>
    public Func<TriggerCue, ViewFollower, float> Extra;

    public CueDef(string sound)
    {
        Sound = sound;
    }
}

/// <summary>What a spell plays when it is cast (PlaySpellAction).</summary>
public class SpellCueDef
{
    /// <summary>Cast sound id under Resources/Audio/SFX/Cues. Empty when the spell's own VFX already makes a sound.</summary>
    public string Sound;
    /// <summary>Seconds after the card reaches the play zone that the sound starts.</summary>
    public float SoundDelay;
    /// <summary>The cast's VFX, played while the card is shown (used when the spell has no GetPlayVfx of its own).</summary>
    public Func<Spell, ITarget, VfxEffect> Vfx;
    /// <summary>Damage the spell deals directly flies as this projectile from its caster (thrown while the card is shown).</summary>
    public ArcProjectileStyle Projectile;

    public SpellCueDef(string sound, float soundDelay = 0f)
    {
        Sound = sound;
        SoundDelay = soundDelay;
    }
}

/// <summary>
/// The one table of combat cues: which sound, flash, projectile or VFX every triggered ability, trinket, ritual buff
/// and spell plays. Keyed by class name (cards, effect defs) so content code stays free of presentation.
///
/// Followers' triggers resolve in this order: the effect def (keyword-like abilities any card can carry, e.g. Life
/// Siphon), then the trinket or ritual buff behind it, then the card ("Card.Trigger" before "Card"), and finally a family
/// inferred from what the ability did (damage, gold, summon, stats...). New content therefore gets a fitting cue
/// without an entry; add one here when it deserves its own. Tools → Cues → Coverage Report lists what everything
/// resolves to.
/// </summary>
public static class CueLibrary
{
    // ----------------------------------------------------------------------------------------- families ---
    // Short trigger sounds shared by everything that does the same kind of thing.
    public const string Gold = "Gold";
    public const string Life = "Life";
    public const string LifeLoss = "LifeLoss";
    public const string Siphon = "Siphon";
    public const string Empower = "Empower";
    public const string Weaken = "Weaken";
    public const string Summon = "Summon";
    public const string Draw = "Draw";
    public const string Strike = "Strike";
    public const string Snap = "Snap";
    public const string Splash = "Splash";
    public const string Flame = "Flame";
    public const string Fury = "Fury";
    public const string BloodSpatter = "BloodSpatter";
    public const string Doom = "Doom";
    public const string Maelstrom = "Maelstrom";
    public const string Petrify = "Petrify";
    public const string Mend = "Mend";
    public const string Rally = "Rally";
    public const string Swift = "Swift";
    public const string Ward = "Ward";
    public const string Revive = "Revive";
    public const string Chaos = "Chaos";
    public const string Hope = "Hope";
    public const string Fate = "Fate";
    public const string Lyre = "Lyre";
    public const string Panpipe = "Panpipe";
    public const string Boon = "Boon";
    public const string Chime = "Chime";

    // ------------------------------------------------------------------------------- follower abilities ---
    static readonly Dictionary<string, CueDef> byEffectDef = new Dictionary<string, CueDef>
    {
        { nameof(LifeSiphonEffectDef), new CueDef(Siphon) { BeatScale = 0.6f } },
    };

    static readonly Dictionary<string, CueDef> byCard = new Dictionary<string, CueDef>
    {
        // Rewards stored for next turn (no action now, so nothing to infer from)
        { nameof(OracleOfDelphi), new CueDef(Gold) },
        // Cards
        { nameof(Cassandra), new CueDef(Draw) },
        { nameof(Pan), new CueDef(Panpipe) },
        // Damage at a distance
        { nameof(Siren), new CueDef(Splash) { Projectile = StrokeProjectiles.Water } },
        { nameof(Chimera), new CueDef(Flame) { Projectile = StrokeProjectiles.Fire } },
        { nameof(Erinyes), new CueDef(Fury) { Projectile = StrokeProjectiles.Ink } },
        { nameof(GoldenHind), new CueDef(Strike) },
        { nameof(Scylla), new CueDef(Snap) },
        // Destroy
        { nameof(Helios), new CueDef(Doom) },
        { nameof(Charybdis), new CueDef(Maelstrom) },
        { nameof(Gorgon), new CueDef(Petrify) },
        // Follower healing
        { nameof(Medea), new CueDef(Mend) },
        { nameof(Minotaur), new CueDef(Mend) },
        { nameof(Hydra), new CueDef(Mend) },
        // Attack again
        { nameof(Typhon), new CueDef(Rally) },
    };

    // ---------------------------------------------------------------------- trinkets and ritual buffs ---
    // Played when the effect's icon flares (its own actions, or follower abilities it granted).
    static readonly Dictionary<string, string> byPlayerEffect = new Dictionary<string, string>
    {
        { nameof(PeltastTrumpetTrinketEffectDef), Summon },
        { nameof(GoldenFleeceTuftTrinketEffectDef), Gold },
        { nameof(LyreOfApolloTrinketEffectDef), Lyre },
        { nameof(VialOfAmbrosiaTrinketEffectDef), Life },
        { nameof(CyclopsEyeTrinketEffectDef), Empower },
        { nameof(PansFluteTrinketEffectDef), Panpipe },
        { nameof(HermesSandalsTrinketEffectDef), Swift },
        { nameof(TheAegisTrinketEffectDef), "" },          // a passive aura: flares, no sound
        { nameof(RodOfAsclepiusTrinketEffectDef), Revive },
        { nameof(HydrasScaleTrinketEffectDef), Summon },
        { nameof(TunicOfNessusTrinketEffectDef), Flame },
        { nameof(MedeasPotionTrinketEffectDef), Draw },
        { nameof(WingsOfIcarusTrinketEffectDef), Empower },
        { nameof(PandorasBoxTrinketEffectDef), Chaos },
        { nameof(PandorasHopeTrinketEffectDef), Hope },
        { nameof(TwistingCorridorsTrinketEffectDef), Summon },
        { nameof(TheEndlessWallTrinketEffectDef), Summon },
        { nameof(TheGreatHuntTrinketEffectDef), Summon },
        { nameof(CallOfTheSeaTrinketEffectDef), Draw },
        { nameof(TheLostReturnTrinketEffectDef), Revive },
        { nameof(OdysseyTrinketEffectDef), Summon },
        { nameof(CreakingOpenTrinketEffectDef), Gold },
        { nameof(StringsOfFateTrinketEffectDef), Fate },
        { nameof(AresMajorEffectDef), Rally },
        { nameof(DemeterMajorEffectDef), Life },
        { nameof(DemeterMinorEffectDef), Empower },
        { nameof(HadesMajorEffectDef), Revive },
        { nameof(HermesMajorEffectDef), Draw },
        { nameof(HeraMajorEffectDef), BloodSpatter },
        { nameof(PoseidonMajorEffectDef), Draw },
    };

    // Damage dealt by these effects flies from the effect's owner (its portrait) to the target.
    static readonly Dictionary<string, ArcProjectileStyle> projectileByPlayerEffect = new Dictionary<string, ArcProjectileStyle>
    {
        { nameof(HeraMajorEffectDef), StrokeProjectiles.Blood },
        { nameof(TunicOfNessusTrinketEffectDef), StrokeProjectiles.Fire },
    };

    // ------------------------------------------------------------------------------------------- spells ---
    static readonly Dictionary<string, SpellCueDef> bySpell = SpellCues.Build();

    static readonly HashSet<string> silentSpells = new HashSet<string> { nameof(DevKill) };

    // ------------------------------------------------------------------------------------------ resolve ---
    public static CueDef Resolve(TriggerCue cue)
    {
        if (!string.IsNullOrEmpty(cue.EffectDefName) && byEffectDef.TryGetValue(cue.EffectDefName, out CueDef byDef))
            return byDef;

        foreach (GameAction action in cue.Actions)
        {
            if (action.SourceEffect != null)
                return new CueDef(EffectSound(action.SourceEffect));
        }

        if (byCard.TryGetValue(cue.SourceName + "." + cue.Trigger, out CueDef specific))
            return specific;
        if (byCard.TryGetValue(cue.SourceName, out CueDef byName))
            return byName;

        return new CueDef(InferFamily(cue.Actions));
    }

    /// <summary>A sound for whatever an ability did, judged by its first meaningful action.</summary>
    public static string InferFamily(IList<GameAction> actions)
    {
        foreach (GameAction action in actions)
        {
            switch (action)
            {
                case KillFollowerAction _:
                case KillLowestAttackFollowersAction _:
                    return Doom;
                case DealDamageAction _:
                case DealDamageToAdjacentEnemyAction _:
                case DealDamageToAllCharactersAction _:
                    return Strike;
                case SummonFollowerAction _:
                case SummonFollowerCopyAction _:
                case SummonLastDeadFollowerAction _:
                case SummonSacrificedFollowerAction _:
                    return Summon;
                case ChangeResourceAction resource:
                    return resource.OfferingType == OfferingType.Gold ? Gold : Boon;
                case CreateOfferingAction offering:
                    return offering.Offering == OfferingType.Gold ? Gold : Boon;
                case ChangePlayerHealthAction health:
                    return health.Amount >= 0 ? Life : LifeLoss;
                case HealAction _:
                    return Mend;
                case ChangeStatsAction stats:
                    return stats.AttackChange + stats.HealthChange >= 0 ? Empower : Weaken;
                case SetStatsAction _:
                    return Petrify;
                case GiveFollowerStaticEffectAction _:
                    return Empower;
                case DrawCardAction _:
                case AddCardCopyToHandAction _:
                case StealCardAction _:
                    return Draw;
                case RefreshFollowerAttackAction _:
                    return Rally;
                case ChangeHeartStringsAction _:
                    return Hope;
            }
        }
        return Chime;
    }

    public static string EffectSound(StaticPlayerEffect effect)
    {
        if (effect == null)
            return Boon;
        return byPlayerEffect.TryGetValue(effect.GetType().Name, out string sound) ? sound : Boon;
    }

    public static SpellCueDef ForSpell(Spell spell)
    {
        if (spell == null || silentSpells.Contains(spell.GetType().Name))
            return null;
        return bySpell.TryGetValue(spell.GetType().Name, out SpellCueDef def) ? def : SpellCues.Default;
    }

    /// <summary>
    /// Last-moment presentation tweaks before an action's animations are built (view side, canon game only):
    /// damage from an ability or buff with a projectile flies as that projectile.
    /// </summary>
    public static void DecorateForView(GameAction action)
    {
        if (!(action is DealDamageAction damage) || damage.ProjectileStyle != null && damage.ProjectileStyle != ArcProjectileStyle.Stone)
            return;

        ArcProjectileStyle style = null;
        if (action.Cue != null)
            style = action.Cue.Def.Projectile;
        else if (action.SourceEffect != null)
            projectileByPlayerEffect.TryGetValue(action.SourceEffect.GetType().Name, out style);

        if (style == null)
            return;

        damage.ProjectileStyle = style;
        damage.PlayProjectileWithDamage = true;
    }

    /// <summary>
    /// Adds animations an action needs for its cue (view side, canon game only): life loss dealt by an ability with a
    /// projectile (Erinyes) flies from the follower to the player's portrait, landing as the loss shows.
    /// </summary>
    public static void DecorateAnimations(GameAction action, List<AnimationAction> animations)
    {
        if (!(action is ChangePlayerHealthAction health) || health.Amount >= 0 || action.Cue == null)
            return;

        ArcProjectileStyle style = action.Cue.Def.Projectile;
        if (style == null || action.Cue.Source == null || health.Target == null || health.Target == action.Cue.Source.Owner)
            return;

        ArcProjectileAnimation shot = new ArcProjectileAnimation(action, style, action.Cue.Source, health.Target, pullDamageOnImpact: true);
        int index = animations.FindIndex(a => a is ChangeStatsAnimation);
        animations.Insert(index < 0 ? 0 : index, shot);
    }

    // ------------------------------------------------------------------------------ trinkets and buffs ---
    static readonly Dictionary<StaticPlayerEffect, float> lastEffectSound = new Dictionary<StaticPlayerEffect, float>();
    const float EffectSoundRepeatGap = 0.3f;

    /// <summary>A trinket or ritual buff's icon flared: play its sound (once per burst of actions).</summary>
    public static void PlayEffectCue(StaticPlayerEffect effect)
    {
        if (effect == null)
            return;

        float now = Time.unscaledTime;
        if (lastEffectSound.TryGetValue(effect, out float last) && now - last < EffectSoundRepeatGap)
            return;
        lastEffectSound[effect] = now;
        if (lastEffectSound.Count > 64)
            lastEffectSound.Clear();

        string sound = EffectSound(effect);
        if (!string.IsNullOrEmpty(sound))
            CuePlayer.Play(sound);
    }

    // ------------------------------------------------------------------------------------- coverage ---
    public static bool HasCardEntry(string cardName, out CueDef def)
    {
        return byCard.TryGetValue(cardName, out def);
    }

    public static bool HasEffectDefEntry(string defName, out CueDef def)
    {
        return byEffectDef.TryGetValue(defName, out def);
    }

    public static bool HasPlayerEffectEntry(string effectName, out string sound)
    {
        return byPlayerEffect.TryGetValue(effectName, out sound);
    }

    public static bool HasSpellEntry(string spellName, out SpellCueDef def)
    {
        return bySpell.TryGetValue(spellName, out def);
    }

    public static IEnumerable<string> AllSoundIds()
    {
        HashSet<string> ids = new HashSet<string>();
        foreach (CueDef def in byEffectDef.Values) ids.Add(def.Sound);
        foreach (CueDef def in byCard.Values) ids.Add(def.Sound);
        foreach (string sound in byPlayerEffect.Values) ids.Add(sound);
        foreach (SpellCueDef def in bySpell.Values) ids.Add(def.Sound);
        ids.Add(SpellCues.Default.Sound);
        foreach (string family in new[] { Gold, Life, LifeLoss, Siphon, Empower, Weaken, Summon, Draw, Strike, Doom, Mend,
                     Petrify, Rally, Hope, Boon, Chime })
            ids.Add(family);
        ids.Remove(null);
        ids.Remove("");
        return ids;
    }
}
