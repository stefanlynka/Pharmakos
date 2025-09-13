using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CardHandler
{
    public static List<List<Card>> Decks = new List<List<Card>>();

    public static List<Card> AllCards = new List<Card>
    {
        // OBJECTS
        new WallOfTroy(),
        new Corridor(),

        // BEASTS
        new Sheep(),
        new Boar(),
        new GoldenHind(),
        new NemeanLion(),
        new MareOfDiomedes(),
        new StymphalianBird(),

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
        new Phalangite(),
        new Satyr(),
        new Maenad(),
        new Nereid(),

        // MYTHS
        new Endymion(),
        new OracleOfDelphi(),
        new Charon(),
        new Calchas(),
        new Podalirius(),
        new Medea(),
        new Patroclus(),
        new Hippolyta(),
        new Amazon(),
        new Agamemnon(),
        new Menelaus(),
        new Pyrrhus(),
        new Diomedes(),
        new Icarus(),
        new Atalanta(),
        new Asclepius(),
        new Melpomene(),
        new Achilles(),
        new Paris(),
        new Hector(),
        new Sarpedon(),
        new Cassandra(),

        // DIVINE
        new Helios(),
        new Pan(),

        // MONSTERS
        new Pytho(),
        new Hydra(),
        new Minotaur(),
        new Scylla(),
        new Charybdis(),
        new Gorgon(),
        new Cerberus(),
        new Siren(),
        new Lamia(),
        new Chimera(),
        new Sphinx(),
        new Cyclops(),
        new Empusa(),
        new Keres(),
        new Erinyes(),
        new Typhon(),
        new Echidna(),

        // SPELLS
        new DragonsTeeth(),
        new ReleasePrey(),
        new CreateFilth(),
        new Smite(),
        new ThrowStone(),
        new Lightning(),
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
        new Drown(),

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
    public static List<Follower> SmallMonsters = new List<Follower>();
    public static List<Spell> AllSpells = new List<Spell>();
    public static Dictionary<int, List<Spell>> SpellsByCost = new Dictionary<int, List<Spell>>();
    public static int HighestSpellCost = 0;


    public static void LoadCards()
    {
        AllFollowers.Clear();
        AllMonsters.Clear();
        AllSpells.Clear();

        foreach (Card card in AllCards)
        {
            int cost = card.Costs[OfferingType.Gold];

            if (card is Follower follower)
            {
                AllFollowers.Add(follower);

                if (follower.Type == Follower.FollowerType.Monster)
                {
                    AllMonsters.Add(follower);
                    if (follower.Costs[OfferingType.Gold] <= 2) SmallMonsters.Add(follower);
                }
                else if (card is Spell spell)
                {
                    AllSpells.Add(spell);

                    if (!SpellsByCost.ContainsKey(cost))
                    {
                        SpellsByCost[cost] = new List<Spell>();
                    }
                    SpellsByCost[cost].Add(spell);
                    if (cost > HighestSpellCost) HighestSpellCost = cost;
                }
            }
        }
    }

    public static Sprite GetSprite(Card card)
    {
        string cardName = card.GetType().Name;
        return Resources.Load<Sprite>("Images/Cards/" + cardName);
    }

    public static Sprite GetSummaryIcon(Card card)
    {
        if (card.Icon == IconType.None) return null;

        string iconName = card.Icon.ToString().ToLower();
        return Resources.Load<Sprite>("Images/Icons/Summary/" + iconName);
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
            rewards.Add(new PoseidonMinor());
            rewards.Add(new HadesMajor());
        }
        if (level >= 2)
        {

        }

        return rewards;
    }
}
