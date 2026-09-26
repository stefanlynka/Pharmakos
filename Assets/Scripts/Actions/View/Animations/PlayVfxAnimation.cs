using System;
using UnityEngine;

public class PlayVfxAnimation : AnimationAction
{
    private VfxEffect effect;
    private int sourceID;
    private int targetID;

    public PlayVfxAnimation(GameAction gameAction, VfxEffect effect, ITarget source, ITarget target) : base(gameAction)
    {
        this.effect = effect;
        sourceID = source != null ? source.GetID() : -1;
        targetID = target != null ? target.GetID() : -1;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (effect == null)
        {
            CallCallback();
            return;
        }

        // Resolve at play time: the view may have changed since this animation was queued
        ViewTarget viewSource = sourceID >= 0 ? View.Instance.GetViewTargetByID(sourceID) : null;
        ViewTarget viewTarget = targetID >= 0 ? View.Instance.GetViewTargetByID(targetID) : null;

        effect.Play(new VfxContext(viewSource, viewTarget), CallCallback);
    }

    protected override void Log()
    {
        Debug.LogWarning("PlayVfxAnimation: " + (effect != null ? effect.GetType().Name : "null") + " source " + sourceID + " target " + targetID);
    }
}
