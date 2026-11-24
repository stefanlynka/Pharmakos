using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// While the human has a ritual selected, pulses the skull on every follower that ritual would destroy
/// (Ritual.GetFollowersKilledBy), fading between full and half opacity. Ticked from View.PlayerUpdate.
/// Leaves each skull at alpha 0 when the preview ends, the resting state FollowerDeathAnimation starts from.
/// </summary>
public class DeathPreview
{
    const float PulsePeriod = 2.4f;
    const float MaxAlpha = 1f;
    const float MinAlpha = 0.5f;
    // Longest step one frame may advance the pulse, so a hitch on selecting the ritual can't skip the fade-in.
    const float MaxStep = 1f / 30f;

    readonly HashSet<ViewFollower> shown = new HashSet<ViewFollower>();
    readonly HashSet<ViewFollower> current = new HashSet<ViewFollower>();
    ViewRitual shownRitual;
    float elapsed;

    public void Tick()
    {
        current.Clear();

        ViewRitual ritual = GetSelectedRitual(out ITarget target);
        if (ritual != null)
        {
            if (ritual != shownRitual)
            {
                shownRitual = ritual;
                elapsed = 0f;
            }
            else
            {
                elapsed += Mathf.Min(Time.deltaTime, MaxStep);
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

        float alpha = GetAlpha(elapsed);
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

    // Eases in from 0 to full over half a pulse, then breathes between full and half opacity.
    // The fade-in is a half-cosine like the pulse's own rise and fall, so the first beat keeps the same rhythm.
    static float GetAlpha(float elapsed)
    {
        float half = PulsePeriod * 0.5f;
        if (elapsed < half)
            return MaxAlpha * (0.5f - 0.5f * Mathf.Cos(elapsed / half * Mathf.PI));

        float pulse = 0.5f + 0.5f * Mathf.Cos((elapsed - half) * 2f * Mathf.PI / PulsePeriod);
        return Mathf.Lerp(MinAlpha, MaxAlpha, pulse);
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
