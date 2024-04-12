using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spell : Card, ITarget
{
    public bool HasTargets = true;
    public override List<ITarget> GetPlayableTargets()
    {
        return new List<ITarget>();
    }

    public bool HasPlayableTargets()
    {
        return !HasTargets || GetPlayableTargets().Count > 0;
    }

    public List<ITarget> GetAllFollowers()
    {
        List<ITarget> targets = new List<ITarget>();
        targets.AddRange(Owner.BattleRow.Followers);
        targets.AddRange(Controller.Instance.GetOtherPlayer(Owner).BattleRow.Followers);
        return targets;
    }
    public List<ITarget> GetOwnFollowers()
    {
        List<ITarget> targets = new List<ITarget>();
        targets.AddRange(Owner.BattleRow.Followers);
        return targets;
    }
    public List<ITarget> GetEnemyFollowers()
    {
        List<ITarget> targets = new List<ITarget>();
        targets.AddRange(Controller.Instance.GetOtherPlayer(Owner).BattleRow.Followers);
        return targets;
    }
    public List<ITarget> GetAllPlayers()
    {
        List<ITarget> targets = new List<ITarget>
        {
            Owner,
            Controller.Instance.GetOtherPlayer(Owner)
        };
        return targets;
    }
}
