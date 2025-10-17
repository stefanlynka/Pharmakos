using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// NOT USED
public class PlayFollowerAnimation : AnimationAction
{
    private PlayFollowerAction playFollowerAction;
    private Tween animationTween;
    Follower follower;

    private ViewCard ViewFollower;
    private Vector3 startPos;
    private Vector3 endPos;

    private float duration = 1; //0.25f;

    public PlayFollowerAnimation(GameAction gameAction) : base(gameAction)
    {
        if (gameAction is PlayFollowerAction)
        {
            playFollowerAction = gameAction as PlayFollowerAction;
            follower = playFollowerAction.Follower;
        }

    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (playFollowerAction == null)
        {
            CallCallback();
            return;
        }

        if (View.Instance.TryGetViewCard(follower, out ViewFollower))
        {
            startPos = ViewFollower.transform.position;
            endPos = View.Instance.GetViewPlayer(follower.Owner).BattleRow.GetPotentialFollowerPosition(playFollowerAction.Index);

            Sequence moveSequence = new Sequence();
            moveSequence.Add(new Tween(TweenProgress, 0, 1, duration));
            moveSequence.Add(new SequenceAction(Complete));
            moveSequence.Start();

            //animationTween?.Clear();
            //animationTween = new Tween(TweenProgress, 0, 1, duration);
            //animationTween.OnFinish = Complete;
            //animationTween.Start();
        }
    }

    private void TweenProgress(float progress)
    {
        ViewFollower.transform.position = startPos + (endPos - startPos) * progress;
    }

    private void Complete()
    {
        //View.Instance.MoveFollowerToBattleRow(follower, playFollowerAction.Index);
        //Debug.LogWarning(playFollowerAction.Follower.Owner.GetName() + " played " + playFollowerAction.Follower.GetName() + " " + playFollowerAction.Follower.ID + " Animation end");
        CallCallback();
    }

    protected override void Log()
    {
        if (follower != null) Debug.LogWarning("PlayFollowerAnimation: Played " + follower.GetName());
    }

}
