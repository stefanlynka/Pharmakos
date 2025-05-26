using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardHandler;

public class ProgressionHandler
{
    public enum DeckName
    {
        None,
        PlayerStarterDeck,
        TestDeck,
        Cyclops,
        Bacchanalia,
        Labyrinth,
        Troy,
        Hunt,
        SeasideCliffs,
        Caves,
        Trials,
        Delphi,
        Underworld,
    }

    public int CurrentLevel = 0;
    public DeckName CurrentEnemy = DeckName.None;
    public int CurrentPool { get { return Mathf.CeilToInt(CurrentLevel / 3f); } }

    public Dictionary<DeckName, PlayerDetails> DetailsByDeckName = new Dictionary<DeckName, PlayerDetails>();
    public Dictionary<int, List<DeckName>> EnemyPools = new Dictionary<int, List<DeckName>>();

    List<Ritual> ritualRewards = new List<Ritual>();

    List<StarterBundle> starterBundles = new List<StarterBundle>();

    public ProgressionHandler()
    {
        DetailsByDeckName[DeckName.PlayerStarterDeck] = new PlayerDetails
        {
            IsEnemy = false,
            BaseHealth = 10,
            Pool = 0,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new Peltast(),
                new Peltast(),
                new Sheep(),
                new Ekdromos(),
                new Ekdromos(),
                new Thureophoros(),
                new Thureophoros(),
                new Chariot(),
                new Chariot(),
                new Chariot(),
                new Myrmidon(),
                new PriceOfProfit(),
                new PriceOfProfit(),
                new Blessing(),
                new Blessing(),
                //new Smite(),
                //new Smite(),
                //new Panic(),
                //new Panic(),
                //new Panic(),
                //new Panic(),
                //new Panic(),
            }
        };

        DetailsByDeckName[DeckName.Cyclops] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 1,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new Sheep(),
                new Sheep(),
                new Sheep(),
                new Sheep(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new Cyclops(),
                new PriceOfReprisal(),
                new PriceOfReprisal(),
                new ThrowStone(),
                new ThrowStone(),
            },
            Rewards = new List<Card>
            {
                new Sheep(),
                new Cyclops(),
                new PriceOfReprisal(),
                new ThrowStone(),
                new Phalangite(),
                new Endymion(),
            }
        };
        DetailsByDeckName[DeckName.Bacchanalia] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 1,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                //new Pan(),
                //new Boar(),
                //new Boar(),
                //new Boar(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Maenad(),
                //new Maenad(),
                //new Maenad(),
                //new Maenad(),
                //new Panic(),
                //new Panic(),
                //new Panic(),
                //new Reverie(),
                //new Reverie(),
                //new Reverie(),
                //new PriceOfProfit(),
                //new PriceOfProfit(),
                //new PriceOfProfit(),
                //new PriceOfProfit(),
                //new PriceOfProfit(),
                //new PriceOfProfit(),
                //new PriceOfProfit(),
                //new PriceOfProfit(),
                //new PriceOfProfit(),
                //new Boar(),
                //new Boar(),
                //new Boar(),
                //new Boar(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Peltast(),
                //new Peltast(),
                //new Peltast(),
                //new PriceOfInspiration(),




                new Pan(),
                new Boar(),
                new Boar(),
                new Boar(),
                new Satyr(),
                new Satyr(),
                new Satyr(),
                new Satyr(),
                new Maenad(),
                new Maenad(),
                new Maenad(),
                new Maenad(),
                new Panic(),
                new Panic(),
                new Panic(),
                new Reverie(),
                new Reverie(),
                new Reverie(),
                new PriceOfInspiration(),
                new PriceOfInspiration(),
            },
            Rewards = new List<Card>
            {
                new Pan(),
                new Boar(),
                new Satyr(),
                new Maenad(),
                new Panic(),
                new Reverie(),
                new PriceOfInspiration(),
                new Medea(),
                new Podalirius(),
            }
        };
        DetailsByDeckName[DeckName.Labyrinth] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 1,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new Minotaur(),
                new Minotaur(),
                new Minotaur(),
                new Corridor(),
                new Corridor(),
                new Corridor(),
                new Corridor(),
                new Corridor(),
                new Corridor(),
                new Corridor(),
                new Corridor(),
                new Rat(),
                new Rat(),
                new Rat(),
                new Icarus(),
                new Contraption(),
                new Contraption(),
                new Contraption(),
                new Contraption(),
                new Contraption(),
            },
            Rewards = new List<Card>
            {
                new Minotaur(),
                new Cyclops(),
                new Rat(),
                new Icarus(),
                new Contraption(),
                new Calchas(),
                new PriceOfKnowledge(),
                new HarpeOfPerseus(),
            }
        };

        DetailsByDeckName[DeckName.Troy] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 2,
            MinorRitual = null,
            MajorRitual = null,
            // This one has to be conditional
            DeckBlueprint = new List<Card>
            {
                new WallOfTroy(),
                new WallOfTroy(),
                new WallOfTroy(),
                new WallOfTroy(),
                new WallOfTroy(),
                new Toxotes(),
                new Toxotes(),
                new Toxotes(),
                new Toxotes(),
                new Hoplite(),
                new Hoplite(),
                new Ekdromos(),
                new Ekdromos(),
                new Hippeis(),
                new Hippeis(),
                new Chariot(),
                new Hector(),
                new Paris(),
                new Sarpedon(),
                new Cassandra(),
            },
            Rewards = new List<Card>
            {
                new WallOfTroy(),
                new Toxotes(),
                new Hoplite(),
                new Ekdromos(),
                new Chariot(),
                new Hector(),
                new Paris(),
                new Sarpedon(),
                new Cassandra(),

                new TrojanHorse(),
                new Patroclus(),
                new Achilles(),
                new Agamemnon(),
                new Menelaus(),
                new Pyrrhus(),
                new Diomedes(),
                new ShieldOfAjax(),
            }
        };
        DetailsByDeckName[DeckName.Hunt] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 2,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new Boar(),
                new Boar(),
                new Boar(),
                new Chariot(),
                new Chariot(),
                new Hippeis(),
                new Hippeis(),
                new Hippeis(),
                new Atalanta(),
                new Endymion(),
                new Hippolyta(),
                new Melpomene(),
                new Lightning(),
                new Lightning(),
                new ReleasePrey(),
                new ReleasePrey(),
                new ReleasePrey(),
                new PriceOfWealth(),
                new PriceOfWealth(),
                new PriceOfWealth(),
            },
            Rewards = new List<Card>
            {
                new Boar(),
                new Chariot(),
                new Atalanta(),
                new Endymion(),
                new Hippolyta(),
                new Melpomene(),
                new Lightning(),
                new ReleasePrey(),
                new PriceOfWealth(),
                new Asclepius(),
            }
        };
        DetailsByDeckName[DeckName.SeasideCliffs] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 2,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new Siren(),
                new Siren(),
                new Siren(),
                new Siren(),
                new Siren(),
                new Siren(),
                new Scylla(),
                new Charybdis(),
                new Hydra(),
                new Nereid(),
                new Nereid(),
                new Nereid(),
                new Nereid(),
                new Panic(),
                new Panic(),
                new Panic(),
                new PriceOfReprisal(),
                new PriceOfReprisal(),
                new Drown(),
                new Drown(),
            },
            Rewards = new List<Card>
            {
                new Siren(),
                new Scylla(),
                new Charybdis(),
                new Hydra(),
                new Nereid(),
                new Panic(),
                new PriceOfReprisal(),
                new Drown(),
                new Titanomachy(),
            }
        };

        DetailsByDeckName[DeckName.Caves] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 3,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new Rat(),
                new Rat(),
                new Pytho(),
                new Pytho(),
                new Lamia(),
                new Lamia(),
                new Chimera(),
                new Chimera(),
                new Sphinx(),
                new Sphinx(),
                new Empusa(),
                new Empusa(),
                new Gorgon(),
                new Gorgon(),
                new Lightning(),
                new Lightning(),
                new Panic(),
                new Panic(),
                new PriceOfRenewal(),
                new PriceOfRenewal(),
            },
            Rewards = new List<Card>
            {
                new Rat(),
                new Pytho(),
                new Lamia(),
                new Chimera(),
                new Sphinx(),
                new Empusa(),
                new Gorgon(),
                new Lightning(),
                new Panic(),
                new PriceOfRenewal(),
                new Typhon(),
                new Echidna(),
                new HarpeOfPerseus(),
            }
        };
        DetailsByDeckName[DeckName.Trials] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 3,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new NemeanLion(),
                new Hydra(),
                new GoldenHind(),
                new CreateFilth(),
                new CreateFilth(),
                new CreateFilth(),
                new StymphalianBird(),
                new StymphalianBird(),
                new StymphalianBird(),
                new MareOfDiomedes(),
                new MareOfDiomedes(),
                new MareOfDiomedes(),
                new Hippolyta(),
                new Amazon(),
                new Amazon(),
            },
            Rewards = new List<Card>
            {
                new NemeanLion(),
                new Hydra(),
                new GoldenHind(),
                new CreateFilth(),
                new StymphalianBird(),
                new MareOfDiomedes(),
                new Hippolyta(),
                new Amazon(),
                new StygianPact(),
            }
        };
        DetailsByDeckName[DeckName.Delphi] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 3,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new Helios(),
                new OracleOfDelphi(),
                new OracleOfDelphi(),
                new Ekdromos(),
                new Ekdromos(),
                new Ekdromos(),
                new Ekdromos(),
                new Sheep(),
                new Sheep(),
                new Sheep(),
                new Sheep(),
                new Endymion(),
                new Endymion(),
                new PriceOfProfit(),
                new PriceOfProfit(),
                new Talaria(),
                new Smite(),
                new Smite(),
                new Reflection(),
                new Reflection(),
            },
            Rewards = new List<Card>
            {
                new Helios(),
                new OracleOfDelphi(),
                new Ekdromos(),
                new Sheep(),
                new Endymion(),
                new PriceOfProfit(),
                new PriceOfKnowledge(),
                new Talaria(),
                new Smite(),
                new Reflection(),
            }
        };

        DetailsByDeckName[DeckName.Underworld] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 4,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                new Erinyes(),
                new Erinyes(),
                new Erinyes(),
                new Erinyes(),
                new Siren(),
                new Siren(),
                new Siren(),
                new Keres(),
                new Keres(),
                new Charon(),
                new Charon(),
                new Cerberus(),
                new Vengeance(),
                new Vengeance(),
                new RiverStyx(),
                new LastingGift(),
                new PriceOfProfit(),
                new PriceOfProfit(),
                new PriceOfLegacy(),
            },
            Rewards = new List<Card>
            {
                new Erinyes(),
                new Siren(),
                new Keres(),
                new Charon(),
                new Cerberus(),
                new Vengeance(),
                new RiverStyx(),
                new LastingGift(),
                new PriceOfProfit(),
                new PriceOfLegacy(),
            }
        };

        // Add enemy decks to their respective pool
        foreach (KeyValuePair<DeckName, PlayerDetails> kvp in DetailsByDeckName)
        {
            if (kvp.Value.IsEnemy)
            {
                if (!EnemyPools.ContainsKey(kvp.Value.Pool)) EnemyPools[kvp.Value.Pool] = new List<DeckName>();
                EnemyPools[kvp.Value.Pool].Add(kvp.Key);
            }
        }

        SetupStarterBundles();
    }

    private void SetupStarterBundles()
    {
        starterBundles.Clear();

        starterBundles.Add(new StarterBundle(
            new ZeusMinor(),
            new List<Card> { 
                new PriceOfReprisal(),
                new Patroclus(),
                new Atalanta(),
            }));
        starterBundles.Add(new StarterBundle(
            new HadesMinor(),
            new List<Card> {
                new Charon(),
                new RiverStyx(),
                new Vengeance(),
            }));
        starterBundles.Add(new StarterBundle(
            new AphroditeMinor(),
            new List<Card> {
                new Asclepius(),
                new Restoration(),
                new Lamia(),
            }));
        starterBundles.Add(new StarterBundle(
            new HermesMinor(),
            new List<Card> {
                new Talaria(),
                new PriceOfKnowledge(),
                new Melpomene(),
            }));
        starterBundles.Add(new StarterBundle(
            new HestiaMinor(),
            new List<Card> {
                new PriceOfLegacy(),
                new Hippolyta(),
                new DragonsTeeth(),
            }));
        
    }

    public List<Ritual> GetPossibleRitualRewards()
    {
        ritualRewards.Clear();

        if (CurrentLevel >= 0)
        {
            ritualRewards.Add(new AresMinor());
            ritualRewards.Add(new ApolloMinor());
            ritualRewards.Add(new AphroditeMinor());
            ritualRewards.Add(new DionysusMinor());
            ritualRewards.Add(new HermesMinor());
            ritualRewards.Add(new HestiaMinor());
            ritualRewards.Add(new HadesMinor());
            ritualRewards.Add(new DemeterMinor());
            ritualRewards.Add(new PoseidonMinor());
            ritualRewards.Add(new ZeusMinor());

            ritualRewards.Add(new AphroditeMajor());
            ritualRewards.Add(new AresMajor());
            ritualRewards.Add(new AthenaMajor());
            ritualRewards.Add(new DemeterMajor());
            ritualRewards.Add(new HadesMajor());
            ritualRewards.Add(new HephaestusMajor());
            ritualRewards.Add(new ZeusMajor());
        }
        if (CurrentLevel >= 2)
        {
        }

        return ritualRewards;
    }

    public List<Card> GetPossibleCardRewards()
    {
        List<Card> rewards = new List<Card>();

        if (!DetailsByDeckName.ContainsKey(CurrentEnemy)) Debug.LogError("Enemy: " + CurrentEnemy + " not found");
        PlayerDetails enemyDetails = DetailsByDeckName[CurrentEnemy];

        foreach (Card card in enemyDetails.Rewards)
        {
            rewards.Add(card.MakeBaseCopy());
        }

        return rewards;
    }

    public void Reset()
    {
        CurrentLevel = 0;
    }

    public void CurrentEnemyDefeated()
    {
        //if (!EnemyPools.ContainsKey(CurrentPool)) return;

        //EnemyPools[CurrentPool].Remove(CurrentEnemy);
    }
    public void SetupNextEnemy()
    {
        CurrentLevel++;
        int possibleEnemyCount = EnemyPools[CurrentPool].Count;
        while (possibleEnemyCount == 0)
        {
            CurrentLevel++;
            if (CurrentPool >= EnemyPools.Count)
            {
                Debug.LogError("You Win!");
                return;
            }
            possibleEnemyCount = EnemyPools[CurrentPool].Count;
        }

        int randomTarget = Controller.Instance.CanonGameState.RNG.Next(0, EnemyPools[CurrentPool].Count);
        CurrentEnemy = EnemyPools[CurrentPool][randomTarget];
        EnemyPools[CurrentPool].Remove(CurrentEnemy);
    }

    public void LoadPlayer(Player player, DeckName deckName)
    {
        if (!DetailsByDeckName.ContainsKey(deckName))
        {
            Debug.LogError("Player Not Found: " + deckName);
            return;
        }

        PlayerDetails newDetails = DetailsByDeckName[deckName];
        if (newDetails.IsEnemy) newDetails.BaseHealth = CurrentPool * 10 + ((CurrentLevel - 1) % 3) * 5; // 1

        player.LoadDetails(newDetails);
    }

    public void LoadEnemy(Player player)
    {
        LoadPlayer(player, CurrentEnemy);
    }

    public List<StarterBundle> GetStarterBundles()
    {
        List<StarterBundle> allBundles = new List<StarterBundle>(starterBundles);
        
        List<StarterBundle> bundles = new List<StarterBundle>();

        for (int i = 0; i < 3; i++)
        {
            int randIndex = Controller.Instance.CanonGameState.RNG.Next(0, allBundles.Count);
            StarterBundle starterBundle = allBundles[randIndex];
            bundles.Add(starterBundle);
            allBundles.RemoveAt(randIndex);
        }

        return bundles;
    }
}

