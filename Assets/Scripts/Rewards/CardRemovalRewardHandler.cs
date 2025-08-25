using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardRemovalRewardHandler : MonoBehaviour
{
    public CardRewardHolder CardRewardHolder1;
    public CardRewardHolder CardRewardHolder2;
    public CardRewardHolder CardRewardHolder3;
    List<Card> allCards = new List<Card>();
    public void Load(List<Card> cards)
    {
        allCards = new List<Card>(cards);

        List<List<Card>> cardBuckets = new List<List<Card>>
        {
            new List<Card>(),
            new List<Card>(),
            new List<Card>()
        };

        if (allCards.Count < 9) return;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int randomIndex = Controller.Instance.MetaRNG.Next(0, allCards.Count);
                Card card = allCards[randomIndex];
                allCards.RemoveAt(randomIndex);
                cardBuckets[i].Add(card);
            } 
        }

        CardRewardHolder1.Load(cardBuckets[0]);
        CardRewardHolder2.Load(cardBuckets[1]);
        CardRewardHolder3.Load(cardBuckets[2]);
    }

    public void RemoveCardsAndContinue()
    {
        List<Card> cards = new List<Card>();
        cards.AddRange(CardRewardHolder1.GetHighlightedCardRewards());
        cards.AddRange(CardRewardHolder2.GetHighlightedCardRewards());
        cards.AddRange(CardRewardHolder3.GetHighlightedCardRewards());

        CardRewardHolder1.Cleanup();
        CardRewardHolder2.Cleanup();
        CardRewardHolder3.Cleanup();

        Controller.Instance.RemoveCardsFromPlayerDeck(cards);
        //Controller.Instance.AddCardsToPlayerDeck(cards);

        //View.Instance.Clear();

        //Controller.Instance.StartNextLevel();
        Controller.Instance.GoToRitualRewardScreen();
        gameObject.SetActive(false);
    }

    //public ViewCardScroller ViewCardScroller;

    //public void Load(List<Card> cards)
    //{
    //    ViewCardScroller.Load(cards);
    //}

    //public void RemoveCardsAndContinue()
    //{
    //    List<Card> cards = ViewCardScroller.GetSelectedCards();

    //    Controller.Instance.RemoveCardsFromPlayerDeck(cards);

    //    ViewCardScroller.ClearViewCards();

    //    Controller.Instance.GoToRitualRewardScreen();
    //    //Controller.Instance.StartNextLevel();

    //    gameObject.SetActive(false);
    //}
}
