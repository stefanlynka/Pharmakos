using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEditor.Playables;

public class AIPlayer : Player
{
    public AITurnPhase TurnPhase = AITurnPhase.Setup;

    private DecisionSet playerDecisions = new DecisionSet();

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
        base.StartTurn();
    }

    private Task planningTask = null;
    private bool planningCompleted = false;
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
                planningTask = Task.Run(() => PlanActions());

                //PlanActions();
                break;
            case AITurnPhase.Preparing:
                if (planningCompleted)
                {
                    Debug.LogWarning("Planning Completed: Preparing");
                    TurnPhase = AITurnPhase.Executing;
                    planningTask = null; // Clear the task reference
                    planningCompleted = false; // Reset the flag
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
                    //Debug.LogError("Do Decision!");
                    //Debug.Log(playerDecision.GetString());
                }

                TurnPhase = AITurnPhase.Ending;
                break;
            case AITurnPhase.Ending:
                Debug.LogWarning("Ending AI Turn");
                TurnPhase = AITurnPhase.Setup;
                GameState.EndTurn();
                break;
        }
    }


    private void PlanActions()
    {
        Debug.Log("PlanActions started on thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
        playerDecisions = new DecisionSet(new List<PlayerDecision>(), 0);
        //System.Threading.Thread.Sleep(2000);
        playerDecisions = GetBestOptions(GameState);
        planningCompleted = true;
        Debug.Log("PlanActions completed on thread: " + System.Threading.Thread.CurrentThread.ManagedThreadId);
    }

    //static int deepestDepth = 0;
    public static DecisionSet GetBestOptions(GameState gameState)
    {
        var options = new List<PlayerDecision>();

        //// Try playing cards
        //if (!gameState.AI.DoneWithPlayingCards)
        //{

        //}

        List<Card> playableCards = gameState.AI.GetPlayableCards();
        Dictionary<string, Follower> followersByName = new Dictionary<string, Follower>();
        Dictionary<string, Spell> spellsByName = new Dictionary<string, Spell>();
        //List<Follower> playableFollowers = new List<Follower>();
        //List<Spell> playableSpells = new List<Spell>();

        foreach (Card card in playableCards)
        {
            Follower follower = card as Follower;
            Spell spell = card as Spell;
            if (follower != null)
            {
                followersByName[follower.GetName()] = follower;
                //PlayerDecision playFollower = new PlayFollowerDecision(follower.ID, BattleRow.Followers.Count);
                //bestOptions.Add(playFollower);
            }
            else if (spell != null)
            {
                spellsByName[spell.GetName()] = spell;
            }
        }



        // Get all options for this player in this GameState
        // if (playableFollowers.Count > 0)
        if (playableCards.Count > 0)
        {
            foreach (Follower follower in followersByName.Values)
            {
                PlayerDecision playFollower = new PlayFollowerDecision(follower.ID, gameState.AI.BattleRow.Followers.Count);
                options.Add(playFollower);
            }
        //}
        //else if (playableSpells.Count > 0)
        //{
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

            // Attack with Followers in any order
            //List<Follower> followersThatCanAttack = gameState.AI.BattleRow.GetFollowersThatCanAttack();
            //foreach (Follower follower in followersThatCanAttack)
            //{
            //    List<ITarget> attackTargets = follower.GetAttackTargets();
            //    foreach (ITarget attackTarget in attackTargets)
            //    {
            //        int targetID = attackTarget.GetID();
            //        AttackWithFollowerDecision attackDecision = new AttackWithFollowerDecision(follower.ID, attackTarget.GetID());
            //        options.Add(attackDecision);
            //    }
            //    SkipFollowerAttackDecision skipAttackDecision = new SkipFollowerAttackDecision(follower.ID);
            //    options.Add(skipAttackDecision);
            //}
        }


        // If we're out of options, calculate this state's utility and return up the recursive chain
        if (options.Count == 0)
        {
            float utility = GetUtility(gameState);
            return new DecisionSet(new List<PlayerDecision>(), utility);
        }


        // Recursively test all options
        DecisionSet bestOptionSoFar = new DecisionSet(new List<PlayerDecision>(), -9999f);
        foreach (PlayerDecision option in options)
        {
            GameState simulatedGameState = new GameState(gameState, true);

            AIPlayer thisPlayer = simulatedGameState.GetPlayer(gameState.AI.PlayerID) as AIPlayer;

            if (thisPlayer == null)
            {
                Debug.LogError("ThisPlayer Should be AI");
                // HOWTO Debugger break
                //if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }

            // Apply this option to the simulatedGameState
            bool result = thisPlayer.DoDecision(option);
            if (!result)
            {
                Debug.LogError("Decision Failed");
            }

            // Recursively explore future options, returning the best one
            DecisionSet bestOption = GetBestOptions(simulatedGameState);
            if (bestOption.Utility > bestOptionSoFar.Utility)
            {
                List<PlayerDecision> updatedList = bestOption.Decisions;
                updatedList.Insert(0, option);
                bestOptionSoFar = new DecisionSet(updatedList, bestOption.Utility);
            }
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

    private static float GetUtility(GameState gameState)
    {
        float utility = 0;

        // Positive utility
        Player thisPlayer = gameState.GetPlayer(gameState.AI.PlayerID);
        foreach (Follower follower in thisPlayer.BattleRow.Followers)
        {
            utility += follower.GetCurrentAttack();
            utility += follower.CurrentHealth;
        }
        utility += thisPlayer.Health;

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
