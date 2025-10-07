using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationAction
{
    public bool Stackable = false;
    public GameAction GameAction;
    public Action OnFinish;

    public virtual void Play(Action onFinish = null)
    {
        OnFinish = onFinish;
        if (Controller.AnimationDebugMode) Log();
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