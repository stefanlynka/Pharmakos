using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Forces the draws and enemy plays the tutorial beats describe.
/// Scripted draws are ignored while the AI is simulating a copy of the fight.
/// </summary>
public static class TutorialCombatScript
{
    public static EncounterType ActiveEncounter { get; private set; } = EncounterType.None;

    static readonly Dictionary<int, Queue<string>> DrawsByPlayer = new Dictionary<int, Queue<string>>();
    static bool openingHandAnnounced;
    static int openingCardsLeft;

    public static void Reset()
    {
        ActiveEncounter = EncounterType.None;
        DrawsByPlayer.Clear();
        openingHandAnnounced = false;
        openingCardsLeft = 0;
    }

    public static void ArmForCurrentFight(Player human, Player enemy)
    {
        Reset();
        if (Controller.Instance == null || !Controller.Instance.IsTutorialRun || human == null || enemy == null)
            return;
        if (Controller.Instance.ProgressionHandler == null)
            return;

        ProgressionHandler.DeckName deck = Controller.Instance.ProgressionHandler.CurrentEnemy;
        if (deck == ProgressionHandler.DeckName.TutorialCombat1)
        {
            ActiveEncounter = EncounterType.TutorialCombat1;
            QueueDraws(human, "Sheep", "Peltast", "Hoplite", "Chariot", "ThrowStone");
            QueueDraws(enemy, "Sheep", "Cyclops", "Sheep", "ThrowStone", "ThrowStone");
        }
        else if (deck == ProgressionHandler.DeckName.TutorialCombat2)
        {
            ActiveEncounter = EncounterType.TutorialCombat2;
            QueueDraws(enemy, "Satyr", "Boar", "Satyr", "Maenad", "Panic", "ThrowStone", "Panic");
        }
    }

    public static Card TryTake(Player player, List<Card> deck)
    {
        if (player == null || deck == null || deck.Count == 0)
            return null;
        if (!DrawsByPlayer.TryGetValue(player.PlayerID, out Queue<string> queue) || queue.Count == 0)
            return null;

        string wanted = queue.Peek();
        int index = deck.FindIndex(card => card != null && card.GetCardType() == wanted);
        if (index < 0)
        {
            Debug.LogWarning("TutorialCombatScript: " + wanted + " was not in " + player.Name + "'s deck. Drawing normally.");
            queue.Dequeue();
            return null;
        }

        queue.Dequeue();
        Card card = deck[index];
        deck.RemoveAt(index);
        return card;
    }

    public static void ExpectOpeningHand(Player player, int count)
    {
        if (player == null || !player.IsHuman || player.TurnNumber != 0 || openingHandAnnounced)
            return;
        if (ActiveEncounter != EncounterType.TutorialCombat1 || count <= 0)
            return;

        openingCardsLeft = count;
    }

    public static void OnCardEnteredHand(Player player)
    {
        if (openingCardsLeft <= 0 || player == null || !player.IsHuman)
            return;

        openingCardsLeft--;
        if (openingCardsLeft > 0)
            return;

        openingHandAnnounced = true;
        TutorialDirector.Raise(TutorialTrigger.HandDrawn, ActiveEncounter);
    }

    public static void OnHumanFollowerPlayed(Follower follower)
    {
        if (follower == null || follower.Owner == null || !follower.Owner.IsHuman)
            return;
        if (ActiveEncounter != EncounterType.TutorialCombat1)
            return;

        if (follower is Chariot)
            TutorialDirector.Raise(TutorialTrigger.FollowerPlayed, ActiveEncounter, 1);
        else if (!RowHas<Chariot>(follower.Owner))
            TutorialDirector.Raise(TutorialTrigger.FollowerPlayed, ActiveEncounter, 0);
    }

    public static void OnBoardChanged()
    {
        if (ActiveEncounter != EncounterType.TutorialCombat1)
            return;
        if (Controller.Instance == null || Controller.Instance.Player1 == null)
            return;

        Player human = Controller.Instance.Player1;
        bool canAttack = false;
        foreach (Follower follower in human.BattleRow.Followers)
        {
            if (follower != null && follower.CanAttack())
                canAttack = true;
        }

        if (!canAttack || AttackLessonPlayed())
            return;

        Player enemy = human.GetOtherPlayer();
        bool enemyHasFollowers = enemy != null && enemy.BattleRow.Followers.Count > 0;
        TutorialDirector.Raise(TutorialTrigger.FollowerCanAttack, ActiveEncounter, enemyHasFollowers ? 1 : 0);
    }

    static bool AttackLessonPlayed()
    {
        for (int i = 0; i < TutorialScript.Beats.Count; i++)
        {
            TutorialBeat beat = TutorialScript.Beats[i];
            if (beat.Trigger == TutorialTrigger.FollowerCanAttack && beat.Played)
                return true;
        }

        return false;
    }

    public static void OnHumanAttackResolved()
    {
        if (ActiveEncounter != EncounterType.TutorialCombat1)
            return;

        TutorialDirector.Raise(TutorialTrigger.AttackResolved, ActiveEncounter);
    }

    public static bool TryScriptTurn(AIPlayer ai, out List<PlayerDecision> decisions)
    {
        decisions = null;
        if (ai == null || ai.GameState == null || ai.GameState.IsSimulated)
            return false;

        if (ActiveEncounter == EncounterType.TutorialCombat1 && ai.TurnNumber == 1)
        {
            decisions = PlayableFollowers(ai);
            return true;
        }

        if (ActiveEncounter == EncounterType.TutorialCombat2 && ai.TurnNumber == 2)
        {
            if (!TryThrowStoneAtHuman(ai, out decisions))
            {
                Debug.LogWarning("TutorialCombatScript: Throw Stone was not in the enemy hand on turn 2.");
                return false;
            }

            return true;
        }

        return false;
    }

    static void QueueDraws(Player player, params string[] cardTypes)
    {
        var queue = new Queue<string>();
        for (int i = 0; i < cardTypes.Length; i++)
            queue.Enqueue(cardTypes[i]);
        DrawsByPlayer[player.PlayerID] = queue;
    }

    static bool RowHas<T>(Player player) where T : Follower
    {
        foreach (Follower follower in player.BattleRow.Followers)
        {
            if (follower is T)
                return true;
        }

        return false;
    }

    static List<PlayerDecision> PlayableFollowers(AIPlayer ai)
    {
        var decisions = new List<PlayerDecision>();
        int index = ai.BattleRow.Followers.Count;
        foreach (Card card in ai.Hand)
        {
            Follower follower = card as Follower;
            if (follower == null || !follower.CanPlay())
                continue;

            decisions.Add(new PlayFollowerDecision(follower.ID, index));
            index++;
        }

        return decisions;
    }

    static bool TryThrowStoneAtHuman(AIPlayer ai, out List<PlayerDecision> decisions)
    {
        decisions = null;
        Spell stone = null;
        foreach (Card card in ai.Hand)
        {
            if (card is ThrowStone spell && spell.CanPlay())
            {
                stone = spell;
                break;
            }
        }

        Player human = ai.GetOtherPlayer();
        if (stone == null || human == null)
            return false;

        decisions = new List<PlayerDecision>
        {
            new PlaySpellDecision(stone.ID, human.GetID()),
        };
        return true;
    }
}
