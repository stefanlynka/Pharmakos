using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewTarget : MonoBehaviour
{
    public Action<ViewTarget> OnClick = null;

    public ITarget Target;

    /// <summary>What grows while this is hovered as a legal target. Defaults to the whole object.</summary>
    public virtual Transform HoverScaleTransform => transform;
}
