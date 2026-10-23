using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationAction
{
    public bool Stackable = false;
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
            GameAction?.SourceEffect?.MarkTriggered();
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