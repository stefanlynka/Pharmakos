using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using System;

public class AIPlayer : Player
{
    public AITurnPhase TurnPhase = AITurnPhase.Setup;

    private DecisionSet playerDecisions = new DecisionSet();

    GameStatePool pool;
    private const int maxBranchSize = 4;

    public enum AITurnPhase
    {
        Setup,
        Preparing,
        Executing,
        Ending
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

        base.StartTurn();
    }

    private Task planningTask = null;
    private volatile bool planningCompleted = false;
    private CancellationTokenSource planningTimeoutCts = null;
    private CancellationTokenSource planningLinkedCts = null;

    public override void RunUpdate()
    {
        base.RunUpdate();
        if (!IsMyTurn) return;

        switch (TurnPhase)
        {
            case AITurnPhase.Setup:
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

                if (Controller.AIDebugMode)
                {
                    PlanActionsSync();
                    planningCompleted = true;
                }
                else
                {
                    planningTask = Task.Run(() => PlanActions(planningLinkedCts.Token), planningLinkedCts.Token);

                    // Start a timer to enforce the 5 second timeout
                    Task.Run(async () =>
                    {
                        try
                        {
                            var completedTask = await Task.WhenAny(planningTask, Task.Delay(5000, planningLinkedCts.Token));
                            if (completedTask != planningTask)
                            {
                                // Timeout occurred
                                planningTimeoutCts.Cancel();
                                Debug.LogWarning("AI planning timed out. Forcing simple actions.");
                                // Ensure ForceChooseSimpleActions runs on main thread
                                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                                {
                                    ForceChooseSimpleActions();
                                    planningCompleted = true;
                                });
                            }
                        }
                        catch (TaskCanceledException) { }
                    });
                }
                break;
            case AITurnPhase.Preparing:
                if (planningCompleted)
                {
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
                }

                TurnPhase = AITurnPhase.Ending;
                break;
            case AITurnPhase.Ending:
                Debug.LogWarning("Ending AI Turn");
                TurnPhase = AITurnPhase.Setup;
                var endTurnAction = new TryEndTurnAction(GameState.CurrentPlayer);
                GameState.ActionHandler.AddAction(endTurnAction);

                break;
        }
    }


    private void PlanActionsSync()
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        playerDecisions = new DecisionSet(new List<PlayerDecision>(), 0);
        pool = new GameStatePool();
        playerDecisions = GetBestOptions(GameState, pool, Controller.Instance.CancellationTokenSource.Token, 0);
        Debug.Log($"[AI] PlanActions (sync) took {stopwatch.ElapsedMilliseconds / 1000f} seconds");
        planningCompleted = true;
    }

    private void PlanActions(CancellationToken cancellationToken)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        playerDecisions = new DecisionSet(new List<PlayerDecision>(), 0);
        pool = new GameStatePool();
        playerDecisions = GetBestOptions(GameState, pool, cancellationToken, 0);
        stopwatch.Stop();
        Debug.Log($"[AI] PlanActions (async) took {stopwatch.ElapsedMilliseconds/1000f} seconds");
        planningCompleted = true;
    }

    private void ForceChooseSimpleActions()
    {
        // This is a fallback method to force the AI to choose simple actions
        // when the planning is taking too long or is cancelled.
        playerDecisions = new DecisionSet(new List<PlayerDecision>(), 0);
        playerDecisions.Decisions.Add(new SkipFollowerAttackDecision(-1)); // Just skip attacks
        Debug.LogWarning("Forced simple actions due to planning timeout or cancellation.");
    }

    static int deepestDepth = 12;
    public static DecisionSet GetBestOptions(GameState gameState, GameStatePool pool, CancellationToken cancellationToken, int currentDepth)
    {
        if (cancellationToken.IsCancellationRequested) return new DecisionSet(new List<PlayerDecision>(), float.NegativeInfinity);

        var options = new List<PlayerDecision>();

        currentDepth++;
        if (currentDepth > deepestDepth)
        {
            // If we've reached the maximum depth, return a utility value
            float utility = GetUtility(gameState);
            return new DecisionSet(new List<PlayerDecision>(), utility);
        }

        List<Card> playableCards = gameState.AI.GetPlayableCards();
        Dictionary<string, Follower> followersByName = new Dictionary<string, Follower>();
        Dictionary<string, Spell> spellsByName = new Dictionary<string, Spell>();
        Dictionary<string, Follower> freeFollowersByName = new Dictionary<string, Follower>();
        Dictionary<string, Spell> freeSpellsByName = new Dictionary<string, Spell>();

        // Sort playable cards into followers and spells, separating free ones
        foreach (Card card in playableCards)
        {
            Follower follower = card as Follower;
            Spell spell = card as Spell;
            if (follower != null)
            {
                if (follower.Costs[OfferingType.Gold] == 0)
                {
                    freeFollowersByName[follower.GetCardType()] = follower;
                }
                else
                {
                    followersByName[follower.GetCardType()] = follower;
                }
            }
            else if (spell != null)
            {
                if (spell.Costs[OfferingType.Gold] == 0)
                {
                    freeSpellsByName[spell.GetCardType()] = spell;
                }
                else
                {
                    spellsByName[spell.GetCardType()] = spell;
                }
            }
        }



        // Get all options for this player in this GameState
        bool pickingAttacks = false;
        if (playableCards.Count > 0)
        {
            if (followersByName.Count > 0 || spellsByName.Count > 0)
            {
                // Play Followers
                foreach (Follower follower in followersByName.Values)
                {
                    PlayerDecision playFollower = new PlayFollowerDecision(follower.ID, gameState.AI.BattleRow.Followers.Count);
                    options.Add(playFollower);
                }

                // Play Spells
                foreach (Spell spell in spellsByName.Values)
                {
                    List<ITarget> possibleTargets = spell.GetTargets();
                    foreach (ITarget target in possibleTargets)
                    {
                        int spellTargetID = target.GetID();
                        PlaySpellDecision playSpell = new PlaySpellDecision(spell.ID, spellTargetID);
                        options.Add(playSpell);
                    }
                }
            }
            else
            {
                // Play Free Followers
                foreach (Follower follower in freeFollowersByName.Values)
                {
                    PlayerDecision playFollower = new PlayFollowerDecision(follower.ID, gameState.AI.BattleRow.Followers.Count);
                    options.Add(playFollower);
                }
                // Play Free Spells
                foreach (Spell spell in freeSpellsByName.Values)
                {
                    List<ITarget> possibleTargets = spell.GetTargets();
                    foreach (ITarget target in possibleTargets)
                    {
                        int spellTargetID = target.GetID();
                        PlaySpellDecision playSpell = new PlaySpellDecision(spell.ID, spellTargetID);
                        options.Add(playSpell);
                    }
                }
            }
        }
        else if (gameState.AI.MajorRitual != null && gameState.AI.MajorRitual.CanPlay())
        {
            List<ITarget> possibleTargets = gameState.AI.MajorRitual.GetTargets();
            foreach (ITarget target in possibleTargets)
            {
                int ritualTargetID = target.GetID();
                UseRitualDecision useRitualDecision = new UseRitualDecision(gameState.AI.MajorRitual.ID, ritualTargetID);
                options.Add(useRitualDecision);
            }
        }
        else if (gameState.AI.MinorRitual != null && gameState.AI.MinorRitual.CanPlay())
        {
            List<ITarget> possibleTargets = gameState.AI.MinorRitual.GetTargets();
            foreach (ITarget target in possibleTargets)
            {
                int ritualTargetID = target.GetID();
                UseRitualDecision useRitualDecision = new UseRitualDecision(gameState.AI.MinorRitual.ID, ritualTargetID);
                options.Add(useRitualDecision);
            }
        }
        else
        {
            pickingAttacks = true;
            // Attack with Followers in order
            Follower followerThatCanAttack = gameState.AI.BattleRow.GetFirstFollowerThatCanAttack();
            if (followerThatCanAttack != null)
            {
                List<ITarget> attackTargets = followerThatCanAttack.GetAttackTargets();
                foreach (ITarget attackTarget in attackTargets)
                {
                    int targetID = attackTarget.GetID();
                    AttackWithFollowerDecision attackDecision = new AttackWithFollowerDecision(followerThatCanAttack.ID, attackTarget.GetID());
                    options.Add(attackDecision);
                }

                SkipFollowerAttackDecision skipAttackDecision = new SkipFollowerAttackDecision(followerThatCanAttack.ID);
                options.Add(skipAttackDecision);
            }
        }


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

        // Positive utility
        Player thisPlayer = gameState.GetPlayer(gameState.AI.PlayerID);
        foreach (Follower follower in thisPlayer.BattleRow.Followers)
        {
            utility += follower.GetCurrentAttack();
            utility += follower.CurrentHealth;
            utility += follower.InherentValue;
        }
        utility += thisPlayer.Health;
        utility += thisPlayer.PlayerEffects.Count * 15;

        // Negative utility
        Player otherPlayer = gameState.GetOtherPlayer(gameState.AI.PlayerID);
        foreach (Follower follower in otherPlayer.BattleRow.Followers)
        {
            utility -= follower.GetCurrentAttack()*2;
            utility -= follower.CurrentHealth*2;
        }
        utility -= otherPlayer.Health;

        return utility;
    }
}
