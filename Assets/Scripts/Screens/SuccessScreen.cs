using TMPro;
using UnityEngine;

/// <summary>
/// Victory screen. The call-to-action copy stays hidden for a moment after the screen appears, then fades in.
/// </summary>
public class SuccessScreen : Screen
{
    const string CallToActionObjectName = "CallToActionText";
    const float CallToActionDelay = 2f;
    const float CallToActionFadeDuration = 0.6f;

    TextMeshProUGUI _callToAction;
    float _alpha;
    float _targetAlpha;
    float _delay;

    public override void Init()
    {
        CacheCallToAction();
        SetAlpha(0f);
    }

    public override void Enter(bool instant = false)
    {
        BeginCallToActionFade();
        base.Enter(instant);
    }

    public override void Exit(bool instant = false)
    {
        base.Exit(instant);
        _delay = 0f;
        _targetAlpha = _alpha;
        if (instant)
            SetAlpha(0f);
    }

    protected override void Update()
    {
        base.Update();

        if (_callToAction == null)
            return;

        if (_delay > 0f)
        {
            _delay -= Time.deltaTime;
            return;
        }

        if (Mathf.Approximately(_alpha, _targetAlpha))
            return;

        float duration = Mathf.Max(0.01f, CallToActionFadeDuration);
        _alpha = Mathf.MoveTowards(_alpha, _targetAlpha, Time.deltaTime / duration);
        _callToAction.alpha = _alpha;
    }

    void BeginCallToActionFade()
    {
        CacheCallToAction();
        _delay = CallToActionDelay;
        _targetAlpha = 1f;
        SetAlpha(0f);
    }

    void CacheCallToAction()
    {
        if (_callToAction != null)
            return;

        Transform child = transform.Find(CallToActionObjectName);
        if (child != null)
            _callToAction = child.GetComponent<TextMeshProUGUI>();

        if (_callToAction == null)
            Debug.LogWarning("SuccessScreen: CallToActionText was not found.");
    }

    void SetAlpha(float alpha)
    {
        _alpha = alpha;
        if (_callToAction != null)
            _callToAction.alpha = _alpha;
    }
}
