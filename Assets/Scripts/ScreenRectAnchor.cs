using UnityEngine;

/// <summary>
/// A popup anchor for things that are not on any camera the popups can project from, such as items drawn by an
/// offscreen camera into a RawImage (the play history). PopupScreenHandler uses <see cref="ScreenRect"/> (screen
/// pixels) as the anchor's rectangle instead of projecting its transform.
/// </summary>
public class ScreenRectAnchor : MonoBehaviour
{
    public Rect ScreenRect;
}
