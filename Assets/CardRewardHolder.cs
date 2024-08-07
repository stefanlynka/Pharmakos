using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardRewardHolder : MonoBehaviour
{
    public List<CardReward> CardRewards = new List<CardReward>();

    public void Load(List<Card> cardRewards)
    {
        if (cardRewards.Count == 0) return;
        CardRewards[0].Load(cardRewards[0], CardClicked);
        if (cardRewards.Count == 1) return;
        CardRewards[1].Load(cardRewards[1], CardClicked);
        if (cardRewards.Count == 2) return;
        CardRewards[2].Load(cardRewards[2], CardClicked);
    }

    public void CardClicked(ViewTarget viewTarget)
    {
        ViewCard viewCard = viewTarget as ViewCard;
        if (viewCard != null)
        {
            bool alreadyClicked = viewCard.IsHighlighted();

            if (alreadyClicked)
            {
                viewCard.SetHighlight(false);
            }
            else
            {
                CardRewards[0].ViewCard.SetHighlight(false);
                CardRewards[1].ViewCard.SetHighlight(false);
                CardRewards[2].ViewCard.SetHighlight(false);

                viewCard.SetHighlight(true);
            }

            //viewCard.SetHighlight(!viewCard.IsHighlighted());
        }
        //ViewEventHandler.Instance.FireTargetInHandClicked(viewTarget);
    }

    public List<Card> GetHighlightedCardRewards()
    {
        List<Card> viewCards = new List<Card>();

        foreach (CardReward cardReward in CardRewards)
        {
            if (cardReward.ViewCard.IsHighlighted()) viewCards.Add(cardReward.ViewCard.Card);
        }

        return viewCards;
    }

    public void Cleanup()
    {
        CardRewards[0].ViewCard.SetHighlight(false);
        CardRewards[1].ViewCard.SetHighlight(false);
        CardRewards[2].ViewCard.SetHighlight(false);
    }
}
