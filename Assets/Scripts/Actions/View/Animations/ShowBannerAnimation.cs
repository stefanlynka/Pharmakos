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


        Sequence attackSequence = new Sequence();
        attackSequence.Add(new Tween(MoveBanner, 0, 1, moveDuration));
        attackSequence.Add(new Tween(Wait, 0, 1, pauseDuration));
        attackSequence.Add(new Tween(MoveBanner, 1, 0, moveDuration));
        attackSequence.Add(new SequenceAction(AnimationOver));
        attackSequence.Add(new SequenceAction(CallCallback));
        attackSequence.Start();

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
        View.Instance.IsHumansTurn = playerBanner;
        //Debug.LogWarning(attackerViewFollower.Follower.GetName() + " attacked " + attackAction.Target.GetName() + " Animation end");
    }
}
