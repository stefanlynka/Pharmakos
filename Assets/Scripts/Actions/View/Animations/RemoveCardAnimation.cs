using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class RemoveCardAnimation : AnimationAction
{
    //private float duration = 0.25f;
    public Card Card;

    public RemoveCardAnimation(GameAction gameAction, Card card) : base(gameAction)
    {
        Card = card;
    }

    public override void Play(Action onFinish = null)
    {
        OnFinish = onFinish;

        View.Instance.RemoveCard(Card);

        OnFinish?.Invoke();
    }

}
