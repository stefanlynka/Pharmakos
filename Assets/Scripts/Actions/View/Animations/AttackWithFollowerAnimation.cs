using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AttackWithFollowerAnimation : AnimationAction
{
    AttackWithFollowerAction attackAction;

    private float attackMoveDuration = 0.25f;
    //private float moveDistance = 1f;
    private float cardSize = 3f;
    private ViewFollower attackerViewFollower;
    private Vector3 startPosition;
    private Vector3 endPosition;

    public AttackWithFollowerAnimation(GameAction gameAction) : base(gameAction)
    {
        if (gameAction is AttackWithFollowerAction) attackAction = (AttackWithFollowerAction)gameAction;

    }

    public override void Play(Action onFinish = null)
    {
        OnFinish = onFinish;
        if (!View.Instance.TryGetViewFollower(attackAction.Attacker, out attackerViewFollower))
        {
            CallCallback();
            return;
        }
        startPosition = attackerViewFollower.transform.position;

        //Debug.LogWarning(attackAction.Attacker.GetName() + " attacked " + attackAction.Target.GetName() + " Animation start");

        Vector3 targetPosition = Vector3.zero;

        Card cardTarget = attackAction.Target as Card;
        Player playerTarget = attackAction.Target as Player;
        if (cardTarget != null && View.Instance.TryGetViewFollower(cardTarget, out ViewFollower targetViewFollower))
        {
            targetPosition = targetViewFollower.transform.position;
        }
        else if (playerTarget != null)
        {
            ViewPlayer targetViewPlayer = View.Instance.GetViewPlayer(playerTarget);
            targetPosition = targetViewPlayer.transform.position;
        }

        float distanceBetweenTargets = Vector2.Distance(startPosition, targetPosition);
        float distanceToTargetPercent = distanceBetweenTargets != 0 ? (distanceBetweenTargets - cardSize) / distanceBetweenTargets : 0;

        endPosition = Vector2.Lerp(startPosition, targetPosition, distanceToTargetPercent);

        Sequence attackSequence = new Sequence();
        attackSequence.Add(new Tween(MoveAttacker, 0, 1, attackMoveDuration));
        attackSequence.Add(new Tween(MoveAttacker, 1, 0, attackMoveDuration));
        //attackSequence.Add(new SequenceAction(AnimationOver));
        attackSequence.Add(new SequenceAction(CallCallback));
        attackSequence.Start();

        //Debug.LogError("Attack Animation");
    }

    private void MoveAttacker(float progress)
    {
        attackerViewFollower.transform.position = Vector2.Lerp(startPosition, endPosition, progress);
    }

    private void AnimationOver()
    {
        //Debug.LogWarning(attackerViewFollower.Follower.GetName() + " attacked " + attackAction.Target.GetName() + " Animation end");
    }
}
