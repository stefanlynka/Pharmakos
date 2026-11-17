using System.Collections.Generic;

/// <summary>
/// The Map button's view: shows the overworld over the current screen (combat, events, temple,
/// shop, rewards) so the player can look around, then puts them back where they were.
///
/// Nothing on the map can be selected, the screen underneath is only hidden (never exited), and
/// the Controller freezes combat/animation updates while the view is open, so nothing can move
/// on underneath it.
/// </summary>
public static class OverworldMapView
{
    /// <summary>Screens that stay visible over the map view.</summary>
    static readonly ScreenName[] KeepVisible =
    {
        ScreenName.Overworld,
        ScreenName.StatusButton,
        ScreenName.MapButton,
        ScreenName.Status,
        ScreenName.Popup,
        ScreenName.Pause,
        ScreenName.Options,
        ScreenName.Blank,
        // TutorialPresenter drives its own canvas group; leave it alone.
        ScreenName.Tutorial,
        ScreenName.NewTutorialScreen,
    };

    static readonly List<Screen> suppressedScreens = new List<Screen>();

    public static bool IsOpen { get; private set; }

    /// <summary>True while the view is open; input that reads the mouse directly must ignore presses.</summary>
    public static bool BlocksGameInput => IsOpen;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsOpen = false;
        suppressedScreens.Clear();
    }

    public static bool Open(OverworldMapNode focusNode)
    {
        if (IsOpen) return true;

        ScreenHandler handler = ScreenHandler.Instance;
        if (handler == null
            || !handler.TryGetScreen(ScreenName.Overworld, out Screen screen)
            || !(screen is OverworldScreen overworld))
            return false;

        IsOpen = true;

        suppressedScreens.Clear();
        foreach (Screen other in handler.Screens)
        {
            if (other == null || !other.IsActive || other.IsSuppressed || ShouldKeepVisible(other.Name))
                continue;

            other.SetSuppressed(true);
            suppressedScreens.Add(other);
        }

        overworld.EnterMapView(focusNode);
        return true;
    }

    public static void Close()
    {
        if (!IsOpen) return;

        if (ScreenHandler.Instance != null
            && ScreenHandler.Instance.TryGetScreen(ScreenName.Overworld, out Screen screen)
            && screen is OverworldScreen overworld)
            overworld.ExitMapView();

        RestoreSuppressedScreens();
    }

    /// <summary>The overworld screen was entered or exited for real while the view was open.</summary>
    public static void NotifyClosedExternally()
    {
        if (!IsOpen) return;
        RestoreSuppressedScreens();
    }

    static void RestoreSuppressedScreens()
    {
        IsOpen = false;
        foreach (Screen suppressed in suppressedScreens)
        {
            if (suppressed != null)
                suppressed.SetSuppressed(false);
        }
        suppressedScreens.Clear();
    }

    static bool ShouldKeepVisible(ScreenName name)
    {
        foreach (ScreenName keep in KeepVisible)
        {
            if (keep == name)
                return true;
        }
        return false;
    }
}
