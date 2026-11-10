using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RitualHolder : MonoBehaviour
{
    public ViewRitual ViewRitual;
    public SpriteRenderer RewardSpriteRenderer;

    [Header("Outline Glow")]
    [SerializeField] Color idleOutlineColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] Color glowOutlineColor = new Color(1f, 0.85f, 0.55f, 1f);
    [SerializeField] float glowFadeDuration = 0.15f;
    [Tooltip("Full pulse cycles per second while glowing.")]
    [SerializeField] float glowPulseSpeed = 1.2f;
    [Tooltip("How far the glow alpha dips during a pulse. 0 = steady.")]
    [SerializeField, Range(0f, 1f)] float glowPulseDepth = 0.3f;

    [Header("Slide")]
    [SerializeField] float slideDuration = 0.25f;

    Vector3 restLocalPosition;
    Quaternion restLocalRotation = Quaternion.identity;

    bool isGlowing;
    float glowAmount;

    bool isSliding;
    float slideElapsed;
    Vector3 slideFrom;

    public Vector3 RestPosition => transform.TransformPoint(restLocalPosition);

    void Awake()
    {
        if (ViewRitual != null)
        {
            restLocalPosition = ViewRitual.transform.localPosition;
            restLocalRotation = ViewRitual.transform.localRotation;
        }

        ApplyGlow();
    }

    void Update()
    {
        UpdateSlide();
        UpdateGlow();
    }

    public void Load(Ritual ritual, Action<ViewTarget> onPressed)
    {
        ViewRitual.Init(ritual);
        ViewRitual.OnClick = onPressed;
        SnapToRest();
    }

    /// <summary>Takes ownership of a ritual and slides it from wherever it is into this holder.</summary>
    public void Receive(ViewRitual viewRitual)
    {
        ViewRitual = viewRitual;
        if (ViewRitual == null) return;

        ViewRitual.transform.SetParent(transform, true);
        ViewRitual.transform.localRotation = restLocalRotation;
        SlideToRest();
    }

    public void SlideToRest()
    {
        if (ViewRitual == null) return;

        slideFrom = ViewRitual.transform.localPosition;
        slideElapsed = 0f;
        isSliding = true;
    }

    public void SnapToRest()
    {
        isSliding = false;
        if (ViewRitual == null) return;

        ViewRitual.transform.localPosition = restLocalPosition;
        ViewRitual.transform.localRotation = restLocalRotation;
    }

    public void StopSliding()
    {
        isSliding = false;
    }

    public void SetGlow(bool active)
    {
        isGlowing = active;
    }

    /// <summary>True when the point sits inside the outline's footprint. Height above the outline is ignored.</summary>
    public bool IsOverOutline(Vector3 worldPoint)
    {
        if (RewardSpriteRenderer == null || RewardSpriteRenderer.sprite == null) return false;

        Vector3 local = RewardSpriteRenderer.transform.InverseTransformPoint(worldPoint);
        Bounds bounds = RewardSpriteRenderer.sprite.bounds;
        return Mathf.Abs(local.x - bounds.center.x) <= bounds.extents.x
            && Mathf.Abs(local.y - bounds.center.y) <= bounds.extents.y;
    }

    void UpdateSlide()
    {
        if (!isSliding) return;
        if (ViewRitual == null)
        {
            isSliding = false;
            return;
        }

        slideElapsed += Time.deltaTime;
        float t = slideDuration <= 0f ? 1f : Mathf.Clamp01(slideElapsed / slideDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        ViewRitual.transform.localPosition = Vector3.LerpUnclamped(slideFrom, restLocalPosition, eased);

        if (t >= 1f)
            isSliding = false;
    }

    void UpdateGlow()
    {
        float step = glowFadeDuration <= 0f ? 1f : Time.deltaTime / glowFadeDuration;
        glowAmount = Mathf.MoveTowards(glowAmount, isGlowing ? 1f : 0f, step);
        ApplyGlow();
    }

    void ApplyGlow()
    {
        if (RewardSpriteRenderer == null) return;

        float wave = 0.5f - 0.5f * Mathf.Cos(Time.time * glowPulseSpeed * Mathf.PI * 2f);
        Color glow = glowOutlineColor;
        glow.a *= 1f - glowPulseDepth * wave;

        RewardSpriteRenderer.color = Color.Lerp(idleOutlineColor, glow, glowAmount);
    }
}
