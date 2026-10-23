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
    CardRewardsShown,
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
    Discard,
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
    // Shown with this image. A beat whose sprites have text waits for a click between images.
    public string Text;
    // From/To are the normalized anchor min and max, and the arrow does not bob.
    public bool Fill;
    // Draw the tutorial text frame around this image.
    public bool Frame;
    // Rest position is pushed out so the bob stays outside the spotlight hole.
    public bool StayOutsideHole;
}

/// <summary>
/// One timed tutorial prompt. <see cref="Hole"/> is a normalized screen rect (x, y, width, height).
/// <see cref="Focus"/> replaces that rect with the live object when it can be found.
/// <see cref="BlockInput"/> makes the cover swallow clicks outside <see cref="Hole"/>.
/// <see cref="ShowBlackBlocker"/> covers the whole screen and blocks input.
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
    public bool BlockInput;
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
            BlockInput = true,
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
            BlockInput = true,
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
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.88f),
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
            BlockInput = true,
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
            Text = "When a Follower has a glowing border, it means it can attack!\nClick and drag it onto a target. \nNothing is in the way, so you can attack the enemy player directly!",
            BlockInput = true,
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
            Text = "Get the enemy's health to 0 to win this combat.",
            BlockInput = true,
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
            Text = "The enemy played a follower. Time to learn about attacking!",
            BlockInput = true,
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
            TextAnchor = new Vector2(0.5f, 0.9f),
            ShowCover = false,
            ShowBlackBlocker = true,
            Sprites = AttackRangeImages(),
        },

        // Card rewards after the first combat
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CardRewardsShown,
            Encounter = EncounterType.TutorialCombat1,
            Duration = -1f,
            ShowCover = false,
            Text = "When you win a combat you will get two sets of card rewards.\nYou can add up to one from each to your deck",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.9f),
        },

        // Temple
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.TutorialCombat1,
            Duration = -1f,
            ShowCover = false,
            Text = "The next location is a temple. There you will be asked to sacrifice some of your spells and followers, and have them removed from your deck.",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.88f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EncounterBegan,
            Encounter = EncounterType.TutorialTemple,
            Duration = -1f,
            ShowCover = false,
            Text = "You must choose one of these three cards to sacrifice. Refuse, and you will anger the gods and face a difficult fight.\n(I do not recommend it in the tutorial)",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.88f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.RitualRewardsShown,
            Duration = -1f,
            ShowCover = false,
            Text = "As a reward for sacrificing to the gods, you may adopt new Rituals. You may have up to two rituals at a time.",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.9f),
        },

        // Second combat
        new TutorialBeat
        {
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat2,
            Duration = -1f,
            ShowCover = false,
            Text = "Now that you have rituals, you just need something to sacrifice.",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.58f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat2,
            Duration = -1f,
            Text = "Every action you take produces Offerings.", 
            BlockInput = true,
            TextAnchor = new Vector2(0.42f, 0.36f),
            Focus = TutorialFocus.Offerings,
            PointAtHole = true,
            Hole = new Rect(0.07f, 0.02f, 0.16f, 0.16f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat2,
            Duration = -1f,
            Text = "Summoning a Follower creates a Crop.",
            BlockInput = true,
            TextAnchor = new Vector2(0.42f, 0.32f),
            Focus = TutorialFocus.Crop,
            PointAtHole = true,
            Hole = new Rect(0.08f, 0.02f, 0.07f, 0.07f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat2,
            Duration = -1f,
            Text = "Playing a Spell creates a Scroll.",
            BlockInput = true,
            TextAnchor = new Vector2(0.42f, 0.32f),
            Focus = TutorialFocus.Scroll,
            PointAtHole = true,
            Hole = new Rect(0.15f, 0.02f, 0.07f, 0.07f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat2,
            Duration = -1f,
            Text = "When a Follower dies on your turn, (yours or your opponent's), you gain a Bone.",
            BlockInput = true,
            TextAnchor = new Vector2(0.42f, 0.36f),
            Focus = TutorialFocus.Bone,
            PointAtHole = true,
            Hole = new Rect(0.15f, 0.09f, 0.07f, 0.07f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat2,
            Duration = -1f,
            Text = "When a player's life changes on your turn, (yours or your opponent's), you gain that much Blood.",
            BlockInput = true,
            TextAnchor = new Vector2(0.42f, 0.36f),
            Focus = TutorialFocus.Blood,
            PointAtHole = true,
            Hole = new Rect(0.08f, 0.09f, 0.07f, 0.07f),
        },
        new TutorialBeat
        {
            Id = OfferingsBeatId,
            Trigger = TutorialTrigger.HandDrawn,
            Encounter = EncounterType.TutorialCombat2,
            Duration = -1f,
            Text = "Generate at least one of each in the same turn and you'll be able to call upon the might of Zeus!",
            BlockInput = true,
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
            BlockInput = true,
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
            BlockInput = true,
            TextAnchor = new Vector2(0.62f, 0.28f),
            Focus = TutorialFocus.EndTurn,
            PointAtHole = true,
            Hole = new Rect(0.82f, 0.02f, 0.16f, 0.1f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EndTurnHovered,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 3,
            Duration = -1f,
            Text = "At the end of each turn, you draw back up to 6 cards.",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.52f),
            Focus = TutorialFocus.Hand,
            PointAtHole = true,
            Hole = new Rect(0.2f, 0.02f, 0.58f, 0.24f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EndTurnHovered,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 3,
            Duration = -1f,
            Text = "During your turn, you can discard cards you don't want by dragging them to the discard zone over here or by clicking the discard all button.",
            BlockInput = true,
            TextAnchor = new Vector2(0.36f, 0.55f),
            Focus = TutorialFocus.Discard,
            PointAtHole = true,
            Hole = new Rect(0.7f, 0.02f, 0.28f, 0.4f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EndTurnHovered,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 3,
            Duration = -1f,
            Text = "Some rituals have Powerful Effects but need lots of Offerings.\nSometimes it's smart to use Discarding to curate your hand as you prepare for an explosive turn!",
            BlockInput = true,
            TextAnchor = new Vector2(0.52f, 0.62f),
            Focus = TutorialFocus.Rituals,
            PointAtHole = true,
            Hole = new Rect(0.02f, 0.25f, 0.14f, 0.4f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Encounter = EncounterType.TutorialCombat2,
            Arg = 3,
            Delay = 1.1f,
            Duration = -1f,
            Text = "Your enemy just damaged you. If your health drops to 0, you'll lose a heartstring and their turn will end. Run out of health and heartstrings, and it's game over.",
            BlockInput = true,
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
            Text = "Up next is a shop. There you can spend your heartstrings on cards and trinkets. The main way to get more heartstrings is from events.",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.88f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.ShopOpened,
            Duration = -1f,
            ShowCover = false,
            Text = "Heartstrings are rare and valuable. Spend them well.",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.9f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.TutorialMarket,
            Duration = -1f,
            ShowCover = false,
            Text = "Those are the basics. Near the end of this demo are two bosses. Each one grants a powerful ability once defeated, which you can use on future runs. Past those bosses is the River Styx, and the final boss.\nGood Luck!",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.80f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.Styx,
            Duration = -1f,
            ShowCover = false,
            Text = "Congratulations on defeating a boss! To reach the final boss, you must pass the River Styx, which comes with a twist...",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.82f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Encounter = EncounterType.Styx,
            Duration = -1f,
            ShowCover = false,
            Text = "The River Styx replaces every card in your deck with the cards you've sacrificed this run. The same is true of trinkets and rituals. You can track what you've sacrificed in the status screen.",
            BlockInput = true,
            TextAnchor = new Vector2(0.5f, 0.82f),
        },
    };

    static TutorialSpriteCue[] AttackRangeImages()
    {
        string[] lines =
        {
            "When a Follower is ready to attack, mousing over it will show you who it can reach.",
            "When your Follower is in line with the enemy, it can attack the enemy directly across from it, and one to either side.",
            "When they're not aligned, your Follower can attack the two enemies in front of it.",
            "When your Follower could attack an empty space, they can attack the enemy player directly!",
            "Also, a Follower with Taunt must be attacked first.",
        };

        var cues = new TutorialSpriteCue[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            cues[i] = new TutorialSpriteCue
            {
                ResourcePath = "Images/Tutorial/NewTutorial/" + (i + 1),
                Text = lines[i],
                From = new Vector2(0.18f, 0f),
                To = new Vector2(0.82f, 0.76f),
                Rotation = 0f,
                Fill = true,
                Frame = true,
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
            new PriceOfKnowledge(),
            new Drown(),
            new Phalangite(),
            new NemeanLion(),
            new Ekdromos(),
            new ThrowStone(),
            new Scry(),
            new Toxotes(),
        };
    }

    public static List<Trinket> MarketTrinkets()
    {
        return new List<Trinket>
        {
            new CyclopsEyeTrinket(),
            new HermesSandalsTrinket(),
            new PeltastTrumpetTrinket(),
        };
    }
}
