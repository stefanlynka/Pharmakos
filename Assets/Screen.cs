using System;
using UnityEngine;

public class Screen : MonoBehaviour
{
    public ScreenName Name;
    public CanvasGroup UIHolder;

    public Camera Camera;

    protected bool neverHide = false;
    public bool ManualHide = false;

    public Action OnEnter;
    public Action OnExit;

    private bool active = false;
    private float screenChangeSpeed = 1.0f;

    private void Update()
    {
        if (active && UIHolder.alpha < 1) UIHolder.alpha += (Time.deltaTime * screenChangeSpeed);
        else if (!active && UIHolder.alpha > 0) UIHolder.alpha -= (Time.deltaTime * screenChangeSpeed);
    }

    public virtual void Enter(bool instant = false)
    {
        SetUIActive(true, instant);

        if (Camera != null) Camera.enabled = true;

        OnEnter?.Invoke();
        OnEnter = null;
    }
    public virtual void Exit(bool instant = false)
    {
        if (neverHide) return;

        OnExit?.Invoke();
        OnExit = null;

        SetUIActive(false, instant);
        if (Camera != null) Camera.enabled = false;
    }
    public virtual void Init()
    {

    }

    private void SetUIActive(bool active, bool instant = false)
    {
        this.active = active;
        UIHolder.interactable = active;
        UIHolder.blocksRaycasts = active;

        if (instant) UIHolder.alpha = active ? 1.0f : 0.0f;
    }
}
