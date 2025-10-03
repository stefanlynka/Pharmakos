using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardHandler;
using static ProgressionHandler;

public class ProgressionHandler
{
    public enum DeckName
    {
        None,
        PlayerStarterDeck,
        TestPlayer,
        TestEnemy,
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
        DetailsByDeckName[DeckName.TestEnemy] = new PlayerDetails
        {
            IsEnemy = true,
            BaseHealth = 100,
            Pool = 0,
            MinorRitual = null,
            MajorRitual = null, //new HadesMajor(),
            DeckBlueprint = new List<Card>
            {
                //new Ekdromos(),
                new Satyr(),
                //new Satyr(),
                //new Satyr(),
                //new Satyr(),
            },
            StartingBattleRow = new List<Follower>
            {
                new Peltast(),
                new Ekdromos(),
                new Myrmidon(),
            }
        };
        DetailsByDeckName[DeckName.TestPlayer] = new PlayerDetails
        {
            IsEnemy = false,
            BaseHealth = 100,
            CardsPerTurn = 5,
            Pool = 0,
            MinorRitual = new ZeusMinor(), //new AphroditeMajor(),
            MajorRitual = null, // new HephaestusMajor(), //new HadesMajor(),
            DeckBlueprint = new List<Card>
            {
                //new Sheep(),
                new Peltast(),
                new Confusion(),
                new Ekdromos(),
                new Chariot(),
                new PriceOfLegacy(),
                ////new Panic(),
            },
            StartingBattleRow = new List<Follower>
            {
                new Chariot(),
                //new Peltast(),
                //new Peltast(),
                //new Chariot(),
                //new Podalirius(),
                //new Ekdromos(),
                //new Chariot(),
            }
        };
        
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
                new Thureophoros(),
                new Thureophoros(),
                new Chariot(),
                new Chariot(),
                new Hippeis(),
                new Hippeis(),
                new Myrmidon(),
                new PriceOfProfit(),
                new PriceOfProfit(),
                new Blessing(),
                new Blessing(),
            }
        };

        DetailsByDeckName[DeckName.Cyclops] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 1,
            GoldPerTurn = 2,
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
                new Cyclops(),
                //new PriceOfReprisal(),
                new ThrowStone(),
                new ThrowStone(),
                new ThrowStone(),
            },
            StartingBattleRow = new List<Follower>
            {
                new Cyclops(),
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
            GoldPerTurn = 2,
            MinorRitual = null,
            MajorRitual = null,
            DeckBlueprint = new List<Card>
            {
                //new Pan(),
                //new Boar(),
                //new Boar(),
                new Boar(),
                new Satyr(),
                new Satyr(),
                new Satyr(),
                new Satyr(),
                new Satyr(),
                new Maenad(),
                new Maenad(),
                new Maenad(),
                new Maenad(),
                new Maenad(),
                new Panic(),
                new Panic(),
                new Panic(),
                new Panic(),
                new Panic(),
                new Reverie(),
                new Restoration(),
                new Restoration(),
                new Restoration(),
                //new PriceOfRenewal(),
                //new PriceOfRenewal(),
            },
            StartingBattleRow = new List<Follower>
            {
                new Satyr(),
                new Boar(),
            },
            Rewards = new List<Card>
            {
                new Pan(),
                new Boar(),
                new Satyr(),
                new Maenad(),
                new Panic(),
                new Reverie(),
                new Medea(),
                new Podalirius(),
                new PriceOfInspiration(),
                new PriceOfRenewal(),
                new LastingGift(),
            }
        };
        DetailsByDeckName[DeckName.Labyrinth] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 1,
            GoldPerTurn = 2,
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
                new Rat(),
                new Rat(),
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
            StartingBattleRow = new List<Follower>
            {
                new Corridor(),
                new Rat(),
                //new Corridor(),
            },
            Rewards = new List<Card>
            {
                new Corridor(),
                new Minotaur(),
                new Cyclops(),
                new Rat(),
                new Icarus(),
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
            StartingBattleRow = new List<Follower>
            {
                new WallOfTroy(),
                new Toxotes(),
                new WallOfTroy(),
            },
            Rewards = new List<Card>
            {
                new WallOfTroy(),
                new Toxotes(),
                //new Hoplite(),
                //new Ekdromos(),
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
            StartingBattleRow = new List<Follower>
            {
                new Prey1(),
                new Atalanta(),
                new Prey2(),
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
                new PriceOfKnowledge(),
                new PriceOfKnowledge(),
                new Drown(),
                new Drown(),
            },
            StartingBattleRow = new List<Follower>
            {
                new Siren(),
                new Siren(),
                new Siren(),
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
            MinorRitual = new OldOnesMinor(),
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
            StartingBattleRow = new List<Follower>
            {
                new Lamia(),
                new Gorgon(),
                new Lamia(),
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
            MinorRitual = new HeraMajor(),
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
            StartingBattleRow = new List<Follower>
            {
                new Amazon(),
                new Hippolyta(),
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
            MinorRitual = new ApolloMajor(),
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
            StartingBattleRow = new List<Follower>
            {
                new Sheep(),
                new OracleOfDelphi(),
                new Sheep(),

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
            GoldPerTurn = 3,
            MinorRitual = new HadesMajor(),
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
                new Keres(),
                new Keres(),
                new Charon(),
                new Charon(),
                new Cerberus(),
                new Vengeance(),
                new Vengeance(),
                //new RiverStyx(),
                new LastingGift(),
                //new PriceOfProfit(),
                new PriceOfProfit(),
                new PriceOfLegacy(),
            },
            StartingBattleRow = new List<Follower>
            {
                new Keres(),
                new Cerberus(),
                new Keres(),
                //new Charon(),
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
            new HadesMinor(), // HadesMinor
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
            ritualRewards.Add(new ApolloMajor());
            ritualRewards.Add(new OldOnesMinor());
            ritualRewards.Add(new HeraMajor());
            
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
    public void SetupNextEnemy(bool isTestChamber = false)
    {
        if (isTestChamber)
        {
            CurrentLevel++;
            CurrentEnemy = DeckName.TestEnemy;

            return;
        }

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

        int randomTarget = Controller.Instance.MetaRNG.Next(0, EnemyPools[CurrentPool].Count);
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
        if (newDetails.IsEnemy) newDetails.BaseHealth = CurrentPool * 10 + ((CurrentLevel - 1) % 3) * 5; 
        else newDetails.BaseHealth = GetPlayerHealth();

        player.LoadDetails(newDetails);
    }
    public List<Follower> GetPlayerStartingFollowers(DeckName deckName)
    {
        PlayerDetails newDetails = DetailsByDeckName[deckName];
        return newDetails.StartingBattleRow;
    }

    public void LoadEnemy(Player player)
    {
        LoadPlayer(player, CurrentEnemy);
    }

    public int GetPlayerHealth()
    {
        return 5 + CurrentPool * 5;
    }

    public List<StarterBundle> GetStarterBundles()
    {
        List<StarterBundle> allBundles = new List<StarterBundle>(starterBundles);
        
        List<StarterBundle> bundles = new List<StarterBundle>();

        for (int i = 0; i < 3; i++)
        {
            int randIndex = Controller.Instance.MetaRNG.Next(0, allBundles.Count);
            StarterBundle starterBundle = allBundles[randIndex];
            bundles.Add(starterBundle);
            allBundles.RemoveAt(randIndex);
        }

        return bundles;
    }
}

