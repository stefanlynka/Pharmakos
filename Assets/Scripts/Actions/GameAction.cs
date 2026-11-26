using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameAction
{
    public ActionName ActionName;
    public StaticPlayerEffect SourceEffect;
    // Play history, canon game only (ActionHandler stamps it; it stays null in AI simulations): the triggered
    // ability, trinket or ritual buff this action resolves for. Copies share it, but simulated copies never read it.
    public TriggeredEffectPlayHistory HistoryRecord;

    /// <summary>True for plays, attacks and rituals, which add an entry of their own (see MakePlayHistoryItem).</summary>
    public virtual bool AddsPlayHistoryEntry => false;
    //protected List<AnimationAction> AnimationActions = new List<AnimationAction>();

    public virtual void Execute(bool simulated = false, bool successful = true)
    {
        if (!simulated) 
        {
            if (successful)
            {
                // An opponent's turn resolves every action before its animations play.
                // The source effect flares when that animation plays, so a trinket flashes
                // with the effect instead of at the start of the turn.
                View.Instance.AnimationHandler.AddGameActionToQueue(this);
            }

            if (Controller.ActionDebugMode)
            {
                LogAction();
                if (!successful) Debug.LogWarning("This Action Failed");
            }

            AddToPlayHistory();
            if (successful)
                AddToTriggeredRecord();
        }
    }

    public virtual void LogAction()
    {

    }

    private void AddToPlayHistory()
    {
        PlayHistoryItem playHistoryItem = MakePlayHistoryItem();
        if (playHistoryItem != null) Controller.Instance.PlayHistoryHandler.AddPlayHistoryEntry(playHistoryItem);
    }

    public virtual PlayHistoryItem MakePlayHistoryItem()
    {
        return null;
    }

    private void AddToTriggeredRecord()
    {
        if (HistoryRecord == null || Controller.Instance == null)
            return;

        AddPlayHistoryResults(HistoryRecord);
        Controller.Instance.PlayHistoryHandler.AddTriggeredRecord(HistoryRecord);
    }

    /// <summary>What this action hit, changed or made, for the triggered-effect entry it belongs to.</summary>
    public virtual void AddPlayHistoryResults(TriggeredEffectPlayHistory record)
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