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
}

public class TutorialSpriteCue
{
    public string ResourcePath;
    public Vector2 From = new Vector2(0.5f, 0.55f);
    public Vector2 To = new Vector2(0.5f, 0.42f);
    // Degrees from the authored arrow, which faces right. -90 points down.
    public float Rotation = -90f;
    public float Duration;
}

/// <summary>
/// One timed tutorial prompt. <see cref="Hole"/> is a normalized screen rect (x, y, width, height).
/// A non-positive width or height covers the whole screen.
/// </summary>
public class TutorialBeat
{
    public TutorialTrigger Trigger;
    public EncounterType Encounter = EncounterType.None;
    public int Arg = -1;
    public float Duration = 4f;
    public bool BlockInput = true;
    public string Text;
    public Vector2 TextAnchor = new Vector2(0.5f, 0.78f);
    public float SecondsPerCharacter = 0.03f;
    public bool ShowCover = true;
    public float CoverAlpha = 0.72f;
    public Rect Hole;
    public TutorialSpriteCue[] Sprites;
    public bool Played;
}

/// <summary>
/// Placeholder tutorial content. Replace these lists to change the prologue without touching the systems.
/// </summary>
public static class TutorialScript
{
    public static readonly List<TutorialBeat> Beats = new List<TutorialBeat>
    {
        new TutorialBeat
        {
            Trigger = TutorialTrigger.OverworldShown,
            Duration = 5f,
            Text = "This is the road. Click the next hex to begin.",
            TextAnchor = new Vector2(0.5f, 0.8f),
            Hole = new Rect(0.38f, 0.28f, 0.24f, 0.28f),
            Sprites = new[]
            {
                new TutorialSpriteCue
                {
                    From = new Vector2(0.5f, 0.62f),
                    To = new Vector2(0.5f, 0.5f),
                    Rotation = -90f,
                },
            },
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EncounterBegan,
            Encounter = EncounterType.TutorialCombat1,
            Duration = 4.5f,
            Text = "A cyclops blocks the path. Spend gold to play followers and spells.",
            TextAnchor = new Vector2(0.5f, 0.84f),
            Hole = new Rect(0.2f, 0.08f, 0.6f, 0.28f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.CombatTurnStarted,
            Duration = 4f,
            Text = "It is your turn. Play a card, or end the turn when you are ready.",
            TextAnchor = new Vector2(0.5f, 0.62f),
            Hole = new Rect(0.15f, 0.02f, 0.7f, 0.32f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EncounterBegan,
            Encounter = EncounterType.TutorialTemple,
            Duration = 4.5f,
            Text = "The temple asks for a sacrifice. Choose one card from the three offered.",
            TextAnchor = new Vector2(0.5f, 0.82f),
            Hole = new Rect(0.2f, 0.25f, 0.6f, 0.4f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.TempleBatchShown,
            Encounter = EncounterType.TutorialTemple,
            Arg = 0,
            Duration = 3.5f,
            Text = "Each offering is a step. Accept a card to give it up.",
            TextAnchor = new Vector2(0.5f, 0.16f),
            ShowCover = false,
            BlockInput = false,
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.RitualRewardsShown,
            Duration = 4.5f,
            Text = "A sacrifice is answered with a ritual. Take one of the two offered.",
            TextAnchor = new Vector2(0.5f, 0.8f),
            Hole = new Rect(0.18f, 0.2f, 0.64f, 0.45f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EncounterBegan,
            Encounter = EncounterType.TutorialCombat2,
            Duration = 4f,
            Text = "Another fight. The revelers look familiar, but their deck is not.",
            TextAnchor = new Vector2(0.5f, 0.84f),
            Hole = new Rect(0.2f, 0.08f, 0.6f, 0.28f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.ShopOpened,
            Duration = 4.5f,
            Text = "The market sells cards and trinkets for heartstrings. Buy what you need, or leave.",
            TextAnchor = new Vector2(0.5f, 0.84f),
            Hole = new Rect(0.12f, 0.18f, 0.76f, 0.5f),
        },
        new TutorialBeat
        {
            Trigger = TutorialTrigger.EncounterBegan,
            Encounter = EncounterType.Combat,
            Duration = 4f,
            Text = "The prologue is over. From here the road is your own.",
            TextAnchor = new Vector2(0.5f, 0.84f),
            ShowCover = false,
            BlockInput = false,
        },
    };

    public static void Reset()
    {
        for (int i = 0; i < Beats.Count; i++)
            Beats[i].Played = false;
    }

    public static List<List<Type>> SacrificeBatches()
    {
        return new List<List<Type>>
        {
            new List<Type> { typeof(Scry), typeof(Scry), typeof(Scry) },
            new List<Type> { typeof(Hoplite), typeof(Hoplite), typeof(Sheep) },
            new List<Type> { typeof(Blessing), typeof(Blessing), typeof(ThrowStone) },
        };
    }

    public static (Ritual top, Ritual bottom) TempleRitualRewards()
    {
        return (new ZeusMinor(), new HadesMinor());
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
