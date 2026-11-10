using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class StaticEffectReminders
{
    const string LifeSiphonText = "<b>Life Siphon</b>\nHeals you for the damage this deals.";

    static readonly StaticEffect[] AllEffects = (StaticEffect[])Enum.GetValues(typeof(StaticEffect));
    static readonly KeywordMention[] Mentions;
    static readonly List<int> MentionOrder = new List<int>();
    static readonly List<int> MentionIndex = new List<int>();

    struct KeywordMention
    {
        public Regex Pattern;
        public string Reminder;
    }

    static StaticEffectReminders()
    {
        var mentions = new List<KeywordMention>();
        for (int i = 0; i < AllEffects.Length; i++)
        {
            StaticEffect effect = AllEffects[i];
            if (effect == StaticEffect.None)
                continue;

            mentions.Add(new KeywordMention
            {
                Pattern = BuildPattern(effect),
                Reminder = GetText(effect),
            });
        }

        mentions.Add(new KeywordMention
        {
            Pattern = new Regex(@"\blife\s+siphon\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
            Reminder = LifeSiphonText,
        });

        Mentions = mentions.ToArray();
    }

    public static void Collect(Follower follower, List<string> results)
    {
        results.Clear();
        if (follower == null)
            return;

        for (int i = 0; i < AllEffects.Length; i++)
        {
            StaticEffect effect = AllEffects[i];
            if (effect == StaticEffect.None)
                continue;

            if (follower.HasStaticEffect(effect) || GrantsOrHasInnate(follower, effect))
                results.Add(GetText(effect));
        }

        if (HasLifeSiphon(follower))
            results.Add(LifeSiphonText);
    }

    /// <summary>
    /// Keyword reminders named in <paramref name="text"/>, in the order they appear.
    /// "Can't attack this turn" is a temporary restriction, not the Can't Attack keyword.
    /// </summary>
    public static void CollectFromText(string text, List<string> results)
    {
        results.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        MentionOrder.Clear();
        MentionIndex.Clear();
        for (int i = 0; i < Mentions.Length; i++)
        {
            Match match = Mentions[i].Pattern.Match(text);
            if (!match.Success)
                continue;

            int index = match.Index;
            int insert = MentionOrder.Count;
            while (insert > 0 && MentionOrder[insert - 1] > index)
                insert--;

            MentionOrder.Insert(insert, index);
            MentionIndex.Insert(insert, i);
        }

        for (int i = 0; i < MentionIndex.Count; i++)
            results.Add(Mentions[MentionIndex[i]].Reminder);
    }

    static Regex BuildPattern(StaticEffect effect)
    {
        if (effect == StaticEffect.CantAttack)
            return new Regex(@"\b(?:can't|cant)\s+attack\b(?!\s+this\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        string phrase = effect.ToString();
        if (effect == StaticEffect.RangedAttacker)
            phrase = "Ranged Attacker";
        else if (effect == StaticEffect.LowVision)
            phrase = "Low Vision";

        string body = Regex.Escape(phrase).Replace("\\ ", "\\s+");
        return new Regex(@"\b" + body + @"\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static string GetText(StaticEffect effect)
    {
        switch (effect)
        {
            case StaticEffect.Sprint:
                return "<b>Sprint</b>\nCan attack the turn it is played.";
            case StaticEffect.Shield:
                return "<b>Shield</b>\nTakes 1 less damage.";
            case StaticEffect.Unstoppable:
                return "<b>Unstoppable</b>\nTakes no damage when attacking.";
            case StaticEffect.Cleave:
                return "<b>Cleave</b>\nAlso deals its attack damage to adjacent enemies.";
            case StaticEffect.Taunt:
                return "<b>Taunt</b>\nMust be attacked before other followers.";
            case StaticEffect.RangedAttacker:
                return "<b>Ranged Attacker</b>\nAttacks enemies outside melee range and takes no damage when attacking.";
            case StaticEffect.Frenzy:
                return "<b>Frenzy</b>\nCan attack twice each turn.";
            case StaticEffect.CantAttack:
                return "<b>Can't Attack</b>\nCannot attack.";
            case StaticEffect.LowVision:
                return "<b>Low Vision</b>\nCan only attack followers directly in front of it.";
            default:
                return effect.ToString();
        }
    }

    static bool GrantsOrHasInnate(Follower follower, StaticEffect effect)
    {
        if (follower.InnateEffects == null)
            return false;

        for (int i = 0; i < follower.InnateEffects.Count; i++)
        {
            FollowerEffect innate = follower.InnateEffects[i];
            if (innate is StaticEffectDef def
                && def.StaticEffectName == effect
                && def.StaticEffectName != StaticEffect.None)
                return true;

            if (innate == null || innate.EffectInstances == null)
                continue;

            for (int j = 0; j < innate.EffectInstances.Count; j++)
            {
                if (innate.EffectInstances[j] is GiveFollowerStaticEffectInstance grant
                    && grant.StaticEffectName == effect
                    && (grant.TargetType == EffectTarget.AdjacentAllies || grant.TargetType == EffectTarget.AllAllies))
                    return true;
            }
        }

        return false;
    }

    static bool HasLifeSiphon(Follower follower)
    {
        if (follower.InnateEffects != null)
        {
            for (int i = 0; i < follower.InnateEffects.Count; i++)
            {
                if (follower.InnateEffects[i] is LifeSiphonEffectDef)
                    return true;
            }
        }

        if (follower.OnDamageEffects == null)
            return false;

        foreach (TriggeredFollowerEffectInstance effect in follower.OnDamageEffects)
        {
            if (effect != null && effect.EffectDef is LifeSiphonEffectDef)
                return true;
        }

        return false;
    }
}
