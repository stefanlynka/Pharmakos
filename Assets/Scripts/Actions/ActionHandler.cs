using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionHandler
{
    public Stack<GameAction> ActionStack = new Stack<GameAction>();
    //public GameState GameState;
    public bool Simulated = false;
    private bool Evaluating = false;

    public ActionHandler(bool simulated = false)
    {
        Simulated = simulated;
        Evaluating = false;
    }

    public void AddAction(GameAction newAction, bool startEvaluating = true, bool forceStartEvaluating = false)
    {
        ActionStack.Push(newAction);

        if (startEvaluating && (!Evaluating || forceStartEvaluating)) StartEvaluating();
    }
    public void StartEvaluating()
    {
        //Debug.LogError("Start evaluating " + ActionStack.Count + " Actions");
        Evaluating = true;
        while (ActionStack.Count > 0)
        {
            GameAction newAction = ActionStack.Pop();
            newAction.Execute(Simulated);
        }
        Evaluating = false;
    }

}


