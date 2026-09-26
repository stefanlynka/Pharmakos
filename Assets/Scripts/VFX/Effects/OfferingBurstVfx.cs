using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Offering tokens and small offering-coloured particles flung outward from the anchor in the GameField's plane.
/// Every piece also drifts away from the camera over its flight, so older pieces sit behind newer ones.
/// Tokens and particles each fade out quickly once they pass their own fade start distance.
/// </summary>
public class OfferingBurstVfx : VfxEffect
{
    // One token per entry; repeat a type for more tokens of it. Order doesn't matter. Empty falls back to a single Gold token.
    public List<OfferingType> Tokens = new List<OfferingType>();
    public int ParticlesPerToken = 8;

    public float Duration = 1.5f;
    public float StartJitter = 0.25f;
    // Pieces launch at random times within this window (capped at half the Duration); later launches stay in front
    public float SpawnWindow = 1.2f;
    // Distance each piece drifts away from the camera over its flight
    public float Depth = 3f;

    // Each piece travels between (FadeStartDistance + FadeLength) and MaxTravelMultiplier times that,
    // so everything has faded by the end of the Duration
    public float TokenFadeStartDistance = 2.2f;
    public float TokenFadeLength = 0.5f;
    public float TokenMaxTravelMultiplier = 1.8f;

    public float ParticleFadeStartDistance = 2.2f;
    public float ParticleFadeLength = 0.5f;
    public float ParticleMaxTravelMultiplier = 2.6f;

    public float TokenScaleMin = 0.09f;
    public float TokenScaleMax = 0.14f;
    public float TokenSpin = 180f;

    public float ParticleSizeMin = 0.16f;
    public float ParticleSizeMax = 0.64f;
    // Random blend towards white per particle, for variation within each offering colour
    public float ParticleWhiteness = 0.35f;

    public int SortingOrder = 0;
    public Material ParticleMaterial = null;

    public OfferingBurstVfx()
    {
        QueueHold = 0.6f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        BurstRun run = new BurstRun(this, CreateEffectObject(context, "OfferingBurstVfx"));

        Sequence sequence = new Sequence();
        sequence.Add(new Tween(run.Step, 0f, Duration, Duration));
        sequence.Add(new SequenceAction(run.Destroy));
        sequence.Start();

        CompleteAfterHold(onComplete);
    }

    // All positions and directions are in the burst root's local space (aligned with GameField)
    private class Piece
    {
        public Vector3 Start;
        public Vector3 Direction;
        public float Distance;
        public float FadeStartDistance;
        public float FadeLength;
        public float Delay;
        public float Spin;
        public float Scale;
        public Color Color;
    }

    private class BurstRun
    {
        private readonly OfferingBurstVfx settings;
        private readonly GameObject root;
        private readonly Vector3 away;

        private readonly List<SpriteRenderer> tokenRenderers = new List<SpriteRenderer>();
        private readonly List<Piece> tokenPieces = new List<Piece>();

        private readonly ParticleSystem particles;
        private readonly List<Piece> particlePieces = new List<Piece>();
        private readonly ParticleSystem.Particle[] particleBuffer;

        public BurstRun(OfferingBurstVfx settings, GameObject root)
        {
            this.settings = settings;
            this.root = root;
            away = VfxContext.GetAwayFromCameraLocal(root.transform);

            List<OfferingType> types = settings.Tokens.Count > 0 ? settings.Tokens : new List<OfferingType> { OfferingType.Gold };

            CreateTokens(types);
            particles = CreateParticles(types);
            particleBuffer = new ParticleSystem.Particle[particlePieces.Count];
        }

        private void CreateTokens(List<OfferingType> types)
        {
            Dictionary<OfferingType, Sprite> sprites = new Dictionary<OfferingType, Sprite>();

            // Spread tokens evenly around the circle (with jitter), with slots shuffled so each type is mixed around it
            float baseAngle = UnityEngine.Random.Range(0f, 360f);
            float slice = 360f / types.Count;
            int[] slots = new int[types.Count];
            for (int i = 0; i < slots.Length; i++) slots[i] = i;
            for (int i = slots.Length - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1);
                (slots[i], slots[swap]) = (slots[swap], slots[i]);
            }

            for (int i = 0; i < types.Count; i++)
            {
                Sprite sprite = GetSprite(types[i], sprites);
                if (sprite == null) continue;

                GameObject tokenObject = new GameObject("Token_" + types[i]);
                tokenObject.transform.SetParent(root.transform, false);

                SpriteRenderer tokenRenderer = tokenObject.AddComponent<SpriteRenderer>();
                tokenRenderer.sprite = sprite;
                tokenRenderer.sortingOrder = settings.SortingOrder;
                tokenRenderer.color = new Color(1f, 1f, 1f, 0f);

                float angle = baseAngle + slice * slots[i] + UnityEngine.Random.Range(-0.4f, 0.4f) * slice;
                Piece piece = MakePiece(angle, UnityEngine.Random.Range(settings.TokenScaleMin, settings.TokenScaleMax),
                    settings.TokenFadeStartDistance, settings.TokenFadeLength, settings.TokenMaxTravelMultiplier);
                piece.Spin = UnityEngine.Random.Range(-settings.TokenSpin, settings.TokenSpin);

                tokenRenderers.Add(tokenRenderer);
                tokenPieces.Add(piece);
                Apply(tokenRenderer, piece, 0f);
            }
        }

        private ParticleSystem CreateParticles(List<OfferingType> types)
        {
            int count = types.Count * Mathf.Max(0, settings.ParticlesPerToken);
            if (count == 0) return null;

            GameObject particleObject = new GameObject("Particles");
            particleObject.transform.SetParent(root.transform, false);

            ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Particles are positioned manually every frame; the system is only used to render them
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = Mathf.Max(0.05f, settings.Duration);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 0f;
            main.startLifetime = settings.Duration + 1f;
            main.gravityModifier = 0f;
            main.maxParticles = count;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortMode = ParticleSystemSortMode.Distance;
            particleRenderer.sortingOrder = settings.SortingOrder;
            particleRenderer.sharedMaterial = settings.ParticleMaterial != null ? settings.ParticleMaterial : ParticleBurstVfx.GetDefaultMaterial();

            system.Play();

            foreach (OfferingType type in types)
            {
                Color baseColor = OfferingHandler.GetOfferingColor(type);
                for (int i = 0; i < settings.ParticlesPerToken; i++)
                {
                    Piece piece = MakePiece(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(settings.ParticleSizeMin, settings.ParticleSizeMax),
                        settings.ParticleFadeStartDistance, settings.ParticleFadeLength, settings.ParticleMaxTravelMultiplier);
                    piece.Color = Color.Lerp(baseColor, Color.white, UnityEngine.Random.Range(0f, settings.ParticleWhiteness));
                    piece.Color.a = 1f;
                    particlePieces.Add(piece);

                    ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                    {
                        position = piece.Start,
                        velocity = Vector3.zero,
                        startSize = piece.Scale,
                        startLifetime = settings.Duration + 1f,
                        startColor = new Color(piece.Color.r, piece.Color.g, piece.Color.b, 0f),
                    };
                    system.Emit(emitParams, 1);
                }
            }

            return system;
        }

        private Piece MakePiece(float angleDegrees, float scale, float fadeStartDistance, float fadeLength, float maxTravelMultiplier)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * settings.StartJitter;
            float safeFadeLength = Mathf.Max(0.01f, fadeLength);
            float fadeEnd = fadeStartDistance + safeFadeLength;

            return new Piece
            {
                Start = new Vector3(jitter.x, jitter.y, 0f),
                Direction = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f),
                Distance = UnityEngine.Random.Range(fadeEnd, fadeEnd * Mathf.Max(1f, maxTravelMultiplier)),
                FadeStartDistance = fadeStartDistance,
                FadeLength = safeFadeLength,
                Delay = UnityEngine.Random.Range(0f, Mathf.Min(settings.SpawnWindow, settings.Duration * 0.5f)),
                Scale = scale,
                Color = Color.white,
            };
        }

        public void Step(float elapsed)
        {
            if (root == null) return;

            for (int i = 0; i < tokenRenderers.Count; i++)
            {
                Apply(tokenRenderers[i], tokenPieces[i], elapsed);
            }

            if (particles == null) return;

            int count = particles.GetParticles(particleBuffer);
            for (int i = 0; i < count && i < particlePieces.Count; i++)
            {
                Piece piece = particlePieces[i];
                Evaluate(piece, elapsed, out Vector3 position, out float alpha, out float progress);

                particleBuffer[i].position = position;
                particleBuffer[i].startSize = piece.Scale * Mathf.Lerp(1f, 0.6f, progress);
                particleBuffer[i].startColor = new Color(piece.Color.r, piece.Color.g, piece.Color.b, alpha);
            }
            particles.SetParticles(particleBuffer, count);
        }

        private void Apply(SpriteRenderer tokenRenderer, Piece piece, float elapsed)
        {
            Evaluate(piece, elapsed, out Vector3 position, out float alpha, out float progress);

            Transform tokenTransform = tokenRenderer.transform;
            tokenTransform.localPosition = position;
            tokenTransform.localRotation = Quaternion.Euler(0f, 0f, piece.Spin * progress);
            tokenTransform.localScale = Vector3.one * (piece.Scale * Mathf.Lerp(1f, 0.8f, progress));

            Color color = tokenRenderer.color;
            color.a = alpha;
            tokenRenderer.color = color;
        }

        private void Evaluate(Piece piece, float elapsed, out Vector3 position, out float alpha, out float progress)
        {
            if (elapsed < piece.Delay)
            {
                position = piece.Start;
                alpha = 0f;
                progress = 0f;
                return;
            }

            float flightTime = Mathf.Max(0.0001f, settings.Duration - piece.Delay);
            progress = Mathf.Clamp01((elapsed - piece.Delay) / flightTime);

            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            float travelled = piece.Distance * eased;

            position = piece.Start + piece.Direction * travelled + away * (settings.Depth * progress);
            alpha = 1f - Mathf.Clamp01((travelled - piece.FadeStartDistance) / piece.FadeLength);
        }

        public void Destroy()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
        }

        private static Sprite GetSprite(OfferingType type, Dictionary<OfferingType, Sprite> cache)
        {
            if (cache.TryGetValue(type, out Sprite sprite)) return sprite;

            sprite = OfferingHandler.Instance != null
                ? OfferingHandler.Instance.GetOfferingSprite(type)
                : Resources.Load<Sprite>("Images/Icons/Offerings/Tokens/" + type);

            cache[type] = sprite;
            return sprite;
        }
    }
}
