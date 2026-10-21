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
    private bool hasReturned;
    private int pendingImpactAnimations;

    const string ArrowPrefabPath = "Prefabs/Misc/Arrow";
    const string ArrowShotSoundPath = "Audio/SFX/Arrow";
    const float RangedShotSpeed = 60f;
    const float MinRangedShotDuration = 0.22f;
    const float MaxRangedShotDuration = 1.35f;
    const float ShotBuryDistance = 0.35f;
    const float ShotClearMargin = 0.6f;

    static GameObject arrowPrefab;
    static AudioClip arrowShotClip;
    static bool loggedMissingArrowPrefab;
    static bool lookedForArrowShot;

    GameObject shotArrow;
    AttackArrowPreview.DirectArc shotArc;
    float shotImpactDistance;
    float shotArrowLength;
    bool shotImpactPlayed;
    Vector3 shotTipLocal = Vector3.left;
    Vector3 shotFaceLocal = Vector3.up;

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

        if (attacker.HasStaticEffect(StaticEffect.RangedAttacker))
        {
            PlayRangedShot();
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
        attackSequence.Add(new SequenceAction(OnReturned));
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

    // The card stays put. The arrow comes out of the same point as the preview,
    // follows that arc, and keeps going until it has cleared the far side of the target.
    private void PlayRangedShot()
    {
        if (!AttackArrowPreview.TryBuildShot(attackerViewFollower, target, out shotArc)
            || !TrySpawnShotArrow())
        {
            DoImpact();
            OnReturned();
            return;
        }

        ApplyShotClearance();
        MoveShot(0f);
        PlayArrowShotSound();

        float speed = View.Instance.IsHumansTurn ? RangedShotSpeed : RangedShotSpeed * 0.75f;
        float duration = Mathf.Clamp(shotArc.TotalLength / speed, MinRangedShotDuration, MaxRangedShotDuration);
        Sequence shot = new Sequence();
        shot.Add(new Tween(MoveShot, 0f, shotArc.TotalLength, duration));
        shot.Add(new SequenceAction(FinishShot));
        shot.Start();
    }

    private bool TrySpawnShotArrow()
    {
        if (arrowPrefab == null)
            arrowPrefab = Resources.Load<GameObject>(ArrowPrefabPath);
        if (arrowPrefab == null)
        {
            if (!loggedMissingArrowPrefab)
            {
                loggedMissingArrowPrefab = true;
                Debug.LogError("Missing shot arrow prefab at Resources/" + ArrowPrefabPath);
            }
            return false;
        }

        shotArrow = UnityEngine.Object.Instantiate(arrowPrefab);
        shotArrow.name = "ShotArrow";
        CalibrateShotArrow(shotArrow.transform);
        return true;
    }

    private void CalibrateShotArrow(Transform arrow)
    {
        Quaternion rotation = arrow.rotation;
        arrow.rotation = Quaternion.identity;

        Renderer renderer = arrow.GetComponentInChildren<Renderer>();
        Vector3 size = renderer != null ? renderer.localBounds.size : Vector3.zero;
        if (renderer != null && size.sqrMagnitude > 0.0001f)
        {
            int longAxis = 0;
            if (size.y > size[longAxis]) longAxis = 1;
            if (size.z > size[longAxis]) longAxis = 2;
            int thinAxis = 0;
            if (size.y < size[thinAxis]) thinAxis = 1;
            if (size.z < size[thinAxis]) thinAxis = 2;
            if (longAxis == thinAxis)
                thinAxis = (longAxis + 1) % 3;

            // This mesh comes to a point on the negative end of its long axis.
            Vector3 meshTip = Vector3.zero;
            meshTip[longAxis] = -1f;
            Vector3 meshFace = Vector3.zero;
            meshFace[thinAxis] = 1f;

            shotTipLocal = arrow.InverseTransformDirection(renderer.transform.TransformDirection(meshTip)).normalized;
            shotFaceLocal = arrow.InverseTransformDirection(renderer.transform.TransformDirection(meshFace)).normalized;

            Vector3 lossy = renderer.transform.lossyScale;
            shotArrowLength = Mathf.Max(
                Mathf.Abs(size.x * lossy.x),
                Mathf.Abs(size.y * lossy.y),
                Mathf.Abs(size.z * lossy.z));
        }

        arrow.rotation = rotation;
        if (shotArrowLength < 0.05f)
            shotArrowLength = 1.5f;
    }

    private void ApplyShotClearance()
    {
        float half = shotArrowLength * 0.5f;
        shotArc.LeadLength = half + ShotBuryDistance;
        shotArc.LeadStart = shotArc.Launch - shotArc.ExitDirection * shotArc.LeadLength;

        float halfDepth = 0.75f;
        if (shotArc.HasTargetBounds)
        {
            Vector3 extents = shotArc.TargetBounds.extents;
            Vector3 arrive = shotArc.ArriveDirection;
            halfDepth = Mathf.Abs(extents.x * arrive.x)
                + Mathf.Abs(extents.y * arrive.y)
                + Mathf.Abs(extents.z * arrive.z);
        }

        shotArc.TailLength = halfDepth + half + ShotClearMargin;
        shotArc.TailEnd = shotArc.End + shotArc.ArriveDirection * shotArc.TailLength;
        shotImpactDistance = shotArc.LeadLength + shotArc.ArcLength - half;
    }

    private void MoveShot(float distance)
    {
        if (shotArrow == null)
            return;

        if (!shotImpactPlayed && distance >= shotImpactDistance)
        {
            shotImpactPlayed = true;
            DoImpact();
        }

        if (!shotArc.Sample(distance, out Vector3 position, out Vector3 tangent))
            return;

        Vector3 face = Vector3.ProjectOnPlane(shotArc.BoardNormal, tangent);
        if (face.sqrMagnitude < 0.04f)
            face = Vector3.ProjectOnPlane(attackerViewFollower.transform.up, tangent);
        if (face.sqrMagnitude < 0.0001f)
            return;

        shotArrow.transform.position = position;
        shotArrow.transform.rotation = AttackArrowPreview.AlignArrow(shotTipLocal, tangent, shotFaceLocal, face);
    }

    private void FinishShot()
    {
        if (!shotImpactPlayed)
        {
            shotImpactPlayed = true;
            DoImpact();
        }

        if (shotArrow != null)
        {
            UnityEngine.Object.Destroy(shotArrow);
            shotArrow = null;
        }

        OnReturned();
    }

    private void PlayArrowShotSound()
    {
        if (!lookedForArrowShot)
        {
            lookedForArrowShot = true;
            arrowShotClip = Resources.Load<AudioClip>(ArrowShotSoundPath);
            if (arrowShotClip == null)
                Debug.LogError("Missing arrow shot at Resources/" + ArrowShotSoundPath);
        }

        if (arrowShotClip != null && View.Instance.AudioHandler != null)
            View.Instance.AudioHandler.PlayOneShot(arrowShotClip);
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

        PlayImpactAnimations();
    }

    // Pulls this attack's combat damage animations out of the queue so they start at the moment of contact
    // instead of waiting for the attacker to return. The attack only finishes once they have finished too,
    // so later queued animations (deaths, triggers) keep their order.
    private void PlayImpactAnimations()
    {
        List<AnimationAction> impactAnimations = View.Instance.AnimationHandler.TakeQueuedAnimations(
            IsThisAttacksDamageAnimation,
            animationAction => animationAction is AttackWithFollowerAnimation);

        pendingImpactAnimations = impactAnimations.Count;
        foreach (AnimationAction impactAnimation in impactAnimations)
        {
            impactAnimation.Play(OnImpactAnimationComplete);
        }
    }

    private bool IsThisAttacksDamageAnimation(AnimationAction animationAction)
    {
        return animationAction is ChangeStatsAnimation
            && animationAction.GameAction is DealDamageAction damageAction
            && damageAction.IsCombatDamage
            && (ReferenceEquals(damageAction.Source, attacker) || ReferenceEquals(damageAction.Target, attacker));
    }

    private void OnImpactAnimationComplete()
    {
        pendingImpactAnimations--;
        TryFinish();
    }

    private void OnReturned()
    {
        hasReturned = true;
        TryFinish();
    }

    private void TryFinish()
    {
        if (hasReturned && pendingImpactAnimations <= 0)
            CallCallback();
    }
}
