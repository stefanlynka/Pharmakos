using System;
using System.Reflection;
using Pharmakos.Events;
using UnityEngine;

public static class EventOutcomePreview
{
    public struct PopupVisibility
    {
        public bool Popup1;
        public bool Popup2;
    }

    public static bool SupportsPreview(EventOutcomeData outcome)
    {
        if (outcome == null) return false;

        switch (outcome.OutcomeType)
        {
            case EventOutcomeType.GainCard:
            case EventOutcomeType.GainTrinket:
            case EventOutcomeType.GainRituals:
            case EventOutcomeType.GainHeartstring:
                return true;
            default:
                return false;
        }
    }

    public static PopupVisibility Populate(EventOutcomeData outcome)
    {
        var visibility = new PopupVisibility();
        if (!SupportsPreview(outcome) || PopupHandler.Instance == null)
            return visibility;

        switch (outcome.OutcomeType)
        {
            case EventOutcomeType.GainCard:
                visibility = PopulateCards(outcome.RewardInfo);
                break;
            case EventOutcomeType.GainTrinket:
                visibility = PopulateTrinkets(outcome.RewardInfo);
                break;
            case EventOutcomeType.GainRituals:
                visibility = PopulateRituals(outcome.RewardInfo);
                break;
            case EventOutcomeType.GainHeartstring:
                PopupHandler.ShowHeartstring(PopupSlot.One);
                visibility.Popup1 = true;
                break;
        }

        return visibility;
    }

    static PopupVisibility PopulateCards(System.Collections.Generic.List<string> rewardInfo)
    {
        var visibility = new PopupVisibility();
        int slot = 0;

        foreach (string rewardName in rewardInfo)
        {
            Card card = TryCreateCard(rewardName);
            if (card == null) continue;

            if (slot == 0)
            {
                PopupHandler.ShowCard(PopupSlot.One, card);
                visibility.Popup1 = true;
            }
            else if (slot == 1)
            {
                PopupHandler.ShowCard(PopupSlot.Two, card);
                visibility.Popup2 = true;
                break;
            }

            slot++;
        }

        return visibility;
    }

    static PopupVisibility PopulateTrinkets(System.Collections.Generic.List<string> rewardInfo)
    {
        var visibility = new PopupVisibility();
        int slot = 0;

        foreach (string rewardName in rewardInfo)
        {
            Trinket trinket = TryCreateTrinket(rewardName);
            if (trinket == null) continue;

            if (slot == 0)
            {
                PopupHandler.ShowTrinket(PopupSlot.One, trinket);
                visibility.Popup1 = true;
            }
            else if (slot == 1)
            {
                PopupHandler.ShowTrinket(PopupSlot.Two, trinket);
                visibility.Popup2 = true;
                break;
            }

            slot++;
        }

        return visibility;
    }

    static PopupVisibility PopulateRituals(System.Collections.Generic.List<string> rewardInfo)
    {
        var visibility = new PopupVisibility();
        int slot = 0;

        foreach (string rewardName in rewardInfo)
        {
            Ritual ritual = TryCreateRitual(rewardName);
            if (ritual == null) continue;

            if (slot == 0)
            {
                PopupHandler.ShowRitual(PopupSlot.One, ritual);
                visibility.Popup1 = true;
            }
            else if (slot == 1)
            {
                PopupHandler.ShowRitual(PopupSlot.Two, ritual);
                visibility.Popup2 = true;
                break;
            }

            slot++;
        }

        return visibility;
    }

    public static void Apply(EventOutcomeData outcome)
    {
        if (outcome == null || Controller.Instance == null)
            return;

        switch (outcome.OutcomeType)
        {
            case EventOutcomeType.GainCard:
                ApplyGainCards(outcome.RewardInfo);
                break;
            case EventOutcomeType.GainTrinket:
                ApplyGainTrinkets(outcome.RewardInfo);
                break;
            case EventOutcomeType.GainHeartstring:
                ApplyGainHeartstrings(outcome.RewardInfo);
                break;
        }
    }

    static void ApplyGainCards(System.Collections.Generic.List<string> rewardInfo)
    {
        var cards = new System.Collections.Generic.List<Card>();
        if (rewardInfo != null)
        {
            foreach (string rewardName in rewardInfo)
            {
                Card card = TryCreateCard(rewardName);
                if (card != null)
                    cards.Add(card.MakeBaseCopy());
            }
        }

        if (cards.Count > 0)
        {
            Controller.Instance.AddCardsToPlayerDeck(cards);
            return;
        }

        if (rewardInfo != null && rewardInfo.Count > 0)
            Debug.LogWarning("EventOutcomePreview: GainCard outcome had no valid card types in RewardInfo.");
    }

    static void ApplyGainTrinkets(System.Collections.Generic.List<string> rewardInfo)
    {
        int added = 0;
        if (rewardInfo != null)
        {
            foreach (string rewardName in rewardInfo)
            {
                Trinket trinket = TryCreateTrinket(rewardName);
                if (trinket == null) continue;

                Controller.Instance.AddTrinket(trinket.MakeBaseCopy());
                added++;
            }
        }

        if (added == 0 && rewardInfo != null && rewardInfo.Count > 0)
            Debug.LogWarning("EventOutcomePreview: GainTrinket outcome had no valid trinket types in RewardInfo.");
    }

    static void ApplyGainHeartstrings(System.Collections.Generic.List<string> rewardInfo)
    {
        int amount = 1;
        if (rewardInfo != null && rewardInfo.Count > 0)
        {
            if (int.TryParse(rewardInfo[0]?.Trim(), out int parsed) && parsed > 0)
                amount = parsed;
            else
            {
                int namedEntries = 0;
                foreach (string entry in rewardInfo)
                {
                    if (!string.IsNullOrWhiteSpace(entry))
                        namedEntries++;
                }

                if (namedEntries > 0)
                    amount = namedEntries;
            }
        }

        Controller.Instance.AddHeartstrings(amount);
    }

    public static Card TryCreateCard(string typeName)
    {
        Type type = FindType(typeName);
        if (type == null || !typeof(Card).IsAssignableFrom(type))
            return null;

        return (Card)Activator.CreateInstance(type);
    }

    public static Ritual TryCreateRitual(string typeName)
    {
        Type type = FindType(typeName);
        if (type == null || !typeof(Ritual).IsAssignableFrom(type))
            return null;

        return (Ritual)Activator.CreateInstance(type);
    }

    public static Trinket TryCreateTrinket(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        string trimmed = typeName.Trim();
        Type type = FindType(trimmed);
        if (type == null || !typeof(Trinket).IsAssignableFrom(type))
            type = FindType(trimmed + "Trinket");

        if (type == null || !typeof(Trinket).IsAssignableFrom(type))
            return null;

        return (Trinket)Activator.CreateInstance(type);
    }

    static Type FindType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        typeName = typeName.Trim();
        Type type = Type.GetType(typeName);
        if (type != null)
            return type;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }
}
