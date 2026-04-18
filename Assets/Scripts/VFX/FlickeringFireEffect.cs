using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sets up two URP particle systems (soft flame billboards + sparks) for a semi-realistic flickering fire.
/// No lights — appearance is purely particle rendering. Add this to an empty GameObject; materials resolve automatically in the editor.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class FlickeringFireEffect : MonoBehaviour
{
    const string FlameMaterialPath = "Assets/VFX/Fire/Materials/Fire_Flames.mat";
    const string SparkMaterialPath = "Assets/VFX/Fire/Materials/Fire_Sparks.mat";

    const string FlamesChildName = "Flames";
    const string SparksChildName = "Sparks";

    [SerializeField]
    Material flameMaterial;

    [SerializeField]
    Material sparkMaterial;

    [Tooltip("Overall scale of the effect (approximate flame height / spread).")]
    [SerializeField]
    float effectScale = 1f;

    [Tooltip("How fast the particle simulation runs (1 = default). Affects motion, flicker, and overall pacing.")]
    [SerializeField]
    [Min(0.01f)]
    float animationSpeed = 1f;

    ParticleSystem flames;
    ParticleSystem sparks;

#if UNITY_EDITOR
    void Reset()
    {
        AssignDefaultMaterialsIfNeeded();
        Rebuild();
    }

    void OnValidate()
    {
        AssignDefaultMaterialsIfNeeded();
        if (flameMaterial != null && sparkMaterial != null)
            Rebuild();
    }

    void AssignDefaultMaterialsIfNeeded()
    {
        if (flameMaterial == null)
            flameMaterial = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath);
        if (sparkMaterial == null)
            sparkMaterial = AssetDatabase.LoadAssetAtPath<Material>(SparkMaterialPath);
    }
#endif

    void Awake()
    {
        if (flameMaterial == null || sparkMaterial == null)
        {
            Debug.LogWarning(
                $"{nameof(FlickeringFireEffect)} on '{name}': assign flame and spark materials (defaults are applied automatically when using Reset in the editor).",
                this);
            return;
        }

        Rebuild();
    }

    [ContextMenu("Rebuild particle systems")]
    void RebuildFromMenu()
    {
#if UNITY_EDITOR
        AssignDefaultMaterialsIfNeeded();
#endif
        Rebuild();
    }

    void Rebuild()
    {
        if (flameMaterial == null || sparkMaterial == null)
            return;

        effectScale = Mathf.Max(0.05f, effectScale);
        float speed = Mathf.Max(0.01f, animationSpeed);

        flames = GetOrCreateSystem(FlamesChildName, flameMaterial, (ps, s) => ConfigureFlames(ps, s, speed));
        sparks = GetOrCreateSystem(SparksChildName, sparkMaterial, (ps, s) => ConfigureSparks(ps, s, speed));

        flames.transform.localPosition = Vector3.zero;
        sparks.transform.localPosition = Vector3.zero;
    }

    ParticleSystem GetOrCreateSystem(string childName, Material mat, System.Action<ParticleSystem, float> configure)
    {
        Transform existing = transform.Find(childName);
        GameObject go;
        if (existing == null)
        {
            go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            go.AddComponent<ParticleSystem>();
        }
        else
            go = existing.gameObject;

        var ps = go.GetComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var renderer = go.GetComponent<ParticleSystemRenderer>();

        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = -4f;

        configure(ps, effectScale);
        ps.Play();

        return ps;
    }

    static void ConfigureFlames(ParticleSystem ps, float s, float animSpeed)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.simulationSpeed = animSpeed;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f * s, 1.05f * s);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f * s, 1.05f * s);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f * s, 0.52f * s);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI * 0.35f, Mathf.PI * 0.35f);
        main.gravityModifier = 0f;
        main.maxParticles = 200;
        main.startColor = Color.white;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(22f, 48f);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.06f * s;
        shape.radiusThickness = 0f;
        shape.length = 0.12f * s;
        shape.arc = 360f;
        shape.randomDirectionAmount = 0.08f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(BuildFlameColorGradient());

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, BuildFlameSizeCurve());

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.separateAxes = true;
        rol.x = new ParticleSystem.MinMaxCurve(0f);
        rol.y = new ParticleSystem.MinMaxCurve(0f);
        rol.z = new ParticleSystem.MinMaxCurve(-32f, 32f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.12f * s, 0.38f * s);
        noise.frequency = 0.9f;
        noise.scrollSpeed = 0.42f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.High;
        noise.separateAxes = false;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.15f * s, 0.15f * s);
        vel.z = new ParticleSystem.MinMaxCurve(-0.15f * s, 0.15f * s);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0.35f * s);
    }

    static void ConfigureSparks(ParticleSystem ps, float s, float animSpeed)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.simulationSpeed = animSpeed;
        main.duration = 1.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f * s, 0.42f * s);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * s, 3.8f * s);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f * s, 0.065f * s);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.gravityModifier = 0.35f;
        main.maxParticles = 120;
        main.startColor = Color.white;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(6f, 14f);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.03f * s;
        shape.radiusThickness = 0f;
        shape.length = 0.05f * s;
        shape.randomDirectionAmount = 0.15f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(BuildSparkColorGradient());

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, BuildSparkSizeCurve());

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f * s;
        noise.frequency = 1.2f;
        noise.scrollSpeed = 0.6f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
    }

    static Gradient BuildFlameColorGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1.2f, 0.18f, 0.04f), 0f),
                new GradientColorKey(new Color(1.4f, 0.42f, 0.1f), 0.18f),
                new GradientColorKey(new Color(1.5f, 0.85f, 0.28f), 0.48f),
                new GradientColorKey(new Color(0.45f, 0.06f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.95f, 0.06f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        return g;
    }

    static AnimationCurve BuildFlameSizeCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.55f, 0.88f),
            new Keyframe(1f, 0.02f));
    }

    static Gradient BuildSparkColorGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1.5f, 1.2f, 0.5f), 0f),
                new GradientColorKey(new Color(1.8f, 1.4f, 0.35f), 0.35f),
                new GradientColorKey(new Color(1f, 0.35f, 0.08f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.05f),
                new GradientAlphaKey(0.65f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        return g;
    }

    static AnimationCurve BuildSparkSizeCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.4f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f));
    }
}
