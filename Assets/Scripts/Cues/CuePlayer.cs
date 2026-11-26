using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays cue sounds (Resources/Audio/SFX/Cues/&lt;Id&gt;.wav, variants &lt;Id&gt;_2, &lt;Id&gt;_3 ... rotate) and works out the
/// beats triggered abilities hold the animation queue for. Combos fire the same ability many times in a row, so a
/// sound repeated within RepeatWindow climbs a semitone per repeat (like the offering collect notes), gets a little
/// quieter, and its beat shortens: ten triggers read as a run of notes, not a wall of noise.
/// Sounds are rendered by Tools/Audio/build_cue_sounds.py; overall levels are AudioHandler.CueVolume / SpellCastVolume.
/// </summary>
public static class CuePlayer
{
    public const string Folder = "Audio/SFX/Cues/";

    const float RepeatWindow = 0.75f;      // seconds between plays of one sound that still count as a chain
    const int MaxRepeatSemitones = 7;
    const float SameInstant = 0.06f;       // a second request this close is the same event (trinket flare + its cue)

    const float BaseBeat = 0.14f;
    const float MinBeat = 0.05f;
    const float AiTurnSlowdown = 1.25f;

    class Chain
    {
        public float LastTime = -10f;
        public int Repeat;
    }

    static readonly Dictionary<string, Chain> chains = new Dictionary<string, Chain>();
    static readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
    static readonly Dictionary<string, int> nextVariant = new Dictionary<string, int>();
    static readonly HashSet<string> loggedMissing = new HashSet<string>();

    /// <summary>Plays a trigger cue sound. Returns how many times it has repeated in the current chain (0 = first).</summary>
    public static int Play(string id, float volume = 1f)
    {
        return PlayInternal(id, volume, false);
    }

    /// <summary>Plays a spell's cast sound (its own volume control; spells never form chains).</summary>
    public static void PlayCast(string id, float volume = 1f)
    {
        PlayInternal(id, volume, true);
    }

    static int PlayInternal(string id, float volume, bool cast)
    {
        if (string.IsNullOrEmpty(id))
            return 0;

        float now = Time.unscaledTime;
        if (!chains.TryGetValue(id, out Chain chain))
        {
            chain = new Chain();
            chains[id] = chain;
        }

        if (now - chain.LastTime < SameInstant)
            return chain.Repeat;

        chain.Repeat = !cast && now - chain.LastTime < RepeatWindow ? chain.Repeat + 1 : 0;
        chain.LastTime = now;

        AudioClip clip = NextClip(id);
        AudioHandler audio = View.Instance != null ? View.Instance.AudioHandler : null;
        if (clip == null || audio == null)
            return chain.Repeat;

        int step = Mathf.Min(chain.Repeat, MaxRepeatSemitones);
        float pitch = Mathf.Pow(2f, step / 12f);
        float level = volume * Mathf.Max(0.7f, Mathf.Pow(0.94f, chain.Repeat));
        level *= cast ? audio.SpellCastVolume : audio.CueVolume;
        audio.PlayPitched(clip, level, pitch);
        return chain.Repeat;
    }

    /// <summary>How long a trigger holds the queue: short, shorter still for repeats, a little longer on the AI's turn.</summary>
    public static float Beat(int repeat)
    {
        float beat = Mathf.Max(MinBeat, BaseBeat * Mathf.Pow(0.7f, repeat));
        if (View.Instance != null && !View.Instance.IsHumansTurn)
            beat *= AiTurnSlowdown;
        return beat;
    }

    public static void Hold(float seconds, Action then)
    {
        if (then == null)
            return;
        if (seconds <= 0f)
        {
            then();
            return;
        }

        Sequence hold = new Sequence();
        hold.Add(new Tween(null, 0f, 1f, seconds));
        hold.Add(new SequenceAction(then));
        hold.Start();
    }

    /// <summary>True when a clip exists for the id (used by the coverage report).</summary>
    public static bool HasClip(string id)
    {
        return Load(id).Length > 0;
    }

    static AudioClip NextClip(string id)
    {
        AudioClip[] variants = Load(id);
        if (variants.Length == 0)
        {
            if (loggedMissing.Add(id))
                Debug.LogWarning("Missing cue sound at Resources/" + Folder + id);
            return null;
        }

        nextVariant.TryGetValue(id, out int index);
        nextVariant[id] = (index + 1) % variants.Length;
        return variants[index % variants.Length];
    }

    static AudioClip[] Load(string id)
    {
        if (clips.TryGetValue(id, out AudioClip[] found))
            return found;

        List<AudioClip> list = new List<AudioClip>();
        AudioClip first = Resources.Load<AudioClip>(Folder + id);
        if (first != null)
        {
            list.Add(first);
            for (int i = 2; i < 10; i++)
            {
                AudioClip variant = Resources.Load<AudioClip>(Folder + id + "_" + i);
                if (variant == null)
                    break;
                list.Add(variant);
            }
        }

        found = list.ToArray();
        clips[id] = found;
        return found;
    }
}
