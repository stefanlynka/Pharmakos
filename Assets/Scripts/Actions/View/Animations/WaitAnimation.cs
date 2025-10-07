using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class WaitAnimation : AnimationAction
{
    private float duration = 0.25f;


    public WaitAnimation(GameAction gameAction, float duration) : base(gameAction)
    {
        this.duration = duration;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

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
        CallCallback();
    }
    protected override void Log()
    {
        Debug.LogWarning("WaitAnimation Wait: " + duration + " seconds");
    }
}
