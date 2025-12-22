using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EaseType
{
    Linear,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic,
    EaseInSine,
    EaseOutSine,
    EaseInOutSine,
    EaseInExpo,
    EaseOutExpo,
    EaseInOutExpo,
    EaseInCirc,
    EaseOutCirc,
    EaseInOutCirc,
    EaseInBack,
}

public class Tween : SequenceItem
{
    //public Action OnFinish = null;

    Action<float> action;
    public float TimeRemaining = 0f;
    public float TotalDuration = 0f;
    float startValue = 0f;
    float endValue = 0f;
    EaseType easeType = EaseType.Linear;
    //float changePerSecond = 0f;

    public Tween(Action<float> action, float startValue, float endValue, float time, EaseType easeType = EaseType.Linear)
    {
        this.action = action;
        TotalDuration = time;
        TimeRemaining = time;
        this.startValue = startValue;
        this.endValue = endValue;
        this.easeType = easeType;
        //if (time != 0) changePerSecond = (endValue - startValue) / time;
    }

    float ApplyEasing(float t)
    {
        t = Mathf.Clamp01(t);
        
        switch (easeType)
        {
            case EaseType.Linear:
                return t;
                
            case EaseType.EaseInQuad:
                return t * t;
                
            case EaseType.EaseOutQuad:
                return 1f - (1f - t) * (1f - t);
                
            case EaseType.EaseInOutQuad:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                
            case EaseType.EaseInCubic:
                return t * t * t;
                
            case EaseType.EaseOutCubic:
                return 1f - Mathf.Pow(1f - t, 3f);
                
            case EaseType.EaseInOutCubic:
                return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                
            case EaseType.EaseInSine:
                return 1f - Mathf.Cos(t * Mathf.PI / 2f);
                
            case EaseType.EaseOutSine:
                return Mathf.Sin(t * Mathf.PI / 2f);
                
            case EaseType.EaseInOutSine:
                return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                
            case EaseType.EaseInExpo:
                return t == 0f ? 0f : Mathf.Pow(2f, 10f * (t - 1f));
                
            case EaseType.EaseOutExpo:
                return t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                
            case EaseType.EaseInOutExpo:
                if (t == 0f) return 0f;
                if (t == 1f) return 1f;
                return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
                
            case EaseType.EaseInCirc:
                return 1f - Mathf.Sqrt(1f - t * t);
                
            case EaseType.EaseOutCirc:
                return Mathf.Sqrt(1f - (1f - t) * (1f - t));
                
            case EaseType.EaseInOutCirc:
                return t < 0.5f 
                    ? (1f - Mathf.Sqrt(1f - 4f * t * t)) / 2f 
                    : (Mathf.Sqrt(1f - 4f * (1f - t) * (1f - t)) + 1f) / 2f;
            case EaseType.EaseInBack:
                float constant1 = 1.70158f;
                float constant2 = constant1 + 1;

                return constant2 * t * t * t - constant1 * t * t;
            default:
                return t;
        }
    }

    // Return true when complete
    public bool Progress()
    {
        if (TimeRemaining <= 0 || action == null) return true;

        TimeRemaining -= Time.deltaTime;
        TimeRemaining = Mathf.Max(0, TimeRemaining);

        float rawProgress = (TotalDuration - TimeRemaining) / TotalDuration;
        float easedProgress = ApplyEasing(rawProgress);
        float totalProgress = startValue + easedProgress * (endValue - startValue);

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
