using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable] // Good practice for deep copying, though direct copying of state array is better
public class CustomRandom
{
    // The internal state of System.Random is typically 56 integers,
    // plus an index. We'll simulate that.
    private int[] _seedArray;
    private int _seedArrayIndex;

    // Constructor to create from a standard seed
    public CustomRandom(int seed)
    {
        _seedArray = new int[56]; // Size based on System.Random's known internal implementation
                                  // Initialize based on the seed (this is the System.Random internal algorithm for initial seeding)
                                  // This part is the "hacky" bit where you mimic System.Random's initialization logic.
                                  // It's non-trivial to perfectly replicate System.Random's internal seed initialization
                                  // without looking at its source code.
                                  // A simpler way: just create a System.Random, generate a few numbers, and store *those* as the initial state.

        // Simpler approach for custom RNG initialization:
        // Use a System.Random to populate your custom RNG's state
        System.Random tempRandom = new System.Random(seed);
        for (int i = 0; i < _seedArray.Length; i++)
        {
            _seedArray[i] = tempRandom.Next();
        }
        _seedArrayIndex = 0; // Or some initial index

        // Alternatively, if you only need Next() and NextDouble(), you can implement a simpler XOR-shift or LCG
        // RNG yourself that *does* expose its full state.
        // For simplicity, let's assume a basic LCG or XorShift for example:
        // _seed = seed;
    }

    // Constructor to restore from a saved state
    public CustomRandom(RandomState state)
    {
        _seedArray = (int[])state.SeedArray.Clone(); // Deep copy the array
        _seedArrayIndex = state.SeedArrayIndex;
    }

    // Method to get the current state
    public RandomState GetCurrentState()
    {
        return new RandomState
        {
            SeedArray = (int[])_seedArray.Clone(), // Crucial: Clone the array
            SeedArrayIndex = _seedArrayIndex
        };
    }

    // Implement your own random number generation logic here.
    // Example (very simplified LCG - not what System.Random uses, but illustrates state):
    private long _currentSeed; // For a simple Linear Congruential Generator

    public CustomRandom(long seed)
    {
        _currentSeed = seed;
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException("maxValue", "maxValue must be greater than or equal to minValue");
        long range = (long)maxValue - minValue;
        if (range < 0) // Handle overflow for very large ranges
        {
            return (int)((this.NextLong() % range) + minValue);
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
        _currentSeed = (_currentSeed * 1103515245 + 12345) & 0x7FFFFFFF; // 31-bit LCG
        return _currentSeed;
    }
}

[Serializable]
public class RandomState
{
    public int[] SeedArray;
    public int SeedArrayIndex;
    // For a simple LCG: public long CurrentSeed;
}
