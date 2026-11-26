using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationAction
{
    public bool Stackable = false;
    /// <summary>Nothing queued after this starts until it finishes, even stackable animations (a trigger cue's beat).</summary>
    public bool HoldsFollowing = false;
    public bool MarkSourceTriggered = false;
    public GameAction GameAction;
    public Action OnFinish;

    public virtual void Play(Action onFinish = null)
    {
        OnFinish = onFinish;
        if (Controller.AnimationDebugMode) Log();
        if (MarkSourceTriggered)
        {
            MarkSourceTriggered = false;
            GameAction?.SourceEffect?.FlareTriggered();
        }
    }

    public AnimationAction(GameAction gameAction)
    {
        GameAction = gameAction;
    }
    protected void CallCallback()
    {
        if (OnFinish != null)
        {
            OnFinish();
            OnFinish = null;
        }
    }
    protected virtual void Log()
    {

    }
}

// Holds a trinket flare in the animation queue when the triggering action has no animation of its own.
public class MarkSourceTriggeredAnimation : AnimationAction
{
    public MarkSourceTriggeredAnimation(GameAction gameAction) : base(gameAction)
    {
        MarkSourceTriggered = true;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);
        CallCallback();
    }
}

// Flares a trinket or ritual buff that acted without an action of its own (StaticPlayerEffect.MarkTriggered), in turn
// with the other animations. Instant and stackable, so it never holds the queue.
public class EffectFlareAnimation : AnimationAction
{
    readonly StaticPlayerEffect effect;
    readonly Action<StaticPlayerEffect> flare;

    public EffectFlareAnimation(StaticPlayerEffect effect, Action<StaticPlayerEffect> flare) : base(null)
    {
        this.effect = effect;
        this.flare = flare;
        Stackable = true;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);
        flare?.Invoke(effect);
        CallCallback();
    }
}
