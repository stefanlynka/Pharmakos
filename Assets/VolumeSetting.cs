using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    //[SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider volumeSlider;

    [SerializeField] public AudioType Type;

    public void SetVolume()
    {
        float volume = volumeSlider.value;

        if (Type == AudioType.Music)
        {
            View.Instance.AudioHandler.SetMusicVolume(volume);
        }
        else
        {
            View.Instance.AudioHandler.SetSoundEffectVolume(volume);
        }
        // Convert the linear 0.0001-1 value to a logarithmic decibel scale
        //myMixer.SetFloat("MasterVol", Mathf.Log10(volume) * 20);
    }

    public enum AudioType
    {
        Music,
        SoundEffects
    }
}