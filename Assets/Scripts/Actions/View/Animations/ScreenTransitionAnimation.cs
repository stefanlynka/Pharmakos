using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreenTransitionAnimation : AnimationAction
{
    private float pauseDuration = 1.1f;
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

        ScreenHandler.Instance.ShowScreen(ScreenName.Blank, false, false);

        Sequence waitSequence = new Sequence();
        waitSequence.Add(new Tween(Wait, 0, 1, pauseDuration));
        waitSequence.Add(new SequenceAction(HideBlankScreen));
        waitSequence.Add(new SequenceAction(Halfway));
        waitSequence.Add(new Tween(Wait, 0, 1, pauseDuration));
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
        halfwayAction?.Invoke();
    }
    private void AnimationOver()
    {
        onFinishAction?.Invoke();
    }
}
