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

    // Presentation (canon game only): each open trigger scope has a cue (flash + sound) that its direct actions carry.
    // A closed cue that acted waits here for the action that caused it to finish animating (see TriggerCue.Close).
    private readonly Stack<TriggerCue> cueScopes = new Stack<TriggerCue>();
    private readonly List<TriggerCue> waitingCues = new List<TriggerCue>();

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

        if (!Simulated && newAction.Cue == null && cueScopes.Count > 0)
            cueScopes.Peek().AddAction(newAction);

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
                    if (!Simulated)
                    {
                        newAction.Cue?.ActionExecuted();
                        ReleaseCuesCausedBy(newAction);
                    }
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
    public TriggerHistoryScope BeginTrigger(Follower source, EffectTrigger trigger, bool viaAlly = false, string effectDefName = null)
    {
        if (Simulated || source == null || Controller.Instance == null)
            return default;

        TriggeredEffectPlayHistory record = new TriggeredEffectPlayHistory(source, trigger, viaAlly);
        triggerScopes.Push(record);
        OpenCue(source, trigger.ToString(), viaAlly, effectDefName);
        return new TriggerHistoryScope(this, history: true, cue: true);
    }

    /// <summary>
    /// Opens a presentation-only scope for an ability that has no play history trigger (start / end of turn
    /// effects): if it does something, its follower flashes and its cue sound plays. Does nothing in simulations.
    /// </summary>
    public TriggerHistoryScope BeginCue(Follower source, string trigger)
    {
        if (Simulated || source == null || Controller.Instance == null)
            return default;

        OpenCue(source, trigger, false, null);
        return new TriggerHistoryScope(this, history: false, cue: true);
    }

    internal void EndTrigger(bool history, bool cue)
    {
        if (history && triggerScopes.Count > 0)
            triggerScopes.Pop();
        if (cue && cueScopes.Count > 0)
            CloseCue(cueScopes.Pop());
    }

    void OpenCue(Follower source, string trigger, bool viaAlly, string effectDefName)
    {
        TriggerCue cue = new TriggerCue(source, trigger, viaAlly, executingAction, effectDefName);
        cueScopes.Push(cue);
        cue.Open();
    }

    void CloseCue(TriggerCue cue)
    {
        if (cue.Close())
            waitingCues.Add(cue);
    }

    // The action that fired these cues has executed (its animations are queued): place any still unplaced after them.
    void ReleaseCuesCausedBy(GameAction action)
    {
        for (int i = waitingCues.Count - 1; i >= 0; i--)
        {
            TriggerCue cue = waitingCues[i];
            if (cue.CausingAction != action && !cue.Placed)
                continue;

            waitingCues.RemoveAt(i);
            cue.PlaceNow();
        }
    }

    /// <summary>
    /// The open trigger did something without adding an action now (e.g. stored a reward for next turn), so its
    /// cue should still play.
    /// </summary>
    public void MarkTriggerActed()
    {
        if (!Simulated && cueScopes.Count > 0)
            cueScopes.Peek().MarkActed();
    }

    public void ClearStack()
    {
        ActionStack.Clear();
        waitingCues.Clear();
    }
}



public struct TriggerHistoryScope : System.IDisposable
{
    readonly ActionHandler handler;
    readonly bool history;
    readonly bool cue;

    public TriggerHistoryScope(ActionHandler handler, bool history = true, bool cue = false)
    {
        this.handler = handler;
        this.history = history;
        this.cue = cue;
    }

    public void Dispose()
    {
        handler?.EndTrigger(history, cue);
    }
}
