using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;


public class Controller : MonoBehaviour
{
    public static Controller Instance;

    public static int starterSeed = 1;
    public bool GamePaused = false;

    public static bool ActionDebugMode = false;
    public static bool AnimationDebugMode = false;
    public static bool AIDebugMode = false;
    public static bool ShowCardIDs = false;

    public static bool BlitzMode = true;


    public CancellationTokenSource CancellationTokenSource = new();

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

    /// <summary>Tracks cards/rituals/trinkets removed during the current run for the River Styx node.</summary>
    public StyxRunState StyxRunState = new StyxRunState();

    /// <summary>Event definitions the player has already seen this run.</summary>
    public HashSet<Pharmakos.Events.EventDefinition> EncounteredEventsThisRun = new HashSet<Pharmakos.Events.EventDefinition>();

    /// <summary>Human player's heartstring count for the current run; persists between combats and events.</summary>
    public int RunHeartStrings = Player.StartingHeartStrings;


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
    public ContentScrollView ContentScrollView;
    public TextHandler TextHandler = new TextHandler();
    public PlayHistoryHandler PlayHistoryHandler = new PlayHistoryHandler();
    public ViewPlayHistoryHandler ViewPlayHistoryHandler;
    public TrinketRewardHandler TrinketRewardHandler;
    public LightingHandler LightingHandler;
    public OverworldMapController OverworldMapController;
    public EventHandler EventHandler;
    public TempleHandler TempleHandler;
    public ShopHandler ShopHandler;

    private StyxScreenHandler _styxScreenHandler;
    private TrinketUnlockHandler _trinketUnlockHandler;
    private StyxTrinketSelectHandler _styxTrinketSelectHandler;
    private StatusScreenHandler _statusScreenHandler;
    private ScreenName _screenBeforeStatus;

    private static readonly ScreenName[] StatusButtonScreens =
    {
        ScreenName.Game,
        ScreenName.Overworld,
        ScreenName.StarterBundle,
        ScreenName.CardGainRewards,
        ScreenName.Temple,
        ScreenName.Event,
        ScreenName.Shop,
        ScreenName.RitualRewards,
        ScreenName.TrinketRewardScreen,
    };

    private OverworldMapNode _lastOverworldNodeEntered;

    public const int OverworldStatusDisabledFloor = 11;

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
        if (Player1 != null && Player2 != null && GameRunning)
        {
            Player1.RunUpdate();
            Player2.RunUpdate();
            View.Instance.PlayerUpdate();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscapeKey();

        View.Instance.AnimationUpdate();
    }

    private void HandleEscapeKey()
    {
        if (ContentScrollView.IsActive)
        {
            if (CurrentScreen == ScreenName.Status)
                CloseStatusScreen();
            else
                HideContentScrollView();
            return;
        }

        if (DeckViewer != null && DeckViewer.gameObject.activeSelf)
        {
            HideDeckViewer();
            return;
        }

        if (ViewPlayHistoryHandler != null && ViewPlayHistoryHandler.gameObject.activeSelf)
        {
            HidePlayHistory();
            return;
        }

        if (GamePaused)
        {
            if (ScreenHandler.Instance.CurrentScreen != null
                && ScreenHandler.Instance.CurrentScreen.Name == ScreenName.Options)
            {
                GoToPause();
                return;
            }

            UnPauseGame();
            return;
        }

        if (!CanOpenPauseMenu())
            return;

        PauseGame();
    }

    private bool CanOpenPauseMenu()
    {
        if (!isGameSetup)
            return false;

        switch (CurrentScreen)
        {
            case ScreenName.Start:
            case ScreenName.Blank:
            case ScreenName.Pause:
            case ScreenName.Options:
            case ScreenName.GameOver:
            case ScreenName.Success:
            case ScreenName.Tutorial:
                return false;
            default:
                return true;
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
        }

        Player1 = new HumanPlayer();
        Player2 = new AIPlayer();
        CanonGameState = new GameState(Player1, Player2);
        ProgressionHandler = new ProgressionHandler();
        if (IsTestChamber)
            ProgressionHandler.SetupNextEnemy(true);

        isGameSetup = true;
    }
    private void LoadLevel()
    {
        CardGainRewardHandler.gameObject.SetActive(false);
        RitualRewardHandler.gameObject.SetActive(false);
        CardRemovalRewardHandler.gameObject.SetActive(false);
        GameField.SetActive(true);

        if (Player1 != null && Player1.IsHuman)
            RunHeartStrings = Mathf.Clamp(Player1.CurrentHeartStrings, 0, Player.MaxHeartStrings);

        Player1 = new HumanPlayer();
        Player2 = new AIPlayer();
        CanonGameState = new GameState(Player1, Player2);
        Player1.AttachToGameState(CanonGameState);
        Player2.AttachToGameState(CanonGameState);

        Player1.LoadDetails(HumanPlayerDetails, 0);

        Player1.Init(0);

        ProgressionHandler.LoadEnemy(Player2);
        ProgressionHandler.RecordLastFoughtEnemy();
        //Player2.LoadDeck(Player2.DeckBlueprint);
        Player2.Init(1);

        View.Instance.Setup();

        Player1.ApplyTrinketBuffs();
        Player2.ApplyTrinketBuffs();

        if (!IsTestChamber)
        {
            var startFightAction = new StartFightAction(ProgressionHandler.GetCurrentFightDisplayName());
            CanonGameState.ActionHandler.AddAction(startFightAction);
        }

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
    public void StartNextLevel(bool useScreenTransition = true)
    {
        PlayHistoryHandler.Clear();

        if (!IsTestChamber && OverworldMapController != null)
        {
            if (useScreenTransition)
                ReturnToOverworld();
            else
                ReturnToOverworldImmediate();
            return;
        }

        ProgressionHandler.SetupNextEnemy();
        View.Instance.DarknessHandler.SetDarkness();
        ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, ShowNextLevel);
        View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
    }

    private void ReturnToOverworld()
    {
        View.Instance.DarknessHandler.SetDarkness();
        ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, ReturnToOverworldImmediate);
        View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
    }

    private void ReturnToOverworldImmediate()
    {
        TearDownOverworldEncounter();
        OverworldMapController.ReturnToMap(_lastOverworldNodeEntered);
        CurrentScreen = ScreenName.Overworld;
        ScreenHandler.Instance.ShowScreen(ScreenName.Overworld);
        ShowStatusButton();
    }

    /// <summary>
    /// Called at the midpoint of the fade when leaving shop, events, etc. back to the overworld.
    /// </summary>
    void TearDownOverworldEncounter()
    {
        if (ShopHandler == null)
            ShopHandler = FindObjectOfType<ShopHandler>();

        if (ShopHandler != null && ShopHandler.gameObject.activeInHierarchy)
            ShopHandler.EndShop();

        if (EventHandler == null)
            EventHandler = FindObjectOfType<EventHandler>();

        if (EventHandler != null)
            EventHandler.CleanupEventPresentation();

        ScreenHandler.Instance.HideScreen(ScreenName.Event, true);
        ScreenHandler.Instance.HideScreen(ScreenName.Styx, true);
        ScreenHandler.Instance.HideScreen(ScreenName.TrinketUnlock, true);
        ScreenHandler.Instance.HideScreen(ScreenName.Game, true);
        GameField.SetActive(false);
        SetMainCombatCameraActive(false);
    }

    void SetMainCombatCameraActive(bool active)
    {
        if (ScreenHandler.Instance == null
            || !ScreenHandler.Instance.TryGetScreen(ScreenName.Overworld, out Screen screen))
            return;

        OverworldScreen overworldScreen = screen as OverworldScreen;
        if (overworldScreen == null || overworldScreen.MainCamera == null)
            return;

        overworldScreen.MainCamera.gameObject.SetActive(active);
    }

    public void BeginEncounterFromOverworldNode(OverworldMapNode node)
    {
        if (node == null || IsTestChamber) return;

        _lastOverworldNodeEntered = node;

        // Temple = sacrifice three cards from deck, then ritual rewards (no combat).
        if (node.EncounterType == EncounterType.Temple)
        {
            ProgressionHandler.RegisterTempleEncounter();
            BeginTempleEncounter();
            return;
        }

        if (node.EncounterType == EncounterType.Market)
        {
            ProgressionHandler.RegisterMarketEncounter();
            BeginShopEncounter();
            return;
        }

        // Styx = exchange deck/rituals/trinkets for everything removed this run (no combat).
        if (node.EncounterType == EncounterType.Styx)
        {
            ProgressionHandler.RegisterStyxEncounter();
            BeginStyxEncounter();
            return;
        }

        if (node.EncounterType == EncounterType.Event)
        {
            ProgressionHandler.RegisterEventEncounter();
            if (EventHandler == null)
            {
                EventHandler = FindObjectOfType<EventHandler>();
            }

            if (EventHandler != null)
            {
                ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, () =>
                {
                    OverworldMapController.HideMap();
                    SetMainCombatCameraActive(false);
                    CurrentScreen = ScreenName.Event;
                    ScreenHandler.Instance.ShowScreen(ScreenName.Event, true, true);
                    ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
                    ShowStatusButton();
                    EventHandler.BeginRandomEvent(() => StartNextLevel());
                }, () => {});
                transitionAnimation.FadeInDuration = EventHandler.FadeInDuration;
                View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
            }
            else
                Debug.LogError("Controller: EventHandler is not assigned and none was found in the scene. Assign the EventHandler component on the Controller.");
            return;
        }

        int fightNumber = Mathf.Max(1, node.R);
        if (ProgressionHandler.TryGetBossDeckName(node.EncounterType, out _))
            ProgressionHandler.SetupBossEncounter(node.EncounterType, fightNumber);
        else
            ProgressionHandler.SetupNextCombatEnemy(fightNumber);

        ScreenTransitionAnimation combatTransitionAnimation = new ScreenTransitionAnimation(null, () =>
        {
            OverworldMapController.HideMap();
            CurrentScreen = ScreenName.Game;
            ScreenHandler.Instance.ShowScreen(ScreenName.Game, true, true);
            // ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, false, false);
            ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, false, false);
            LoadLevel();
        }, () => { });
        View.Instance.AnimationHandler.AddAnimationActionToQueue(combatTransitionAnimation);
    }

    private void BeginTempleEncounter()
    {
        if (TempleHandler == null)
            TempleHandler = FindObjectOfType<TempleHandler>();

        if (TempleHandler == null)
        {
            Debug.LogError("Controller: TempleHandler is not assigned and none was found in the scene.");
            GoToCardRemovalRewardScreen();
            return;
        }

        ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, () =>
        {
            OverworldMapController.HideMap();
            GameField.SetActive(false);
            SetMainCombatCameraActive(false);
            CurrentScreen = ScreenName.Temple;
            ScreenHandler.Instance.ShowScreen(ScreenName.Temple, true, true);
            ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
            ShowStatusButton();
            TempleHandler.BeginTempleSacrifice();
        }, () => { });
        View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
    }

    private void BeginStyxEncounter()
    {
        StyxScreenHandler styxScreenHandler = GetStyxScreenHandler();
        if (styxScreenHandler == null)
        {
            Debug.LogError("Controller: No StyxScreenHandler found in the scene.");
            StartNextLevel();
            return;
        }

        ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, () =>
        {
            OverworldMapController.HideMap();
            GameField.SetActive(false);
            SetMainCombatCameraActive(false);
            CurrentScreen = ScreenName.Styx;
            ScreenHandler.Instance.ShowScreen(ScreenName.Styx, true, true);
            ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
            styxScreenHandler.BeginStyx();
        }, () => { });
        View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
    }

    private void BeginShopEncounter()
    {
        if (ShopHandler == null)
            ShopHandler = FindObjectOfType<ShopHandler>();

        if (ShopHandler == null)
        {
            Debug.LogError("Controller: ShopHandler is not assigned and none was found in the scene.");
            StartNextLevel();
            return;
        }

        ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, () =>
        {
            OverworldMapController.HideMap();
            GameField.SetActive(false);
            SetMainCombatCameraActive(false);
            CurrentScreen = ScreenName.Shop;
            ScreenHandler.Instance.ShowScreen(ScreenName.Shop, true, true);
            ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
            ShowStatusButton();
            ShopHandler.BeginShop();
        }, () => { });
        View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
    }

    private void ShowNextLevel()
    {
        LoadLevel();

        CurrentScreen = ScreenName.Game;

        ScreenHandler.Instance.HideScreen(ScreenName.CardGainRewards, true);
        ScreenHandler.Instance.HideScreen(ScreenName.CardRemovalRewards, true);
        ScreenHandler.Instance.HideScreen(ScreenName.RitualRewards, true);
        ScreenHandler.Instance.HideScreen(ScreenName.Temple, true);
        ScreenHandler.Instance.HideScreen(ScreenName.Shop, true);
        // ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, false, false);
        ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, false, false);
        ScreenHandler.Instance.ShowScreen(ScreenName.Game, true, false);
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

        ScreenHandler.Instance.HideScreen(ScreenName.Blank, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.Start, true);
    }
    public void StartGame()
    {
        RunHeartStrings = Player.StartingHeartStrings;
        StyxRunState.Reset();
        EncounteredEventsThisRun.Clear();
        FirstTimeSetup();

        if (!IsTestChamber && OverworldMapController != null)
        {
            OverworldMapController.SetupOverworldForNewRun();
        }

        CurrentScreen = ScreenName.Game;

        ScreenHandler.Instance.HideScreen(ScreenName.Start);
        // ScreenHandler.Instance.HideScreen(ScreenName.StarterBundle);
        // ScreenHandler.Instance.ShowScreen(ScreenName.Game, true, false);
        // ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, false, false);

        if (IsTestChamber)
        {
            ScreenHandler.Instance.HideScreen(ScreenName.Blank);
            ScreenHandler.Instance.ShowScreen(ScreenName.Game, false, false);
            View.Instance.DarknessHandler.SetDarkness(0);
            LoadLevel();
        }
        else if (OverworldMapController != null)
        {
            ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, () =>
            {
                HideStarterBundles();
                ScreenHandler.Instance.HideScreen(ScreenName.StyxTrinketSelect, true);
                CurrentScreen = ScreenName.Overworld;
                ScreenHandler.Instance.ShowScreen(ScreenName.Overworld, true, false);
                ShowStatusButton();
            });
            View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
        }
        else
        {
            ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, HideStarterBundles);
            View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
            LoadLevel();
        }
    }
    private void HideStarterBundles()
    {
        StarterBundleHandler.Hide();
        ScreenHandler.Instance.HideScreen(ScreenName.StarterBundle, true);
        View.Instance.DarknessHandler.SetDarkness();
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

        ScreenHandler.Instance.HideScreen(ScreenName.Overworld, true);
        ScreenHandler.Instance.HideScreen(ScreenName.Game, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.CardRemovalRewards, false, false);
    }
    public void GoToRitualRewardScreen()
    {
        GoToRitualRewardScreen(null, null);
    }

    public void GoToRitualRewardScreen(Ritual offeredTopReward, Ritual offeredBottomReward)
    {
        CurrentScreen = ScreenName.RitualRewards;

        GameField.SetActive(false);
        RitualRewardHandler.gameObject.SetActive(true);
        RitualRewardHandler.Load(
            ProgressionHandler.CurrentLevel,
            HumanPlayerDetails.MajorRituals[0],
            HumanPlayerDetails.MinorRituals[0],
            offeredTopReward,
            offeredBottomReward);
        ScreenHandler.Instance.HideScreen(ScreenName.Event, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.RitualRewards);
        ShowStatusButton();
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
        ShowStatusButton();
    }

    public void GoToStarterBundleScreen()
    {
        CurrentScreen = ScreenName.StarterBundle;

        FirstTimeSetup();

        ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, () =>
        {
            ScreenHandler.Instance.HideScreen(ScreenName.Start, true);
            ScreenHandler.Instance.ShowScreen(ScreenName.StarterBundle, true, false);
            // ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, true, false);
            ShowStatusButton();

            GameField.SetActive(false);
            StarterBundleHandler.gameObject.SetActive(true);
            StarterBundleHandler.Load();
        }, () => {});
        View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);

        //ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, false, false);
    }

    public void PauseGame()
    {
        if (GamePaused)
            return;

        GamePaused = true;
        ScreenHandler.Instance.ShowScreen(ScreenName.Pause, true, true, true);
    }

    public void UnPauseGame()
    {
        if (!GamePaused)
            return;

        GamePaused = false;
        ScreenHandler.Instance.HideScreen(ScreenName.Pause, true);
        ScreenHandler.Instance.HideScreen(ScreenName.Options, true);
        RestoreScreenAfterUnpause();
    }

    private void RestoreScreenAfterUnpause()
    {
        GameField.SetActive(CurrentScreen == ScreenName.Game && GameRunning);

        ScreenHandler.Instance.ShowScreen(CurrentScreen, true, true);

        switch (CurrentScreen)
        {
            case ScreenName.Game:
                // ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, true, false);
                ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, true, false);
                break;
            case ScreenName.Status:
                GetStatusScreenHandler()?.Open();
                break;
            case ScreenName.StarterBundle:
                // ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, true, false);
                ShowStatusButton();
                break;
            default:
                if (ScreenSupportsStatusButton(CurrentScreen))
                    ShowStatusButton();
                break;
        }
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

    public bool CheckForPlayerDeath()
    {
        if (CurrentScreen != ScreenName.Game) return false;

        if (Player1.Health <= 0)
        {
            ClearLevel();
            GameRunning = false;

            GoToGameOverScreen();
            Player1.Health = 1;
            return true;
        }
        else if (Player2.Health <= 0)
        {
            ClearLevel();
            GameRunning = false;

            ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, ProgressToNextLevel);
            View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);

            

            Player2.Health = 1;
            return true;
        }

        return false;
    }

    private void ProgressToNextLevel()
    {
        // After winning a fight (combat or boss).
        ApplyPostEncounterProgression(offerCardPackReward: true);
    }

    /// <summary>
    /// Called when the non-combat temple flow finishes (removal + ritual UI). No extra card pack unless trinket tier says so.
    /// </summary>
    public void OnOverworldRitualNodeRewardsComplete()
    {
        ApplyPostEncounterProgression(offerCardPackReward: false);
    }

    void ApplyPostEncounterProgression(bool offerCardPackReward)
    {
        if (TryShowStyxTrinketUnlock(offerCardPackReward))
            return;

        ApplyPostEncounterProgressionCore(offerCardPackReward);
    }

    /// <summary>
    /// First-time Fates/Gate boss kills permanently unlock a Styx trinket and show the
    /// reveal screen before the usual post-encounter rewards.
    /// </summary>
    private bool TryShowStyxTrinketUnlock(bool offerCardPackReward)
    {
        Trinket unlockedTrinket = null;

        if (ProgressionHandler.CurrentEnemy == ProgressionHandler.DeckName.Fates && !StyxUnlocks.StringsOfFateUnlocked)
        {
            StyxUnlocks.UnlockStringsOfFate();
            unlockedTrinket = new StringsOfFateTrinket();
        }
        else if (ProgressionHandler.CurrentEnemy == ProgressionHandler.DeckName.TheGate && !StyxUnlocks.GatesBeyondUnlocked)
        {
            StyxUnlocks.UnlockGatesBeyond();
            unlockedTrinket = new GatesBeyondTrinket();
        }

        if (unlockedTrinket == null) return false;

        GoToTrinketUnlockScreen(unlockedTrinket, () => ApplyPostEncounterProgressionCore(offerCardPackReward, useScreenTransition: false));
        return true;
    }

    void ApplyPostEncounterProgressionCore(bool offerCardPackReward, bool useScreenTransition = true)
    {
        if (ProgressionHandler.CurrentEnemy == ProgressionHandler.DeckName.Throne)
        {
            GameField.SetActive(false);
            SetMainCombatCameraActive(false);
            CurrentScreen = ScreenName.Success;
            ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
            ScreenHandler.Instance.ShowScreen(ScreenName.Success);
            return;
        }

        if (ProgressionHandler.IsCurrentEnemyBoss())
        {
            StartNextLevel(useScreenTransition);
            return;
        }

        if (offerCardPackReward)
        {
            // Combat nodes: card gain only. Temple uses removal + rituals without a fight.
            GoToCardGainRewardScreen();
        }
        else
            StartNextLevel(useScreenTransition);
    }

    public void AddCardsToPlayerDeck(List<Card> cards)
    {
        HumanPlayerDetails.DeckBlueprint[0].AddRange(cards);
    }
    public void SetRituals(Ritual topRitual, Ritual bottomRitual)
    {
        RecordReplacedRitual(HumanPlayerDetails.MajorRituals[0], topRitual);
        RecordReplacedRitual(HumanPlayerDetails.MinorRituals[0], bottomRitual);

        HumanPlayerDetails.MajorRituals[0] = topRitual;
        HumanPlayerDetails.MinorRituals[0] = bottomRitual;
    }

    /// <summary>Rituals swapped out for a different ritual count as removed for the Styx.</summary>
    private void RecordReplacedRitual(Ritual previousRitual, Ritual newRitual)
    {
        if (previousRitual == null) return;
        if (newRitual != null && newRitual.GetType() == previousRitual.GetType()) return;

        StyxRunState.RecordRemovedRitual(previousRitual);
    }

    /// <summary>Cards and rituals given up in a full deck swap count as sacrificed to the Styx.</summary>
    public void RecordSacrificedDeckAndRituals()
    {
        foreach (Card card in HumanPlayerDetails.DeckBlueprint[0])
            StyxRunState.RecordRemovedCard(card);

        StyxRunState.RecordRemovedRitual(HumanPlayerDetails.MajorRituals[0]);
        StyxRunState.RecordRemovedRitual(HumanPlayerDetails.MinorRituals[0]);
    }

    public void AddTrinket(Trinket trinket)
    {
        HumanPlayerDetails.Trinkets[0].Add(trinket);

        if (trinket is GatesBeyondTrinket)
            HumanPlayerDetails.CanSacrificeForHeartstrings = true;
    }

    public bool RemoveTrinket(Trinket trinket)
    {
        if (trinket == null) return false;
        if (!HumanPlayerDetails.Trinkets[0].Remove(trinket)) return false;

        StyxRunState.RecordRemovedTrinket(trinket);
        RefreshCanSacrificeForHeartstrings();
        return true;
    }

    private void RefreshCanSacrificeForHeartstrings()
    {
        HumanPlayerDetails.CanSacrificeForHeartstrings =
            HumanPlayerDetails.Trinkets[0].Exists(trinket => trinket is GatesBeyondTrinket);
    }

    /// <summary>Gates Beyond: sacrifice a trinket to the Styx in exchange for a heartstring.</summary>
    public bool TrySacrificeTrinketForHeartstring(Trinket trinket)
    {
        if (!HumanPlayerDetails.CanSacrificeForHeartstrings) return false;
        if (RunHeartStrings >= Player.MaxHeartStrings) return false;
        if (!RemoveTrinket(trinket)) return false;

        AddHeartstrings(1);
        return true;
    }

    /// <summary>Gates Beyond: sacrifice the major or minor ritual to the Styx in exchange for a heartstring.</summary>
    public bool TrySacrificeRitualForHeartstring(bool major)
    {
        if (!HumanPlayerDetails.CanSacrificeForHeartstrings) return false;
        if (RunHeartStrings >= Player.MaxHeartStrings) return false;

        Dictionary<int, Ritual> slot = major ? HumanPlayerDetails.MajorRituals : HumanPlayerDetails.MinorRituals;
        Ritual ritual = slot[0];
        if (ritual == null) return false;

        slot[0] = null;
        StyxRunState.RecordRemovedRitual(ritual);
        AddHeartstrings(1);
        return true;
    }

    /// <summary>
    /// The River Styx exchange: the Styx deck/trinkets/chosen rituals become the player's,
    /// while everything the player carried joins the removed pool.
    /// </summary>
    public void ApplyStyxExchange(List<Ritual> chosenRituals)
    {
        if (chosenRituals == null) chosenRituals = new List<Ritual>();

        // Deck swap.
        List<Card> oldDeck = HumanPlayerDetails.DeckBlueprint[0];
        HumanPlayerDetails.DeckBlueprint[0] = StyxRunState.GetStyxDeck();
        StyxRunState.RemovedCards.Clear();
        foreach (Card card in oldDeck)
            StyxRunState.RecordRemovedCard(card);

        // Trinket swap.
        List<Trinket> oldTrinkets = new List<Trinket>(HumanPlayerDetails.Trinkets[0]);
        HumanPlayerDetails.Trinkets[0] = new List<Trinket>(StyxRunState.RemovedTrinkets);
        StyxRunState.RemovedTrinkets.Clear();
        StyxRunState.RemovedTrinkets.AddRange(oldTrinkets);

        // Rituals: first chosen -> Major, second chosen -> Minor; old rituals join the pool.
        Ritual newMajor = chosenRituals.Count > 0 ? chosenRituals[0] : null;
        Ritual newMinor = chosenRituals.Count > 1 ? chosenRituals[1] : null;
        StyxRunState.RemovedRituals.Remove(newMajor);
        StyxRunState.RemovedRituals.Remove(newMinor);

        Ritual oldMajor = HumanPlayerDetails.MajorRituals[0];
        Ritual oldMinor = HumanPlayerDetails.MinorRituals[0];
        HumanPlayerDetails.MajorRituals[0] = newMajor;
        HumanPlayerDetails.MinorRituals[0] = newMinor;
        StyxRunState.RecordRemovedRitual(oldMajor);
        StyxRunState.RecordRemovedRitual(oldMinor);

        RefreshCanSacrificeForHeartstrings();
    }
    public void AddHeartstrings(int amount)
    {
        if (amount <= 0) return;

        RunHeartStrings = Mathf.Clamp(RunHeartStrings + amount, 0, Player.MaxHeartStrings);
    }

    public bool TrySpendHeartstrings(int amount)
    {
        if (amount <= 0) return true;
        if (RunHeartStrings < amount) return false;

        RunHeartStrings -= amount;
        return true;
    }
    public void RemoveCardsFromPlayerDeck(List<Card> cards)
    {
        foreach (Card card in cards)
        {
            if (card is Follower follower) SacrificedFollowers.Add(follower);

            if (HumanPlayerDetails.DeckBlueprint[0].Remove(card))
                StyxRunState.RecordRemovedCard(card);
        }
    }

    public void RemoveOneCardCopyFromPlayerDeck(string cardType)
    {
        List<Card> deck = HumanPlayerDetails.DeckBlueprint[0];
        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i].GetCardType() == cardType)
            {
                RemoveCardsFromPlayerDeck(new List<Card> { deck[i] });
                return;
            }
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

            GiveFollowerStaticEffectAction sprintAction = new GiveFollowerStaticEffectAction(follower, StaticEffect.Sprint);
            Player1.GameState.ActionHandler.AddAction(sprintAction, true);
        }

        foreach (Follower follower in Player2.StartingBattleRow)
        {
            int index = Player2.BattleRow.Followers.Count;
            follower.Init(Player2);
            //follower.Owner.SummonFollower(follower, index, false);
            GameAction newAction = new SummonFollowerAction(follower, index, false);
            Player2.GameState.ActionHandler.AddAction(newAction);

            GiveFollowerStaticEffectAction sprintAction = new GiveFollowerStaticEffectAction(follower, StaticEffect.Sprint);
            Player2.GameState.ActionHandler.AddAction(sprintAction, true);
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
        if (IsContentScrollViewActive())
            HideContentScrollView();
        else
            LoadDeckViewer();
    }
    public void LoadDeckViewer()
    {
        ScreenHandler.Instance.HideScreen(CurrentScreen, true);
        // ScreenHandler.Instance.HideScreen(ScreenName.DeckScreenButton, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);

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

        ScreenHandler.Instance.ShowScreen(ScreenName.DeckViewerScreen, true, false);

        if (ContentScrollView.Instance != null)
            ContentScrollView.ShowCards(playerDeck);
        else if (DeckViewer != null)
        {
            DeckViewer.gameObject.SetActive(true);
            DeckViewer.Load(playerDeck);
        }
    }
    public void HideDeckViewer() => HideContentScrollView();
    public void HideContentScrollView()
    {
        if (ContentScrollView.Instance != null)
            ContentScrollView.Hide();

        ScreenHandler.Instance.HideScreen(ScreenName.DeckViewerScreen, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryScreen, true);

        if (DeckViewer != null && DeckViewer.gameObject.activeSelf)
        {
            DeckViewer.Exit();
            DeckViewer.gameObject.SetActive(false);
        }
        else if (ViewPlayHistoryHandler != null && ViewPlayHistoryHandler.gameObject.activeSelf)
        {
            ViewPlayHistoryHandler.Exit();
            ViewPlayHistoryHandler.gameObject.SetActive(false);
        }

        ScreenHandler.Instance.ShowScreen(CurrentScreen, true);
        if (CurrentScreen == ScreenName.Game)
        {
            ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryButton, true, false);
            // ScreenHandler.Instance.ShowScreen(ScreenName.DeckScreenButton, true, false);
        }
    }
    bool IsContentScrollViewActive() => ContentScrollView.IsActive;
    public static int GetRandomMetaSeed()
    {
        //return 1; // For consistent testing
        return UnityEngine.Random.Range(0, 1000);
    }

    public void TogglePlayHistory()
    {
        if (IsContentScrollViewActive())
            HidePlayHistory();
        else
            LoadPlayHistory();
    }
    public void LoadPlayHistory()
    {
        ScreenHandler.Instance.HideScreen(CurrentScreen, true);
        // ScreenHandler.Instance.HideScreen(ScreenName.DeckScreenButton, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);

        List<PlayHistoryItem> items = PlayHistoryHandler.GetPlayHistoryItems();

        ScreenHandler.Instance.ShowScreen(ScreenName.PlayHistoryScreen, true, false);

        if (ContentScrollView.Instance != null)
            ContentScrollView.ShowPlayHistory(items);
        else if (ViewPlayHistoryHandler != null)
        {
            ViewPlayHistoryHandler.gameObject.SetActive(true);
            ViewPlayHistoryHandler.Load(items);
        }
    }
    public void HidePlayHistory() => HideContentScrollView();

    public void GoToTrinketScreen()
    {
        CurrentScreen = ScreenName.TrinketRewardScreen;

        GameField.SetActive(false);
        TrinketRewardHandler.gameObject.SetActive(true);
        TrinketRewardHandler.Load();

        //ScreenHandler.Instance.HideScreen(ScreenName.DeckScreenButton, true);
        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.TrinketRewardScreen);
        ShowStatusButton();
    }

    // ---------------------------------------------------------------------
    // Styx feature: handler lookups (found in scene on demand, like ShopHandler).
    // ---------------------------------------------------------------------

    private StyxScreenHandler GetStyxScreenHandler()
    {
        if (_styxScreenHandler == null)
            _styxScreenHandler = FindObjectOfType<StyxScreenHandler>(true);
        return _styxScreenHandler;
    }

    private TrinketUnlockHandler GetTrinketUnlockHandler()
    {
        if (_trinketUnlockHandler == null)
            _trinketUnlockHandler = FindObjectOfType<TrinketUnlockHandler>(true);
        return _trinketUnlockHandler;
    }

    private StyxTrinketSelectHandler GetStyxTrinketSelectHandler()
    {
        if (_styxTrinketSelectHandler == null)
            _styxTrinketSelectHandler = FindObjectOfType<StyxTrinketSelectHandler>(true);
        return _styxTrinketSelectHandler;
    }

    private StatusScreenHandler GetStatusScreenHandler()
    {
        if (_statusScreenHandler == null)
            _statusScreenHandler = FindObjectOfType<StatusScreenHandler>(true);
        return _statusScreenHandler;
    }

    /// <summary>Reveal screen for a newly unlocked Styx trinket; continues the reward flow afterwards.</summary>
    public void GoToTrinketUnlockScreen(Trinket trinket, Action onContinue)
    {
        TrinketUnlockHandler trinketUnlockHandler = GetTrinketUnlockHandler();
        if (trinketUnlockHandler == null)
        {
            Debug.LogError("Controller: No TrinketUnlockHandler found in the scene.");
            onContinue?.Invoke();
            return;
        }

        CurrentScreen = ScreenName.TrinketUnlock;

        GameField.SetActive(false);
        SetMainCombatCameraActive(false);
        trinketUnlockHandler.Show(trinket, onContinue);

        ScreenHandler.Instance.HideScreen(ScreenName.PlayHistoryButton, true);
        ScreenHandler.Instance.ShowScreen(ScreenName.TrinketUnlock);
    }

    /// <summary>Fades out through the blank screen before running post-trinket-unlock progression.</summary>
    public void ContinueFromTrinketUnlockScreen(Action onContinue)
    {
        ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, () =>
        {
            ScreenHandler.Instance.HideScreen(ScreenName.TrinketUnlock, true);
            GameField.SetActive(false);
            SetMainCombatCameraActive(false);
            onContinue?.Invoke();
        });
        View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
    }

    /// <summary>
    /// Shown after the starting devotion pick when the player has unlocked Styx trinkets.
    /// The handler calls StartGame() once a choice is made.
    /// </summary>
    public void GoToStyxTrinketSelectScreen()
    {
        StyxTrinketSelectHandler styxTrinketSelectHandler = GetStyxTrinketSelectHandler();
        if (styxTrinketSelectHandler == null)
        {
            Debug.LogError("Controller: No StyxTrinketSelectHandler found in the scene.");
            StartGame();
            return;
        }

        CurrentScreen = ScreenName.StyxTrinketSelect;

        ScreenTransitionAnimation transitionAnimation = new ScreenTransitionAnimation(null, () =>
        {
            StarterBundleHandler.Hide();
            ScreenHandler.Instance.HideScreen(ScreenName.StarterBundle, true);
            // ScreenHandler.Instance.HideScreen(ScreenName.DeckScreenButton, true);
            ScreenHandler.Instance.ShowScreen(ScreenName.StyxTrinketSelect, true, true);
            styxTrinketSelectHandler.Show();
        }, () => { });
        View.Instance.AnimationHandler.AddAnimationActionToQueue(transitionAnimation);
    }

    static bool ScreenSupportsStatusButton(ScreenName screen)
    {
        foreach (ScreenName supportedScreen in StatusButtonScreens)
        {
            if (supportedScreen == screen)
                return true;
        }

        return false;
    }

    void ShowStatusButton()
    {
        if (IsOverworldStatusDisabled())
        {
            HideStatusButton();
            return;
        }

        ScreenHandler.Instance.ShowScreen(ScreenName.StatusButton, true, false);
    }

    void HideStatusButton()
    {
        if (CurrentScreen == ScreenName.Status)
            CloseStatusScreen();

        ScreenHandler.Instance.HideScreen(ScreenName.StatusButton, true);
        ScreenHandler.Instance.HideScreen(ScreenName.Status, true);
    }

    bool IsOverworldStatusDisabled()
    {
        return !IsTestChamber
            && OverworldMapController != null
            && OverworldMapController.CurrentNode != null
            && OverworldMapController.CurrentNode.R == OverworldStatusDisabledFloor;
    }

    public void ToggleStatusScreen()
    {
        if (CurrentScreen == ScreenName.Status)
            CloseStatusScreen();
        else
            OpenStatusScreen();
    }

    public void OpenStatusScreen()
    {
        if (!ScreenSupportsStatusButton(CurrentScreen)) return;
        if (CurrentScreen == ScreenName.Overworld && IsOverworldStatusDisabled()) return;

        StatusScreenHandler statusScreenHandler = GetStatusScreenHandler();
        if (statusScreenHandler == null)
        {
            Debug.LogError("Controller: No StatusScreenHandler found in the scene.");
            return;
        }

        _screenBeforeStatus = CurrentScreen;
        CurrentScreen = ScreenName.Status;
        ScreenHandler.Instance.ShowScreen(ScreenName.Status, true, false);
        statusScreenHandler.Open();
    }

    public void CloseStatusScreen()
    {
        if (CurrentScreen != ScreenName.Status) return;

        GetStatusScreenHandler()?.Close();

        ScreenName returnScreen = _screenBeforeStatus;
        CurrentScreen = returnScreen;
        ScreenHandler.Instance.HideScreen(ScreenName.Status, true);

        if (ScreenHandler.Instance.TryGetScreen(returnScreen, out Screen screen))
            ScreenHandler.Instance.CurrentScreen = screen;

        ShowStatusButton();
    }
}
