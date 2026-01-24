using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// DEPRECATED - Use MoveCardAnimation
/// </summary>
public class SummonFollowerAnimation : AnimationAction
{
    private SummonFollowerAction summonFollowerAction;
    private Tween animationTween;
    private Follower follower;

    private ViewCard ViewFollower;
    private Vector3 startPos;
    private Vector3 endPos;

    private float duration = 0.25f;

    public SummonFollowerAnimation(GameAction gameAction) : base(gameAction)
    {
        if (gameAction is SummonFollowerAction)
        {
            summonFollowerAction = gameAction as SummonFollowerAction;
            follower = summonFollowerAction.Follower;
        }

    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (summonFollowerAction == null)
        {
            CallCallback();
            return;
        }

        // If we can't find a ViewCard for this Follower
        if (!View.Instance.TryGetViewCard(follower, out ViewFollower))
        {
            // Make a new ViewCard
            ViewFollower = View.Instance.MakeNewViewCard(follower);
            // and put it offscreen to the right
            ViewFollower.transform.position = new Vector3(50, 0, 10);
        }

        startPos = ViewFollower.transform.position;
        endPos = View.Instance.GetViewPlayer(follower.Owner).BattleRow.GetPotentialFollowerPosition(summonFollowerAction.Index);

        Sequence summonSequence = new Sequence();
        summonSequence.Add(new Tween(TweenProgress, 0, 1, duration));
        //skullSequence.Add(new Tween(TweenProgress, 1, 0, duration));
        summonSequence.Add(new SequenceAction(Complete));
        summonSequence.Start();

        //skullSequence.Add
        //animationTween?.Clear();
        //animationTween = new Tween(TweenProgress, 0, 1, duration);
        //animationTween.OnFinish = Complete;
        //animationTween.Start();

    }

    private void TweenProgress(float progress)
    {
        ViewFollower.transform.position = startPos + (endPos - startPos) * progress;
    }

    private void Complete()
    {
        ViewFollower.transform.position = endPos;
        View.Instance.MoveFollowerToBattleRow(follower, summonFollowerAction.Index);

        CallCallback();
    }

    protected override void Log()
    {
        if (follower != null) Debug.LogWarning("SummonFollowerAnimation: Summon " + follower.GetName());
    }

}
