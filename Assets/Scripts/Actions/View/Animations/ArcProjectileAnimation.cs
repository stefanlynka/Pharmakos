using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How a prefab is thrown along the attack-preview arc.
/// The object is removed once it has passed through the target.
/// </summary>
public class ArcProjectileStyle
{
    public string PrefabPath;
    public string SoundPath;
    public float ArcHeight = AttackArrowPreview.ArcHeight;
    public bool KeepArcHeight;
    public bool AlignToPath;
    public bool Spin;
    public float SpinDegreesPerUnit = 90f;
    public float Speed = 18f;
    public float MinDuration = 0.22f;
    public float MaxDuration = 1.35f;

    public static readonly ArcProjectileStyle Arrow = new ArcProjectileStyle
    {
        PrefabPath = "Prefabs/Misc/Arrow",
        SoundPath = "Audio/SFX/Arrow",
        ArcHeight = AttackArrowPreview.ArcHeight,
        AlignToPath = true,
        Speed = 60f,
    };

    public static readonly ArcProjectileStyle Stone = new ArcProjectileStyle
    {
        PrefabPath = "Prefabs/Misc/Stone",
        SoundPath = "Audio/SFX/Arrow",
        ArcHeight = AttackArrowPreview.SpellArcHeight,
        Spin = true,
        Speed = 40f,
    };
}

/// <summary>
/// Flies one prefab from a source to a target. Damage numbers for the same action
/// can be held until the object reaches the target.
/// </summary>
public class ArcProjectileAnimation : AnimationAction
{
    const float BuryDistance = 0.35f;
    const float ClearMargin = 0.6f;

    static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    static readonly Dictionary<string, AudioClip> sounds = new Dictionary<string, AudioClip>();
    static readonly HashSet<string> loggedMissing = new HashSet<string>();

    readonly ArcProjectileStyle style;
    readonly ITarget source;
    readonly ITarget target;
    readonly bool pullDamageOnImpact;
    Predicate<AnimationAction> damageMatch;

    public Action OnReachedTarget;

    public void MatchDamage(Predicate<AnimationAction> match)
    {
        damageMatch = match;
    }

    GameObject projectile;
    AttackArrowPreview.DirectArc arc;
    float impactDistance;
    float projectileLength;
    bool reached;
    bool flightDone;
    int pendingDamageAnimations;
    bool align;
    Vector3 tipLocal = Vector3.left;
    Vector3 faceLocal = Vector3.up;
    Quaternion baseRotation = Quaternion.identity;
    Vector3 spinAxis = Vector3.up;

    public ArcProjectileAnimation(GameAction gameAction, ArcProjectileStyle style, ITarget source, ITarget target, bool pullDamageOnImpact = false) : base(gameAction)
    {
        this.style = style;
        this.source = source;
        this.target = target;
        this.pullDamageOnImpact = pullDamageOnImpact;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (style == null
            || !AttackArrowPreview.TryBuildShot(source, target, style.ArcHeight, style.KeepArcHeight, out arc)
            || !TrySpawn())
        {
            ReachTarget();
            FinishFlight();
            return;
        }

        ApplyClearance();
        Place(0f);
        PlaySound();

        float speed = View.Instance.IsHumansTurn ? style.Speed : style.Speed * 0.75f;
        float duration = Mathf.Clamp(arc.TotalLength / Mathf.Max(0.01f, speed), style.MinDuration, style.MaxDuration);
        Sequence shot = new Sequence();
        shot.Add(new Tween(Place, 0f, arc.TotalLength, duration));
        shot.Add(new SequenceAction(FinishFlight));
        shot.Start();
    }

    bool TrySpawn()
    {
        GameObject prefab = LoadPrefab(style.PrefabPath);
        if (prefab == null)
            return false;

        projectile = UnityEngine.Object.Instantiate(prefab);
        projectile.name = prefab.name + "Shot";
        Collider[] colliders = projectile.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        baseRotation = projectile.transform.rotation;
        Measure(projectile.transform);
        if (style.Spin)
        {
            spinAxis = Vector3.Cross(arc.ExitDirection, arc.BoardNormal);
            if (spinAxis.sqrMagnitude < 0.0001f)
                spinAxis = Vector3.up;
            spinAxis.Normalize();
        }

        return true;
    }

    void Measure(Transform thrown)
    {
        Quaternion rotation = thrown.rotation;
        thrown.rotation = Quaternion.identity;

        Renderer renderer = thrown.GetComponentInChildren<Renderer>();
        Vector3 size = renderer != null ? renderer.localBounds.size : Vector3.zero;
        if (renderer != null && size.sqrMagnitude > 0.0001f)
        {
            Vector3 lossy = renderer.transform.lossyScale;
            projectileLength = Mathf.Max(
                Mathf.Abs(size.x * lossy.x),
                Mathf.Abs(size.y * lossy.y),
                Mathf.Abs(size.z * lossy.z));

            if (style.AlignToPath)
            {
                int longAxis = 0;
                if (size.y > size[longAxis]) longAxis = 1;
                if (size.z > size[longAxis]) longAxis = 2;
                int thinAxis = 0;
                if (size.y < size[thinAxis]) thinAxis = 1;
                if (size.z < size[thinAxis]) thinAxis = 2;
                if (longAxis == thinAxis)
                    thinAxis = (longAxis + 1) % 3;

                Vector3 meshTip = Vector3.zero;
                meshTip[longAxis] = -1f;
                Vector3 meshFace = Vector3.zero;
                meshFace[thinAxis] = 1f;
                tipLocal = thrown.InverseTransformDirection(renderer.transform.TransformDirection(meshTip)).normalized;
                faceLocal = thrown.InverseTransformDirection(renderer.transform.TransformDirection(meshFace)).normalized;
                align = true;
            }
        }

        thrown.rotation = rotation;
        if (projectileLength < 0.05f)
            projectileLength = 1f;
    }

    void ApplyClearance()
    {
        float half = projectileLength * 0.5f;
        arc.LeadLength = half + BuryDistance;
        arc.LeadStart = arc.Launch - arc.ExitDirection * arc.LeadLength;

        float halfDepth = 0.75f;
        if (arc.HasTargetBounds)
        {
            Vector3 extents = arc.TargetBounds.extents;
            Vector3 arrive = arc.ArriveDirection;
            halfDepth = Mathf.Abs(extents.x * arrive.x)
                + Mathf.Abs(extents.y * arrive.y)
                + Mathf.Abs(extents.z * arrive.z);
        }

        arc.TailLength = halfDepth + half + ClearMargin;
        arc.TailEnd = arc.End + arc.ArriveDirection * arc.TailLength;
        impactDistance = style.AlignToPath
            ? arc.LeadLength + arc.ArcLength - half
            : arc.LeadLength + arc.ArcLength;
    }

    void Place(float distance)
    {
        if (projectile == null)
            return;

        if (!reached && distance >= impactDistance)
            ReachTarget();

        if (!arc.Sample(distance, out Vector3 position, out Vector3 tangent))
            return;

        projectile.transform.position = position;
        if (align)
        {
            Vector3 face = Vector3.ProjectOnPlane(arc.BoardNormal, tangent);
            if (face.sqrMagnitude < 0.04f && source != null && View.Instance != null)
            {
                ViewTarget view = View.Instance.GetViewTargetByID(source.GetID());
                if (view != null)
                    face = Vector3.ProjectOnPlane(view.transform.up, tangent);
            }

            if (face.sqrMagnitude > 0.0001f)
                projectile.transform.rotation = AttackArrowPreview.AlignArrow(tipLocal, tangent, faceLocal, face);
        }
        else if (style.Spin)
        {
            projectile.transform.rotation = Quaternion.AngleAxis(distance * style.SpinDegreesPerUnit, spinAxis) * baseRotation;
        }
    }

    void ReachTarget()
    {
        if (reached)
            return;

        reached = true;
        OnReachedTarget?.Invoke();
        if (pullDamageOnImpact)
            PullDamage();
    }

    void PullDamage()
    {
        if (View.Instance == null)
            return;

        Predicate<AnimationAction> match = damageMatch ?? (animationAction =>
            animationAction is ChangeStatsAnimation && ReferenceEquals(animationAction.GameAction, GameAction));
        List<AnimationAction> damageAnimations = View.Instance.AnimationHandler.TakeQueuedAnimations(
            match,
            animationAction => false);

        pendingDamageAnimations = damageAnimations.Count;
        foreach (AnimationAction damageAnimation in damageAnimations)
            damageAnimation.Play(OnDamageAnimationComplete);
    }

    void OnDamageAnimationComplete()
    {
        pendingDamageAnimations--;
        TryFinish();
    }

    void FinishFlight()
    {
        if (flightDone)
            return;

        if (!reached)
            ReachTarget();

        if (projectile != null)
        {
            UnityEngine.Object.Destroy(projectile);
            projectile = null;
        }

        flightDone = true;
        TryFinish();
    }

    void TryFinish()
    {
        if (flightDone && pendingDamageAnimations <= 0)
            CallCallback();
    }

    void PlaySound()
    {
        if (string.IsNullOrEmpty(style.SoundPath) || View.Instance == null || View.Instance.AudioHandler == null)
            return;

        if (!sounds.TryGetValue(style.SoundPath, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>(style.SoundPath);
            sounds[style.SoundPath] = clip;
            if (clip == null && loggedMissing.Add(style.SoundPath))
                Debug.LogError("Missing projectile sound at Resources/" + style.SoundPath);
        }

        if (clip != null)
            View.Instance.AudioHandler.PlayOneShot(clip, 0.5f);
    }

    static GameObject LoadPrefab(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        if (prefabs.TryGetValue(path, out GameObject prefab) && prefab != null)
            return prefab;

        prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            if (loggedMissing.Add(path))
                Debug.LogError("Missing projectile prefab at Resources/" + path);
            return null;
        }

        prefabs[path] = prefab;
        return prefab;
    }

    protected override void Log()
    {
        string thrown = style != null ? style.PrefabPath : "projectile";
        Debug.LogWarning("ArcProjectileAnimation: " + thrown);
    }
}
