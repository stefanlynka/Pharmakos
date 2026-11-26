using System;
using UnityEngine;

/// <summary>
/// A follower dies: the skull stamps onto the card while it chars dark, holds a beat, then the card burns black and
/// sinks into the board, breaking into a puff of ash and embers as it is removed.
/// The card's root is positioned by its battle row every frame, so the motion is applied to CardHolder and undone
/// before the card goes back to the pool.
/// </summary>
public class FollowerDeathAnimation : AnimationAction
{
    // Seconds on the human's turn; the AI's turn plays a little slower so the player can follow it.
    const float StrikeDuration = 0.16f;
    const float HoldDuration = 0.3f;
    const float FallDuration = 0.32f;
    const float AiTurnSlowdown = 1.25f;

    const float SkullStampScale = 1.3f;
    const float FallScale = 0.78f;
    // Into the board (the card faces -Z)
    const float FallDepth = 0.6f;

    static readonly Color AshColor = new Color(0.82f, 0.77f, 0.66f, 1f);
    static readonly Color AshEndColor = new Color(0.23f, 0.20f, 0.17f, 0f);
    static readonly Color EmberColor = new Color(1f, 0.55f, 0.28f, 1f);
    static readonly Color EmberEndColor = new Color(0.54f, 0.25f, 0.12f, 0f);

    private ViewFollower ViewFollower;
    private Follower follower;

    Transform cardHolder;
    Vector3 holderScale;
    Vector3 holderPosition;
    Vector3 skullScale;

    public FollowerDeathAnimation(GameAction gameAction, Follower follower) : base(gameAction)
    {
        Stackable = true;

        this.follower = follower;
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
        float strike = StrikeDuration * pace;
        float hold = HoldDuration * pace;
        float fall = FallDuration * pace;

        cardHolder = ViewFollower.CardHolder != null ? ViewFollower.CardHolder.transform : null;
        if (cardHolder != null)
        {
            holderScale = cardHolder.localScale;
            holderPosition = cardHolder.localPosition;
        }
        if (ViewFollower.SkullRenderer != null)
            skullScale = ViewFollower.SkullRenderer.transform.localScale;

        // The life goes out of it: the card darkens under the skull, then burns fully black as it falls.
        VfxContext context = new VfxContext(null, ViewFollower);
        Char(0.6f, 0f, strike * 2f, strike + hold + fall + 1f).Play(context, null);
        Char(0.85f, strike + hold, fall * 0.6f, strike + hold + fall + 1f).Play(context, null);

        Sequence sequence = new Sequence();
        sequence.Add(new Tween(Strike, 0f, 1f, strike));
        sequence.Add(new Tween(null, 0f, 1f, hold));
        sequence.Add(new Tween(Fall, 0f, 1f, fall));
        sequence.Add(new SequenceAction(() => Ash(context)));
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

    // The skull lands: in from a larger size, settling with a slight overshoot.
    void Strike(float t)
    {
        if (ViewFollower == null || ViewFollower.SkullRenderer == null)
            return;

        SpriteRenderer skull = ViewFollower.SkullRenderer;
        Color color = skull.color;
        color.a = Mathf.Clamp01(t * 2f);
        skull.color = color;
        skull.transform.localScale = skullScale * Mathf.LerpUnclamped(SkullStampScale, 1f, EaseOutBack(t));
    }

    void Fall(float t)
    {
        if (cardHolder == null)
            return;

        float eased = t * t;
        cardHolder.localScale = holderScale * Mathf.Lerp(1f, FallScale, eased);
        cardHolder.localPosition = holderPosition + Vector3.forward * (FallDepth * eased);
    }

    void Ash(VfxContext context)
    {
        if (ViewFollower == null || !ViewFollower.gameObject.activeInHierarchy)
            return;

        // Ash: bone-pale flakes that puff out from the card's footprint and drift up as they fade.
        new ParticleBurstVfx
        {
            Count = 55,
            LifetimeMin = 0.6f,
            LifetimeMax = 1.3f,
            SpeedMin = 1.5f,
            SpeedMax = 5f,
            SizeMin = 0.14f,
            SizeMax = 0.36f,
            StartColor = AshColor,
            EndColor = AshEndColor,
            EmitRadius = 1.8f,
            Drag = 3f,
            Gravity = -0.3f,
            QueueHold = 0f,
        }.Play(context, null);

        // Embers: a few hot sparks rising out of the ash.
        new ParticleBurstVfx
        {
            Count = 22,
            LifetimeMin = 0.6f,
            LifetimeMax = 1.2f,
            SpeedMin = 2.5f,
            SpeedMax = 6.5f,
            SizeMin = 0.08f,
            SizeMax = 0.18f,
            StartColor = EmberColor,
            EndColor = EmberEndColor,
            EmitRadius = 1.4f,
            Drag = 2f,
            Gravity = -0.5f,
            QueueHold = 0f,
        }.Play(context, null);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
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
