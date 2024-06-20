using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerDecision
{
    public virtual string GetString()
    {
        return "Default Decision Text";
    }
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

    public override string GetString()
    {
        Follower follower = Controller.Instance.CanonGameState.TargetsByID[CardID] as Follower;
        if (follower != null)
        {
            return "Play " + follower.GetName() + " at index: " + PlacementIndex;
        }
        return "Follower not found";
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

    public override string GetString()
    {
        Spell spell = Controller.Instance.CanonGameState.TargetsByID[CardID] as Spell;
        ITarget target = Controller.Instance.CanonGameState.TargetsByID[TargetID];
        if (spell != null && target != null)
        {
            return "Play " + spell.GetName() + " targeting " + target.GetName();
        }
        return "Spell not found";
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

    public override string GetString()
    {
        Ritual ritual = Controller.Instance.CanonGameState.TargetsByID[RitualID] as Ritual;
        ITarget target = Controller.Instance.CanonGameState.TargetsByID[TargetID];
        if (ritual != null)
        {
            string targetText = target != null ? target.GetName() : "nothing";
            return "Play " + ritual.GetName() + " targeting " + targetText;
        }
        return "Ritual not found";
    }
}

public class AttackWithFollowerDecision : PlayerDecision
{
    public int FollowerID;
    public int TargetID;

    public AttackWithFollowerDecision(int followerID, int targetID)
    {
        FollowerID = followerID;
        TargetID = targetID;
    }

    public override string GetString()
    {
        Follower follower = Controller.Instance.CanonGameState.TargetsByID[FollowerID] as Follower;
        ITarget target = Controller.Instance.CanonGameState.TargetsByID[TargetID];
        if (follower == null) return "Attacker not found";
        if (target == null) return "Attack target not found";

        return follower.GetName() + " attacks " + target.GetName();
    }
}