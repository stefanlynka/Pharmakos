using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StartFightAnimation : AnimationAction
{
    FightBanner fightBanner;
    private RectTransform targetBanner;
    private int fightNum;
    private Vector3 startPosition = new Vector3(0, 205, 0);
    private Vector3 endPosition = new Vector3(0, -100, 0);
    private float darknessLevel = 1f;
    //private float darknessShowUpDuration = 0.3f;
    private float moveDuration = 0.35f; // 0.45f
    private float pauseDuration = 1.2f;

    public StartFightAnimation(GameAction gameAction, int fightNumber) : base(gameAction)
    {
        fightNum = fightNumber;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        fightBanner = View.Instance.FightNumberBanner;
        targetBanner = fightBanner.RectTransform;

        fightBanner.SetText(fightNum);

        SetDarkness(darknessLevel);

        Sequence moveBannerSequence = new Sequence();
        //moveBannerSequence.Add(new Tween(Wait, 0, 1, darknessShowUpDuration));
        moveBannerSequence.Add(new Tween(MoveBanner, 0, 1, moveDuration));
        moveBannerSequence.Add(new Tween(Wait, 0, 1, pauseDuration));
        moveBannerSequence.Add(new Tween(MoveBanner, 1, 0, moveDuration));
        moveBannerSequence.Add(new Tween(Wait, 0, 1, 0.1f)); // buffer
        moveBannerSequence.Add(new SequenceAction(AnimationOver));
        moveBannerSequence.Add(new SequenceAction(CallCallback));
        moveBannerSequence.Start();

        Sequence darknessSequence = new Sequence();
        //darknessSequence.Add(new Tween(SetDarkness, 0, darknessLevel, darknessShowUpDuration));
        darknessSequence.Add(new Tween(Wait, 0, 1, pauseDuration + moveDuration));
        darknessSequence.Add(new Tween(SetDarkness, darknessLevel, 0, moveDuration));
        darknessSequence.Start();

        //Debug.LogError("Attack Animation");
    }

    private void MoveBanner(float progress)
    {
        targetBanner.anchoredPosition = Vector2.Lerp(startPosition, endPosition, progress);
    }
    private void Wait(float progress)
    {

    }
    private void SetDarkness(float progress)
    {
        View.Instance.DarknessHandler.SetDarkness(progress);
    }

    protected override void Log()
    {
        Debug.LogWarning("ShowBannerAnimation: Moving Banner");
    }

    private void AnimationOver()
    {
        View.Instance.TurnIsEnding = false;
        View.Instance.DoingEndOfTurnActions = false;
        //Debug.LogWarning(attackerViewFollower.Follower.GetName() + " attacked " + attackAction.Target.GetName() + " Animation end");
    }
}
