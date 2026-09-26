using System;
using System.Collections.Generic;

/// <summary>
/// Plays all children at once and completes once every child has completed.
/// Children use their own Anchor/Offset/QueueHold; the composite's are ignored.
/// </summary>
public class ParallelVfx : VfxEffect
{
    public List<VfxEffect> Children = new List<VfxEffect>();

    public ParallelVfx(params VfxEffect[] children)
    {
        Children.AddRange(children);
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        List<VfxEffect> children = Children.FindAll(child => child != null);
        int remaining = children.Count;
        if (remaining == 0)
        {
            onComplete?.Invoke();
            return;
        }

        foreach (VfxEffect child in children)
        {
            child.Play(context, () =>
            {
                remaining--;
                if (remaining == 0) onComplete?.Invoke();
            });
        }
    }
}

/// <summary>
/// Plays children one after another; each starts when the previous one completes (after its QueueHold).
/// Children use their own Anchor/Offset/QueueHold; the composite's are ignored.
/// </summary>
public class SequenceVfx : VfxEffect
{
    public List<VfxEffect> Children = new List<VfxEffect>();

    public SequenceVfx(params VfxEffect[] children)
    {
        Children.AddRange(children);
    }

    public override void Play(VfxContext context, Action onComplete)
    {
        PlayFrom(0, context, onComplete);
    }

    private void PlayFrom(int index, VfxContext context, Action onComplete)
    {
        while (index < Children.Count && Children[index] == null) index++;

        if (index >= Children.Count)
        {
            onComplete?.Invoke();
            return;
        }

        Children[index].Play(context, () => PlayFrom(index + 1, context, onComplete));
    }
}
