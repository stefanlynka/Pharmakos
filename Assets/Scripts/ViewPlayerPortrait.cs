using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewPlayerPortrait : MonoBehaviour
{
    public MeshRenderer Highlight;
    public GameObject HealthObject;
    public TextMeshPro HealthText;
    public GameObject DamageIcon;
    public TextMeshPro DamageText;
    [SerializeField] DamageIconStroke damageStroke = new DamageIconStroke();
    public SpriteRenderer PortraitRenderer;

    [Header("Colour flash")]
    [SerializeField] SpriteRenderer colorOverlay;
    private int health = 0;
    public GameObject HeartStringHolder;
    public List<GameObject> HeartStrings = new List<GameObject>();

    private Player player;

    public void Load(Player player)
    {
        this.player = player;

        SetHealthVisible(true);

        PortraitRenderer.sprite = Resources.Load<Sprite>("Images/Portraits/" + player.PlayerDetails.PortraitName);
        SyncColorOverlay();

        if (player.CurrentHeartStrings > 0)
        {
            SetHeartStrings(player.CurrentHeartStrings, Player.MaxHeartStrings);
        }
        else if (HeartStringHolder != null)
        {
            HeartStringHolder.SetActive(false);
        }
    }

    public void SetHeartStrings(int current, int max)
    {
        if (HeartStringHolder != null)
        {
            HeartStringHolder.SetActive(current > 0);
        }

        for (int i = 0; i < HeartStrings.Count; i++)
        {
            if (HeartStrings[i] != null)
            {
                HeartStrings[i].SetActive(i < current);
            }
        }
    }

    public void ChangeHealth(int change)
    {
        health += change;
        HealthText.text = health.ToString();
    }
    public void SetHealth(int newHealth)
    {
        health = newHealth;
        HealthText.text = health.ToString();
    }
    public int GetHealth()
    {
        return health;
    }
    public void SetHealthVisible(bool value)
    {
        HealthObject.SetActive(value);
    }

    public void ShowDamage(int damage)
    {
        damageStroke.Show(this, DamageIcon, DamageText, damage);
    }
    public void HideDamage()
    {
        damageStroke.Hide(this, DamageIcon, DamageText);
    }

    // Same wash as a follower's stat change: fades in over the first half, then out over the second.
    // color.a is the peak strength. A newer flash replaces the colour; each call still finishes on its own.
    public void PlayColorFlash(Color color, float duration, Action onComplete = null)
    {
        if (colorOverlay == null)
        {
            if (!missingOverlayWarned)
            {
                missingOverlayWarned = true;
                Debug.LogWarning("ViewPlayerPortrait: Colour Overlay is not assigned, so the colour flash cannot play.");
            }

            onComplete?.Invoke();
            return;
        }

        duration = Mathf.Max(duration, 0.05f);
        int version = ++flashVersion;
        SyncColorOverlay();
        colorOverlay.enabled = true;
        SetOverlayColor(color, 0f);

        float fade = duration * 0.5f;
        Sequence sequence = new Sequence();
        sequence.Add(new Tween(elapsed => StepFlash(version, color, elapsed, duration, fade), 0f, duration, duration));
        sequence.Add(new SequenceAction(() => FinishFlash(version, color, onComplete)));
        sequence.Start();
    }

    int flashVersion;
    static bool missingOverlayWarned;

    void SyncColorOverlay()
    {
        if (colorOverlay == null || PortraitRenderer == null)
            return;

        colorOverlay.sprite = PortraitRenderer.sprite;
        colorOverlay.flipX = PortraitRenderer.flipX;
        colorOverlay.flipY = PortraitRenderer.flipY;
        colorOverlay.sortingLayerID = PortraitRenderer.sortingLayerID;
        colorOverlay.sortingOrder = PortraitRenderer.sortingOrder + 1;
    }

    void StepFlash(int version, Color color, float elapsed, float duration, float fade)
    {
        if (version != flashVersion || colorOverlay == null)
            return;

        SetOverlayColor(color, EvaluateFlashAlpha(elapsed, duration, fade, fade));
    }

    void FinishFlash(int version, Color color, Action onComplete)
    {
        if (version == flashVersion && colorOverlay != null)
        {
            SetOverlayColor(color, 0f);
            colorOverlay.enabled = false;
        }

        onComplete?.Invoke();
    }

    static void SetOverlayColor(SpriteRenderer overlay, Color color, float alpha)
    {
        overlay.color = new Color(color.r, color.g, color.b, color.a * alpha);
    }

    void SetOverlayColor(Color color, float alpha)
    {
        SetOverlayColor(colorOverlay, color, alpha);
    }

    static float EvaluateFlashAlpha(float elapsed, float duration, float fadeInDuration, float fadeOutDuration)
    {
        float fadeIn = fadeInDuration > 0f
            ? EaseOutQuad(Mathf.Clamp01(elapsed / fadeInDuration))
            : 1f;

        float fadeOutStart = duration - fadeOutDuration;
        if (elapsed <= fadeOutStart)
            return fadeIn;

        float fadeOut = 1f - EaseOutQuad(Mathf.Clamp01((elapsed - fadeOutStart) / fadeOutDuration));
        return fadeIn * fadeOut;
    }

    static float EaseOutQuad(float t)
    {
        float remaining = 1f - Mathf.Clamp01(t);
        return 1f - remaining * remaining;
    }
}
