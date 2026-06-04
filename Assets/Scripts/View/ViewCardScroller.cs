using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewCardScroller : MonoBehaviour
{
    public float Width = 0;
    public float Height = 0;

    public float CardWidth = 0;
    public float CardHeight = 0;
    public float CardMargin = 0;
    public float CardScale = 1f;

    private List<Card> cards = new List<Card>();
    private List<ViewCard> viewCards = new List<ViewCard>();

    private int selectedCards = 0;
    private int numberCardsToRemove = 3;

    public float scrollIncrement = 5f;
    public float scrollSpeed = 5f;
    private float currentScrollOffset = 0f;
    private float desiredScrollOffset = 0f;
    private float maxScrollOffset = 0f;

    public void Load(List<Card> cards)
    {
        this.cards = cards;

        CreateViewCards();
    }
    public void Exit()
    {
        ClearViewCards();
    }

    private void CreateViewCards()
    {
        ClearViewCards();

        foreach (Card card in cards)
        {
            ViewCard viewCard = View.Instance.MakeNewViewCard(card, false);
            if (viewCard == null)
                continue;

            // Hide until layout and descriptive mode are applied (avoids one-frame flicker).
            viewCard.transform.localScale = Vector3.zero;
            viewCard.transform.parent = this.transform;
            viewCard.SetHighlight(false);
            viewCard.SetDescriptiveMode(true);
            viewCards.Add(viewCard);
        }

        selectedCards = 0;
        RepositionCards();

        foreach (ViewCard viewCard in viewCards)
            ApplyCardScale(viewCard);
    }

    void ApplyCardScale(ViewCard viewCard)
    {
        float scale = CardScale;
        viewCard.transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void ClearViewCards()
    {
        foreach (ViewCard viewCard in viewCards)
        {
            viewCard.transform.localScale = Vector3.zero;
            View.Instance.ReleaseCard(viewCard);
        }
        viewCards.Clear();
    }
    
    private void CardClicked(ViewTarget viewTarget)
    {
        //Debug.LogError("Card Clicked");
        ViewCard viewCard = viewTarget as ViewCard;
        if (viewCard == null) return;

        if (viewCard.IsHighlighted())
        {
            viewCard.SetHighlight(false);
            selectedCards--;
        }
        else if (selectedCards < numberCardsToRemove)
        {
            viewCard.SetHighlight(true);
            selectedCards++;
        }
    }

    public List<Card> GetSelectedCards()
    {
        List<Card> cards = new List<Card>();

        foreach (ViewCard viewCard in viewCards)
        {
            if (viewCard.IsHighlighted()) cards.Add(viewCard.Card);
        }

        return cards;
    }

    // Update is called once per frame
    void Update()
    {
        CheckInputs();

        RepositionCards();
    }
    private void CheckInputs()
    {
        float scrollVector = Input.mouseScrollDelta.y;
        if (scrollVector == 0) return;

        if (scrollVector < 0)
        {
            desiredScrollOffset = currentScrollOffset + scrollIncrement;
        }
        else
        {
            desiredScrollOffset = currentScrollOffset - scrollIncrement;
        }

        desiredScrollOffset = Mathf.Clamp(desiredScrollOffset, 0, maxScrollOffset);
    }
    private void RepositionCards()
    {
        if (CardWidth <= 0) return;

        float direction = desiredScrollOffset > currentScrollOffset ? 1 : -1;
        float delta = direction * Time.deltaTime * scrollSpeed;
        if (Mathf.Abs(delta) > Mathf.Abs(desiredScrollOffset - currentScrollOffset))
        {
            currentScrollOffset = desiredScrollOffset;
        }
        else
        {
            currentScrollOffset += delta;
        }

        // Find out how many columns will fit
        int totalColumns = 1;
        float currentWidth = CardWidth;
        while (currentWidth < Width)
        {
            totalColumns++;
            currentWidth += CardWidth + CardMargin;
        }

        float numRows = Mathf.Ceil(viewCards.Count / (float)totalColumns);
        maxScrollOffset = (numRows*CardHeight + (numRows-1)*CardMargin) - Height;
        maxScrollOffset = Mathf.Max(maxScrollOffset, 0);

        int col = 0;
        int row = 0;
        foreach (ViewCard viewCard in viewCards)
        {
            viewCard.transform.localPosition = new Vector3(col * (CardWidth + CardMargin), -row * (CardHeight + CardMargin) + currentScrollOffset);
            if (col == totalColumns-1)
            {
                col = 0;
                row++;
            }
            else
            {
                col++;
            }
        }
    }
}
