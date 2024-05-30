using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CardManager
{
    public static List<List<Card>> Decks = new List<List<Card>>();

    public enum DeckNames
    {
        Warriors,
        Monsters
    }

    public static List<Card> GetDeck(DeckNames name)
    {
        List<Card> deck;
        switch (name)
        {
            case DeckNames.Warriors:
                deck = new List<Card>
                {
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Chariot(),
                    new Chariot(),
                    new Chariot(),
                    new Chariot(),
                };
                return deck;
            case DeckNames.Monsters:
                deck = new List<Card>
                {
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Smite(),
                    new Smite(),
                    new Smite(),
                    new Smite(),
                    new Smite(),
                    //new DragonsTeeth(),
                    //new DragonsTeeth(),
                    //new Sphinx(),
                    //new Sphinx(),
                };
                return deck;
        }
        return new List<Card>();
    }

    public static Sprite GetSprite(Card card)
    {
        string cardName = card.GetType().Name;
        return Resources.Load<Sprite>("Images/Cards/" + cardName);
    }
}
