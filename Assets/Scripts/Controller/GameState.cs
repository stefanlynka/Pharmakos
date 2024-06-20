using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    // Deep copied
    public ActionManager ActionManager = new ActionManager();
    public Player Human;
    public Player AI;
    public int CurrentTeamID = 1;
    public int HighestTargetID {  get; private set; }
    public Dictionary<int, ITarget> TargetsByID = new Dictionary<int, ITarget>();

    public bool IsSimulated = false;

    public Player CurrentPlayer { get
        {
            return CurrentTeamID == Human.PlayerID ? Human : AI;
        } 
    }


    public GameState()
    {

    }

    public GameState(Player human, Player ai)
    {
        Human = human;
        AI = ai;
        CurrentTeamID = 0;

        //ActionManager.GameState = this;
    }

    // Deep Copy
    public GameState(GameState original, bool simulated)
    {
        ActionManager = new ActionManager(simulated);
        IsSimulated = simulated;

        HighestTargetID = original.HighestTargetID;
        CurrentTeamID = original.CurrentTeamID;

        Human = original.Human.DeepCopy(this);
        AI = original.AI.DeepCopy(this);
    }

    public void TryAssignID(ITarget target)
    {
        int ID = HighestTargetID;
        HighestTargetID++;

        if (TargetsByID.ContainsKey(ID))
        {
            Debug.LogError("ID: " + ID + " is already assigned");
            return;
        }

        Card card = target as Card;
        if (card != null) card.ID = ID;

        Player player = target as Player;
        if (player != null) player.ITargetID = ID;

        Ritual ritual = target as Ritual;
        if (ritual != null) ritual.ID = ID;

        TargetsByID[ID] = target;
    }

    public void EndTurn()
    {
        CurrentPlayer.EndTurn();
        CurrentTeamID = Mathf.Abs(CurrentTeamID - 1);
        CurrentPlayer.StartTurn();
    }

    public Player GetPlayer(int ID)
    {
        return Human.PlayerID == ID ? Human : AI;
    }
    public Player GetOtherPlayer(int ID)
    {
        return Human.PlayerID == ID ? AI : Human;
    }
}
