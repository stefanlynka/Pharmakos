using System.Collections;
using TMPro;
using UnityEngine;

public class OfferingLabel : MonoBehaviour
{
    public TextMeshPro OfferingName;
    public TextMeshPro OfferingAmount;
    public SpriteRenderer Icon;
    [SerializeField]
    public Light Spotlight;

    [Tooltip("Which side of this offering the hover summary popup appears on.")]
    public PopupPosition SummaryPosition = PopupPosition.Above;

    [Header("Collect Pulse")]
    public float CollectPeakIntensity = 100f;
    public float CollectPeakScale = 1.2f;
    public float CollectAttackDuration = 0.1f;
    public float CollectDecayDuration = 2f;

    string summaryDescription = string.Empty;
    float baseSpotlightIntensity;
    Vector3 baseScale;
    Coroutine collectPulseRoutine;

    public void SetSummaryText(string text)
    {
        summaryDescription = text ?? string.Empty;
    }

    void Awake()
    {
        baseScale = transform.localScale;
        if (Spotlight != null)
            baseSpotlightIntensity = Spotlight.intensity;
    }

    public void PlayCollectPulse()
    {
        if (!isActiveAndEnabled)
        {
            SetCollectPulse(1f);
            return;
        }

        if (collectPulseRoutine != null)
            StopCoroutine(collectPulseRoutine);

        collectPulseRoutine = StartCoroutine(CollectPulseRoutine());
    }

    IEnumerator CollectPulseRoutine()
    {
        float t = GetCollectPulseT();
        float attackTime = Mathf.Max(0.01f, CollectAttackDuration * (1f - t));

        float elapsed = 0f;
        float startT = t;
        while (elapsed < attackTime)
        {
            elapsed += Time.deltaTime;
            float raw = Mathf.Clamp01(elapsed / attackTime);
            float eased = 1f - (1f - raw) * (1f - raw);
            SetCollectPulse(Mathf.Lerp(startT, 1f, eased));
            yield return null;
        }

        SetCollectPulse(1f);

        elapsed = 0f;
        while (elapsed < CollectDecayDuration)
        {
            elapsed += Time.deltaTime;
            float decayT = 1f - Mathf.Clamp01(elapsed / CollectDecayDuration);
            decayT = decayT * decayT * decayT;
            SetCollectPulse(decayT);
            yield return null;
        }

        SetCollectPulse(0f);
        collectPulseRoutine = null;
    }

    float GetCollectPulseT()
    {
        if (Spotlight != null && !Mathf.Approximately(CollectPeakIntensity, baseSpotlightIntensity))
            return Mathf.InverseLerp(baseSpotlightIntensity, CollectPeakIntensity, Spotlight.intensity);

        if (Mathf.Approximately(CollectPeakScale, 1f) || baseScale.x <= 0.0001f)
            return 0f;

        return Mathf.InverseLerp(1f, CollectPeakScale, transform.localScale.x / baseScale.x);
    }

    void SetCollectPulse(float t)
    {
        t = Mathf.Clamp01(t);
        if (Spotlight != null)
            Spotlight.intensity = Mathf.Lerp(baseSpotlightIntensity, CollectPeakIntensity, t);

        float scaleMul = Mathf.Lerp(1f, CollectPeakScale, t);
        transform.localScale = new Vector3(baseScale.x * scaleMul, baseScale.y * scaleMul, baseScale.z);
    }

    void OnMouseEnter()
    {
        if (ContentScrollView.BlocksGameInput)
            return;

        ShowHoverSummary();
    }

    void OnMouseExit()
    {
        HideHoverSummary();
    }

    void OnDisable()
    {
        if (collectPulseRoutine != null)
        {
            StopCoroutine(collectPulseRoutine);
            collectPulseRoutine = null;
        }

        SetCollectPulse(0f);
        HideHoverSummary();
    }

    void ShowHoverSummary()
    {
        if (ContentScrollView.BlocksGameInput)
            return;

        if (string.IsNullOrEmpty(summaryDescription))
            return;

        PopupScreenHandler.Instance?.ShowTextPopup(summaryDescription, transform, SummaryPosition);
    }

    void HideHoverSummary()
    {
        PopupScreenHandler.Instance?.HideTextPopup(transform);
    }
}
