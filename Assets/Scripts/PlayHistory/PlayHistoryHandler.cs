using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The canon game's play history: this turn's and last turn's entries, newest first.
/// Plays, attacks and rituals are added by their actions (GameAction.MakePlayHistoryItem). Triggered abilities,
/// trinkets and ritual buffs are TriggeredEffectPlayHistory records that their actions fill in as they resolve.
/// A reaction can resolve before the play that caused it (a spell's On Targeted triggers run before its
/// PlaySpellAction), so while a play is pending (ExpectEntry) reactions are held back and inserted just above
/// the play's entry once it arrives.
/// </summary>
public class PlayHistoryHandler
{
    private List<PlayHistoryItem> ThisTurnsItems = new List<PlayHistoryItem>();
    private List<PlayHistoryItem> LastTurnsItems = new List<PlayHistoryItem>();

    private List<PlayHistoryItem> PlayHistoryItems = new List<PlayHistoryItem>();

    readonly List<TriggeredEffectPlayHistory> heldRecords = new List<TriggeredEffectPlayHistory>();
    bool expectingEntry;
    // Cards being played right now (Spell.Play): their reactions may resolve in an action chain that ends before
    // the play's own entry is even queued, so the end of that chain must not release them.
    int playsInProgress;
    // Counts outermost action-queue evaluations, so one trinket's actions within one chain share a record.
    int evaluationChain;

    public List<PlayHistoryItem> GetPlayHistoryItems()
    {
        PlayHistoryItems = new List<PlayHistoryItem>();

        PlayHistoryItems.AddRange(ThisTurnsItems);
        PlayHistoryItems.AddRange(LastTurnsItems);

        return PlayHistoryItems;
    }
    public void AddPlayHistoryEntry(PlayHistoryItem newItem)
    {
        StampTurn(newItem);
        ThisTurnsItems.Insert(0, newItem);

        // Reactions held back for this entry go above it (they happened after it).
        expectingEntry = false;
        FlushHeldRecords();
    }

    /// <summary>A play, attack or ritual is under way; hold reactions until its entry is added.</summary>
    public void ExpectEntry()
    {
        expectingEntry = true;
    }

    /// <summary>Adds a triggered-effect record the first time one of its actions resolves.</summary>
    public void AddTriggeredRecord(TriggeredEffectPlayHistory record)
    {
        if (record == null || record.Added)
            return;

        record.Added = true;
        record.Chain = evaluationChain;
        if (expectingEntry)
        {
            heldRecords.Add(record);
            return;
        }

        StampTurn(record);
        ThisTurnsItems.Insert(0, record);
    }

    /// <summary>The open record for a trinket or ritual buff in this chain of actions, or a new one.</summary>
    public TriggeredEffectPlayHistory GetEffectRecord(StaticPlayerEffect effect)
    {
        foreach (TriggeredEffectPlayHistory held in heldRecords)
        {
            if (held.SourceEffect == effect)
                return held;
        }

        foreach (PlayHistoryItem item in ThisTurnsItems)
        {
            if (item is TriggeredEffectPlayHistory record && record.SourceEffect == effect && record.Chain == evaluationChain)
                return record;
        }

        return new TriggeredEffectPlayHistory(effect);
    }

    public void BeginPlay()
    {
        playsInProgress++;
        expectingEntry = true;
    }

    public void EndPlay()
    {
        playsInProgress = Mathf.Max(0, playsInProgress - 1);
    }

    public void BeginEvaluationChain()
    {
        evaluationChain++;
    }

    /// <summary>Called when the action queue empties: a play that never arrived (its attacker died first) releases its reactions.</summary>
    public void EndEvaluationChain()
    {
        if (playsInProgress > 0)
            return;

        expectingEntry = false;
        FlushHeldRecords();
    }

    void FlushHeldRecords()
    {
        foreach (TriggeredEffectPlayHistory record in heldRecords)
        {
            StampTurn(record);
            ThisTurnsItems.Insert(0, record);
        }

        heldRecords.Clear();
    }

    static void StampTurn(PlayHistoryItem item)
    {
        if (item.TurnPlayer == null)
            item.TurnPlayer = Controller.Instance != null ? Controller.Instance.CanonGameState?.CurrentPlayer : null;
        if (item.TurnPlayer == null)
            item.TurnPlayer = item.Owner;
    }

    public void ProgressPlayHistory()
    {
        playsInProgress = 0;
        EndEvaluationChain();
        LastTurnsItems.Clear();
        LastTurnsItems.AddRange(ThisTurnsItems);
        ThisTurnsItems.Clear();
    }
    public void Clear()
    {
        LastTurnsItems.Clear();
        ThisTurnsItems.Clear();
        heldRecords.Clear();
        expectingEntry = false;
        playsInProgress = 0;
    }

    /// <summary>The canon game's history, or null (no controller, or a simulated game state).</summary>
    public static PlayHistoryHandler For(GameState gameState)
    {
        if (gameState == null || gameState.IsSimulated || Controller.Instance == null)
            return null;
        return Controller.Instance.PlayHistoryHandler;
    }
}

public abstract class PlayHistoryItem
{
    public Player Owner;
    // Whose turn it happened in; the view groups entries by this (a reaction can belong to the other player).
    public Player TurnPlayer;
    public PlayHistoryType Type;
    public abstract List<PlayHistoryComponent> GetComponents();
}
public class PlayFollowerPlayHistory : PlayHistoryItem
{
    public Follower Follower;
    public PlayFollowerPlayHistory(Player owner, Follower follower)
    {
        Owner = owner;
        Follower = follower;
    }
    public override List<PlayHistoryComponent> GetComponents()
    {
        List<PlayHistoryComponent> playHistoryComponents = new List<PlayHistoryComponent>();
        playHistoryComponents.Add(new FollowerPlayHistoryComponent(Follower));
        return playHistoryComponents;
    }
}
public class PlaySpellPlayHistory : PlayHistoryItem
{
    public Spell Spell;
    public ITarget Target;
    // A buff the spell granted (Twist of Fate), shown after its target. The spell sets it after its entry is made,
    // so it is read when the view loads.
    public Trinket GrantedTrinket => grantedTrinket ?? (grantedTrinket = Spell?.GrantedTrinket);
    Trinket grantedTrinket;
    public PlaySpellPlayHistory(Player owner, Spell spell, ITarget target)
    {
        Owner = owner;
        Spell = spell;
        Target = target;
    }
    public override List<PlayHistoryComponent> GetComponents()
    {
        List<PlayHistoryComponent> playHistoryComponents = new List<PlayHistoryComponent>();
        playHistoryComponents.Add(new SpellPlayHistoryComponent(Spell));
        if (Target != null)
        {
            playHistoryComponents.Add(new TargetPlayHistoryComponent());

            if (Target is Player targetPlayer)
            {
                playHistoryComponents.Add(new PlayerPlayHistoryComponent(targetPlayer, targetPlayer.Health));
            }
            else if (Target is Follower targetFollower)
            {
                playHistoryComponents.Add(new FollowerPlayHistoryComponent(targetFollower));
            }
        }
        if (GrantedTrinket != null)
            playHistoryComponents.Add(new EffectPlayHistoryComponent(GrantedTrinket));
        return playHistoryComponents;
    }
}
public class AttackWithFollowerPlayHistory : PlayHistoryItem
{
    public Follower Attacker;
    public ITarget Target;
    public AttackWithFollowerPlayHistory(Player owner, Follower attacker, ITarget target)
    {
        Owner = owner;
        Attacker = attacker;
        Target = target;
    }
    public override List<PlayHistoryComponent> GetComponents()
    {
        List<PlayHistoryComponent> playHistoryComponents = new List<PlayHistoryComponent>();
        playHistoryComponents.Add(new FollowerPlayHistoryComponent(Attacker));
        playHistoryComponents.Add(new AttackPlayHistoryComponent());
        if (Target is Player targetPlayer)
        {
            playHistoryComponents.Add(new PlayerPlayHistoryComponent(targetPlayer, targetPlayer.Health));
        }
        else if (Target is Follower targetFollower)
        {
            playHistoryComponents.Add(new FollowerPlayHistoryComponent(targetFollower));
        }
        return playHistoryComponents;
    }
}
public class RitualPlayHistory : PlayHistoryItem
{
    public Ritual Ritual;
    public ITarget Target;
    public RitualPlayHistory(Player owner, Ritual ritual, ITarget target)
    {
        Owner = owner;
        Ritual = ritual;
        Target = target;
    }
    public override List<PlayHistoryComponent> GetComponents()
    {
        List<PlayHistoryComponent> playHistoryComponents = new List<PlayHistoryComponent>();
        playHistoryComponents.Add(new RitualPlayHistoryComponent(Ritual));
        playHistoryComponents.Add(new TargetPlayHistoryComponent());
        if (Target is Player targetPlayer)
        {
            playHistoryComponents.Add(new PlayerPlayHistoryComponent(targetPlayer, targetPlayer.Health));
        }
        else if (Target is Follower targetFollower)
        {
            playHistoryComponents.Add(new FollowerPlayHistoryComponent(targetFollower));
        }
        return playHistoryComponents;
    }
}
public enum PlayHistoryType
{
    None,
    PlayFollower,
    PlaySpell,
    AttackWithFollower,
    UseRitual,
}
public abstract class PlayHistoryComponent
{
    public abstract PlayHistoryComponentType GetComponentType();
}
// Maybe don't need this
public enum PlayHistoryComponentType
{
    Player,
    Follower,
    Spell,
    Target,
    Attack,
    Ritual,
    TriggerIcon,
    Effect,
}
public class PlayerPlayHistoryComponent : PlayHistoryComponent
{
    public Player Player;
    public int Health;
    public bool ShowHealth;
    public PlayerPlayHistoryComponent(Player player, int health, bool showHealth = true)
    {
        Player = player;
        Health = health;
        ShowHealth = showHealth;
    }
    public override PlayHistoryComponentType GetComponentType()
    {
        return PlayHistoryComponentType.Player;
    }
}
public class FollowerPlayHistoryComponent : PlayHistoryComponent
{
    public Follower Follower;
    public FollowerPlayHistoryComponent(Follower follower)
    {
        Follower = follower;
    }
    public override PlayHistoryComponentType GetComponentType()
    {
        return PlayHistoryComponentType.Follower;
    }
}
public class SpellPlayHistoryComponent : PlayHistoryComponent
{
    public Spell Spell;
    public SpellPlayHistoryComponent(Spell spell)
    {
        Spell = spell;
    }
    public override PlayHistoryComponentType GetComponentType()
    {
        return PlayHistoryComponentType.Spell;
    }
}
public class RitualPlayHistoryComponent : PlayHistoryComponent
{
    public Ritual Ritual;
    public RitualPlayHistoryComponent(Ritual ritual)
    {
        Ritual = ritual;
    }
    public override PlayHistoryComponentType GetComponentType()
    {
        return PlayHistoryComponentType.Ritual;
    }
}
public class TargetPlayHistoryComponent : PlayHistoryComponent
{
    public override PlayHistoryComponentType GetComponentType()
    {
        return PlayHistoryComponentType.Target;
    }
}
public class AttackPlayHistoryComponent : PlayHistoryComponent
{
    public override PlayHistoryComponentType GetComponentType()
    {
        return PlayHistoryComponentType.Attack;
    }
}
/// <summary>The trigger's icon in the verb column (one of Images/Icons/SummaryMono).</summary>
public class TriggerIconPlayHistoryComponent : PlayHistoryComponent
{
    public string IconName;
    public TriggerIconPlayHistoryComponent(string iconName)
    {
        IconName = iconName;
    }
    public override PlayHistoryComponentType GetComponentType()
    {
        return PlayHistoryComponentType.TriggerIcon;
    }
}

/// <summary>A trinket's or ritual buff's icon, as the source of a trigger or the result of a play.</summary>
public class EffectPlayHistoryComponent : PlayHistoryComponent
{
    public Sprite Icon;
    public string Name;

    public EffectPlayHistoryComponent(Trinket trinket)
    {
        Icon = trinket.GetDescriptionData()?.Icon;
        Name = trinket.Name;
    }

    public EffectPlayHistoryComponent(StaticPlayerEffect effect)
    {
        Icon = effect.GetDescriptionData()?.Icon;
        Name = GetTrinketName(effect);
    }

    // Trinket effects are named <X>TrinketEffectDef after their <X>Trinket, which holds the display name.
    static string GetTrinketName(StaticPlayerEffect effect)
    {
        string typeName = effect.GetType().Name;
        const string suffix = "EffectDef";
        if (!(effect is TrinketPlayerEffect) || !typeName.EndsWith(suffix))
            return null;

        Type trinketType = effect.GetType().Assembly.GetType(typeName.Substring(0, typeName.Length - suffix.Length));
        if (trinketType == null || !typeof(Trinket).IsAssignableFrom(trinketType) || trinketType.IsAbstract)
            return null;

        try
        {
            return (Activator.CreateInstance(trinketType) as Trinket)?.Name;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override PlayHistoryComponentType GetComponentType()
    {
        return PlayHistoryComponentType.Effect;
    }
}

/// <summary>
/// A triggered ability, trinket or ritual buff going off: its source, the trigger, and what it hit or made.
/// Created when the trigger fires (canon game only); its actions add their results as they resolve
/// (GameAction.AddPlayHistoryResults), and it enters the history when the first of them does.
/// </summary>
public class TriggeredEffectPlayHistory : PlayHistoryItem
{
    public const int MaxResults = 4;

    public Follower SourceFollower;
    public StaticPlayerEffect SourceEffect;
    public EffectTrigger Trigger;
    public bool Added;
    public int Chain;

    readonly List<Card> cardResults = new List<Card>();
    readonly List<Player> playerResults = new List<Player>();

    // The ability sits on a neighbour and fires on its event ("when an adjacent Follower dies").
    public bool ViaAlly;

    public TriggeredEffectPlayHistory(Follower source, EffectTrigger trigger, bool viaAlly = false)
    {
        SourceFollower = source;
        Owner = source.Owner;
        Trigger = trigger;
        ViaAlly = viaAlly;
    }

    public TriggeredEffectPlayHistory(StaticPlayerEffect effect)
    {
        SourceEffect = effect;
        Owner = effect.Owner;
    }

    /// <summary>Something the trigger hit, changed or made: a follower, a card added to hand, or a player.</summary>
    public void AddResult(ITarget target)
    {
        if (target == null || ReferenceEquals(target, SourceFollower))
            return;

        if (target is Card card)
        {
            if (!cardResults.Contains(card))
                cardResults.Add(card);
        }
        else if (target is Player player)
        {
            if (!playerResults.Contains(player))
                playerResults.Add(player);
        }
    }

    public string Label
    {
        get
        {
            if (SourceEffect != null)
                return "Triggered";

            if (ViaAlly)
            {
                switch (Trigger)
                {
                    case EffectTrigger.OnEnter: return "Ally Entered";
                    case EffectTrigger.OnDeath: return "Ally Died";
                    case EffectTrigger.OnAttack: return "Ally Attacks";
                    case EffectTrigger.OnAttacked: return "Ally Attacked";
                    case EffectTrigger.OnDamage: return "Ally Dealt Damage";
                    case EffectTrigger.OnDamaged: return "Ally Damaged";
                    case EffectTrigger.OnDrawBlood: return "Ally Drew Blood";
                    case EffectTrigger.OnKill: return "Ally Killed";
                    case EffectTrigger.OnTargeted: return "Ally Targeted";
                }
            }

            switch (Trigger)
            {
                case EffectTrigger.OnEnter: return "On Enter";
                case EffectTrigger.OnDeath: return "On Death";
                case EffectTrigger.OnAttack: return "On Attack";
                case EffectTrigger.OnAttacked: return "On Attacked";
                case EffectTrigger.OnDamage: return "On Damage";
                case EffectTrigger.OnDamaged: return "On Damaged";
                case EffectTrigger.OnDrawBlood: return "On Draw Blood";
                case EffectTrigger.OnKill: return "On Kill";
                case EffectTrigger.OnTargeted: return "On Targeted";
                case EffectTrigger.OnAnyFollowerDeath: return "A Follower Died";
                case EffectTrigger.OnAnyFollowerEnter: return "A Follower Entered";
                case EffectTrigger.OnAnySpellCast: return "Spell Cast";
                default: return "Triggered";
            }
        }
    }

    // Trigger icons reuse the follower summary glyphs (Images/Icons/SummaryMono).
    string IconName
    {
        get
        {
            if (SourceEffect != null)
                return "star";

            switch (Trigger)
            {
                case EffectTrigger.OnEnter: return "horn";
                case EffectTrigger.OnDeath:
                case EffectTrigger.OnAnyFollowerDeath: return "skull";
                case EffectTrigger.OnAttack: return "sword";
                case EffectTrigger.OnAttacked: return "shield";
                case EffectTrigger.OnDamage:
                case EffectTrigger.OnDamaged: return "fangs";
                case EffectTrigger.OnDrawBlood: return "blood";
                case EffectTrigger.OnKill: return "sickle";
                case EffectTrigger.OnTargeted: return "target";
                case EffectTrigger.OnAnySpellCast: return "scroll";
                default: return "star";
            }
        }
    }

    public override List<PlayHistoryComponent> GetComponents()
    {
        List<PlayHistoryComponent> components = new List<PlayHistoryComponent>();
        if (SourceFollower != null)
            components.Add(new FollowerPlayHistoryComponent(SourceFollower));
        else if (SourceEffect != null)
            components.Add(new EffectPlayHistoryComponent(SourceEffect));

        components.Add(new TriggerIconPlayHistoryComponent(IconName));

        int shown = 0;
        foreach (Card card in cardResults)
        {
            if (shown >= MaxResults) break;
            if (card is Follower follower)
                components.Add(new FollowerPlayHistoryComponent(follower));
            else if (card is Spell spell)
                components.Add(new SpellPlayHistoryComponent(spell));
            else
                continue;
            shown++;
        }
        foreach (Player player in playerResults)
        {
            if (shown >= MaxResults) break;
            components.Add(new PlayerPlayHistoryComponent(player, player.Health));
            shown++;
        }

        return components;
    }
}
