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

        // End-of-turn offering resets apply to the view when the next turn banner appears.
        SyncResourceLabels();

        Sequence moveBannerSequence = new Sequence();
        moveBannerSequence.Add(new Tween(MoveBanner, 0, 1, moveDuration));
        moveBannerSequence.Add(new Tween(Wait, 0, 1, pauseDuration));
        moveBannerSequence.Add(new Tween(MoveBanner, 1, 0, moveDuration));
        moveBannerSequence.Add(new SequenceAction(AnimationOver));
        moveBannerSequence.Add(new SequenceAction(CallCallback));
        moveBannerSequence.Start();
    }

    private static void SyncResourceLabels()
    {
        if (View.Instance == null || Controller.Instance == null)
            return;

        SyncPlayerResources(Controller.Instance.Player1);
        SyncPlayerResources(Controller.Instance.Player2);
    }

    private static void SyncPlayerResources(Player player)
    {
        if (player == null || View.Instance == null)
            return;

        ViewPlayer viewPlayer = View.Instance.GetViewPlayer(player);
        if (viewPlayer?.ViewResources != null)
            viewPlayer.ViewResources.SyncToPlayerState();
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
    }
}
