#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Test chamber benches for the combat cues (trigger flashes, cue sounds, spell casts, projectiles). Dev builds only.
/// In the test chamber, L / K load the next / previous setup: the fight is rebuilt with these rows and hands, both
/// players at 60 health and every offering (gold included) at 10. The player always has a Chariot in the middle of
/// their row, so followers played next to it can attack at once (the setup's own followers are ready too).
/// [ / ] turn every cue and cast sound down / up (AudioHandler.CueVolume and SpellCastVolume, play mode only; copy the
/// values shown on screen into the AudioHandler inspector to keep them).
/// Add a setup by adding an entry to Setups. Card, trinket and buff classes are referenced by type so typos don't compile.
/// </summary>
public static class CueTestSetups
{
    public class Setup
    {
        public string Name;
        public string Hint;
        public Type[] HumanRow = new Type[0];
        public Type[] AiRow = new Type[0];
        public Type[] HumanHand = new Type[0];
        public Type[] AiHand = new Type[0];
        public Type[] HumanTrinkets = new Type[0];
        /// <summary>Lasting buffs (ritual effects) for the human, given before the turn starts.</summary>
        public Func<Player, Player, PlayerEffect[]> HumanBuffs;
        /// <summary>Runs once the turn has started (e.g. the AI casting a spell).</summary>
        public Action<Player, Player> AfterStart;
    }

    const int Health = 60;
    const int Offerings = 10;

    static Type[] T(params Type[] types) => types;

    public static readonly Setup[] Setups =
    {
        new Setup
        {
            Name = "Deaths and gold",
            Hint = "Lightning the enemy row: Sheep (own On Death gold), Charon (gold when a neighbour dies), Keres (+2 when a neighbour dies), Trojan Horse and Phantasma (summon on death). Attack with Pyrrhus: kill gold, Typhon lets it attack again.",
            HumanRow = T(typeof(Pyrrhus), typeof(Typhon), typeof(Hoplite)),
            HumanHand = T(typeof(Lightning), typeof(Smite), typeof(ThrowStone), typeof(Vengeance), typeof(Haunt)),
            AiRow = T(typeof(Sheep), typeof(Charon), typeof(Sheep), typeof(Keres), typeof(TrojanHorse), typeof(Phantasma)),
        },
        new Setup
        {
            Name = "Reactions and projectiles",
            Hint = "Attack Siren (water), Chimera (fire) and Menelaus (summons). Kill anything: Erinyes throws ink at you. Rat weakens what it hits; Agamemnon summons when it attacks; Pan and Odysseus draw. End turn: Gorgon turns your attackers to stone.",
            HumanRow = T(typeof(Rat), typeof(Agamemnon), typeof(Hoplite), typeof(Pan), typeof(Odysseus)),
            HumanHand = T(typeof(FirstFlame), typeof(Haunt), typeof(Smite), typeof(Diomedes)),
            AiRow = T(typeof(Siren), typeof(Chimera), typeof(Menelaus), typeof(Erinyes), typeof(Gorgon)),
        },
        new Setup
        {
            Name = "Allies",
            Hint = "Attack with Maenad's and Sarpedon's neighbours (buffs), Lamia (Life Siphon). Play Hippolyta (On Enter buff), Shade (draw), Endymion (summons), Calchas or Medea next to an attacker that kills. Melpomene adds Blessing when a neighbour dies. Prey costs its owner life when it dies.",
            HumanRow = T(typeof(Maenad), typeof(Sarpedon), typeof(Lamia), typeof(Melpomene)),
            HumanHand = T(typeof(Hippolyta), typeof(Shade), typeof(Endymion), typeof(Calchas), typeof(Medea)),
            AiRow = T(typeof(Peltast), typeof(Prey1), typeof(Peltast), typeof(Hoplite)),
        },
        new Setup
        {
            Name = "Turn effects and delayed rewards",
            Hint = "Play followers next to Oracle (gold next turn) and Cassandra (card next turn). Smite the Hydra. End turn: Helios, Scylla, Charybdis, Hydra and Minotaur act at the start and end of the enemy's turn; Oracle's gold flies in on yours.",
            HumanRow = T(typeof(Hoplite), typeof(OracleOfDelphi), typeof(Cassandra), typeof(Hoplite)),
            HumanHand = T(typeof(Peltast), typeof(Hoplite), typeof(Smite)),
            AiRow = T(typeof(Scylla), typeof(Hydra), typeof(Hoplite), typeof(Minotaur), typeof(Hoplite), typeof(Charybdis), typeof(Helios)),
        },
        new Setup
        {
            Name = "Spells: blessings and curses",
            Hint = "Every buff and debuff spell. Target Nereid (grows when you cast) or Asclepius' neighbours (you gain life).",
            HumanRow = T(typeof(Nereid), typeof(Asclepius), typeof(Hoplite)),
            HumanHand = T(typeof(Blessing), typeof(Contraption), typeof(Confusion), typeof(Talaria), typeof(HarpeOfPerseus),
                typeof(ShieldOfAjax), typeof(StygianPact), typeof(Reverie), typeof(Restoration), typeof(Reflection)),
            AiRow = T(typeof(Hoplite), typeof(Hoplite)),
        },
        new Setup
        {
            Name = "Spells: cards, summons and offerings",
            Hint = "Every card-draw, summoning and offering spell.",
            HumanRow = T(typeof(Hoplite)),
            HumanHand = T(typeof(Scry), typeof(Herald), typeof(PropheticDraught), typeof(FinalRites), typeof(LastingGift),
                typeof(MacabreFestivities), typeof(DragonTeeth), typeof(ReleasePrey), typeof(CreateFilth), typeof(EyesInTheDark)),
            AiRow = T(typeof(Hoplite)),
        },
        new Setup
        {
            Name = "Spells: wrath",
            Hint = "Damage and destruction spells, plus Gnaw, Roar and Panic.",
            HumanRow = T(typeof(Hoplite), typeof(Hoplite)),
            HumanHand = T(typeof(Drown), typeof(RiverStyx), typeof(Titanomachy), typeof(Gnaw), typeof(Roar), typeof(Panic),
                typeof(Lightning), typeof(Vengeance), typeof(FirstFlame), typeof(Haunt)),
            AiRow = T(typeof(Cyclops), typeof(Hoplite), typeof(Agamemnon), typeof(Menelaus)),
        },
        new Setup
        {
            Name = "The Price Of...",
            Hint = "All seven sacrifices (heartbeat, then each one's own coda). Tunic of Nessus burns a random enemy and Medea's Potion copies the sacrifice to your hand.",
            HumanRow = T(typeof(Hoplite), typeof(Peltast), typeof(Sheep), typeof(Satyr), typeof(Agamemnon), typeof(Hoplite)),
            HumanHand = T(typeof(PriceOfProfit), typeof(PriceOfKnowledge), typeof(PriceOfInspiration), typeof(PriceOfWealth),
                typeof(PriceOfReprisal), typeof(PriceOfRenewal), typeof(PriceOfLegacy)),
            HumanTrinkets = T(typeof(TunicOfNessusTrinket), typeof(MedeasPotionTrinket)),
            AiRow = T(typeof(Hoplite), typeof(Hoplite), typeof(Hoplite)),
        },
        new Setup
        {
            Name = "Boss spells",
            Hint = "Cast Roar, Gnaw and Panic yourself, then end turn: the enemy has them too and casts them at you.",
            HumanRow = T(typeof(Hoplite), typeof(Hoplite), typeof(Hoplite)),
            HumanHand = T(typeof(Roar), typeof(Gnaw), typeof(Panic)),
            AiRow = T(typeof(Hoplite), typeof(Hoplite), typeof(Hoplite)),
            AiHand = T(typeof(Roar), typeof(Gnaw), typeof(Panic)),
        },
        new Setup
        {
            Name = "Twist of Fate",
            Hint = "The enemy casts Twist of Fate as this loads. Press K then L to see it again.",
            HumanRow = T(typeof(Hoplite), typeof(Hoplite)),
            AiRow = T(typeof(Hoplite), typeof(Hoplite)),
            AfterStart = CastTwistOfFate,
        },
        new Setup
        {
            Name = "Trinkets and ritual buffs",
            Hint = "Use a ritual (free here): Golden Fleece gold, Lyre draws, Vial heals, Trumpet summons. Reverie changes your life: Hera's blood. Attack straight ahead: Cyclops' Eye. Play a follower: Hermes' Sandals, Demeter. Play Cyclops (5+): Hydra's Scale. Discard a card: Strings of Fate. Sacrifice: Poseidon's Drown. Creaking Open and Pandora's Box (a random spell) act at the start of each of your turns, including as this loads.",
            HumanRow = T(typeof(Hoplite), typeof(Hoplite)),
            HumanHand = T(typeof(Reverie), typeof(Hoplite), typeof(Cyclops), typeof(PriceOfProfit), typeof(Herald)),
            HumanTrinkets = T(typeof(GoldenFleeceTuftTrinket), typeof(LyreOfApolloTrinket), typeof(VialOfAmbrosiaTrinket),
                typeof(PeltastTrumpetTrinket), typeof(CyclopsEyeTrinket), typeof(HermesSandalsTrinket), typeof(HydrasScaleTrinket),
                typeof(CreakingOpenTrinket), typeof(PandorasBoxTrinket), typeof(StringsOfFateTrinket)),
            HumanBuffs = (human, ai) => new PlayerEffect[]
            {
                new HeraMajorEffectDef(human, human),
                new DemeterMajorEffectDef(human, human, 0, 0),
                new HermesMajorEffectDef(human, human),
                new PoseidonMajorEffectDef(human, human),
            },
            AiRow = T(typeof(Hoplite), typeof(Hoplite), typeof(Hoplite)),
        },
    };

    /// <summary>The setup the next test chamber fight loads instead of the deck's own start (-1: none yet).</summary>
    public static int Current = -1;
    public static Setup Active => Current >= 0 && Current < Setups.Length ? Setups[Current] : null;

    public static void Step(int step)
    {
        Current = Current < 0
            ? (step > 0 ? 0 : Setups.Length - 1)
            : ((Current + step) % Setups.Length + Setups.Length) % Setups.Length;
    }

    /// <summary>Fills the rows, hands, trinkets and buffs. Called by Controller.LoadLevel before the turn starts.</summary>
    public static void Apply(Player human, Player ai)
    {
        Setup setup = Active;
        if (setup == null)
            return;

        foreach (Player player in new[] { human, ai })
            player.Health = Health;

        var humanDelayed = new HashSet<DelayedGameAction>(human.StartOfTurnActions);
        var aiDelayed = new HashSet<DelayedGameAction>(ai.StartOfTurnActions);

        List<Type> row = new List<Type>(setup.HumanRow);
        row.Insert(row.Count / 2, typeof(Chariot));
        Summon(human, row);
        Summon(ai, new List<Type>(setup.AiRow));

        AddToHand(human, setup.HumanHand);
        AddToHand(ai, setup.AiHand);

        // Staging the rows sets off "when a follower enters" rewards (Oracle, Cassandra); don't pay them out at load.
        human.StartOfTurnActions.RemoveAll(action => !humanDelayed.Contains(action));
        ai.StartOfTurnActions.RemoveAll(action => !aiDelayed.Contains(action));

        foreach (Type type in setup.HumanTrinkets)
            ((Trinket)Activator.CreateInstance(type)).ApplyEffect(human);
        if (setup.HumanBuffs != null)
        {
            foreach (PlayerEffect effect in setup.HumanBuffs(human, ai))
                human.AddPlayerEffect(effect);
        }
    }

    /// <summary>After the turn has started: offerings and health shown, then anything the setup does on its own.</summary>
    public static void AfterStart(Player human, Player ai)
    {
        Setup setup = Active;
        if (setup == null)
            return;

        foreach (Player player in new[] { human, ai })
        {
            foreach (OfferingType type in new[] { OfferingType.Gold, OfferingType.Blood, OfferingType.Bone, OfferingType.Crop, OfferingType.Scroll })
                player.Offerings[type] = Offerings;

            ViewPlayer view = View.Instance.GetViewPlayer(player);
            if (view == null)
                continue;
            view.SetHealth(Health);
            if (view.ViewResources != null)
                view.ViewResources.SyncToPlayerState();
        }

        setup.AfterStart?.Invoke(human, ai);
        Debug.Log("Cue setup " + (Current + 1) + "/" + Setups.Length + ": " + setup.Name + ". " + setup.Hint);
    }

    static void Summon(Player owner, List<Type> types)
    {
        foreach (Type type in types)
        {
            if (owner.BattleRow.Followers.Count >= Player.MaxFollowerCount)
                break;
            Follower follower = (Follower)Activator.CreateInstance(type);
            follower.Init(owner);
            owner.GameState.ActionHandler.AddAction(new SummonFollowerAction(follower, owner.BattleRow.Followers.Count, false));
            // Ready to attack, like a fight's starting row.
            follower.PlayedThisTurn = false;
        }
    }

    static void AddToHand(Player owner, Type[] types)
    {
        foreach (Type type in types)
        {
            Card card = (Card)Activator.CreateInstance(type);
            card.Init(owner);
            owner.GameState.ActionHandler.AddAction(new AddCardCopyToHandAction(card));
        }
    }

    static void CastTwistOfFate(Player human, Player ai)
    {
        ai.TwistOfFateBuffs.Clear();
        ai.TwistOfFateBuffs.Add(new CyclopsEyeTrinket());
        ai.TwistOfFateBuffs.Add(new PansFluteTrinket());
        ai.TwistOfFateBuffs.Add(new CreakingOpenTrinket());

        TwistOfFate twist = new TwistOfFate();
        twist.Init(ai);
        ai.Hand.Add(twist);
        ai.TryPlaySpell(twist, ai);
    }

    // ------------------------------------------------------------------------------------------- volume ---
    const float VolumeStep = 0.8f;

    /// <summary>Scales AudioHandler.CueVolume and SpellCastVolume together (step &gt; 0 louder).</summary>
    public static void StepVolume(int step)
    {
        AudioHandler audio = View.Instance != null ? View.Instance.AudioHandler : null;
        if (audio == null)
            return;

        float factor = step > 0 ? 1f / VolumeStep : VolumeStep;
        audio.CueVolume = Mathf.Clamp(audio.CueVolume * factor, 0.02f, 2f);
        audio.SpellCastVolume = Mathf.Clamp(audio.SpellCastVolume * factor, 0.02f, 2f);
        CuePlayer.Play(CueLibrary.Gold);
        Debug.Log("Cue volume " + audio.CueVolume.ToString("0.00") + ", spell cast volume " + audio.SpellCastVolume.ToString("0.00"));
    }

    /// <summary>The on-screen label in the test chamber: current setup, its hint, keys and volumes.</summary>
    public static void DrawLabel()
    {
        AudioHandler audio = View.Instance != null ? View.Instance.AudioHandler : null;
        Setup setup = Active;
        string title = setup != null
            ? "Cue setup " + (Current + 1) + "/" + Setups.Length + ": " + setup.Name
            : "Cue setups: press L (next) or K (previous)";
        string text = title
            + (setup != null ? "\n" + setup.Hint : "")
            + "\nL / K next / previous setup    [ / ] cue volume"
            + (audio != null ? "    cues " + audio.CueVolume.ToString("0.00") + ", casts " + audio.SpellCastVolume.ToString("0.00") : "");

        GUIStyle style = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 15 };
        style.normal.textColor = new Color(0.91f, 0.86f, 0.76f);
        Rect area = new Rect(12, 8, Mathf.Min(760, UnityEngine.Screen.width - 24), 110);
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(area.x - 6, area.y - 4, area.width + 12, style.CalcHeight(new GUIContent(text), area.width) + 8), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(area, text, style);
    }
}
#endif
