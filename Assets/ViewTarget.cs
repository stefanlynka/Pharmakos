using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewTarget : MonoBehaviour
{
    public Action<ViewTarget> OnClick = null;

    public ITarget Target;
}
