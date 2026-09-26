using System;
using UnityEngine;

/// <summary>
/// Dims the main lights, spotlights the anchor and shakes the screen (see LightingHandler.DoRitualAnimation).
/// </summary>
public class RitualLightingVfx : VfxEffect
{
    public RitualLightingVfx()
    {
        QueueHold = 0f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        Transform anchor = context.GetAnchorTransform(Anchor);
        if (anchor != null && Controller.Instance.LightingHandler != null)
        {
            Controller.Instance.LightingHandler.DoRitualAnimation(anchor.gameObject);
        }

        CompleteAfterHold(onComplete);
    }
}
