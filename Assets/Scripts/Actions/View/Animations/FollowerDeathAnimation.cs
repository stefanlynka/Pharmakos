using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A follower dies, with weight: the skull drops onto the card like something heavy (accelerating in, a hard landing,
/// the card recoiling into the board) while the card chars; a beat; then the card lifts slightly and falls into the
/// board, burnt black, landing with a thud (sound, a short screen shake) and breaking into bone-white dust and soot.
/// The Bone offering token it yields leaves from the dust (CreateOfferingAnimation waits via DeferUntilAsh).
/// The card's root is positioned by its battle row every frame, so the motion is applied to CardHolder and undone
/// before the card goes back to the pool. Sounds: Tools/Audio/build_death_sounds.py.
/// </summary>
public class FollowerDeathAnimation : AnimationAction
{
    // Seconds on the human's turn; the AI's turn plays a little slower so the player can follow it.
    const float DropDuration = 0.11f;      // skull accelerating onto the card
    const float SettleDuration = 0.12f;    // the landing settling
    const float HoldDuration = 0.36f;
    const float LiftDuration = 0.09f;      // the card gathering before it falls
    const float FallDuration = 0.24f;
    const float AiTurnSlowdown = 1.25f;

    const float SkullDropScale = 1.7f;
    const float SkullLandScale = 0.93f;
    // The card is knocked into the board when the skull lands, then rises back
    const float RecoilDepth = 0.25f;
    const float RecoilScale = 0.97f;
    const float LiftScale = 1.03f;
    const float LiftHeight = 0.15f;
    const float FallScale = 0.72f;
    // Into the board (the card faces -Z)
    const float FallDepth = 0.8f;

    const float ShakeIntensity = 0.3f;
    const float ShakeDuration = 0.22f;
    // Deaths in the same instant (a cleave, a board wipe) share one sound and one shake.
    const float SharedImpactWindow = 0.08f;

    // White is death (the Bone offering colour); soot for the burnt card.
    static readonly Color DustColor = new Color(0.85f, 0.83f, 0.80f, 1f);
    static readonly Color DustEndColor = new Color(0.45f, 0.43f, 0.40f, 0f);
    static readonly Color SootColor = new Color(0.10f, 0.09f, 0.08f, 0.9f);
    static readonly Color SootEndColor = new Color(0.05f, 0.04f, 0.04f, 0f);

    static float lastStrikeSoundTime = -1f;
    static float lastCollapseTime = -1f;

    // Followers mid-death, with what waits for their ash (the Bone token's flight).
    static readonly Dictionary<ViewFollower, List<Action>> waitingForAsh = new Dictionary<ViewFollower, List<Action>>();

    // Where recently dead followers broke into ash (by follower ID), for anything that leaves from them after their
    // card is gone (e.g. a gold token queued behind another trigger). Only the last few are kept.
    static readonly Dictionary<int, Vector3> ashPositions = new Dictionary<int, Vector3>();
    static readonly Queue<int> ashOrder = new Queue<int>();
    const int AshPositionsKept = 16;

    /// <summary>Where the follower with this ID broke into ash, if it died recently.</summary>
    public static bool TryGetAshPosition(int followerID, out Vector3 position)
    {
        return ashPositions.TryGetValue(followerID, out position);
    }

    static void RememberAsh(int followerID, Vector3 position)
    {
        if (!ashPositions.ContainsKey(followerID))
        {
            ashOrder.Enqueue(followerID);
            if (ashOrder.Count > AshPositionsKept)
                ashPositions.Remove(ashOrder.Dequeue());
        }
        ashPositions[followerID] = position;
    }

    private ViewFollower ViewFollower;
    private Follower follower;

    Transform cardHolder;
    Vector3 holderScale;
    Vector3 holderPosition;
    Vector3 skullScale;
    bool landed;

    public FollowerDeathAnimation(GameAction gameAction, Follower follower) : base(gameAction)
    {
        Stackable = true;

        this.follower = follower;
    }

    /// <summary>
    /// If <paramref name="viewFollower"/> is dying, runs <paramref name="action"/> when it breaks into ash (while its
    /// card is still in place) and returns true; otherwise returns false and the caller should go ahead now.
    /// </summary>
    public static bool DeferUntilAsh(ViewFollower viewFollower, Action action)
    {
        if (viewFollower == null || action == null || !waitingForAsh.TryGetValue(viewFollower, out List<Action> actions))
            return false;

        actions.Add(action);
        return true;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (follower == null)
        {
            CallCallback();
            return;
        }

        if (!View.Instance.TryGetViewFollower(follower, out ViewFollower))
        {
            CallCallback();
            return;
        }

        float pace = View.Instance.IsHumansTurn ? 1f : AiTurnSlowdown;
        float drop = DropDuration * pace;
        float settle = SettleDuration * pace;
        float hold = HoldDuration * pace;
        float lift = LiftDuration * pace;
        float fall = FallDuration * pace;
        float total = drop + settle + hold + lift + fall;

        cardHolder = ViewFollower.CardHolder != null ? ViewFollower.CardHolder.transform : null;
        if (cardHolder != null)
        {
            holderScale = cardHolder.localScale;
            holderPosition = cardHolder.localPosition;
        }
        if (ViewFollower.SkullRenderer != null)
            skullScale = ViewFollower.SkullRenderer.transform.localScale;

        if (!waitingForAsh.ContainsKey(ViewFollower))
            waitingForAsh[ViewFollower] = new List<Action>();

        // The life goes out of it: the card darkens as the skull lands, then burns fully black as it falls.
        VfxContext context = new VfxContext(null, ViewFollower);
        Char(0.6f, drop * 0.5f, drop + settle * 2f, total + 1f).Play(context, null);
        Char(0.85f, drop + settle + hold, lift + fall * 0.5f, total + 1f).Play(context, null);

        Sequence sequence = new Sequence();
        sequence.Add(new Tween(Drop, 0f, 1f, drop));
        sequence.Add(new SequenceAction(Land));
        sequence.Add(new Tween(Settle, 0f, 1f, settle));
        sequence.Add(new Tween(null, 0f, 1f, hold));
        sequence.Add(new Tween(Lift, 0f, 1f, lift));
        sequence.Add(new Tween(Fall, 0f, 1f, fall));
        sequence.Add(new SequenceAction(() => Collapse(context)));
        sequence.Add(new SequenceAction(RemoveFollower));
        sequence.Add(new SequenceAction(CallCallback));
        sequence.Start();
    }

    static CharCardVfx Char(float strength, float delay, float fade, float lifetime)
    {
        return new CharCardVfx
        {
            CharColor = new Color(0.05f, 0.04f, 0.035f, strength),
            Delay = delay,
            FadeDuration = fade,
            Lifetime = lifetime,
            QueueHold = 0f,
        };
    }

    // The skull falls onto the card: large and faint, accelerating as it comes down.
    void Drop(float t)
    {
        if (ViewFollower == null || ViewFollower.SkullRenderer == null)
            return;

        SpriteRenderer skull = ViewFollower.SkullRenderer;
        Color color = skull.color;
        color.a = Mathf.Clamp01(t * 1.6f);
        skull.color = color;
        skull.transform.localScale = skullScale * Mathf.Lerp(SkullDropScale, SkullLandScale, t * t * t);
    }

    void Land()
    {
        if (landed)
            return;
        landed = true;

        if (Time.time - lastStrikeSoundTime > SharedImpactWindow)
        {
            lastStrikeSoundTime = Time.time;
            View.Instance?.AudioHandler?.PlayDeathStrike();
        }
    }

    // The landing: the skull settles to its size; the card, knocked into the board, comes back up.
    void Settle(float t)
    {
        if (ViewFollower != null && ViewFollower.SkullRenderer != null)
        {
            Color color = ViewFollower.SkullRenderer.color;
            color.a = 1f;
            ViewFollower.SkullRenderer.color = color;
            ViewFollower.SkullRenderer.transform.localScale = skullScale * Mathf.Lerp(SkullLandScale, 1f, EaseOut(t));
        }

        if (cardHolder == null)
            return;

        float knock = 1f - EaseOut(t);
        cardHolder.localScale = holderScale * Mathf.Lerp(1f, RecoilScale, knock);
        cardHolder.localPosition = holderPosition + Vector3.forward * (RecoilDepth * knock);
    }

    // The card gathers itself before it falls: a little up and a little larger.
    void Lift(float t)
    {
        if (cardHolder == null)
            return;

        float eased = EaseOut(t);
        cardHolder.localScale = holderScale * Mathf.Lerp(1f, LiftScale, eased);
        cardHolder.localPosition = holderPosition + Vector3.back * (LiftHeight * eased);
    }

    // Then drops into the board, accelerating, as dead weight.
    void Fall(float t)
    {
        if (cardHolder == null)
            return;

        float eased = t * t * t;
        cardHolder.localScale = holderScale * Mathf.Lerp(LiftScale, FallScale, eased);
        cardHolder.localPosition = holderPosition + Vector3.back * LiftHeight
            + Vector3.forward * ((LiftHeight + FallDepth) * eased);
    }

    void Collapse(VfxContext context)
    {
        if (ViewFollower != null && follower != null)
            RememberAsh(follower.ID, ViewFollower.transform.position);

        if (ViewFollower != null && waitingForAsh.TryGetValue(ViewFollower, out List<Action> waiting))
        {
            waitingForAsh.Remove(ViewFollower);
            foreach (Action action in waiting)
                action?.Invoke();
        }

        if (ViewFollower == null || !ViewFollower.gameObject.activeInHierarchy)
            return;

        if (Time.time - lastCollapseTime > SharedImpactWindow)
        {
            lastCollapseTime = Time.time;
            View.Instance?.AudioHandler?.PlayDeathCollapse();
            ScreenShakeHandler.Shake(ShakeIntensity, ShakeDuration);
        }

        // Dust: bone-white, a heavy puff that spreads low and settles rather than flying off.
        new ParticleBurstVfx
        {
            Count = 60,
            LifetimeMin = 0.7f,
            LifetimeMax = 1.4f,
            SpeedMin = 2f,
            SpeedMax = 6f,
            SizeMin = 0.16f,
            SizeMax = 0.4f,
            StartColor = DustColor,
            EndColor = DustEndColor,
            EmitRadius = 1.8f,
            Drag = 4f,
            Gravity = -0.15f,
            QueueHold = 0f,
        }.Play(context, null);

        // Soot: dark flakes from the burnt card, drifting up.
        new ParticleBurstVfx
        {
            Count = 26,
            LifetimeMin = 0.8f,
            LifetimeMax = 1.5f,
            SpeedMin = 1f,
            SpeedMax = 3.5f,
            SizeMin = 0.12f,
            SizeMax = 0.3f,
            StartColor = SootColor,
            EndColor = SootEndColor,
            EmitRadius = 1.5f,
            Drag = 2.5f,
            Gravity = -0.35f,
            QueueHold = 0f,
        }.Play(context, null);
    }

    static float EaseOut(float t)
    {
        float r = 1f - Mathf.Clamp01(t);
        return 1f - r * r * r;
    }

    // Undo the motion so the pooled card comes back as authored (the skull's alpha is reset by ResetForPool).
    void RestoreTransforms()
    {
        if (cardHolder != null)
        {
            cardHolder.localScale = holderScale;
            cardHolder.localPosition = holderPosition;
        }
        if (ViewFollower != null && ViewFollower.SkullRenderer != null)
            ViewFollower.SkullRenderer.transform.localScale = skullScale;
    }

    private void RemoveFollower()
    {
        if (ViewFollower != null && waitingForAsh.TryGetValue(ViewFollower, out List<Action> waiting))
        {
            // Never leave a token stranded, even if the card went away before its ash.
            waitingForAsh.Remove(ViewFollower);
            foreach (Action action in waiting)
                action?.Invoke();
        }

        //Debug.LogWarning(ViewFollower.Follower.GetName() + " " + ViewFollower.Follower.ID + " death Animation end");
        if (ViewFollower == null || follower == null || View.Instance == null)
            return;

        // The fight may already have cleared this card once the screen went black.
        if (!View.Instance.TryGetViewFollower(follower, out ViewFollower current) || current != ViewFollower)
        {
            RestoreTransforms();
            return;
        }

        RestoreTransforms();
        View.Instance.RemoveViewCard(ViewFollower);
    }

    protected override void Log()
    {
        if (follower != null && follower.Owner != null) Debug.LogWarning("FollowerDeathAnimation: " + follower .Owner.GetName() + "'s " + follower.GetName() + " dies");
    }
}
