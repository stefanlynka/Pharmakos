using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.Progress;

public class AIPlayer : Player
{
    public AITurnPhase TurnPhase = AITurnPhase.Setup;

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

    public override void RunUpdate()
    {
        base.RunUpdate();
        if (!IsMyTurn) return;

        switch (TurnPhase)
        {
            case AITurnPhase.Setup:
                Debug.LogError("Start Planning AI Turn");
                //var t = Task.Run(() => PlanActions());
                PlanActions();
                TurnPhase = AITurnPhase.Preparing;
                break;
            case AITurnPhase.Preparing:
                //Debug.LogError("Thonking");
                break;
            case AITurnPhase.Executing:
                //Debug.LogError("Executing");
                TurnPhase = AITurnPhase.Ending;
                break;
            case AITurnPhase.Ending:
                //Debug.LogError("Ending");
                TurnPhase = AITurnPhase.Setup;
                GameState.EndTurn();
                break;
        }
    }

    private void PlanActions()
    {
        DecisionSet playerDecisions = GetBestOptions(GameState);

        foreach (PlayerDecision playerDecision in playerDecisions.Decisions)
        {
            DoDecision(playerDecision);
            Debug.LogError("Do Decision!");
            //Debug.Log(playerDecision.GetString());
        }

        TurnPhase = AITurnPhase.Executing;
    }

    //static int depth = 0;
    public DecisionSet GetBestOptions(GameState gameState)
    {
        var options = new List<PlayerDecision>();

        //depth++;
        //if (depth > 3) return new DecisionSet();

        List<Card> playableCards = GetPlayableCards();
        List<Follower> playableFollowers = new List<Follower>();
        List<Spell> playableSpells = new List<Spell>();

        foreach (Card card in playableCards)
        {
            Follower follower = card as Follower;
            Spell spell = card as Spell;
            if (follower != null)
            {
                playableFollowers.Add(follower);
                //PlayerDecision playFollower = new PlayFollowerDecision(follower.ID, BattleRow.Followers.Count);
                //bestOptions.Add(playFollower);
            }
            else if (spell != null)
            {
                playableSpells.Add(spell);
            }
        }



        // Get all options for this player in this GameState
        // if (playableFollowers.Count > 0)
        if (playableCards.Count > 0)
        {
            foreach (Follower follower in playableFollowers)
            {
                PlayerDecision playFollower = new PlayFollowerDecision(follower.ID, BattleRow.Followers.Count);
                options.Add(playFollower);
            }
        //}
        //else if (playableSpells.Count > 0)
        //{
            foreach (Spell spell in playableSpells)
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
            if (MajorRitual.CanPlay())
            {
                List<ITarget> possibleTargets = MajorRitual.GetTargets();
                foreach (ITarget target in possibleTargets)
                {
                    int ritualTargetID = target.GetID();
                    UseRitualDecision useRitualDecision = new UseRitualDecision(MajorRitual.ID, ritualTargetID);
                    options.Add(useRitualDecision);
                }
            }
            if (MinorRitual.CanPlay())
            {
                List<ITarget> possibleTargets = MinorRitual.GetTargets();
                foreach (ITarget target in possibleTargets)
                {
                    int ritualTargetID = target.GetID();
                    UseRitualDecision useRitualDecision = new UseRitualDecision(MinorRitual.ID, ritualTargetID);
                    options.Add(useRitualDecision);
                }
            }
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
            AIPlayer thisPlayer = simulatedGameState.GetPlayer(PlayerID) as AIPlayer;

            if (thisPlayer  == null)
            {
                Debug.LogError("ThisPlayer Should be AI");
                // HOWTO Debugger break
                //if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                // Should be AI player
            }

            // Apply this option to the simulatedGameState
            thisPlayer.DoDecision(option);

            // Recursively explore future options, returning the best one
            DecisionSet bestOption = thisPlayer.GetBestOptions(simulatedGameState);
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

    private float GetUtility(GameState gameState)
    {
        float utility = 0;

        // Positive utility
        Player thisPlayer = gameState.GetPlayer(PlayerID);
        foreach (Follower follower in thisPlayer.BattleRow.Followers)
        {
            utility += follower.CurrentAttack;
            utility += follower.CurrentHealth;
        }
        utility += thisPlayer.Health;

        // Negative utility
        Player otherPlayer = gameState.GetOtherPlayer(PlayerID);
        foreach (Follower follower in otherPlayer.BattleRow.Followers)
        {
            utility -= follower.CurrentAttack*2;
            utility -= follower.CurrentHealth*2;
        }
        utility -= otherPlayer.Health;

        return utility;
    }


    private List<Card> GetPlayableCards()
    {
        var playableCards = new List<Card>();

        foreach (Card card in Hand)
        {
            Follower follower = card as Follower;
            if (follower != null)
            {
                if (CanPlayCard(follower))
                {
                    playableCards.Add(follower);
                }
            }

            Spell spell = card as Spell;
            if (spell != null)
            {
                if (CanPlayCard(spell))
                {
                    playableCards.Add(spell);
                }
            }
        }

        return playableCards;
    }

}
