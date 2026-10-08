using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Paints a sprite in and wipes it out along the sprite's local up axis, like a brushstroke.
/// Requires a material using the Pharmakos/BrushStrokeReveal shader. Progress is written to SpriteRenderer.color
/// (r = reveal, g = erase, b = noise seed, a = opacity) so many strokes can share one material and still batch.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public sealed class BrushStroke : MonoBehaviour
{
    [Tooltip("Overall opacity written to the alpha channel.")]
    [SerializeField]
    [Range(0f, 1f)]
    float opacity = 1f;

    static readonly int StrokeAngleId = Shader.PropertyToID("_StrokeAngle");

    SpriteRenderer spriteRenderer;
    MaterialPropertyBlock propertyBlock;
    Coroutine playRoutine;
    float noiseSeed;
    float reveal;
    float erase;

    public SpriteRenderer SpriteRenderer
    {
        get
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            return spriteRenderer;
        }
    }

    public bool IsPlaying => playRoutine != null;

    public float Opacity
    {
        get => opacity;
        set => opacity = Mathf.Clamp01(value);
    }

    void Awake()
    {
        SetProgress(0f, 0f);
    }

    void OnDisable()
    {
        playRoutine = null;
    }

    /// <summary>Paints in, holds, then wipes out.</summary>
    public void Play(float paintIn, float hold, float wipeOut, Action onDone = null)
    {
        Stop();
        noiseSeed = UnityEngine.Random.value;
        if (!isActiveAndEnabled)
        {
            SetProgress(1f, 1f);
            onDone?.Invoke();
            return;
        }
        playRoutine = StartCoroutine(PlayRoutine(paintIn, hold, wipeOut, onDone));
    }

    /// <summary>Paints in from hidden and stays visible until <see cref="WipeOut"/> is called.</summary>
    public void PaintIn(float duration, Action onDone = null)
    {
        Stop();
        noiseSeed = UnityEngine.Random.value;
        if (!isActiveAndEnabled)
        {
            SetProgress(1f, 0f);
            onDone?.Invoke();
            return;
        }
        playRoutine = StartCoroutine(PaintInRoutine(duration, onDone));
    }

    /// <summary>Wipes out from the current state, continuing any paint-in still in progress.</summary>
    public void WipeOut(float duration, Action onDone = null)
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        if (!isActiveAndEnabled)
        {
            SetProgress(1f, 1f);
            onDone?.Invoke();
            return;
        }
        playRoutine = StartCoroutine(WipeOutRoutine(duration, onDone));
    }

    public void Stop()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        SetProgress(0f, 0f);
    }

    /// <summary>
    /// Overrides the material's stroke angle for this renderer (degrees clockwise from up; 45 = bottom-left to top-right).
    /// This breaks batching for this renderer, so flurries of strokes should rotate their transforms instead.
    /// </summary>
    public void SetStrokeAngle(float degrees)
    {
        propertyBlock ??= new MaterialPropertyBlock();
        SpriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(StrokeAngleId, degrees);
        SpriteRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetProgress(float reveal, float erase)
    {
        this.reveal = reveal;
        this.erase = erase;
        SpriteRenderer.color = EncodeState(reveal, erase, noiseSeed, opacity);
    }

    /// <summary>The vertex color the BrushStrokeReveal shader reads its animation state from.</summary>
    public static Color EncodeState(float reveal, float erase, float noiseSeed, float opacity)
    {
        return new Color(reveal, erase, noiseSeed, opacity);
    }

    IEnumerator PlayRoutine(float paintIn, float hold, float wipeOut, Action onDone)
    {
        yield return AnimateReveal(paintIn);

        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        yield return AnimateErase(wipeOut);

        playRoutine = null;
        onDone?.Invoke();
    }

    IEnumerator PaintInRoutine(float duration, Action onDone)
    {
        yield return AnimateReveal(duration);
        playRoutine = null;
        onDone?.Invoke();
    }

    IEnumerator WipeOutRoutine(float duration, Action onDone)
    {
        // Let an unfinished reveal keep moving ahead of the erase edge so the stroke never shows a gap.
        float startReveal = reveal;
        float startErase = erase;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = EaseOutCubic(t / duration);
            SetProgress(Mathf.Lerp(startReveal, 1f, k), Mathf.Lerp(startErase, 1f, k));
            yield return null;
        }
        SetProgress(1f, 1f);
        playRoutine = null;
        onDone?.Invoke();
    }

    IEnumerator AnimateReveal(float duration)
    {
        SetProgress(0f, 0f);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetProgress(EaseOutCubic(t / duration), 0f);
            yield return null;
        }
        SetProgress(1f, 0f);
    }

    IEnumerator AnimateErase(float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetProgress(1f, EaseOutCubic(t / duration));
            yield return null;
        }
        SetProgress(1f, 1f);
    }

    /// <summary>The easing used for both edges of a stroke: fast start, slow finish, like a brush.</summary>
    public static float EaseOutCubic(float x)
    {
        float inv = 1f - Mathf.Clamp01(x);
        return 1f - inv * inv * inv;
    }
}
