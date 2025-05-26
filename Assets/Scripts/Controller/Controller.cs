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

    public bool GameRunning = false;

    public Player Player1 = null;
    public Player Player2 = null;

    public List<Card> PlayerDeckDefinition = new List<Card>();
    public Ritual PlayerMinorRitual = null;
    public Ritual PlayerMajorRitual = null;

    public int PlayerStartingHealth = 0;

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
    public StarterBundleHandler StarterBundleHandler;
    public ProgressionHandler ProgressionHandler = new ProgressionHandler();


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
        if (Player1 == null || Player2 == null || !GameRunning) return;

        Player1.RunUpdate();
        Player2.RunUpdate();
        View.Instance.DoUpdate();
    }
    private void FirstTimeSetup()
    {
        CardHandler.LoadCards();

        if (Player1 == null)
        {
            Player1 = new HumanPlayer();
            ProgressionHandler.LoadPlayer(Player1, ProgressionHandler.DeckName.PlayerStarterDeck);
            PlayerStartingHealth = Player1.StartingHealth;
            PlayerDeckDefinition = Player1.DeckBlueprint;
            PlayerMinorRitual = Player1.MinorRitual;
            PlayerMajorRitual = Player1.MajorRitual;
        }

        Player1 = new HumanPlayer();
        Player2 = new AIPlayer();
        CanonGameState = new GameState(Player1, Player2);
        ProgressionHandler.Reset();
        ProgressionHandler.SetupNextEnemy();

        GoToStartScreen();
        //LoadLevel();
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
        Player1.DeckBlueprint = playerDeckCopy;
        Player1.StartingHealth = PlayerStartingHealth;
        if (PlayerMinorRitual != null) Player1.MinorRitual = PlayerMinorRitual.MakeBaseCopy();
        if (PlayerMajorRitual != null) Player1.MajorRitual = PlayerMajorRitual.MakeBaseCopy();
        Player1.Init(0);

        ProgressionHandler.LoadEnemy(Player2);
        //Player2.LoadDeck(Player2.DeckBlueprint);
        Player2.Init(1);


        View.Instance.Setup();

        GameRunning = true;

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
        ProgressionHandler.SetupNextEnemy();

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
    public void GoToStartScreen()
    {
        ScreenHandler.Instance.ShowScreen(ScreenName.Blank, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.Start);
    }
    public void StartGame()
    {
        //ProgressionHandler.CurrentLevel = 0;

        ScreenHandler.Instance.HideScreen(ScreenName.StarterBundle);
        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game);

        LoadLevel();
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
        RitualRewardHandler.Load(ProgressionHandler.CurrentLevel, PlayerMajorRitual, PlayerMinorRitual);
        ScreenHandler.Instance.ShowScreen(ScreenName.RitualRewards);
    }
    public void GoToCardGainRewardScreen()
    {
        GameField.SetActive(false);
        CardGainRewardHandler.gameObject.SetActive(true);
        CardGainRewardHandler.Load(ProgressionHandler.CurrentLevel);
        ScreenHandler.Instance.ShowScreen(ScreenName.CardGainRewards);
    }

    public void GoToStarterBundleScreen()
    {
        GameField.SetActive(false);
        StarterBundleHandler.gameObject.SetActive(true);
        StarterBundleHandler.Load();
        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.StarterBundle);
    }
    public void HideStarterBundleScreen()
    {
        ScreenHandler.Instance.HideScreen(ScreenName.StarterBundle);
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

            //GoToCardRemovalRewardScreen();
            if (ProgressionHandler.CurrentLevel % 2 != 0) GoToCardGainRewardScreen();
            else GoToRitualRewardScreen();

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
