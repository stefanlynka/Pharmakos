using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionHandler
{
    public List<GameAction> ActionStack = new List<GameAction>();
    //public GameState GameState;
    public bool Simulated = false;
    private bool Evaluating = false;

    // Play history (canon game only). Actions added while a trigger scope is open belong to that trigger's record;
    // otherwise they inherit the record of the action that is executing when they are added.
    private readonly Stack<TriggeredEffectPlayHistory> triggerScopes = new Stack<TriggeredEffectPlayHistory>();
    private GameAction executingAction;
    private int evaluationDepth;

    public ActionHandler(bool simulated = false)
    {
        Simulated = simulated;
        Evaluating = false;
    }

    public void AddAction(GameAction newAction, bool startEvaluating = true, bool frontOfQueue = false, bool forceStartEvaluating = false)
    {
        if (!Simulated && newAction.HistoryRecord == null)
        {
            if (triggerScopes.Count > 0)
                newAction.HistoryRecord = triggerScopes.Peek();
            else if (executingAction != null)
                newAction.HistoryRecord = executingAction.HistoryRecord;
        }

        if (frontOfQueue)
        {
            ActionStack.Insert(0, newAction);
        }
        else
        {
            ActionStack.Add(newAction);
        }

        if (startEvaluating && (!Evaluating || forceStartEvaluating)) StartEvaluating();
    }
    public void StartEvaluating()
    {
        //Debug.LogError("Start evaluating " + ActionStack.Count + " Actions");
        Evaluating = true;
        PlayHistoryHandler history = Simulated ? null : Controller.Instance?.PlayHistoryHandler;
        if (history != null && evaluationDepth == 0)
            history.BeginEvaluationChain();
        evaluationDepth++;
        try
        {
            while (ActionStack.Count > 0)
            {
                GameAction newAction = ActionStack[0];
                ActionStack.RemoveAt(0);

                if (history != null)
                {
                    // A trinket's actions belong to the trinket, even when a follower effect it granted made them.
                    if (newAction.SourceEffect != null)
                        newAction.HistoryRecord = history.GetEffectRecord(newAction.SourceEffect);
                    if (newAction.AddsPlayHistoryEntry)
                        history.ExpectEntry();
                }

                GameAction previous = executingAction;
                executingAction = newAction;
                try
                {
                    newAction.Execute(Simulated);
                }
                finally
                {
                    executingAction = previous;
                }
            }
        }
        finally
        {
            // A throwing action must not leave Evaluating set, or later actions (like ending the turn) never run.
            Evaluating = false;
            evaluationDepth--;
            if (history != null && evaluationDepth == 0)
                history.EndEvaluationChain();
        }
    }

    /// <summary>
    /// Opens a play history scope for a follower's triggered ability: actions added until it is disposed belong to
    /// one entry. Does nothing in simulations. Use with <c>using</c>.
    /// </summary>
    public TriggerHistoryScope BeginTrigger(Follower source, EffectTrigger trigger, bool viaAlly = false)
    {
        if (Simulated || source == null || Controller.Instance == null)
            return default;

        TriggeredEffectPlayHistory record = new TriggeredEffectPlayHistory(source, trigger, viaAlly);
        triggerScopes.Push(record);
        return new TriggerHistoryScope(this);
    }

    internal void EndTrigger()
    {
        if (triggerScopes.Count > 0)
            triggerScopes.Pop();
    }

    public void ClearStack()
    {
        ActionStack.Clear();
    }
}



public struct TriggerHistoryScope : System.IDisposable
{
    readonly ActionHandler handler;

    public TriggerHistoryScope(ActionHandler handler)
    {
        this.handler = handler;
    }

    public void Dispose()
    {
        handler?.EndTrigger();
    }
}
