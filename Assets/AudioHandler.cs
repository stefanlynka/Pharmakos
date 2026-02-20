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

    public Dictionary<DeckName, AudioClip> MusicByName = new Dictionary<DeckName, AudioClip>();
    public Dictionary<SoundEffectType, AudioClip> SoundEffectsByName = new Dictionary<SoundEffectType, AudioClip>();

    public Dictionary<DeckName, float> MusicMultipliers = new Dictionary<DeckName, float>();

    private float baseMusicVolume = 0.1f;

    private float userMusicVolume = 0.5f;
    private float userSoundEffectVolume = 0.5f;

    private float currentMusicMultiplier = 1.0f;

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
        MusicMultipliers[DeckName.Underworld] = 0.25f;

        userMusicVolume = PlayerPrefs.GetFloat("UserMusicVolume", 0.5f);
        MusicSlider.value = userMusicVolume;
        userSoundEffectVolume = PlayerPrefs.GetFloat("UserSoundEffectVolume", 0.5f);
        SoundEffectSlider.value = userSoundEffectVolume;
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

                MusicSource.volume = baseMusicVolume * userMusicVolume * currentMusicMultiplier;

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

        MusicSource.volume = baseMusicVolume * userMusicVolume * currentMusicMultiplier;
    }
    public void SetSoundEffectVolume(float volume)
    {
        userSoundEffectVolume = volume;
        PlayerPrefs.SetFloat("UserSoundEffectVolume", userSoundEffectVolume);
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
}
