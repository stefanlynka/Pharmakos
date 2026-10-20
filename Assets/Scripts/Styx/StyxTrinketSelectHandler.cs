using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shown at the start of a run (after the starting devotion is chosen) when the player
/// has unlocked at least one Styx trinket. Shears and the hammer in StyxTrinketArea are
/// the choices: click to take or leave each unlocked trinket, then continue.
/// </summary>
public class StyxTrinketSelectHandler : MonoBehaviour
{
    [Header("Scene References")]
    public Button ContinueButton;
    public StyxTrinketArea Area;

    void Awake()
    {
        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(Continue);
    }

    public void Show()
    {
        if (Area == null)
            Area = FindObjectOfType<StyxTrinketArea>(true);

        if (Area == null)
        {
            Debug.LogError("StyxTrinketSelectHandler: No StyxTrinketArea found in the scene.");
            return;
        }

        Area.ShowUnlocked();

        Camera selectionCamera = Area.AreaCamera;
        if (View.Instance != null && View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Activate(selectionCamera, () => false);

        Screen screen = GetComponent<Screen>();
        if (screen != null)
        {
            if (screen.Camera == null && selectionCamera != null)
                screen.Camera = selectionCamera;
            screen.OnExit = HideSelection;
        }
    }

    public void Continue()
    {
        if (Area != null)
        {
            List<Trinket> chosen = new List<Trinket>();
            Area.CollectSelected(chosen);
            for (int i = 0; i < chosen.Count; i++)
                Controller.Instance.AddTrinket(chosen[i]);
        }

        Controller.Instance.StartGame();
    }

    void HideSelection()
    {
        if (Area != null)
            Area.Hide();

        if (View.Instance != null && View.Instance.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Deactivate();
    }
}
