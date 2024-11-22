using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class Utility
//{
//}

public static class DictionaryExtensions
{
    public static void AddToList<TKey, TValue>(this Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
    {
        // Check if the key exists, if not, create a new list
        if (!dict.ContainsKey(key))
        {
            dict[key] = new List<TValue>();
        }

        // Add the value to the list
        dict[key].Add(value);
    }
}