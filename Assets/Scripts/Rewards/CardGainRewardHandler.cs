using System.Collections;
using System.Collections.Generic;
using System.Numerics;
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
            //CardRewardHolders[i].Cleanup();
        }

        Controller.Instance.AddCardsToPlayerDeck(cards);

        //View.Instance.Clear();

        Controller.Instance.StartNextLevel();
        //gameObject.SetActive(false);

        Sequence moveSequence = new Sequence();
        moveSequence.Add(new Tween(Wait, 0, 1, 0.8f));
        moveSequence.Add(new SequenceAction(Cleanup));
        moveSequence.Start();
    }
    private void Wait(float progress)
    {

    }
    private void Cleanup()
    {
        for (int i = 0; i < rewardCount; i++)
        {
            CardRewardHolders[i].Cleanup();
        }
        View.Instance.Clear();
        gameObject.SetActive(false);
    }
}
