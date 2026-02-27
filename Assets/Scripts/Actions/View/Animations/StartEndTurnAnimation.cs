using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StartEndTurnAnimation : AnimationAction
{
    public StartEndTurnAnimation(GameAction gameAction) : base(gameAction)
    {
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        View.Instance.TurnIsEnding = true;

        CallCallback();
    }

    protected override void Log()
    {
        Debug.LogWarning("StartEndTurnAnimation");
    }
}
