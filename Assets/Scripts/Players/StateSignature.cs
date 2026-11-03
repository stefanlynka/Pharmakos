using System;
using System.Collections.Generic;

// 64-bit hash of every GameState field that can change the AI's legal moves or utility during its turn.
// The AI search reuses a cached line for any state with the same signature, so a field missing here can
// make it reuse a line that is wrong for the current state. AIDebugMode compares against an uncached search to catch that.
// Target IDs are included because cached lines refer to cards and targets by ID.
public static class StateSignature
{
    static readonly OfferingType[] Offerings =
    {
        OfferingType.Gold, OfferingType.Blood, OfferingType.Bone, OfferingType.Crop, OfferingType.Scroll,
    };

    static readonly StaticEffect[] AllStaticEffects = (StaticEffect[])Enum.GetValues(typeof(StaticEffect));

    public static ulong Compute(GameState state)
    {
        ulong hash = 0xCBF29CE484222325UL;

        Add(ref hash, state.CurrentTeamID);
        Add(ref hash, state.HighestTargetID);
        Add(ref hash, state.RNG.CurrentSeed);
        Add(ref hash, state.FollowerDeathsThisTurn);
        AddCardIdentity(ref hash, state.LastFollowerThatDied);

        AddPlayer(ref hash, state.Human);
        AddPlayer(ref hash, state.AI);

        return hash;
    }

    static void AddPlayer(ref ulong hash, Player player)
    {
        Add(ref hash, player.Health);
        Add(ref hash, player.CurrentHeartStrings);
        Add(ref hash, player.DoneWithPlayingCards ? 1 : 0);

        AddOfferings(ref hash, player.Offerings);
        AddOfferings(ref hash, player.FollowerCostReductions);
        AddOfferings(ref hash, player.RitualCostReductions);

        Add(ref hash, player.MinorRitual != null ? player.MinorRitual.ID : -1);
        Add(ref hash, player.MajorRitual != null ? player.MajorRitual.ID : -1);

        Add(ref hash, player.PlayerEffects.Count);
        foreach (PlayerEffect effect in player.PlayerEffects)
            Add(ref hash, effect.GetType().GetHashCode());

        Add(ref hash, player.StartOfTurnActions.Count);
        Add(ref hash, player.EndOfTurnActions.Count);
        Add(ref hash, player.StartOfEveryTurnActions.Count);
        Add(ref hash, player.EndOfEveryTurnActions.Count);

        Add(ref hash, player.Hand.Count);
        foreach (Card card in player.Hand)
        {
            AddCardIdentity(ref hash, card);
            AddOfferings(ref hash, card.Costs);
        }

        Add(ref hash, player.Deck.Count);
        foreach (Card card in player.Deck)
            AddCardIdentity(ref hash, card);

        Add(ref hash, player.Graveyard.Count);
        foreach (Card card in player.Graveyard)
            Add(ref hash, card.ID);

        Add(ref hash, player.BattleRow.Followers.Count);
        foreach (Follower follower in player.BattleRow.Followers)
            AddFollower(ref hash, follower);
    }

    static void AddFollower(ref ulong hash, Follower follower)
    {
        AddCardIdentity(ref hash, follower);
        Add(ref hash, follower.CurrentAttack);
        Add(ref hash, follower.CurrentHealth);
        Add(ref hash, follower.MaxHealth);
        Add(ref hash, follower.BaseAttacksPerTurn);
        Add(ref hash, follower.TimesThisAttackedThisTurn);
        Add(ref hash, follower.PlayedThisTurn ? 1 : 0);
        Add(ref hash, follower.SkippedAttack ? 1 : 0);
        Add(ref hash, follower.Alive ? 1 : 0);
        Add(ref hash, follower.InnateEffects.Count);
        Add(ref hash, follower.SpellsCastOnThisByOwner.Count);

        foreach (StaticEffect effect in AllStaticEffects)
        {
            int count = follower.StaticEffects.TryGetValue(effect, out List<FollowerEffectInstance> instances) && instances != null
                ? instances.Count
                : 0;
            Add(ref hash, count);
        }

        Add(ref hash, follower.OnEnterEffects.Count);
        Add(ref hash, follower.OnDeathEffects.Count);
        Add(ref hash, follower.OnAttackEffects.Count);
        Add(ref hash, follower.OnAttackedEffects.Count);
        Add(ref hash, follower.OnDamageEffects.Count);
        Add(ref hash, follower.OnDamagedEffects.Count);
        Add(ref hash, follower.OnDrawBloodEffects.Count);
        Add(ref hash, follower.OnKillEffects.Count);
        Add(ref hash, follower.OnTargetedEffects.Count);
    }

    static void AddCardIdentity(ref ulong hash, Card card)
    {
        if (card == null)
        {
            Add(ref hash, -1);
            return;
        }

        Add(ref hash, card.ID);
        Add(ref hash, card.GetType().GetHashCode());
    }

    static void AddOfferings(ref ulong hash, Dictionary<OfferingType, int> offerings)
    {
        foreach (OfferingType offering in Offerings)
        {
            int amount = offerings != null && offerings.TryGetValue(offering, out int value) ? value : 0;
            Add(ref hash, amount);
        }
    }

    static void Add(ref ulong hash, long value)
    {
        hash ^= (ulong)value + 0x9E3779B97F4A7C15UL + (hash << 6) + (hash >> 2);
        hash = (hash ^ (hash >> 30)) * 0xBF58476D1CE4E5B9UL;
        hash ^= hash >> 27;
    }
}
