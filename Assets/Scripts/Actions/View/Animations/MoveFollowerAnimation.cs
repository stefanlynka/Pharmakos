using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MoveFollowerAnimation : AnimationAction
{
    private Tween animationTween;

    private ViewCard viewCard;
    private Vector3 startPos;
    private Vector3 endPos;

    private float duration = 1f; //0.25f;

    private Follower follower;
    private int battleRowIndex;

    public MoveFollowerAnimation(GameAction gameAction, Follower follower, int battleRowIndex) : base(gameAction)
    {
        this.follower = follower;
        this.battleRowIndex = battleRowIndex;
    }

    public override void Play(Action onFinish = null)
    {
        OnFinish = onFinish;

        // If we can't find a ViewCard for this Follower
        if (!View.Instance.TryGetViewCard(follower, out viewCard))
        {
            // Make a new ViewCard
            viewCard = View.Instance.MakeNewViewCard(follower);
            // and put it offscreen to the right
            viewCard.transform.position = new Vector3(50, 0, 10);
        }
        else if (viewCard is ViewFollower viewFollower)
        {
            // Try removing it from ViewBattleRows
            View.Instance.Player1.BattleRow.TryRemoveFollower(viewFollower);
            View.Instance.Player2.BattleRow.TryRemoveFollower(viewFollower);
        }

        startPos = viewCard.transform.position;
        endPos = View.Instance.GetViewPlayer(follower.Owner).BattleRow.GetPotentialFollowerPosition(battleRowIndex);

        Sequence summonSequence = new Sequence();
        summonSequence.Add(new Tween(TweenProgress, 0, 1, duration));
        //skullSequence.Add(new Tween(TweenProgress, 1, 0, duration));
        summonSequence.Add(new SequenceAction(Complete));
        summonSequence.Start();
    }

    private void TweenProgress(float progress)
    {
        viewCard.transform.position = startPos + (endPos - startPos) * progress;
    }

    private void Complete()
    {
        OnFinish?.Invoke();
        View.Instance.MoveFollowerToBattleRow(follower, battleRowIndex);
    }
}
