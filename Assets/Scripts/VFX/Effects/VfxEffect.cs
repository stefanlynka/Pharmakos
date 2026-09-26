using System;
using UnityEngine;

public enum VfxAnchor
{
    Target,
    Source,
}

public class VfxContext
{
    public ViewTarget Source;
    public ViewTarget Target;
    // Every effect object is parented here so it shares the board's space with the cards it targets
    public Transform Field;

    public VfxContext(ViewTarget source, ViewTarget target)
    {
        Source = source;
        Target = target;

        if (Controller.Instance != null && Controller.Instance.GameField != null)
        {
            Field = Controller.Instance.GameField.transform;
        }
    }

    // Falls back to the other anchor if the requested one isn't on screen
    public Transform GetAnchorTransform(VfxAnchor anchor)
    {
        ViewTarget primary = anchor == VfxAnchor.Target ? Target : Source;
        ViewTarget secondary = anchor == VfxAnchor.Target ? Source : Target;

        if (primary != null) return primary.transform;
        if (secondary != null) return secondary.transform;
        return null;
    }

    /// <summary>
    /// World position for an effect on the anchor. Offset is in GameField space. Lift pulls the point toward the camera
    /// along the camera-to-anchor ray, so it renders in front of the board while staying centred on the anchor on screen.
    /// </summary>
    public Vector3 GetEffectPosition(VfxAnchor anchor, Vector3 offset, float liftTowardCamera)
    {
        Transform anchorTransform = GetAnchorTransform(anchor);
        Vector3 position;
        if (anchorTransform != null) position = anchorTransform.position;
        else if (Field != null) position = Field.position;
        else position = Vector3.zero;

        position += Field != null ? Field.TransformVector(offset) : offset;

        Camera cam = Camera.main;
        if (cam != null && liftTowardCamera != 0f)
        {
            position += (cam.transform.position - position).normalized * liftTowardCamera;
        }

        return position;
    }

    /// <summary>
    /// Creates an empty object under GameField (aligned with it) at the effect position for the anchor.
    /// </summary>
    public GameObject CreateEffectObject(string name, VfxAnchor anchor, Vector3 offset, float liftTowardCamera)
    {
        GameObject effectObject = new GameObject(name);
        if (Field != null) effectObject.transform.SetParent(Field, false);
        effectObject.transform.position = GetEffectPosition(anchor, offset, liftTowardCamera);
        return effectObject;
    }

    // Unit direction pointing away from the camera through the given transform, in that transform's local space
    public static Vector3 GetAwayFromCameraLocal(Transform space)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.forward;

        Vector3 worldDirection = (space.position - cam.transform.position).normalized;
        return space.InverseTransformDirection(worldDirection).normalized;
    }
}

/// <summary>
/// A reusable visual effect. Create a fresh instance per play (see VfxLibrary) and tweak its public fields.
/// QueueHold controls when onComplete is called, which releases the animation queue; the visual itself may keep
/// running afterwards and is responsible for cleaning itself up.
/// Effects are placed under GameField: Offset is in GameField space, and LiftTowardCamera moves the effect toward the
/// camera without shifting it off its anchor on screen.
/// </summary>
public abstract class VfxEffect
{
    public VfxAnchor Anchor = VfxAnchor.Target;
    public Vector3 Offset = Vector3.zero;
    public float LiftTowardCamera = 5f;
    public float QueueHold = 0.3f;

    public abstract void Play(VfxContext context, Action onComplete);

    protected GameObject CreateEffectObject(VfxContext context, string name)
    {
        return context.CreateEffectObject(name, Anchor, Offset, LiftTowardCamera);
    }

    protected void CompleteAfterHold(Action onComplete)
    {
        CompleteAfter(QueueHold, onComplete);
    }

    protected static void CompleteAfter(float delay, Action onComplete)
    {
        if (onComplete == null) return;

        if (delay <= 0f)
        {
            onComplete();
            return;
        }

        Sequence holdSequence = new Sequence();
        holdSequence.Add(new Tween(null, 0, 1, delay));
        holdSequence.Add(new SequenceAction(onComplete));
        holdSequence.Start();
    }
}
