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
    private Vector3 controlPoint;

    private float duration = 0.65f;
    private Vector3 middleOfScreen = new Vector3(0, 0, 30);

    public CreateOfferingAnimation(GameAction gameAction, Player owner, OfferingType offeringType, int amount, int sourceID, int destinationID) : base(gameAction)
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

        if (createOfferingAction == null)
        {
            CallCallback();
            return;
        }

        ViewTarget viewSource = View.Instance.GetViewTargetByID(sourceID);

        if (viewSource == null)
        {
            Debug.LogWarning("CreateOfferingAnimation: ViewTarget with ID " + sourceID + " not found.");
            startPos = middleOfScreen;
            //View.Instance.GetViewTargetByID(sourceID);

            //CallCallback();
            //return;
        }
        else
        {
            startPos = viewSource.transform.position;
        }

        ViewTarget destinationTarget = View.Instance.GetViewTargetByID(destinationID);
        if (destinationTarget is ViewPlayer)
        {
            endPos = OfferingHandler.Instance.GetOfferingPosition(owner, offeringType);
        }
        else if (destinationTarget is ViewRitual)
        {
            endPos = destinationTarget.transform.position;
        }


		int remaining = Mathf.Max(1, amount);
		for (int i = 0; i < remaining; i++)
		{
			GameObject obj = View.Instance.MakeNewOffering(offeringType);
			obj.transform.position = startPos;

			Vector3 toEnd = endPos - startPos;
			float distance = toEnd.magnitude;
			if (distance < 0.0001f)
			{
				// Degenerate case: immediately complete this one
				View.Instance.RemoveOffering(obj);
				remaining--;
				if (remaining == 0) Complete();
				continue;
			}

			Vector3 baseDir = toEnd.normalized;
			// Randomize the initial angle of the curve by rotating the initial tangent
			float angleDeg = UnityEngine.Random.Range(-60f, 60f);
			Vector3 rotatedDir = Quaternion.AngleAxis(angleDeg, new Vector3(0f, 0f, 1f)) * baseDir;
			float controlFactor = UnityEngine.Random.Range(0.35f, 0.65f);
			Vector3 ctrl = startPos + rotatedDir * (distance * controlFactor);

			Sequence seq = new Sequence();
			seq.Add(new Tween(p =>
			{
				// Ease-in parameter
				float t = p; float tEased = t * t;
				float omt = 1f - tEased;
				Vector3 pos = omt * omt * startPos + 2f * omt * tEased * ctrl + tEased * tEased * endPos;
				obj.transform.position = pos;
			}, 0f, 1f, duration));
			seq.Add(new SequenceAction(() =>
			{
				View.Instance.RemoveOffering(obj);
				remaining--;
				if (remaining == 0) Complete();
			}));
			seq.Start();
		}
    }

    private void TweenProgress(float progress)
    {
        // Ease-in (accelerate): quadratic ease-in
        float t = progress;
        float tEased = t * t;

        // Quadratic Bézier interpolation with eased parameter
        float oneMinusT = 1f - tEased;
        Vector3 pos = oneMinusT * oneMinusT * startPos + 2f * oneMinusT * tEased * controlPoint + tEased * tEased * endPos;
        offeringObject.transform.position = pos;
    }

    private void Complete()
    {
        //offeringObject.transform.position = endPos;
        //View.Instance.RemoveOffering(offeringObject);
        //Debug.LogWarning(summonFollowerAction.Follower.Owner.GetName() + " played " + summonFollowerAction.Follower.GetName() + " " + summonFollowerAction.Follower.ID + " Animation end");
        CallCallback();
    }
    protected override void Log()
    {
        Debug.LogWarning("CreateOfferingAnimation: Made " + amount + " " + offeringType.ToString());
    }
}
