using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// While a ritual is selected, spins one offering token per base-cost type around
/// the cursor image and sheds a coloured spray of particles.
/// </summary>
public class RitualCursorOfferings : MonoBehaviour
{
    // Offering.prefab local scale. Tokens are drawn at three quarters of that.
    const float NormalOfferingScale = 0.2f;
    const float TokenScaleFactor = 0.35f;
    const float SpinPeriodSeconds = 2f;
    const float FadeDuration = 0.3f;
    const float OrbitPaddingPixels = 10f;
    const float EmitPerSecond = 110f;
    const int TokenSortingOrder = 21000;
    const int TrailSortingOrder = 20990;

    static readonly OfferingType[] TypeOrder =
    {
        OfferingType.Blood,
        OfferingType.Bone,
        OfferingType.Crop,
        OfferingType.Scroll,
        OfferingType.Gold,
    };

    readonly List<Token> tokens = new List<Token>();
    ViewRitual shownRitual;
    bool tokensBuilt;
    float fade;
    Texture2D trailTexture;
    Material trailMaterial;
    Camera cachedCamera;
    ParticleSystem.Particle[] particleBuffer = System.Array.Empty<ParticleSystem.Particle>();

    class Token
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public ParticleSystem Particles;
        public Color Color;
        public Vector3 LastEmissionPosition;
        public bool HasLastEmissionPosition;
    }

    void LateUpdate()
    {
        ViewRitual selected = null;
        if (View.Instance != null && View.Instance.SelectionHandler != null)
            selected = View.Instance.SelectionHandler.SelectedRitual;

        if (selected != shownRitual)
        {
            shownRitual = selected;
            tokensBuilt = false;
        }

        if (selected != null && !tokensBuilt && OfferingHandler.Instance != null)
        {
            RebuildTokens(selected);
            tokensBuilt = true;
            fade = 0f;
        }

        float target = shownRitual != null && tokens.Count > 0 ? 1f : 0f;
        float fadeStep = FadeDuration <= 0f ? 1f : Time.deltaTime / FadeDuration;
        fade = Mathf.MoveTowards(fade, target, fadeStep);

        if (tokens.Count == 0)
            return;

        // Orbit until the fade finishes. Particles stay in world space, so while fading
        // out they are carried with the token instead of being left behind mid-fade.
        bool fadingOut = target <= 0f;
        if (fade > 0f || (fadingOut && AnyParticlesAlive()))
            LayoutTokens(fade, fadingOut);

        if (fadingOut && fade <= 0f)
        {
            HideTokens();
            SetEmission(0f);
            if (!AnyParticlesAlive())
                ClearTokens();
        }
    }

    void OnDestroy()
    {
        ClearTokens();
        if (trailTexture != null)
            Destroy(trailTexture);
        if (trailMaterial != null)
            Destroy(trailMaterial);
    }

    void RebuildTokens(ViewRitual ritual)
    {
        ClearTokens();
        if (ritual == null || ritual.Ritual == null || OfferingHandler.Instance == null)
            return;

        Dictionary<OfferingType, int> costs = ritual.Ritual.Costs;
        if (costs == null)
            return;

        for (int i = 0; i < TypeOrder.Length; i++)
        {
            OfferingType type = TypeOrder[i];
            if (!costs.TryGetValue(type, out int amount) || amount <= 0)
                continue;

            Sprite sprite = OfferingHandler.Instance.GetOfferingSprite(type);
            if (sprite == null)
                continue;

            var tokenObject = new GameObject("RitualCursorToken_" + type);
            tokenObject.transform.SetParent(transform, false);

            var renderer = tokenObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = TokenSortingOrder;
            renderer.color = new Color(1f, 1f, 1f, 0f);

            Color color = ColorFor(type);
            tokens.Add(new Token
            {
                Transform = tokenObject.transform,
                Renderer = renderer,
                Particles = CreateEmission(color),
                Color = color,
            });
        }
    }

    void LayoutTokens(float alpha, bool carryParticles)
    {
        Camera cam = ResolveCamera();
        if (cam != null)
            cachedCamera = cam;
        else
            cam = cachedCamera;

        CursorHandler cursor = CursorHandler.Instance;
        if (cam == null || cursor == null || !cursor.TryGetCursorImageCenter(out Vector2 center))
        {
            SetTokenAlpha(alpha);
            return;
        }

        float boardDistance = BoardDistance(cam, center);
        float drawDistance = Mathf.Max(cam.nearClipPlane + 0.5f, boardDistance * 0.35f);
        float worldScale = NormalOfferingScale * TokenScaleFactor * (drawDistance / boardDistance);
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
        float localScale = worldScale / parentScale;
        float worldPerPixel = WorldUnitsPerPixel(cam, drawDistance);

        float cursorRadius = 0.5f * Mathf.Max(cursor.ActiveCursorPixelSize.x, cursor.ActiveCursorPixelSize.y);
        float tokenRadiusPixels = 0f;
        for (int i = 0; i < tokens.Count; i++)
        {
            Vector3 extents = tokens[i].Renderer.sprite.bounds.extents;
            float worldRadius = Mathf.Max(extents.x, extents.y) * worldScale;
            tokenRadiusPixels = Mathf.Max(tokenRadiusPixels, worldRadius / worldPerPixel);
        }

        float orbitPixels = cursorRadius + tokenRadiusPixels + OrbitPaddingPixels;
        float spin = -Time.time * (Mathf.PI * 2f / SpinPeriodSeconds);
        Quaternion facing = cam.transform.rotation;

        int count = tokens.Count;
        for (int i = 0; i < count; i++)
        {
            float angle = spin + i * (Mathf.PI * 2f / count);
            Vector2 screen = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbitPixels;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, drawDistance));

            Token token = tokens[i];
            if (carryParticles && token.HasLastEmissionPosition)
                OffsetParticles(token.Particles, world - token.LastEmissionPosition);
            token.LastEmissionPosition = world;
            token.HasLastEmissionPosition = true;

            token.Transform.position = world;
            token.Transform.rotation = facing;
            token.Transform.localScale = new Vector3(localScale, localScale, localScale);

            Color color = token.Renderer.color;
            color.a = alpha;
            token.Renderer.color = color;

            Vector3 extents = token.Renderer.sprite.bounds.extents;
            float tokenPixels = Mathf.Max(extents.x, extents.y) * worldScale / worldPerPixel;
            Transform emission = token.Particles.transform;
            emission.SetPositionAndRotation(world, facing);
            float emissionScale = 1f / parentScale;
            emission.localScale = new Vector3(emissionScale, emissionScale, emissionScale);
            SyncEmission(token.Particles, alpha, worldPerPixel, tokenPixels);
        }
    }

    ParticleSystem CreateEmission(Color color)
    {
        EnsureTrailResources();

        var particleObject = new GameObject("RitualCursorEmission");
        particleObject.transform.SetParent(transform, false);

        var ps = particleObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Color bright = Lift(color, 0.72f) * 1.65f;
        Color mid = Lift(color, 0.28f) * 1.2f;
        bright.a = 1f;
        mid.a = 1f;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(mid, bright);
        main.maxParticles = 220;
        main.gravityModifier = 0f;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.radiusThickness = 0.65f;
        shape.arc = 360f;
        shape.randomDirectionAmount = 0.45f;
        shape.sphericalDirectionAmount = 1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.15f, 0f),
                new GradientAlphaKey(1f, 0.06f),
                new GradientAlphaKey(0.55f, 0.45f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0.2f)));

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = false;
        rotation.z = new ParticleSystem.MinMaxCurve(-3.5f, 3.5f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = 0.02f;
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.35f;
        noise.damping = true;
        noise.octaveCount = 1;
        noise.quality = ParticleSystemNoiseQuality.Low;

        var renderer = particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingOrder = TrailSortingOrder;
        if (trailMaterial != null)
            renderer.sharedMaterial = trailMaterial;

        ps.Play();
        return ps;
    }

    void OffsetParticles(ParticleSystem ps, Vector3 delta)
    {
        if (ps == null || delta.sqrMagnitude < 0.0000001f)
            return;

        int count = ps.particleCount;
        if (count <= 0)
            return;

        if (particleBuffer.Length < count)
            particleBuffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(count)];

        count = ps.GetParticles(particleBuffer);
        for (int i = 0; i < count; i++)
            particleBuffer[i].position += delta;
        ps.SetParticles(particleBuffer, count);
    }

    static void SyncEmission(ParticleSystem ps, float alpha, float worldPerPixel, float tokenPixels)
    {
        if (ps == null)
            return;

        float dust = 3.5f * worldPerPixel;
        float mote = 17f * worldPerPixel;
        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(dust, mote);
        main.startSpeed = new ParticleSystem.MinMaxCurve(16f * worldPerPixel, 62f * worldPerPixel);

        var shape = ps.shape;
        shape.radius = Mathf.Max(5f, tokenPixels * 0.42f) * worldPerPixel;

        var noise = ps.noise;
        noise.strength = new ParticleSystem.MinMaxCurve(14f * worldPerPixel);

        var emission = ps.emission;
        emission.rateOverTime = alpha < 0.02f ? 0f : EmitPerSecond * alpha;
    }

    void SetEmission(float rate)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            ParticleSystem ps = tokens[i].Particles;
            if (ps == null)
                continue;

            var emission = ps.emission;
            emission.rateOverTime = rate;
        }
    }

    bool AnyParticlesAlive()
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            ParticleSystem ps = tokens[i].Particles;
            if (ps != null && ps.particleCount > 0)
                return true;
        }

        return false;
    }

    void SetTokenAlpha(float alpha)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            SpriteRenderer renderer = tokens[i].Renderer;
            if (renderer == null)
                continue;

            renderer.enabled = true;
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }

    void HideTokens()
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            SpriteRenderer renderer = tokens[i].Renderer;
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = 0f;
            renderer.color = color;
            renderer.enabled = false;
        }
    }

    void EnsureTrailResources()
    {
        if (trailTexture != null)
            return;

        const int size = 32;
        trailTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        trailTexture.name = "RitualCursorTrail";
        trailTexture.wrapMode = TextureWrapMode.Clamp;
        trailTexture.filterMode = FilterMode.Bilinear;
        float radius = (size - 1) * 0.5f;
        var center = new Vector2(radius, radius);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float pixelAlpha = Mathf.Clamp01(1f - distance);
                pixelAlpha = Mathf.Pow(pixelAlpha, 0.55f);
                trailTexture.SetPixel(x, y, new Color(1f, 1f, 1f, pixelAlpha));
            }
        }
        trailTexture.Apply();

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return;

        trailMaterial = new Material(shader);
        trailMaterial.mainTexture = trailTexture;
        trailMaterial.SetTexture("_BaseMap", trailTexture);
        trailMaterial.SetTexture("_MainTex", trailTexture);
        trailMaterial.SetColor("_BaseColor", Color.white);
        trailMaterial.SetColor("_Color", Color.white);
        trailMaterial.SetFloat("_Surface", 1f);
        trailMaterial.SetFloat("_Blend", 0f);
        trailMaterial.SetFloat("_SrcBlend", 5f);
        trailMaterial.SetFloat("_DstBlend", 10f);
        trailMaterial.SetFloat("_ZWrite", 0f);
        trailMaterial.SetFloat("_SoftParticlesEnabled", 0f);
        trailMaterial.DisableKeyword("_SOFTPARTICLES_ON");
        trailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        trailMaterial.SetOverrideTag("RenderType", "Transparent");
        trailMaterial.renderQueue = 3000;
    }

    void ClearTokens()
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Particles != null)
                Destroy(tokens[i].Particles.gameObject);
            if (tokens[i].Transform != null)
                Destroy(tokens[i].Transform.gameObject);
        }
        tokens.Clear();
    }

    static Camera ResolveCamera()
    {
        if (ScreenHandler.Instance != null)
        {
            Camera screenCamera = ScreenHandler.Instance.ResolveWorldCamera();
            if (screenCamera != null && screenCamera.isActiveAndEnabled)
                return screenCamera;
        }

        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        return null;
    }

    static float BoardDistance(Camera cam, Vector2 screen)
    {
        Ray ray = cam.ScreenPointToRay(screen);
        Plane board = new Plane(Vector3.forward, Vector3.zero);
        if (board.Raycast(ray, out float distance) && distance > cam.nearClipPlane + 0.05f)
            return distance;

        return 30f;
    }

    static float WorldUnitsPerPixel(Camera cam, float distance)
    {
        float height = cam.orthographic
            ? cam.orthographicSize * 2f
            : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return height / Mathf.Max(1, cam.pixelHeight);
    }

    static Color Lift(Color color, float amount)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        v = Mathf.Lerp(Mathf.Max(v, 0.4f), 1f, amount);
        s = Mathf.Lerp(s, 0.55f, amount * 0.35f);
        Color lifted = Color.HSVToRGB(h, s, Mathf.Clamp01(v));
        lifted.a = 1f;
        return lifted;
    }

    static Color ColorFor(OfferingType type)
    {
        switch (type)
        {
            case OfferingType.Gold:
                return new Color(0.95f, 0.75f, 0.22f, 1f);
            case OfferingType.Blood:
                return new Color(0.72f, 0.05f, 0.08f, 1f);
            case OfferingType.Bone:
                return new Color(0.86f, 0.84f, 0.78f, 1f);
            case OfferingType.Crop:
                return new Color(0.28f, 0.58f, 0.2f, 1f);
            case OfferingType.Scroll:
                return new Color(0.32f, 0.52f, 0.75f, 1f);
            default:
                return Color.white;
        }
    }
}
