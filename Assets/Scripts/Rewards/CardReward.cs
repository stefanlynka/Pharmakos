using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardReward : MonoBehaviour
{
    public ViewCard ViewCard;

    public void Load(Card card, Action<ViewTarget> CardClicked, bool clickable = true)
    {
        if (ViewCard == null || card is Follower != ViewCard is ViewFollower)
        {
            if (ViewCard != null)
            {
                View.Instance.ReleaseCard(ViewCard);
                ViewCard.transform.parent = null;
            }
            
            ViewCard = View.Instance.MakeNewViewCard(card, false);
            ViewCard.transform.parent = transform;
            ViewCard.transform.localPosition = new Vector3(0, 0, -0.1f);
            
            ViewCard.gameObject.SetActive(true);
        }

        ViewCard.Load(card, CardClicked);
        ViewCard.SetDescriptiveMode(true);

        if (ViewCard.CardCollider != null) ViewCard.CardCollider.enabled = clickable;
    }
}
