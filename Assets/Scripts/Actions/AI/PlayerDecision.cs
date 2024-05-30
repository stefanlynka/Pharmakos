using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerDecision
{
    
}

// CardID/TargetID/RitualID all refer to the unique, incrementing IDs assigned to all ITargets by the GameState
// They are used to later lookup the actual Card/Player/Ritual being referred to.
public class PlayFollowerDecision : PlayerDecision
{
    public int CardID;
    public int PlacementIndex;

    public PlayFollowerDecision(int cardID, int placementIndex)
    {
        CardID = cardID;
        PlacementIndex = placementIndex;
    }
}

public class PlaySpellDecision : PlayerDecision
{
    public int CardID;
    public int TargetID;

    public PlaySpellDecision(int cardID, int targetID)
    {
        CardID = cardID;
        TargetID = targetID;
    }
}

public class UseRitualDecision : PlayerDecision
{
    public int RitualID;
    public int TargetID;

    public UseRitualDecision(int cardID, int targetID)
    {
        RitualID = cardID;
        TargetID = targetID;
    }
}