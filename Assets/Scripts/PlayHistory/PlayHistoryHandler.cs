using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayHistoryHandler
{
    private List<PlayHistoryItem> ThisTurnsItems = new List<PlayHistoryItem>();
    private List<PlayHistoryItem> LastTurnsItems = new List<PlayHistoryItem>();

    private List<PlayHistoryItem> PlayHistoryItems = new List<PlayHistoryItem>();

    public List<PlayHistoryItem> GetPlayHistoryItems()
    {
        PlayHistoryItems = new List<PlayHistoryItem>();

        PlayHistoryItems.AddRange(ThisTurnsItems);
        PlayHistoryItems.AddRange(LastTurnsItems);

        return PlayHistoryItems;
    }
    public void AddPlayHistoryEntry(PlayHistoryItem newItem)
    {
        ThisTurnsItems.Insert(0, newItem);
    }
    public void ProgressPlayHistory()
    {
        LastTurnsItems.Clear();
        LastTurnsItems.AddRange(ThisTurnsItems);
        ThisTurnsItems.Clear();
    }
    public void Clear()
    {
        LastTurnsItems.Clear();
        ThisTurnsItems.Clear();
    }
}

public abstract class PlayHistoryItem
{
    public Player Owner;
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
    Ritual
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