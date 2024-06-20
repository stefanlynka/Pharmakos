using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageAnimation : AnimationAction
{
    DealDamageAction dealDamageAction;
    public DamageAnimation(GameAction gameAction) : base(gameAction)
    {
        if (gameAction is DealDamageAction) dealDamageAction = (DealDamageAction)gameAction;

        // Setup Tweens
    }

    public override void Play(Action onFinish = null)
    {
        OnFinish = onFinish;
        CallCallback();
        // Get ViewCards
        // TODO: Add unique int IDs per card. Create dictionaries pointing to those cards and their viewCards. Dictionary<int id, Card Card>
    }

 
}
