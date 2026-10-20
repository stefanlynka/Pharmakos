using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseDebugMenuHandler : MonoBehaviour
{
    RectTransform debugButtonRect;
    RectTransform submenuRoot;
    TextMeshProUGUI enemyHealthLabel;
    Image enemyHealthBackground;
    bool pausePanelVisible;

    void Start()
    {
        BuildUIIfNeeded();
        SetSubmenuOpen(false);
    }

    void Update()
    {
        bool shouldShow = Controller.Instance != null
            && Controller.Instance.GamePaused
            && ScreenHandler.Instance != null
            && ScreenHandler.Instance.CurrentScreen != null
            && ScreenHandler.Instance.CurrentScreen.Name == ScreenName.Pause;

        if (!shouldShow && pausePanelVisible)
            SetSubmenuOpen(false);

        pausePanelVisible = shouldShow;
    }

    void BuildUIIfNeeded()
    {
        if (debugButtonRect != null) return;

        Button debugButton = StyxUI.CreateButton(transform, "DebugMenuButton", "Debug", 22, ToggleSubmenu);
        debugButtonRect = (RectTransform)debugButton.transform;
        StyxUI.SetAnchored(debugButtonRect, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(160f, 52f));

        submenuRoot = StyxUI.CreateRect(transform, "DebugSubmenu");
        StyxUI.SetAnchored(submenuRoot, new Vector2(1f, 1f), new Vector2(-24f, -88f), new Vector2(280f, 188f));

        Image panel = StyxUI.CreatePanel(submenuRoot, "Background", StyxUI.PanelColor);
        StyxUI.Stretch(panel.rectTransform);

        TextMeshProUGUI header = StyxUI.CreateText(submenuRoot, "Header", "Debug", 20, TextAlignmentOptions.Center);
        header.color = StyxUI.DimTextColor;
        StyxUI.SetAnchored(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(250f, 28f));

        Button enemyHealthButton = StyxUI.CreateButton(submenuRoot, "EnemyOneHealth", "Enemy 1 HP: Off", 20, ToggleEnemyHealth);
        StyxUI.SetAnchored((RectTransform)enemyHealthButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(240f, 48f));
        enemyHealthBackground = enemyHealthButton.GetComponent<Image>();
        enemyHealthLabel = enemyHealthButton.GetComponentInChildren<TextMeshProUGUI>();

        Button closeButton = StyxUI.CreateButton(submenuRoot, "Close", "Close", 20, () => SetSubmenuOpen(false));
        StyxUI.SetAnchored((RectTransform)closeButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(240f, 48f));

        RefreshEnemyHealthToggle();
    }

    void ToggleSubmenu()
    {
        SetSubmenuOpen(submenuRoot == null || !submenuRoot.gameObject.activeSelf);
    }

    void SetSubmenuOpen(bool open)
    {
        if (submenuRoot != null)
            submenuRoot.gameObject.SetActive(open);
    }

    void ToggleEnemyHealth()
    {
        ProgressionHandler.ForceCombatEnemyHealthToOne = !ProgressionHandler.ForceCombatEnemyHealthToOne;
        if (ProgressionHandler.ForceCombatEnemyHealthToOne)
            ApplyToCurrentCombat();
        RefreshEnemyHealthToggle();
    }

    void RefreshEnemyHealthToggle()
    {
        if (enemyHealthLabel == null)
            return;

        bool enabled = ProgressionHandler.ForceCombatEnemyHealthToOne;
        enemyHealthLabel.text = enabled ? "Enemy 1 HP: On" : "Enemy 1 HP: Off";
        if (enemyHealthBackground != null)
            enemyHealthBackground.color = enabled ? StyxUI.ButtonSelectedColor : StyxUI.ButtonColor;
    }

    static void ApplyToCurrentCombat()
    {
        Controller controller = Controller.Instance;
        if (controller == null || controller.Player2 == null || controller.Player2.IsHuman)
            return;
        if (controller.GameField == null || !controller.GameField.activeInHierarchy)
            return;

        controller.Player2.Health = 1;
        controller.Player2.StartingHealth = 1;

        if (View.Instance == null)
            return;

        ViewPlayer viewPlayer = View.Instance.GetViewPlayer(controller.Player2);
        if (viewPlayer != null)
            viewPlayer.SetHealth(1);
    }
}
