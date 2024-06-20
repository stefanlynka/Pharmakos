using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler
{
    public static AnimationHandler instance;

    public struct AnimationQueueItem
    {
        public AnimationAction AnimationAction;
        public bool Immediate;
        public AnimationQueueItem(AnimationAction animationAction, bool immediate)
        {
            AnimationAction = animationAction;
            Immediate = immediate;
        }
    }
    public List<AnimationQueueItem> AnimationActionQueue = new List<AnimationQueueItem>();
    public static bool IsAnimating = false;

    // TODO: Check player lose state when this is called
    public Action OnAnimationFinish;

    public bool HasPlayerLost = false;

    public Action OnAllAnimationsFinished;



    public void UpdateAnimations()
    {
        StartPerforming();


        if (AnimationsComplete())
        {
            DoAnimationCleanup();

            if (HasPlayerLost)
            {
                PlayerLost();
            }
        }
    }



    private void StartPerforming()
    {
        if (IsAnimating) return;

        //Debug.LogError("Start evaluating the first of " + AnimationActionQueue.Count + " animations");
        if (AnimationActionQueue.Count == 0)
        {
            return;
        }

        AnimationQueueItem animationStackItem = AnimationActionQueue[0];
        AnimationAction newAction = animationStackItem.AnimationAction;
        AnimationActionQueue.RemoveAt(0);
        IsAnimating = !animationStackItem.Immediate;
        if (animationStackItem.Immediate)
        {
            //Debug.LogError("Play: " + newAction.GetType().Name);
            newAction.Play(null);
        }
        else
        {
            //Debug.LogError("Play: " + newAction.GetType().Name);
            newAction.Play(AnimationComplete);
        }
    }
    public void AnimationComplete()
    {
        //Debug.LogError("Animation Complete");
        IsAnimating = false;

        OnAnimationFinish?.Invoke();
    }

    private void DoAnimationCleanup()
    {
        OnAllAnimationsFinished?.Invoke();
        OnAllAnimationsFinished = null;
    }

    public void AddGameActionToQueue(GameAction gameAction, bool backOfQueue = true)
    {
        //int index = backOfQueue ? AnimationActionQueue.Count : 0;
        List<AnimationAction> animationActions = gameAction.GetAnimationActions();
        if (!backOfQueue) animationActions.Reverse();

        foreach (AnimationAction animationAction in animationActions)
        {
            if (animationAction == null) continue;

            AnimationQueueItem animationStackItem = new AnimationQueueItem(animationAction, animationAction.Simultaneous);
            if (backOfQueue)
            {
                AnimationActionQueue.Add(animationStackItem);
            }
            else
            {
                AnimationActionQueue.Insert(0, animationStackItem);
            }
            //AnimationActionQueue.Insert(index, animationStackItem);
        }

    }
    public bool AnimationsComplete()
    {
        return (!IsAnimating && AnimationActionQueue.Count == 0);
    }

    public void PlayerLost()
    {
        //Controller.Instance..instance.DoPlayerLoses();
        Debug.LogError("Player Lost");
        HasPlayerLost = false;
    }
}