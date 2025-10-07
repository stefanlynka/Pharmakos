using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionHandler
{
    public List<GameAction> ActionStack = new List<GameAction>();
    //public GameState GameState;
    public bool Simulated = false;
    private bool Evaluating = false;

    public ActionHandler(bool simulated = false)
    {
        Simulated = simulated;
        Evaluating = false;
    }

    public void AddAction(GameAction newAction, bool startEvaluating = true, bool frontOfQueue = false, bool forceStartEvaluating = false)
    {
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
        while (ActionStack.Count > 0)
        {
            GameAction newAction = ActionStack[0];
            ActionStack.RemoveAt(0);
            newAction.Execute(Simulated);
        }
        Evaluating = false;
    }

}


