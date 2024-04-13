using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Controller : MonoBehaviour
{
    public static Controller Instance;

    public View View;

    public Player Player1 = null;
    public Player Player2 = null;

    public Player CurrentPlayer = null;
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

        StartNewTurn();
    }

    private void Update()
    {
        View.Instance.DoUpdate();
    }
    private void Setup()
    {
        //ViewEventHandler.Instance.ViewTargetInHandClicked -= CardInHandClicked;
        //ViewEventHandler.Instance.ViewTargetInHandClicked += CardInHandClicked;

        if (Player1 == null)
        {
            Player1 = new Player();
            Player1.IsHuman = true;
            Player1.Name = "Human";
            Player1.Init(CardManager.GetDeck(CardManager.DeckNames.Warriors));
            //for (int i = 0; i < View.PlayerCreaturePositions.Length; i++)
            //{
            //    View.PlayerCreaturePositions[i].CreaturePosition = Player1.CreaturePositions[i];
            //}
            //Player1.ManaPool.IncreaseMaxMana(Colour.Red, 2);
            //Player1.ManaPool.IncreaseMaxMana(Colour.Blue, 2);

            Player2 = new AIPlayer();
            Player2.IsHuman = false;
            Player2.Name = "AI";
            Player2.Init(CardManager.GetDeck(CardManager.DeckNames.Monsters));

            Follower follower1 = new Hoplite();
            follower1.Init(Player2);
            Player2.SummonFollower(follower1, 0);
            Follower follower2 = new Chariot();
            follower2.Init(Player2);
            Player2.SummonFollower(follower2, 0);
            Follower follower3 = new Sphinx();
            follower3.Init(Player2);
            Player2.SummonFollower(follower3, 0);
            //Player2.BattleRow.Followers.Insert(0, follower1);
            //Follower follower2 = new Hoplite();
            //Player2.BattleRow.Followers.Insert(0, follower2);
            //Follower follower3 = new Hoplite();
            //Player2.BattleRow.Followers.Insert(0, follower3);
            //for (int i = 0; i < View.AICreaturePositions.Length; i++)
            //{
            //    View.AICreaturePositions[i].CreaturePosition = Player2.CreaturePositions[i];
            //}

            CurrentPlayer = Player1;
        }

        View.Instance.Setup();
    }

    public void TryEndTurn()
    {
        TriggerEndOfTurnEffects();
        CurrentPlayer = CurrentPlayer == Player1 ? Player2 : Player1;
        StartNewTurn();
    }

    private void TriggerEndOfTurnEffects()
    {
        CurrentPlayer.EndTurn();
    }
    private void StartNewTurn()
    {
        CurrentPlayer.DrawHand();
        CurrentPlayer.StartTurn();
    }

    

    public Player GetOtherPlayer(Player player)
    {
        return player == Player1 ? Player2 : Player1;
    }
}
