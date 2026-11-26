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
public static class StringExtensions
{
    public static string TrimEnd(this string source, string valueToRemove)
    {
        if (!string.IsNullOrEmpty(source) && source.EndsWith(valueToRemove))
        {
            return source.Substring(0, source.Length - valueToRemove.Length);
        }

        return source;
    }
}

public static class Utility
{
    public static int GetPreyDamage(Follower follower)
    {
        int damage = 1;

        // Reference cards (ritual popups, play history, deck viewers) can be unowned or owned by a
        // player outside combat, so there's no opponent to read Growing Bounties from.
        Player owner = follower?.Owner;
        if (owner?.GameState == null) return damage;
        Player otherPlayer = owner.GetOtherPlayer();
        if (otherPlayer?.PlayerEffects == null) return damage;

        foreach (PlayerEffect playerEffect in otherPlayer.PlayerEffects)
        {
            if (playerEffect is GrowingBountiesTrinketEffectDef growing)
            {
                damage++;
            }
        }

        return damage;
    }
}