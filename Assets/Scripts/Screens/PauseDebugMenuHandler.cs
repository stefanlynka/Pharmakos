using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseDebugMenuHandler : MonoBehaviour
{
    static readonly OfferingType[] PreviewOfferings =
    {
        OfferingType.Gold,
        OfferingType.Blood,
        OfferingType.Bone,
        OfferingType.Crop,
        OfferingType.Scroll,
    };

    RectTransform debugButtonRect;
    RectTransform submenuRoot;
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
        StyxUI.SetAnchored(submenuRoot, new Vector2(1f, 1f), new Vector2(-24f, -88f), new Vector2(280f, 460f));

        Image panel = StyxUI.CreatePanel(submenuRoot, "Background", StyxUI.PanelColor);
        StyxUI.Stretch(panel.rectTransform);

        TextMeshProUGUI header = StyxUI.CreateText(submenuRoot, "Header", "Offering Collect", 20, TextAlignmentOptions.Center);
        header.color = StyxUI.DimTextColor;
        StyxUI.SetAnchored(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(250f, 28f));

        float y = -56f;
        const float rowHeight = 48f;
        const float rowSpacing = 8f;

        for (int i = 0; i < PreviewOfferings.Length; i++)
        {
            OfferingType type = PreviewOfferings[i];
            Button button = StyxUI.CreateButton(submenuRoot, type + "Preview", type + " 1-10", 20, () => Preview(type));
            StyxUI.SetAnchored((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(240f, rowHeight));
            y -= rowHeight + rowSpacing;
        }

        Button closeButton = StyxUI.CreateButton(submenuRoot, "Close", "Close", 20, () => SetSubmenuOpen(false));
        StyxUI.SetAnchored((RectTransform)closeButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, y - 8f), new Vector2(240f, rowHeight));
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

    void Preview(OfferingType type)
    {
        if (View.Instance == null || View.Instance.AudioHandler == null)
            return;

        View.Instance.AudioHandler.PreviewOfferingCollect(type, 1, 10);
    }
}
