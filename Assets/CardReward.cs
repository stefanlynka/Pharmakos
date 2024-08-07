using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardReward : MonoBehaviour
{
    public ViewCard ViewCard;

    public void Load(Card card, Action<ViewTarget> CardClicked)
    {
        ViewCard.Load(card, CardClicked);
    }
}
