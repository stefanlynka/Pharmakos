using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewFollower : ViewCard
{
    public TextMeshPro AttackText;
    public TextMeshPro HealthText;

    public Follower Follower;

    public override void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        base.Load(cardData, onClick);
        Follower = cardData as Follower;
        if (Follower == null) return;

        Target = Follower;
        AttackText.text = Follower.BaseAttack.ToString();
        HealthText.text = Follower.BaseHealth.ToString();
    }
}
