using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GameAction
{
    public ActionName ActionName;
    protected List<AnimationAction> AnimationActions = new List<AnimationAction>();

    public virtual void Execute(bool simulated = false)
    {

    }

    public void AddAnimationAction(AnimationAction action)
    {
        AnimationActions.Add(action);
    }
    public virtual List<AnimationAction> GetAnimationActions()
    {
        return AnimationActions;
    }

    public GameAction() { }
}
public enum ActionName
{
    Empty,
    Walk,
    Step,
    Extract,
    TryPush,
    Pushed,
    BasicPush,
    MediumPush,
    StrongPush,
    GrappleBeckon,
    GrappleAdvance,
    PulsePush,
    PulsePull,
    RotateClockwise,
    RotateCounterClockwise,
    CreateRock,
    CloisteredPush,
    MomentumPush
}

//public delegate void GameEffect(Body body, Node node, int amount = 0);
//public delegate List<Node> GetTargets(Body user, int amount = 0);