using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialTrigger
{
    OverworldShown,
    EncounterBegan,
    CombatTurnStarted,
    TempleBatchShown,
    RitualRewardsShown,
    ShopOpened,
    HandDrawn,
    FollowerPlayed,
    FollowerCanAttack,
    AttackResolved,
    RitualReady,
    EndTurnHovered,
}

public enum TutorialFocus
{
    None,
    Gold,
    Hand,
    PlayerFollowers,
    EnemyPlayer,
    PlayerHealth,
    Offerings,
    Blood,
    Bone,
    Crop,
    Scroll,
    Rituals,
    ReadyRitual,
    EndTurn,
}

public class TutorialSpriteCue
{
    public string ResourcePath;
    public Vector2 From = new Vector2(0.5f, 0.55f);
    public Vector2 To = new Vector2(0.5f, 0.42f);
    // Degrees from the authored arrow, which faces right. -90 points down.
    public float Rotation = -90f;
    public float Start;
    public float Duration;
    // From/To are the normalized anchor min and max, and the arrow does not bob.
    public bool Fill;
    // Rest position is pushed out so the bob stays outside the spotlight hole.
    public bool StayOutsideHole;
}

/// <summary>
/// One timed tutorial prompt. <see cref="Hole"/> is a normalized screen rect (x, y, width, height).
/// <see cref="Focus"/> replaces that rect with the live object when it can be found.
/// Set <see cref="ShowBlackBlocker"/> to cover the screen in black and block input.
/// </summary>
public class TutorialBeat
{
    public string Id;
    public string RequiresId;
    public TutorialTrigger Trigger;
    public EncounterType Encounter = EncounterType.None;
    public int Arg = -1;
    public float Delay;
    public float Duration = 4f;
    public bool BlockInput = true;
    public string Text;
    public Vector2 TextAnchor = new Vector2(0.5f, 0.78f);
    public float SecondsPerCharacter = 0.03f;
    public bool ShowCover = true;
    public bool ShowBlackBlocker;
    public float CoverAlpha = 0.72f;
    public TutorialFocus Focus;
    public bool PointAtHole;
    public Rect Hole;
    public TutorialSpriteCue[] Sprites;
    public bool Played;
}

/// <summary>
/// Tutorial prologue content. <see cref="TutorialCombatScript"/> forces the draws and enemy plays these beats assume.
/// </summary>
public static class TutorialScript
{
    public const string OfferingsBeatId = "Offerings";

    public static readonly List<TutorialBeat> Beats = new List<TutorialBeat>
    {
        // Welcome message
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Duration = -1f,
            Text = "Behold the road to the Underworld.\nClick the Combat Hex ahead of you to begin.",
            TextAnchor = new Vector2(0.5f, 0.9f),
        },
        // First combat
        new TutorialBeat
        {
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat1,
            Duration = -1,
            Text = "This is your gold, which you spend to play cards.\nIt refills each turn.",
            TextAnchor = new Vector2(0.38f, 0.5f),
            Focus = TutorialFocus.Gold,
            PointAtHole = true,
            Hole = new Rect(0.01f, 0.02f, 0.1f, 0.18f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat1,
            Duration = -1,
            Text = "The number on the top left of a card is its gold cost.\nDrag a Follower (A green card) onto your Battle Row to play it.",
            TextAnchor = new Vector2(0.5f, 0.5f),
            Focus = TutorialFocus.Hand,
            PointAtHole = true,
            Hole = new Rect(0.2f, 0.02f, 0.58f, 0.24f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.FollowerPlayed,
            Encounter = EncounterType.TutorialCombat1,
            Arg = 0,
            Delay = 0.55f,
            Duration = -1f,
            Text = "You played a Follower!\nThe number in the bottom left is it's attack.\nThe number in the bottom right is it's health.\nFollowers can't attack the turn they're played.",
            TextAnchor = new Vector2(0.5f, 0.82f),
            Focus = TutorialFocus.PlayerFollowers,
            PointAtHole = true,
            Hole = new Rect(0.3f, 0.28f, 0.4f, 0.2f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.FollowerPlayed,
            Encounter = EncounterType.TutorialCombat1,
            Arg = 1,
            Delay = 0.55f,
            Duration = -1,
            Text = "Some Followers give abilities to their neighbours.\nPositioning matters!\nSprint lets Followers attack the turn they're played.",
            TextAnchor = new Vector2(0.5f, 0.2f),
            Focus = TutorialFocus.PlayerFollowers,
            PointAtHole = true,
            Hole = new Rect(0.28f, 0.28f, 0.44f, 0.2f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.FollowerCanAttack,
            Encounter = EncounterType.TutorialCombat1,
            Arg = 0,
            Delay = 0.35f,
            Duration = -1f,
            Text = "When a Follower has a glowing border, it means it can attack!\nClick it, or click and drag it onto a target. \nNothing is in the way, so you can attack the enemy player directly!",
            TextAnchor = new Vector2(0.5f, 0.2f),
            Focus = TutorialFocus.PlayerFollowers,
            PointAtHole = true,
            Hole = new Rect(0.28f, 0.28f, 0.44f, 0.2f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.AttackResolved,
            Encounter = EncounterType.TutorialCombat1,
            Delay = 0.9f,
            Duration = -1f,
            Text = "Get the enemy's health to 0 to win.",
            TextAnchor = new Vector2(0.5f, 0.2f),
            Focus = TutorialFocus.EnemyPlayer,
            PointAtHole = true,
            Hole = new Rect(0.42f, 0.74f, 0.16f, 0.18f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat1,
            Arg = 2,
            Delay = 0.7f,
            Duration = -1f,
            Text = "The enemy played followers. Mouse over yours to see who each one can attack.",
            TextAnchor = new Vector2(0.5f, 0.2f),
            Focus = TutorialFocus.PlayerFollowers,
            PointAtHole = true,
            Hole = new Rect(0.22f, 0.28f, 0.56f, 0.2f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat1,
            Arg = 2,
            Duration = -1f,
            Text = "A follower can only attack nearby enemies. Taunt has to be attacked first.",
            TextAnchor = new Vector2(0.5f, 0.93f),
            ShowCover = false,
            ShowBlackBlocker = true,
            Sprites = AttackRangeImages(),
        },

        // Temple
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.TutorialCombat1,
            Duration = -1f,
            ShowCover = false,
            Text = "The next location is a temple. There you will be asked to sacrifice some of your spells and followers, and have them removed from your deck.",
            TextAnchor = new Vector2(0.5f, 0.82f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EncounterBegan,
            Encounter = EncounterType.TutorialTemple,
            Duration = -1f,
            ShowCover = false,
            Text = "You must choose one of these three cards to sacrifice. Refuse, and you will anger the gods and face a difficult fight. I do not recommend it in the tutorial.",
            TextAnchor = new Vector2(0.5f, 0.84f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.RitualRewardsShown,
            Duration = -1f,
            ShowCover = false,
            Text = "As a reward for sacrificing to the gods, you may adopt new Rituals. You may have up to two rituals at a time.",
            TextAnchor = new Vector2(0.5f, 0.84f),
        },

        // Second combat
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 1,
            Delay = 1.6f,
            Duration = -1f,
            ShowCover = false,
            Text = "Now that you have rituals, you just need something to sacrifice.",
            TextAnchor = new Vector2(0.5f, 0.58f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 1,
            Duration = -1f,
            Text = "Every action you take produces Offerings.",
            TextAnchor = new Vector2(0.42f, 0.36f),
            Focus = TutorialFocus.Offerings,
            PointAtHole = true,
            Hole = new Rect(0.07f, 0.02f, 0.16f, 0.16f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 1,
            Duration = -1f,
            Text = "Summoning a Follower creates a Crop.",
            TextAnchor = new Vector2(0.42f, 0.32f),
            Focus = TutorialFocus.Crop,
            PointAtHole = true,
            Hole = new Rect(0.08f, 0.02f, 0.07f, 0.07f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 1,
            Duration = -1f,
            Text = "Playing a Spell creates a Scroll.",
            TextAnchor = new Vector2(0.42f, 0.32f),
            Focus = TutorialFocus.Scroll,
            PointAtHole = true,
            Hole = new Rect(0.15f, 0.02f, 0.07f, 0.07f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 1,
            Duration = -1f,
            Text = "When a Follower dies on your turn, yours or your opponent's, you gain a Bone.",
            TextAnchor = new Vector2(0.42f, 0.36f),
            Focus = TutorialFocus.Bone,
            PointAtHole = true,
            Hole = new Rect(0.15f, 0.09f, 0.07f, 0.07f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 1,
            Duration = -1f,
            Text = "When a player's life changes on your turn, yours or your opponent's, you gain that much Blood.",
            TextAnchor = new Vector2(0.42f, 0.36f),
            Focus = TutorialFocus.Blood,
            PointAtHole = true,
            Hole = new Rect(0.08f, 0.09f, 0.07f, 0.07f),
        },
        new TutorialBeat
        {
            Id = OfferingsBeatId,
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 1,
            Duration = -1f,
            Text = "Generate at least one of each and you'll be able to call upon the might of Zeus.",
            TextAnchor = new Vector2(0.46f, 0.4f),
            Focus = TutorialFocus.Rituals,
            PointAtHole = true,
            Hole = new Rect(0.02f, 0.25f, 0.14f, 0.4f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.RitualReady,
            Encounter = EncounterType.TutorialCombat2,
            Delay = 2f,
            Duration = -1f,
            Text = "You've acquired enough Offerings. Click and drag the ritual onto your target to spend them and use its ability.",
            TextAnchor = new Vector2(0.46f, 0.42f),
            Focus = TutorialFocus.ReadyRitual,
            PointAtHole = true,
            Hole = new Rect(0.02f, 0.3f, 0.14f, 0.22f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EndTurnHovered,
            RequiresId = OfferingsBeatId,
            Duration = -1f,
            Text = "Offerings disappear at the end of your turn. The more expensive a ritual, the more carefully you'll have to plan your turn.",
            TextAnchor = new Vector2(0.62f, 0.28f),
            Focus = TutorialFocus.EndTurn,
            PointAtHole = true,
            Hole = new Rect(0.82f, 0.02f, 0.16f, 0.1f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 3,
            Delay = 1.1f,
            Duration = -1f,
            Text = "Your enemy just damaged you. If your health drops to 0, you'll lose a heartstring and their turn will end. Run out of health and heartstrings, and it's game over.",
            TextAnchor = new Vector2(0.5f, 0.42f),
            Focus = TutorialFocus.PlayerHealth,
            PointAtHole = true,
            Hole = new Rect(0.42f, 0.12f, 0.16f, 0.16f),
        },

        // Shop and the road ahead
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.TutorialCombat2,
            Duration = -1f,
            ShowCover = false,
            Text = "Up next is a shop. There you can spend your heartstrings on cards and trinkets. The main way to get more heartstrings is from event nodes.",
            TextAnchor = new Vector2(0.5f, 0.82f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.ShopOpened,
            Duration = -1f,
            ShowCover = false,
            Text = "Heartstrings are rare and valuable. Spend them well.",
            TextAnchor = new Vector2(0.5f, 0.84f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.TutorialMarket,
            Duration = -1f,
            ShowCover = false,
            Text = "Those are the basics. Near the end of this demo are two bosses. Each one grants a powerful ability once defeated, which you can use on future runs. Past those bosses is the River Styx, and the final boss.",
            TextAnchor = new Vector2(0.5f, 0.82f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.Styx,
            Duration = -1f,
            ShowCover = false,
            Text = "Congratulations on defeating a boss. To reach the final boss, you must pass the River Styx, which comes with a twist.",
            TextAnchor = new Vector2(0.5f, 0.82f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.Styx,
            Duration = -1f,
            ShowCover = false,
            Text = "The River Styx replaces every card in your deck with the cards you've sacrificed this run. The same is true of trinkets and rituals. You can track what you've sacrificed in the status screen.",
            TextAnchor = new Vector2(0.5f, 0.82f),
        },
    };

    static TutorialSpriteCue[] AttackRangeImages()
    {
        string[] pages = { "7", "7_5", "9", "9_5", "10" };
        const float page = 2.6f;
        var cues = new TutorialSpriteCue[pages.Length];
        for (int i = 0; i < pages.Length; i++)
        {
            cues[i] = new TutorialSpriteCue
            {
                ResourcePath = "Images/Tutorial/" + pages[i],
                From = new Vector2(0.18f, 0.08f),
                To = new Vector2(0.82f, 0.88f),
                Rotation = 0f,
                Start = i * page,
                Duration = page,
                Fill = true,
            };
        }

        return cues;
    }

    public static void Reset()
    {
        for (int i = 0; i < Beats.Count; i++)
            Beats[i].Played = false;
    }

    public static bool HasPlayed(string id)
    {
        if (string.IsNullOrEmpty(id))
            return true;

        for (int i = 0; i < Beats.Count; i++)
        {
            TutorialBeat beat = Beats[i];
            if (beat.Id == id && beat.Played)
                return true;
        }

        return false;
    }

    public static List<List<Type>> SacrificeBatches()
    {
        return new List<List<Type>>
        {
            new List<Type> { typeof(Hoplite), typeof(Sheep), typeof(Blessing) },
        };
    }

    public static (Ritual top, Ritual bottom) TempleRitualRewards()
    {
        return (new ZeusMinor(), new ZeusMajor());
    }

    public static List<Card> MarketCards()
    {
        return new List<Card>
        {
            new Sheep(),
            new ThrowStone(),
            new Blessing(),
            new Hoplite(),
        };
    }

    public static List<Trinket> MarketTrinkets()
    {
        return new List<Trinket>
        {
            new CyclopsEyeTrinket(),
            new HermesSandalsTrinket(),
        };
    }
}
