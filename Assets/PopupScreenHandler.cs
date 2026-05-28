using UnityEngine;
using UnityEngine.UI;

public enum PopupPosition
{
    Above,
    Below,
    Left,
    Right,
}

public class PopupScreenHandler : MonoBehaviour
{
    public static PopupScreenHandler Instance { get; private set; }

    const string TextPopupResourcePath = "Prefabs/Popups/TextPopup";
    const string ImagePopupResourcePath = "Prefabs/Popups/PopupImage";
    const string PopupRenderTextureOnePath = "RenderTextures/Popup1";
    const string PopupRenderTextureTwoPath = "RenderTextures/Popup2";

    [Tooltip("Parent for instantiated text popups. Defaults to this object's RectTransform.")]
    public RectTransform PopupContainer;

    [Tooltip("Optional template in the scene. If unset, loads from Resources or finds a child TextPopup.")]
    public TextPopup TextPopupTemplate;
    [Tooltip("Optional template in the scene for image popups. If unset, loads from Resources.")]
    public RectTransform PopupImageTemplate;

    public float ScreenMargin = 24f;
    public float TargetGap = 16f;

    [Tooltip("Used when ShowTextPopup is called without an explicit position.")]
    public PopupPosition DefaultPosition = PopupPosition.Above;

    TextPopup _activePopup;
    Transform _hoverAnchor;
    RectTransform _imagePopupOne;
    RectTransform _imagePopupTwo;
    RawImage _imageRawOne;
    RawImage _imageRawTwo;
    Transform _imageAnchorOne;
    Transform _imageAnchorTwo;
    RenderTexture _popupTextureOne;
    RenderTexture _popupTextureTwo;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (PopupContainer == null)
            PopupContainer = transform as RectTransform;

        if (TextPopupTemplate == null)
            TextPopupTemplate = GetComponentInChildren<TextPopup>(true);

        if (TextPopupTemplate != null)
            TextPopupTemplate.gameObject.SetActive(false);

        if (PopupImageTemplate != null)
            PopupImageTemplate.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Shows a text popup near <paramref name="anchor"/>, kept on-screen and offset so it does not cover the anchor.</summary>
    public void ShowTextPopup(string text, Transform anchor, Camera worldCamera = null)
    {
        ShowTextPopup(text, anchor, DefaultPosition, worldCamera);
    }

    /// <summary>Shows a text popup on the given side of <paramref name="anchor"/>, with fallbacks if it does not fit on screen.</summary>
    public void ShowTextPopup(string text, Transform anchor, PopupPosition position, Camera worldCamera = null)
    {
        if (string.IsNullOrEmpty(text) || anchor == null)
            return;

        _hoverAnchor = anchor;

        TextPopup popup = GetOrCreatePopup();
        if (popup == null)
            return;

        popup.SetText(text);
        popup.gameObject.SetActive(true);
        popup.SetRaycastTargets(false);

        PositionNearTarget(popup.RectTransform, anchor, position, worldCamera);
    }

    /// <summary>Hides the hover popup only if it was shown for <paramref name="anchor"/>.</summary>
    public void HideTextPopup(Transform anchor)
    {
        if (_activePopup == null || _hoverAnchor != anchor)
            return;

        HideTextPopup();
    }

    public void HideTextPopup()
    {
        _hoverAnchor = null;

        if (_activePopup != null)
            _activePopup.gameObject.SetActive(false);
    }

    public void ShowCardPopup(Card card, Transform anchor, PopupPosition position, PopupSlot slot = PopupSlot.One, Camera worldCamera = null)
    {
        if (card == null || anchor == null || PopupHandler.Instance == null)
            return;

        PopupHandler.ShowCard(slot, card);
        ShowImagePopup(slot, anchor, position, worldCamera);
    }

    public void ShowRitualPopup(Ritual ritual, Transform anchor, PopupPosition position, PopupSlot slot = PopupSlot.One, Camera worldCamera = null)
    {
        if (ritual == null || anchor == null || PopupHandler.Instance == null)
            return;

        PopupHandler.ShowRitual(slot, ritual);
        ShowImagePopup(slot, anchor, position, worldCamera);
    }

    public void ShowTrinketPopup(Trinket trinket, Transform anchor, PopupPosition position, PopupSlot slot = PopupSlot.One, Camera worldCamera = null)
    {
        if (trinket == null || anchor == null || PopupHandler.Instance == null)
            return;

        PopupHandler.ShowTrinket(slot, trinket);
        ShowImagePopup(slot, anchor, position, worldCamera);
    }

    public void ShowBuffPopup(StaticPlayerEffect buff, Transform anchor, PopupPosition position, PopupSlot slot = PopupSlot.One, int amount = 1, Camera worldCamera = null)
    {
        if (buff == null || anchor == null || PopupHandler.Instance == null)
            return;

        PopupHandler.ShowBuff(slot, buff, amount);
        ShowImagePopup(slot, anchor, position, worldCamera);
    }

    public void ShowHeartstringPopup(Transform anchor, PopupPosition position, PopupSlot slot = PopupSlot.One, Camera worldCamera = null)
    {
        if (anchor == null || PopupHandler.Instance == null)
            return;

        PopupHandler.ShowHeartstring(slot);
        ShowImagePopup(slot, anchor, position, worldCamera);
    }

    public void HideImagePopup(PopupSlot slot)
    {
        SetImageAnchor(slot, null);
        RectTransform popup = GetImagePopup(slot);
        if (popup != null)
            popup.gameObject.SetActive(false);

        PopupHandler.Clear(slot);
    }

    public void HideImagePopup(Transform anchor)
    {
        if (_imageAnchorOne == anchor)
            HideImagePopup(PopupSlot.One);
        if (_imageAnchorTwo == anchor)
            HideImagePopup(PopupSlot.Two);
    }

    TextPopup GetOrCreatePopup()
    {
        if (_activePopup != null)
            return _activePopup;

        if (TextPopupTemplate != null)
            _activePopup = Instantiate(TextPopupTemplate, PopupContainer);
        else
        {
            TextPopup prefab = Resources.Load<TextPopup>(TextPopupResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"PopupScreenHandler: could not load TextPopup at Resources/{TextPopupResourcePath}");
                return null;
            }

            _activePopup = Instantiate(prefab, PopupContainer);
        }

        return _activePopup;
    }

    void ShowImagePopup(PopupSlot slot, Transform anchor, PopupPosition position, Camera worldCamera)
    {
        RectTransform popup = GetOrCreateImagePopup(slot);
        if (popup == null)
            return;

        popup.gameObject.SetActive(true);
        SetImageAnchor(slot, anchor);
        PositionNearTarget(popup, anchor, position, worldCamera);
    }

    RectTransform GetOrCreateImagePopup(PopupSlot slot)
    {
        RectTransform current = GetImagePopup(slot);
        if (current != null)
            return current;

        RectTransform template = PopupImageTemplate;
        if (template == null)
        {
            GameObject prefab = Resources.Load<GameObject>(ImagePopupResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"PopupScreenHandler: could not load PopupImage at Resources/{ImagePopupResourcePath}");
                return null;
            }

            GameObject instanceFromPrefab = Instantiate(prefab, PopupContainer);
            current = instanceFromPrefab.GetComponent<RectTransform>();
        }
        else
            current = Instantiate(template, PopupContainer);

        if (current == null)
            return null;

        RawImage rawImage = current.GetComponentInChildren<RawImage>(true);
        if (rawImage == null)
        {
            Debug.LogError("PopupScreenHandler: PopupImage prefab is missing a RawImage.");
            Destroy(current.gameObject);
            return null;
        }

        rawImage.texture = GetPopupTexture(slot);
        rawImage.raycastTarget = false;
        current.gameObject.SetActive(false);

        if (slot == PopupSlot.One)
        {
            _imagePopupOne = current;
            _imageRawOne = rawImage;
        }
        else
        {
            _imagePopupTwo = current;
            _imageRawTwo = rawImage;
        }

        return current;
    }

    RectTransform GetImagePopup(PopupSlot slot)
    {
        return slot == PopupSlot.One ? _imagePopupOne : _imagePopupTwo;
    }

    void SetImageAnchor(PopupSlot slot, Transform anchor)
    {
        if (slot == PopupSlot.One)
            _imageAnchorOne = anchor;
        else
            _imageAnchorTwo = anchor;
    }

    Texture GetPopupTexture(PopupSlot slot)
    {
        if (slot == PopupSlot.One)
        {
            if (_popupTextureOne == null)
                _popupTextureOne = Resources.Load<RenderTexture>(PopupRenderTextureOnePath);
            return _popupTextureOne;
        }

        if (_popupTextureTwo == null)
            _popupTextureTwo = Resources.Load<RenderTexture>(PopupRenderTextureTwoPath);
        return _popupTextureTwo;
    }

    void PositionNearTarget(RectTransform popupRect, Transform anchor, PopupPosition position, Camera worldCamera)
    {
        if (popupRect == null || PopupContainer == null)
            return;

        worldCamera = ResolveWorldCamera(worldCamera);
        if (worldCamera == null)
            return;

        Rect targetScreenRect = GetScreenRect(anchor, worldCamera);
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);

        Vector2 popupSize = popupRect.rect.size;
        if (popupSize.x <= 0f || popupSize.y <= 0f)
            popupSize = popupRect.sizeDelta;

        Rect screenBounds = GetScreenBounds();
        Vector2 anchoredPosition = ChoosePopupPosition(targetScreenRect, popupSize, screenBounds, position);

        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = anchoredPosition;
    }

    static Camera ResolveWorldCamera(Camera explicitCamera)
    {
        if (ScreenHandler.Instance != null)
            return ScreenHandler.Instance.ResolveWorldCamera(explicitCamera);

        if (explicitCamera != null && explicitCamera.isActiveAndEnabled)
            return explicitCamera;

        return Camera.main;
    }

    Vector2 ChoosePopupPosition(Rect targetScreenRect, Vector2 popupSize, Rect screenBounds, PopupPosition preferredPosition)
    {
        float halfW = popupSize.x * 0.5f;
        float halfH = popupSize.y * 0.5f;

        PopupPosition[] candidateOrder = GetCandidateOrder(preferredPosition);

        Vector2 bestPosition = Vector2.zero;
        float bestScore = float.MinValue;
        bool foundCandidate = false;

        for (int i = 0; i < candidateOrder.Length; i++)
        {
            Vector2 candidate = GetPositionForSide(candidateOrder[i], targetScreenRect, halfW, halfH, TargetGap);
            Rect popupScreenRect = CenteredScreenRect(candidate, popupSize);
            popupScreenRect = ClampRectToBounds(popupScreenRect, screenBounds);

            Vector2 localCenter = ScreenPointToContainerLocal(popupScreenRect.center);
            if (localCenter == Vector2.negativeInfinity)
                continue;

            float score = Overlaps(popupScreenRect, targetScreenRect, TargetGap)
                ? -RectOverlapArea(popupScreenRect, targetScreenRect)
                : 100000f;

            // Strongly prefer the requested side; only use fallbacks when it cannot fit.
            score -= i * 10000f;
            score -= Vector2.Distance(popupScreenRect.center, candidate);

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = localCenter;
                foundCandidate = true;
            }
        }

        if (!foundCandidate)
            bestPosition = ScreenPointToContainerLocal(targetScreenRect.center);

        return bestPosition;
    }

    static PopupPosition[] GetCandidateOrder(PopupPosition preferred)
    {
        switch (preferred)
        {
            case PopupPosition.Above:
                return new[] { PopupPosition.Above, PopupPosition.Below, PopupPosition.Right, PopupPosition.Left };
            case PopupPosition.Below:
                return new[] { PopupPosition.Below, PopupPosition.Above, PopupPosition.Right, PopupPosition.Left };
            case PopupPosition.Left:
                return new[] { PopupPosition.Left, PopupPosition.Right, PopupPosition.Above, PopupPosition.Below };
            case PopupPosition.Right:
                return new[] { PopupPosition.Right, PopupPosition.Left, PopupPosition.Above, PopupPosition.Below };
            default:
                return new[] { PopupPosition.Above, PopupPosition.Below, PopupPosition.Right, PopupPosition.Left };
        }
    }

    static Vector2 GetPositionForSide(PopupPosition side, Rect targetScreenRect, float halfW, float halfH, float gap)
    {
        Vector2 targetCenter = targetScreenRect.center;

        switch (side)
        {
            case PopupPosition.Below:
                return new Vector2(targetCenter.x, targetScreenRect.yMin - gap - halfH);
            case PopupPosition.Left:
                return new Vector2(targetScreenRect.xMin - gap - halfW, targetCenter.y);
            case PopupPosition.Right:
                return new Vector2(targetScreenRect.xMax + gap + halfW, targetCenter.y);
            case PopupPosition.Above:
            default:
                return new Vector2(targetCenter.x, targetScreenRect.yMax + gap + halfH);
        }
    }

    static float RectOverlapArea(Rect a, Rect b)
    {
        float xOverlap = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
        float yOverlap = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
        return xOverlap * yOverlap;
    }

    static bool Overlaps(Rect a, Rect b, float gap)
    {
        return a.xMin - gap < b.xMax
            && a.xMax + gap > b.xMin
            && a.yMin - gap < b.yMax
            && a.yMax + gap > b.yMin;
    }

    static Rect CenteredScreenRect(Vector2 center, Vector2 size)
    {
        return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
    }

    static Rect ClampRectToBounds(Rect rect, Rect bounds)
    {
        float xMin = Mathf.Clamp(rect.xMin, bounds.xMin, bounds.xMax - rect.width);
        float yMin = Mathf.Clamp(rect.yMin, bounds.yMin, bounds.yMax - rect.height);
        return new Rect(xMin, yMin, rect.width, rect.height);
    }

    Rect GetScreenBounds()
    {
        return new Rect(
            ScreenMargin,
            ScreenMargin,
            UnityEngine.Screen.width - ScreenMargin * 2f,
            UnityEngine.Screen.height - ScreenMargin * 2f);
    }

    Vector2 ScreenPointToContainerLocal(Vector2 screenPoint)
    {
        if (PopupContainer == null)
            return Vector2.negativeInfinity;

        Canvas canvas = PopupContainer.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                PopupContainer, screenPoint, eventCamera, out Vector2 localPoint))
            return localPoint;

        return Vector2.negativeInfinity;
    }

    static Rect GetScreenRect(Transform target, Camera camera)
    {
        if (target == null)
            return Rect.zero;

        if (camera == null)
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
            Vector3 screenPoint = camera.WorldToScreenPoint(target.position);
            float size = 48f;
            return new Rect(screenPoint.x - size * 0.5f, screenPoint.y - size * 0.5f, size, size);
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
