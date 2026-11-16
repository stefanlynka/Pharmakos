using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hover/press feedback for the terracotta buttons (see Docs/StyleGuide.md):
/// the clay brightens (Button colour tint), the label turns warm white and the button lifts slightly on hover,
/// and it dips on press.
/// Works alongside the Button's own colour tint. Uses unscaled time so it works while paused.
/// Also guards against double-clicks via <see cref="InputGuard"/>.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Tooltip("After a click, block all clicks briefly so a double-click can't fire the action twice. See InputGuard.")]
    public bool BlockDoubleClicks = true;

    public Color LabelHoverColor = new Color(1f, 0.97f, 0.92f, 1f); // warm white (#FFF7EB) on terracotta
    public float HoverScale = 1.04f;
    public float PressedScale = 0.97f;
    public float Speed = 18f;

    Selectable selectable;
    TMP_Text[] labels;
    Color[] labelBaseColors;
    Vector3 baseScale;
    bool hovered;
    bool pressed;
    float hoverAmount;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
        labels = GetComponentsInChildren<TMP_Text>(true);
        CaptureLabelColors();
    }

    void OnEnable()
    {
        hovered = false;
        pressed = false;
        holdingHoverThroughLock = false;
        hoverAmount = 0f;
        Apply();
    }

    void OnDisable()
    {
        holdingHoverThroughLock = false;
        hovered = false;
        pressed = false;
        hoverAmount = 0f;
        Apply();
    }

    /// <summary>Call if code changes a label's colour at runtime, so the new colour becomes the resting colour.</summary>
    public void CaptureLabelColors()
    {
        if (labels == null) return;
        labelBaseColors = new Color[labels.Length];
        for (int i = 0; i < labels.Length; i++)
            labelBaseColors[i] = labels[i] != null ? labels[i].color : Color.white;
    }

    bool Interactable => selectable == null || selectable.IsInteractable();

    // True while InputGuard's click-catcher overlay has stolen the pointer from us but the mouse is still
    // physically over the button. We keep the hover look (label, lift and clay tint) until the lock ends,
    // instead of dropping to rest and popping back once the overlay disappears.
    bool holdingHoverThroughLock;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        holdingHoverThroughLock = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pressed = false;
        if (InputGuard.IsBlocked && PointerIsOverButton())
        {
            holdingHoverThroughLock = true;
            return;
        }

        hovered = false;
        holdingHoverThroughLock = false;
    }
    public void OnPointerDown(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BlockDoubleClicks && eventData.button == PointerEventData.InputButton.Left && Interactable)
            InputGuard.BlockFor(InputGuard.ClickBlockDuration);
    }

    bool lastAppliedPressed;

    void Update()
    {
        if (holdingHoverThroughLock)
            UpdateHeldHover();

        float target = hovered && Interactable ? 1f : 0f;
        bool showPressed = pressed && Interactable;
        if (Mathf.Approximately(hoverAmount, target) && showPressed == lastAppliedPressed) return;

        hoverAmount = Mathf.MoveTowards(hoverAmount, target, Speed * Time.unscaledDeltaTime);
        Apply();
    }

    void LateUpdate()
    {
        // The Button's own tint dropped to normal when the overlay stole the pointer; hold it at highlighted.
        if (holdingHoverThroughLock && selectable != null && selectable.targetGraphic != null && Interactable)
        {
            ColorBlock c = selectable.colors;
            selectable.targetGraphic.CrossFadeColor(c.highlightedColor * c.colorMultiplier, 0f, true, true);
        }
    }

    void UpdateHeldHover()
    {
        bool lockEnded = !InputGuard.IsBlocked;
        bool mouseLeft = !PointerIsOverButton();
        if (!lockEnded && !mouseLeft) return;

        holdingHoverThroughLock = false;
        if (mouseLeft)
        {
            // Mouse moved off while locked: settle to rest. (If the lock ended with the mouse still over us,
            // the EventSystem re-sends OnPointerEnter, which keeps us hovered with no visible change.)
            hovered = false;
            if (selectable != null && selectable.targetGraphic != null)
            {
                ColorBlock c = selectable.colors;
                selectable.targetGraphic.CrossFadeColor(c.normalColor * c.colorMultiplier, c.fadeDuration, true, true);
            }
        }
    }

    bool PointerIsOverButton()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null)
        {
            Canvas root = canvas.rootCanvas;
            if (root.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = root.worldCamera != null ? root.worldCamera : Camera.main;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, cam);
    }

    void Apply()
    {
        if (labels != null && labelBaseColors != null)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null) continue;
                labels[i].color = Color.Lerp(labelBaseColors[i], LabelHoverColor, hoverAmount);
            }
        }

        lastAppliedPressed = pressed && Interactable;
        float scale = lastAppliedPressed ? PressedScale : Mathf.Lerp(1f, HoverScale, hoverAmount);
        transform.localScale = baseScale * scale;
    }
}
