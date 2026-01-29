using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardRemovalRewardHandler : MonoBehaviour
{
    public List<CardRewardHolder> CardRewardHolders = new List<CardRewardHolder>();

    //public CardRewardHolder CardRewardHolder1;
    //public CardRewardHolder CardRewardHolder2;
    //public CardRewardHolder CardRewardHolder3;
    List<Card> allCards = new List<Card>();

    public int rewardCount = 2;

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

        for (int i = 0; i < rewardCount; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int randomIndex = Controller.Instance.MetaRNG.Next(0, allCards.Count);
                Card card = allCards[randomIndex];
                allCards.RemoveAt(randomIndex);
                cardBuckets[i].Add(card);
            }
            CardRewardHolders[i].Load(cardBuckets[i]);
        }

    }

    public void RemoveCardsAndContinue()
    {
        List<Card> cards = new List<Card>();
        for (int i = 0; i < rewardCount; i++)
        {
            cards.AddRange(CardRewardHolders[i].GetHighlightedCardRewards());
            CardRewardHolders[i].Cleanup();
        }

        Controller.Instance.RemoveCardsFromPlayerDeck(cards);
        Controller.Instance.GoToRitualRewardScreen();
        gameObject.SetActive(false);
    }

}
