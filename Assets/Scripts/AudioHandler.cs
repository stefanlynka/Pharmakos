using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ProgressionHandler;


public class AudioHandler : MonoBehaviour
{
    public Slider MusicSlider;
    public Slider SoundEffectSlider;

    public AudioSource MusicSource;
    public AudioSource SoundEffectSource;
    public AudioSource OtherSource;
    
    private Dictionary<OfferingType, AudioClip> offeringCollectClips = new Dictionary<OfferingType, AudioClip>();
    public AudioSource offeringCollectSource;

    [Header("Offering Collect")]
    [Tooltip("Semitone offset of the 0→1 collect for this offering, relative to the recorded clip. Tweak in Play Mode.")]
    public float GoldBaseSemitones = 0f;
    public float BloodBaseSemitones = 0f;
    public float BoneBaseSemitones = 0f;
    public float CropBaseSemitones = 0f;
    public float ScrollBaseSemitones = 0f;
    [Tooltip("Rise in pitch for each additional offering of that type you hold.")]
    public float SemitonesPerOffering = 1f;
    [Tooltip("Base volume for this offering's collect note. Still scaled by the SFX slider.")]
    [Range(0f, 2f)] public float GoldBaseVolume = 1f;
    [Range(0f, 2f)] public float BloodBaseVolume = 1f;
    [Range(0f, 2f)] public float BoneBaseVolume = 1f;
    [Range(0f, 2f)] public float CropBaseVolume = 1f;
    [Range(0f, 2f)] public float ScrollBaseVolume = 1f;
    [Tooltip("Minimum time between queued collect notes, so stacked landings still punch one at a time.")]
    public float MinOfferingCollectInterval = 0.12f;

    public Dictionary<DeckName, AudioClip> MusicByName = new Dictionary<DeckName, AudioClip>();
    public Dictionary<SoundEffectType, AudioClip> SoundEffectsByName = new Dictionary<SoundEffectType, AudioClip>();
    public Dictionary<OtherSoundType, AudioClip> OtherAudioByName = new Dictionary<OtherSoundType, AudioClip>();

    public Dictionary<DeckName, float> MusicMultipliers = new Dictionary<DeckName, float>();

    [Header("Ritual Flames")]
    [Tooltip("Seconds after a ritual flare begins before the looping flame bed starts.")]
    public float FlameOngoingDelay = 2f;
    [Tooltip("Volume of the rumble that plays while a ritual is selected. Still scaled by the SFX slider.")]
    [Range(0f, 2f)] public float RumbleVolume = 0.8f;

    private float baseMusicVolume = 0.1f;

    private float userMusicVolume = 0.5f;
    private float userSoundEffectVolume = 0.5f;

    private float currentMusicMultiplier = 1.0f;

    [Header("Music Fade")]
    [Tooltip("Seconds for the music to fade down or back up (e.g. while a ritual is selected).")]
    public float MusicFadeDuration = 1.5f;
    [Tooltip("Fraction of normal music volume while faded down.")]
    [Range(0f, 1f)] public float MusicFadedVolume = 0.2f;
    private float musicFadeMultiplier = 1.0f;
    private float musicFadeTarget = 1.0f;

    private readonly Queue<OfferingCollectNote> offeringCollectQueue = new Queue<OfferingCollectNote>();
    private float nextOfferingCollectTime;

    AudioClip flameStartClip;
    AudioClip flameOngoingClip;
    AudioSource flameOngoingSource;
    readonly HashSet<ViewRitual> readyRituals = new HashSet<ViewRitual>();
    float flameOngoingStartTime = -1f;

    private struct OfferingCollectNote
    {
        public OfferingType Type;
        public int CountAfterCollect;
        public Action OnPlayed;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadMusic();
    }
    private void LoadMusic()
    {
        // Fill MusicByName from the Music in Resources
        IList deckNames = Enum.GetValues(typeof(DeckName));
        for (int i = 0; i < deckNames.Count; i++)
        {
            DeckName name = (DeckName)deckNames[i];
            AudioClip music = Resources.Load<AudioClip>("Audio/Music/" + name.ToString());
            if (music != null)
            {
                MusicByName[name] = music;
            }
        }

        // Fill MusicByName from the Music in Resources
        IList soundEffectNames = Enum.GetValues(typeof(SoundEffectType));
        for (int i = 0; i < deckNames.Count; i++)
        {
            SoundEffectType name = (SoundEffectType)deckNames[i];
            AudioClip music = Resources.Load<AudioClip>("Audio/SFX/" + name.ToString());
            if (music != null)
            {
                SoundEffectsByName[name] = music;
            }
        }

        MusicMultipliers[DeckName.Labyrinth] = 0.3f;
        MusicMultipliers[DeckName.Bacchanalia] = 0.3f;
        MusicMultipliers[DeckName.Cyclops] = 0.3f;
        MusicMultipliers[DeckName.SeasideCliffs] = 0.3f;
        MusicMultipliers[DeckName.Troy] = 0.25f;
        MusicMultipliers[DeckName.Hunt] = 0.3f;
        MusicMultipliers[DeckName.Trials] = 0.2f;
        MusicMultipliers[DeckName.Caves] = 0.2f;
        MusicMultipliers[DeckName.Delphi] = 0.4f;
        MusicMultipliers[DeckName.Throne] = 0.25f;
        MusicMultipliers[DeckName.Fates] = 0.25f;
        MusicMultipliers[DeckName.TheGate] = 0.25f;

        userMusicVolume = PlayerPrefs.GetFloat("UserMusicVolume", 0.5f);
        MusicSlider.value = userMusicVolume;
        userSoundEffectVolume = PlayerPrefs.GetFloat("UserSoundEffectVolume", 0.5f);
        SoundEffectSlider.value = userSoundEffectVolume;

        OtherAudioByName[OtherSoundType.Rumble] = Resources.Load<AudioClip>("Audio/Other/Rumble");

        LoadOfferingCollectSounds();
        LoadRitualFlameSounds();
    }

    private void LoadOfferingCollectSounds()
    {
        if (offeringCollectSource == null) offeringCollectSource = gameObject.AddComponent<AudioSource>();
        offeringCollectSource.playOnAwake = false;
        offeringCollectSource.spatialBlend = 0f;
        offeringCollectSource.loop = false;

        IList offeringTypes = Enum.GetValues(typeof(OfferingType));
        for (int i = 0; i < offeringTypes.Count; i++)
        {
            OfferingType type = (OfferingType)offeringTypes[i];
            if (type == OfferingType.None) continue;

            AudioClip clip = Resources.Load<AudioClip>("Audio/SFX/Offerings/" + type.ToString());
            if (clip != null)
            {
                offeringCollectClips[type] = clip;
            }
        }
    }

    public void QueueOfferingCollect(OfferingType type, int countAfterCollect, Action onPlayed = null)
    {
        offeringCollectQueue.Enqueue(new OfferingCollectNote
        {
            Type = type,
            CountAfterCollect = countAfterCollect,
            OnPlayed = onPlayed
        });
    }

    public void PreviewOfferingCollect(OfferingType type, int fromCount, int toCount)
    {
        offeringCollectQueue.Clear();
        nextOfferingCollectTime = 0f;
        if (offeringCollectSource != null)
            offeringCollectSource.Stop();

        int start = Mathf.Min(fromCount, toCount);
        int end = Mathf.Max(fromCount, toCount);
        for (int count = start; count <= end; count++)
        {
            QueueOfferingCollect(type, count);
        }
    }

    private float GetOfferingBaseSemitones(OfferingType type)
    {
        switch (type)
        {
            case OfferingType.Gold: return GoldBaseSemitones;
            case OfferingType.Blood: return BloodBaseSemitones;
            case OfferingType.Bone: return BoneBaseSemitones;
            case OfferingType.Crop: return CropBaseSemitones;
            case OfferingType.Scroll: return ScrollBaseSemitones;
            default: return 0f;
        }
    }

    private float GetOfferingBaseVolume(OfferingType type)
    {
        switch (type)
        {
            case OfferingType.Gold: return GoldBaseVolume;
            case OfferingType.Blood: return BloodBaseVolume;
            case OfferingType.Bone: return BoneBaseVolume;
            case OfferingType.Crop: return CropBaseVolume;
            case OfferingType.Scroll: return ScrollBaseVolume;
            default: return 1f;
        }
    }

    private void LoadRitualFlameSounds()
    {
        flameStartClip = Resources.Load<AudioClip>("Audio/SFX/Rituals/FlameStart");
        flameOngoingClip = Resources.Load<AudioClip>("Audio/SFX/Rituals/FlameOngoing");

        if (flameOngoingSource == null)
            flameOngoingSource = gameObject.AddComponent<AudioSource>();

        flameOngoingSource.playOnAwake = false;
        flameOngoingSource.spatialBlend = 0f;
        flameOngoingSource.loop = true;
    }

    public void NotifyRitualReady(ViewRitual ritual)
    {
        if (ritual == null || !readyRituals.Add(ritual))
            return;

        if (flameStartClip != null && SoundEffectSource != null)
            SoundEffectSource.PlayOneShot(flameStartClip, userSoundEffectVolume);

        if (flameOngoingSource != null && flameOngoingSource.isPlaying)
            return;

        if (flameOngoingStartTime < 0f)
            flameOngoingStartTime = Time.time + Mathf.Max(0f, FlameOngoingDelay);
    }

    public void NotifyRitualUnready(ViewRitual ritual)
    {
        if (ritual == null || !readyRituals.Remove(ritual))
            return;

        if (readyRituals.Count > 0)
            return;

        flameOngoingStartTime = -1f;
        if (flameOngoingSource != null && flameOngoingSource.isPlaying)
            flameOngoingSource.Stop();
    }

    public void FadeMusicOut()
    {
        musicFadeTarget = MusicFadedVolume;
    }

    public void FadeMusicIn()
    {
        musicFadeTarget = 1f;
    }

    private void UpdateMusicFade()
    {
        if (Mathf.Approximately(musicFadeMultiplier, musicFadeTarget))
            return;

        float fadeRange = Mathf.Max(0.01f, 1f - MusicFadedVolume);
        float step = MusicFadeDuration > 0f ? Time.unscaledDeltaTime * fadeRange / MusicFadeDuration : 1f;
        musicFadeMultiplier = Mathf.MoveTowards(musicFadeMultiplier, musicFadeTarget, step);
        ApplyMusicVolume();
    }

    private void ApplyMusicVolume()
    {
        if (MusicSource != null)
            MusicSource.volume = baseMusicVolume * userMusicVolume * currentMusicMultiplier * musicFadeMultiplier;
    }

    private void LateUpdate()
    {
        UpdateMusicFade();
        UpdateRitualFlame();

        if (offeringCollectQueue.Count == 0)
            return;

        if (Time.time < nextOfferingCollectTime)
            return;

        OfferingCollectNote note = offeringCollectQueue.Dequeue();
        float wait = Mathf.Max(0.01f, MinOfferingCollectInterval);

        if (offeringCollectClips.TryGetValue(note.Type, out AudioClip clip) && clip != null && offeringCollectSource != null)
        {
            float pitch = GetOfferingPitch(note.Type, note.CountAfterCollect);
            offeringCollectSource.Stop();
            offeringCollectSource.pitch = pitch;
            offeringCollectSource.volume = GetOfferingBaseVolume(note.Type) * userSoundEffectVolume;
            offeringCollectSource.clip = clip;
            offeringCollectSource.Play();
            wait = Mathf.Max(wait, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)));
        }

        nextOfferingCollectTime = Time.time + wait;
        note.OnPlayed?.Invoke();
    }

    private void UpdateRitualFlame()
    {
        if (flameOngoingStartTime < 0f || Time.time < flameOngoingStartTime)
            return;

        flameOngoingStartTime = -1f;
        if (readyRituals.Count == 0 || flameOngoingClip == null || flameOngoingSource == null)
            return;

        flameOngoingSource.clip = flameOngoingClip;
        flameOngoingSource.loop = true;
        flameOngoingSource.volume = userSoundEffectVolume;
        flameOngoingSource.Play();
    }

    private float GetOfferingPitch(OfferingType type, int countAfterCollect)
    {
        int stepsAboveBase = Mathf.Max(0, countAfterCollect - 1);
        float semitones = GetOfferingBaseSemitones(type) + stepsAboveBase * SemitonesPerOffering;
        return Mathf.Clamp(Mathf.Pow(2f, semitones / 12f), 0.5f, 3f);
    }

    public void PlayMusic(DeckName name)
    {
        if (MusicByName.ContainsKey(name))
        {
            AudioClip audioClip = MusicByName[name];
            if (audioClip != null)
            {
                MusicSource.clip = audioClip;

                if (MusicMultipliers.ContainsKey(name))
                {
                    currentMusicMultiplier = MusicMultipliers[name];
                }

                ApplyMusicVolume();

                MusicSource.Play();
            }
        }
    }

    public void PlaySoundEffect(SoundEffectType name)
    {
        if (SoundEffectsByName.ContainsKey(name))
        {
            AudioClip audioClip = SoundEffectsByName[name];
            if (audioClip != null)
            {
                //AudioSource.clip = audioClip;
                switch(name)
                {
                    case SoundEffectType.CardDraw:
                        SoundEffectSource.PlayOneShot(audioClip, userSoundEffectVolume * 0.1f);
                        break;
                    case SoundEffectType.Impact:
                        SoundEffectSource.PlayOneShot(audioClip, userSoundEffectVolume * 0.5f);
                        break;
                    case SoundEffectType.Blood:
                        SoundEffectSource.PlayOneShot(audioClip, userSoundEffectVolume * 0.25f);
                        break;
                    case SoundEffectType.Scroll:
                        SoundEffectSource.PlayOneShot(audioClip, userSoundEffectVolume * 0.6f);
                        break;
                    case SoundEffectType.Defeat:
                        SoundEffectSource.PlayOneShot(audioClip, userSoundEffectVolume * 0.3f);
                        break;
                    default:
                        SoundEffectSource.PlayOneShot(audioClip, userSoundEffectVolume);
                        break;

                }
            }
        }
    }

    public void SetMusicVolume(float volume)
    {
        userMusicVolume = volume;
        PlayerPrefs.SetFloat("UserMusicVolume", userMusicVolume);

        ApplyMusicVolume();
    }
    public void SetSoundEffectVolume(float volume)
    {
        userSoundEffectVolume = volume;
        PlayerPrefs.SetFloat("UserSoundEffectVolume", userSoundEffectVolume);

        if (flameOngoingSource != null && flameOngoingSource.isPlaying)
            flameOngoingSource.volume = userSoundEffectVolume;

        if (OtherSource != null && OtherSource.isPlaying
            && OtherAudioByName.TryGetValue(OtherSoundType.Rumble, out AudioClip rumbleClip)
            && OtherSource.clip == rumbleClip)
            OtherSource.volume = userSoundEffectVolume * RumbleVolume;
    }

    public void PlayOther(OtherSoundType name)
    {
        if (OtherAudioByName.ContainsKey(name))
        {
            AudioClip audioClip = OtherAudioByName[name];
            if (audioClip != null)
            {
                OtherSource.clip = audioClip;

                if (name == OtherSoundType.Rumble)
                    OtherSource.volume = userSoundEffectVolume * RumbleVolume;

                OtherSource.Play();
            }
        }
    }
    public void StopOther()
    {
        OtherSource.Stop();
    }
    public enum SoundEffectType
    {
        CardDraw,
        Bone,
        Crop,
        Scroll,
        Blood,
        Impact,
        Bump,
        Defeat,
        Whoosh,
    }
    public enum OtherSoundType
    {
        Rumble
    }
}
