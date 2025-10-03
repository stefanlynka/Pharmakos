using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;


public class Controller : MonoBehaviour
{
    public static Controller Instance;

    public static int starterSeed = 1;
    public bool gamePaused = false;

    public static bool DebugMode = false;
    public static bool ShowCardIDs = false;


    public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

    public View View;

    public GameState CanonGameState;

    public bool GameRunning = false;
    public bool IsTestChamber = false;
    private bool isGameSetup = false;

    public CustomRandom MetaRNG = new CustomRandom(2);

    public Player Player1 = null;
    public Player Player2 = null;

    public List<Card> PlayerDeckDefinition = new List<Card>();
    public List<Follower> PlayerStartingBattleRow = new List<Follower>();
    public Ritual PlayerMinorRitual = null;
    public Ritual PlayerMajorRitual = null;
    public int PlayerCardsPerTurn = 5;

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
    public TutorialHandler TutorialHandler;
    public ProgressionHandler ProgressionHandler = new ProgressionHandler();
    public ViewCardScroller DeckViewer;

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

        starterSeed = UnityEngine.Random.Range(0, 1000);
        Debug.Log("MetaSeed: " + starterSeed);

        MetaRNG = new CustomRandom(starterSeed);
    }

    private void Start()
    {
        //FirstTimeSetup();
        GoToStartScreen();
    }

    private void Update()
    {
        if (Player1 == null || Player2 == null || !GameRunning) return;

        Player1.RunUpdate();
        Player2.RunUpdate();
        View.Instance.DoUpdate();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gamePaused)
            {
                UnPauseGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    void OnDisable() // Called when exiting Play Mode
    {
        AlertCancelToken();
    }
    void OnApplicationQuit()
    {
        AlertCancelToken();
    }
    private void AlertCancelToken()
    {
        if (CancellationTokenSource != null)
        {
            CancellationTokenSource.Cancel(); // Request cancellation
            CancellationTokenSource.Dispose();
            CancellationTokenSource = null;
        }
    }

    private void FirstTimeSetup()
    {
        if (isGameSetup) return;

        CardHandler.LoadCards();

        // This should be changed. We shouldn't save "PlayerStartingHealth" we should be saving PlayerDetails which stores everything
        if (Player1 == null)
        {
            Player1 = new HumanPlayer();
            ProgressionHandler.DeckName playerDeckName = IsTestChamber ? ProgressionHandler.DeckName.TestPlayer : ProgressionHandler.DeckName.PlayerStarterDeck;
            ProgressionHandler.LoadPlayer(Player1, playerDeckName);
            PlayerStartingHealth = Player1.StartingHealth;
            PlayerDeckDefinition = Player1.DeckBlueprint;
            PlayerStartingBattleRow = ProgressionHandler.GetPlayerStartingFollowers(playerDeckName);
            PlayerMinorRitual = Player1.MinorRitual;
            PlayerMajorRitual = Player1.MajorRitual;
            PlayerCardsPerTurn = Player1.CardsPerTurn;
        }

        Player1 = new HumanPlayer();
        Player2 = new AIPlayer();
        CanonGameState = new GameState(Player1, Player2);
        ProgressionHandler = new ProgressionHandler();
        //ProgressionHandler.Reset();
        ProgressionHandler.SetupNextEnemy(IsTestChamber);

        //GoToStartScreen();
        //LoadLevel();
        isGameSetup = true;
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
        //Player1.PlayerDetails.StartingBattleRow = PlayerDeckDefinition.
        Player1.DeckBlueprint = playerDeckCopy;
        Player1.StartingHealth = ProgressionHandler.GetPlayerHealth();
        Player1.CardsPerTurn = PlayerCardsPerTurn;
        Player1.PlayerDetails.StartingBattleRow = PlayerStartingBattleRow;
        if (PlayerMinorRitual != null) Player1.MinorRitual = PlayerMinorRitual.MakeBaseCopy();
        if (PlayerMajorRitual != null) Player1.MajorRitual = PlayerMajorRitual.MakeBaseCopy();
        Player1.Init(0);

        ProgressionHandler.LoadEnemy(Player2);
        //Player2.LoadDeck(Player2.DeckBlueprint);
        Player2.Init(1);

        View.Instance.Setup();

        // Load Starting BattleRow
        LoadStartingBattleRow();

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        //ClearLevel();
        //isGameSetup = false;
        //FirstTimeSetup();
        //gamePaused = false;
        //ScreenHandler.Instance.ShowScreen(ScreenName.Blank, true, false);
        //ScreenHandler.Instance.ShowScreen(ScreenName.Start);
    }
    public void GoToStartScreen()
    {
        ScreenHandler.Instance.ShowScreen(ScreenName.Blank, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.Start);
    }
    public void StartGame()
    {
        //ProgressionHandler.CurrentLevel = 0;

        FirstTimeSetup();

        ScreenHandler.Instance.HideScreen(ScreenName.StarterBundle);
        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game);

        LoadLevel();
    }
    public void StartTestChamber()
    {
        IsTestChamber = true;

        StartGame();
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
    public void GoToRitualRewardScreen()
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
        FirstTimeSetup();

        GameField.SetActive(false);
        StarterBundleHandler.gameObject.SetActive(true);
        StarterBundleHandler.Load();
        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.StarterBundle);
        ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, false, false);
    }

    public void PauseGame()
    {
        gamePaused = true;
        GameField.SetActive(false);
        ScreenHandler.Instance.ShowScreen(ScreenName.Pause);
    }
    public void UnPauseGame()
    {
        gamePaused = false;
        GameField.SetActive(true);
        ScreenHandler.Instance.HideScreen(ScreenName.Pause);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game);
    }
    public void QuitGame()
    {
#if UNITY_STANDALONE
        Application.Quit();
#endif
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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

            if (ProgressionHandler.CurrentLevel >= 10)
            {
                ScreenHandler.Instance.ShowScreen(ScreenName.Success);
            }
            else if (ProgressionHandler.CurrentLevel % 2 != 0)
            {
                GoToCardGainRewardScreen();
            }
            else
            {
                GoToCardRemovalRewardScreen();
            }

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

    private void LoadStartingBattleRow()
    {
        foreach (Follower follower in Player1.PlayerDetails.StartingBattleRow)
        {
            int index = Player1.BattleRow.Followers.Count;
            follower.Init(Player1);
            //follower.Owner.SummonFollower(follower, index, false);
            GameAction newAction = new SummonFollowerAction(follower, index, false);
            Player1.GameState.ActionHandler.AddAction(newAction);
        }

        foreach (Follower follower in Player2.PlayerDetails.StartingBattleRow)
        {
            int index = Player2.BattleRow.Followers.Count;
            follower.Init(Player2);
            //follower.Owner.SummonFollower(follower, index, false);
            GameAction newAction = new SummonFollowerAction(follower, index, false);
            Player2.GameState.ActionHandler.AddAction(newAction);
        }
        
    }

    public void LoadTutorial()
    {
        ScreenHandler.Instance.ShowScreen(ScreenName.Tutorial);
        TutorialHandler.Load();
    }
    public void HideTutorial()
    {
        ScreenHandler.Instance.HideScreen(ScreenName.Tutorial);
        ScreenHandler.Instance.ShowScreen(ScreenName.Start);
    }
    public void ToggleDeckViewer()
    {
        if (DeckViewer.gameObject.activeSelf)
        {
            HideDeckViewer();
        }
        else
        {
            LoadDeckViewer();
        }
    }
    public void LoadDeckViewer()
    {
        DeckViewer.gameObject.SetActive(true);
        List<Card> playerDeck = new List<Card>(PlayerDeckDefinition);

        // Sort by Gold cost, then by type (Followers before Spells)
        playerDeck.Sort((a, b) =>
        {
            int costCompare = a.Costs[OfferingType.Gold].CompareTo(b.Costs[OfferingType.Gold]);
            if (costCompare != 0)
                return costCompare;

            // Followers before Spells
            bool aIsFollower = a is Follower;
            bool bIsFollower = b is Follower;
            if (aIsFollower && !bIsFollower) return -1;
            if (!aIsFollower && bIsFollower) return 1;
            return 0;
        });
        DeckViewer.Load(playerDeck);
    }
    public void HideDeckViewer()
    {
        DeckViewer.gameObject.SetActive(false);
    }
}
