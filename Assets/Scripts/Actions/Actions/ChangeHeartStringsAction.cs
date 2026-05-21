using System;
using System.Collections.Generic;
using UnityEngine;

public class ChangeHeartStringsAction : GameAction
{

    private Player player;
    private int amount;
    private ITarget source;
    private bool interruptAiTurnOnLoss;

    public ChangeHeartStringsAction(Player player, int amount, ITarget source = null, bool interruptAiTurnOnLoss = false)
    {
        this.player = player;
        this.amount = amount;
        this.source = source;
        this.interruptAiTurnOnLoss = interruptAiTurnOnLoss;
    }

    public int Amount => amount;

    public override GameAction DeepCopy(Player newOwner)
    {
        ChangeHeartStringsAction copy = (ChangeHeartStringsAction)MemberwiseClone();
        copy.player = newOwner.GameState.GetTargetByID<Player>(player.GetID());
        if (source != null)
        {
            copy.source = newOwner.GameState.GetTargetByID<ITarget>(source.GetID());
        }
        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        int newCount = Mathf.Clamp(player.CurrentHeartStrings + amount, 0, Player.MaxHeartStrings);
        if (newCount == player.CurrentHeartStrings)
        {
            base.Execute(simulated, false);
            return;
        }

        if (!simulated)
        {
            Debug.LogWarning("ChangeHeartStringsAction: " + player.GetName() + " " + amount + " heartstring(s) (" + player.CurrentHeartStrings + " remaining)");
        }

        int actualChange = newCount - player.CurrentHeartStrings;
        player.CurrentHeartStrings = newCount;
        player.OnHeartStringsChange?.Invoke();

        if (!simulated && player.IsHuman && Controller.Instance != null)
            Controller.Instance.RunHeartStrings = player.CurrentHeartStrings;

        if (actualChange < 0 && interruptAiTurnOnLoss && !player.GameState.CurrentPlayer.IsHuman)
        {
            player.GameState.ActionHandler.ClearStack();
            TryEndTurnAction tryEndTurnAction = new TryEndTurnAction(player.GameState.CurrentPlayer);
            player.GameState.ActionHandler.AddAction(tryEndTurnAction, true, true, false);

            // EndTurnAction endTurnAction = new EndTurnAction(player.GameState.CurrentPlayer);
            // player.GameState.ActionHandler.AddAction(endTurnAction, true, true, false);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>();

        if (amount > 0)
        {
            animationActions.Add(new GainHeartStringAnimation(this, player));
        }
        else if (amount < 0)
        {
            animationActions.Add(new LoseHeartStringAnimation(this, player));
        }

        return animationActions;
    }

    public override void LogAction()
    {
        string changeText = amount > 0 ? "gains" : "loses";
        Debug.LogWarning("ChangeHeartStringsAction: " + player.GetName() + " " + changeText + " " + Mathf.Abs(amount) + " heartstring(s) (" + player.CurrentHeartStrings + " remaining)");
    }
}
