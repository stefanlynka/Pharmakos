using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class DelayStartOfAnimation : AnimationAction
{
    private float duration;


    public DelayStartOfAnimation(GameAction gameAction, float duration) : base(gameAction)
    {
        this.duration = duration;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        View.Instance.AnimationHandler.AnimationsDelayed = true;

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
        View.Instance.AnimationHandler.AnimationsDelayed = false;
        CallCallback();
    }
    protected override void Log()
    {
        Debug.LogWarning("Delay Animation Start: " + duration + " seconds");
    }
}
