using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class CardGainRewardHandler : MonoBehaviour
{
    public Camera CardRewardCamera;
    public List<CardRewardHolder> CardRewardHolders = new List<CardRewardHolder>();

    public int rewardCount = 2;

    void OnDisable()
    {
        DeactivateMenuSelection();
    }

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

        ActivateMenuSelection();
    }

    void ActivateMenuSelection()
    {
        if (View.Instance?.MenuSelectionHandler == null) return;

        Camera selectionCamera = CardRewardCamera;
        if (selectionCamera == null)
            selectionCamera = GetComponentInChildren<Camera>(true);
        if (selectionCamera == null
            && ScreenHandler.Instance != null
            && ScreenHandler.Instance.TryGetScreen(ScreenName.CardGainRewards, out Screen screen))
            selectionCamera = screen.Camera;

        SetRewardCameraActive(true);
        View.Instance.MenuSelectionHandler.Activate(selectionCamera, () => false);
    }

    void DeactivateMenuSelection()
    {
        if (View.Instance?.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Deactivate();
    }

    void SetRewardCameraActive(bool active)
    {
        if (CardRewardCamera == null)
            CardRewardCamera = GetComponentInChildren<Camera>(true);
        if (CardRewardCamera == null) return;

        CardRewardCamera.gameObject.SetActive(active);
        CardRewardCamera.enabled = active;
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
        DeactivateMenuSelection();

        for (int i = 0; i < rewardCount; i++)
        {
            CardRewardHolders[i].Cleanup();
        }
        View.Instance.Clear();
        gameObject.SetActive(false);
    }
}
