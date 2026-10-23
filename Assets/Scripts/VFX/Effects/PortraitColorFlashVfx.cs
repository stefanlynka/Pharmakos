using System;
using UnityEngine;

/// <summary>
/// Brief colour wash on a player portrait. The anchor is a ViewPlayer (or the portrait itself).
/// QueueHold matches the wash, so an animation waiting on this effect releases as the colour fades out.
/// </summary>
public class PortraitColorFlashVfx : VfxEffect
{
    public Color FlashColor = new Color(1f, 1f, 1f, 0.3f);
    public float Duration = 0.8f;

    public override void Play(VfxContext context, Action onComplete)
    {
        ViewPlayerPortrait portrait = FindPortrait(context);
        if (portrait != null)
            portrait.PlayColorFlash(FlashColor, Duration, null);

        CompleteAfterHold(onComplete);
    }

    private ViewPlayerPortrait FindPortrait(VfxContext context)
    {
        Transform anchor = context.GetAnchorTransform(Anchor);
        if (anchor == null)
            return null;

        ViewPlayerPortrait portrait = anchor.GetComponent<ViewPlayerPortrait>();
        if (portrait != null)
            return portrait;

        ViewPlayer player = anchor.GetComponent<ViewPlayer>();
        if (player == null)
            player = anchor.GetComponentInParent<ViewPlayer>();
        if (player != null && player.ViewPlayerPortrait != null)
            return player.ViewPlayerPortrait;

        return anchor.GetComponentInChildren<ViewPlayerPortrait>(true);
    }
}
