using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowBannerAnimation : AnimationAction
{
    private RectTransform targetBanner;
    private bool playerBanner; // Is it the player's banner moving, or the AI's
    private Vector3 startPosition = new Vector3(0, 205, 0);
    private Vector3 endPosition = new Vector3(0, -15, 0);
    private float moveDuration = 0.3f;
    private float pauseDuration = 0.75f;

    public ShowBannerAnimation(GameAction gameAction, bool playerBanner) : base(gameAction)
    {
        this.playerBanner = playerBanner;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        targetBanner = playerBanner ? View.Instance.PlayerTurnBanner : View.Instance.AITurnBanner;

        View.Instance.IsHumansTurn = playerBanner;

        Sequence moveBannerSequence = new Sequence();
        moveBannerSequence.Add(new Tween(MoveBanner, 0, 1, moveDuration));
        moveBannerSequence.Add(new Tween(Wait, 0, 1, pauseDuration));
        moveBannerSequence.Add(new Tween(MoveBanner, 1, 0, moveDuration));
        moveBannerSequence.Add(new SequenceAction(AnimationOver));
        moveBannerSequence.Add(new SequenceAction(CallCallback));
        moveBannerSequence.Start();

        //Debug.LogError("Attack Animation");
    }

    private void MoveBanner(float progress)
    {
        targetBanner.anchoredPosition = Vector2.Lerp(startPosition, endPosition, progress);
    }
    private void Wait(float progress)
    {

    }

    protected override void Log()
    {
        Debug.LogWarning("ShowBannerAnimation: Moving Banner");
    }

    private void AnimationOver()
    {
        View.Instance.TurnIsEnding = false;
        View.Instance.WaitingForTurnBanner = false;
        View.Instance.DoingEndOfTurnActions = false;
        //Debug.LogWarning(attackerViewFollower.Follower.GetName() + " attacked " + attackAction.Target.GetName() + " Animation end");
    }
}
