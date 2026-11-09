using System.Collections;
using System.Collections.Concurrent;
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
                    bool result;
                    try
                    {
                        result = DoDecision(playerDecision);
                    }
                    catch (Exception e)
                    {
                        // Escaping here would leave the phase on Executing and replay every decision next frame.
                        Debug.LogError("[AI] Decision threw. Skipping the rest of the turn.");
                        Debug.LogException(e);
                        break;
                    }
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
        GameStatePool localPool = new GameStatePool();
        DecisionSet result;
        try
        {
            CancellationToken token = Controller.Instance.CancellationTokenSource.Token;
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            SearchContext context = SearchContext.Optimized(localPool, token, UseParallelSearch);
            result = SearchRoot(GameState, context);
            stopwatch.Stop();
            Debug.Log($"[AI] PlanActions (sync) took {stopwatch.ElapsedMilliseconds / 1000f} seconds. {context.Stats}");

            CompareWithBaselineSearch(result, token);
        }
        catch (Exception e)
        {
            localPool = new GameStatePool();
            result = RecoverFromFailedSearch(e, localPool);
        }
        CommitPlan(generation, result, localPool);
    }

    private void PlanActions(CancellationToken cancellationToken, int generation)
    {
        GameStatePool localPool = new GameStatePool();
        DecisionSet result;
        try
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            SearchContext context = SearchContext.Optimized(localPool, cancellationToken, UseParallelSearch);
            result = SearchRoot(GameState, context);
            stopwatch.Stop();
            Debug.Log($"[AI] PlanActions (async) took {stopwatch.ElapsedMilliseconds/1000f} seconds. {context.Stats}");
            result = ResolveTimedOutSearch(result, cancellationToken, localPool);
        }
        catch (Exception e)
        {
            localPool = new GameStatePool();
            result = RecoverFromFailedSearch(e, localPool);
        }
        CommitPlan(generation, result, localPool);
    }

    // The planning Task swallows its exceptions, so without this a throw leaves the AI in Preparing forever.
    private DecisionSet RecoverFromFailedSearch(Exception e, GameStatePool localPool)
    {
        Debug.LogError("[AI] Planning threw an exception. Trying a one-ply greedy turn.");
        LogPlanningException(e);

        try
        {
            return ChooseGreedyLine(GameState, localPool);
        }
        catch (Exception greedyException)
        {
            Debug.LogError("[AI] Greedy fallback also threw. Ending the turn.");
            LogPlanningException(greedyException);
            return new DecisionSet(new List<PlayerDecision>(), 0f);
        }
    }

    private static void LogPlanningException(Exception e)
    {
        if (e is AggregateException aggregate)
        {
            foreach (Exception inner in aggregate.Flatten().InnerExceptions)
                Debug.LogException(inner);
            return;
        }

        Debug.LogException(e);
    }

    // AIDebugMode only. Reruns the original search on the same state; the optimized search must never score lower.
    private void CompareWithBaselineSearch(DecisionSet optimized, CancellationToken token)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        SearchContext baseline = SearchContext.Baseline(new GameStatePool(), token);
        DecisionSet baselineResult = GetBestOptions(GameState, baseline, 0);
        stopwatch.Stop();

        Debug.Log($"[AI] Baseline search took {stopwatch.ElapsedMilliseconds / 1000f} seconds. {baseline.Stats}. " +
                  $"Utility: optimized {optimized.Utility}, baseline {baselineResult.Utility}");

        if (optimized.Utility < baselineResult.Utility)
        {
            Debug.LogWarning($"[AI] Optimized search scored {optimized.Utility}, below the baseline's {baselineResult.Utility}. " +
                             "StateSignature is probably missing a field that differs between the two lines.");
        }
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

    // Searches the top root branches on separate threads. Machines with two or fewer cores stay single-threaded
    // so the search does not compete with Unity's main and render threads.
    public static bool ParallelRootSearch = true;
    private static bool UseParallelSearch => ParallelRootSearch && Environment.ProcessorCount > 2;

    public sealed class SearchStats
    {
        public long Nodes;
        public long StatesCopied;
        public long CacheHits;
        public long DuplicateSiblings;
        public long LethalCutoffs;

        public override string ToString()
        {
            return $"nodes={Interlocked.Read(ref Nodes)}, copies={Interlocked.Read(ref StatesCopied)}, " +
                   $"cacheHits={Interlocked.Read(ref CacheHits)}, duplicateSiblings={Interlocked.Read(ref DuplicateSiblings)}, " +
                   $"lethalCutoffs={Interlocked.Read(ref LethalCutoffs)}";
        }
    }

    private sealed class CachedLine
    {
        public readonly PlayerDecision[] Decisions;
        public readonly float Utility;

        public CachedLine(DecisionSet line)
        {
            Decisions = line.Decisions.ToArray();
            Utility = line.Utility;
        }

        // Callers prepend to the returned list, so every hit gets its own copy.
        public DecisionSet ToDecisionSet()
        {
            return new DecisionSet(new List<PlayerDecision>(Decisions), Utility);
        }
    }

    private sealed class SearchContext
    {
        public GameStatePool Pool;
        public CancellationToken Token;
        public SearchStats Stats;
        public ConcurrentDictionary<(ulong signature, int depth), CachedLine> Cache;
        public bool DedupeSiblings;
        public bool StopOnLethal;
        public bool ParallelRoot;

        public static SearchContext Optimized(GameStatePool pool, CancellationToken token, bool parallelRoot)
        {
            return new SearchContext
            {
                Pool = pool,
                Token = token,
                Stats = new SearchStats(),
                Cache = new ConcurrentDictionary<(ulong signature, int depth), CachedLine>(),
                DedupeSiblings = true,
                StopOnLethal = true,
                ParallelRoot = parallelRoot,
            };
        }

        public static SearchContext Baseline(GameStatePool pool, CancellationToken token)
        {
            return new SearchContext
            {
                Pool = pool,
                Token = token,
                Stats = new SearchStats(),
            };
        }

        // GameStatePool is not thread-safe, so each parallel branch gets its own. Stats and cache stay shared.
        public SearchContext ForBranch(GameStatePool pool, CancellationToken token)
        {
            SearchContext branch = (SearchContext)MemberwiseClone();
            branch.Pool = pool;
            branch.Token = token;
            return branch;
        }
    }

    private static DecisionSet SearchRoot(GameState gameState, SearchContext context)
    {
        if (!context.ParallelRoot)
            return GetBestOptions(gameState, context, 0);

        if (context.Token.IsCancellationRequested) return new DecisionSet(new List<PlayerDecision>(), float.NegativeInfinity);

        const int rootDepth = 1;
        Interlocked.Increment(ref context.Stats.Nodes);

        LegalMoves legalMoves = CollectLegalMoves(gameState);
        if (legalMoves.Options.Count == 0)
            return new DecisionSet(new List<PlayerDecision>(), GetUtility(gameState));

        List<OptionSet> optionSets = ExpandOptions(gameState, legalMoves.Options, context);
        SortOptions(optionSets);
        int branchCount = Mathf.Min(legalMoves.PickingAttacks ? 1 : maxBranchSize, optionSets.Count);

        DecisionSet[] results = new DecisionSet[branchCount];
        using (CancellationTokenSource lethalCts = CancellationTokenSource.CreateLinkedTokenSource(context.Token))
        {
            void SearchBranch(int index)
            {
                OptionSet optionSet = optionSets[index];
                SearchContext branch = context.ForBranch(new GameStatePool(), lethalCts.Token);
                DecisionSet result = GetBestOptions(optionSet.GameState, branch, rootDepth, optionSet.Signature);
                results[index] = result;
                if (context.StopOnLethal && float.IsPositiveInfinity(result.Utility))
                    lethalCts.Cancel();
            }

            // LongRunning gives each branch its own thread. The planning thread is itself a pool thread,
            // so blocking it on pool tasks could stall until the pool injects more threads.
            Task[] workers = new Task[branchCount - 1];
            for (int i = 1; i < branchCount; i++)
            {
                int index = i;
                workers[i - 1] = Task.Factory.StartNew(() => SearchBranch(index), CancellationToken.None,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            try
            {
                SearchBranch(0);
            }
            finally
            {
                // Workers use lethalCts, so they must finish before it is disposed.
                Task.WaitAll(workers);
            }
        }

        // Combine in branch order with a strict comparison so ties resolve the same way as the sequential search.
        DecisionSet bestOptionSoFar = new DecisionSet(new List<PlayerDecision>(), -9999f);
        for (int i = 0; i < branchCount; i++)
        {
            if (results[i].Utility > bestOptionSoFar.Utility)
            {
                List<PlayerDecision> updatedList = results[i].Decisions;
                updatedList.Insert(0, optionSets[i].PlayerDecision);
                bestOptionSoFar = new DecisionSet(updatedList, results[i].Utility);
            }
        }

        foreach (OptionSet optionSet in optionSets)
        {
            context.Pool.Return(optionSet.GameState);
        }

        return bestOptionSoFar;
    }

    private static DecisionSet GetBestOptions(GameState gameState, SearchContext context, int currentDepth, ulong? knownSignature = null)
    {
        if (context.Token.IsCancellationRequested) return new DecisionSet(new List<PlayerDecision>(), float.NegativeInfinity);

        currentDepth++;
        if (currentDepth > deepestDepth)
        {
            // If we've reached the maximum depth, return a utility value
            float utility = GetUtility(gameState);
            return new DecisionSet(new List<PlayerDecision>(), utility);
        }

        Interlocked.Increment(ref context.Stats.Nodes);

        (ulong signature, int depth) cacheKey = default;
        if (context.Cache != null)
        {
            cacheKey = (knownSignature ?? StateSignature.Compute(gameState), currentDepth);
            if (context.Cache.TryGetValue(cacheKey, out CachedLine cached))
            {
                Interlocked.Increment(ref context.Stats.CacheHits);
                return cached.ToDecisionSet();
            }
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

        DecisionSet bestOptionSoFar = new DecisionSet(new List<PlayerDecision>(), -9999f);

        List<OptionSet> optionSets = ExpandOptions(gameState, options, context);
        SortOptions(optionSets);
        // Keep the top maxBranchSize options
        int branchSize = pickingAttacks ? 1 : maxBranchSize;
        int topCount = Mathf.Min(branchSize, optionSets.Count);

        for (int i = 0; i < topCount; i++)
        {
            OptionSet optionSet = optionSets[i];
            // Recursively explore future options, returning the best one
            DecisionSet bestOption = GetBestOptions(optionSet.GameState, context, currentDepth, optionSet.Signature);
            if (bestOption.Utility > bestOptionSoFar.Utility)
            {
                List<PlayerDecision> updatedList = bestOption.Decisions;
                updatedList.Insert(0, optionSet.PlayerDecision);
                bestOptionSoFar = new DecisionSet(updatedList, bestOption.Utility);
            }

            if (context.StopOnLethal && float.IsPositiveInfinity(bestOptionSoFar.Utility))
            {
                if (i < topCount - 1) Interlocked.Increment(ref context.Stats.LethalCutoffs);
                break;
            }
        }

        foreach (OptionSet optionSet in optionSets)
        {
            context.Pool.Return(optionSet.GameState);
        }

        // A cancelled search may have skipped branches, so its result is not the true best for this state.
        if (context.Cache != null && !context.Token.IsCancellationRequested)
            context.Cache.TryAdd(cacheKey, new CachedLine(bestOptionSoFar));

        return bestOptionSoFar;
    }

    // Simulates each option on its own copy of the state and scores it.
    private static List<OptionSet> ExpandOptions(GameState gameState, List<PlayerDecision> options, SearchContext context)
    {
        List<OptionSet> optionSets = new List<OptionSet>(options.Count);
        bool needsSignature = context.Cache != null || context.DedupeSiblings;
        HashSet<ulong> siblingSignatures = context.DedupeSiblings ? new HashSet<ulong>() : null;

        for (int i = 0; i < options.Count; i++)
        {
            PlayerDecision option = options[i];
            GameState simulatedGameState = context.Pool.Get(gameState, true);
            Interlocked.Increment(ref context.Stats.StatesCopied);
            AIPlayer thisPlayer = simulatedGameState.GetPlayer(gameState.AI.PlayerID) as AIPlayer;
            bool result = thisPlayer.DoDecision(option);
            if (!result) Debug.LogError("Decision Failed");

            ulong? signature = null;
            if (needsSignature)
            {
                signature = StateSignature.Compute(simulatedGameState);
                if (siblingSignatures != null && !siblingSignatures.Add(signature.Value))
                {
                    context.Pool.Return(simulatedGameState);
                    Interlocked.Increment(ref context.Stats.DuplicateSiblings);
                    continue;
                }
            }

            float utility = GetUtility(simulatedGameState);
            optionSets.Add(new OptionSet(simulatedGameState, option, utility, i, signature));
        }

        return optionSets;
    }

    // Utility descending, ties in legal-move order, so dropping a duplicate sibling never reorders the rest.
    private static void SortOptions(List<OptionSet> optionSets)
    {
        optionSets.Sort((a, b) =>
        {
            int byUtility = b.Utility.CompareTo(a.Utility);
            return byUtility != 0 ? byUtility : a.Index.CompareTo(b.Index);
        });
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
        public int Index;
        public ulong? Signature;

        public OptionSet(GameState gameState, PlayerDecision playerDecision, float utility, int index, ulong? signature)
        {
            GameState = gameState;
            PlayerDecision = playerDecision;
            Utility = utility;
            Index = index;
            Signature = signature;
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
