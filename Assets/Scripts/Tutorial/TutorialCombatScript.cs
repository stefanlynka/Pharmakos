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
            QueueOpeningHand(human);
            QueueDraws(enemy, "Sheep", "Cyclops", "Sheep", "ThrowStone", "ThrowStone");
        }
        else if (deck == ProgressionHandler.DeckName.TutorialCombat2)
        {
            ActiveEncounter = EncounterType.TutorialCombat2;
            QueueOpeningHand(human);
            QueueDraws(enemy, "Satyr", "Boar", "Satyr", "Maenad", "Panic", "ThrowStone", "Panic");
        }
    }

    public static int ScriptedDrawCount(Player player)
    {
        if (player == null || !player.IsHuman || player.TurnNumber != 0)
            return 0;
        if (ActiveEncounter != EncounterType.TutorialCombat1 && ActiveEncounter != EncounterType.TutorialCombat2)
            return 0;
        if (!DrawsByPlayer.TryGetValue(player.PlayerID, out Queue<string> queue) || queue.Count == 0)
            return 0;

        return queue.Count;
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
        if (ActiveEncounter != EncounterType.TutorialCombat1 && ActiveEncounter != EncounterType.TutorialCombat2)
            return;
        if (count <= 0)
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

    static void QueueOpeningHand(Player player)
    {
        QueueDraws(player, "Peltast", "Sheep", "Hoplite", "Chariot", "Blessing", "Smite");
    }

    static void QueueDraws(Player player, params string[] cardTypes)
    {
        var queue = new Queue<string>();
        var needed = new Dictionary<string, int>();
        for (int i = 0; i < cardTypes.Length; i++)
        {
            queue.Enqueue(cardTypes[i]);
            if (!needed.ContainsKey(cardTypes[i]))
                needed[cardTypes[i]] = 0;
            needed[cardTypes[i]]++;
        }

        DrawsByPlayer[player.PlayerID] = queue;
        foreach (KeyValuePair<string, int> pair in needed)
            EnsureDeckHas(player, pair.Key, pair.Value);
    }

    static void EnsureDeckHas(Player player, string cardType, int needed)
    {
        if (player == null || player.Deck == null || needed <= 0)
            return;

        int have = 0;
        for (int i = 0; i < player.Deck.Count; i++)
        {
            if (player.Deck[i] != null && player.Deck[i].GetCardType() == cardType)
                have++;
        }

        for (int i = have; i < needed; i++)
        {
            Card card = CreateScriptedCard(cardType);
            if (card == null)
                return;

            card.Init(player);
            player.Deck.Add(card);
        }
    }

    static Card CreateScriptedCard(string cardType)
    {
        switch (cardType)
        {
            case "Peltast": return new Peltast();
            case "Sheep": return new Sheep();
            case "Hoplite": return new Hoplite();
            case "Chariot": return new Chariot();
            case "Blessing": return new Blessing();
            case "Smite": return new Smite();
            default: return null;
        }
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
