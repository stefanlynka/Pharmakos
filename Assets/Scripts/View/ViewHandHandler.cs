using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewHandHandler : MonoBehaviour
{
    public List<ViewCard> ViewCards = new List<ViewCard>();

    public bool IsHuman = false;

    public float MaxSpacing = 1;
    public float CardWidth = 0;
    public float HandWidth = 0;
    public float CardY = 0;
    public float CardZ = 0;
    public float CardZOffset = 1;

    public Vector3 Scale = Vector3.one;
    public float HighlightY = 0;
    public float HighlightZ = 0;

    public static Vector3 HighlightScaleVector = new Vector3(2,2,2);

    public void UpdateHand()
    {
        //ScaleVector = Scale;
        RefreshPositions();
    }

    public void Clear()
    {
        List<ViewCard> viewCardsCopy = new List<ViewCard>(ViewCards);
        foreach (ViewCard viewCard in viewCardsCopy)
        {
            View.Instance.RemoveCard(viewCard.Card.ID);
        }

        ViewCards.Clear();
    }

    public void MoveCardToHand(ViewCard viewCard)
    {
        
        if (viewCard.Card.Owner.IsHuman != IsHuman)
        {
            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            Debug.LogError("Card Added to wrong hand!");
        }
        viewCard.transform.SetParent(transform, false);
        ViewCards.Add(viewCard);
        if (viewCard.Card.Owner.IsHuman)
        {
            viewCard.OnClick = CardInHandClicked;
        }
        //RefreshPositions();
        viewCard.transform.localScale = Scale;
    }

    public void RemoveCard(ViewCard viewCard)
    {
        foreach (ViewCard card in ViewCards)
        {
            if (card.Card.ID == viewCard.Card.ID)
            {
                card.OnClick = null;
                ViewCards.Remove(card);
                RefreshPositions();
                return;
            }
        }
    }

    public void CardInHandClicked(ViewTarget viewTarget)
    {
        ViewEventHandler.Instance.FireTargetInHandClicked(viewTarget);
    }
    private void RefreshPositions()
    {
        int cardCount = ViewCards.Count;
        if (cardCount == 0) return;

        float spacing = Mathf.Min(MaxSpacing, (HandWidth - CardWidth * cardCount) / cardCount);

        bool selectingTarget = View.Instance.SelectionHandler.IsSelectingTarget();

        bool skipHighlighting = false;
        if (!IsHuman) skipHighlighting = true;
        else if (selectingTarget) skipHighlighting = true;

        for (int i = 0; i < ViewCards.Count; i++)
        {
            ViewCard viewCard = ViewCards[i];
            Vector3 newPos = new Vector3(CardWidth / 2 + CardWidth * i + spacing * i, CardY, CardZ - CardZOffset * i);
            if (View.Instance.CurrentHover == viewCard && !selectingTarget)
            {
                viewCard.transform.localScale = HighlightScaleVector;

                newPos.y = HighlightY;
                newPos.z = HighlightZ;

                viewCard.SetDescriptiveMode(true);
            }
            else
            {
                viewCard.transform.localScale = Scale;

                viewCard.SetDescriptiveMode(false);
            }
            viewCard.transform.localPosition = newPos;

            bool canPlay = viewCard.Card.CanPlay();

            viewCard.SetHighlight(!skipHighlighting && canPlay && View.Instance.IsInteractible);
            viewCard.UpdateCost();
        }
    }

    public Vector3 GetPotentialCardPosition(int index = -1)
    {
        if (index == -1) index = ViewCards.Count;

        int cardCount = ViewCards.Count + 1;
        if (index >= cardCount)
        {
            cardCount = index + 1;
        }

        float spacing = Mathf.Min(MaxSpacing, (HandWidth - CardWidth * cardCount) / cardCount);

        Vector3 newPos = transform.position + new Vector3(CardWidth / 2 + CardWidth * index + spacing * index, CardY, CardZ - CardZOffset * index);
        return newPos;
    }

    public ViewCard GetViewCardByITargetID(int targetID)
    {
        foreach (ViewCard viewCard in ViewCards)
        {
            if (viewCard.Card.ID == targetID)
            {
                return viewCard;
            }
        }
        return null;
    }
}
