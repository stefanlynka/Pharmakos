using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using System;

public class AIPlayer : Player
{
    public AITurnPhase TurnPhase = AITurnPhase.Setup;
    public bool TurnInterruptedEarly = false;

    private DecisionSet playerDecisions = new DecisionSet();

    GameStatePool pool;
    private const int maxBranchSize = 4;

    public enum AITurnPhase
    {
        Setup,
        Preparing,
        Executing,
        Ending,
        Waiting,
    }

    public AIPlayer() { }

    public override void StartTurn()
    {
        // Increment turn counter
        TurnNumber++;

        // Give TwistOfFate card every X turns (every 3 turns)
        if (TurnNumber % TwistOfFateInterval == 0 && TwistOfFateBuffs.Count > 0)
        {
            // Create a TwistOfFate card and add it to hand
            TwistOfFate twistOfFateCard = new TwistOfFate();
            twistOfFateCard.Init(this);
            AddCardCopyToHandAction addCardAction = new AddCardCopyToHandAction(twistOfFateCard);
            GameState.ActionHandler.AddAction(addCardAction);
        }

        TurnPhase = AITurnPhase.Setup;
        TurnInterruptedEarly = false;

        base.StartTurn();
    }

    private Task planningTask = null;
    private volatile bool planningCompleted = false;
    private CancellationTokenSource planningTimeoutCts = null;
    private CancellationTokenSource planningLinkedCts = null;
    private readonly object planLock = new object();
    private bool planCommitted;
    private int planningGeneration;

    public override void RunUpdate()
    {
        base.RunUpdate();
        if (!IsMyTurn) return;

        switch (TurnPhase)
        {
            case AITurnPhase.Setup:
                if (TutorialCombatScript.TryScriptTurn(this, out List<PlayerDecision> scripted))
                {
                    playerDecisions = new DecisionSet(scripted, 0f);
                    planningCompleted = true;
                    TurnPhase = AITurnPhase.Preparing;
                    break;
                }

                Debug.Log("Start Planning AI Turn");
                TurnPhase = AITurnPhase.Preparing;
                planningCompleted = false;

                // Dispose previous tokens if any
                planningTimeoutCts?.Dispose();
                planningLinkedCts?.Dispose();

                // Setup cancellation tokens
                var globalToken = Controller.Instance.CancellationTokenSource.Token;
                planningTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                planningLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(globalToken, planningTimeoutCts.Token);

                int generation;
                lock (planLock)
                {
                    planCommitted = false;
                    generation = ++planningGeneration;
                }

                if (Controller.AIDebugMode)
                {
                    PlanActionsSync(generation);
                }
                else
                {
                    planningTask = Task.Run(() => PlanActions(planningLinkedCts.Token, generation), planningLinkedCts.Token);

                    // Start a timer to enforce the 5 second timeout
                    Task.Run(async () =>
                    {
                        try
                        {
                            var completedTask = await Task.WhenAny(planningTask, Task.Delay(5000, planningLinkedCts.Token));
                            if (completedTask != planningTask)
                            {
                                // Cancel unstarted branches. PlanActions commits whatever line has
                                // already finished, or a one-ply greedy turn if none has.
                                planningTimeoutCts.Cancel();
                                Debug.LogWarning("[AI] Planning timed out. Waiting for the search to finish its current line.");
                            }
                        }
                        catch (TaskCanceledException) { }
                    });
                }
                break;
            case AITurnPhase.Preparing:
                if (planningCompleted)
                {
                    // The search often finishes while discard, draw, or the turn banner is still queued.
                    // Hold the chosen line until that playback finishes. The line's own animations start
                    // only after this transition, so they do not keep the turn waiting.
                    if (!View.Instance.AnimationHandler.AnimationsComplete())
                        break;

                    Debug.LogWarning("Planning Completed: Preparing");
                    TurnPhase = AITurnPhase.Executing;
                    planningTask = null;
                    planningCompleted = false;

                    planningTimeoutCts?.Dispose();
                    planningTimeoutCts = null;
                    planningLinkedCts?.Dispose();
                    planningLinkedCts = null;
                }
                break;
            case AITurnPhase.Executing:
                Debug.LogWarning("Starting AI Turn");
                foreach (PlayerDecision playerDecision in playerDecisions.Decisions)
                {
                    Debug.LogWarning("Decision: " + playerDecision.GetString());
                    bool result = DoDecision(playerDecision);
                    if (!result)
                    {
                        Debug.LogWarning("Decision Failed");
                    }
                    if (TurnInterruptedEarly)
                    {
                        Debug.LogWarning("AI turn interrupted early.");
                        break;
                    }
                }

                TurnPhase = TurnInterruptedEarly ? AITurnPhase.Waiting : AITurnPhase.Ending;
                break;
            case AITurnPhase.Ending:
                Debug.LogWarning("Ending AI Turn");
                TurnPhase = AITurnPhase.Waiting;
                var endTurnAction = new TryEndTurnAction(GameState.CurrentPlayer);
                GameState.ActionHandler.AddAction(endTurnAction);

                break;
            case AITurnPhase.Waiting:
                break;
        }
    }


    private void PlanActionsSync(int generation)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        GameStatePool localPool = new GameStatePool();
        DecisionSet result = GetBestOptions(GameState, localPool, Controller.Instance.CancellationTokenSource.Token, 0);
        CommitPlan(generation, result, localPool);
        Debug.Log($"[AI] PlanActions (sync) took {stopwatch.ElapsedMilliseconds / 1000f} seconds");
    }

    private void PlanActions(CancellationToken cancellationToken, int generation)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        GameStatePool localPool = new GameStatePool();
        DecisionSet result = GetBestOptions(GameState, localPool, cancellationToken, 0);
        stopwatch.Stop();
        Debug.Log($"[AI] PlanActions (async) took {stopwatch.ElapsedMilliseconds/1000f} seconds");
        result = ResolveTimedOutSearch(result, cancellationToken, localPool);
        CommitPlan(generation, result, localPool);
    }

    private DecisionSet ResolveTimedOutSearch(DecisionSet result, CancellationToken cancellationToken, GameStatePool localPool)
    {
        if (!cancellationToken.IsCancellationRequested)
            return result;

        if (result.Decisions != null && result.Decisions.Count > 0)
        {
            Debug.LogWarning($"[AI] Planning timed out. Playing the best finished line ({result.Decisions.Count} decisions).");
            return result;
        }

        Debug.LogWarning("[AI] Planning timed out before a finished line. Choosing a one-ply greedy turn.");
        DecisionSet greedy = ChooseGreedyLine(GameState, localPool);
        int decisionCount = greedy.Decisions != null ? greedy.Decisions.Count : 0;
        if (decisionCount == 0)
            Debug.LogWarning("[AI] Greedy search found no actions. Ending the turn.");
        else
            Debug.LogWarning($"[AI] Greedy turn has {decisionCount} decisions.");
        return greedy;
    }

    private void CommitPlan(int generation, DecisionSet result, GameStatePool localPool)
    {
        lock (planLock)
        {
            if (generation != planningGeneration || planCommitted)
            {
                Debug.LogWarning("[AI] Planning finished after another result was committed. Keeping the committed result.");
                return;
            }

            pool = localPool;
            playerDecisions = result;
            planCommitted = true;
            planningCompleted = true;
        }
    }

    static int deepestDepth = 12;
    public static DecisionSet GetBestOptions(GameState gameState, GameStatePool pool, CancellationToken cancellationToken, int currentDepth)
    {
        if (cancellationToken.IsCancellationRequested) return new DecisionSet(new List<PlayerDecision>(), float.NegativeInfinity);

        currentDepth++;
        if (currentDepth > deepestDepth)
        {
            // If we've reached the maximum depth, return a utility value
            float utility = GetUtility(gameState);
            return new DecisionSet(new List<PlayerDecision>(), utility);
        }

        LegalMoves legalMoves = CollectLegalMoves(gameState);
        List<PlayerDecision> options = legalMoves.Options;
        bool pickingAttacks = legalMoves.PickingAttacks;

        // If we're out of options, calculate this state's utility and return up the recursive chain
        if (options.Count == 0)
        {
            float utility = GetUtility(gameState);
            return new DecisionSet(new List<PlayerDecision>(), utility);
        }

        //Debug.Log("Count: " + options.Count);

        DecisionSet bestOptionSoFar = new DecisionSet(new List<PlayerDecision>(), -9999f);

        // Get and store the utility of each option in a list
        List<OptionSet> optionSets = new List<OptionSet>(options.Count);
        foreach (PlayerDecision option in options)
        {
            GameState simulatedGameState = pool.Get(gameState, true);
            AIPlayer thisPlayer = simulatedGameState.GetPlayer(gameState.AI.PlayerID) as AIPlayer;
            bool result = thisPlayer.DoDecision(option);
            if (!result) Debug.LogError("Decision Failed");
            float utility = GetUtility(simulatedGameState);
            optionSets.Add(new OptionSet(simulatedGameState, option, utility));
        }

        // Sort the options by utility, descending
        optionSets.Sort((a, b) => b.Utility.CompareTo(a.Utility));
        // Keep the top maxBranchSize options
        int branchSize = pickingAttacks ? 1 : maxBranchSize;
        List<OptionSet> topOptions = optionSets.GetRange(0, Mathf.Min(branchSize, optionSets.Count));

        foreach (OptionSet optionSet in topOptions)
        {
            // Recursively explore future options, returning the best one
            DecisionSet bestOption = GetBestOptions(optionSet.GameState, pool, cancellationToken, currentDepth);
            if (bestOption.Utility > bestOptionSoFar.Utility)
            {
                List<PlayerDecision> updatedList = bestOption.Decisions;
                updatedList.Insert(0, optionSet.PlayerDecision);
                bestOptionSoFar = new DecisionSet(updatedList, bestOption.Utility);
            }
        }

        foreach (OptionSet optionSet in optionSets)
        {
            pool.Return(optionSet.GameState);
        }

        return bestOptionSoFar;
    }

    private static DecisionSet ChooseGreedyLine(GameState gameState, GameStatePool pool)
    {
        List<PlayerDecision> decisions = new List<PlayerDecision>();
        GameState state = pool.Get(gameState, true);

        for (int step = 0; step < deepestDepth; step++)
        {
            LegalMoves legalMoves = CollectLegalMoves(state);
            if (legalMoves.Options.Count == 0) break;

            PlayerDecision bestDecision = null;
            float bestUtility = float.NegativeInfinity;
            foreach (PlayerDecision option in legalMoves.Options)
            {
                GameState simulated = pool.Get(state, true);
                AIPlayer actor = simulated.GetPlayer(state.AI.PlayerID) as AIPlayer;
                bool applied = actor != null && actor.DoDecision(option);
                float utility = applied ? GetUtility(simulated) : float.NegativeInfinity;
                pool.Return(simulated);
                if (!applied) continue;
                if (utility <= bestUtility) continue;

                bestUtility = utility;
                bestDecision = option;
            }

            if (bestDecision == null) break;

            AIPlayer current = state.GetPlayer(state.AI.PlayerID) as AIPlayer;
            if (current == null || !current.DoDecision(bestDecision)) break;
            decisions.Add(bestDecision);
        }

        pool.Return(state);
        return new DecisionSet(decisions, 0f);
    }

    private struct LegalMoves
    {
        public List<PlayerDecision> Options;
        public bool PickingAttacks;
    }

    private static LegalMoves CollectLegalMoves(GameState gameState)
    {
        var options = new List<PlayerDecision>();
        bool pickingAttacks = false;

        List<Card> playableCards = gameState.AI.GetPlayableCards();
        Dictionary<string, Follower> followersByName = new Dictionary<string, Follower>();
        Dictionary<string, Spell> spellsByName = new Dictionary<string, Spell>();
        Dictionary<string, Follower> freeFollowersByName = new Dictionary<string, Follower>();
        Dictionary<string, Spell> freeSpellsByName = new Dictionary<string, Spell>();

        foreach (Card card in playableCards)
        {
            Follower follower = card as Follower;
            Spell spell = card as Spell;
            if (follower != null)
            {
                if (follower.Costs[OfferingType.Gold] == 0)
                    freeFollowersByName[follower.GetCardType()] = follower;
                else
                    followersByName[follower.GetCardType()] = follower;
            }
            else if (spell != null)
            {
                if (spell.Costs[OfferingType.Gold] == 0)
                    freeSpellsByName[spell.GetCardType()] = spell;
                else
                    spellsByName[spell.GetCardType()] = spell;
            }
        }

        if (playableCards.Count > 0)
        {
            if (followersByName.Count > 0 || spellsByName.Count > 0)
            {
                foreach (Follower follower in followersByName.Values)
                    options.Add(new PlayFollowerDecision(follower.ID, gameState.AI.BattleRow.Followers.Count));

                foreach (Spell spell in spellsByName.Values)
                {
                    foreach (ITarget target in spell.GetTargets())
                        options.Add(new PlaySpellDecision(spell.ID, target.GetID()));
                }
            }
            else
            {
                foreach (Follower follower in freeFollowersByName.Values)
                    options.Add(new PlayFollowerDecision(follower.ID, gameState.AI.BattleRow.Followers.Count));

                foreach (Spell spell in freeSpellsByName.Values)
                {
                    foreach (ITarget target in spell.GetTargets())
                        options.Add(new PlaySpellDecision(spell.ID, target.GetID()));
                }
            }
        }
        else if (gameState.AI.MajorRitual != null && gameState.AI.MajorRitual.CanPlay())
        {
            foreach (ITarget target in gameState.AI.MajorRitual.GetTargets())
                options.Add(new UseRitualDecision(gameState.AI.MajorRitual.ID, target.GetID()));
        }
        else if (gameState.AI.MinorRitual != null && gameState.AI.MinorRitual.CanPlay())
        {
            foreach (ITarget target in gameState.AI.MinorRitual.GetTargets())
                options.Add(new UseRitualDecision(gameState.AI.MinorRitual.ID, target.GetID()));
        }
        else
        {
            pickingAttacks = true;
            Follower followerThatCanAttack = gameState.AI.BattleRow.GetFirstFollowerThatCanAttack();
            if (followerThatCanAttack != null)
            {
                foreach (ITarget attackTarget in followerThatCanAttack.GetAttackTargets())
                    options.Add(new AttackWithFollowerDecision(followerThatCanAttack.ID, attackTarget.GetID()));

                options.Add(new SkipFollowerAttackDecision(followerThatCanAttack.ID));
            }
        }

        return new LegalMoves { Options = options, PickingAttacks = pickingAttacks };
    }

    public struct DecisionSet
    {
        public List<PlayerDecision> Decisions;
        public float Utility;

        public DecisionSet(List<PlayerDecision> decisions, float utility)
        {
            Decisions = decisions;
            Utility = utility;
        }
    }

    public struct OptionSet
    {
        public GameState GameState;
        public PlayerDecision PlayerDecision;
        public float Utility;

        public OptionSet(GameState gameState, PlayerDecision playerDecision, float utility)
        {
            GameState = gameState;
            PlayerDecision = playerDecision;
            Utility = utility;
        }
    }

    private static float GetUtility(GameState gameState)
    {
        float utility = 0;

        Player otherPlayer = gameState.GetOtherPlayer(gameState.AI.PlayerID);
        if (otherPlayer.Health <= 0)
        {
            return float.PositiveInfinity;
        }

        // Positive utility
        Player thisPlayer = gameState.GetPlayer(gameState.AI.PlayerID);
        foreach (Follower follower in thisPlayer.BattleRow.Followers)
        {
            utility += follower.GetUtility();
        }
        utility += thisPlayer.Health;
        utility += thisPlayer.PlayerEffects.Count * 15;

        // Negative utility
        foreach (Follower follower in otherPlayer.BattleRow.Followers)
        {
            utility -= follower.GetUtility();
        }
        utility -= otherPlayer.Health;

        return utility;
    }
}
