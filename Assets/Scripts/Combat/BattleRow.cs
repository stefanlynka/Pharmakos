using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class BattleRow
{
    public List<Follower> Followers = new List<Follower>();
    public Player Owner;

    // BattleRow Positions Explanation:
    // Cards are 1.0 apart. Center = 0.
    // Odd number of Followers:  Center position is 0. Left of that is -1, another one left is -2, etc
    // Even number of Followers: Slightly left of center is -0.5, left of that is -1.5, etc

    public BattleRow DeepCopy(Player newOwner)
    {
        BattleRow copy = new BattleRow();

        copy.Owner = newOwner;

        foreach (Follower follower in Followers)
        {
            Follower newFollower = (Follower)follower.DeepCopy(newOwner);
            copy.Followers.Add(newFollower);
        }

        return copy;
    }

    public void Init(Player newOwner)
    {
        Owner = newOwner;
    }
    public void Clear()
    {
        Followers.Clear();
        Owner = null;
    }

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

    // Get Followers from this BattleRow that can be attacked from the opponent's attackerPosition index
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
        if (targets.Count != expectedTargetCount) targets.Add(Owner);

        return targets;
    }

    public List<Follower> GetFollowersThatCanAttack()
    {
        List<Follower> followers = new List<Follower>();

        foreach (Follower follower in Followers)
        {
            if (follower.CanAttack()) followers.Add(follower);
        }

        return followers;
    }
}
