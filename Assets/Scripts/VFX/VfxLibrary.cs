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
