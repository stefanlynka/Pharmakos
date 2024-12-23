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

    public Vector3 HighlightScaleVector = new Vector3(2,2,2);

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
            View.Instance.RemoveCard(viewCard.Card);
        }

        ViewCards.Clear();
    }

    public void MoveCardToHand(ViewCard viewCard)
    {
        viewCard.transform.SetParent(transform, false);
        ViewCards.Add(viewCard);
        viewCard.OnClick = CardInHandClicked;
        //RefreshPositions();
        viewCard.transform.localScale = Scale;
    }

    public void RemoveCard(ViewCard viewCard)
    {
        ViewCards.Remove(viewCard);
        RefreshPositions();
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

        bool skipHighlighting = false;
        if (!IsHuman) skipHighlighting = true;
        else if (View.Instance.SelectionHandler.IsHoldingCard()) skipHighlighting = true;

        for (int i = 0; i < ViewCards.Count; i++)
        {
            ViewCard viewCard = ViewCards[i];
            Vector3 newPos = new Vector3(CardWidth / 2 + CardWidth * i + spacing * i, CardY, CardZ - CardZOffset * i);
            if (View.Instance.CurrentHover == viewCard)
            {
                viewCard.transform.localScale = HighlightScaleVector;

                newPos.y = HighlightY;
                newPos.z = HighlightZ;
            }
            else
            {
                viewCard.transform.localScale = Scale;
            }
            viewCard.transform.localPosition = newPos;

            bool canPlay = viewCard.Card.CanPlay();

            viewCard.SetHighlight(!skipHighlighting && canPlay);
        }
    }

    
}
