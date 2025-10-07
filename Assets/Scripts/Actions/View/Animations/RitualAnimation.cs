using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class RitualAnimation : AnimationAction
{
    private PlayRitualAction ritualAction;
    public ITarget Source;
    public ITarget Target;

    private Tween animationTween;
    GameObject ritualObject;

    //private Vector3 startPos;
    //private Vector3 endPos;

    private float duration = 0.55f;

    public RitualAnimation(GameAction gameAction, ITarget source, ITarget target) : base(gameAction)
    {
        //Stackable = true;
        ritualAction = gameAction as PlayRitualAction;
        Source = source;
        Target = target;
    }



    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        //if (createOfferingAction == null)
        //{
        //    CallCallback();
        //    return;
        //}

        ViewTarget viewSource = View.Instance.GetViewTargetByID(Source.GetID());
        ViewTarget viewTarget = View.Instance.GetViewTargetByID(Target.GetID());

        if (viewTarget == null)
        {
            Debug.LogError("RitualAnimation: ViewTarget with ID " + Target.GetID() + " not found.");
            CallCallback();
            return;
        }

        ritualObject = View.Instance.CreateRitualAnimation();

        if (ritualObject == null)
        {
            CallCallback();
            return;
        }

        ritualObject.SetActive(true);
        ritualObject.transform.position = viewTarget.transform.position + new Vector3(0, 0, -5);
        ritualObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
        ritualObject.transform.localScale = Vector3.one * 2.5f;

        Sequence moveSequence = new Sequence();
        moveSequence.Add(new Tween(TweenProgress, 0, 1, duration));
        moveSequence.Add(new SequenceAction(Complete));
        moveSequence.Start();
    }

    private void TweenProgress(float progress)
    {

        //offeringObject.transform.position = startPos + (endPos - startPos) * progress;
    }

    private void Complete()
    {
        if (ritualObject != null)
        {
            UnityEngine.Object.Destroy(ritualObject); 
        }

        CallCallback();
    }
}
