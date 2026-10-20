using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shown the first time the player defeats the Fates or Gate boss. The unlocked
/// trinket is the shears or hammer in StyxTrinketUnlockArea; hovering it shows the effect.
/// </summary>
public class TrinketUnlockHandler : MonoBehaviour
{
    [Header("Scene References")]
    public Button ContinueButton;
    public StyxTrinketUnlockArea Area;

    Action onContinue;

    void Awake()
    {
        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(Continue);
    }

    public void Show(Trinket trinket, Action onContinueCallback)
    {
        onContinue = onContinueCallback;

        if (Area == null)
            Area = FindObjectOfType<StyxTrinketUnlockArea>(true);

        if (Area == null)
        {
            Debug.LogError("TrinketUnlockHandler: No StyxTrinketUnlockArea found in the scene.");
            return;
        }

        Area.Show(trinket);

        Camera selectionCamera = Area.AreaCamera;
        if (View.Instance != null && View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Activate(selectionCamera, () => false);

        Screen screen = GetComponent<Screen>();
        if (screen != null)
        {
            if (screen.Camera == null && selectionCamera != null)
                screen.Camera = selectionCamera;
            screen.OnExit = HideUnlock;
        }
    }

    public void Continue()
    {
        Action callback = onContinue;
        onContinue = null;
        if (callback == null) return;

        if (Controller.Instance != null)
            Controller.Instance.ContinueFromTrinketUnlockScreen(callback);
        else
            callback.Invoke();
    }

    void HideUnlock()
    {
        if (Area != null)
            Area.Hide();

        if (View.Instance != null && View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Deactivate();
    }
}
