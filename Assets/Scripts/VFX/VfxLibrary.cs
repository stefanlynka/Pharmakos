using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Named, reusable effect presets. Every call returns a fresh instance so callers can tweak fields freely.
/// </summary>
public static class VfxLibrary
{
    private static readonly OfferingType[] RitualTokenOrder =
    {
        OfferingType.Blood,
        OfferingType.Bone,
        OfferingType.Crop,
        OfferingType.Scroll,
        OfferingType.Gold,
    };

    public static VfxEffect DefaultRitual(Ritual ritual)
    {
        return new ParallelVfx(RitualOfferingBurst(ritual), new RitualLightingVfx());
    }

    // Always spawns totalTokens; the ritual's base costs only decide how they're split between offering types.
    // Every offering type the ritual costs gets at least one token.
    public static OfferingBurstVfx RitualOfferingBurst(Ritual ritual, int totalTokens = 16)
    {
        OfferingBurstVfx burst = new OfferingBurstVfx();

        List<OfferingType> types = new List<OfferingType>();
        List<int> costs = new List<int>();
        if (ritual != null && ritual.Costs != null)
        {
            foreach (OfferingType type in RitualTokenOrder)
            {
                if (!ritual.Costs.TryGetValue(type, out int cost) || cost <= 0) continue;
                types.Add(type);
                costs.Add(cost);
            }
        }

        if (types.Count == 0)
        {
            types.Add(OfferingType.Gold);
            costs.Add(1);
        }

        int[] counts = DistributeProportionally(costs, Mathf.Max(totalTokens, types.Count));
        for (int i = 0; i < types.Count; i++)
        {
            for (int j = 0; j < counts[i]; j++)
            {
                burst.Tokens.Add(types[i]);
            }
        }

        return burst;
    }

    // Largest-remainder split of total across weights, guaranteeing each weight at least one
    private static int[] DistributeProportionally(List<int> weights, int total)
    {
        int count = weights.Count;
        int[] result = new int[count];
        float[] remainders = new float[count];

        int weightSum = 0;
        foreach (int weight in weights) weightSum += weight;

        int assigned = 0;
        for (int i = 0; i < count; i++)
        {
            float share = (float)total * weights[i] / weightSum;
            result[i] = Mathf.FloorToInt(share);
            remainders[i] = share - result[i];
            assigned += result[i];
        }

        while (assigned < total)
        {
            int best = 0;
            for (int i = 1; i < count; i++)
            {
                if (remainders[i] > remainders[best]) best = i;
            }
            result[best]++;
            remainders[best] = -1f;
            assigned++;
        }

        for (int i = 0; i < count; i++)
        {
            if (result[i] > 0) continue;

            int largest = 0;
            for (int j = 1; j < count; j++)
            {
                if (result[j] > result[largest]) largest = j;
            }
            result[largest]--;
            result[i]++;
        }

        return result;
    }

    // Flame brushstrokes flurry over the anchor (e.g. a CardView) until it is completely covered, then die down
    public static BrushStrokeFlurryVfx FireBrushStrokes()
    {
        return new BrushStrokeFlurryVfx
        {
            SpriteFolder = "Images/Particles/Fire",
            StartSoundPath = "Audio/SFX/Rituals/FlameStart",
            Duration = 1f,       // how long new strokes keep being created
            PeakRate = 90f,      // strokes per second when the curve is at 1
            RateOverTime = new AnimationCurve(
                new Keyframe(0f,   0.6f),   // starts at 20% of peak (9 per second)
                new Keyframe(0.4f, 1f),     // full rate 0.8 seconds in
                new Keyframe(0.75f, 1f),    // holds full rate until 1.7 seconds
                new Keyframe(1f,   0f)),    // dies out by 2 seconds
            PaintInDuration = 0.1f,
            HoldDuration = 0.25f,
            WipeOutDuration = 0.1f,
            ScaleMin = 1.5f,
            ScaleMax = 2.5f,
            AngleJitter = 15f,
            RiseSpeed = 2f,
            WiggleAmplitude = 0.12f,
            WiggleFrequency = 1.5f,
            QueueHold = 2f,
        };
    }

    // Water brushstrokes flood the anchor's area from right to left, then drain away the same way. Strokes are short-lived
    // and keep being replaced; the flood moves because of where they're allowed to spawn. The whole area is covered from
    // about SweepDuration + PaintInDuration until SweepOutDelay.
    // Callers covering a large area should set AreaSize, CoverageCellSize, scale and PeakRate to match it.
    public static BrushStrokeFlurryVfx WaterFlood()
    {
        BrushStrokeFlurryVfx flood = new BrushStrokeFlurryVfx
        {
            SpriteFolder = "Images/Particles/Water",
            PeakRate = 280f,
            SweepDirection = Vector2.left,
            SweepDuration = 1.5f,
            SweepOutDelay = 2f,
            PaintInDuration = 0.15f,
            HoldDuration = 0.35f,
            WipeOutDuration = 0.2f,
            ScaleMin = 0.6f,
            ScaleMax = 1f,
            // The water sprites are vertical: turned a quarter, they lie horizontally and paint/wipe right to left
            StrokeAngle = 90f,
            AngleJitter = 8f,
            SortingOrder = 10000,
        };
        flood.Duration = flood.SweepOutDelay + flood.SweepDuration;
        flood.QueueHold = flood.Duration + flood.StrokeLifetime;
        return flood;
    }

    // The card's body fades to black over its art, text, and icons
    public static CharCardVfx CharCard()
    {
        return new CharCardVfx();
    }

    // Same wash as StatChangeTint, played on the target player's portrait.
    public static PortraitColorFlashVfx PortraitColorFlash(Color color, float duration = 0.8f)
    {
        duration = Mathf.Max(duration, 0.05f);
        return new PortraitColorFlashVfx
        {
            FlashColor = color,
            Duration = duration,
            QueueHold = duration,
        };
    }

    // Gold wash when a heartstring is spent to stop lethal damage.
    public static PortraitColorFlashVfx PortraitHeartstringFlash(float duration = 0.8f)
    {
        return PortraitColorFlash(new Color(1f, 0.78f, 0.2f, 0.3f), duration);
    }

    // Red, white, green, or blue from the ritual's largest Blood, Bone, Crop, or Scroll cost.
    // Ties keep the earlier of those four. No positive cost falls back to white.
    public static PortraitColorFlashVfx PortraitOfferingFlash(Ritual ritual, float duration = 0.8f)
    {
        return PortraitColorFlash(OfferingFlashColor(DominantOffering(ritual)), duration);
    }

    public static OfferingType DominantOffering(Ritual ritual)
    {
        OfferingType best = OfferingType.Bone;
        int bestCost = 0;
        if (ritual == null || ritual.Costs == null)
            return best;

        foreach (OfferingType type in RitualTokenOrder)
        {
            if (type == OfferingType.Gold)
                continue;
            if (!ritual.Costs.TryGetValue(type, out int cost) || cost <= bestCost)
                continue;

            bestCost = cost;
            best = type;
        }

        return best;
    }

    public static Color OfferingFlashColor(OfferingType type)
    {
        const float alpha = 0.3f;
        switch (type)
        {
            case OfferingType.Blood:
                return new Color(0.95f, 0.2f, 0.15f, alpha);
            case OfferingType.Crop:
                return new Color(0.25f, 0.95f, 0.3f, alpha);
            case OfferingType.Scroll:
                return new Color(0.35f, 0.55f, 1f, alpha);
            default:
                return new Color(0.95f, 0.95f, 0.92f, alpha);
        }
    }

    // A green or red wash over the card. It fades in quickly, holds, and is gone when duration elapses.
    public static CharCardVfx StatChangeTint(bool buff, float duration)
    {
        duration = Mathf.Max(duration, 0.05f);
        return new CharCardVfx
        {
            CharColor = buff
                ? new Color(0.25f, 0.95f, 0.3f, 0.3f)
                : new Color(0.95f, 0.2f, 0.15f, 0.3f),
            Delay = 0f,
            FadeDuration = duration * 0.5f,
            FadeOutDuration = duration * 0.5f,
            Lifetime = duration,
            QueueHold = duration,
        };
    }

    // The card burns: a fire brushstroke flurry over it while it chars black underneath
    public static ParallelVfx FireSacrifice()
    {
        return new ParallelVfx(FireBrushStrokes(), CharCard());
    }

    public static ParticleBurstVfx LightningSparks()
    {
        return new ParticleBurstVfx
        {
            Count = 40,
            SpeedMin = 6f,
            SpeedMax = 12f,
            LifetimeMin = 0.2f,
            LifetimeMax = 0.5f,
            SizeMin = 0.08f,
            SizeMax = 0.2f,
            StartColor = new Color(0.8f, 0.9f, 1f),
            EndColor = new Color(0.4f, 0.6f, 1f, 0f),
            Drag = 4f,
            QueueHold = 0.2f,
        };
    }
}
