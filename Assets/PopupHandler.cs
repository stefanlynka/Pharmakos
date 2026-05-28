using UnityEngine;

public class PopupHandler : MonoBehaviour
{
    public static PopupHandler Instance;

    public PopupCapture Popup1;
    public PopupCapture Popup2;
    [Header("Popup UI Anchors")]
    [Tooltip("UI container for slot one popup (optional, auto-resolved from EventScreenHandler if unset).")]
    public RectTransform Popup1View;
    [Tooltip("UI container for slot two popup (optional, auto-resolved from EventScreenHandler if unset).")]
    public RectTransform Popup2View;
    [Tooltip("Screen margin used when clamping popup positions.")]
    public float ScreenMargin = 24f;
    [Tooltip("Gap between hovered target and popup.")]
    public float TargetGap = 24f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoResolvePopupViews();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void ShowCard(PopupSlot slot, Card card) => Instance?.ShowCardInternal(slot, card);
    public static void ShowCardAtAnchor(
        PopupSlot slot,
        Card card,
        Transform anchor,
        PopupPosition preferredPosition,
        Camera worldCamera = null) =>
        Instance?.ShowCardAtAnchorInternal(slot, card, anchor, preferredPosition, worldCamera);
    public static void ShowRitual(PopupSlot slot, Ritual ritual) => Instance?.ShowRitualInternal(slot, ritual);
    public static void ShowTrinket(PopupSlot slot, Trinket trinket) => Instance?.ShowTrinketInternal(slot, trinket);
    public static void ShowBuff(PopupSlot slot, StaticPlayerEffect buff, int amount = 1) =>
        Instance?.ShowBuffInternal(slot, buff, null, amount);
    public static void ShowBuff(PopupSlot slot, StaticPlayerEffect buff, PlayerEffectDescriptionData descriptionData, int amount = 1) =>
        Instance?.ShowBuffInternal(slot, buff, descriptionData, amount);
    public static void ShowHeartstring(PopupSlot slot) => Instance?.ShowHeartstringInternal(slot);
    public static void Clear(PopupSlot slot) => Instance?.ClearInternal(slot);
    public static void ClearAll()
    {
        Clear(PopupSlot.One);
        Clear(PopupSlot.Two);
    }

    void ShowCardInternal(PopupSlot slot, Card card) => GetCapture(slot)?.LoadCard(card);
    void ShowCardAtAnchorInternal(PopupSlot slot, Card card, Transform anchor, PopupPosition preferredPosition, Camera worldCamera)
    {
        PopupCapture capture = GetCapture(slot);
        if (capture == null)
            return;

        capture.LoadCard(card);

        RectTransform popupView = GetPopupView(slot);
        if (popupView == null)
            return;

        popupView.gameObject.SetActive(card != null);
        if (card == null || anchor == null)
            return;

        PositionPopupNearTarget(popupView, anchor, preferredPosition, worldCamera);
    }
    void ShowRitualInternal(PopupSlot slot, Ritual ritual) => GetCapture(slot)?.LoadRitual(ritual);
    void ShowTrinketInternal(PopupSlot slot, Trinket trinket) => GetCapture(slot)?.LoadTrinket(trinket);
    void ShowBuffInternal(PopupSlot slot, StaticPlayerEffect buff, PlayerEffectDescriptionData descriptionData, int amount) =>
        GetCapture(slot)?.LoadBuff(buff, descriptionData, amount);
    void ShowHeartstringInternal(PopupSlot slot) => GetCapture(slot)?.LoadHeartstring();
    void ClearInternal(PopupSlot slot)
    {
        GetCapture(slot)?.Clear();

        RectTransform popupView = GetPopupView(slot);
        if (popupView != null)
            popupView.gameObject.SetActive(false);
    }

    PopupCapture GetCapture(PopupSlot slot)
    {
        if (slot == PopupSlot.One)
            return Popup1;
        if (slot == PopupSlot.Two)
            return Popup2;

        Debug.LogWarning($"PopupHandler: unknown slot {slot}");
        return null;
    }

    RectTransform GetPopupView(PopupSlot slot)
    {
        if (slot == PopupSlot.One)
            return Popup1View;
        if (slot == PopupSlot.Two)
            return Popup2View;

        return null;
    }

    void AutoResolvePopupViews()
    {
        if (Popup1View != null && Popup2View != null)
            return;

        EventScreenHandler eventScreenHandler = FindFirstObjectByType<EventScreenHandler>();
        if (eventScreenHandler == null)
            return;

        if (Popup1View == null && eventScreenHandler.Popup1 != null)
            Popup1View = eventScreenHandler.Popup1.GetComponent<RectTransform>();

        if (Popup2View == null && eventScreenHandler.Popup2 != null)
            Popup2View = eventScreenHandler.Popup2.GetComponent<RectTransform>();
    }

    void PositionPopupNearTarget(RectTransform popupRect, Transform anchor, PopupPosition side, Camera worldCamera)
    {
        if (popupRect == null || anchor == null)
            return;

        RectTransform parentRect = popupRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Camera camera = ResolveWorldCamera(worldCamera);
        if (camera == null)
            return;

        Rect targetRect = GetScreenRect(anchor, camera);
        Vector2 popupSize = GetPopupSize(popupRect);
        Rect screenBounds = new Rect(
            ScreenMargin,
            ScreenMargin,
            UnityEngine.Screen.width - ScreenMargin * 2f,
            UnityEngine.Screen.height - ScreenMargin * 2f);

        Vector2 desired = GetScreenPositionForSide(side, targetRect, popupSize, TargetGap);
        Rect popupScreenRect = CenteredRect(desired, popupSize);
        Rect clampedRect = ClampRect(popupScreenRect, screenBounds);

        Canvas canvas = popupRect.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, clampedRect.center, eventCamera, out Vector2 localPoint))
            popupRect.anchoredPosition = localPoint;
    }

    static Camera ResolveWorldCamera(Camera explicitCamera)
    {
        if (ScreenHandler.Instance != null)
            return ScreenHandler.Instance.ResolveWorldCamera(explicitCamera);

        if (explicitCamera != null && explicitCamera.isActiveAndEnabled)
            return explicitCamera;

        return Camera.main;
    }

    static Vector2 GetPopupSize(RectTransform popupRect)
    {
        Vector2 size = popupRect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            size = popupRect.sizeDelta;
        return size;
    }

    static Vector2 GetScreenPositionForSide(PopupPosition side, Rect target, Vector2 popupSize, float gap)
    {
        Vector2 center = target.center;
        float halfW = popupSize.x * 0.5f;
        float halfH = popupSize.y * 0.5f;

        switch (side)
        {
            case PopupPosition.Left:
                return new Vector2(target.xMin - gap - halfW, center.y);
            case PopupPosition.Above:
                return new Vector2(center.x, target.yMax + gap + halfH);
            case PopupPosition.Below:
                return new Vector2(center.x, target.yMin - gap - halfH);
            case PopupPosition.Right:
            default:
                return new Vector2(target.xMax + gap + halfW, center.y);
        }
    }

    static Rect CenteredRect(Vector2 center, Vector2 size)
    {
        return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
    }

    static Rect ClampRect(Rect rect, Rect bounds)
    {
        float xMin = Mathf.Clamp(rect.xMin, bounds.xMin, bounds.xMax - rect.width);
        float yMin = Mathf.Clamp(rect.yMin, bounds.yMin, bounds.yMax - rect.height);
        return new Rect(xMin, yMin, rect.width, rect.height);
    }

    static Rect GetScreenRect(Transform target, Camera camera)
    {
        if (target == null || camera == null)
            return Rect.zero;

        Bounds bounds = default;
        bool hasBounds = false;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
                bounds.Encapsulate(renderer.bounds);
        }

        if (!hasBounds)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(collider.bounds);
            }
        }

        if (!hasBounds)
        {
            Vector3 screen = camera.WorldToScreenPoint(target.position);
            const float fallbackSize = 48f;
            return new Rect(
                screen.x - fallbackSize * 0.5f,
                screen.y - fallbackSize * 0.5f,
                fallbackSize,
                fallbackSize);
        }

        Vector3[] corners =
        {
            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
        };

        Vector3 first = camera.WorldToScreenPoint(corners[0]);
        float xMin = first.x;
        float xMax = first.x;
        float yMin = first.y;
        float yMax = first.y;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 screen = camera.WorldToScreenPoint(corners[i]);
            xMin = Mathf.Min(xMin, screen.x);
            xMax = Mathf.Max(xMax, screen.x);
            yMin = Mathf.Min(yMin, screen.y);
            yMax = Mathf.Max(yMax, screen.y);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
}

public enum PopupSlot
{
    One,
    Two
}

public enum PopupType
{
    None,
    Follower,
    Spell,
    Ritual,
    Trinket,
    Heartstring
}
