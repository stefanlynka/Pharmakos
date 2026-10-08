using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Covers an area with a flurry of painted brushstrokes (Pharmakos/BrushStrokeReveal) that paint in, hold, and wipe out.
/// Every stroke lives for the same PaintIn + Hold + WipeOut time; only the rate at which strokes are created changes,
/// following RateOverTime. Strokes are spread over a coverage grid, always filling the least-covered cell next, so a
/// high enough rate fully obscures the area.
/// With an anchor, the effect is parented to it and placed in its local space (in front of its local -Z face), so it
/// follows, scales, and sorts with e.g. a CardView; Offset and LiftTowardCamera are ignored in that case.
/// </summary>
public class BrushStrokeFlurryVfx : VfxEffect
{
    private const string DefaultMaterialPath = "VFX/Materials/BrushStroke";
    private static Material defaultMaterial;
    private static readonly Dictionary<string, Sprite[]> spritesByFolder = new Dictionary<string, Sprite[]>();
    private static readonly Dictionary<string, AudioClip> soundsByPath = new Dictionary<string, AudioClip>();

    // Every sprite in this Resources folder is used, unless Sprites is set
    public string SpriteFolder = "";
    public Sprite[] Sprites = null;
    public Material Material = null;

    // Seconds during which strokes are created; strokes still alive at the end finish their own lifetime
    public float Duration = 2f;
    // Strokes per second when RateOverTime is 1
    public float PeakRate = 40f;
    // x: 0-1 through Duration, y: fraction of PeakRate
    public AnimationCurve RateOverTime = AnimationCurve.Constant(0f, 1f, 1f);

    public float PaintInDuration = 0.2f;
    public float HoldDuration = 0.35f;
    public float WipeOutDuration = 0.25f;

    // Area that stroke centres are spread over, in the anchor's local space. Zero size uses the anchor's BoxCollider.
    public Vector2 AreaSize = Vector2.zero;
    public Vector2 AreaCenter = Vector2.zero;
    // Approximate size of one stroke's painted footprint; the area is split into cells of about this size for even coverage
    public Vector2 CoverageCellSize = new Vector2(1.3f, 1.9f);
    // Distance in front of the anchor's face (towards its local -Z)
    public float Lift = 0.5f;

    public float ScaleMin = 0.9f;
    public float ScaleMax = 1.3f;
    // 0 paints bottom to top in the anchor's space
    public float StrokeAngle = 0f;
    public float AngleJitter = 15f;
    // Mirrors half the strokes horizontally (via negative scale; the paint direction is unaffected)
    public bool RandomFlipX = true;
    // Units per second each stroke drifts up (the anchor's local +Y) over its lifetime; negative sinks
    public float RiseSpeed = 0f;
    // Side-to-side sway along the anchor's local X while drifting: max offset in units, and full swings per second.
    // Each stroke starts at a random point in the swing so they don't move in unison.
    public float WiggleAmplitude = 0f;
    public float WiggleFrequency = 1.5f;

    // Resources path of a sound played when the effect starts; empty for silence
    public string StartSoundPath = "";
    public float StartSoundVolume = 1f;

    public string SortingLayerName = "CardLayer";
    // Each new stroke gets the next order up from this, so later strokes paint over earlier ones
    public int SortingOrder = 100;

    public float StrokeLifetime => PaintInDuration + HoldDuration + WipeOutDuration;

    public override void Play(VfxContext context, Action onComplete)
    {
        Sprite[] sprites = ResolveSprites();
        Material material = Material != null ? Material : GetDefaultMaterial();
        if (sprites.Length == 0 || material == null)
        {
            Debug.LogWarning("BrushStrokeFlurryVfx: no sprites (folder '" + SpriteFolder + "') or missing material.");
            onComplete?.Invoke();
            return;
        }

        PlayStartSound();

        Transform root = CreateRoot(context, out Vector2 areaCenter, out Vector2 areaSize);
        FlurryRun run = new FlurryRun(this, root, sprites, material, areaCenter, areaSize);

        float totalDuration = Duration + StrokeLifetime;
        Sequence sequence = new Sequence();
        sequence.Add(new Tween(run.Step, 0f, totalDuration, totalDuration));
        sequence.Add(new SequenceAction(run.Destroy));
        sequence.Start();

        CompleteAfterHold(onComplete);
    }

    private Transform CreateRoot(VfxContext context, out Vector2 areaCenter, out Vector2 areaSize)
    {
        areaCenter = AreaCenter;
        areaSize = AreaSize;

        Transform anchor = context.GetAnchorTransform(Anchor);
        if (anchor == null)
        {
            return CreateEffectObject(context, "BrushStrokeFlurryVfx").transform;
        }

        if (areaSize == Vector2.zero)
        {
            BoxCollider box = anchor.GetComponent<BoxCollider>();
            if (box == null) box = anchor.GetComponentInChildren<BoxCollider>();
            if (box != null)
            {
                Vector3 localCenter = anchor.InverseTransformPoint(box.transform.TransformPoint(box.center));
                Vector3 localSize = anchor.InverseTransformVector(box.transform.TransformVector(box.size));
                areaCenter += new Vector2(localCenter.x, localCenter.y);
                areaSize = new Vector2(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y));
            }
        }

        GameObject rootObject = new GameObject("BrushStrokeFlurryVfx");
        rootObject.layer = anchor.gameObject.layer;
        Transform root = rootObject.transform;
        root.SetParent(anchor, false);
        root.localPosition = new Vector3(0f, 0f, -Lift);
        return root;
    }

    private void PlayStartSound()
    {
        if (string.IsNullOrEmpty(StartSoundPath) || View.Instance == null || View.Instance.AudioHandler == null) return;

        if (!soundsByPath.TryGetValue(StartSoundPath, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>(StartSoundPath);
            if (clip == null) Debug.LogWarning("BrushStrokeFlurryVfx: missing sound Resources/" + StartSoundPath);
            soundsByPath[StartSoundPath] = clip;
        }
        View.Instance.AudioHandler.PlayOneShot(clip, StartSoundVolume);
    }

    private Sprite[] ResolveSprites()
    {
        if (Sprites != null && Sprites.Length > 0) return Sprites;
        if (string.IsNullOrEmpty(SpriteFolder)) return Array.Empty<Sprite>();

        if (!spritesByFolder.TryGetValue(SpriteFolder, out Sprite[] sprites))
        {
            sprites = Resources.LoadAll<Sprite>(SpriteFolder);
            spritesByFolder[SpriteFolder] = sprites;
        }
        return sprites;
    }

    public static Material GetDefaultMaterial()
    {
        if (defaultMaterial == null) defaultMaterial = Resources.Load<Material>(DefaultMaterialPath);
        return defaultMaterial;
    }

    private class Stroke
    {
        public SpriteRenderer Renderer;
        public Vector3 StartPosition;
        public float WigglePhase;
        public float SpawnTime;
        public int Cell;
        public float NoiseSeed;
    }

    private class FlurryRun
    {
        private readonly BrushStrokeFlurryVfx settings;
        private readonly Transform root;
        private readonly Sprite[] sprites;
        private readonly Material material;
        private readonly int sortingLayerID;

        private readonly Vector2 areaMin;
        private readonly Vector2 cellSize;
        private readonly int columns;
        private readonly int rows;
        private readonly int[] strokesPerCell;
        private readonly List<int> candidateCells = new List<int>();

        private readonly List<Stroke> activeStrokes = new List<Stroke>();
        private readonly Stack<SpriteRenderer> freeRenderers = new Stack<SpriteRenderer>();

        private float lastElapsed;
        private float spawnBudget;
        private int spawnCount;

        public FlurryRun(BrushStrokeFlurryVfx settings, Transform root, Sprite[] sprites, Material material, Vector2 areaCenter, Vector2 areaSize)
        {
            this.settings = settings;
            this.root = root;
            this.sprites = sprites;
            this.material = material;

            int layerID = SortingLayer.NameToID(settings.SortingLayerName);
            sortingLayerID = SortingLayer.IsValid(layerID) ? layerID : 0;

            areaSize = new Vector2(Mathf.Max(0f, areaSize.x), Mathf.Max(0f, areaSize.y));
            areaMin = areaCenter - areaSize * 0.5f;
            columns = CellCount(areaSize.x, settings.CoverageCellSize.x);
            rows = CellCount(areaSize.y, settings.CoverageCellSize.y);
            cellSize = new Vector2(areaSize.x / columns, areaSize.y / rows);
            strokesPerCell = new int[columns * rows];
        }

        private static int CellCount(float extent, float cell)
        {
            if (cell <= 0f) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(extent / cell));
        }

        public void Step(float elapsed)
        {
            // The anchor (and with it the root) may have been destroyed, or deactivated and pooled, mid-effect
            if (root == null) return;
            if (!root.gameObject.activeInHierarchy)
            {
                Destroy();
                return;
            }

            float deltaTime = elapsed - lastElapsed;
            lastElapsed = elapsed;

            if (elapsed < settings.Duration && settings.Duration > 0f)
            {
                float rate = settings.PeakRate * Mathf.Max(0f, settings.RateOverTime.Evaluate(elapsed / settings.Duration));
                spawnBudget += rate * deltaTime;
                while (spawnBudget >= 1f)
                {
                    spawnBudget -= 1f;
                    Spawn(elapsed);
                }
            }

            for (int i = activeStrokes.Count - 1; i >= 0; i--)
            {
                Stroke stroke = activeStrokes[i];
                float age = elapsed - stroke.SpawnTime;
                if (age >= settings.StrokeLifetime)
                {
                    Retire(i);
                    continue;
                }
                stroke.Renderer.color = EvaluateStroke(stroke, age);
                stroke.Renderer.transform.localPosition = stroke.StartPosition + DriftOffset(stroke, age);
            }
        }

        private Vector3 DriftOffset(Stroke stroke, float age)
        {
            float wiggle = settings.WiggleAmplitude * Mathf.Sin(stroke.WigglePhase + 2f * Mathf.PI * settings.WiggleFrequency * age);
            return new Vector3(wiggle, settings.RiseSpeed * age, 0f);
        }

        private Color EvaluateStroke(Stroke stroke, float age)
        {
            float reveal = settings.PaintInDuration > 0f ? BrushStroke.EaseOutCubic(age / settings.PaintInDuration) : 1f;

            float wipeAge = age - settings.PaintInDuration - settings.HoldDuration;
            float erase = 0f;
            if (wipeAge > 0f)
                erase = settings.WipeOutDuration > 0f ? BrushStroke.EaseOutCubic(wipeAge / settings.WipeOutDuration) : 1f;

            return BrushStroke.EncodeState(reveal, erase, stroke.NoiseSeed, 1f);
        }

        private void Spawn(float time)
        {
            int cell = PickLeastCoveredCell();
            strokesPerCell[cell]++;

            int column = cell % columns;
            int row = cell / columns;
            Vector2 position = areaMin + new Vector2(
                (column + UnityEngine.Random.value) * cellSize.x,
                (row + UnityEngine.Random.value) * cellSize.y);

            SpriteRenderer strokeRenderer = GetRenderer();
            Transform strokeTransform = strokeRenderer.transform;
            strokeTransform.localPosition = new Vector3(position.x, position.y, 0f);
            strokeTransform.localRotation = Quaternion.Euler(0f, 0f, settings.StrokeAngle + UnityEngine.Random.Range(-settings.AngleJitter, settings.AngleJitter));
            float scale = UnityEngine.Random.Range(settings.ScaleMin, settings.ScaleMax);
            float flip = settings.RandomFlipX && UnityEngine.Random.value < 0.5f ? -1f : 1f;
            strokeTransform.localScale = new Vector3(scale * flip, scale, scale);

            strokeRenderer.sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
            strokeRenderer.sortingOrder = settings.SortingOrder + spawnCount % 1000;
            spawnCount++;

            Stroke stroke = new Stroke
            {
                Renderer = strokeRenderer,
                StartPosition = strokeTransform.localPosition,
                WigglePhase = UnityEngine.Random.Range(0f, 2f * Mathf.PI),
                SpawnTime = time,
                Cell = cell,
                NoiseSeed = UnityEngine.Random.value,
            };
            strokeRenderer.color = EvaluateStroke(stroke, 0f);
            activeStrokes.Add(stroke);
        }

        private int PickLeastCoveredCell()
        {
            int fewest = int.MaxValue;
            candidateCells.Clear();
            for (int i = 0; i < strokesPerCell.Length; i++)
            {
                if (strokesPerCell[i] < fewest)
                {
                    fewest = strokesPerCell[i];
                    candidateCells.Clear();
                }
                if (strokesPerCell[i] == fewest) candidateCells.Add(i);
            }
            return candidateCells[UnityEngine.Random.Range(0, candidateCells.Count)];
        }

        private SpriteRenderer GetRenderer()
        {
            if (freeRenderers.Count > 0)
            {
                SpriteRenderer reused = freeRenderers.Pop();
                reused.gameObject.SetActive(true);
                return reused;
            }

            GameObject strokeObject = new GameObject("Stroke");
            strokeObject.layer = root.gameObject.layer;
            strokeObject.transform.SetParent(root, false);
            SpriteRenderer strokeRenderer = strokeObject.AddComponent<SpriteRenderer>();
            strokeRenderer.sharedMaterial = material;
            strokeRenderer.sortingLayerID = sortingLayerID;
            return strokeRenderer;
        }

        private void Retire(int index)
        {
            Stroke stroke = activeStrokes[index];
            activeStrokes.RemoveAt(index);
            strokesPerCell[stroke.Cell]--;
            stroke.Renderer.gameObject.SetActive(false);
            freeRenderers.Push(stroke.Renderer);
        }

        public void Destroy()
        {
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
