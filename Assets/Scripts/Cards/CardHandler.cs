using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CardHandler
{
    public static List<List<Card>> Decks = new List<List<Card>>();

    public static List<Card> AllCards = new List<Card>
    {
        // BEASTS
        new Sheep(),

        // MORTALS
        new Peltast(),
        new Hoplite(),
        new Hippeis(),
        new Myrmidon(),
        new Ekdromos(),
        new Thureophoros(),
        new Toxotes(),
        new TrojanHorse(),
        new Chariot(),
        new Endymion(),
        new OracleOfDelphi(),
        new Charon(),
        new Calchas(),
        new Podalirius(),
        new Medea(),
        new Patroclus(),
        new Hippolyta(),
        new Agamemnon(),
        new Menelaus(),
        new Pyrrhus(),
        new Diomedes(),
        new Icarus(),
        new Atalanta(),
        new Asclepius(),
        new Melpomene(),

        // DIVINE
        new Helios(),

        // MONSTERS
        new Pytho(),
        new Hydra(),
        new Cerberus(),
        new Siren(),
        new Lamia(),
        new Chimera(),
        new Sphinx(),
        new Cyclops(),
        new Empusa(),
        new Keres(),
        new Typhon(),
        new Echidna(),

        // SPELLS
        new DragonsTeeth(),
        new Smite(),
        new Scry(),
        new Blessing(),
        new Talaria(),
        new HarpeOfPerseus(),
        new ShieldOfAjax(),
        new StygianPact(),
        new Restoration(),
        new Vengeance(),
        new LastingGift(),
        new RiverStyx(),
        new Reflection(),
        new Titanomachy(),
        // SACRIFICES
        new PriceOfProfit(),
        new PriceOfKnowledge(),
        new PriceOfInspiration(),
        new PriceOfWealth(),
        new PriceOfReprisal(),
        new PriceOfRenewal(),
        new PriceOfLegacy(),
    };
    public static List<Follower> AllFollowers = new List<Follower>();
    public static List<Follower> AllMonsters = new List<Follower>();
    public static List<Spell> AllSpells = new List<Spell>();

    public enum DeckName
    {
        PlayerStarterDeck,
        Monsters1,
        Monsters2,
        Monsters3,
    }

    public static void LoadCards()
    {
        AllFollowers.Clear();
        AllMonsters.Clear();
        AllSpells.Clear();

        foreach (Card card in AllCards)
        {
            if (card is Follower follower)
            {
                AllFollowers.Add(follower);

                if (follower.Type == Follower.FollowerType.Monster) AllMonsters.Add(follower);
            }
            else if (card is Spell spell)
            {
                AllSpells.Add(spell);
            }
        }
    }

    public static void LoadPlayer(Player player, DeckName name)
    {
        switch (name)
        {
            case DeckName.PlayerStarterDeck:
                {
                    player.IsHuman = true;
                    player.StartingHealth = 20;
                    player.MinorRitual = new DemeterMinor();
                    player.MajorRitual = new DemeterMajor();
                    player.Deck = new List<Card>
                    {
                        new Chariot(),
                        new Chariot(),
                        new Peltast(),
                        new Peltast(),
                        new Smite(),
                        //new Ekdromos(),
                        //new Ekdromos(),
                        //new Ekdromos(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                    };
                }
                break;
            case DeckName.Monsters1:
                {
                    player.StartingHealth = 5;
                    player.MinorRitual = null;
                    player.MajorRitual = null;
                    player.Deck = new List<Card>
                    {
                        new Peltast(),
                        new Hoplite(),
                        new Hippeis(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                        //new Hoplite(),
                    };
                }
                break;
            case DeckName.Monsters2:
                {
                    player.StartingHealth = 10;
                    player.MinorRitual = new ZeusMinor();
                    player.MajorRitual = null;
                    player.Deck = new List<Card>
                    {
                        new Chariot(),
                        new Chariot(),
                        new Chariot(),
                        new Chariot(),
                        new Chariot(),
                        new Chariot(),
                        new Chariot(),
                        new Chariot(),
                        new Chariot(),
                        new Chariot(),
                    };
                }
                break;
            case DeckName.Monsters3:
                {
                    player.StartingHealth = 15;
                    player.MinorRitual = null;
                    player.MajorRitual = new ZeusMajor();
                    player.Deck = new List<Card>
                    {
                        new Sphinx(),
                        new Sphinx(),
                        new Sphinx(),
                        new Sphinx(),
                        new Sphinx(),
                        new Sphinx(),
                        new Sphinx(),
                        new Sphinx(),
                        new Sphinx(),
                        new Sphinx(),
                    };
                }
                break;
        }
    }

    public static List<Card> GetDeck(DeckName name)
    {
        List<Card> deck;
        switch (name)
        {
            case DeckName.PlayerStarterDeck:
                deck = new List<Card>
                {
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Smite(),
                    new Smite(),
                    new Smite(),
                    new Smite(),
                };
                return deck;
            case DeckName.Monsters1:
                deck = new List<Card>
                {
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    //new DragonsTeeth(),

                };
                return deck;
            case DeckName.Monsters2:
                deck = new List<Card>
                {
                    new Chariot(),
                    new Chariot(),
                    new Chariot(),
                    new Chariot(),
                    new Chariot(),
                    new Chariot(),
                    new Chariot(),
                    new Chariot(),
                };
                return deck;
            case DeckName.Monsters3:
                deck = new List<Card>
                {
                    new Sphinx(),
                    new Sphinx(),
                    new Sphinx(),
                    new Sphinx(),
                    new Sphinx(),
                    new Sphinx(),
                    new Sphinx(),
                    new Sphinx(),
                };
                return deck;
        }
        return new List<Card>();
    }

    public static Sprite GetSprite(Card card)
    {
        string cardName = card.GetType().Name;
        return Resources.Load<Sprite>("Images/Cards/" + cardName);
    }

    public static List<Card> GetPossibleCardRewards(int level)
    {
        List<Card> rewards = new List<Card>();
        if (level >= 0)
        {
            rewards.Add(new Hoplite());
            rewards.Add(new Smite());
            rewards.Add(new Chariot());
        }
        if (level >= 2)
        {
            rewards.Add(new DragonsTeeth());
            rewards.Add(new Sphinx());
            rewards.Add(new Cyclops());
        }
        
        return rewards;
    }

    public static List<Ritual> GetPossibleRitualRewards(int level)
    {
        List<Ritual> rewards = new List<Ritual>();

        if (level >= 0)
        {
            rewards.Add(new HadesMinor());
            rewards.Add(new HadesMajor());
        }
        if (level >= 2)
        {

        }

        return rewards;
    }

    public static DeckName GetCurrentEnemyDeckName(int level)
    {
        switch (level)
        {
            case 0:
                return DeckName.Monsters1;
            case 1:
                return DeckName.Monsters2;
            case 2:
                return DeckName.Monsters3;
            default:
                return DeckName.Monsters1;
        }
    }

}
