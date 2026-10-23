using System;
using UnityEngine;

public class RitualUsePulseAnimation : AnimationAction
{
    readonly int ritualID;

    public RitualUsePulseAnimation(GameAction gameAction, Ritual ritual) : base(gameAction)
    {
        ritualID = ritual != null ? ritual.GetID() : -1;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (ritualID >= 0 && View.Instance != null && View.Instance.GetViewTargetByID(ritualID) is ViewRitual viewRitual)
            viewRitual.PlayUsePulse();

        CallCallback();
    }

    protected override void Log()
    {
        Debug.LogWarning("RitualUsePulseAnimation: ritual " + ritualID);
    }
}
