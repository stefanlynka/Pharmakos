using System.Collections.Generic;

public class KillLowestAttackFollowersAction : GameAction
{
    public ITarget Source;

    public KillLowestAttackFollowersAction(ITarget source)
    {
        Source = source;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        KillLowestAttackFollowersAction copy = (KillLowestAttackFollowersAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        Player playerSource = Source as Player;
        if (playerSource == null)
        {
            Follower followerSource = Source as Follower;
            if (followerSource == null) return;

            playerSource = followerSource.Owner;
        }

        List<Follower> lowestAttackFollowers = GetLowestAttackFollowers(playerSource);

        foreach (Follower follower in lowestAttackFollowers)
        {
            KillFollowerAction killAction = new KillFollowerAction(playerSource, follower);
            playerSource.GameState.ActionHandler.AddAction(killAction);
        }

        base.Execute(simulated);
    }

    // Every follower on both sides tied for the lowest current attack. Also used by the view to preview the deaths.
    public static List<Follower> GetLowestAttackFollowers(Player player)
    {
        int lowestAttack = int.MaxValue;
        List<Follower> lowestAttackFollowers = new List<Follower>();

        foreach (Follower follower in ITarget.GetAllFollowers(player))
        {
            int currentAttack = follower.GetCurrentAttack();
            if (currentAttack == lowestAttack)
            {
                lowestAttackFollowers.Add(follower);
            }
            else if (currentAttack < lowestAttack)
            {
                lowestAttackFollowers.Clear();
                lowestAttackFollowers.Add(follower);
                lowestAttack = currentAttack;
            }
        }

        return lowestAttackFollowers;
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            //new DamageAnimation(this)
        };
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }

}
