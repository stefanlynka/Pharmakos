using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public static int starterSeed;
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

        starterSeed = Controller.GetRandomMetaSeed(); //UnityEngine.Random.Range(0, 1000);
        Debug.Log("MetaSeed: " + starterSeed);

        RNG = new CustomRandom(starterSeed);

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

        RNG = new CustomRandom(original.RNG);
        //int oldResult = original.RNG.Next(0, 2);
        //int newResult = RNG.Next(0, 2);

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

    public void DoEndOfTurnActions()
    {
        CurrentPlayer.DoEndOfTurnActions();
    }
    public void EndTurn()
    {
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

    public void ResetFrom(GameState original, bool simulated)
    {
        // Reset or reinitialize all fields to match the original GameState
        ActionHandler = new ActionHandler(simulated);
        IsSimulated = simulated;

        HighestTargetID = original.HighestTargetID;
        CurrentTeamID = original.CurrentTeamID;

        // Clear TargetsByID (they're repopulated later)
        TargetsByID.Clear();

        // Deep copy players and their effects/delayed effects
        if (Human == null)
            Human = original.Human.DeepCopy(this);
        else
            Human = original.Human.DeepCopy(this); // If you have a ResetFrom for Player, use it here

        if (AI == null)
            AI = original.AI.DeepCopy(this);
        else
            AI = original.AI.DeepCopy(this);

        // Deep copy effects after players
        original.Human.DeepCopyPlayerEffects(Human);
        original.AI.DeepCopyPlayerEffects(AI);

        original.Human.DeepCopyDelayedEffects(Human);
        original.AI.DeepCopyDelayedEffects(AI);

        RNG.SetSeed(original.RNG.CurrentSeed); // = new CustomRandom(original.RNG);

        FollowerDeathsThisTurn = original.FollowerDeathsThisTurn;
        LastFollowerThatDied = original.LastFollowerThatDied;
    }

    public void Cleanup()
    {
        // Null out references and clear collections to prepare for reuse
        ActionHandler = null;
        Human = null;
        AI = null;
        //RNG = null;
        LastFollowerThatDied = null;

        TargetsByID.Clear();

        FollowerEnters = null;
        FollowerDies = null;
        SpellPlayed = null;
        // Reset other fields as needed
        FollowerDeathsThisTurn = 0;
        CurrentTeamID = 0;
        HighestTargetID = 0;
        IsSimulated = false;
    }
}

public enum GameZone
{
    Deck,
    Hand,
    Discard,
    BattleRow,
    Offscreen,
    PlayZone,
}

public class GameStatePool
{
    private Stack<GameState> pool = new Stack<GameState>();

    public GameState Get(GameState original, bool simulated)
    {
        GameState state;
        if (pool.Count > 0)
        {
            state = pool.Pop();
            state.ResetFrom(original, simulated); // You must implement this
        }
        else
        {
            state = new GameState(original, simulated);
        }
        return state;
    }

    public void Return(GameState state)
    {
        state.Cleanup(); // Clear lists, null references, etc.
        pool.Push(state);
    }
}