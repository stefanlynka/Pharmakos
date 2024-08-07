using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.GraphicsBuffer;

public class Controller : MonoBehaviour
{
    public static Controller Instance;

    public View View;

    public GameState CanonGameState;

    public Player Player1 = null;
    public Player Player2 = null;

    public List<Card> PlayerDeckDefinition = new List<Card>();
    public Ritual PlayerMinorRitual = null;
    public Ritual PlayerMajorRitual = null;

    public int CurrentLevel = 0;

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

    public GameObject GameField;
    public CardGainRewardHandler CardGainRewardHandler;
    public RitualRewardHandler RitualRewardHandler;
    public CardRemovalRewardHandler CardRemovalRewardHandler;


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
        FirstTimeSetup();
    }

    private void Update()
    {
        if (Player1 == null || Player2 == null) return;

        Player1.RunUpdate();
        Player2.RunUpdate();
        View.Instance.DoUpdate();
    }
    private void FirstTimeSetup()
    {
        if (Player1 == null)
        {
            Player1 = new HumanPlayer();
            CardHandler.LoadPlayer(Player1, CardHandler.DeckName.PlayerStarterDeck);
            PlayerDeckDefinition = Player1.Deck;
            PlayerMinorRitual = Player1.MinorRitual;
            PlayerMajorRitual = Player1.MajorRitual;
        }

        LoadLevel();
    }
    private void LoadLevel()
    {
        CardGainRewardHandler.gameObject.SetActive(false);
        RitualRewardHandler.gameObject.SetActive(false);
        CardRemovalRewardHandler.gameObject.SetActive(false);
        GameField.SetActive(true);

        Player1 = new HumanPlayer();
        Player2 = new AIPlayer();
        CanonGameState = new GameState(Player1, Player2);
        Player1.AttachToGameState(CanonGameState);
        Player2.AttachToGameState(CanonGameState);

        List<Card> playerDeckCopy = new List<Card>(PlayerDeckDefinition);
        Player1.Deck = playerDeckCopy;
        Player1.MinorRitual = PlayerMinorRitual.MakeBaseCopy();
        Player1.MajorRitual = PlayerMajorRitual.MakeBaseCopy();
        Player1.Init(0);

        CardHandler.LoadPlayer(Player2, CardHandler.GetCurrentEnemyDeckName(CurrentLevel));
        Player2.LoadDeck(Player2.Deck);
        Player2.Init(1);


        View.Instance.Setup();

        CurrentPlayer.StartTurn();
    }

    public void ClearLevel()
    {
        Player1.Clear();
        Player2.Clear();

        View.Instance.Clear();
    }
    public void StartNextLevel()
    {
        CurrentLevel++;

        LoadLevel();

        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game);
    }
    public void TryEndTurn()
    {
        CanonGameState.EndTurn();
    }

    public Player GetOtherPlayer(Player player)
    {
        return player == Player1 ? Player2 : Player1;
    }

    public void RestartGame()
    {
        ScreenHandler.Instance.ShowScreen(ScreenName.Blank, true, false);
        ScreenHandler.Instance.ShowScreen(ScreenName.Start);
    }
    public void StartGame()
    {
        CurrentLevel = 0;

        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game);
    }
    private void GoToGameOverScreen()
    {
        ScreenHandler.Instance.ShowScreen(ScreenName.GameOver);
    }
    private void GoToCardRemovalRewardScreen()
    {
        GameField.SetActive(false);
        CardRemovalRewardHandler.gameObject.SetActive(true);
        CardRemovalRewardHandler.Load(PlayerDeckDefinition);
        //ScreenHandler.Instance.ShowScreen(ScreenName.Blank, true, false);
        ScreenHandler.Instance.ShowScreen(ScreenName.CardRemovalRewards);
    }
    private void GoToRitualRewardScreen()
    {
        GameField.SetActive(false);
        RitualRewardHandler.gameObject.SetActive(true);
        RitualRewardHandler.Load(CurrentLevel, PlayerMajorRitual, PlayerMinorRitual);
        ScreenHandler.Instance.ShowScreen(ScreenName.RitualRewards);
    }
    public void GoToCardGainRewardScreen()
    {
        GameField.SetActive(false);
        CardGainRewardHandler.gameObject.SetActive(true);
        CardGainRewardHandler.Load(CurrentLevel);
        ScreenHandler.Instance.ShowScreen(ScreenName.CardGainRewards);
    }

    public void CheckForPlayerDeath()
    {
        if (Player1.Health <= 0)
        {
            ClearLevel();

            GoToGameOverScreen();
            Player1.Health = 1;
        }
        else if (Player2.Health <= 0)
        {
            ClearLevel();

            GoToCardRemovalRewardScreen();
            //if (CurrentLevel % 2 == 0) GoToCardGainRewardScreen();
            //else GoToRitualRewardScreen();

            Player2.Health = 1;
        }
    }

    public void AddCardsToPlayerDeck(List<Card> cards)
    {
        PlayerDeckDefinition.AddRange(cards);
    }
    public void SetRituals(Ritual topRitual, Ritual bottomRitual)
    {
        PlayerMajorRitual = topRitual;
        PlayerMinorRitual = bottomRitual;
    }
    public void RemoveCardsFromPlayerDeck(List<Card> cards)
    {
        foreach (Card card in cards)
        {
            PlayerDeckDefinition.Remove(card);
        }
    }
}
