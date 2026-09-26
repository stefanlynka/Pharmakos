using System;
using UnityEngine;

/// <summary>
/// One-shot radial (or cone) burst of particles in the GameField's XY plane, built from a runtime ParticleSystem.
/// The particle object destroys itself once every particle has died.
/// </summary>
public class ParticleBurstVfx : VfxEffect
{
    private const string DefaultMaterialPath = "VFX/Materials/ParticleDefault";
    private static Material defaultMaterial;

    public int Count = 30;
    public float LifetimeMin = 0.4f;
    public float LifetimeMax = 0.8f;
    public float SpeedMin = 3f;
    public float SpeedMax = 8f;
    public float SizeMin = 0.1f;
    public float SizeMax = 0.3f;
    public Color StartColor = Color.white;
    public Color EndColor = new Color(1f, 1f, 1f, 0f);
    // 360 = full radial burst; smaller values emit a cone centred on Direction
    public float SpreadAngle = 360f;
    public Vector3 Direction = Vector3.up;
    public float EmitRadius = 0f;
    public float Gravity = 0f;
    // Linear drag; roughly 2-6 gives a noticeable "puff then slow down"
    public float Drag = 0f;
    public bool ShrinkOverLifetime = true;
    // Parent to the anchor and simulate in local space, so the burst moves with e.g. a moving follower
    public bool FollowAnchor = false;
    public int SortingOrder = 0;
    public Material Material = null;

    public ParticleBurstVfx()
    {
        QueueHold = 0.25f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        GameObject burstObject = CreateEffectObject(context, "ParticleBurstVfx");

        Transform anchor = context.GetAnchorTransform(Anchor);
        if (FollowAnchor && anchor != null)
        {
            burstObject.transform.SetParent(anchor, true);
        }

        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ConfigureMain(particles);
        ConfigureEmission(particles);
        ConfigureShape(particles);
        ConfigureOverLifetime(particles);
        ConfigureRenderer(burstObject.GetComponent<ParticleSystemRenderer>());

        particles.Play();

        CompleteAfterHold(onComplete);
    }

    private void ConfigureMain(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.1f;
        main.simulationSpace = FollowAnchor ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(LifetimeMin, LifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(SpeedMin, SpeedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(SizeMin, SizeMax);
        main.startColor = Color.white;
        main.gravityModifier = Gravity;
        main.maxParticles = Mathf.Max(1, Count);
    }

    private void ConfigureEmission(ParticleSystem particles)
    {
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, Count)) });
    }

    private void ConfigureShape(ParticleSystem particles)
    {
        float arc = Mathf.Clamp(SpreadAngle, 0f, 360f);

        // Circle lies in the shape's local XY plane and its arc starts at +X, so rotate to centre it on Direction
        float directionAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.0001f, EmitRadius);
        shape.radiusThickness = 1f;
        shape.arc = arc;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        shape.rotation = new Vector3(0f, 0f, directionAngle - arc / 2f);
    }

    private void ConfigureOverLifetime(ParticleSystem particles)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(StartColor, 0f), new GradientColorKey(EndColor, 1f) },
            new[] { new GradientAlphaKey(StartColor.a, 0f), new GradientAlphaKey(EndColor.a, 1f) });

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = ShrinkOverLifetime;
        if (ShrinkOverLifetime)
        {
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        }

        ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity = particles.limitVelocityOverLifetime;
        limitVelocity.enabled = Drag > 0f;
        if (Drag > 0f)
        {
            limitVelocity.drag = Drag;
            limitVelocity.multiplyDragByParticleSize = false;
            limitVelocity.multiplyDragByParticleVelocity = false;
        }
    }

    private void ConfigureRenderer(ParticleSystemRenderer particleRenderer)
    {
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = Material != null ? Material : GetDefaultMaterial();
        particleRenderer.sortingOrder = SortingOrder;
    }

    public static Material GetDefaultMaterial()
    {
        if (defaultMaterial != null) return defaultMaterial;

        defaultMaterial = Resources.Load<Material>(DefaultMaterialPath);
        if (defaultMaterial == null)
        {
            Debug.LogWarning("ParticleBurstVfx: missing Resources/" + DefaultMaterialPath + ", falling back to a runtime URP particle material.");
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            defaultMaterial = new Material(shader);
        }

        return defaultMaterial;
    }
}
