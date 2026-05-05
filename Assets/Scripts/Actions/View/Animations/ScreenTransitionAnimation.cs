using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreenTransitionAnimation : AnimationAction
{
    // private float fadeDuration = 0.8f;
    private const float halfwayPauseDuration = 0.25f;
    public float FadeOutDuration = -1f;
    public float FadeInDuration = -1f;
    private Action halfwayAction;
    private Action onFinishAction;
    public ScreenTransitionAnimation(GameAction gameAction, Action halfwayAction = null, Action onFinishAction = null) : base(gameAction)
    {
        this.halfwayAction = halfwayAction;
        this.onFinishAction = onFinishAction;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (FadeOutDuration < 0) FadeOutDuration = Screen.ScreenChangeDuration;
        if (FadeInDuration < 0) FadeInDuration = Screen.ScreenChangeDuration;

        //Debug.LogWarning("Starting transition");
        ScreenHandler.Instance.ShowScreen(ScreenName.Blank, false, false);

        Sequence waitSequence = new Sequence();
        waitSequence.Add(new Tween(Wait, 0, 1, FadeOutDuration));
        waitSequence.Add(new SequenceAction(Halfway));
        waitSequence.Add(new Tween(Wait, 0, 1, halfwayPauseDuration));
        waitSequence.Add(new SequenceAction(HideBlankScreen));
        waitSequence.Add(new Tween(Wait, 0, 1, FadeInDuration));
        waitSequence.Add(new SequenceAction(AnimationOver));
        waitSequence.Add(new SequenceAction(CallCallback));
        waitSequence.Start();
    }

    private void Wait(float progress)
    {

    }

    protected override void Log()
    {
        //Debug.LogWarning("ScreenTransitionAnimation");
    }
    private void HideBlankScreen()
    {
        ScreenHandler.Instance.HideScreen(ScreenName.Blank, false);
    }
    private void Halfway()
    {
        //Debug.LogWarning("Halways through transition");
        halfwayAction?.Invoke();
    }
    private void AnimationOver()
    {
        //Debug.LogWarning("Finished transition");
        onFinishAction?.Invoke();
    }
}
