using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Strikes from the source view, with world Z pinned to <see cref="OriginZ"/>, to one or more targets.
/// Extra target ids all fire on the same call. With none set, the context target is used.
/// </summary>
public class LightningStrikeVfx : VfxEffect
{
    public const float OriginZ = -4.5f;

    public List<int> TargetIDs = new List<int>();

    public LightningStrikeVfx()
    {
        QueueHold = 0f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        if (LightningStrikes.Instance != null && context.Source != null)
        {
            Vector3 boltOrigin = context.Source.transform.position;
            boltOrigin.z = OriginZ;

            List<Vector3> points = new List<Vector3>();
            if (TargetIDs != null && TargetIDs.Count > 0)
            {
                for (int i = 0; i < TargetIDs.Count; i++)
                {
                    ViewTarget view = View.Instance.GetViewTargetByID(TargetIDs[i]);
                    if (view != null)
                        points.Add(view.transform.position);
                }
            }
            else if (context.Target != null)
            {
                points.Add(context.Target.transform.position);
            }

            if (points.Count > 0)
                LightningStrikes.Instance.Strike(boltOrigin, points);
        }

        CompleteAfterHold(onComplete);
    }
}
