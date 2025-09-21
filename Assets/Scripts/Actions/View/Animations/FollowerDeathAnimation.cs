using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class FollowerDeathAnimation : AnimationAction
{
    private FollowerDeathAction followerDeathAction;
    private Tween animationTween;

    private ViewFollower ViewFollower;

    private float duration = 0.20f;

    public FollowerDeathAnimation(GameAction gameAction) : base(gameAction)
    {
        Stackable = true;

        if (gameAction is FollowerDeathAction) followerDeathAction = gameAction as FollowerDeathAction;
    }

    public override void Play(Action onFinish = null)
    {
        Debug.LogWarning(followerDeathAction.Follower.GetName() + " " + followerDeathAction.Follower.ID + " death Animation start");

        OnFinish = onFinish;

        if (followerDeathAction == null)
        {
            CallCallback();
            return;
        }

        if (!View.Instance.TryGetViewFollower(followerDeathAction.Follower, out ViewFollower))
        {
            CallCallback();
            return;
        }

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
}
