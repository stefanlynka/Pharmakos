using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Covers an area with a flurry of pooled <see cref="BrushStroke"/>s that paint in and wipe out over a short window.
/// Strokes lie in this transform's local XY plane; a stroke angle of 0 paints along this transform's up axis.
/// Strokes live under a separate runtime container so they don't inherit this transform's scale and
/// can be recycled safely when this object is disabled.
/// </summary>
[DisallowMultipleComponent]
public sealed class BrushStrokeFlurry : MonoBehaviour
{
    const string DefaultStrokePrefabPath = "Prefabs/Animations/BrushStroke";

    [Tooltip("Stroke prefab (SpriteRenderer + BrushStroke using the BrushStroke material). Defaults to Resources/" + DefaultStrokePrefabPath + ".")]
    [SerializeField]
    BrushStroke strokePrefab;

    [Tooltip("Sprites picked at random for each stroke. If empty, the prefab's sprite is used.")]
    [SerializeField]
    Sprite[] sprites = new Sprite[0];

    [Header("Flurry")]
    [SerializeField]
    [Min(1)]
    int strokeCount = 40;

    [Tooltip("Seconds over which stroke start times are spread.")]
    [SerializeField]
    [Min(0f)]
    float duration = 1.5f;

    [Tooltip("Area used by Play() with no arguments, in this transform's local space.")]
    [SerializeField]
    Bounds localArea = new Bounds(Vector3.zero, new Vector3(2f, 2f, 0f));

    [Header("Per Stroke (min, max)")]
    [SerializeField]
    Vector2 paintInRange = new Vector2(0.12f, 0.25f);

    [SerializeField]
    Vector2 holdRange = new Vector2(0.05f, 0.3f);

    [SerializeField]
    Vector2 wipeOutRange = new Vector2(0.15f, 0.3f);

    [SerializeField]
    Vector2 scaleRange = new Vector2(0.6f, 1.2f);

    [Tooltip("Degrees around this transform's forward axis. 0 paints bottom to top.")]
    [SerializeField]
    float strokeAngle = 0f;

    [SerializeField]
    [Min(0f)]
    float angleJitter = 20f;

    [Header("Sorting")]
    [Tooltip("Each new stroke gets a higher sorting order so later strokes paint over earlier ones.")]
    [SerializeField]
    int sortingOrderStart = 100;

    [Tooltip("Sorting orders cycle within [start, start + range).")]
    [SerializeField]
    [Min(1)]
    int sortingOrderRange = 1000;

    [Header("Testing")]
    [SerializeField]
    KeyCode testKey = KeyCode.None;

    [SerializeField]
    bool playOnEnable;

    ObjectPool<BrushStroke> pool;
    readonly List<BrushStroke> activeStrokes = new List<BrushStroke>();
    Transform container;
    int spawnCounter;

    ObjectPool<BrushStroke> Pool
    {
        get
        {
            if (pool == null)
                pool = new ObjectPool<BrushStroke>(CreateStroke, OnStrokeGet, OnStrokeRelease, OnStrokeDestroy, false, 32);
            return pool;
        }
    }

    void Awake()
    {
        if (strokePrefab == null)
            strokePrefab = Resources.Load<BrushStroke>(DefaultStrokePrefabPath);
    }

    void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (container == null)
            return;
        for (int i = activeStrokes.Count - 1; i >= 0; i--)
            Pool.Release(activeStrokes[i]);
        activeStrokes.Clear();
    }

    void OnDestroy()
    {
        activeStrokes.Clear();
        pool?.Dispose();
        if (container != null)
            Destroy(container.gameObject);
    }

    void Update()
    {
        if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
            Play();
    }

    [ContextMenu("Play")]
    public void Play()
    {
        Play(localArea, true, strokeCount, duration);
    }

    public void Play(Bounds worldArea)
    {
        Play(worldArea, false, strokeCount, duration);
    }

    public void Play(Bounds worldArea, int count, float window)
    {
        Play(worldArea, false, count, window);
    }

    public void Play(Renderer target)
    {
        Play(target.bounds);
    }

    void Play(Bounds area, bool areaIsLocal, int count, float window)
    {
        if (strokePrefab == null)
        {
            Debug.LogWarning($"{nameof(BrushStrokeFlurry)} on {name} has no stroke prefab.", this);
            return;
        }
        StartCoroutine(FlurryRoutine(area, areaIsLocal, count, window));
    }

    IEnumerator FlurryRoutine(Bounds area, bool areaIsLocal, int count, float window)
    {
        float slot = window / count;
        float elapsed = 0f;
        for (int i = 0; i < count; i++)
        {
            // One random start time per evenly sized slot keeps the flurry steady without clumping.
            float spawnAt = (i + Random.value) * slot;
            while (elapsed < spawnAt)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            SpawnStroke(area, areaIsLocal);
        }
    }

    void SpawnStroke(Bounds area, bool areaIsLocal)
    {
        BrushStroke stroke = Pool.Get();
        activeStrokes.Add(stroke);

        Vector3 position = new Vector3(
            Random.Range(area.min.x, area.max.x),
            Random.Range(area.min.y, area.max.y),
            Random.Range(area.min.z, area.max.z));
        if (areaIsLocal)
            position = transform.TransformPoint(position);

        float angle = strokeAngle + Random.Range(-angleJitter, angleJitter);
        stroke.transform.SetPositionAndRotation(position, transform.rotation * Quaternion.Euler(0f, 0f, angle));
        stroke.transform.localScale = Vector3.one * RandomIn(scaleRange);

        SpriteRenderer spriteRenderer = stroke.SpriteRenderer;
        if (sprites.Length > 0)
            spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
        spriteRenderer.sortingOrder = sortingOrderStart + (spawnCounter++ % sortingOrderRange);

        stroke.Play(RandomIn(paintInRange), RandomIn(holdRange), RandomIn(wipeOutRange), () => ReleaseStroke(stroke));
    }

    void ReleaseStroke(BrushStroke stroke)
    {
        if (activeStrokes.Remove(stroke))
            Pool.Release(stroke);
    }

    BrushStroke CreateStroke()
    {
        if (container == null)
            container = new GameObject($"{name} Strokes").transform;
        BrushStroke stroke = Instantiate(strokePrefab, container);
        stroke.gameObject.SetActive(false);
        return stroke;
    }

    static void OnStrokeGet(BrushStroke stroke)
    {
        stroke.gameObject.SetActive(true);
    }

    static void OnStrokeRelease(BrushStroke stroke)
    {
        stroke.Stop();
        stroke.gameObject.SetActive(false);
    }

    static void OnStrokeDestroy(BrushStroke stroke)
    {
        if (stroke != null)
            Destroy(stroke.gameObject);
    }

    static float RandomIn(Vector2 range)
    {
        return Random.Range(range.x, range.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(localArea.center, localArea.size);
    }
}
