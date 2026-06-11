using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay screen holding the "Status" button shown on the overworld and non-combat menus.
/// The button is built at runtime; clicking it opens the status screen.
/// </summary>
public class StatusButtonScreenHandler : MonoBehaviour
{
    Button button;

    void Awake()
    {
        Screen screen = GetComponent<Screen>();
        if (screen != null)
            screen.InstantExit = true;
    }

    void Start()
    {
        if (button != null) return;

        button = StyxUI.CreateButton(transform, "StatusButton", "Status", 26,
            () => Controller.Instance.ToggleStatusScreen());
        StyxUI.SetAnchored((RectTransform)button.transform, new Vector2(1f, 1f), new Vector2(-25, -25), new Vector2(180, 56));
    }
}
