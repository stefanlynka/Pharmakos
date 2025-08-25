using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class WaitAnimation : AnimationAction
{
    private float duration = 0.25f;


    public WaitAnimation(GameAction gameAction, float duration) : base(gameAction)
    {
        this.duration = duration;
    }

    public override void Play(Action onFinish = null)
    {
        OnFinish = onFinish;


        Sequence moveSequence = new Sequence();
        moveSequence.Add(new Tween(TweenProgress, 0, 1, duration));
        moveSequence.Add(new SequenceAction(Complete));
        moveSequence.Start();
    }

    private void TweenProgress(float progress)
    {
        // Do nothing
    }

    private void Complete()
    {
        OnFinish?.Invoke();
    }
}
