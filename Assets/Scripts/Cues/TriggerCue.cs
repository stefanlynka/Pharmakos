using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One firing of a follower's triggered ability, for presentation only (canon game, never in AI simulations):
/// the follower flashes, its cue sound plays and the queue holds for a short beat, just before what the ability did.
/// ActionHandler opens one per trigger scope and stamps the actions added inside it (see Cues/CLAUDE.md).
///
/// Where it lands in the animation queue: right before the animations of its first action (its visible result). An
/// ability that acted without an action of its own (a reward stored for next turn) plays right after the action that
/// caused it; one whose actions all failed plays once they have run. A follower's own On Death cue is placed the
/// moment it fires, so it plays after the lethal damage and before the death animation. A cue whose ability did
/// nothing (no actions, no delayed reward) never plays.
/// </summary>
public class TriggerCue
{
    public const string StartOfTurn = "StartOfTurn";
    public const string EndOfTurn = "EndOfTurn";

    public readonly Follower Source;
    public readonly string SourceName;
    public readonly string Trigger;
    /// <summary>The ability's effect def class (keyword-like abilities resolve by it, e.g. LifeSiphonEffectDef).</summary>
    public readonly string EffectDefName;
    public readonly bool ViaAlly;
    public readonly GameAction CausingAction;
    public readonly List<GameAction> Actions = new List<GameAction>();

    public bool Acted { get; private set; }
    public bool Placed { get; private set; }

    TriggerCueAnimation animation;
    bool placedOnOpen;
    bool closed;
    int pendingActions;
    CueDef def;

    public TriggerCue(Follower source, string trigger, bool viaAlly, GameAction causingAction, string effectDefName = null)
    {
        Source = source;
        SourceName = source != null ? source.GetType().Name : "";
        Trigger = trigger;
        EffectDefName = effectDefName;
        ViaAlly = viaAlly;
        CausingAction = causingAction;
    }

    public CueDef Def => def ??= CueLibrary.Resolve(this);

    bool PlacesOnOpen => Trigger == nameof(EffectTrigger.OnDeath) && !ViaAlly;

    static AnimationHandler Queue => View.Instance != null ? View.Instance.AnimationHandler : null;

    public void Open()
    {
        if (!PlacesOnOpen || Queue == null)
            return;

        animation = new TriggerCueAnimation(this);
        Queue.AddAnimationActionToQueue(animation);
        Placed = true;
        placedOnOpen = true;
    }

    public void AddAction(GameAction action)
    {
        action.Cue = this;
        Actions.Add(action);
        pendingActions++;
        Acted = true;
    }

    /// <summary>One of this cue's actions has executed. If none of them queued an animation, place the cue now.</summary>
    public void ActionExecuted()
    {
        pendingActions--;
        if (closed && pendingActions <= 0)
            PlaceNow();
    }

    /// <summary>For abilities that act without an action of their own right now (a reward stored for next turn).</summary>
    public void MarkActed()
    {
        Acted = true;
    }

    /// <summary>Called by AnimationHandler when one of this cue's actions queues its animations.</summary>
    public AnimationAction TakeAnimationForPlacement()
    {
        if (Placed)
            return null;

        Placed = true;
        return animation ??= new TriggerCueAnimation(this);
    }

    /// <summary>Scope closed. Returns true when the cue still has to be placed after its causing action.</summary>
    public bool Close()
    {
        closed = true;
        if (!Acted)
        {
            if (placedOnOpen && animation != null)
                Queue?.AnimationActionQueue.Remove(animation);
            Placed = true;
            return false;
        }

        if (Placed)
            return false;

        // Its actions place it when they animate (or once they have all run).
        if (Actions.Count > 0)
        {
            if (pendingActions <= 0)
                PlaceNow();
            return false;
        }

        if (CausingAction == null)
        {
            PlaceNow();
            return false;
        }

        return true;
    }

    public void PlaceNow()
    {
        if (Placed || Queue == null)
            return;

        Placed = true;
        animation ??= new TriggerCueAnimation(this);
        Queue.AddAnimationActionToQueue(animation);
    }
}

/// <summary>A triggered ability's flash: the source follower glows votive gold and pulses, its sound plays, a beat.</summary>
public class TriggerCueAnimation : AnimationAction
{
    readonly TriggerCue cue;

    public TriggerCueAnimation(TriggerCue cue) : base(null)
    {
        this.cue = cue;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        CueDef def = cue.Def;
        int repeat = CuePlayer.Play(def.Sound);

        ViewFollower view = null;
        if (cue.Source != null && View.Instance != null)
            View.Instance.TryGetViewFollower(cue.Source, out view);

        float hold = CuePlayer.Beat(repeat) * def.BeatScale;
        if (view != null && def.Flash)
        {
            float pulse = TriggerFlash.Play(view, repeat);
            // A dying follower's card is about to be moved by its death animation: let the pulse finish first.
            if (!cue.Source.Alive)
                hold = Mathf.Max(hold, pulse);
        }

        if (def.Extra != null)
            hold = Mathf.Max(hold, def.Extra(cue, view));

        CuePlayer.Hold(hold, CallCallback);
    }

    protected override void Log()
    {
        Debug.LogWarning("TriggerCueAnimation: " + cue.SourceName + " " + cue.Trigger + " -> " + cue.Def.Sound);
    }
}

/// <summary>The trigger flash on a card: a votive-gold flare of its glow and a small scale pulse of its CardHolder.</summary>
public static class TriggerFlash
{
    public const float FlareDuration = 0.45f;
    const float PulseDuration = 0.22f;
    const float PulseScale = 1.07f;

    class PulseState
    {
        public Vector3 Resting;
        public int Running;
    }

    static readonly Dictionary<Transform, PulseState> pulses = new Dictionary<Transform, PulseState>();

    /// <summary>Returns the pulse's length in seconds.</summary>
    public static float Play(ViewCard view, int repeat = 0)
    {
        if (view == null)
            return 0f;

        HighlightPalette palette = HighlightPalette.Instance;
        if (view.CardHighlightEffect != null)
        {
            if (palette != null && palette.Triggered != null)
                view.CardHighlightEffect.Flare(palette.Triggered, FlareDuration);
            else
                view.CardHighlightEffect.Flare();
        }

        Transform holder = view.CardHolder != null ? view.CardHolder.transform : null;
        if (holder == null)
            return 0f;

        // Repeats in a fast chain pulse a little less, so a combo reads as a ripple rather than a shake.
        float amount = (PulseScale - 1f) * Mathf.Max(0.45f, Mathf.Pow(0.8f, repeat));
        return Pulse(holder, 1f + amount, PulseDuration);
    }

    /// <summary>Scales the transform up and back over the duration; overlapping pulses share one resting scale.</summary>
    public static float Pulse(Transform target, float peakScale, float duration)
    {
        if (target == null)
            return 0f;

        if (!pulses.TryGetValue(target, out PulseState state))
        {
            state = new PulseState { Resting = target.localScale };
            pulses[target] = state;
        }
        state.Running++;

        Sequence sequence = new Sequence();
        sequence.Add(new Tween(t =>
        {
            if (target == null)
                return;
            target.localScale = state.Resting * Mathf.Lerp(1f, peakScale, Mathf.Sin(t * Mathf.PI));
        }, 0f, 1f, duration));
        sequence.Add(new SequenceAction(() =>
        {
            state.Running--;
            if (state.Running > 0)
                return;
            if (target != null)
                target.localScale = state.Resting;
            pulses.Remove(target);
        }));
        sequence.Start();
        return duration;
    }
}
