using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A ritual being answered. Timed to the ritual's sound, whose notes fall on <see cref="MajorBeats"/> / <see cref="MinorBeats"/>
/// (Tools/Audio/build_ritual_sounds.py uses the same times):
///  1. Gathering (first notes): the board goes dark, motes in the ritual's offering colours stream from the altar into
///     the god's sigil, which forms over the target while a seal turns beneath it and a shaft of the god's light grows.
///     Each note pulses the sigil and the seal.
///  2. The boon (final note): flash, shockwave, the god's particles, screen shake, a tinted post-processing pulse
///     (and lightning for Zeus). The animation queue is released just after, so the ritual's effect plays out over:
///  3. The tail: the sigil burns out, embers/petals/ash drift, the lights come back.
/// Look per god comes from its <see cref="RitualTheme"/>; Minor rituals play it smaller and quicker.
/// </summary>
public class RitualInvocationVfx : VfxEffect
{
    public static readonly float[] MajorBeats = { 0f, 0.40f, 0.90f };
    public static readonly float[] MinorBeats = { 0f, 0.60f };

    public Ritual Ritual;
    public RitualTheme Theme;
    public bool Major = true;
    public bool PlaySound = true;
    // Seconds after the final note before the animation queue moves on to the ritual's own effect.
    public float ReleaseAfterClimax = 0.15f;

    // The theme's Pace scales every timing: beats, tail, particles, shake, lights and post pulse.
    public float Pace => Theme != null ? Mathf.Clamp(Theme.Pace, 0.25f, 2f) : 1f;
    public float[] Beats
    {
        get
        {
            float[] standard = Major ? MajorBeats : MinorBeats;
            float[] beats = new float[standard.Length];
            for (int i = 0; i < beats.Length; i++)
                beats[i] = standard[i] * Pace;
            return beats;
        }
    }
    public float Climax => (Major ? MajorBeats[MajorBeats.Length - 1] : MinorBeats[MinorBeats.Length - 1]) * Pace;

    public RitualInvocationVfx(Ritual ritual)
    {
        Ritual = ritual;
        Theme = RitualTheme.For(ritual);
        Major = IsMajor(ritual);
        QueueHold = Climax + ReleaseAfterClimax * Pace;
        LiftTowardCamera = 0f;
    }

    // Majors get three notes and the full effect. Old Ones is a Minor by name but costs as much as a Major.
    public static bool IsMajor(Ritual ritual)
    {
        if (ritual == null)
            return true;
        string name = ritual.GetName();
        return name.EndsWith("Major") || name == "OldOnesMinor";
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        QueueHold = Climax + ReleaseAfterClimax * Pace;

        GameObject root = new GameObject("RitualInvocation_" + (Ritual != null ? Ritual.GetName() : "?"));
        if (context.Field != null)
            root.transform.SetParent(context.Field, false);
        root.AddComponent<RitualInvocationRun>().Begin(this, context);

        CompleteAfterHold(onComplete);
    }
}

/// <summary>Drives one ritual invocation frame by frame and destroys itself when done. See RitualInvocationVfx.</summary>
public class RitualInvocationRun : MonoBehaviour
{
    const string AddMaterialPath = "VFX/Materials/RitualGlowAdd";
    const string AlphaMaterialPath = "VFX/Materials/RitualGlowAlpha";
    const string TexturePath = "Images/RitualFX/";
    const int SortingOrder = 9000;

    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    static Mesh quadMesh;
    static Material addMaterial;
    static Material alphaMaterial;
    static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    static readonly Dictionary<(Texture2D, bool), Material> particleMaterials = new Dictionary<(Texture2D, bool), Material>();

    // Layout in world units (a follower card is about 6.6 x 9.1). Minors scale these by MinorScale.
    const float MinorScale = 0.78f;
    const float SigilSize = 17f;
    const float SigilLift = 9f;        // toward the camera, so the sigil hangs in front of the cards
    const float SealSize = 19f;
    const float BeamWidth = 13f;
    const float BeamLength = 46f;
    const float FlashSize = 24f;
    const float ScreenMargin = 0.03f;  // viewport fraction the sigil keeps from the screen edge
    const float ShockwaveSize = 46f;
    const float PlaneLift = 0.6f;      // keeps board-plane pieces just off the board
    const float ParticleSizeScale = 2f; // theme particle sizes are in card-ish units; this maps them to the board

    RitualInvocationVfx fx;
    RitualTheme theme;
    float scale;
    float[] beats;
    float climax;
    float elapsed;
    int nextBeat;
    bool climaxDone;
    float lifetime;
    float pace = 1f;

    Vector3 targetPos;
    Vector3 sigilPos;
    Vector3 up;
    Transform targetTransform;
    ViewTarget targetView;

    Glyph sigil;
    Glyph sigilEcho;
    Glyph seal;
    Glyph beam;
    Glyph flash;
    Glyph shockwave;
    readonly List<Mote> motes = new List<Mote>();

    float sigilPulse;
    float sealPulse;

    class Glyph
    {
        public Transform Transform;
        public MeshRenderer Renderer;
        public MaterialPropertyBlock Block;
        public Color Color;
        public float Intensity;

        public void Set(float alpha, float size)
        {
            Transform.localScale = new Vector3(size, size, 1f);
            SetRaw(alpha, Transform.localScale);
        }

        public void SetRaw(float alpha, Vector3 localScale)
        {
            Transform.localScale = localScale;
            Renderer.enabled = alpha > 0.002f;
            Color c = Color;
            c.a *= Mathf.Clamp01(alpha);
            Block.SetColor(ColorId, c);
            Block.SetFloat(IntensityId, Intensity);
            Renderer.SetPropertyBlock(Block);
        }
    }

    class Mote
    {
        public Glyph Glyph;
        public Glyph[] Trail;
        public Vector3 Start, Control, End;
        public float StartTime, Flight, Size;
    }

    public void Begin(RitualInvocationVfx effect, VfxContext context)
    {
        fx = effect;
        theme = effect.Theme;
        scale = effect.Major ? 1f : MinorScale;
        beats = effect.Beats;
        climax = effect.Climax;
        pace = effect.Pace;
        lifetime = climax + 2.6f * pace;

        Camera cam = Camera.main;
        targetTransform = context.GetAnchorTransform(VfxAnchor.Target);
        targetView = context.Target != null ? context.Target : context.Source;
        targetPos = targetTransform != null ? targetTransform.position : (context.Field != null ? context.Field.position : Vector3.zero);
        up = context.Field != null ? context.Field.up : Vector3.up;
        Vector3 toCam = cam != null ? (cam.transform.position - targetPos).normalized : Vector3.back;
        sigilPos = targetPos + toCam * SigilLift;
        sigilPos = KeepOnScreen(cam, sigilPos, SigilSize * scale * (theme != null ? theme.SigilScale : 1f) * 1.15f);

        Color glow = theme != null ? theme.Glow : new Color(1f, 0.78f, 0.45f);
        Color accent = theme != null ? theme.Accent : Color.white;
        float intensity = theme != null ? theme.GlowIntensity : 2.2f;
        Texture2D sigilTexture = theme != null && theme.Sigil != null ? theme.Sigil : LoadTexture("Sigils/Default");

        Quaternion facePlane = context.Field != null ? context.Field.rotation : Quaternion.identity;
        Quaternion faceCam = cam != null ? Quaternion.LookRotation(sigilPos - cam.transform.position, cam.transform.up) : Quaternion.identity;
        Vector3 planePos = targetPos + toCam * PlaneLift;

        seal = MakeGlyph("Seal", LoadTexture("Halo"), true, glow, intensity * 0.8f, planePos, facePlane, 0);
        beam = MakeGlyph("Beam", LoadTexture("Beam"), true, glow, intensity * 0.55f, planePos + up * (BeamLength * scale * 0.5f), facePlane, 1);
        sigil = MakeGlyph("Sigil", sigilTexture, true, glow, intensity, sigilPos, faceCam, 3);
        sigilEcho = MakeGlyph("SigilEcho", sigilTexture, true, glow, intensity * 0.7f, sigilPos, faceCam, 2);
        flash = MakeGlyph("Flash", LoadTexture("Flash"), true, Color.Lerp(glow, accent, 0.35f), intensity * 0.9f, sigilPos + toCam * 0.5f, faceCam, 4);
        shockwave = MakeGlyph("Shockwave", LoadTexture("Ring"), true, Color.Lerp(glow, accent, 0.4f), intensity, planePos, facePlane, 2);
        foreach (Glyph g in new[] { seal, beam, sigil, sigilEcho, flash, shockwave })
            g.Set(0f, 0f);

        CreateMotes(context, glow, intensity);

        if (fx.PlaySound && fx.Ritual != null && View.Instance != null && View.Instance.AudioHandler != null)
            View.Instance.AudioHandler.PlayRitualInvocation(fx.Ritual);

        PostFXPulse.RitualGather(climax);

        if (Controller.Instance != null && Controller.Instance.LightingHandler != null && targetTransform != null)
        {
            float tint = theme != null ? theme.SpotlightTint : 0.6f;
            Controller.Instance.LightingHandler.PlayRitualLighting(targetTransform.gameObject, Color.Lerp(Color.white, glow, tint),
                climax, effectMajor: fx.Major, pace: pace);
        }

        UpdateVisuals();
    }

    // ------------------------------------------------------------ timeline ---
    void Update()
    {
        elapsed += Time.deltaTime;

        while (nextBeat < beats.Length && elapsed >= beats[nextBeat])
        {
            OnBeat(nextBeat, nextBeat == beats.Length - 1);
            nextBeat++;
        }

        UpdateVisuals();

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }

    void OnBeat(int index, bool final)
    {
        if (!final)
        {
            sigilPulse = 1f;
            sealPulse = 1f;
            if (index > 0)
                ScreenShakeHandler.Shake(0.12f * scale, 0.12f * pace);
            return;
        }

        climaxDone = true;
        float strength = theme != null ? theme.ShakeStrength : 1f;
        ScreenShakeHandler.Shake((fx.Major ? 0.75f : 0.45f) * strength, (fx.Major ? 0.5f : 0.32f) * pace);
        if (theme != null)
            PostFXPulse.Ritual(theme.Glow, fx.Major ? 1f : 0.7f, pace);
        else
            PostFXPulse.Ritual();

        if (theme != null)
        {
            SpawnLayer(theme.Burst, "Burst");
            SpawnLayer(theme.Drift, "Drift");
            SpawnLayer(theme.Extra, "Extra");
        }

        if (theme != null && theme.LightningStrike && LightningStrikes.Instance != null)
        {
            Vector3 origin = targetPos + up * 48f;
            origin.z = LightningStrikeVfx.OriginZ;
            LightningStrikes.Instance.Strike(origin, targetPos);
        }

        if (targetView is ViewPlayer && theme != null)
        {
            Color wash = theme.Glow;
            wash.a = 0.3f;
            VfxLibrary.PortraitColorFlash(wash, 0.9f * pace).Play(new VfxContext(null, targetView), null);
        }
    }

    void UpdateVisuals()
    {
        float t = elapsed;
        float dt = Time.deltaTime;
        sigilPulse = Mathf.MoveTowards(sigilPulse, 0f, dt * 4f / pace);
        sealPulse = Mathf.MoveTowards(sealPulse, 0f, dt * 3f / pace);

        float build = Mathf.Clamp01(t / climax);              // 0..1 over the gathering
        float after = Mathf.Max(0f, t - climax) / pace;       // seconds since the final note, at standard pace

        // Seal: unfolds under the target and turns slowly; flares and widens on the boon, then fades.
        float sealSize = SealSize * scale;
        float sealAlpha, sealGrow;
        if (!climaxDone)
        {
            sealAlpha = Smooth(0f, 0.6f, build) * 0.55f + sealPulse * 0.3f;
            sealGrow = Mathf.Lerp(0.55f, 1f, EaseOut(build));
        }
        else
        {
            sealAlpha = Mathf.Lerp(0.85f, 0f, Smooth(0f, 1.1f, after));
            sealGrow = 1f + 0.25f * EaseOut(Mathf.Clamp01(after / 1.1f));
        }
        seal.Transform.localRotation = Quaternion.Euler(0f, 0f, -t / pace * 14f);
        seal.Set(sealAlpha, sealSize * sealGrow);

        // Shaft of light from above onto the target.
        float beamAlpha = !climaxDone
            ? Smooth(0.1f, 1f, build) * 0.5f
            : Mathf.Lerp(1f, 0f, Smooth(0.05f, 0.9f, after));
        beam.SetRaw(beamAlpha, new Vector3(BeamWidth * scale * (climaxDone ? 1f + 0.4f * Smooth(0f, 0.2f, after) : 1f), BeamLength * scale, 1f));

        // Sigil: forms, tightening as the offerings pour in; on the boon it flares and burns out outward.
        float sigilSize = SigilSize * scale * (theme != null ? theme.SigilScale : 1f);
        if (!climaxDone)
        {
            float a = Smooth(0.08f, 0.85f, build) * 0.7f + sigilPulse * 0.25f;
            sigil.Set(a, sigilSize * Mathf.Lerp(1.3f, 1f, EaseOut(build)) * (1f + sigilPulse * 0.05f));
        }
        else
        {
            float burn = Mathf.Clamp01(after / 0.75f);
            sigil.Set((1f - Smooth(0.15f, 1f, burn)) * 1.25f, sigilSize * (1f + 0.25f * EaseOut(burn)));
            float echo = Mathf.Clamp01(after / 0.45f);
            sigilEcho.Set((1f - echo) * 0.8f, sigilSize * (1f + 0.9f * EaseOut(echo)));
        }

        // Flash: small on each note, full on the boon.
        if (!climaxDone)
            flash.Set(sigilPulse * 0.35f, FlashSize * scale * 0.4f);
        else
        {
            float f = Mathf.Clamp01(after / 0.22f);
            flash.Set((1f - Smooth(0f, 1f, f)) * 0.6f, FlashSize * scale * (0.45f + 0.55f * EaseOut(f)));
        }

        // Shockwave across the board.
        if (climaxDone)
        {
            float s = Mathf.Clamp01(after / 0.65f);
            shockwave.Set((1f - s) * 0.5f, ShockwaveSize * scale * (0.15f + EaseOut(s)));
        }

        UpdateMotes(t);
    }

    // -------------------------------------------------------- offering motes ---
    void CreateMotes(VfxContext context, Color glow, float intensity)
    {
        // The sacrifice: motes in the ritual's offering colours rise from its altar and pour into the sigil.
        Vector3 source = GetSourcePosition(context);
        List<Color> colors = new List<Color>();
        if (fx.Ritual != null && fx.Ritual.Costs != null)
        {
            foreach (KeyValuePair<OfferingType, int> cost in fx.Ritual.Costs)
            {
                for (int i = 0; i < Mathf.Clamp(cost.Value, 0, 6); i++)
                    colors.Add(OfferingHandler.GetOfferingColor(cost.Key));
            }
        }
        if (colors.Count == 0)
            colors.Add(glow);

        Texture2D moteTexture = LoadTexture("Glow");
        int count = fx.Major ? 30 : 18;
        float lastArrival = climax - 0.08f;
        System.Random rng = new System.Random(fx.Ritual != null ? fx.Ritual.GetID() * 31 + 7 : 7);
        for (int i = 0; i < count; i++)
        {
            Color c = Color.Lerp(colors[i % colors.Count], Color.white, 0.25f);
            float flight = Mathf.Lerp(0.38f, 0.55f, (float)rng.NextDouble()) * (fx.Major ? 1f : 0.85f) * pace;
            float startTime = Mathf.Lerp(0.02f, Mathf.Max(0.05f, lastArrival - flight), (float)i / Mathf.Max(1, count - 1));
            startTime += ((float)rng.NextDouble() - 0.5f) * 0.06f * pace;
            Vector3 jitter = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, 0f) * 3f;
            Vector3 start = source + jitter;
            Vector3 end = sigilPos + new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, 0f) * 2f;
            Vector3 control = Vector3.Lerp(start, end, 0.35f) + up * (10f + (float)rng.NextDouble() * 8f);

            Mote mote = new Mote
            {
                Glyph = MakeGlyph("Mote", moteTexture, true, c, intensity * 1.3f, start, sigil.Transform.rotation, 5),
                Start = start,
                Control = control,
                End = end,
                StartTime = Mathf.Max(0f, startTime),
                Flight = flight,
                Size = Mathf.Lerp(3.2f, 5f, (float)rng.NextDouble()) * scale,
            };
            mote.Trail = new Glyph[MoteTrail];
            for (int k = 0; k < MoteTrail; k++)
            {
                mote.Trail[k] = MakeGlyph("MoteTrail", moteTexture, true, c, intensity * 0.9f, start, sigil.Transform.rotation, 4);
                mote.Trail[k].Set(0f, 0f);
            }
            mote.Glyph.Set(0f, 0f);
            motes.Add(mote);
        }
    }

    // Moves a camera-facing quad of the given world size so it sits fully on screen (targets at the top edge, such as
    // the enemy portrait, would otherwise cut the sigil off).
    static Vector3 KeepOnScreen(Camera cam, Vector3 position, float size)
    {
        if (cam == null)
            return position;
        Vector3 vp = cam.WorldToViewportPoint(position);
        if (vp.z <= 0f)
            return position;
        float viewHeight = 2f * vp.z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfY = size * 0.5f / viewHeight;
        float halfX = halfY / Mathf.Max(0.1f, cam.aspect);
        float minY = ScreenMargin + halfY, maxY = 1f - ScreenMargin - halfY;
        float minX = ScreenMargin + halfX, maxX = 1f - ScreenMargin - halfX;
        vp.x = minX < maxX ? Mathf.Clamp(vp.x, minX, maxX) : 0.5f;
        vp.y = minY < maxY ? Mathf.Clamp(vp.y, minY, maxY) : 0.5f;
        return cam.ViewportToWorldPoint(vp);
    }

    Vector3 GetSourcePosition(VfxContext context)
    {
        if (context.Source != null && context.Source != targetView)
            return context.Source.transform.position;
        // Rituals the opponent uses have no altar on screen: the offerings come from its portrait.
        if (fx.Ritual != null && fx.Ritual.Owner != null && View.Instance != null)
        {
            ViewTarget owner = View.Instance.GetViewTargetByID(fx.Ritual.Owner.GetID());
            if (owner != null)
                return owner.transform.position;
        }
        return targetPos + up * 30f;
    }

    const int MoteTrail = 4;
    const float MoteTrailSpacing = 0.045f; // flight fraction between trail pieces

    void UpdateMotes(float t)
    {
        foreach (Mote m in motes)
        {
            float p = (t - m.StartTime) / m.Flight;
            PlaceMote(m, m.Glyph, p, 1f);
            for (int k = 0; k < m.Trail.Length; k++)
            {
                float f = 1f - (k + 1f) / (m.Trail.Length + 1f);
                PlaceMote(m, m.Trail[k], p - (k + 1) * MoteTrailSpacing, f * 0.7f);
            }
        }
    }

    void PlaceMote(Mote m, Glyph g, float p, float weight)
    {
        if (p < 0f || p > 1f)
        {
            g.Renderer.enabled = false;
            return;
        }
        float e = p * p * (3f - 2f * p);
        e = Mathf.Lerp(e, p * p, 0.5f); // accelerate into the sigil
        Vector3 a = Vector3.Lerp(m.Start, m.Control, e);
        Vector3 b = Vector3.Lerp(m.Control, m.End, e);
        g.Transform.position = Vector3.Lerp(a, b, e);
        float alpha = Smooth(0f, 0.15f, p) * (1f - Smooth(0.85f, 1f, p)) * weight;
        g.Set(alpha, m.Size * Mathf.Lerp(1f, 0.55f, p) * Mathf.Lerp(0.5f, 1f, weight));
    }

    // ----------------------------------------------------------- particles ---
    void SpawnLayer(RitualParticleLayer layer, string name)
    {
        if (layer == null || !layer.Enabled || layer.Count <= 0)
            return;

        Texture2D texture = layer.Texture != null ? layer.Texture : LoadTexture("Glow");
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = Vector3.Lerp(targetPos, sigilPos, 0.6f);
        go.transform.rotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        int count = Mathf.Max(1, Mathf.RoundToInt(layer.Count * (fx.Major ? 1f : 0.6f)));
        float speedScale = fx.Major ? 1f : 0.8f;

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = Mathf.Max(0.05f, layer.Duration * pace);
        main.startDelay = Mathf.Max(0f, layer.Delay * pace);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(layer.Lifetime.x * pace, layer.Lifetime.y * pace);
        main.startSpeed = new ParticleSystem.MinMaxCurve(layer.Speed.x * speedScale, layer.Speed.y * speedScale);
        main.startSize = new ParticleSystem.MinMaxCurve(layer.Size.x * scale * ParticleSizeScale, layer.Size.y * scale * ParticleSizeScale);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(layer.ColorA, layer.ColorB);
        main.gravityModifier = layer.Gravity;
        main.maxParticles = count;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        if (layer.Duration <= 0f)
        {
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }
        else
        {
            emission.rateOverTime = count / (layer.Duration * pace);
        }

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        switch (layer.Shape)
        {
            case RitualParticleShape.Radial:
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 1.5f * scale;
                break;
            case RitualParticleShape.Sphere:
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 1.5f * scale;
                break;
            case RitualParticleShape.Rise:
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 35f;
                shape.radius = 4f * scale;
                shape.rotation = new Vector3(-90f, 0f, 0f);
                break;
            case RitualParticleShape.Fall:
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(22f * scale, 2f, 2f);
                shape.position = new Vector3(0f, 16f * scale, 0f);
                shape.rotation = new Vector3(90f, 0f, 0f);
                break;
        }

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = fade;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.6f));

        if (layer.Spin != Vector2.zero)
        {
            ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(layer.Spin.x * Mathf.Deg2Rad, layer.Spin.y * Mathf.Deg2Rad);
        }

        if (layer.Drag > 0f)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 1000f;
            limit.drag = layer.Drag;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;
        }

        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = layer.Stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (layer.Stretch)
        {
            psr.velocityScale = 0.06f;
            psr.lengthScale = 1.5f;
        }
        psr.sortMode = ParticleSystemSortMode.Distance;
        psr.sortingOrder = SortingOrder + 6;
        psr.sharedMaterial = GetParticleMaterial(texture, layer.Additive, layer.Additive ? layer.Intensity : 1f);

        ps.Play();
    }

    static Material GetParticleMaterial(Texture2D texture, bool additive, float intensity)
    {
        if (particleMaterials.TryGetValue((texture, additive), out Material cached) && cached != null)
        {
            cached.SetFloat(IntensityId, intensity);
            return cached;
        }
        Material baseMaterial = additive ? AddMaterial : AlphaMaterial;
        Material m = new Material(baseMaterial) { name = baseMaterial.name + "_" + texture.name };
        m.SetTexture(MainTexId, texture);
        m.SetFloat(IntensityId, intensity);
        particleMaterials[(texture, additive)] = m;
        return m;
    }

    // ------------------------------------------------------------- helpers ---
    Glyph MakeGlyph(string name, Texture2D texture, bool additive, Color color, float intensity, Vector3 position, Quaternion rotation, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.AddComponent<MeshFilter>().sharedMesh = QuadMesh;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = additive ? AddMaterial : AlphaMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.sortingOrder = SortingOrder + order;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        if (texture != null)
            block.SetTexture(MainTexId, texture);
        return new Glyph { Transform = go.transform, Renderer = r, Block = block, Color = color, Intensity = intensity };
    }

    static Texture2D LoadTexture(string name)
    {
        if (!textures.TryGetValue(name, out Texture2D t) || t == null)
        {
            t = Resources.Load<Texture2D>(TexturePath + name);
            textures[name] = t;
        }
        return t;
    }

    static Material AddMaterial
    {
        get
        {
            if (addMaterial == null)
                addMaterial = Resources.Load<Material>(AddMaterialPath);
            return addMaterial;
        }
    }

    static Material AlphaMaterial
    {
        get
        {
            if (alphaMaterial == null)
                alphaMaterial = Resources.Load<Material>(AlphaMaterialPath);
            return alphaMaterial;
        }
    }

    static Mesh QuadMesh
    {
        get
        {
            if (quadMesh != null)
                return quadMesh;
            quadMesh = new Mesh { name = "RitualQuad" };
            quadMesh.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) };
            quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quadMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quadMesh.RecalculateBounds();
            return quadMesh;
        }
    }

    static float Smooth(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    static float EaseOut(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - (1f - x) * (1f - x) * (1f - x);
    }
}
