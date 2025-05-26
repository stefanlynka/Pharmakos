using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardGainRewardHandler : MonoBehaviour
{
    public CardRewardHolder CardRewardHolder1;
    public CardRewardHolder CardRewardHolder2;
    public CardRewardHolder CardRewardHolder3;


    public void Load(int levelCompleted)
    {
        List<Card> possibleRewards;
        List<List<Card>> cardBuckets = new List<List<Card>>
        {
            new List<Card>(),
            new List<Card>(),
            new List<Card>()
        };
        for (int i = 0; i < 3; i++)
        {
            possibleRewards = Controller.Instance.ProgressionHandler.GetPossibleCardRewards();
            for (int j = 0; j < 3; j++)
            {
                int randomIndex = Controller.Instance.CanonGameState.RNG.Next(0, possibleRewards.Count);
                cardBuckets[i].Add(possibleRewards[randomIndex]);
                possibleRewards.RemoveAt(randomIndex);
            }
        }

        CardRewardHolder1.Load(cardBuckets[0]);
        CardRewardHolder2.Load(cardBuckets[1]);
        CardRewardHolder3.Load(cardBuckets[2]);

        //bucket1.Add(possibleRewards)
    }

    public void SelectCardsAndContinue()
    {
        List<Card> cards = new List<Card>();
        cards.AddRange(CardRewardHolder1.GetHighlightedCardRewards());
        cards.AddRange(CardRewardHolder2.GetHighlightedCardRewards());
        cards.AddRange(CardRewardHolder3.GetHighlightedCardRewards());

        CardRewardHolder1.Cleanup();
        CardRewardHolder2.Cleanup();
        CardRewardHolder3.Cleanup();


        Controller.Instance.AddCardsToPlayerDeck(cards);

        View.Instance.Clear();

        Controller.Instance.StartNextLevel();
        gameObject.SetActive(false);
    }
}
