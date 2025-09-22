using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenHandler : MonoBehaviour
{
    public static ScreenHandler Instance;

    public List<Screen> Screens = new List<Screen>();

    public Screen CurrentScreen = null;

    void Awake()
    {
        AwakeSetup();
    }

    protected virtual void AwakeSetup()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        SetupAll();
        HideAll(true);

        //ShowScreen(ScreenName.Blank, true);

        ShowScreen(ScreenName.Game, true, false);
        //ShowScreen(ScreenName.Start, false, false);
    }

    protected virtual void SetupAll()
    {
        foreach (Screen screen in Screens)
        {
            screen.Init();
        }
    }

    protected virtual void HideAll(bool instant = false)
    {
        foreach (Screen screen in Screens)
        {
            if (!screen.ManualHide) screen.Exit(instant);
        }
    }

    public virtual void ShowScreen(ScreenName name, bool instant = false, bool hideOthers = true)
    {
        if (TryGetScreen(name, out Screen screen))
        {
            if (hideOthers) HideAll();
            screen.Enter(instant);
            CurrentScreen = screen;
        }
        else
        {
            Debug.LogError("Can't find " + name + " screen");
        }
    }

    public virtual void HideScreen(ScreenName name)
    {
        if (TryGetScreen(name, out Screen screen))
        {
            screen.Exit();
        }
        else
        {
            Debug.LogError("Can't find " + name + " screen");
        }
    }

    public virtual bool TryGetScreen(ScreenName name, out Screen screen)
    {
        foreach (Screen availableScreen in Screens)
        {
            if (availableScreen.Name == name)
            {
                screen = availableScreen;
                return true;
            }
        }

        screen = null;
        return false;
    }

    public bool TryGetCurrentScreen(out Screen screen)
    {
        screen = CurrentScreen;

        return screen != null;
    }
}


public enum ScreenName
{
    Blank,
    Start,
    Game,
    GameOver,
    Success,
    RitualRewards,
    CardRemovalRewards,
    CardGainRewards,
    Pause,
    StarterBundle,
    Tutorial,
}

