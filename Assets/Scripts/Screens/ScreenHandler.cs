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

        RegisterUnlistedSceneScreens();
        EnsurePauseStyxUnlocksHandler();
        SetupAll();
        HideAll(true);
        EnablePersistentScreens(true);

        //ShowScreen(ScreenName.Blank, true);

        ShowScreen(ScreenName.Game, true, false);
        //ShowScreen(ScreenName.Start, false, false);
    }

    /// <summary>
    /// Picks up Screen components in the scene that aren't in the serialized list,
    /// so new screens can be added without editing this handler's inspector list.
    /// </summary>
    protected virtual void RegisterUnlistedSceneScreens()
    {
        foreach (Screen screen in FindObjectsOfType<Screen>(true))
        {
            if (!Screens.Contains(screen))
                Screens.Add(screen);
        }
    }

    protected virtual void EnsurePauseStyxUnlocksHandler()
    {
        if (!TryGetScreen(ScreenName.Pause, out Screen pauseScreen))
            return;

        if (pauseScreen.GetComponent<PauseStyxUnlocksHandler>() == null)
            pauseScreen.gameObject.AddComponent<PauseStyxUnlocksHandler>();
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

    /// <summary>Enters overlay screens that stay visible across screen changes.</summary>
    protected virtual void EnablePersistentScreens(bool instant = false)
    {
        if (TryGetScreen(ScreenName.Popup, out Screen popup))
            popup.Enter(instant);
    }

    public virtual void ShowScreen(ScreenName name, bool instant = false, bool hideOthers = true, bool hideOthersInstant = false)
    {
        if (TryGetScreen(name, out Screen screen))
        {
            if (hideOthers) HideAll(hideOthersInstant);
            screen.Enter(instant);
            CurrentScreen = screen;
        }
        else
        {
            Debug.LogError("Can't find " + name + " screen");
        }
    }

    public virtual void HideScreen(ScreenName name, bool instant = false)
    {
        if (TryGetScreen(name, out Screen screen))
        {
            screen.Exit(instant);
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

    /// <summary>
    /// Resolves the world camera for projecting 3D anchors into screen space.
    /// Prefers <paramref name="explicitCamera"/>, then the current screen's camera,
    /// then <see cref="MenuSelectionHandler"/> when active, then <see cref="Camera.main"/>.
    /// </summary>
    public Camera ResolveWorldCamera(Camera explicitCamera = null)
    {
        if (explicitCamera != null && explicitCamera.isActiveAndEnabled)
            return explicitCamera;

        if (TryGetCurrentScreen(out Screen screen)
            && screen != null
            && screen.Name != ScreenName.Popup
            && screen.Camera != null
            && screen.Camera.isActiveAndEnabled)
            return screen.Camera;

        if (MenuSelectionHandler.Instance != null && MenuSelectionHandler.Instance.IsActive)
        {
            Camera menuCamera = MenuSelectionHandler.Instance.GetSelectionCamera();
            if (menuCamera != null && menuCamera.isActiveAndEnabled)
                return menuCamera;
        }

        return Camera.main;
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
    DeckScreenButton,
    PlayHistoryButton,
    Options,
    TrinketRewardScreen,
    Overworld,
    Event,
    Temple,
    Shop,
    Popup,
    Styx,
    TrinketUnlock,
    StyxTrinketSelect,
    Status,
    StatusButton,
}

