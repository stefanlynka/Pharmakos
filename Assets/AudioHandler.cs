using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ProgressionHandler;


public class AudioHandler : MonoBehaviour
{
    public AudioSource AudioSource;
    public AudioSource SoundEffectSource;

    public Dictionary<DeckName, AudioClip> MusicByName = new Dictionary<DeckName, AudioClip>();
    public Dictionary<SoundEffectType, AudioClip> SoundEffectsByName = new Dictionary<SoundEffectType, AudioClip>();

    private float baseVolume = 0.1f;

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
    }

    public void PlayMusic(DeckName name)
    {
        if (MusicByName.ContainsKey(name))
        {
            AudioClip audioClip = MusicByName[name];
            if (audioClip != null)
            {
                AudioSource.clip = audioClip;
                switch (name)
                {
                    case DeckName.Labyrinth:
                        AudioSource.volume = baseVolume * 0.3f;
                        break;
                    case DeckName.Bacchanalia:
                        AudioSource.volume = baseVolume * 0.3f;
                        break;
                    case DeckName.Cyclops:
                        AudioSource.volume = baseVolume * 0.3f;
                        break;
                    case DeckName.SeasideCliffs:
                        AudioSource.volume = baseVolume * 0.3f;
                        break;
                    case DeckName.Troy:
                        AudioSource.volume = baseVolume * 0.25f;
                        break;
                    case DeckName.Hunt:
                        AudioSource.volume = baseVolume * 0.3f;
                        break;
                    case DeckName.Trials:
                        AudioSource.volume = baseVolume * 0.2f;
                        break;
                    case DeckName.Caves:
                        AudioSource.volume = baseVolume * 0.2f;
                        break;
                    case DeckName.Delphi:
                        AudioSource.volume = baseVolume * 0.4f;
                        break;
                    case DeckName.Underworld:
                        AudioSource.volume = baseVolume * 0.25f;
                        break;
                    default:
                        AudioSource.volume = baseVolume;
                        break;
                }

                AudioSource.Play();
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
                        SoundEffectSource.PlayOneShot(audioClip, 0.1f);
                        break;
                    case SoundEffectType.Impact:
                        SoundEffectSource.PlayOneShot(audioClip, 0.5f);
                        break;
                    case SoundEffectType.Blood:
                        SoundEffectSource.PlayOneShot(audioClip, 0.25f);
                        break;
                    case SoundEffectType.Scroll:
                        SoundEffectSource.PlayOneShot(audioClip, 0.6f);
                        break;
                    default:
                        SoundEffectSource.PlayOneShot(audioClip);
                        break;

                }
            }
        }
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
