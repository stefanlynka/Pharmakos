using System.Collections.Generic;
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
    const string KeywordReminderResourcePath = "Prefabs/Popups/KeywordReminder";
    const float KeywordReminderGap = 8f;
    const float KeywordReminderPopupGap = 16f / 5f;
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
    readonly List<KeywordReminderPopup> _keywordRemindersOne = new List<KeywordReminderPopup>();
    readonly List<KeywordReminderPopup> _keywordRemindersTwo = new List<KeywordReminderPopup>();
    readonly List<string> _reminderTextBuffer = new List<string>();
    KeywordReminderSource _keywordSourceOne;
    KeywordReminderSource _keywordSourceTwo;

    enum KeywordReminderSource
    {
        None,
        ImagePopup,
        Hand,
    }

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

    public void ShowCardPopup(Card card, Transform anchor, PopupPosition position, PopupSlot slot = PopupSlot.One, Camera worldCamera = null, float lowerByWorld = 0f, float sideGapWorld = 0f)
    {
        if (card == null || anchor == null || PopupHandler.Instance == null)
            return;

        PopupHandler.ShowCard(slot, card);
        ShowImagePopup(slot, anchor, position, worldCamera, lowerByWorld, sideGapWorld);

        if (card is Follower follower)
        {
            SetKeywordSource(slot, KeywordReminderSource.ImagePopup);
            ShowStaticEffectReminders(slot, follower, anchor, worldCamera, besidePopup: true);
        }
        else if (GetKeywordSource(slot) == KeywordReminderSource.ImagePopup)
        {
            HideStaticEffectReminders(slot);
            SetKeywordSource(slot, KeywordReminderSource.None);
        }
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
        if (GetKeywordSource(slot) != KeywordReminderSource.Hand)
        {
            HideStaticEffectReminders(slot);
            SetKeywordSource(slot, KeywordReminderSource.None);
        }
    }

    public void ShowHandKeywordReminders(Follower follower, Transform anchor, PopupSlot slot, Camera worldCamera = null)
    {
        if (follower == null || anchor == null)
            return;

        SetKeywordSource(slot, KeywordReminderSource.Hand);
        ShowStaticEffectReminders(slot, follower, anchor, worldCamera, besidePopup: false);
    }

    public void HideHandKeywordReminders(PopupSlot slot)
    {
        if (GetKeywordSource(slot) != KeywordReminderSource.Hand)
            return;

        HideStaticEffectReminders(slot);
        SetKeywordSource(slot, KeywordReminderSource.None);
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

    void ShowImagePopup(PopupSlot slot, Transform anchor, PopupPosition position, Camera worldCamera, float lowerByWorld = 0f, float sideGapWorld = 0f)
    {
        RectTransform popup = GetOrCreateImagePopup(slot);
        if (popup == null)
            return;

        popup.gameObject.SetActive(true);
        SetImageAnchor(slot, anchor);
        PositionNearTarget(popup, anchor, position, worldCamera, lowerByWorld, sideGapWorld);
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

    void PositionNearTarget(RectTransform popupRect, Transform anchor, PopupPosition position, Camera worldCamera, float lowerByWorld = 0f, float sideGapWorld = 0f)
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
        float verticalScreenOffset = WorldDownToScreenY(anchor, worldCamera, lowerByWorld);
        float sideGapScreen = WorldDistanceToScreen(anchor, worldCamera, sideGapWorld);
        Vector2 anchoredPosition = ChoosePopupPosition(targetScreenRect, popupSize, screenBounds, position, verticalScreenOffset, sideGapScreen);

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

    Vector2 ChoosePopupPosition(Rect targetScreenRect, Vector2 popupSize, Rect screenBounds, PopupPosition preferredPosition, float verticalScreenOffset = 0f, float sideGapScreen = 0f)
    {
        float halfW = popupSize.x * 0.5f;
        float halfH = popupSize.y * 0.5f;

        PopupPosition[] candidateOrder = GetCandidateOrder(preferredPosition);

        Vector2 bestPosition = Vector2.zero;
        float bestScore = float.MinValue;
        bool foundCandidate = false;

        for (int i = 0; i < candidateOrder.Length; i++)
        {
            PopupPosition side = candidateOrder[i];
            bool lateral = side == PopupPosition.Left || side == PopupPosition.Right;
            float gap = TargetGap + (lateral ? sideGapScreen : 0f);

            Vector2 candidate = GetPositionForSide(side, targetScreenRect, halfW, halfH, gap);
            // Side choice stays based on the unshifted rect so dropping the popup does not flip it above or below.
            Rect idealPopupRect = CenteredScreenRect(candidate, popupSize);
            Vector2 placed = new Vector2(candidate.x, candidate.y + verticalScreenOffset);
            Rect popupScreenRect = ClampRectToBounds(CenteredScreenRect(placed, popupSize), screenBounds);

            Vector2 localCenter = ScreenPointToContainerLocal(popupScreenRect.center);
            if (localCenter == Vector2.negativeInfinity)
                continue;

            float score = Overlaps(idealPopupRect, targetScreenRect, gap)
                ? -RectOverlapArea(idealPopupRect, targetScreenRect)
                : 100000f;

            // Strongly prefer the requested side; only use fallbacks when it cannot fit.
            score -= i * 10000f;
            score -= Vector2.Distance(popupScreenRect.center, placed);

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

    static float WorldDownToScreenY(Transform anchor, Camera camera, float worldDistance)
    {
        if (worldDistance == 0f || anchor == null || camera == null)
            return 0f;

        Vector3 origin = camera.WorldToScreenPoint(anchor.position);
        Vector3 lowered = camera.WorldToScreenPoint(anchor.position - camera.transform.up * worldDistance);
        return lowered.y - origin.y;
    }

    static float WorldDistanceToScreen(Transform anchor, Camera camera, float worldDistance)
    {
        if (worldDistance == 0f || anchor == null || camera == null)
            return 0f;

        Vector3 origin = camera.WorldToScreenPoint(anchor.position);
        Vector3 shifted = camera.WorldToScreenPoint(anchor.position + camera.transform.right * worldDistance);
        return Vector2.Distance(new Vector2(origin.x, origin.y), new Vector2(shifted.x, shifted.y));
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

        Collider anchorCollider = target.GetComponent<Collider>();
        if (anchorCollider != null && anchorCollider.enabled)
            return BoundsToScreenRect(anchorCollider.bounds, camera);

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

        return BoundsToScreenRect(bounds, camera);
    }

    static Rect BoundsToScreenRect(Bounds bounds, Camera camera)
    {
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

    void ShowStaticEffectReminders(PopupSlot slot, Follower follower, Transform anchor, Camera worldCamera, bool besidePopup)
    {
        RectTransform cardPopup = GetImagePopup(slot);
        if (besidePopup && (cardPopup == null || !cardPopup.gameObject.activeSelf))
        {
            HideStaticEffectReminders(slot);
            return;
        }

        StaticEffectReminders.Collect(follower, _reminderTextBuffer);
        List<KeywordReminderPopup> reminders = GetKeywordReminders(slot);

        while (reminders.Count < _reminderTextBuffer.Count)
        {
            KeywordReminderPopup created = CreateKeywordReminder();
            if (created == null)
                return;

            reminders.Add(created);
        }

        for (int i = 0; i < reminders.Count; i++)
        {
            bool show = i < _reminderTextBuffer.Count;
            reminders[i].gameObject.SetActive(show);
            if (!show)
                continue;

            reminders[i].SetText(_reminderTextBuffer[i]);
            reminders[i].SetRaycastTargets(false);
            reminders[i].transform.SetAsLastSibling();
        }

        if (_reminderTextBuffer.Count == 0)
            return;

        LayoutKeywordReminders(slot, reminders, _reminderTextBuffer.Count, cardPopup, anchor, worldCamera, besidePopup);
    }

    void HideStaticEffectReminders(PopupSlot slot)
    {
        List<KeywordReminderPopup> reminders = GetKeywordReminders(slot);
        for (int i = 0; i < reminders.Count; i++)
        {
            if (reminders[i] != null)
                reminders[i].gameObject.SetActive(false);
        }
    }

    List<KeywordReminderPopup> GetKeywordReminders(PopupSlot slot)
    {
        return slot == PopupSlot.One ? _keywordRemindersOne : _keywordRemindersTwo;
    }

    KeywordReminderPopup CreateKeywordReminder()
    {
        KeywordReminderPopup prefab = Resources.Load<KeywordReminderPopup>(KeywordReminderResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"PopupScreenHandler: could not load KeywordReminder at Resources/{KeywordReminderResourcePath}");
            return null;
        }

        return Instantiate(prefab, PopupContainer);
    }

    void LayoutKeywordReminders(PopupSlot slot, List<KeywordReminderPopup> reminders, int count, RectTransform cardPopup, Transform anchor, Camera worldCamera, bool besidePopup)
    {
        worldCamera = ResolveWorldCamera(worldCamera);
        Rect followerRect = GetAnchorRectInContainer(anchor, worldCamera);
        Rect popupBounds = besidePopup ? GetVisiblePopupCardRect(slot, cardPopup) : followerRect;
        if (popupBounds.width <= 0f || popupBounds.height <= 0f)
            popupBounds = followerRect;

        Rect screenBounds = GetContainerLocalScreenBounds();
        bool outerOnRight = true;
        if (besidePopup && followerRect.width > 0f)
            outerOnRight = popupBounds.center.x >= followerRect.center.x;
        else if (screenBounds.width > 0f && followerRect.width > 0f)
            outerOnRight = screenBounds.xMax - followerRect.xMax >= followerRect.xMin - screenBounds.xMin;

        PlaceKeywordReminders(reminders, count, popupBounds, outerOnRight);
        float outerScore = KeywordPlacementScore(reminders, count, followerRect, screenBounds);

        PlaceKeywordReminders(reminders, count, popupBounds, !outerOnRight);
        float otherScore = KeywordPlacementScore(reminders, count, followerRect, screenBounds);
        if (outerScore <= otherScore)
            PlaceKeywordReminders(reminders, count, popupBounds, outerOnRight);

        ShiftRemindersIntoBounds(reminders, count, screenBounds, followerRect);
    }

    float KeywordPlacementScore(List<KeywordReminderPopup> reminders, int count, Rect followerRect, Rect screenBounds)
    {
        return ReminderOverlap(reminders, count, followerRect) * 10000f
            + OffScreenAmount(reminders, count, screenBounds);
    }

    void PlaceKeywordReminders(List<KeywordReminderPopup> reminders, int count, Rect popupBounds, bool placeOnRight)
    {
        float y = popupBounds.yMax;

        for (int i = 0; i < count; i++)
        {
            RectTransform rect = reminders[i].RectTransform;
            float width = rect.rect.width;
            float height = rect.rect.height;
            float x = placeOnRight
                ? popupBounds.xMax + KeywordReminderPopupGap + width * 0.5f
                : popupBounds.xMin - KeywordReminderPopupGap - width * 0.5f;
            float centerY = y - height * 0.5f;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, centerY);

            y -= height + KeywordReminderGap;
        }
    }

    void ShiftRemindersIntoBounds(List<KeywordReminderPopup> reminders, int count, Rect bounds, Rect forbidden)
    {
        if (count == 0 || bounds.width <= 0f || bounds.height <= 0f)
            return;

        GetReminderUnion(reminders, count, out float xMin, out float xMax, out float yMin, out float yMax);

        float shiftY = 0f;
        if (yMin < bounds.yMin)
            shiftY += bounds.yMin - yMin;
        if (yMax + shiftY > bounds.yMax)
            shiftY += bounds.yMax - (yMax + shiftY);

        if (shiftY != 0f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 position = reminders[i].RectTransform.anchoredPosition;
                position.y += shiftY;
                reminders[i].RectTransform.anchoredPosition = position;
            }
        }

        GetReminderUnion(reminders, count, out xMin, out xMax, out yMin, out yMax);

        float shiftX = 0f;
        if (xMin < bounds.xMin)
            shiftX += bounds.xMin - xMin;
        if (xMax + shiftX > bounds.xMax)
            shiftX += bounds.xMax - (xMax + shiftX);

        shiftX = LimitShiftAwayFromRect(xMin, xMax, yMin, yMax, forbidden, shiftX);
        if (shiftX == 0f)
            return;

        for (int i = 0; i < count; i++)
        {
            Vector2 position = reminders[i].RectTransform.anchoredPosition;
            position.x += shiftX;
            reminders[i].RectTransform.anchoredPosition = position;
        }
    }

    static float LimitShiftAwayFromRect(float xMin, float xMax, float yMin, float yMax, Rect forbidden, float shiftX)
    {
        if (shiftX == 0f || forbidden.width <= 0f || forbidden.height <= 0f)
            return shiftX;

        float movedYMin = yMin;
        float movedYMax = yMax;
        bool verticalOverlap = movedYMin < forbidden.yMax && movedYMax > forbidden.yMin;
        if (!verticalOverlap)
            return shiftX;

        if (shiftX < 0f && xMin >= forbidden.xMax)
        {
            float minShift = forbidden.xMax + KeywordReminderPopupGap - xMin;
            return Mathf.Max(shiftX, minShift);
        }

        if (shiftX > 0f && xMax <= forbidden.xMin)
        {
            float maxShift = forbidden.xMin - KeywordReminderPopupGap - xMax;
            return Mathf.Min(shiftX, maxShift);
        }

        Rect moved = Rect.MinMaxRect(xMin + shiftX, yMin, xMax + shiftX, yMax);
        if (moved.Overlaps(forbidden))
            return 0f;

        return shiftX;
    }

    float ReminderOverlap(List<KeywordReminderPopup> reminders, int count, Rect other)
    {
        if (other.width <= 0f || other.height <= 0f)
            return 0f;

        float area = 0f;
        for (int i = 0; i < count; i++)
            area += OverlapArea(GetReminderRect(reminders[i].RectTransform), other);

        return area;
    }

    float OffScreenAmount(List<KeywordReminderPopup> reminders, int count, Rect bounds)
    {
        if (bounds.width <= 0f || bounds.height <= 0f)
            return 0f;

        GetReminderUnion(reminders, count, out float xMin, out float xMax, out float yMin, out float yMax);
        float amount = 0f;
        if (xMin < bounds.xMin)
            amount += bounds.xMin - xMin;
        if (xMax > bounds.xMax)
            amount += xMax - bounds.xMax;
        if (yMin < bounds.yMin)
            amount += bounds.yMin - yMin;
        if (yMax > bounds.yMax)
            amount += yMax - bounds.yMax;
        return amount;
    }

    static Rect GetReminderRect(RectTransform rect)
    {
        Vector2 center = rect.anchoredPosition;
        Vector2 size = rect.rect.size;
        return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
    }

    static void GetReminderUnion(List<KeywordReminderPopup> reminders, int count, out float xMin, out float xMax, out float yMin, out float yMax)
    {
        xMin = float.MaxValue;
        xMax = float.MinValue;
        yMin = float.MaxValue;
        yMax = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            Rect rect = GetReminderRect(reminders[i].RectTransform);
            xMin = Mathf.Min(xMin, rect.xMin);
            xMax = Mathf.Max(xMax, rect.xMax);
            yMin = Mathf.Min(yMin, rect.yMin);
            yMax = Mathf.Max(yMax, rect.yMax);
        }
    }

    static float OverlapArea(Rect a, Rect b)
    {
        float xOverlap = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
        float yOverlap = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
        return xOverlap * yOverlap;
    }

    KeywordReminderSource GetKeywordSource(PopupSlot slot)
    {
        return slot == PopupSlot.One ? _keywordSourceOne : _keywordSourceTwo;
    }

    void SetKeywordSource(PopupSlot slot, KeywordReminderSource source)
    {
        if (slot == PopupSlot.One)
            _keywordSourceOne = source;
        else
            _keywordSourceTwo = source;
    }

    Rect GetAnchorRectInContainer(Transform anchor, Camera camera)
    {
        if (anchor == null || camera == null)
            return Rect.zero;

        Rect screenRect = GetScreenRect(anchor, camera);
        Vector2 min = ScreenPointToContainerLocal(new Vector2(screenRect.xMin, screenRect.yMin));
        Vector2 max = ScreenPointToContainerLocal(new Vector2(screenRect.xMax, screenRect.yMax));
        if (min == Vector2.negativeInfinity || max == Vector2.negativeInfinity)
            return Rect.zero;

        return Rect.MinMaxRect(
            Mathf.Min(min.x, max.x),
            Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x),
            Mathf.Max(min.y, max.y));
    }

    Rect GetVisiblePopupCardRect(PopupSlot slot, RectTransform cardPopup)
    {
        Rect imageRect = GetRectInContainer(cardPopup);
        if (imageRect.width <= 0f || imageRect.height <= 0f)
            return imageRect;

        if (PopupHandler.Instance == null)
            return imageRect;

        PopupCapture capture = slot == PopupSlot.One ? PopupHandler.Instance.Popup1 : PopupHandler.Instance.Popup2;
        if (capture == null || capture.ViewFollower == null || !capture.ViewFollower.gameObject.activeInHierarchy)
            return imageRect;

        Camera popupCamera = capture.GetComponentInChildren<Camera>(true);
        if (popupCamera == null)
            return imageRect;

        Rect viewport = GetViewportRect(capture.ViewFollower.transform, popupCamera);
        if (viewport.width <= 0.05f || viewport.height <= 0.05f)
            return imageRect;

        return new Rect(
            imageRect.x + viewport.xMin * imageRect.width,
            imageRect.y + viewport.yMin * imageRect.height,
            viewport.width * imageRect.width,
            viewport.height * imageRect.height);
    }

    static Rect GetViewportRect(Transform target, Camera camera)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds bounds = default;
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
            return new Rect(0f, 0f, 1f, 1f);

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

        float xMin = 1f;
        float xMax = 0f;
        float yMin = 1f;
        float yMax = 0f;
        bool hasPoint = false;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 viewport = camera.WorldToViewportPoint(corners[i]);
            if (viewport.z <= 0f)
                continue;

            hasPoint = true;
            xMin = Mathf.Min(xMin, viewport.x);
            xMax = Mathf.Max(xMax, viewport.x);
            yMin = Mathf.Min(yMin, viewport.y);
            yMax = Mathf.Max(yMax, viewport.y);
        }

        if (!hasPoint || xMax <= xMin || yMax <= yMin)
            return new Rect(0f, 0f, 1f, 1f);

        xMin = Mathf.Clamp01(xMin);
        xMax = Mathf.Clamp01(xMax);
        yMin = Mathf.Clamp01(yMin);
        yMax = Mathf.Clamp01(yMax);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    Rect GetRectInContainer(RectTransform target)
    {
        if (target == null || PopupContainer == null)
            return Rect.zero;

        Vector3[] corners = new Vector3[4];
        bool hasBounds = false;
        Bounds bounds = new Bounds();
        RectTransform[] rects = target.GetComponentsInChildren<RectTransform>(false);
        for (int r = 0; r < rects.Length; r++)
        {
            rects[r].GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = PopupContainer.InverseTransformPoint(corners[i]);
                if (!hasBounds)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(local);
            }
        }

        if (!hasBounds)
            return new Rect(target.anchoredPosition, target.rect.size);

        return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
    }

    Rect GetContainerLocalScreenBounds()
    {
        Vector2 min = ScreenPointToContainerLocal(new Vector2(ScreenMargin, ScreenMargin));
        Vector2 max = ScreenPointToContainerLocal(new Vector2(UnityEngine.Screen.width - ScreenMargin, UnityEngine.Screen.height - ScreenMargin));
        if (min == Vector2.negativeInfinity || max == Vector2.negativeInfinity)
            return Rect.zero;

        return Rect.MinMaxRect(
            Mathf.Min(min.x, max.x),
            Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x),
            Mathf.Max(min.y, max.y));
    }
}
