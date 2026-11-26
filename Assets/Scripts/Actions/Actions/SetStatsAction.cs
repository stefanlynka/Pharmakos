using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class SetStatsAction : GameAction
{
    //public ITarget Source;
    public ITarget Target;
    public int NewAttack = -1;
    public int NewHealth = -1;

    public SetStatsAction(ITarget target, int newAttack = -1, int newHealth = -1)
    {
        Target = target;
        NewAttack = newAttack;
        NewHealth = newHealth;
    }

    public override void AddPlayHistoryResults(TriggeredEffectPlayHistory record)
    {
        record.AddResult(Target);
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        ChangeStatsAction copy = (ChangeStatsAction)MemberwiseClone();
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        Follower follower = Target as Follower;
        //Player player = Target as Player;

        if (follower != null )
        {
            if (NewAttack == -1) NewAttack = follower.GetCurrentAttack();
            if (NewHealth == -1) NewHealth = follower.CurrentHealth;

            follower.SetStats(NewAttack, NewHealth);
            if (!simulated)
                PlayHistoryHandler.For(follower.GameState)?.RecordStatChange(this, follower);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>();
        Follower follower = Target as Follower;
        if (follower != null)
            animationActions.Add(new UpdateStatsAnimation(this, Target, follower.GetCurrentAttack(), follower.CurrentHealth));
        return animationActions;
    }

}
