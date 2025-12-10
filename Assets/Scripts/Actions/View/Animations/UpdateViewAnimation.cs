using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class UpdateViewAnimation : AnimationAction
{
    private GameAction gameAction;
    //private PlayRitualAction ritualAction;
    public Player Player;
    //public ITarget Target;

    //private Tween animationTween;
    //GameObject ritualObject;

    ////private Vector3 startPos;
    ////private Vector3 endPos;

    //private float duration = 0.55f;

    public UpdateViewAnimation(GameAction gameAction, Player player) : base(gameAction)
    {
        //Stackable = true;
        gameAction = gameAction as PlayRitualAction;
        Player = player;
    }



    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        View.Instance.Player1.ViewMajorRitual.Refresh();
        View.Instance.Player1.ViewMinorRitual.Refresh();

        View.Instance.Player2.ViewMajorRitual.Refresh();
        View.Instance.Player2.ViewMinorRitual.Refresh();

        CallCallback();
    }
}
