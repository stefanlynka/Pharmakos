using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Blocks player clicks while a screen transition is in flight, and briefly after any button
/// click, so double-clicking Continue / an overworld node can't trigger the action twice.
///
/// UI clicks are swallowed by an invisible top-most overlay. Code that reads the mouse directly
/// (Input.GetMouseButtonDown + raycasts) must check <see cref="IsBlocked"/> itself.
/// A safety cap guarantees the lock always releases, so a transition that never finishes
/// can't soft-lock input.
/// </summary>
public static class InputGuard
{
    /// <summary>How long a button click blocks further clicks.</summary>
    public const float ClickBlockDuration = 0.35f;

    /// <summary>Longest a transition may hold the lock before it is force-released.</summary>
    public const float TransitionSafetyCap = 5f;

    static float blockedUntil;
    static int transitionDepth;
    static float transitionSafetyUntil;
    static GameObject blocker;

    public static bool IsBlocked
    {
        get
        {
            float now = Time.unscaledTime;
            if (transitionDepth > 0 && now >= transitionSafetyUntil)
                transitionDepth = 0; // a transition never reported its end; release

            return now < blockedUntil || transitionDepth > 0;
        }
    }

    /// <summary>Block clicks for <paramref name="seconds"/> (extends, never shortens, an existing block).</summary>
    public static void BlockFor(float seconds)
    {
        blockedUntil = Mathf.Max(blockedUntil, Time.unscaledTime + seconds);
        EnsureBlocker();
    }

    public static void BeginTransition()
    {
        transitionDepth++;
        transitionSafetyUntil = Time.unscaledTime + TransitionSafetyCap;
        EnsureBlocker();
    }

    public static void EndTransition()
    {
        if (transitionDepth > 0) transitionDepth--;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        blockedUntil = 0f;
        transitionDepth = 0;
        transitionSafetyUntil = 0f;
        blocker = null;
    }

    static void EnsureBlocker()
    {
        if (blocker != null || !Application.isPlaying) return;

        blocker = new GameObject("InputGuardBlocker", typeof(RectTransform));
        Object.DontDestroyOnLoad(blocker);

        Canvas canvas = blocker.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // above every other canvas
        blocker.AddComponent<GraphicRaycaster>();

        GameObject catcher = new GameObject("ClickCatcher", typeof(RectTransform));
        catcher.transform.SetParent(blocker.transform, false);
        RectTransform rect = catcher.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = catcher.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        blocker.AddComponent<InputGuardBlocker>().Catcher = catcher;
        catcher.SetActive(false);
    }
}
