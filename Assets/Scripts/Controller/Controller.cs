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
    public bool GamePaused = false;

    public static bool ActionDebugMode = true;
    public static bool AnimationDebugMode = false;
    public static bool AIDebugMode = true;
    public static bool ShowCardIDs = true;

    public static bool BlitzMode = true;


    public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

    public View View;

    public GameState CanonGameState;

    public bool GameRunning = false;
    public bool IsTestChamber = false;
    private bool isGameSetup = false;

    public CustomRandom MetaRNG = new CustomRandom(2);

    public Player Player1 = null;
    public Player Player2 = null;

    public PlayerDetails HumanPlayerDetails;

    private ScreenName CurrentScreen = ScreenName.Blank;

    public List<Follower> SacrificedFollowers = new List<Follower>();

    //public List<Card> PlayerDeckDefinition = new List<Card>();
    //public List<Follower> PlayerStartingBattleRow = new List<Follower>();
    //public Ritual PlayerMinorRitual = null;
    //public Ritual PlayerMajorRitual = null;
    //public int PlayerCardsPerTurn = 5;
    //public int PlayerStartingHealth = 0;



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
    public ProgressionHandler ProgressionHandler;
    public DeckViewer DeckViewer;
    public TextHandler TextHandler = new TextHandler();
    public PlayHistoryHandler PlayHistoryHandler = new PlayHistoryHandler();
    public ViewPlayHistoryHandler ViewPlayHistoryHandler;
    public TrinketRewardHandler TrinketRewardHandler;

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

        starterSeed = GetRandomMetaSeed(); // UnityEngine.Random.Range(0, 1000);
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

        //Debug.LogError("Human Turn: " + CanonGameState.CurrentPlayer.IsHuman);
        Player1.RunUpdate();
        Player2.RunUpdate();
        View.Instance.DoUpdate();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GamePaused)
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

        ProgressionHandler = new ProgressionHandler();

        CardHandler.LoadCards();

        // This should be changed. We shouldn't save "PlayerStartingHealth" we should be saving PlayerDetails which stores everything
        if (Player1 == null)
        {
            Player1 = new HumanPlayer();
            ProgressionHandler.DeckName playerDeckName = IsTestChamber ? ProgressionHandler.DeckName.TestPlayer : ProgressionHandler.DeckName.PlayerStarterDeck;
            ProgressionHandler.LoadPlayer(Player1, playerDeckName);
            HumanPlayerDetails = Player1.PlayerDetails;

            //PlayerStartingHealth = Player1.StartingHealth;
            //PlayerDeckDefinition = Player1.DeckBlueprint;
            //PlayerStartingBattleRow = ProgressionHandler.GetPlayerStartingFollowers(playerDeckName);
            //PlayerMinorRitual = Player1.MinorRitual;
            //PlayerMajorRitual = Player1.MajorRitual;
            //PlayerCardsPerTurn = Player1.CardsPerTurn;
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

        Player1.LoadDetails(HumanPlayerDetails, 0);

        Player1.Init(0);

        ProgressionHandler.LoadEnemy(Player2);
        //Player2.LoadDeck(Player2.DeckBlueprint);
        Player2.Init(1);

        View.Instance.Setup();

        Player1.ApplyTrinketBuffs();
        Player2.ApplyTrinketBuffs();

        // Load Starting BattleRow
        LoadStartingBattleRow();

        GameRunning = true;

        Player1.DrawHand();
        Player2.DrawHand();
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
        PlayHistoryHandler.Clear();

        ProgressionHandler.SetupNextEnemy();

        LoadLevel();

        CurrentScreen = ScreenName.Game;

        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, false, false);
        ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, false, false);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game);
    }
    public void TryEndTurn()
    {
        if (CanonGameState.CurrentPlayer.IsHuman)
        {
            var endTurnAction = new TryEndTurnAction(CanonGameState.CurrentPlayer);
            CanonGameState.ActionHandler.AddAction(endTurnAction);
            //CanonGameState.EndTurn();
        }
    }

    public void DiscardPlayerHand()
    {
        if (CanonGameState.CurrentPlayer.IsHuman)
        {
            CanonGameState.CurrentPlayer.DiscardHand();
        }
    }

    public Player GetOtherPlayer(Player player)
    {
        return player == Player1 ? Player2 : Player1;
    }

    public void RestartGame()
    {

        View.ClearPools();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void GoToOptions()
    {
        ScreenHandler.Instance.ShowScreen(ScreenName.Options, true);
    }
    public void GoToPause()
    {
        ScreenHandler.Instance.HideScreen(ScreenName.Options, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.Pause, true);
    }
    public void GoToStartScreen()
    {
        CurrentScreen = ScreenName.Blank;

        ScreenHandler.Instance.ShowScreen(ScreenName.Blank, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.Start);
    }
    public void StartGame()
    {
        //ProgressionHandler.CurrentLevel = 0;

        FirstTimeSetup();

        CurrentScreen = ScreenName.Game;

        ScreenHandler.Instance.HideScreen(ScreenName.StarterBundle);
        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game);
        ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, false, false);

        LoadLevel();
    }
    public void StartTestChamber()
    {
        IsTestChamber = true;

        CurrentScreen = ScreenName.Game;

        StartGame();
    }
    private void GoToGameOverScreen()
    {
        CurrentScreen = ScreenName.GameOver;

        ScreenHandler.Instance.ShowScreen(ScreenName.GameOver);
    }
    private void GoToCardRemovalRewardScreen()
    {
        CurrentScreen = ScreenName.CardRemovalRewards;

        GameField.SetActive(false);
        CardRemovalRewardHandler.gameObject.SetActive(true);
        CardRemovalRewardHandler.Load(HumanPlayerDetails.DeckBlueprint[0]);
        //ScreenHandler.Instance.ShowScreen(ScreenName.Blank, true, false);

        ScreenHandler.Instance.HideScreen(ScreenName.Game, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.CardRemovalRewards, false, false);
    }
    public void GoToRitualRewardScreen()
    {
        CurrentScreen = ScreenName.RitualRewards;

        GameField.SetActive(false);
        RitualRewardHandler.gameObject.SetActive(true);
        RitualRewardHandler.Load(ProgressionHandler.CurrentLevel, HumanPlayerDetails.MajorRituals[0], HumanPlayerDetails.MinorRituals[0]);
        ScreenHandler.Instance.ShowScreen(ScreenName.RitualRewards);
    }
    public void GoToCardGainRewardScreen()
    {
        CurrentScreen = ScreenName.CardGainRewards;

        GameField.SetActive(false);
        CardGainRewardHandler.gameObject.SetActive(true);
        CardGainRewardHandler.Load(ProgressionHandler.CurrentLevel);

        //ScreenHandler.Instance.HideScreen(ScreenName.DeckScreenButton, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.CardGainRewards);
    }

    public void GoToStarterBundleScreen()
    {
        CurrentScreen = ScreenName.StarterBundle;

        FirstTimeSetup();

        GameField.SetActive(false);
        StarterBundleHandler.gameObject.SetActive(true);
        StarterBundleHandler.Load();
        ScreenHandler.Instance.HideScreen(ScreenName.Blank);
        ScreenHandler.Instance.ShowScreen(ScreenName.StarterBundle);
        ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, false, false);
        //ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, false, false);
    }

    public void PauseGame()
    {
        GamePaused = true;
        //GameField.SetActive(false);
        ScreenHandler.Instance.ShowScreen(ScreenName.Pause, true);
    }
    public void UnPauseGame()
    {
        GamePaused = false;
        GameField.SetActive(true);
        ScreenHandler.Instance.HideScreen(ScreenName.Pause, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game, true);
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

            if (ProgressionHandler.CurrentLevel == 5)
            {
                GoToTrinketScreen();
            }
            else if (ProgressionHandler.CurrentLevel >= 10)
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
        HumanPlayerDetails.DeckBlueprint[0].AddRange(cards);
    }
    public void SetRituals(Ritual topRitual, Ritual bottomRitual)
    {
        HumanPlayerDetails.MajorRituals[0] = topRitual;
        HumanPlayerDetails.MinorRituals[0] = bottomRitual;
    }
    public void AddTrinket(Trinket trinket)
    {
        HumanPlayerDetails.Trinkets[0].Add(trinket);
    }
    public void RemoveCardsFromPlayerDeck(List<Card> cards)
    {
        foreach (Card card in cards)
        {
            if (card is Follower follower) SacrificedFollowers.Add(follower);

            HumanPlayerDetails.DeckBlueprint[0].Remove(card);
        }
    }

    private void LoadStartingBattleRow()
    {
        foreach (Follower follower in Player1.StartingBattleRow)
        {
            int index = Player1.BattleRow.Followers.Count;
            follower.Init(Player1);
            //follower.Owner.SummonFollower(follower, index, false);
            GameAction newAction = new SummonFollowerAction(follower, index, false);
            Player1.GameState.ActionHandler.AddAction(newAction);
        }

        foreach (Follower follower in Player2.StartingBattleRow)
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
        ScreenHandler.Instance.HideScreen(CurrentScreen, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);

        DeckViewer.gameObject.SetActive(true);
        List<Card> playerDeck = new List<Card>(HumanPlayerDetails.DeckBlueprint[0]);

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
        ScreenHandler.Instance.ShowScreen(CurrentScreen, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, true, false);

        DeckViewer.Exit();
        DeckViewer.gameObject.SetActive(false);
    }
    public static int GetRandomMetaSeed()
    {
        //return 1; // For consistent testing
        return UnityEngine.Random.Range(0, 1000);
    }

    public void TogglePlayHistory()
    {
        if (ViewPlayHistoryHandler.gameObject.activeSelf)
        {
            HidePlayHistory();
        }
        else
        {
            LoadPlayHistory();
        }
    }
    public void LoadPlayHistory()
    {
        ScreenHandler.Instance.HideScreen(CurrentScreen, true);
        ScreenHandler.Instance.HideScreen(ScreenName.DeckScreenButton, true);

        ViewPlayHistoryHandler.gameObject.SetActive(true);
        
        List<PlayHistoryItem> items = PlayHistoryHandler.GetPlayHistoryItems(); // new List<PlayHistoryItem>();
        ViewPlayHistoryHandler.Load(items);
    }
    public void HidePlayHistory()
    {
        ViewPlayHistoryHandler.Exit();
        ViewPlayHistoryHandler.gameObject.SetActive(false);

        ScreenHandler.Instance.ShowScreen(CurrentScreen, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, true, false);
    }

    public void GoToTrinketScreen()
    {
        CurrentScreen = ScreenName.TrinketRewardScreen;

        GameField.SetActive(false);
        TrinketRewardHandler.gameObject.SetActive(true);
        TrinketRewardHandler.Load();

        //ScreenHandler.Instance.HideScreen(ScreenName.DeckScreenButton, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.TrinketRewardScreen);
    }
}
