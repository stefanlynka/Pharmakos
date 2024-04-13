using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class BattleRow
{
    public List<Follower> Followers = new List<Follower>();
    public Player Player;

    public int GetIndexOfFollower(Follower targetFollower)
    {
        int index = -1;

        for (int i = 0; i < Followers.Count; i++)
        {
            Follower follower = Followers[i];
            if (follower == targetFollower)
            {
                index = i;
                break;
            }
        }

        return index;
    }

    // Get Followers from this BattleRow that can be attacked from
    public List<ITarget> GetTargetsInRange(float attackerPosition)
    {
        List<ITarget> targets = new List<ITarget>();

        float currentPosition = -0.5f * (Followers.Count-1);
        for (int i = 0; i < Followers.Count; i++)
        {
            if (Mathf.Abs(attackerPosition-currentPosition) <= 1) targets.Add(Followers[i]);

            currentPosition++;
        }

        bool evenAttackerCount = Mathf.Abs(attackerPosition % 1) == 0.5f;
        bool evenDefenderCount = Followers.Count % 2 == 0;

        bool rowsAreAligned = evenAttackerCount == evenDefenderCount;

        // If you are aligned
        int expectedTargetCount = rowsAreAligned ? 3 : 2;
        if (targets.Count != expectedTargetCount) targets.Add(Player);

        return targets;
    }
}
