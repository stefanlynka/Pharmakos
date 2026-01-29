using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardGainRewardHandler : MonoBehaviour
{
    public List<CardRewardHolder> CardRewardHolders = new List<CardRewardHolder>();

    public int rewardCount = 2;

    public void Load(int levelCompleted)
    {
        List<Card> possibleRewards;
        List<List<Card>> cardBuckets = new List<List<Card>>
        {
            new List<Card>(),
            new List<Card>(),
            new List<Card>()
        };
        for (int i = 0; i < rewardCount; i++)
        {
            possibleRewards = Controller.Instance.ProgressionHandler.GetPossibleCardRewards();
            if (possibleRewards.Count == 0) continue;

            for (int j = 0; j < 3; j++)
            {
                int randomIndex = Controller.Instance.MetaRNG.Next(0, possibleRewards.Count);
                cardBuckets[i].Add(possibleRewards[randomIndex]);
                possibleRewards.RemoveAt(randomIndex);
            }

            CardRewardHolders[i].Load(cardBuckets[i]);
        }

    }

    public void SelectCardsAndContinue()
    {
        List<Card> cards = new List<Card>();

        for (int i = 0; i < rewardCount; i++)
        {
            cards.AddRange(CardRewardHolders[i].GetHighlightedCardRewards());
            CardRewardHolders[i].Cleanup();
        }

        Controller.Instance.AddCardsToPlayerDeck(cards);

        View.Instance.Clear();

        Controller.Instance.StartNextLevel();
        gameObject.SetActive(false);
    }
}
