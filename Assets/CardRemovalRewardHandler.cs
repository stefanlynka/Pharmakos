using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardRemovalRewardHandler : MonoBehaviour
{
    public ViewCardScroller ViewCardScroller;

    public void Load(List<Card> cards)
    {
        ViewCardScroller.Load(cards);
    }
    
    public void RemoveCardsAndContinue()
    {
        List<Card> cards = ViewCardScroller.GetSelectedCards();

        Controller.Instance.RemoveCardsFromPlayerDeck(cards);

        ViewCardScroller.ClearViewCards();

        Controller.Instance.StartNextLevel();

        gameObject.SetActive(false);
    }
}
