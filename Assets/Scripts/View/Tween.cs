using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tween : SequenceItem
{
    //public Action OnFinish = null;

    Action<float> action;
    public float TimeRemaining = 0f;
    public float TotalDuration = 0f;
    float startValue = 0f;
    float endValue = 0f;
    //float changePerSecond = 0f;

    public Tween(Action<float> action, float startValue, float endValue, float time)
    {
        this.action = action;
        TotalDuration = time;
        TimeRemaining = time;
        this.startValue = startValue;
        this.endValue = endValue;
        //if (time != 0) changePerSecond = (endValue - startValue) / time;
    }

    // Return true when complete
    public bool Progress()
    {
        if (TimeRemaining <= 0 || action == null) return true;

        TimeRemaining -= Time.deltaTime;
        TimeRemaining = Mathf.Max(0, TimeRemaining);

        float totalProgress = 1f;
        totalProgress = startValue + (((TotalDuration - TimeRemaining) / TotalDuration) * (endValue - startValue));

        action(totalProgress);

        return false;
    }

    //public void Start()
    //{
    //    TweenManager.instance.StartTween(this);
    //}
    //public void Clear()
    //{
    //    TweenManager.instance.RemoveTween(this);
    //    OnFinish = null;
    //}
}
