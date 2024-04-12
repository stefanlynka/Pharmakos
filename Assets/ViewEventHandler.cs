using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewEventHandler : MonoBehaviour
{
    public static ViewEventHandler Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public Action<ViewTarget> ViewTargetInHandClicked;
    public void FireTargetInHandClicked(ViewTarget target) { if (ViewTargetInHandClicked != null) ViewTargetInHandClicked(target); }

    public Action ManaChanged;
    public void FireManaChanged() { if (ManaChanged != null) ManaChanged(); }

    public Action<ITarget> ITargetClicked;
    public void FireITargetClicked(ITarget target) { if (ITargetClicked != null) ITargetClicked(target); }

    public Action ClickedAway;
    public void FireClickedAway() { if (ClickedAway != null) ClickedAway(); }
}