using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared helpers for the cue VFX: where on screen a target is, and shaking a card.</summary>
public static class CueVfxUtil
{
    public const float Lift = 0.8f;
    const float AuthoredCardWidth = 2.5f;
    const float FallbackUnit = 2.6f;
    static float unit = -1f;

    /// <summary>
    /// World size of one authoring unit. Cue VFX sizes, offsets and shakes are written for a board card about 2.5 units
    /// wide; this scales them to the real cards (measured once from a follower on the board, about 6.5 units today).
    /// </summary>
    public static float Unit
    {
        get
        {
            if (unit > 0f)
                return unit;

            ViewFollower sample = null;
            foreach (ViewFollower view in UnityEngine.Object.FindObjectsOfType<ViewFollower>())
            {
                if (view.Follower != null && view.Follower.Owner != null && view.Follower.Owner.BattleRow.Followers.Contains(view.Follower))
                {
                    sample = view;
                    break;
                }
            }
            BoxCollider box = sample != null ? sample.GetComponentInChildren<BoxCollider>() : null;
            if (box == null)
                return FallbackUnit;

            unit = Mathf.Clamp(box.bounds.size.x / AuthoredCardWidth, 0.25f, 10f);
            return unit;
        }
    }

    /// <summary>A point on the target, pulled toward the camera so strokes draw in front of it.</summary>
    public static Vector3 Point(ViewTarget target, float lift = Lift)
    {
        if (target == null)
            return Vector3.zero;

        Transform anchor = target.transform;
        if (target is ViewPlayer player && player.ViewPlayerPortrait != null && player.ViewPlayerPortrait.PortraitContainer != null)
            anchor = player.ViewPlayerPortrait.PortraitContainer;
        else if (target is ViewCard card && card.CardHolder != null)
            anchor = card.CardHolder.transform;

        Vector3 position = anchor.position;
        Camera cam = Camera.main;
        if (cam != null && lift != 0f)
            position += (cam.transform.position - position).normalized * lift * Unit;
        return position;
    }

    public static Vector3 ScreenUp()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.up : Vector3.up;
    }

    public static Vector3 ScreenRight()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.right : Vector3.right;
    }

    /// <summary>Shakes a card (its CardHolder) side to side, decaying over the duration. Fear, a roar, a bite.</summary>
    public static void Shudder(ViewCard card, float amplitude, float duration, float frequency = 22f)
    {
        if (card == null || card.CardHolder == null)
            return;

        Transform holder = card.CardHolder.transform;
        Vector3 rest = holder.localPosition;
        amplitude *= Unit / Mathf.Max(0.01f, holder.lossyScale.x);
        Vector3 right = holder.parent != null ? holder.parent.InverseTransformDirection(ScreenRight()) : ScreenRight();
        float phase = UnityEngine.Random.value * 6f;

        Sequence sequence = new Sequence();
        sequence.Add(new Tween(t =>
        {
            if (holder == null)
                return;
            float decay = 1f - t;
            holder.localPosition = rest + right * (Mathf.Sin(phase + t * duration * frequency * Mathf.PI * 2f) * amplitude * decay);
        }, 0f, 1f, duration));
        sequence.Add(new SequenceAction(() =>
        {
            if (holder != null)
                holder.localPosition = rest;
        }));
        sequence.Start();
    }

    public static ViewTarget ViewOf(ITarget target)
    {
        if (target == null || View.Instance == null)
            return null;
        return View.Instance.GetViewTargetByID(target.GetID());
    }
}

/// <summary>Strokes from one set burst outward on the target (or source). A splash of water, blood or gold.</summary>
public class StrokeBurstVfx : VfxEffect
{
    public string Folder = "Images/Particles/Water";
    public int Count = 9;
    public float Scale = 1f;
    public float Spread = 0.4f;
    public float Drift = 2.2f;
    public float Delay = 0f;

    public StrokeBurstVfx()
    {
        QueueHold = 0.25f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        ViewTarget anchor = Anchor == VfxAnchor.Target ? context.Target ?? context.Source : context.Source ?? context.Target;
        Vector3 point = CueVfxUtil.Point(anchor);
        CuePlayer.Hold(Delay, () =>
        {
            StrokeProjectileView painter = StrokeProjectileView.Create(Folder, false);
            painter?.Burst(point, CueVfxUtil.ScreenUp(), Count, Scale, Spread, Drift);
        });
        CompleteAfter(Delay + QueueHold, onComplete);
    }
}

/// <summary>The target card trembles (Panic): ink strokes flicker round it while it shakes.</summary>
public class FearVfx : VfxEffect
{
    public float Duration = 0.55f;

    public FearVfx()
    {
        QueueHold = 0.6f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        ViewCard card = context.Target as ViewCard;
        CueVfxUtil.Shudder(card, 0.14f, Duration, 18f);

        StrokeProjectileView painter = StrokeProjectileView.Create("Images/Particles/Ink", false);
        if (painter != null && context.Target != null)
        {
            Vector3 centre = CueVfxUtil.Point(context.Target);
            Vector3 up = CueVfxUtil.ScreenUp();
            Vector3 right = CueVfxUtil.ScreenRight();
            // Strokes close in on the card from the sides: dread pressing in.
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float u = CueVfxUtil.Unit;
                Vector3 start = centre + right * side * UnityEngine.Random.Range(1.3f, 1.8f) * u + up * UnityEngine.Random.Range(-1.4f, 1.4f) * u;
                Vector3 inward = (centre - start).normalized;
                painter.Paint(start, inward, UnityEngine.Random.Range(0.45f, 0.65f), 0.08f + i * 0.03f, 0.12f, 0.3f, 1.4f);
            }
        }

        if (card != null)
            TriggerFlash.Pulse(card.CardHolder != null ? card.CardHolder.transform : card.transform, 0.94f, Duration);

        CompleteAfterHold(onComplete);
    }
}

/// <summary>
/// The sacrificed follower's heart: two heartbeats (lub-dub, lub-dub) pulse the card and flush it blood red, timed to
/// the Price Of... cast sounds (build_cue_sounds.py HEARTBEATS), then blood bursts from it before it dies.
/// </summary>
public class HeartbeatVfx : VfxEffect
{
    // Must match HEARTBEATS in Tools/Audio/build_cue_sounds.py.
    public static readonly float[] Beats = { 0.0f, 0.17f, 0.62f, 0.79f };
    public string BurstFolder = "Images/Particles/Blood";

    public HeartbeatVfx()
    {
        QueueHold = 1.0f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        ViewCard card = context.Target as ViewCard;
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        Transform holder = card.CardHolder != null ? card.CardHolder.transform : card.transform;
        for (int i = 0; i < Beats.Length; i++)
        {
            bool strong = i % 2 == 0;
            int beat = i;
            CuePlayer.Hold(Beats[i], () =>
            {
                TriggerFlash.Pulse(holder, strong ? 1.08f : 1.05f, 0.16f);
                if (beat == 0)
                    VfxLibrary.StatChangeTint(false, Beats[Beats.Length - 1] + 0.35f).Play(new VfxContext(null, card), null);
            });
        }

        CuePlayer.Hold(Beats[Beats.Length - 1] + 0.05f, () =>
        {
            StrokeProjectileView painter = StrokeProjectileView.Create(BurstFolder, false);
            painter?.Burst(CueVfxUtil.Point(card), CueVfxUtil.ScreenUp(), 10, 1.05f, 0.45f, 2.6f);
        });

        CompleteAfterHold(onComplete);
    }
}

/// <summary>Gnaw: two rows of blood strokes snap shut on the target like jaws, then blood bursts and it shakes.</summary>
public class BiteVfx : VfxEffect
{
    public const float CloseTime = 0.16f;

    public BiteVfx()
    {
        QueueHold = 0.45f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        ViewTarget target = context.Target;
        StrokeProjectileView painter = StrokeProjectileView.Create("Images/Particles/Blood", false);
        if (painter == null || target == null)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 centre = CueVfxUtil.Point(target);
        Vector3 up = CueVfxUtil.ScreenUp();
        Vector3 right = CueVfxUtil.ScreenRight();
        // Upper and lower jaws: teeth-like strokes pointing at each other, drifting together.
        for (int jaw = 0; jaw < 2; jaw++)
        {
            float side = jaw == 0 ? 1f : -1f;
            for (int i = 0; i < 5; i++)
            {
                float u = CueVfxUtil.Unit;
                float x = (i - 2) * 0.48f * u;
                float curve = 0.18f * (Mathf.Abs(i - 2));
                Vector3 start = centre + right * x + up * side * (1.35f - curve) * u;
                painter.Paint(start, -up * side, 0.35f + 0.06f * (2 - Mathf.Abs(i - 2)), CloseTime * 0.7f, 0.12f, 0.22f, 5.2f);
            }
        }

        CuePlayer.Hold(CloseTime, () =>
        {
            painter = StrokeProjectileView.Create("Images/Particles/Blood", false);
            painter?.Burst(centre, up, 9, 0.9f, 0.35f, 2.4f);
            CueVfxUtil.Shudder(target as ViewCard, 0.12f, 0.3f, 16f);
            ScreenShakeHandler.Shake(0.12f, 0.15f);
        });

        CompleteAfterHold(onComplete);
    }
}

/// <summary>
/// Roar: bands of ink strokes roll out from the caster's side across the victim's row like a shockwave, and every
/// victim follower flinches as the wave reaches it, with a short screen shake. The Attack loss animations follow.
/// </summary>
public class RoarVfx : VfxEffect
{
    public Player Caster;
    const int Bands = 4;
    const float BandGap = 0.07f;

    public RoarVfx()
    {
        QueueHold = 0.65f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        Player victim = Caster != null ? Caster.GetOtherPlayer() : null;
        ViewTarget casterView = CueVfxUtil.ViewOf(Caster);
        ViewTarget victimView = CueVfxUtil.ViewOf(victim);
        if (casterView == null || victimView == null)
        {
            onComplete?.Invoke();
            return;
        }

        ScreenShakeHandler.Shake(0.25f, 0.5f);
        float u = CueVfxUtil.Unit;
        Vector3 right = CueVfxUtil.ScreenRight();

        // The victim's row: its followers' centre and span (or its portrait when the row is empty).
        List<ViewCard> cards = new List<ViewCard>();
        Vector3 rowCentre = Vector3.zero;
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (Follower follower in victim.BattleRow.Followers)
        {
            if (CueVfxUtil.ViewOf(follower) is ViewCard card)
            {
                Vector3 point = CueVfxUtil.Point(card);
                cards.Add(card);
                rowCentre += point;
                float x = Vector3.Dot(point, right);
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
            }
        }
        Vector3 origin = CueVfxUtil.Point(casterView);
        if (cards.Count > 0)
        {
            rowCentre /= cards.Count;
        }
        else
        {
            rowCentre = CueVfxUtil.Point(victimView);
            minX = maxX = Vector3.Dot(rowCentre, right);
        }
        float halfWidth = (maxX - minX) * 0.5f + 1.6f * u;
        Vector3 away = (rowCentre - origin).normalized;

        StrokeProjectileView painter = StrokeProjectileView.Create("Images/Particles/Ink", false);
        for (int band = 0; band < Bands; band++)
        {
            // Bands start between the two sides and finish just past the victim's row.
            Vector3 bandCentre = Vector3.Lerp(origin, rowCentre, 0.45f + 0.2f * band);
            int strokes = Mathf.Max(5, Mathf.RoundToInt(halfWidth * 2f / (0.8f * u)));
            int captured = band;
            CuePlayer.Hold(BandGap * band, () =>
            {
                for (int i = 0; i < strokes; i++)
                {
                    float t = strokes == 1 ? 0f : i / (float)(strokes - 1) * 2f - 1f;
                    Vector3 position = bandCentre + right * (t * halfWidth) + away * UnityEngine.Random.Range(-0.3f, 0.3f) * u;
                    Vector3 direction = Quaternion.AngleAxis(UnityEngine.Random.Range(-12f, 12f), Camera.main != null ? Camera.main.transform.forward : Vector3.forward) * away;
                    painter?.Paint(position, direction, UnityEngine.Random.Range(0.6f, 0.85f) * (1f - 0.1f * captured), 0.05f, 0.06f, 0.3f, 2.5f);
                }
            });
        }

        // Each victim flinches as the last band reaches it.
        foreach (ViewCard card in cards)
            CuePlayer.Hold(BandGap * (Bands - 1), () => CueVfxUtil.Shudder(card, 0.18f, 0.4f, 13f));

        CompleteAfterHold(onComplete);
    }
}

/// <summary>
/// Twist of Fate: the board darkens and gold threads are drawn in from every side, converging on the favoured
/// player's portrait, where they burst. The granted buff is revealed right after (RevealPlayedTrinketAnimation).
/// Timed to the CastFate sound: threads gather over GatherTime, the burst lands on the final bell.
/// </summary>
public class TwistOfFateVfx : VfxEffect
{
    // Must match FATE_STRIKE in Tools/Audio/build_cue_sounds.py.
    public const float GatherTime = 1.05f;
    const int Threads = 7;

    public TwistOfFateVfx()
    {
        QueueHold = GatherTime + 0.25f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        ViewTarget target = context.Target;
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        PostFXPulse.TwistOfFate(GatherTime);

        Vector3 end = CueVfxUtil.Point(target, 1.2f);
        Vector3 up = CueVfxUtil.ScreenUp();
        Vector3 right = CueVfxUtil.ScreenRight();
        Vector3 centre = Vector3.zero;
        if (Controller.Instance != null && Controller.Instance.GameField != null)
            centre = Controller.Instance.GameField.transform.position;
        Camera cam = Camera.main;
        if (cam != null)
            centre += (cam.transform.position - centre).normalized * 1.5f * CueVfxUtil.Unit;

        for (int i = 0; i < Threads; i++)
        {
            // Each thread starts somewhere round the edge of the board and curls in, arriving one after another.
            float angle = (i / (float)Threads) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
            float u = CueVfxUtil.Unit;
            Vector3 start = centre + right * Mathf.Cos(angle) * 11f * u + up * Mathf.Sin(angle) * 6f * u;
            Vector3 control = Vector3.Lerp(start, end, 0.5f) + Vector3.Cross(end - start, cam != null ? cam.transform.forward : Vector3.forward).normalized * 3.5f * u;
            float launch = 0.08f * i;
            float flight = GatherTime - launch;
            CuePlayer.Hold(launch, () => FlyThread(start, control, end, flight));
        }

        CuePlayer.Hold(GatherTime, () =>
        {
            StrokeProjectileView burst = StrokeProjectileView.Create("Images/Particles/Gold", false);
            burst?.Burst(end, up, 14, 1.25f, 0.6f, 3.4f);
            ScreenShakeHandler.Shake(0.1f, 0.2f);
        });

        CompleteAfterHold(onComplete);
    }

    static void FlyThread(Vector3 start, Vector3 control, Vector3 end, float duration)
    {
        StrokeProjectileView thread = StrokeProjectileView.Create("Images/Particles/Gold", true, 0.3f);
        if (thread == null)
            return;

        Vector3 previous = start;
        Sequence flight = new Sequence();
        flight.Add(new Tween(t =>
        {
            if (thread == null)
                return;
            Vector3 a = Vector3.Lerp(start, control, t);
            Vector3 b = Vector3.Lerp(control, end, t);
            Vector3 point = Vector3.Lerp(a, b, t);
            Vector3 tangent = point - previous;
            if (tangent.sqrMagnitude < 0.000001f)
                tangent = b - a;
            thread.MoveHead(point, tangent);
            previous = point;
        }, 0f, 1f, duration, EaseType.EaseInQuad));
        flight.Add(new SequenceAction(() =>
        {
            if (thread != null)
                thread.Release();
        }));
        flight.Start();
    }
}

/// <summary>Plays a cast sound part-way into a spell's showing, without holding the queue.</summary>
public class CastSoundAnimation : AnimationAction
{
    readonly string sound;
    readonly float delay;

    public CastSoundAnimation(GameAction gameAction, string sound, float delay) : base(gameAction)
    {
        this.sound = sound;
        this.delay = delay;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);
        CuePlayer.Hold(delay, () => CuePlayer.PlayCast(sound));
        CallCallback();
    }
}
