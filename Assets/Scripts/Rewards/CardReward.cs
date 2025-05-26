using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardReward : MonoBehaviour
{
    public ViewCard ViewCard;

    public void Load(Card card, Action<ViewTarget> CardClicked, bool clickable = true)
    {
        if (card is Follower != ViewCard is ViewFollower)
        {
            View.Instance.ReleaseCard(ViewCard);
            ViewCard.transform.parent = null;
            ViewCard = View.Instance.MakeNewViewCard(card, false);
            ViewCard.transform.parent = transform;
            ViewCard.transform.localPosition = new Vector3();
            ViewCard.gameObject.SetActive(true);
        }

        BoxCollider collider = ViewCard.GetComponent<BoxCollider>();
        if (collider != null) collider.enabled = clickable;

        ViewCard.Load(card, CardClicked);
    }
}
