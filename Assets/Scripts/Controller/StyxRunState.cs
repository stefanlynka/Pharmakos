using System.Collections.Generic;

/// <summary>
/// Tracks everything the player has removed (sacrificed, replaced, given up) during the
/// current run. The River Styx node swaps the player's current deck/rituals/trinkets with
/// the contents of this state.
/// </summary>
public class StyxRunState
{
    public const int StyxBaseDeckSize = 20;

    public List<Card> RemovedCards = new List<Card>();
    public List<Ritual> RemovedRituals = new List<Ritual>();
    public List<Trinket> RemovedTrinkets = new List<Trinket>();

    public void Reset()
    {
        RemovedCards.Clear();
        RemovedRituals.Clear();
        RemovedTrinkets.Clear();
    }

    public void RecordRemovedCard(Card card)
    {
        if (card == null) return;
        RemovedCards.Add(card.MakeBaseCopy());
    }

    public void RecordRemovedRitual(Ritual ritual)
    {
        if (ritual == null) return;
        RemovedRituals.Add(ritual);
    }

    public void RecordRemovedTrinket(Trinket trinket)
    {
        if (trinket == null) return;
        RemovedTrinkets.Add(trinket);
    }

    /// <summary>
    /// The Styx deck starts as 10 Shades and 10 Haunts (alternating). Each card
    /// removed during the run replaces one base card, in removal order. Removals beyond
    /// the base 20 are appended.
    /// </summary>
    public List<Card> GetStyxDeck()
    {
        List<Card> deck = new List<Card>();
        for (int i = 0; i < StyxBaseDeckSize; i++)
        {
            if (i % 2 == 0) deck.Add(new Shade());
            else deck.Add(new Haunt());
        }

        for (int i = 0; i < RemovedCards.Count; i++)
        {
            Card copy = RemovedCards[i].MakeBaseCopy();
            if (i < StyxBaseDeckSize) deck[i] = copy;
            else deck.Add(copy);
        }

        return deck;
    }
}
