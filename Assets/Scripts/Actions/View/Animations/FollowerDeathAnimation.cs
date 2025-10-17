using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FollowerDeathAnimation : AnimationAction
{
    private ViewFollower ViewFollower;
    private Follower follower;
    private float duration = 0.2f;

    public FollowerDeathAnimation(GameAction gameAction, Follower follower) : base(gameAction)
    {
        Stackable = true;

        this.follower = follower;

    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (follower == null)
        {
            CallCallback();
            return;
        }

        if (!View.Instance.TryGetViewFollower(follower, out ViewFollower))
        {
            CallCallback();
            return;
        }

        duration = View.Instance.IsHumansTurn ? 0.2f : 0.3f;
        Sequence skullSequence = new Sequence();
        skullSequence.Add(new Tween(UpdateSkullAlpha, 0, 1, duration));
        skullSequence.Add(new Tween(UpdateSkullAlpha, 1, 0, duration*2));
        skullSequence.Add(new SequenceAction(RemoveFollower)); 
        skullSequence.Add(new SequenceAction(CallCallback));
        skullSequence.Start();
    }

    private void UpdateSkullAlpha(float progress)
    {
        if (ViewFollower == null) return;

        Color newColor = ViewFollower.SkullRenderer.color;
        newColor.a = progress;
        ViewFollower.SkullRenderer.color = newColor;
    }

    private void RemoveFollower()
    {
        //Debug.LogWarning(ViewFollower.Follower.GetName() + " " + ViewFollower.Follower.ID + " death Animation end");
        View.Instance.RemoveViewCard(ViewFollower);
    }

    protected override void Log()
    {
        if (follower != null && follower.Owner != null) Debug.LogWarning("FollowerDeathAnimation: " + follower .Owner.GetName() + "'s " + follower.GetName() + " dies");
    }
}
