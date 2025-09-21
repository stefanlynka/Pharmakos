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
        public bool Stackable;
        public AnimationQueueItem(AnimationAction animationAction, bool stackable)
        {
            AnimationAction = animationAction;
            Stackable = stackable;
        }
    }
    public List<AnimationQueueItem> AnimationActionQueue = new List<AnimationQueueItem>();
    public static bool IsAnimating { get { return ActiveAnimationCount > 0; } }
    public static int ActiveAnimationCount = 0;

    public bool CurrentAnimationIsStackable = false;

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
        }
    }



    private void StartPerforming()
    {
        // If a non-stackable animation is already playing and our next animation isn't stackable, early out
        bool nextActionIsStackable = AnimationActionQueue.Count > 0 && AnimationActionQueue[0].Stackable;
        if (IsAnimating && (!nextActionIsStackable || !CurrentAnimationIsStackable)) return;

        //Debug.LogError("Start evaluating the first of " + AnimationActionQueue.Count + " animations");
        if (AnimationActionQueue.Count == 0)
        {
            Controller.Instance.CheckForPlayerDeath();
            return;
        }

        AnimationQueueItem animationStackItem = AnimationActionQueue[0];
        AnimationAction newAction = animationStackItem.AnimationAction;
        AnimationActionQueue.RemoveAt(0);
        ActiveAnimationCount++;
        
        if (animationStackItem.Stackable)
        {
            CurrentAnimationIsStackable = true;
        }

        //Debug.LogError("Play: " + newAction.GetType().Name);
        newAction.Play(AnimationComplete);
    }
    public void AnimationComplete()
    {
        //Debug.LogError("Animation Complete");
        ActiveAnimationCount--;
        //IsAnimating = false;
        if (ActiveAnimationCount <= 0) CurrentAnimationIsStackable = false;

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

            AnimationQueueItem animationStackItem = new AnimationQueueItem(animationAction, animationAction.Stackable);
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

    public void AddAnimationActionToQueue(AnimationAction animationAction, bool immediate = false)
    {
        AnimationQueueItem animationStackItem = new AnimationQueueItem(animationAction, immediate);
        if (immediate)
        {
            AnimationActionQueue.Insert(0, animationStackItem);
        }
        else
        {
            AnimationActionQueue.Add(animationStackItem);
        }
    }

    public bool AnimationsComplete()
    {
        return (!IsAnimating && AnimationActionQueue.Count == 0);
    }

}