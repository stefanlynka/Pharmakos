using System;
using UnityEngine;

public class MagicCircleVfx : VfxEffect
{
    public float Lifetime = 0.55f;
    public float Scale = 2.5f;
    public Vector3 Rotation = new Vector3(-90, 0, 0);

    public MagicCircleVfx()
    {
        QueueHold = 0.55f;
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        Transform anchor = context.GetAnchorTransform(Anchor);
        if (anchor == null)
        {
            Debug.LogError("MagicCircleVfx: no anchor ViewTarget found.");
            onComplete?.Invoke();
            return;
        }

        GameObject circle = View.Instance.CreateRitualAnimation();
        if (circle != null)
        {
            if (context.Field != null) circle.transform.SetParent(context.Field, false);
            circle.SetActive(true);
            circle.transform.position = context.GetEffectPosition(Anchor, Offset, LiftTowardCamera);
            circle.transform.localRotation = Quaternion.Euler(Rotation);
            circle.transform.localScale = Vector3.one * Scale;

            CompleteAfter(Lifetime, () =>
            {
                if (circle != null) UnityEngine.Object.Destroy(circle);
            });
        }

        CompleteAfterHold(onComplete);
    }
}
