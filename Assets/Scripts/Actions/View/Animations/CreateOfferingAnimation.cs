using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CreateOfferingAnimation : AnimationAction
{
    private CreateOfferingAction createOfferingAction;
    private Player owner;
    private OfferingType offeringType;
    private int amount;
    private int sourceID;
    private int destinationID;

    private Tween animationTween;
    GameObject offeringObject;

    private Vector3 startPos;
    private Vector3 endPos;

    private float duration = 0.5f;

    public CreateOfferingAnimation(GameAction gameAction, Player owner,  OfferingType offeringType, int amount, int sourceID, int destinationID) : base(gameAction)
    {
        Stackable = true;

        if (gameAction is CreateOfferingAction)
        {
            createOfferingAction = gameAction as CreateOfferingAction;

            this.owner = owner;
            this.offeringType = offeringType;
            this.amount = amount;
            this.sourceID = sourceID;
            this.destinationID = destinationID;
        }
    }



    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        //Debug.LogWarning("Create Offering Animation for "+ sourceID);

        if (createOfferingAction == null)
        {
            CallCallback();
            return;
        }

        ViewTarget viewSource = View.Instance.GetViewTargetByID(sourceID);

        if (viewSource == null)
        {
            Debug.LogError("CreateOfferingAnimation: ViewTarget with ID " + sourceID + " not found.");
            CallCallback();
            return;
        }

        startPos = viewSource.transform.position;
        ViewTarget destinationTarget = View.Instance.GetViewTargetByID(destinationID);
        if (destinationTarget is ViewPlayer)
        {
            endPos = OfferingHandler.Instance.GetOfferingPosition(owner, offeringType);
        }
        else if (destinationTarget is ViewRitual)
        {
            endPos = destinationTarget.transform.position;
        }

        offeringObject = View.Instance.MakeNewOffering(offeringType);

        Sequence moveSequence = new Sequence();
        moveSequence.Add(new Tween(TweenProgress, 0, 1, duration));
        moveSequence.Add(new SequenceAction(Complete));
        moveSequence.Start();
    }

    private void TweenProgress(float progress)
    {
        offeringObject.transform.position = startPos + (endPos - startPos) * progress;
    }

    private void Complete()
    {
        //offeringObject.transform.position = endPos;
        View.Instance.RemoveOffering(offeringObject);
        //Debug.LogWarning(summonFollowerAction.Follower.Owner.GetName() + " played " + summonFollowerAction.Follower.GetName() + " " + summonFollowerAction.Follower.ID + " Animation end");
        OnFinish?.Invoke();
    }
}
