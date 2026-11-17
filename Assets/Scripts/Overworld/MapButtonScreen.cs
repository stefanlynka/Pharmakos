using TMPro;
using UnityEngine;

/// <summary>
/// The Map button under the Status button. Shows itself wherever <see cref="Controller.ShouldShowMapButton"/>
/// allows (combat, events, temple, shop, rewards) and flips its label while the map view is open.
/// The button's onClick calls <see cref="Controller.ToggleMapView"/>.
/// </summary>
[RequireComponent(typeof(Screen))]
public class MapButtonScreen : MonoBehaviour
{
    public TMP_Text Label;
    public string OpenLabel = "Map";
    public string CloseLabel = "Return";

    Screen _screen;
    bool _mapOpenShown;

    void Awake()
    {
        _screen = GetComponent<Screen>();
    }

    void Update()
    {
        Controller controller = Controller.Instance;
        if (_screen == null || controller == null)
            return;

        // Entered/exited directly rather than through ScreenHandler.ShowScreen, which would
        // make this overlay the handler's CurrentScreen.
        bool shouldShow = controller.ShouldShowMapButton();
        if (shouldShow && !_screen.IsActive)
            _screen.Enter(false);
        else if (!shouldShow && _screen.IsActive)
            _screen.Exit(false);

        bool mapOpen = OverworldMapView.IsOpen;
        if (Label != null && (mapOpen != _mapOpenShown || Label.text.Length == 0))
        {
            _mapOpenShown = mapOpen;
            Label.text = mapOpen ? CloseLabel : OpenLabel;
        }
    }
}
