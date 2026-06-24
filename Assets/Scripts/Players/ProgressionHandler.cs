using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardHandler;
using static ProgressionHandler;

public class ProgressionHandler
{
    private const int ENEMY_HEALTH_OVERRIDE = -1; // -1 to disables
    public const int BossPoolNumber = 4;
    public const int HighestEnemyPoolNumber = 3;

    static readonly HashSet<Type> ShopExcludedCardTypes = new HashSet<Type>
    {
        typeof(Contraption),
        typeof(FirstFlame),
        typeof(Shade),
        typeof(Haunt),
        typeof(Hippeis),
        typeof(Hoplite),
        typeof(Myrmidon),
        typeof(Prey1),
        typeof(Prey2),
        typeof(Prey3),
        typeof(Gnaw),
        typeof(Roar),
    };
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
        Throne,
        Fates,
        TheGate,
    }

    public int CurrentLevel = 0;
    /// <summary>Overworld floor (<see cref="OverworldMapNode.R"/>) for the current combat; drives enemy scaling.</summary>
    public int CurrentFightNumber = 0;
    public DeckName CurrentEnemy = DeckName.None;
    public DeckName LastFoughtEnemyDeckName = DeckName.None;
    public int LastFoughtEnemyPoolNum = 0;
    public int CurrentPool =>
        CurrentEnemy != DeckName.None && DetailsByDeckName.TryGetValue(CurrentEnemy, out PlayerDetails currentDetails)
            ? GetDetailsPool(currentDetails)
            : GetFightPool(CurrentFightNumber > 0 ? CurrentFightNumber : CurrentLevel);

    public static int GetFightPool(int fightNumber) =>
        Mathf.Max(1, Mathf.CeilToInt(fightNumber / 3f));

    public static int GetDetailsPool(PlayerDetails details, int fightNumber) =>
        details.IsBoss ? BossPoolNumber : Mathf.Min(GetFightPool(fightNumber), HighestEnemyPoolNumber);

    public int GetDetailsPool(PlayerDetails details)
    {
        if (!details.IsEnemy)
            return 0;

        int fightNumber = CurrentFightNumber > 0 ? CurrentFightNumber : CurrentLevel;
        return GetDetailsPool(details, fightNumber);
    }

    public bool IsCurrentEnemyBoss() =>
        CurrentEnemy != DeckName.None
        && DetailsByDeckName.TryGetValue(CurrentEnemy, out PlayerDetails details)
        && details.IsBoss;

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
            BaseHealth = 1,
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
                [1] = new List<Trinket>() { }, // new TwistingCorridorsTrinket()
            },
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [1] = new List<Trinket>() { new EverDeeperTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [1] = 10,
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [1] = new List<Card>
                {
                    //new Hoplite(),
                    new ThrowStone(),
                    new ThrowStone(),
                    // new Helios(),
                    // new Helios(),
                    // new Helios(),
                    // new Helios(),
                    new Helios(),
                }
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [1] = new List<Follower>
                {
                    //new Corridor(),
                    //new Peltast(),
                    //new Pytho(),
                    //new Icarus(),
                    //new Siren(),
                }
            }
        };
        DetailsByDeckName[DeckName.TestPlayer] = new PlayerDetails
        {
            IsEnemy = false,
            IsFightableEnemy = false,
            BaseHealth = 1,
            CardsPerTurn = 5,
            GoldPerTurn = 5,
            Pool = 0,
            PortraitName = "Player",
            MinorRituals = new Dictionary<int, Ritual>
            {
                [0] = new AthenaMajor(),
            },
            MajorRituals = new Dictionary<int, Ritual>
            {
                [0] = new MuseMinor(),
            },
            Trinkets = new Dictionary<int, List<Trinket>>
            {
                [0] = new List<Trinket>() { new OdysseyTrinket() }
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [0] = new List<Card>
                {
                    //new DevKill(),
                    //new ThrowStone(),
                    new Pegasus(),
                    new Blessing(),
                    new Smite(),
                    new ThrowStone(),
                    new Peltast(),
                    //new PriceOfProfit(),
                    //new Pan(),
                    //new Endymion(),
                },
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>
            {
                [0] = new List<Follower>
                {
                    // new Ekdromos(),
                    // new Ekdromos(),
                    //new Pytho(),
                    //new Agamemnon(),
                    //new Chariot(),
                    //new Charybdis(),
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
                    new Hoplite(),
                    new Thureophoros(),
                    //new Chariot(),
                    new Chariot(),
                    new Hippeis(),
                    new Hippeis(),
                    new Myrmidon(),
                    //new PriceOfProfit(),
                    new PriceOfProfit(),
                    new Blessing(),
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
                    new Atalanta(),
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
                    new Atalanta(),
                    new Atalanta(),
                    new Atalanta(),
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
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    new Hoplite(),
                    //new CreateFilth(),
                    new CreateFilth(),
                    new StymphalianBird(),
                    new StymphalianBird(),
                    //new StymphalianBird(),
                    new MareOfDiomedes(),
                    //new MareOfDiomedes(),
                    //new Hippolyta(),
                    //new Amazon(),
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
                new Chariot(),
                new MareOfDiomedes(),
                new Hippolyta(),
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

        DetailsByDeckName[DeckName.Throne] = new PlayerDetails
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

        DetailsByDeckName[DeckName.Fates] = new PlayerDetails
        {
            IsFightableEnemy = false,
            IsBoss = true,
            BaseHealth = 10,
            Pool = 4,
            PortraitName = "Fates",
            MinorRituals = new Dictionary<int, Ritual>(),
            MajorRituals = new Dictionary<int, Ritual>(),
            Trinkets = new Dictionary<int, List<Trinket>>(),
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>(),
            TwistOfFateIntervals = new Dictionary<int, int>(),
            DeckBlueprint = new Dictionary<int, List<Card>>(),
            StartingBattleRow = new Dictionary<int, List<Follower>>(),
            Rewards = new List<Card>(),
        };

        DetailsByDeckName[DeckName.TheGate] = new PlayerDetails
        {
            IsFightableEnemy = false,
            IsBoss = true,
            BaseHealth = 10,
            Pool = 4,
            PortraitName = "TheGate",
            MinorRituals = new Dictionary<int, Ritual>(),
            MajorRituals = new Dictionary<int, Ritual>(),
            Trinkets = new Dictionary<int, List<Trinket>>(),
            TwistOfFateBuffs = new Dictionary<int, List<Trinket>>()
            {
                [4] = new List<Trinket>() { new CreakingOpenTrinket() },
            },
            TwistOfFateIntervals = new Dictionary<int, int>()
            {
                [4] = 1,
            },
            DeckBlueprint = new Dictionary<int, List<Card>>
            {
                [4] = new List<Card>
                {
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new EyesInTheDark(),
                    new Gnaw(),
                    new Gnaw(),
                    new Gnaw(),
                    new Gnaw(),
                    new Gnaw(),
                    new Roar(),
                    new Roar(),
                    new Roar(),
                    new Roar(),
                    new Roar(),
                },
            },
            StartingBattleRow = new Dictionary<int, List<Follower>>(),
            Rewards = new List<Card>
            {
                new EyesInTheDark(),
                new Gnaw(),
                new Roar(),
            },
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

        Trinket randomTrinket = GetRandomTrinket(new List<Ritual> { new ZeusMinor() }, forStartingBundle: true);
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new ZeusMinor(), // ZeusMinor
            new List<Card> { 
                new PriceOfReprisal(),
                new Patroclus(),
                new Atalanta(),
            },
            randomTrinket));

        randomTrinket = GetRandomTrinket(new List<Ritual> { new HadesMinor() }, forStartingBundle: true);
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new HadesMinor(), // HadesMinor
            new List<Card> {
                new Charon(),
                new Corridor(),
                new Vengeance(),
            },
            randomTrinket));

        randomTrinket = GetRandomTrinket(new List<Ritual> { new AphroditeMinor() }, forStartingBundle: true);
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new AphroditeMinor(), // AphroditeMinor
            new List<Card> {
                new Asclepius(),
                new Restoration(),
                new Lamia()
            },
            randomTrinket));

        randomTrinket = GetRandomTrinket(new List<Ritual> { new HermesMinor() }, forStartingBundle: true);
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new HermesMinor(),
            new List<Card> {
                new Talaria(),
                new PriceOfKnowledge(),
                new Melpomene(),
            }, randomTrinket));

        randomTrinket = GetRandomTrinket(new List<Ritual> { new HestiaMinor() }, forStartingBundle: true);
        alreadyDisplayedTrinkets.Add(randomTrinket);
        starterBundles.Add(new StarterBundle(
            new HestiaMinor(),
            new List<Card> {
                new PriceOfLegacy(),
                new Hippolyta(),
                new DragonTeeth(),
            }, randomTrinket));
    }

    private List<Trinket> CreateFullTrinketPool()
    {
        return new List<Trinket>
        {
            new CyclopsEyeTrinket(),
            new AresWhetstoneTrinket(),
            new FuneralAmphoraTrinket(),
            new DemetersSickleTrinket(),
            new AthenasQuillTrinket(),
            new PeltastTrumpetTrinket(),
            new GoldenFleeceTuftTrinket(),
            new LyreOfApolloTrinket(),
            new VialOfAmbrosiaTrinket(),
            new PansFluteTrinket(),
            new HermesSandalsTrinket(),
            new TheAegisTrinket(),
            new RodOfAsclepiusTrinket(),
            new HydrasScaleTrinket(),
            new TunicOfNessusTrinket(),
            new MedeasPotionTrinket(),
            new WingsOfIcarusTrinket(),
            new PandorasBoxTrinket(),
            new PandorasHopeTrinket(),
            new OdysseyTrinket(),
        };
    }

    private void SetupTrinkets()
    {
        availableTrinkets = CreateFullTrinketPool();
    }

    // Get a random trinket that's viable with the given ritual
    private Trinket GetRandomTrinket(List<Ritual> rituals, bool ignoreSelectedTrinkets = true, bool forStartingBundle = false)
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
            if (forStartingBundle && !trinket.AvailableAsStartingTrinket) continue;

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
            ritualRewards.Add(new HermesMajor());
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
            ritualRewards.Add(new PoseidonMajor());
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
        CurrentFightNumber = 0;
        LastFoughtEnemyDeckName = DeckName.None;
        LastFoughtEnemyPoolNum = 0;
    }

    public static string FormatDeckNameAsFightName(DeckName deckName)
    {
        string name = deckName.ToString();
        if (string.IsNullOrEmpty(name))
            return "";

        var formatted = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                formatted.Append(' ');
            formatted.Append(c);
        }

        return formatted.ToString();
    }

    public string GetCurrentFightDisplayName() => FormatDeckNameAsFightName(CurrentEnemy);

    public void RecordLastFoughtEnemy()
    {
        if (CurrentEnemy == DeckName.None)
            return;

        LastFoughtEnemyDeckName = CurrentEnemy;
        LastFoughtEnemyPoolNum = CurrentPool;
    }

    public bool ApplyBecomeLastEnemy()
    {
        if (LastFoughtEnemyDeckName == DeckName.None)
        {
            Debug.LogWarning("BecomeLastEnemy: No last fought enemy recorded.");
            return false;
        }

        if (!DetailsByDeckName.TryGetValue(LastFoughtEnemyDeckName, out PlayerDetails enemyDetails))
        {
            Debug.LogError("BecomeLastEnemy: Enemy deck not found: " + LastFoughtEnemyDeckName);
            return false;
        }

        if (Controller.Instance?.HumanPlayerDetails == null)
        {
            Debug.LogError("BecomeLastEnemy: HumanPlayerDetails is not available.");
            return false;
        }

        int pool = LastFoughtEnemyPoolNum;
        PlayerDetails playerDetails = Controller.Instance.HumanPlayerDetails;

        if (!enemyDetails.DeckBlueprint.TryGetValue(pool, out List<Card> enemyDeck) || enemyDeck == null)
        {
            Debug.LogWarning("BecomeLastEnemy: Enemy deck not found for pool " + pool + " on " + LastFoughtEnemyDeckName);
            return false;
        }

        Controller.Instance.RecordSacrificedDeckAndRituals();

        var newDeck = new List<Card>();
        foreach (Card card in enemyDeck)
            newDeck.Add(card.MakeBaseCopy());
        playerDetails.DeckBlueprint[0] = newDeck;

        if (enemyDetails.MinorRituals.TryGetValue(pool, out Ritual minorRitual) && minorRitual != null)
            playerDetails.MinorRituals[0] = minorRitual.MakeBaseCopy();
        else
            playerDetails.MinorRituals[0] = null;

        if (enemyDetails.MajorRituals.TryGetValue(pool, out Ritual majorRitual) && majorRitual != null)
            playerDetails.MajorRituals[0] = majorRitual.MakeBaseCopy();
        else
            playerDetails.MajorRituals[0] = null;

        if (!playerDetails.Trinkets.ContainsKey(0))
            playerDetails.Trinkets[0] = new List<Trinket>();

        if (enemyDetails.Trinkets.TryGetValue(pool, out List<Trinket> enemyTrinkets))
        {
            foreach (Trinket trinket in enemyTrinkets)
            {
                if (trinket != null)
                    playerDetails.Trinkets[0].Add(trinket.MakeBaseCopy());
            }
        }

        return true;
    }

    public void CurrentEnemyDefeated()
    {
        //if (!EnemyPools.ContainsKey(CurrentPool)) return;

        //EnemyPools[CurrentPool].Remove(CurrentEnemy);
    }

    /// <summary>Overworld temple (ritual) node: advance level without selecting an enemy; rewards are removal then rituals.</summary>
    public void RegisterTempleEncounter()
    {
        CurrentLevel++;
    }

    /// <summary>Overworld shop node: advance level without combat.</summary>
    public void RegisterMarketEncounter()
    {
        CurrentLevel++;
    }

    /// <summary>Overworld event node: advance level without combat.</summary>
    public void RegisterEventEncounter()
    {
        CurrentLevel++;
    }

    /// <summary>Overworld Styx node: advance level without combat.</summary>
    public void RegisterStyxEncounter()
    {
        CurrentLevel++;
    }

    public List<Card> GetShopCardPool()
    {
        var pool = new List<Card>();
        var seenTypes = new HashSet<Type>();

        foreach (KeyValuePair<DeckName, PlayerDetails> kvp in DetailsByDeckName)
        {
            PlayerDetails details = kvp.Value;
            if (details == null || !details.IsFightableEnemy || details.Rewards == null) continue;

            foreach (Card card in details.Rewards)
            {
                if (card == null) continue;
                Type cardType = card.GetType();
                if (ShopExcludedCardTypes.Contains(cardType)) continue;
                if (!seenTypes.Add(cardType)) continue;
                pool.Add(card.MakeBaseCopy());
            }
        }

        return pool;
    }

    public List<Trinket> GetRandomShopTrinkets(int count)
    {
        var result = new List<Trinket>();
        var pool = new List<Trinket>(availableTrinkets);
        int pickCount = Mathf.Min(count, pool.Count);

        for (int i = 0; i < pickCount; i++)
        {
            int idx = Controller.Instance.MetaRNG.Next(0, pool.Count);
            result.Add(pool[idx].MakeBaseCopy());
            pool.RemoveAt(idx);
        }

        return result;
    }

    void RefillEnemyPoolIfDepleted()
    {
        if (EnemyPool.Count > 0) return;
        foreach (var kvp in DetailsByDeckName)
        {
            if (kvp.Value != null && kvp.Value.IsFightableEnemy)
                EnemyPool.Add(kvp.Key);
        }
    }

    public void SetupNextCombatEnemy(int floorFightNumber = -1)
    {
        CurrentLevel++;
        CurrentFightNumber = floorFightNumber >= 0 ? floorFightNumber : CurrentLevel;
        RefillEnemyPoolIfDepleted();
        if (EnemyPool.Count == 0)
        {
            Debug.LogError("No fightable enemies available for combat.");
            return;
        }

        int randomTarget = Controller.Instance.MetaRNG.Next(0, EnemyPool.Count);
        CurrentEnemy = EnemyPool[randomTarget];
        EnemyPool.RemoveAt(randomTarget);
    }

    public void SetupNextBossEnemy()
    {
        CurrentLevel++;
        if (BossPool.Count == 0)
        {
            CurrentLevel--;
            Debug.LogError("Boss pool is empty; using a regular combat encounter instead.");
            SetupNextCombatEnemy();
            return;
        }

        int idx = Controller.Instance.MetaRNG.Next(0, BossPool.Count);
        CurrentEnemy = BossPool[idx];
    }

    public void SetupBossEncounter(EncounterType bossType, int floorFightNumber = -1)
    {
        CurrentLevel++;
        CurrentFightNumber = floorFightNumber >= 0 ? floorFightNumber : CurrentLevel;
        if (!TryGetBossDeckName(bossType, out DeckName bossDeck))
        {
            CurrentLevel--;
            Debug.LogError("Unknown boss encounter type: " + bossType + "; using a regular combat encounter instead.");
            SetupNextCombatEnemy(floorFightNumber);
            return;
        }

        CurrentEnemy = bossDeck;
    }

    public static bool TryGetBossDeckName(EncounterType bossType, out DeckName deckName)
    {
        switch (bossType)
        {
            case EncounterType.BossFate:
                deckName = DeckName.Fates;
                return true;
            case EncounterType.BossGate:
                deckName = DeckName.TheGate;
                return true;
            case EncounterType.BossThrone:
                deckName = DeckName.Throne;
                return true;
            default:
                deckName = DeckName.None;
                return false;
        }
    }

    public void SetupNextEnemy(bool isTestChamber = false)
    {
        if (isTestChamber)
        {
            CurrentLevel++;
            CurrentEnemy = DeckName.TestEnemy;

            return;
        }

        SetupNextCombatEnemy();
    }

    public void LoadPlayer(Player player, DeckName deckName)
    {
        if (!DetailsByDeckName.ContainsKey(deckName))
        {
            Debug.LogError("Player Not Found: " + deckName);
            return;
        }

        PlayerDetails newDetails = DetailsByDeckName[deckName];

        if (!UsesConfiguredBaseHealth(deckName))
        {
            if (newDetails.IsEnemy)
            {
                int fightNumber = CurrentFightNumber > 0 ? CurrentFightNumber : CurrentLevel;
                int fightPool = GetDetailsPool(newDetails, fightNumber);
                if (TryGetFixedBossHealth(deckName, out int bossHealth))
                    newDetails.BaseHealth = bossHealth;
                else
                    newDetails.BaseHealth = ENEMY_HEALTH_OVERRIDE > 0 ? ENEMY_HEALTH_OVERRIDE : fightPool * 10 + ((fightNumber - 1) % 3) * 5;
                newDetails.GoldPerTurn = fightNumber >= 10 ? 5 : 2 + Mathf.Min(fightPool, 2);
            }
            else newDetails.BaseHealth = GetPlayerHealth();
        }

        int pool = GetDetailsPool(newDetails);
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
        if (CurrentEnemy == DeckName.Fates)
            PrepareFatesDetailsFromPlayer(CurrentPool);
        else if (CurrentEnemy == DeckName.TheGate)
            PrepareTheGateDetails(CurrentPool);

        LoadPlayer(player, CurrentEnemy);
    }

    private const int StartingBattleRowTargetGoldCost = 6;

    private static List<Follower> GetFollowersFromDeck(IEnumerable<Card> deck)
    {
        var followers = new List<Follower>();
        foreach (Card card in deck)
        {
            if (card is Follower follower)
                followers.Add(follower);
        }
        return followers;
    }

    private static List<Follower> BuildRandomStartingBattleRow(List<Follower> candidates, CustomRandom rng, int targetGoldCost = StartingBattleRowTargetGoldCost)
    {
        var pool = new List<Follower>(candidates);
        var result = new List<Follower>();
        int totalGold = 0;

        while (pool.Count > 0 && totalGold < targetGoldCost)
        {
            int idx = rng.Next(0, pool.Count);
            Follower picked = pool[idx];
            pool.RemoveAt(idx);
            result.Add((Follower)picked.MakeBaseCopy());
            totalGold += picked.Costs[OfferingType.Gold];
        }

        return result;
    }

    private void PrepareFatesDetailsFromPlayer(int pool)
    {
        PlayerDetails fatesDetails = DetailsByDeckName[DeckName.Fates];
        PlayerDetails humanDetails = Controller.Instance.HumanPlayerDetails;

        var deckCopy = new List<Card>();
        if (humanDetails.DeckBlueprint.TryGetValue(0, out List<Card> playerDeck))
        {
            foreach (Card card in playerDeck)
                deckCopy.Add(card.MakeBaseCopy());
        }
        fatesDetails.DeckBlueprint[pool] = deckCopy;

        fatesDetails.MinorRituals[pool] = humanDetails.MinorRituals.TryGetValue(0, out Ritual minorRitual) && minorRitual != null
            ? minorRitual.MakeBaseCopy()
            : null;
        fatesDetails.MajorRituals[pool] = humanDetails.MajorRituals.TryGetValue(0, out Ritual majorRitual) && majorRitual != null
            ? majorRitual.MakeBaseCopy()
            : null;

        fatesDetails.Trinkets[pool] = new List<Trinket>();
        if (humanDetails.Trinkets.TryGetValue(0, out List<Trinket> playerTrinkets))
        {
            foreach (Trinket trinket in playerTrinkets)
                fatesDetails.Trinkets[pool].Add(trinket.MakeBaseCopy());
        }

        fatesDetails.TwistOfFateIntervals[pool] = 1;
        fatesDetails.TwistOfFateBuffs[pool] = new List<Trinket>();
        foreach (Trinket trinket in CreateFullTrinketPool())
            fatesDetails.TwistOfFateBuffs[pool].Add(trinket.MakeBaseCopy());

        fatesDetails.StartingBattleRow[pool] = BuildRandomStartingBattleRow(
            GetFollowersFromDeck(deckCopy),
            Controller.Instance.MetaRNG);
    }

    private void PrepareTheGateDetails(int pool)
    {
        PlayerDetails gateDetails = DetailsByDeckName[DeckName.TheGate];
        gateDetails.StartingBattleRow[pool] = BuildRandomStartingBattleRow(
            CardHandler.AllMonsters,
            Controller.Instance.MetaRNG);
    }

    private static bool UsesConfiguredBaseHealth(DeckName deckName) =>
        deckName == DeckName.TestPlayer || deckName == DeckName.TestEnemy;

    private static bool TryGetFixedBossHealth(DeckName deckName, out int health)
    {
        switch (deckName)
        {
            case DeckName.Fates:
            case DeckName.TheGate:
                health = 50;
                return true;
            case DeckName.Throne:
                health = 60;
                return true;
            default:
                health = 0;
                return false;
        }
    }

    public int GetPlayerHealth(bool isHuman = true)
    {
        //return 1;
        if (Controller.Instance != null && Controller.Instance.IsTestChamber)
        {
            DeckName deck = isHuman ? DeckName.TestPlayer : DeckName.TestEnemy;
            return DetailsByDeckName[deck].BaseHealth;
        }

        if (!isHuman && ENEMY_HEALTH_OVERRIDE > 0) return ENEMY_HEALTH_OVERRIDE;

        if (CurrentLevel >= 10) return 40;

        return 15 + Mathf.FloorToInt(CurrentLevel / 3f) * 5; // CurrentPool * 5;
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

