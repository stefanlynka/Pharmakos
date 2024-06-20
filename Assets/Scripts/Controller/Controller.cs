using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Controller : MonoBehaviour
{
    public static Controller Instance;

    public View View;

    public GameState CanonGameState;

    public Player Player1 = null;
    public Player Player2 = null;

    public Player CurrentPlayer
    {
        get
        {
            if (CanonGameState == null) return null;
            return CanonGameState.CurrentPlayer;
        }
    }
    public Player OtherPlayer
    {
        get
        {
            return CurrentPlayer == Player1 ? Player2 : Player1;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        Setup();

        CurrentPlayer.StartTurn();
    }

    private void Update()
    {
        if (Player1 == null || Player2 == null) return;

        Player1.RunUpdate();
        Player2.RunUpdate();
        View.Instance.DoUpdate();
    }
    private void Setup()
    {
        if (Player1 == null)
        {
            Player1 = new Player();
            Player2 = new AIPlayer();
            CanonGameState = new GameState(Player1, Player2);

            Player1.IsHuman = true;
            Player1.Name = "Human";
            Player1.Init(CanonGameState, CardManager.GetDeck(CardManager.DeckNames.Warriors), 0);
            Player1.MinorRitual = new ZeusMinor(Player1);
            Player1.MinorRitual.Init();
            Player1.MajorRitual = new ZeusMajor(Player1);
            Player1.MajorRitual.Init();


            Player2.IsHuman = false;
            Player2.Name = "AI";
            Player2.Init(CanonGameState, CardManager.GetDeck(CardManager.DeckNames.Monsters), 1);
            Player2.MinorRitual = new ZeusMinor(Player2);
            Player2.MinorRitual.Init();
            Player2.MajorRitual = new ZeusMajor(Player2);
            Player2.MajorRitual.Init();

            //Follower follower1 = new Hoplite();
            //follower1.Init(Player2);
            //Player2.SummonFollower(follower1, 0, false);
            //Follower follower2 = new Chariot();
            //follower2.Init(Player2);
            //Player2.SummonFollower(follower2, 0, false);
            //Follower follower3 = new Sphinx();
            //follower3.Init(Player2);
            //Player2.SummonFollower(follower3, 0, false);
        }

        View.Instance.Setup();
    }

    public void TryEndTurn()
    {
        CanonGameState.EndTurn();
    }

    public Player GetOtherPlayer(Player player)
    {
        return player == Player1 ? Player2 : Player1;
    }
}
