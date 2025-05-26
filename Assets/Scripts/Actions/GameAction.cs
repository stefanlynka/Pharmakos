using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class GameAction
{
    public ActionName ActionName;
    //protected List<AnimationAction> AnimationActions = new List<AnimationAction>();

    public virtual void Execute(bool simulated = false)
    {
        if (!simulated) 
        {
            View.Instance.AnimationHandler.AddGameActionToQueue(this);

            LogAction();
        }
    }

    public virtual void LogAction()
    {

    }

    //public void AddAnimationAction(AnimationAction action)
    //{
    //    AnimationActions.Add(action);
    //}
    public virtual List<AnimationAction> GetAnimationActions()
    {
        return new List<AnimationAction>();
    }

    public GameAction() { }

    public abstract GameAction DeepCopy(Player newOwner);
    //{
    //    GameAction copy = (GameAction)MemberwiseClone();

    //    return copy;
    //}
}
public enum ActionName
{
    Empty
}

//public delegate void GameEffect(Body body, Node node, int amount = 0);
//public delegate List<Node> GetTargets(Body user, int amount = 0);