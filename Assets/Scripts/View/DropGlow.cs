using UnityEngine;

/// <summary>
/// Shared timing for board drop-target glows (lane edges, the Discard bowl): fade in and out, pulse between
/// two strengths while active, and hold a steady, brighter strength while hovered. Each time the glow switches on,
/// the pulse restarts halfway between min and max and rising, so it never opens at a random point.
/// </summary>
public class DropGlow
{
    bool active, hovered;
    float level, hoverLevel, pulseStartTime;

    public bool IsVisible => level > 0.001f;

    /// <summary>active = it's a valid drop target right now; hovered = the cursor is over it.</summary>
    public void Set(bool isActive, bool isHovered)
    {
        if (isActive && !active)
            pulseStartTime = Time.unscaledTime;
        active = isActive;
        hovered = isActive && isHovered;
    }

    /// <summary>Advances the fades and returns the glow strength for this frame (0 when off).</summary>
    public float Update(float pulseSpeed, float pulseMin, float pulseMax, float hoverStrength, float fadeSpeed)
    {
        float step = Application.isPlaying ? Time.unscaledDeltaTime * fadeSpeed : 1f;
        level = Mathf.MoveTowards(level, active ? 1f : 0f, step);
        hoverLevel = Mathf.MoveTowards(hoverLevel, hovered ? 1f : 0f, step);

        // sin starts at 0 and rising, so the pulse opens halfway between min and max, getting brighter.
        float wave = 0.5f + 0.5f * Mathf.Sin((Time.unscaledTime - pulseStartTime) * pulseSpeed * Mathf.PI * 2f);
        return Mathf.Lerp(Mathf.Lerp(pulseMin, pulseMax, wave), hoverStrength, hoverLevel) * level;
    }
}
