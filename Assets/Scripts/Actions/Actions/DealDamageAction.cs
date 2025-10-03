using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class DealDamageAction : GameAction
{
    public ITarget Source;
    public ITarget Target;
    public int Damage;
    private int damageDealt = 0;
    private int attackChange = 0;

    public DealDamageAction(ITarget source, ITarget target, int damage)
    {
        Source = source;
        Target = target;
        Damage = damage;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        DealDamageAction copy = (DealDamageAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Follower follower = Target as Follower;
        Player player = Target as Player;

        if (follower != null )
        {
            int prevAttack = follower.GetCurrentAttack();
            int prevHealth = follower.CurrentHealth;
            follower.ChangeHealth(Source , -Damage);
            damageDealt = prevHealth - follower.CurrentHealth;
            attackChange = prevAttack - follower.GetCurrentAttack();
        }
        else if (player != null )
        {
            int prevHealth = player.Health;
            player.ChangeHealth(Source , -Damage);
            damageDealt = prevHealth - player.Health;
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new ChangeStatsAnimation(this, Target, -attackChange, -damageDealt)
            //new DamageAnimation(this)
        };
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }

}
