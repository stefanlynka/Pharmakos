using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    //
    // Deep copied
    //
    public ActionHandler ActionHandler = new ActionHandler();
    public Player Human;
    public Player AI;
    public int CurrentTeamID = 0; // Human:0   AI:1
    public int HighestTargetID {  get; private set; }
    public Dictionary<int, ITarget> TargetsByID = new Dictionary<int, ITarget>();

    public void FireFollowerEnters(Follower follower) { if (FollowerEnters != null) FollowerEnters(follower); }
    public Action<Follower> FollowerEnters;
    public void FireFollowerDies(Follower follower) { if (FollowerDies != null) FollowerDies(follower); }
    public Action<Follower> FollowerDies;
    public void FireSpellPlayed(Spell spell) { if (SpellPlayed != null) SpellPlayed(spell); }
    public Action<Spell> SpellPlayed;

    public Follower LastFollowerThatDied = null;
    public int FollowerDeathsThisTurn = 0;
    public bool IsSimulated = false;

    public CustomRandom RNG;

    //
    // Not Deep Copied
    //

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

        RNG = new CustomRandom(1);

        //ActionManager.GameState = this;
    }

    // Deep Copy
    public GameState(GameState original, bool simulated)
    {
        ActionHandler = new ActionHandler(simulated);
        IsSimulated = simulated;

        HighestTargetID = original.HighestTargetID;
        CurrentTeamID = original.CurrentTeamID;

        Human = original.Human.DeepCopy(this);
        AI = original.AI.DeepCopy(this);

        // Deep Copy Effects /after/ Deep Copying Players so all targets are added to the GameState
        original.Human.DeepCopyPlayerEffects(Human);
        original.AI.DeepCopyPlayerEffects(AI);

        original.Human.DeepCopyDelayedEffects(Human);
        original.AI.DeepCopyDelayedEffects(AI);

        RNG = new CustomRandom(original.RNG.GetCurrentState());

        FollowerDeathsThisTurn = original.FollowerDeathsThisTurn;
        LastFollowerThatDied = original.LastFollowerThatDied;
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

    public T GetTargetByID<T>(int ID) where T : class, ITarget
    {
        if (!TargetsByID.TryGetValue(ID, out ITarget target))
            return null;

        return target as T;
    }

}

public enum GameZone
{
    Deck,
    Hand,
    Discard,
    BattleRow,
    Offscreen,
}