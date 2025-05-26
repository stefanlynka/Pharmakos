using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarterBundleHandler : MonoBehaviour
{
    public MenuSelectionHandler SelectionHandler;

    public List<StarterBundleBucket> BundleBuckets = new List<StarterBundleBucket>();
    public List<StarterBundle> StarterBundles = new List<StarterBundle>();
    public Dictionary<ViewTarget, StarterBundleBucket> BucketsByTarget = new Dictionary<ViewTarget, StarterBundleBucket>();

    public StarterBundleBucket SelectedBucket;

    public void Load()
    {
        SelectionHandler.enabled = true;

        StarterBundles = Controller.Instance.ProgressionHandler.GetStarterBundles();

        for (int i = 0; i < 3; i++)
        {
            BundleBuckets[i].Load(StarterBundles[i], BundleClicked);
            BucketsByTarget[BundleBuckets[i].ViewTarget] = BundleBuckets[i];
        }

        SelectedBucket = BundleBuckets[0];
        BundleBuckets[0].SetSelected(true);
    }

    public void Continue()
    {
        // Add cards to deck
        Controller.Instance.AddCardsToPlayerDeck(SelectedBucket.StarterBundle.Cards);
        // Add ritual
        Controller.Instance.SetRituals(null, SelectedBucket.StarterBundle.Ritual);

        //Controller.Instance.HideStarterBundleScreen();
        Controller.Instance.StartGame();

        SelectionHandler.enabled = false;
        gameObject.SetActive(false);
    }

    public void BundleClicked(ViewTarget viewTarget)
    {
        if (BucketsByTarget.ContainsKey(viewTarget))
        {
            for (int i = 0; i < 3; i++)
            {
                BundleBuckets[i].SetSelected(false);
            }

            SelectedBucket = BucketsByTarget[viewTarget];
            BucketsByTarget[viewTarget].SetSelected(true);
        }
    }
}

public class StarterBundle
{
    public Ritual Ritual;
    public List<Card> Cards = new List<Card>();

    public StarterBundle(Ritual ritual, List<Card> cards)
    {
        Ritual = ritual;
        Cards = cards;
    }
}

