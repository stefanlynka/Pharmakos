using System;
using UnityEngine;

public class Screen : MonoBehaviour
{
    public ScreenName Name;
    public CanvasGroup UIHolder;

    public Camera Camera;

    protected bool neverHide = false;
    public bool ManualHide = false;
    public bool InstantEnter = false;
    public bool InstantExit = false;

    public Action OnEnter;
    public Action OnExit;

    private bool active = false;
    private bool suppressed = false;

    /// <summary>True between Enter and Exit, regardless of the fade.</summary>
    public bool IsActive => active;

    /// <summary>True while the screen is entered but hidden under the map view.</summary>
    public bool IsSuppressed => suppressed;
    public static float ScreenChangeDuration =>
        screenChangeDurationOverride > 0f ? screenChangeDurationOverride : screenChangeDuration;
    private const float screenChangeDuration = 0.6f;
    static float screenChangeDurationOverride = -1f;

    public static void SetScreenChangeDurationOverride(float duration)
    {
        screenChangeDurationOverride = duration;
    }

    public static void ClearScreenChangeDurationOverride()
    {
        screenChangeDurationOverride = -1f;
    }


    protected virtual void Update()
    {
        bool visible = active && !suppressed;
        if (visible && UIHolder.alpha < 1) UIHolder.alpha += Time.deltaTime / ScreenChangeDuration;
        else if (!visible && UIHolder.alpha > 0) UIHolder.alpha -= Time.deltaTime / ScreenChangeDuration;
    }

    public virtual void Enter(bool instant = false)
    {
        if (InstantEnter) instant = true;

        SetUIActive(true, instant);

        if (Camera != null) Camera.enabled = true;

        OnEnter?.Invoke();
        OnEnter = null;
    }
    public virtual void Exit(bool instant = false)
    {
        if (neverHide) return;
        if (InstantExit) instant = true;

        OnExit?.Invoke();
        OnExit = null;

        SetUIActive(false, instant);
        if (Camera != null) Camera.enabled = false;
    }
    public virtual void Init()
    {

    }

    /// <summary>
    /// Fades the screen's UI out (and stops it taking clicks) without exiting it, so it can come
    /// back exactly as it was. Used while the map view covers the current screen.
    /// </summary>
    public void SetSuppressed(bool value, bool instant = false)
    {
        suppressed = value;
        ApplyInteractable();
        if (instant) UIHolder.alpha = active && !suppressed ? 1.0f : 0.0f;
    }

    private void SetUIActive(bool active, bool instant = false)
    {
        this.active = active;
        if (!active) suppressed = false;
        ApplyInteractable();

        if (instant) UIHolder.alpha = active && !suppressed ? 1.0f : 0.0f;
    }

    private void ApplyInteractable()
    {
        bool usable = active && !suppressed;
        UIHolder.interactable = usable;
        UIHolder.blocksRaycasts = usable;
    }
}
