using System;
using System.Collections.Generic;

[Serializable]
public class MapNodeSave
{
    public int q;
    public int r;
    public int encounter;
    public bool cleared;
}

[Serializable]
public class RunSaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public int runSeed;
    public int metaRngSeed;
    public int heartstrings;
    public List<string> deck = new List<string>();
    public string majorRitual = "";
    public string minorRitual = "";
    public List<string> trinkets = new List<string>();
    public List<string> styxCards = new List<string>();
    public List<string> styxRituals = new List<string>();
    public List<string> styxTrinkets = new List<string>();
    public bool styxHasCrossed;
    public List<string> sacrificedFollowers = new List<string>();
    public List<string> encounteredEvents = new List<string>();
    public int currentLevel;
    public int currentFightNumber;
    public bool hasSetupFirstCombat;
    public string lastFoughtEnemy = "";
    public int lastFoughtPool;
    public List<string> enemyPool = new List<string>();
    public List<string> bossPool = new List<string>();
    public List<string> trinketPool = new List<string>();
    public int currentQ;
    public int currentR;
    public List<MapNodeSave> nodes = new List<MapNodeSave>();
}
