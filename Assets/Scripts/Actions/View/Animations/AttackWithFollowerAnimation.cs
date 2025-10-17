using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AttackWithFollowerAnimation : AnimationAction
{
    AttackWithFollowerAction attackAction;

    private float attackMoveDuration = 0.18f;
    //private float moveDistance = 1f;
    private float cardSize = 3f;
    private ViewFollower attackerViewFollower;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Follower attacker;
    private ITarget target;

    public AttackWithFollowerAnimation(GameAction gameAction) : base(gameAction)
    {
        if (gameAction is AttackWithFollowerAction)
        {
            attackAction = (AttackWithFollowerAction)gameAction;
            attacker = attackAction.Attacker;
            target = attackAction.Target;
        }

    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (!View.Instance.TryGetViewFollower(attacker, out attackerViewFollower))
        {
            CallCallback();
            return;
        }
        startPosition = attackerViewFollower.transform.position;

        //Debug.LogWarning(attackAction.Attacker.GetName() + " attacked " + attackAction.Target.GetName() + " Animation start");

        Vector3 targetPosition = Vector3.zero;

        Card cardTarget = target as Card;
        Player playerTarget = target as Player;
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

        attackMoveDuration = View.Instance.IsHumansTurn ? 0.18f : 0.25f;
        Sequence attackSequence = new Sequence();
        attackSequence.Add(new Tween(MoveAttacker, 0, 1, attackMoveDuration));
        attackSequence.Add(new SequenceAction(ShakeScreen));
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

    protected override void Log()
    {
        if (attacker != null && attacker.Owner != null) Debug.LogWarning("AttackWithFollowerAnimation: " + attacker.Owner.GetName() + "'s " + attacker.GetName() + " attacks " + target.GetName());
    }

    private void AnimationOver()
    {
        //Debug.LogWarning(attackerViewFollower.Follower.GetName() + " attacked " + attackAction.Target.GetName() + " Animation end");
    }

    private void ShakeScreen()
    {
        Player playerTarget = target as Player;
        if (playerTarget != null && playerTarget.Health - attacker.CurrentAttack < 0)
        {
            ScreenShakeHandler.Shake(1f, 0.4f);
        }
        else
        {
            int enemyAttack = target is Follower targetFollower ? targetFollower.CurrentAttack : 0;
            int higherAttack = Mathf.Max(attacker.CurrentAttack, enemyAttack);
            float shakeDuration = Mathf.Min(0.15f + higherAttack * 0.05f, 0.4f);
            ScreenShakeHandler.Shake(higherAttack * 0.15f, 0.15f + higherAttack * 0.05f);
        }
    }
}
