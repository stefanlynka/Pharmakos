using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AttackWithFollowerAnimation : AnimationAction
{
    AttackWithFollowerAction attackAction;

    private float attackMoveDuration = 0.18f;
    private float cardSize = 3f;
    private float attackLiftHeight = 1.2f;
    private ViewFollower attackerViewFollower;
    private Transform unitHolder;
    private Vector3 startLocal;
    private Vector3 endLocal;
    private Follower attacker;
    private ITarget target;

    public AttackWithFollowerAnimation(GameAction gameAction) : base(gameAction)
    {
        if (gameAction is AttackWithFollowerAction attackWithFollowerAction)
        {
            attackAction = attackWithFollowerAction;
            attacker = attackAction.Attacker;
            target = attackAction.Target;
        }
        else if (gameAction is PreAttackWithFollowerAction preAttackAction)
        {
            attacker = preAttackAction.Attacker;
            target = preAttackAction.Target;
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
        ViewBattleRow attackerBattleRow = attacker.Owner.IsHuman
            ? View.Instance.Player1.BattleRow
            : View.Instance.Player2.BattleRow;
        unitHolder = attackerBattleRow.UnitHolderTransform;

        startLocal = attackerViewFollower.transform.localPosition;

        Vector3 targetWorldPosition = Vector3.zero;

        Card cardTarget = target as Card;
        Player playerTarget = target as Player;
        if (cardTarget != null && View.Instance.TryGetViewFollower(cardTarget, out ViewFollower targetViewFollower))
        {
            targetWorldPosition = targetViewFollower.transform.position;
        }
        else if (playerTarget != null)
        {
            ViewPlayer targetViewPlayer = View.Instance.GetViewPlayer(playerTarget);
            targetWorldPosition = targetViewPlayer.transform.position;
        }

        Vector3 targetLocal = unitHolder.InverseTransformPoint(targetWorldPosition);
        targetLocal.z = startLocal.z;

        float distanceBetweenTargets = Vector3.Distance(startLocal, targetLocal);
        float distanceToTargetPercent = distanceBetweenTargets != 0 ? (distanceBetweenTargets - cardSize) / distanceBetweenTargets : 0;

        endLocal = Vector3.Lerp(startLocal, targetLocal, distanceToTargetPercent);
        endLocal.z = startLocal.z;

        attackMoveDuration = View.Instance.IsHumansTurn ? 0.18f : 0.25f;
        Sequence attackSequence = new Sequence();
        attackSequence.Add(new Tween(MoveAttackerForward, 0, 1, attackMoveDuration, EaseType.EaseInBack));
        attackSequence.Add(new SequenceAction(DoImpact));
        attackSequence.Add(new Tween(MoveAttacker, 1, 0, attackMoveDuration));
        //attackSequence.Add(new SequenceAction(AnimationOver));
        attackSequence.Add(new SequenceAction(CallCallback));
        attackSequence.Start();

        //Debug.LogError("Attack Animation");
    }

    private bool hasPlayedSound = false;

    private void SetAttackerPosition(float progress, bool applyLift)
    {
        Vector3 pos = Vector3.Lerp(startLocal, endLocal, progress);
        pos.z = startLocal.z;
        if (applyLift)
            pos.y += attackLiftHeight * (1f - progress);

        attackerViewFollower.transform.localPosition = pos;
    }

    private void MoveAttackerForward(float progress)
    {
        SetAttackerPosition(progress, applyLift: true);
        if (!hasPlayedSound)
        {
            hasPlayedSound = true;

            View.Instance.AudioHandler.PlaySoundEffect(AudioHandler.SoundEffectType.Impact);
        }
    }
    private void MoveAttacker(float progress)
    {
        SetAttackerPosition(progress, applyLift: false);
    }

    protected override void Log()
    {
        if (attacker != null && attacker.Owner != null) Debug.LogWarning("AttackWithFollowerAnimation: " + attacker.Owner.GetName() + "'s " + attacker.GetName() + " attacks " + target.GetName());
    }

    private void AnimationOver()
    {
        //Debug.LogWarning(attackerViewFollower.Follower.GetName() + " attacked " + attackAction.Target.GetName() + " Animation end");
    }

    private void DoImpact()
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
