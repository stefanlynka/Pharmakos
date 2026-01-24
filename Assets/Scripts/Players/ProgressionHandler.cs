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
    //public Dictionary<int, List<DeckName>> EnemyPools = new Dictionary<int, List<DeckName>>();

    public List<DeckName> EnemyPool = new List<DeckName>();
    public List<DeckName> BossPool = new List<DeckName>();

    List<Ritual> ritualRewards = new List<Ritual>();

    List<StarterBundle> starterBundles = new List<StarterBundle>();

    List<Trinket> availableTrinkets = new List<Trinket>();

    public ProgressionHandler()
    {
        DetailsByDeckName[DeckName.TestEnemy] = new PlayerDetails
        {
            IsEnemy = true,
            IsFightableEnemy = false,
            BaseHealth = 100,
            Pool = 1,
            PortraitName = "Underworld",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() { new TheLostReturnTrinket() },
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new GrowingBountiesTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 1,
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    new Helios(),
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    //new Peltast(),
                    new Hoplite(),
                    new Prey1(),
                    new Siren(),
                }
            }
        };
        DetailsByDeckName[DeckName.TestPlayer] = new PlayerDetails
        {
            IsEnemy = false,
            IsFightableEnemy = false,
            BaseHealth = 100,
            CardsPerTurn = 5,
            GoldPerTurn = 5,
            Pool = 0,
            PortraitName = "Player",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [0] = new DemeterMinor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [0] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [0] = new List<Trinket>()
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [0] = new List<Card>
                {
                    //new DevKill(),
                    new ThrowStone(),
                    //new Blessing(),
                    new Talaria(),
                    new Peltast(),
                    new PriceOfProfit(),
                    //new Endymion(),
                },
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [0] = new List<Follower>
                {
                    new Ekdromos(),
                    new Chariot(),
                    new Ekdromos(),
                }
            }
        };
        
        DetailsByDeckName[DeckName.PlayerStarterDeck] = new PlayerDetails
        {
            IsEnemy = false,
            IsFightableEnemy = false,
            BaseHealth = 10,
            Pool = 0,
            GoldPerTurn = Controller.BlitzMode ? 4 : 3,
            PortraitName = "Player",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [0] = null
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [0] = null
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [0] = new List<Trinket>()
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [0] = new List<Card>
                {
                    new Peltast(),
                    new Sheep(),
                    new Ekdromos(),
                    new Hoplite(),
                    new Thureophoros(),
                    new Chariot(),
                    new Chariot(),
                    new Hippeis(),
                    new Hippeis(),
                    new Myrmidon(),
                    new PriceOfProfit(),
                    new PriceOfProfit(),
                    new Blessing(),
                    new ThrowStone(),
                    new Scry(),
                }
            }
        };

        DetailsByDeckName[DeckName.Cyclops] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 1,
            //GoldPerTurn = Controller.BlitzMode ? 3 : 2,
            PortraitName = "Cyclops",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new ZeusMajor(),
                [3] = new ZeusMajor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() {  },
                [2] = new List<Trinket>() { new CyclopsEyeTrinket() },
                [3] = new List<Trinket>() { new CyclopsEyeTrinket() }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new CyclopsEyeTrinket() },
                [2] = new List<Trinket>() { new CyclopsEyeTrinket() },
                [3] = new List<Trinket>() { new CyclopsEyeTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 3,
                [3] = 2
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
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
                [2] = new List<Card>
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
                    new Cyclops(),
                    new PriceOfReprisal(),
                    //new ThrowStone(),
                    new ThrowStone(),
                    new ThrowStone(),
                },
                [3] = new List<Card>
                {
                    new Sheep(),
                    new Sheep(),
                    new Sheep(),
                    new Endymion(),
                    new Endymion(),
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
                    new PriceOfReprisal(),
                    new PriceOfReprisal(),
                    new ThrowStone(),
                    new ThrowStone(),
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new Cyclops(),
                },
                [2] = new List<Follower>
                {
                    new Sheep(),
                    new Cyclops(),
                    new Sheep(),
                },
                [3] = new List<Follower>
                {
                    new Sheep(),
                    new Cyclops(),
                    new Cyclops(),
                    new Sheep(),
                }
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
            //GoldPerTurn = Controller.BlitzMode ? 3 : 2,
            PortraitName = "Bacchanalia",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new DionysusMinor(),
                [3] = new DionysusMinor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() {  },
                [2] = new List<Trinket>() { new AresMinorTrinket() },
                [3] = new List<Trinket>() { new AresMinorTrinket() }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new PansFluteTrinket() },
                [2] = new List<Trinket>() { new PansFluteTrinket() },
                [3] = new List<Trinket>() { new PansFluteTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 3,
                [3] = 2
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
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
                },
                [2] = new List<Card>
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
                [3] = new List<Card>
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
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new Satyr(),
                    new Boar(),
                },
                [2] = new List<Follower>
                {
                    new Satyr(),
                    new Maenad(),
                    new Satyr(),
                },
                [3] = new List<Follower>
                {
                    new Pan(),
                }
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
            //GoldPerTurn = Controller.BlitzMode ? 3 : 2,
            PortraitName = "Labyrinth",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new HephaestusMajor(),
                [3] = new HephaestusMajor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() { },
                [2] = new List<Trinket>() { new TwistingCorridorsTrinket() },
                [3] = new List<Trinket>() { new TwistingCorridorsTrinket() }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new EverDeeperTrinket() },
                [2] = new List<Trinket>() { new EverDeeperTrinket() },
                [3] = new List<Trinket>() { new EverDeeperTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 3,
                [3] = 2
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    new Corridor(),
                    new Corridor(),
                    new Corridor(),
                    new Corridor(),
                    new Rat(),
                    new Rat(),
                    new Rat(),
                    new Rat(),
                    new Rat(),
                    new Rat(),
                    new Rat(),
                    new Icarus(),
                    new Minotaur(),
                    new Contraption(),
                    new Contraption(),
                    new Contraption(),
                    new Contraption(),
                    new Contraption(),
                },
                [2] = new List<Card>
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
                [3] = new List<Card>
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
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new Corridor(),
                    new Rat(),
                    //new Corridor(),
                },
                [2] = new List<Follower>
                {
                    new Minotaur(),
                },
                [3] = new List<Follower>
                {
                    new Corridor(),
                    new Minotaur(),
                    new Corridor(),
                }
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
            //GoldPerTurn = Controller.BlitzMode ? 4 : 3,
            PortraitName = "Troy",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new AresMajor(),
                [3] = new AresMajor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() { },
                [2] = new List<Trinket>() { new TheEndlessWallTrinket() },
                [3] = new List<Trinket>() { new TheEndlessWallTrinket() }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new AresMajorTrinket() },
                [2] = new List<Trinket>() { new AresMajorTrinket() },
                [3] = new List<Trinket>() { new AresMajorTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 3,
                [3] = 2
            },
            // This one has to be conditional
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    new WallOfTroy(),
                    new WallOfTroy(),
                    new Toxotes(),
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
                    new Myrmidon(),
                },
                [2] = new List<Card>
                {
                    new WallOfTroy(),
                    new WallOfTroy(),
                    new WallOfTroy(),
                    new WallOfTroy(),
                    new Toxotes(),
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
                [3] = new List<Card>
                {
                    new WallOfTroy(),
                    new WallOfTroy(),
                    new WallOfTroy(),
                    new WallOfTroy(),
                    new Toxotes(),
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
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new WallOfTroy(),
                    new Toxotes(),
                    new WallOfTroy(),
                },
                [2] = new List<Follower>
                {
                    new WallOfTroy(),
                    new Toxotes(),
                    new Toxotes(),
                    new WallOfTroy(),
                },
                [3] = new List<Follower>
                {
                    new WallOfTroy(),
                    new Achilles(),
                    new WallOfTroy(),
                }
            },
            Rewards = new List<Card>
            {
                //new WallOfTroy(),
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
            //GoldPerTurn = Controller.BlitzMode ? 4 : 3,
            PortraitName = "Hunt",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new AthenaMajor(), // TODO: Add ArtemisMajor. Whenever you cast a spell, summon a prey for target player
                [3] = new AthenaMajor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() { },
                [2] = new List<Trinket>() { new TheGreatHuntTrinket() },
                [3] = new List<Trinket>() { new TheGreatHuntTrinket() }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new GrowingBountiesTrinket() },
                [2] = new List<Trinket>() { new GrowingBountiesTrinket() },
                [3] = new List<Trinket>() { new GrowingBountiesTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 3,
                [3] = 2
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    new Boar(),
                    new Boar(),
                    new Boar(),
                    new Boar(),
                    new Chariot(),
                    new Hippeis(),
                    new Hippeis(),
                    new Hippeis(),
                    new ReleasePrey(),
                    new ReleasePrey(),
                    new ReleasePrey(),
                    new PriceOfWealth(),
                },
                [2] = new List<Card>
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
                [3] = new List<Card>
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
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new Prey1(),
                    new Boar(),
                    new Prey2(),
                },
                [2] = new List<Follower>
                {
                    new Prey1(),
                    new Atalanta(),
                    new Prey2(),
                },
                [3] = new List<Follower>
                {
                    new Prey1(),
                    new GoldenHind(),
                    new Prey2(),
                }
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
            //GoldPerTurn = Controller.BlitzMode ? 4 : 3,
            PortraitName = "SeasideCliffs",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new PoseidonMinor(), // TODO: Add Poseidon Major (?) Summon Cetus/Nereid every time X?
                [3] = new PoseidonMinor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() { },
                [2] = new List<Trinket>() { new CallOfTheSeaTrinket() },
                [3] = new List<Trinket>() { new CallOfTheSeaTrinket() }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new CallOfTheSeaTrinket() },
                [2] = new List<Trinket>() { new CallOfTheSeaTrinket() },
                [3] = new List<Trinket>() { new CallOfTheSeaTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 3,
                [3] = 2
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    new Siren(),
                    new Siren(),
                    new Siren(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Panic(),
                    new PriceOfKnowledge(),
                    new Drown(),
                },
                [2] = new List<Card>
                {
                    new Siren(),
                    new Siren(),
                    new Siren(),
                    new Siren(),
                    new Siren(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Scylla(),
                    new Charybdis(),
                    new Hydra(),
                    new Panic(),
                    new Panic(),
                    //new Panic(),
                    new PriceOfKnowledge(),
                    new PriceOfKnowledge(),
                    //new Drown(),
                    new Drown(),
                },
                [3] = new List<Card>
                {
                    new Siren(),
                    new Siren(),
                    new Siren(),
                    new Siren(),
                    new Siren(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Nereid(),
                    new Scylla(),
                    new Charybdis(),
                    new Hydra(),
                    new Panic(),
                    new Panic(),
                    //new Panic(),
                    new PriceOfKnowledge(),
                    new PriceOfKnowledge(),
                    //new Drown(),
                    new Drown(),
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new Siren(),
                    new Siren(),
                },
                [2] = new List<Follower>
                {
                    new Nereid(),
                    new Nereid(),
                },
                [3] = new List<Follower>
                {
                    new Siren(),
                    new Hydra(),
                }
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
            //GoldPerTurn = Controller.BlitzMode ? 4 : 3,
            PortraitName = "Caves",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new OldOnesMinor(),
                [3] = new OldOnesMinor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() {  },
                [2] = new List<Trinket>() {  },
                [3] = new List<Trinket>() {  }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new AresWhetstoneTrinket(), new DemetersSickleTrinket(), new FuneralAmphoraTrinket(), new AthenasQuillTrinket() },
                [2] = new List<Trinket>() { new AresWhetstoneTrinket(), new DemetersSickleTrinket(), new FuneralAmphoraTrinket(), new AthenasQuillTrinket() },
                [3] = new List<Trinket>() { new AresWhetstoneTrinket(), new DemetersSickleTrinket(), new FuneralAmphoraTrinket(), new AthenasQuillTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 2,
                [3] = 1
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    new Rat(),
                    new Rat(),
                    new Pytho(),
                    new Pytho(),
                    new Lamia(),
                    new Lamia(),
                    new Sphinx(),
                    new Sphinx(),
                    //new Empusa(),
                    //new Empusa(),
                    //new Gorgon(),
                    new Panic(),
                    new Panic(),
                    new PriceOfRenewal(),
                },
                [2] = new List<Card>
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
                [3] = new List<Card>
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
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new Lamia(),
                    new Lamia(),
                },
                [2] = new List<Follower>
                {
                    new Chimera(),
                },
                [3] = new List<Follower>
                {
                    new Lamia(),
                    new Gorgon(),
                    new Lamia(),
                }
            },
            Rewards = new List<Card>
            {
                //new Rat(),
                new Pytho(),
                new Lamia(),
                new Chimera(),
                new Sphinx(),
                new Empusa(),
                new Gorgon(),
                new Lightning(),
                new Panic(),
                //new PriceOfRenewal(),
                new Typhon(),
                new Echidna(),
                new HarpeOfPerseus(),
                new Titanomachy(),
            }
        };
        DetailsByDeckName[DeckName.Trials] = new PlayerDetails
        {
            BaseHealth = 10,
            Pool = 3,
            //GoldPerTurn = Controller.BlitzMode ? 4 : 3,
            PortraitName = "Trials",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new HeraMajor(),
                [3] = new HeraMajor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() { },
                [2] = new List<Trinket>() { new PandorasBoxTrinket() },
                [3] = new List<Trinket>() { new PandorasBoxTrinket() }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new PandorasBoxTrinket() },
                [2] = new List<Trinket>() { new PandorasBoxTrinket() },
                [3] = new List<Trinket>() { new PandorasBoxTrinket() }
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 2,
                [3] = 1
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    new CreateFilth(),
                    new CreateFilth(),
                    new StymphalianBird(),
                    new StymphalianBird(),
                    new StymphalianBird(),
                    new MareOfDiomedes(),
                    new MareOfDiomedes(),
                    new Hippolyta(),
                    new Amazon(),
                    new Amazon(),
                    new Amazon(),
                    new Amazon(),
                },
                [2] = new List<Card>
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
                [3] = new List<Card>
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
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new Amazon(),
                    new Amazon(),
                },
                [2] = new List<Follower>
                {
                    new NemeanLion(),
                },
                [3] = new List<Follower>
                {
                    new Amazon(),
                    new Hippolyta(),
                    new Amazon(),
                }
            },
            Rewards = new List<Card>
            {
                new NemeanLion(),
                new Hydra(),
                new GoldenHind(),
                new CreateFilth(),
                //new StymphalianBird(),
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
            //GoldPerTurn = Controller.BlitzMode ? 4 : 3,
            PortraitName = "Delphi",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = new ApolloMajor(),
                [3] = new ApolloMajor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [1] = null,
                [2] = null,
                [3] = null,
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [1] = new List<Trinket>() { },
                [2] = new List<Trinket>() { new LyreOfApolloTrinket() },
                [3] = new List<Trinket>() { new LyreOfApolloTrinket() }
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new VialOfAmbrosiaTrinket(), new PeltastTrumpetTrinket(), new GoldenFleeceTuftTrinket() },
                [2] = new List<Trinket>() { new VialOfAmbrosiaTrinket(), new PeltastTrumpetTrinket(), new GoldenFleeceTuftTrinket() },
                [3] = new List<Trinket>() { new VialOfAmbrosiaTrinket(), new PeltastTrumpetTrinket(), new GoldenFleeceTuftTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
                [2] = 3,
                [3] = 2
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    new OracleOfDelphi(),
                    new OracleOfDelphi(),
                    new Ekdromos(),
                    new Ekdromos(),
                    new Ekdromos(),
                    new Sheep(),
                    new Sheep(),
                    new Sheep(),
                    new Sheep(),
                    new PriceOfProfit(),
                    new PriceOfProfit(),
                    new Talaria(),
                    new Talaria(),
                    new Smite(),
                    new Smite(),
                    new Reflection(),
                    new Reflection(),
                },
                [2] = new List<Card>
                {
                    new Helios(),
                    new Helios(),
                    new OracleOfDelphi(),
                    new OracleOfDelphi(),
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
                [3] = new List<Card>
                {
                    new Helios(),
                    new Helios(),
                    new OracleOfDelphi(),
                    new OracleOfDelphi(),
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
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    new Sheep(),
                    new Sheep(),
                    new Sheep(),
                },
                [2] = new List<Follower>
                {
                    new Sheep(),
                    new OracleOfDelphi(),
                    new Sheep(),
                },
                [3] = new List<Follower>
                {
                    new Sheep(),
                    new Sheep(),
                    new OracleOfDelphi(),
                    new Sheep(),
                    new Sheep(),
                }
            },
            Rewards = new List<Card>
            {
                new Helios(),
                new OracleOfDelphi(),
                //new Ekdromos(),
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
            IsFightableEnemy = false,
            IsBoss = true,
            BaseHealth = 10,
            Pool = 4,
            //GoldPerTurn = Controller.BlitzMode ? 5 : 4,
            PortraitName = "Underworld",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [4] = new DionysusMinor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [4] = new HadesMajor(),
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [4] = new List<Trinket>() { new TheLostReturnTrinket() },
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [4] = new List<Trinket>() { new TunicOfNessusTrinket(), new MedeasPotionTrinket()},
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [4] = 1,
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [4] = new List<Card>
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
                    new PriceOfProfit(),
                    new PriceOfProfit(),
                    new PriceOfProfit(),
                    //new PriceOfLegacy(),
                },
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [4] = new List<Follower>
                {
                    new Keres(),
                    new Cerberus(),
                    new Keres(),
                    //new Charon(),
                }
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
            if (kvp.Value.IsFightableEnemy)
            {
                EnemyPool.Add(kvp.Key);
                //if (!EnemyPools.ContainsKey(kvp.Value.Pool)) EnemyPools[kvp.Value.Pool] = new List<DeckName>();
                //EnemyPools[kvp.Value.Pool].Add(kvp.Key);
            }
            else if (kvp.Value.IsBoss)
            {
                BossPool.Add(kvp.Key);
            }
        }

        SetupTrinkets();
        SetupStarterBundles();
    }

    private List<Trinket> alreadyDisplayedTrinkets = new List<Trinket>();
    private void SetupStarterBundles()
    {
        starterBundles.Clear();
        alreadyDisplayedTrinkets.Clear();

        Trinket randomTrinket = GetRandomTrinket(new List<Ritual> { new ZeusMinor() });
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new ZeusMinor(), // ZeusMinor
            new List<Card> { 
                new PriceOfReprisal(),
                new Patroclus(),
                new Atalanta(),
            },
            randomTrinket));

        randomTrinket = GetRandomTrinket(new List<Ritual> { new HadesMinor() });
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new HadesMinor(), // HadesMinor
            new List<Card> {
                new Charon(),
                new Corridor(),
                new Vengeance(),
            },
            randomTrinket));

        randomTrinket = GetRandomTrinket(new List<Ritual> { new AphroditeMinor() });
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new AphroditeMinor(), // AphroditeMinor
            new List<Card> {
                new Asclepius(),
                new Restoration(),
                new Lamia()
            },
            randomTrinket));

        randomTrinket = GetRandomTrinket(new List<Ritual> { new HermesMinor() });
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new HermesMinor(),
            new List<Card> {
                new Talaria(),
                new PriceOfKnowledge(),
                new Melpomene(),
            }, randomTrinket));

        randomTrinket = GetRandomTrinket(new List<Ritual> { new HestiaMinor() });
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new HestiaMinor(),
            new List<Card> {
                new PriceOfLegacy(),
                new Hippolyta(),
                new DragonsTeeth(),
            }, randomTrinket));
    }

    private void SetupTrinkets()
    {
        availableTrinkets.Clear();
        availableTrinkets.Add(new CyclopsEyeTrinket());
        availableTrinkets.Add(new AresWhetstoneTrinket());
        availableTrinkets.Add(new FuneralAmphoraTrinket());
        availableTrinkets.Add(new DemetersSickleTrinket());
        availableTrinkets.Add(new AthenasQuillTrinket());
        availableTrinkets.Add(new PeltastTrumpetTrinket());
        availableTrinkets.Add(new GoldenFleeceTuftTrinket());
        availableTrinkets.Add(new LyreOfApolloTrinket());
        availableTrinkets.Add(new VialOfAmbrosiaTrinket());
        availableTrinkets.Add(new PansFluteTrinket());
        availableTrinkets.Add(new HermesSandalsTrinket());
        availableTrinkets.Add(new TheAegisTrinket());
        availableTrinkets.Add(new RodOfAsclepiusTrinket());
        availableTrinkets.Add(new HydrasScaleTrinket());
        availableTrinkets.Add(new TunicOfNessusTrinket());
        availableTrinkets.Add(new MedeasPotionTrinket());
        availableTrinkets.Add(new WingsOfIcarusTrinket());
        availableTrinkets.Add(new PandorasBoxTrinket());
    }

    // Get a random trinket that's viable with the given ritual
    private Trinket GetRandomTrinket(List<Ritual> rituals, bool ignoreSelectedTrinkets = true)
    {
        HashSet<OfferingType> relevantOfferings = new HashSet<OfferingType>();
        foreach (Ritual ritual in rituals)
        {
            if (ritual == null) continue;

            foreach (KeyValuePair<OfferingType, int> kvp in ritual.Costs)
            {
                if (kvp.Value > 0) relevantOfferings.Add(kvp.Key);
            }
        }
        List<Trinket> viableTrinkets = new List<Trinket>();
        foreach (Trinket trinket in availableTrinkets)
        {
            // Ignore trinkets already displayed on the current reward screen
            if (alreadyDisplayedTrinkets.Contains(trinket)) continue;

            // Ignore trinkets that buff an offering not relevant to this ritual
            if (trinket.RelevantOffering != OfferingType.None && !relevantOfferings.Contains(trinket.RelevantOffering))
            {
                continue;
            }

            viableTrinkets.Add(trinket);
        }

        int randIndex = Controller.Instance.MetaRNG.Next(0, viableTrinkets.Count);
        Trinket randomTrinket = viableTrinkets[randIndex];
        if (!randomTrinket.RepeatTrinket)
        {
            availableTrinkets.RemoveAt(randIndex);
        }
        //return new PandorasBoxTrinket();
        return randomTrinket;
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
        int possibleEnemyCount = EnemyPool.Count;
        while (possibleEnemyCount == 0)
        {
            CurrentLevel++;
            if (CurrentPool >= EnemyPool.Count)
            {
                Debug.LogError("You Win!");
                return;
            }
            possibleEnemyCount = EnemyPool.Count;
        }

        int randomTarget = Controller.Instance.MetaRNG.Next(0, EnemyPool.Count);
        CurrentEnemy = EnemyPool[randomTarget];
        // For Testing
        //CurrentEnemy = DeckName.SeasideCliffs;
        EnemyPool.Remove(CurrentEnemy);
    }

    public void LoadPlayer(Player player, DeckName deckName)
    {
        if (!DetailsByDeckName.ContainsKey(deckName))
        {
            Debug.LogError("Player Not Found: " + deckName);
            return;
        }

        PlayerDetails newDetails = DetailsByDeckName[deckName];

        if (deckName != DeckName.TestEnemy)
        {
            if (newDetails.IsEnemy)
            {
                newDetails.BaseHealth = CurrentPool * 10 + ((CurrentLevel - 1) % 3) * 5;
                newDetails.GoldPerTurn = CurrentLevel >= 10 ? 5 : 2 + Mathf.Min(CurrentPool, 2);
            }
            else newDetails.BaseHealth = GetPlayerHealth();
        }

        int pool = newDetails.IsEnemy ? CurrentPool : 0;
        player.LoadDetails(newDetails, pool);
    }
    public List<Follower> GetPlayerStartingFollowers(DeckName deckName, int pool)
    {
        PlayerDetails newDetails = DetailsByDeckName[deckName];
        if (newDetails.StartingBattleRow.ContainsKey(pool))
        {
            return newDetails.StartingBattleRow[pool];
        }
        return new List<Follower>();
    }

    public void LoadEnemy(Player player)
    {
        LoadPlayer(player, CurrentEnemy);
    }

    public int GetPlayerHealth()
    {
        if (CurrentLevel >= 10) return 50;

        return 10 + Mathf.FloorToInt(CurrentLevel / 2f) * 5; // CurrentPool * 5;
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

    public List<Trinket> GetRandomTrinkets()
    {
        List<Trinket> trinkets = new List<Trinket>();
        alreadyDisplayedTrinkets.Clear();

        List<Ritual> rituals = new List<Ritual>
        {
            Controller.Instance.HumanPlayerDetails.MajorRituals[0],
            Controller.Instance.HumanPlayerDetails.MinorRituals[0]
        };

        trinkets.Add(GetRandomTrinket(rituals));
        trinkets.Add(GetRandomTrinket(rituals));
        trinkets.Add(GetRandomTrinket(rituals));

        return trinkets;
    }
}

