using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable] // Good practice for deep copying, though direct copying of state array is better
public class CustomRandom
{
    private long currentSeed;
    public long CurrentSeed { get { return currentSeed; } }
    public CustomRandom(int seed)
    {
        currentSeed = seed;
    }

    // Constructor to copy
    public CustomRandom(CustomRandom originalRandom)
    {
        currentSeed = originalRandom.CurrentSeed;
    }

    public void SetSeed(long newSeed)
    {
        currentSeed = newSeed;
    }

    /// <summary>
    /// Returns a random integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>.
    /// <paramref name="maxValue"/> is exclusive, matching System.Random.Next behavior.
    /// </summary>
    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException("maxValue", "maxValue must be greater than or equal to minValue");
        long range = (long)maxValue - minValue;
        if (range < 0) // Handle overflow for very large ranges
        {
            return (int)((this.NextLong() % range) + minValue);
        }
        else if (range == 0)
        {
            return 0;
        }
        else
        {
            return (int)((this.NextLong() % range) + minValue);
        }
    }

    public double NextDouble()
    {
        return (double)NextLong() / long.MaxValue; // Normalize to 0-1
    }

    private long NextLong()
    {
        // Simple LCG: X_n+1 = (a * X_n + c) mod m
        // Standard constants (from Numerical Recipes) for reproducibility
        currentSeed = (currentSeed * 1103515245 + 12345) & 0x7FFFFFFF; // 31-bit LCG
        return currentSeed;
    }
}
