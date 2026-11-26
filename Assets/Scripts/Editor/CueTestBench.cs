using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editor-only helpers for staging combat cues in play mode from MCP execute_code (after Controller.StartTestChamber()):
/// put followers on either side, make them attack, cast spells, grant trinkets or ritual buffs. Names are class names.
///     CueTestBench.Clear(); CueTestBench.Summon(true, "Siren"); CueTestBench.Summon(false, "Hoplite");
///     CueTestBench.Attack(false, 0, true, 0);   // AI follower 0 attacks human follower 0
///     CueTestBench.Cast(false, "Roar", "player:human");
/// Everything goes through real actions, so cues, history and animations behave as in a fight.
/// </summary>
public static class CueTestBench
{
    static GameState State => Controller.Instance.CanonGameState;

    public static Player Side(bool human) => human ? State.Human : State.AI;

    public static Follower Summon(bool human, string followerClass, int index = -1)
    {
        Player owner = Side(human);
        Follower follower = (Follower)Activator.CreateInstance(FindType(followerClass));
        follower.Init(owner);
        State.ActionHandler.AddAction(new SummonFollowerAction(follower, index, false));
        return follower;
    }

    /// <summary>
    /// Starts the test chamber if needed, sets both players to 60 health (the chamber starts very low), clears the
    /// rows and summons the comma-separated follower classes for each side.
    /// </summary>
    public static void Stage(string human, string ai)
    {
        if (Controller.Instance.CanonGameState == null)
            Controller.Instance.StartTestChamber();
        foreach (Player player in new[] { State.Human, State.AI })
        {
            player.Health = 60;
            ViewPlayer view = View.Instance.GetViewPlayer(player);
            if (view != null)
                view.SetHealth(60);
        }
        Clear();
        foreach (string name in human.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            Summon(true, name.Trim());
        foreach (string name in ai.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            Summon(false, name.Trim());
    }

    /// <summary>Removes every follower from both rows without death animations.</summary>
    public static void Clear()
    {
        foreach (Player player in new[] { State.Human, State.AI })
        {
            foreach (Follower follower in new List<Follower>(player.BattleRow.Followers))
                State.ActionHandler.AddAction(new KillFollowerAction(player, follower));
        }
    }

    public static void Attack(bool attackerHuman, int attackerIndex, bool targetHuman, int targetIndex)
    {
        Follower attacker = Side(attackerHuman).BattleRow.Followers[attackerIndex];
        ITarget target = targetIndex < 0 ? Side(targetHuman) : (ITarget)Side(targetHuman).BattleRow.Followers[targetIndex];
        State.ActionHandler.AddAction(new PreAttackWithFollowerAction(attacker, target));
    }

    public static void Damage(bool human, int index, int amount)
    {
        Follower follower = Side(human).BattleRow.Followers[index];
        State.ActionHandler.AddAction(new DealDamageAction(Side(!human), follower, amount));
    }

    /// <summary>Target: "player:human", "player:ai", "human:N", "ai:N" or null.</summary>
    public static void Cast(bool human, string spellClass, string target)
    {
        Player owner = Side(human);
        Spell spell = (Spell)Activator.CreateInstance(FindType(spellClass));
        spell.Init(owner);
        foreach (OfferingType type in new List<OfferingType>(spell.Costs.Keys))
            spell.Costs[type] = 0;
        owner.Hand.Add(spell);
        owner.TryPlaySpell(spell, ParseTarget(target));
    }

    public static void GiveTrinket(bool human, string trinketClass)
    {
        Trinket trinket = (Trinket)Activator.CreateInstance(FindType(trinketClass));
        trinket.Owner = Side(human);
        trinket.ApplyEffect(Side(human));
    }

    public static void GiveEffect(bool human, StaticPlayerEffect effect)
    {
        Side(human).AddPlayerEffect(effect);
    }

    public static ITarget ParseTarget(string target)
    {
        if (string.IsNullOrEmpty(target))
            return null;
        string[] parts = target.Split(':');
        if (parts[0] == "player")
            return Side(parts[1] == "human");
        return Side(parts[0] == "human").BattleRow.Followers[int.Parse(parts[1])];
    }

    public static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(name);
            if (type != null)
                return type;
        }
        throw new ArgumentException("No type " + name);
    }

    public static void Shot(string file)
    {
        ScreenCapture.CaptureScreenshot(file);
    }

    /// <summary>The animation queue from index `from`, one line each (cues show source, trigger and sound).</summary>
    public static string DumpQueue(int from = 0)
    {
        var queue = View.Instance.AnimationHandler.AnimationActionQueue;
        var field = typeof(TriggerCueAnimation).GetField("cue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        List<string> lines = new List<string>();
        for (int i = from; i < queue.Count; i++)
        {
            AnimationAction animation = queue[i];
            string extra = "";
            if (animation is TriggerCueAnimation)
            {
                TriggerCue cue = (TriggerCue)field.GetValue(animation);
                extra = " " + cue.SourceName + "." + cue.Trigger + " -> " + cue.Def.Sound;
            }
            else if (animation.GameAction is FollowerDeathAction death)
                extra = " " + death.Follower.GetType().Name;
            else if (animation.GameAction is CreateOfferingAction offering)
                extra = " " + offering.Offering + " from #" + offering.SourceID;
            else if (animation.GameAction != null)
                extra = " (" + animation.GameAction.GetType().Name + ")";
            lines.Add(animation.GetType().Name + extra);
        }
        return string.Join("\n", lines);
    }

    public static int QueueCount => View.Instance.AnimationHandler.AnimationActionQueue.Count;

    /// <summary>Logs how long the animation queue takes to drain from now (game seconds), as "CueTestBench: drained in Xs".</summary>
    public static void TimeUntilIdle(string label)
    {
        float start = Time.time;
        void Check()
        {
            if (Idle)
                Debug.Log("CueTestBench: " + label + " drained in " + (Time.time - start).ToString("0.00") + "s");
            else
                CuePlayer.Hold(0.05f, Check);
        }
        CuePlayer.Hold(0.05f, Check);
    }

    /// <summary>True when no animation is playing or queued.</summary>
    public static bool Idle => !AnimationHandler.IsAnimating && View.Instance.AnimationHandler.AnimationActionQueue.Count == 0;

    /// <summary>
    /// Captures count frames every step game-seconds into Claude/Cues/Shots/film_NN.png at the given time scale
    /// (trigger flashes run on unscaled time, so film them at 1). Restores the time scale afterwards.
    /// Contact sheet: python Tools/VFX/film_sheet.py name x0 y0 x1 y1 [cols].
    /// </summary>
    public static void Film(float step, int count, float timeScale = 1f)
    {
        string dir = System.IO.Path.GetFullPath("Claude/Cues/Shots/");
        System.IO.Directory.CreateDirectory(dir);
        Time.timeScale = timeScale;
        for (int i = 0; i < count; i++)
        {
            int k = i;
            CuePlayer.Hold(step * i, () => ScreenCapture.CaptureScreenshot(dir + "film_" + k.ToString("00") + ".png"));
        }
        CuePlayer.Hold(step * count + 0.1f, () => Time.timeScale = 1f);
    }
}
