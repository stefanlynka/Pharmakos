using LineworkLite.FreeOutline;
using UnityEngine;

/// <summary>
/// Drives the shared Linework ritual outline color. Multiple <see cref="ViewRitual"/> instances
/// can pulse in sync via the same sine phase.
/// </summary>
public static class RitualOutlineHighlight
{
    const uint RitualRenderingLayer = 1u << 3;

    static Outline ritualOutline;
    static Color defaultOutlineColor;
    static bool defaultColorCaptured;
    static int activeCount;

    public static void SetActive(bool active)
    {
        if (active)
        {
            activeCount++;
            return;
        }

        activeCount = Mathf.Max(0, activeCount - 1);
        if (activeCount == 0)
            RestoreDefaultColor();
    }

    public static void ApplyPulse(Color colorA, Color colorB, float pulseT)
    {
        if (activeCount <= 0)
            return;

        Outline outline = GetRitualOutline();
        if (outline == null)
            return;

        outline.color = Color.Lerp(colorA, colorB, pulseT);
    }

    static void RestoreDefaultColor()
    {
        Outline outline = GetRitualOutline();
        if (outline == null || !defaultColorCaptured)
            return;

        outline.color = defaultOutlineColor;
    }

    static Outline GetRitualOutline()
    {
        if (ritualOutline != null)
            return ritualOutline;

        foreach (FreeOutlineSettings settings in Resources.FindObjectsOfTypeAll<FreeOutlineSettings>())
        {
            if (settings == null)
                continue;

            foreach (Outline outline in settings.Outlines)
            {
                if (outline == null || !outline.IsActive())
                    continue;

                if (outline.RenderingLayer != RitualRenderingLayer)
                    continue;

                ritualOutline = outline;
                defaultOutlineColor = outline.color;
                defaultColorCaptured = true;
                return ritualOutline;
            }
        }

        return null;
    }
}
