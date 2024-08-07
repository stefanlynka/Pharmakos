using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionHandler
{
    public Stack<GameAction> ActionStack = new Stack<GameAction>();
    //public GameState GameState;
    public bool Simulated = false;

    public ActionHandler(bool simulated = false)
    {
        Simulated = simulated;
    }

    public void AddAction(GameAction newAction)
    {
        ActionStack.Push(newAction);
    }
    public void StartEvaluating()
    {
        //Debug.LogError("Start evaluating " + ActionStack.Count + " Actions");
        while (ActionStack.Count > 0)
        {
            GameAction newAction = ActionStack.Pop();
            newAction.Execute(Simulated);
        }
    }

}


