using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// While the human has a ritual selected, pulses the skull on every follower that ritual would destroy
/// (Ritual.GetFollowersKilledBy), fading between full and half opacity. Ticked from View.PlayerUpdate.
/// Leaves each skull at alpha 0 when the preview ends, the resting state FollowerDeathAnimation starts from.
/// </summary>
public class DeathPreview
{
    const float FadeInDuration = 0.15f;
    const float PulsePeriod = 1.2f;
    const float MaxAlpha = 1f;
    const float MinAlpha = 0.5f;

    readonly HashSet<ViewFollower> shown = new HashSet<ViewFollower>();
    readonly HashSet<ViewFollower> current = new HashSet<ViewFollower>();
    ViewRitual shownRitual;
    float startTime;

    public void Tick()
    {
        current.Clear();

        ViewRitual ritual = GetSelectedRitual(out ITarget target);
        if (ritual != null)
        {
            if (ritual != shownRitual)
            {
                shownRitual = ritual;
                startTime = Time.time;
            }

            List<Follower> doomed = ritual.Ritual.GetFollowersKilledBy(target);
            if (doomed != null)
            {
                foreach (Follower follower in doomed)
                {
                    if (View.Instance.TryGetViewFollower(follower, out ViewFollower viewFollower)
                        && viewFollower != null && viewFollower.SkullRenderer != null)
                        current.Add(viewFollower);
                }
            }
        }
        else
        {
            shownRitual = null;
        }

        foreach (ViewFollower viewFollower in shown)
        {
            if (viewFollower != null && !current.Contains(viewFollower))
                SetSkullAlpha(viewFollower, 0f);
        }

        shown.Clear();
        if (current.Count == 0)
            return;

        float alpha = GetAlpha(Time.time - startTime);
        foreach (ViewFollower viewFollower in current)
        {
            SetSkullAlpha(viewFollower, alpha);
            shown.Add(viewFollower);
        }
    }

    public void Clear()
    {
        foreach (ViewFollower viewFollower in shown)
        {
            if (viewFollower != null)
                SetSkullAlpha(viewFollower, 0f);
        }
        shown.Clear();
        shownRitual = null;
    }

    static ViewRitual GetSelectedRitual(out ITarget target)
    {
        target = null;
        if (View.Instance == null || !View.Instance.IsInteractible)
            return null;

        SelectionHandler selection = View.Instance.SelectionHandler;
        ViewRitual ritual = selection?.SelectedRitual;
        if (ritual == null || ritual.Ritual == null || selection.PotentialRitualTargets.Count == 0)
            return null;

        // Preview against the hovered legal target, else the first one (Poseidon Minor's only target is its owner).
        ITarget hovered = selection.CurrentHover != null ? selection.CurrentHover.Target : null;
        target = hovered != null && selection.PotentialRitualTargets.Contains(hovered)
            ? hovered
            : selection.PotentialRitualTargets[0];
        return ritual;
    }

    // Fades in to full, then breathes between full and half opacity.
    static float GetAlpha(float elapsed)
    {
        float fadeIn = Mathf.Clamp01(elapsed / FadeInDuration);
        float pulse = 0.5f + 0.5f * Mathf.Cos(elapsed * 2f * Mathf.PI / PulsePeriod);
        return fadeIn * Mathf.Lerp(MinAlpha, MaxAlpha, pulse);
    }

    static void SetSkullAlpha(ViewFollower viewFollower, float alpha)
    {
        if (viewFollower.SkullRenderer == null)
            return;

        Color color = viewFollower.SkullRenderer.color;
        color.a = alpha;
        viewFollower.SkullRenderer.color = color;
    }
}
