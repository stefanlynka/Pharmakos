using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectiles painted from brushstrokes (the game's VFX language, see BrushStrokeFlurryVfx): a head of strokes that
/// points along the flight, a trail of strokes that wipe out behind it, and a burst of strokes radiating from the
/// target on impact. Thrown by ArcProjectileAnimation when a style has a StrokeFolder.
/// Stroke sets live in Resources/Images/Particles/&lt;Set&gt; (Water and Fire are painted; Blood, Ink and Gold are
/// recoloured from Water by Tools/VFX/build_stroke_sets.py).
/// </summary>
public static class StrokeProjectiles
{
    public static readonly ArcProjectileStyle Water = Make("Images/Particles/Water", "ImpactWater");
    public static readonly ArcProjectileStyle Fire = Make("Images/Particles/Fire", "ImpactFire");
    public static readonly ArcProjectileStyle Blood = Make("Images/Particles/Blood", "ImpactBlood");
    public static readonly ArcProjectileStyle Ink = Make("Images/Particles/Ink", "ImpactInk");
    public static readonly ArcProjectileStyle Gold = Make("Images/Particles/Gold", "ImpactGold");

    static ArcProjectileStyle Make(string folder, string impactCue)
    {
        return new ArcProjectileStyle
        {
            StrokeFolder = folder,
            ImpactCue = impactCue,
            SoundPath = null,
            ArcHeight = AttackArrowPreview.SpellArcHeight,
            Speed = 26f,
            MinDuration = 0.3f,
            MaxDuration = 0.8f,
        };
    }
}

public class StrokeProjectileView : MonoBehaviour
{
    const string MaterialPath = "VFX/Materials/BrushStroke";
    const string SortingLayerName = "CardLayer";
    const int SortingOrderBase = 6000;

    // Head
    const int HeadStrokes = 3;
    // Heads sit this far either side of the path (authoring units), so the head reads as a glob, not a line.
    const float HeadSpread = 0.12f;
    const float HeadScale = 0.45f;
    // Trail
    const float TrailSpacing = 0.35f;
    const float TrailScaleMin = 0.24f;
    const float TrailScaleMax = 0.36f;
    const float TrailPaint = 0.04f;
    const float TrailHold = 0.05f;
    const float TrailWipe = 0.3f;
    // Impact
    const int ImpactStrokes = 10;
    const float ImpactScaleMin = 0.28f;
    const float ImpactScaleMax = 0.46f;
    const float ImpactPaint = 0.06f;
    const float ImpactHold = 0.06f;
    const float ImpactWipe = 0.3f;
    const float ImpactSpread = 0.3f;
    const float ImpactDrift = 1.6f;

    static readonly float[] HeadFan = { 0f, 34f, -34f };

    static Vector3 CameraForward()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.forward : Vector3.forward;
    }

    static Material material;
    static readonly Dictionary<string, Sprite[]> spriteSets = new Dictionary<string, Sprite[]>();

    class Stroke
    {
        public SpriteRenderer Renderer;
        public float Born;
        public float Paint;
        public float Hold;
        public float Wipe;
        public float Seed;
        public Vector3 Position;
        public Vector3 Direction;
        public float Drift;
        public bool Head;
    }

    readonly List<Stroke> strokes = new List<Stroke>();
    readonly Stack<SpriteRenderer> free = new Stack<SpriteRenderer>();
    Sprite[] sprites;
    Vector3 lastTrailPoint;
    bool hasTrailPoint;
    bool released;
    int spawnCount;
    float now;

    public static StrokeProjectileView Create(ArcProjectileStyle style)
    {
        return Create(style.StrokeFolder, true);
    }

    /// <summary>A stroke painter for a Resources folder of stroke sprites; with a head it can fly (MoveHead).</summary>
    public static StrokeProjectileView Create(string folder, bool withHead, float headScale = HeadScale)
    {
        Sprite[] sprites = LoadSprites(folder);
        if (sprites.Length == 0 || LoadMaterial() == null)
            return null;

        GameObject root = new GameObject(withHead ? "StrokeProjectile" : "StrokePainter");
        StrokeProjectileView view = root.AddComponent<StrokeProjectileView>();
        view.sprites = sprites;
        if (withHead)
        {
            for (int i = 0; i < HeadStrokes; i++)
                view.Spawn(Vector3.zero, Vector3.up, headScale * (1f - 0.22f * i), 0.05f, 999f, 0f, 0f, true);
        }
        else
        {
            view.released = true;
        }
        return view;
    }

    /// <summary>Paints one stroke facing the camera, its length along direction (on screen), drifting that way.</summary>
    public void Paint(Vector3 position, Vector3 direction, float scale, float paint, float hold, float wipe, float drift = 0f)
    {
        Spawn(position, direction, scale, paint, hold, wipe, drift, false);
    }

    /// <summary>A ring of strokes flung outward from a point (scale multiplies the impact strokes).</summary>
    public void Burst(Vector3 point, Vector3 direction, int count, float scale, float spread, float drift)
    {
        Camera cam = Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 travel = Vector3.ProjectOnPlane(direction, forward);
        if (travel.sqrMagnitude < 0.0001f)
            travel = cam != null ? cam.transform.up : Vector3.up;
        travel.Normalize();

        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * 360f + Random.Range(-18f, 18f);
            Vector3 dir = Quaternion.AngleAxis(angle, forward) * travel;
            Vector3 start = point + dir * spread * CueVfxUtil.Unit * Random.Range(0.4f, 1f);
            Spawn(start, dir, Random.Range(ImpactScaleMin, ImpactScaleMax) * scale, ImpactPaint, ImpactHold, ImpactWipe,
                drift * Random.Range(0.6f, 1.2f), false);
        }
    }

    /// <summary>Moves the head along the flight, leaving trail strokes behind it.</summary>
    public void MoveHead(Vector3 position, Vector3 tangent)
    {
        for (int i = 0; i < strokes.Count; i++)
        {
            Stroke stroke = strokes[i];
            if (!stroke.Head)
                continue;
            // Heads trail each other a little and fan out either side of the path, so it reads as a glob, not a line.
            Vector3 side = Vector3.Cross(tangent, CameraForward()).normalized;
            float sideSign = i == 0 ? 0f : (i % 2 == 0 ? -1f : 1f);
            stroke.Position = position - tangent.normalized * (0.25f * i * CueVfxUtil.Unit) + side * sideSign * HeadSpread * CueVfxUtil.Unit;
            stroke.Direction = Quaternion.AngleAxis(HeadFan[i % HeadFan.Length], CameraForward()) * tangent;
        }

        if (!hasTrailPoint)
        {
            lastTrailPoint = position;
            hasTrailPoint = true;
            return;
        }

        float distance = Vector3.Distance(position, lastTrailPoint);
        int count = Mathf.Min(6, Mathf.FloorToInt(distance / (TrailSpacing * CueVfxUtil.Unit)));
        for (int i = 1; i <= count; i++)
        {
            Vector3 point = Vector3.Lerp(lastTrailPoint, position, i / (float)count);
            Spawn(point, Quaternion.AngleAxis(Random.Range(-15f, 15f), CameraForward()) * tangent, Random.Range(TrailScaleMin, TrailScaleMax), TrailPaint, TrailHold, TrailWipe, 0f, false);
        }
        if (count > 0)
            lastTrailPoint = position;
    }

    /// <summary>The projectile hits: strokes burst outward from the point, painting in and wiping away.</summary>
    public void Impact(Vector3 point, Vector3 arriveDirection)
    {
        Camera cam = Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 travel = Vector3.ProjectOnPlane(arriveDirection, forward);
        if (travel.sqrMagnitude < 0.0001f)
            travel = cam != null ? cam.transform.up : Vector3.up;
        travel.Normalize();

        for (int i = 0; i < ImpactStrokes; i++)
        {
            // Fan forward around the travel direction, with a few strokes flung back.
            float angle = (i / (float)ImpactStrokes) * 360f + Random.Range(-18f, 18f);
            Vector3 direction = Quaternion.AngleAxis(angle, forward) * travel;
            float forwardBias = Mathf.Max(0f, Vector3.Dot(direction, travel));
            Vector3 start = point + direction * ImpactSpread * CueVfxUtil.Unit * Random.Range(0.4f, 1f);
            float scale = Random.Range(ImpactScaleMin, ImpactScaleMax) * (0.8f + 0.4f * forwardBias);
            Spawn(start, direction, scale, ImpactPaint, ImpactHold, ImpactWipe, ImpactDrift * (0.6f + forwardBias), false);
        }
    }

    /// <summary>The flight is over: the head wipes out and the object removes itself once every stroke is gone.</summary>
    public void Release()
    {
        released = true;
        foreach (Stroke stroke in strokes)
        {
            if (!stroke.Head)
                continue;
            stroke.Head = false;
            stroke.Born = now - stroke.Paint;
            stroke.Hold = 0f;
            stroke.Wipe = 0.16f;
        }
    }

    void Spawn(Vector3 position, Vector3 direction, float scale, float paint, float hold, float wipe, float drift, bool head)
    {
        // Sizes and drifts are authored for a card about 2.5 units wide; Unit scales them to the board's cards.
        scale *= CueVfxUtil.Unit;
        drift *= CueVfxUtil.Unit;

        SpriteRenderer renderer = GetRenderer();
        renderer.sprite = sprites[Random.Range(0, sprites.Length)];
        renderer.sortingOrder = SortingOrderBase + (spawnCount++ % 1000);
        float flip = Random.value < 0.5f ? -1f : 1f;
        renderer.transform.localScale = new Vector3(scale * flip, scale, scale);

        Stroke stroke = new Stroke
        {
            Renderer = renderer,
            Born = now,
            Paint = paint,
            Hold = hold,
            Wipe = wipe,
            Seed = Random.value,
            Position = position,
            Direction = direction,
            Drift = drift,
            Head = head,
        };
        strokes.Add(stroke);
        Apply(stroke, 0f);
    }

    void LateUpdate()
    {
        now += Time.deltaTime;
        for (int i = strokes.Count - 1; i >= 0; i--)
        {
            Stroke stroke = strokes[i];
            float age = now - stroke.Born;
            if (!stroke.Head && age >= stroke.Paint + stroke.Hold + stroke.Wipe)
            {
                stroke.Renderer.gameObject.SetActive(false);
                free.Push(stroke.Renderer);
                strokes.RemoveAt(i);
                continue;
            }
            Apply(stroke, age);
        }

        if (released && strokes.Count == 0)
            Destroy(gameObject);
    }

    void Apply(Stroke stroke, float age)
    {
        float reveal = stroke.Paint > 0f ? BrushStroke.EaseOutCubic(age / stroke.Paint) : 1f;
        float erase = 0f;
        float wipeAge = age - stroke.Paint - stroke.Hold;
        if (!stroke.Head && wipeAge > 0f)
            erase = stroke.Wipe > 0f ? BrushStroke.EaseOutCubic(wipeAge / stroke.Wipe) : 1f;
        stroke.Renderer.color = BrushStroke.EncodeState(reveal, erase, stroke.Seed, 1f);

        Vector3 position = stroke.Position;
        if (stroke.Drift > 0f)
            position += stroke.Direction.normalized * stroke.Drift * age * (1f - 0.5f * Mathf.Clamp01(age / 0.4f));

        Transform t = stroke.Renderer.transform;
        t.position = position;
        Camera cam = Camera.main;
        if (cam != null)
        {
            // Face the camera, with the stroke's painted length along its direction on screen.
            Vector3 forward = cam.transform.forward;
            Vector3 up = Vector3.ProjectOnPlane(stroke.Direction, forward);
            if (up.sqrMagnitude < 0.0001f)
                up = cam.transform.up;
            t.rotation = Quaternion.LookRotation(forward, up);
        }
    }

    SpriteRenderer GetRenderer()
    {
        if (free.Count > 0)
        {
            SpriteRenderer reused = free.Pop();
            reused.gameObject.SetActive(true);
            return reused;
        }

        GameObject strokeObject = new GameObject("Stroke");
        strokeObject.transform.SetParent(transform, false);
        SpriteRenderer renderer = strokeObject.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = material;
        int layer = SortingLayer.NameToID(SortingLayerName);
        renderer.sortingLayerID = SortingLayer.IsValid(layer) ? layer : 0;
        return renderer;
    }

    static Material LoadMaterial()
    {
        if (material == null)
            material = Resources.Load<Material>(MaterialPath);
        return material;
    }

    public static Sprite[] LoadSprites(string folder)
    {
        if (string.IsNullOrEmpty(folder))
            return new Sprite[0];
        if (!spriteSets.TryGetValue(folder, out Sprite[] set))
        {
            set = Resources.LoadAll<Sprite>(folder);
            spriteSets[folder] = set;
            if (set.Length == 0)
                Debug.LogWarning("StrokeProjectile: no sprites in Resources/" + folder);
        }
        return set;
    }
}
