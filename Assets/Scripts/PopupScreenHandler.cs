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
    const string RitualPopupResourcePath = "Prefabs/Popups/RitualPopup";
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
    RitualPopup _activeRitualPopup;
    Transform _ritualHoverAnchor;
    readonly List<Card> _ritualReferenceCards = new List<Card>();
    Transform _ritualReferenceAnchor;
    int _ritualReferenceKey;
    readonly List<Card> _followerReferenceCards = new List<Card>();
    Transform _followerReferenceAnchor;
    PopupSlot _followerReferenceSlot = PopupSlot.Two;
    int _followerReferenceKey;
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

    /// <summary>Shows the ritual summary popup on the given side of <paramref name="anchor"/>.</summary>
    public void ShowRitualHoverPopup(Ritual ritual, Transform anchor, PopupPosition position, Camera worldCamera = null)
    {
        if (ritual == null || anchor == null)
            return;

        if (_ritualHoverAnchor != null && _ritualHoverAnchor != anchor)
            HideRitualReferenceCards(_ritualHoverAnchor);

        _ritualHoverAnchor = anchor;

        RitualPopup popup = GetOrCreateRitualPopup();
        if (popup == null)
            return;

        popup.SetRitual(ritual);
        popup.gameObject.SetActive(true);
        popup.SetRaycastTargets(false);

        PositionNearTarget(popup.RectTransform, anchor, position, worldCamera);
        ShowRitualReferenceCards(ritual, anchor, worldCamera);
    }

    /// <summary>Hides the ritual summary popup only if it was shown for <paramref name="anchor"/>.</summary>
    public void HideRitualHoverPopup(Transform anchor)
    {
        if (anchor == null)
            return;

        // Another altar may already have replaced the summary. Still drop cards this one opened.
        if (_activeRitualPopup == null || _ritualHoverAnchor != anchor)
        {
            HideRitualReferenceCards(anchor);
            return;
        }

        HideRitualHoverPopup();
    }

    public void HideRitualHoverPopup()
    {
        Transform anchor = _ritualHoverAnchor;
        _ritualHoverAnchor = null;
        _ritualReferenceAnchor = null;
        _ritualReferenceKey = 0;

        if (_activeRitualPopup != null)
            _activeRitualPopup.gameObject.SetActive(false);

        HideRitualReferenceCards(anchor);
    }

    void ShowRitualReferenceCards(Ritual ritual, Transform anchor, Camera worldCamera)
    {
        _ritualReferenceCards.Clear();
        if (ritual != null)
            ritual.CollectReferencedCards(_ritualReferenceCards);

        int count = Mathf.Min(_ritualReferenceCards.Count, 2);
        int key = 17;
        for (int i = 0; i < count; i++)
        {
            Card card = _ritualReferenceCards[i];
            key = key * 31 + (card != null ? card.GetType().GetHashCode() : 0);
        }

        bool reuse = count > 0 && key == _ritualReferenceKey && _ritualReferenceAnchor == anchor;
        _ritualReferenceKey = count > 0 ? key : 0;
        _ritualReferenceAnchor = count > 0 ? anchor : null;

        for (int i = 0; i < 2; i++)
        {
            PopupSlot slot = i == 0 ? PopupSlot.One : PopupSlot.Two;
            if (i >= count || _ritualReferenceCards[i] == null)
            {
                if (GetImageAnchor(slot) == anchor)
                    HideImagePopup(slot);
                continue;
            }

            RectTransform existing = GetImagePopup(slot);
            bool showing = existing != null && existing.gameObject.activeSelf && GetImageAnchor(slot) == anchor;
            if (!reuse || !showing)
                ShowCardPopup(_ritualReferenceCards[i], anchor, PopupPosition.Right, slot, worldCamera);
        }

        if (count == 0)
            return;

        PlaceRitualReferenceCards(anchor, worldCamera, count);

        for (int i = 0; i < count; i++)
        {
            if (_ritualReferenceCards[i] is not Follower follower)
                continue;

            PopupSlot slot = i == 0 ? PopupSlot.One : PopupSlot.Two;
            SetKeywordSource(slot, KeywordReminderSource.ImagePopup);
            ShowStaticEffectReminders(slot, follower, anchor, worldCamera, besidePopup: true);
            RepositionRitualReminders(slot);
        }
    }

    void HideRitualReferenceCards(Transform anchor)
    {
        if (anchor == null)
            return;

        if (GetImageAnchor(PopupSlot.One) == anchor)
            HideImagePopup(PopupSlot.One);
        if (GetImageAnchor(PopupSlot.Two) == anchor)
            HideImagePopup(PopupSlot.Two);
    }

    void PlaceRitualReferenceCards(Transform anchor, Camera worldCamera, int count)
    {
        if (_activeRitualPopup == null)
            return;

        RectTransform ritualPopup = _activeRitualPopup.RectTransform;
        LayoutRebuilder.ForceRebuildLayoutImmediate(ritualPopup);
        Rect ritualRect = GetRectInContainer(ritualPopup);
        if (ritualRect.width <= 0f || ritualRect.height <= 0f)
            return;

        worldCamera = ResolveWorldCamera(worldCamera);
        Rect altarRect = GetAnchorRectInContainer(anchor, worldCamera);
        Rect screenBounds = GetContainerLocalScreenBounds();

        RectTransform[] popups = new RectTransform[count];
        Vector2[] sizes = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            PopupSlot slot = i == 0 ? PopupSlot.One : PopupSlot.Two;
            RectTransform popup = GetImagePopup(slot);
            popups[i] = popup;
            if (popup == null)
                continue;

            LayoutRebuilder.ForceRebuildLayoutImmediate(popup);
            sizes[i] = GetRectInContainer(popup).size;
        }

        PlaceRitualCards(popups, sizes, count, ritualRect, true);
        ClampPopupsIntoBounds(popups, count, screenBounds);
        float rightScore = RitualCardScore(popups, count, ritualRect, altarRect, screenBounds);
        Vector2[] rightPositions = SnapshotPopupPositions(popups, count);

        PlaceRitualCards(popups, sizes, count, ritualRect, false);
        ClampPopupsIntoBounds(popups, count, screenBounds);
        float leftScore = RitualCardScore(popups, count, ritualRect, altarRect, screenBounds);
        if (rightScore < leftScore)
            RestorePopupPositions(popups, rightPositions, count);
    }

    void PlaceRitualCards(RectTransform[] popups, Vector2[] sizes, int count, Rect ritualRect, bool placeOnRight)
    {
        float totalHeight = 0f;
        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            if (popups[i] == null || sizes[i].y <= 0f)
                continue;

            if (placed > 0)
                totalHeight += KeywordReminderPopupGap;
            totalHeight += sizes[i].y;
            placed++;
        }

        float y = ritualRect.center.y + totalHeight * 0.5f;
        for (int i = 0; i < count; i++)
        {
            if (popups[i] == null || sizes[i].y <= 0f)
                continue;

            float width = sizes[i].x;
            float height = sizes[i].y;
            float x = placeOnRight
                ? ritualRect.xMax + KeywordReminderPopupGap + width * 0.5f
                : ritualRect.xMin - KeywordReminderPopupGap - width * 0.5f;
            MovePopupToContainerPoint(popups[i], new Vector2(x, y - height * 0.5f));
            popups[i].SetAsLastSibling();
            y -= height + KeywordReminderPopupGap;
        }
    }

    float RitualCardScore(RectTransform[] popups, int count, Rect ritualRect, Rect altarRect, Rect screenBounds)
    {
        float score = 0f;
        for (int i = 0; i < count; i++)
        {
            if (popups[i] == null)
                continue;

            Rect rect = GetRectInContainer(popups[i]);
            score += OffScreenAmount(rect, screenBounds) * 100000f;
            score += OverlapArea(rect, ritualRect) * 10000f;
            if (altarRect.width > 0f && altarRect.height > 0f)
                score += OverlapArea(rect, altarRect) * 1000f;
        }

        return score;
    }

    static Vector2[] SnapshotPopupPositions(RectTransform[] popups, int count)
    {
        Vector2[] positions = new Vector2[count];
        for (int i = 0; i < count; i++)
            positions[i] = popups[i] != null ? popups[i].anchoredPosition : Vector2.zero;
        return positions;
    }

    static void RestorePopupPositions(RectTransform[] popups, Vector2[] positions, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (popups[i] != null)
                popups[i].anchoredPosition = positions[i];
        }
    }

    void ClampPopupsIntoBounds(RectTransform[] popups, int count, Rect bounds)
    {
        if (bounds.width <= 0f || bounds.height <= 0f)
            return;

        GetPopupUnion(popups, count, out float xMin, out float xMax, out float yMin, out float yMax);
        if (xMin == float.MaxValue)
            return;

        float shiftY = 0f;
        if (yMin < bounds.yMin)
            shiftY += bounds.yMin - yMin;
        if (yMax + shiftY > bounds.yMax)
            shiftY += bounds.yMax - (yMax + shiftY);
        ShiftPopups(popups, count, 0f, shiftY);

        GetPopupUnion(popups, count, out xMin, out xMax, out yMin, out yMax);
        float shiftX = 0f;
        if (xMin < bounds.xMin)
            shiftX += bounds.xMin - xMin;
        if (xMax + shiftX > bounds.xMax)
            shiftX += bounds.xMax - (xMax + shiftX);
        ShiftPopups(popups, count, shiftX, 0f);
    }

    static void ShiftPopups(RectTransform[] popups, int count, float shiftX, float shiftY)
    {
        if (shiftX == 0f && shiftY == 0f)
            return;

        for (int i = 0; i < count; i++)
        {
            if (popups[i] == null)
                continue;

            Vector2 position = popups[i].anchoredPosition;
            position.x += shiftX;
            position.y += shiftY;
            popups[i].anchoredPosition = position;
        }
    }

    void RepositionRitualReminders(PopupSlot slot)
    {
        if (_activeRitualPopup == null)
            return;

        List<KeywordReminderPopup> reminders = GetKeywordReminders(slot);
        int count = 0;
        while (count < reminders.Count && reminders[count] != null && reminders[count].gameObject.activeSelf)
            count++;
        if (count == 0)
            return;

        RectTransform cardPopup = GetImagePopup(slot);
        Rect cardRect = GetVisiblePopupCardRect(slot, cardPopup);
        if (cardRect.width <= 1f || cardRect.height <= 1f)
            cardRect = GetRectInContainer(cardPopup);
        if (cardRect.width <= 0f || cardRect.height <= 0f)
            return;

        Rect ritualRect = GetRectInContainer(_activeRitualPopup.RectTransform);
        Rect screenBounds = GetContainerLocalScreenBounds();
        Rect siblingRect = GetOtherRitualCardRect(slot);
        bool outerOnRight = cardRect.center.x >= ritualRect.center.x;

        float bestScore = float.MaxValue;
        Vector2[] bestPositions = null;

        void Consider(System.Action place)
        {
            place();
            ClampRemindersIntoBounds(reminders, count, screenBounds, cardRect, ritualRect, siblingRect);
            float score = RitualReminderScore(reminders, count, cardRect, ritualRect, siblingRect, screenBounds);
            if (score >= bestScore)
                return;

            bestScore = score;
            bestPositions = CopyReminderPositions(reminders, count);
        }

        Consider(() => PlaceKeywordReminders(reminders, count, cardRect, outerOnRight));
        Consider(() => PlaceKeywordReminders(reminders, count, cardRect, !outerOnRight));
        Consider(() => PlaceReminderStack(reminders, count, cardRect.center.x, cardRect.yMax + KeywordReminderPopupGap, upward: true));
        Consider(() => PlaceReminderStack(reminders, count, cardRect.center.x, cardRect.yMin - KeywordReminderPopupGap, upward: false));

        if (bestPositions != null)
            ApplyReminderPositions(reminders, count, bestPositions);
    }

    void PlaceReminderStack(List<KeywordReminderPopup> reminders, int count, float centerX, float edgeY, bool upward)
    {
        float cursor = edgeY;
        for (int i = 0; i < count; i++)
        {
            RectTransform rect = reminders[i].RectTransform;
            float height = rect.rect.height;
            float centerY = upward ? cursor + height * 0.5f : cursor - height * 0.5f;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(centerX, centerY);

            float step = height + KeywordReminderGap;
            cursor = upward ? cursor + step : cursor - step;
        }
    }

    Rect GetOtherRitualCardRect(PopupSlot slot)
    {
        RectTransform other = GetImagePopup(slot == PopupSlot.One ? PopupSlot.Two : PopupSlot.One);
        if (other == null || !other.gameObject.activeSelf)
            return Rect.zero;

        Rect rect = GetRectInContainer(other);
        if (rect.width <= 0f || rect.height <= 0f)
            return Rect.zero;

        return rect;
    }

    float RitualReminderScore(List<KeywordReminderPopup> reminders, int count, Rect cardRect, Rect ritualRect, Rect siblingRect, Rect screenBounds)
    {
        return OffScreenAmount(reminders, count, screenBounds) * 100000f
            + ReminderOverlap(reminders, count, ritualRect) * 100000f
            + ReminderOverlap(reminders, count, cardRect) * 10000f
            + ReminderOverlap(reminders, count, siblingRect) * 10000f;
    }

    void ClampRemindersIntoBounds(List<KeywordReminderPopup> reminders, int count, Rect bounds, Rect cardRect, Rect ritualRect, Rect siblingRect)
    {
        if (bounds.width <= 0f || bounds.height <= 0f)
            return;

        GetReminderUnion(reminders, count, out float xMin, out float xMax, out float yMin, out float yMax);
        if (xMin == float.MaxValue)
            return;

        float shiftY = 0f;
        if (yMin < bounds.yMin)
            shiftY += bounds.yMin - yMin;
        if (yMax + shiftY > bounds.yMax)
            shiftY += bounds.yMax - (yMax + shiftY);
        shiftY = LimitReminderShift(xMin, xMax, yMin, yMax, cardRect, shiftY, vertical: true);
        shiftY = LimitReminderShift(xMin, xMax, yMin, yMax, ritualRect, shiftY, vertical: true);
        shiftY = LimitReminderShift(xMin, xMax, yMin, yMax, siblingRect, shiftY, vertical: true);
        ShiftReminders(reminders, count, 0f, shiftY);

        GetReminderUnion(reminders, count, out xMin, out xMax, out yMin, out yMax);
        float shiftX = 0f;
        if (xMin < bounds.xMin)
            shiftX += bounds.xMin - xMin;
        if (xMax + shiftX > bounds.xMax)
            shiftX += bounds.xMax - (xMax + shiftX);
        shiftX = LimitReminderShift(xMin, xMax, yMin, yMax, cardRect, shiftX, vertical: false);
        shiftX = LimitReminderShift(xMin, xMax, yMin, yMax, ritualRect, shiftX, vertical: false);
        shiftX = LimitReminderShift(xMin, xMax, yMin, yMax, siblingRect, shiftX, vertical: false);
        ShiftReminders(reminders, count, shiftX, 0f);
    }

    static void ShiftReminders(List<KeywordReminderPopup> reminders, int count, float shiftX, float shiftY)
    {
        if (shiftX == 0f && shiftY == 0f)
            return;

        for (int i = 0; i < count; i++)
        {
            Vector2 position = reminders[i].RectTransform.anchoredPosition;
            position.x += shiftX;
            position.y += shiftY;
            reminders[i].RectTransform.anchoredPosition = position;
        }
    }

    static Vector2[] CopyReminderPositions(List<KeywordReminderPopup> reminders, int count)
    {
        Vector2[] positions = new Vector2[count];
        for (int i = 0; i < count; i++)
            positions[i] = reminders[i].RectTransform.anchoredPosition;
        return positions;
    }

    static void ApplyReminderPositions(List<KeywordReminderPopup> reminders, int count, Vector2[] positions)
    {
        for (int i = 0; i < count; i++)
            reminders[i].RectTransform.anchoredPosition = positions[i];
    }

    static float OffScreenAmount(Rect rect, Rect bounds)
    {
        if (bounds.width <= 0f || bounds.height <= 0f)
            return 0f;

        float amount = 0f;
        if (rect.xMin < bounds.xMin)
            amount += bounds.xMin - rect.xMin;
        if (rect.xMax > bounds.xMax)
            amount += rect.xMax - bounds.xMax;
        if (rect.yMin < bounds.yMin)
            amount += bounds.yMin - rect.yMin;
        if (rect.yMax > bounds.yMax)
            amount += rect.yMax - bounds.yMax;
        return amount;
    }

    void GetPopupUnion(RectTransform[] popups, int count, out float xMin, out float xMax, out float yMin, out float yMax)
    {
        xMin = float.MaxValue;
        xMax = float.MinValue;
        yMin = float.MaxValue;
        yMax = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            if (popups[i] == null)
                continue;

            Rect rect = GetRectInContainer(popups[i]);
            if (rect.width <= 0f || rect.height <= 0f)
                continue;

            xMin = Mathf.Min(xMin, rect.xMin);
            xMax = Mathf.Max(xMax, rect.xMax);
            yMin = Mathf.Min(yMin, rect.yMin);
            yMax = Mathf.Max(yMax, rect.yMax);
        }
    }

    void MovePopupToContainerPoint(RectTransform popup, Vector2 containerCenter)
    {
        Rect current = GetRectInContainer(popup);
        if (current.width <= 0f || current.height <= 0f)
        {
            popup.anchoredPosition = containerCenter;
            return;
        }

        Vector2 delta = containerCenter - current.center;
        popup.anchoredPosition += delta;
    }

    public void ShowCardPopup(Card card, Transform anchor, PopupPosition position, PopupSlot slot = PopupSlot.One, Camera worldCamera = null, float lowerByWorld = 0f, float sideGapWorld = 0f, string reminderText = null)
    {
        if (card == null || anchor == null || PopupHandler.Instance == null)
            return;

        PopupHandler.ShowCard(slot, card);
        ShowImagePopup(slot, anchor, position, worldCamera, lowerByWorld, sideGapWorld);

        if (card is Follower follower)
        {
            SetKeywordSource(slot, KeywordReminderSource.ImagePopup);
            ShowStaticEffectReminders(slot, follower, anchor, worldCamera, besidePopup: true, reminderText);
        }
        else if (!string.IsNullOrEmpty(reminderText))
        {
            SetKeywordSource(slot, KeywordReminderSource.ImagePopup);
            ShowReminderText(slot, reminderText, anchor, worldCamera, besidePopup: true);
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

    public Transform FollowerReferenceAnchor => _followerReferenceAnchor;

    /// <summary>
    /// Shows the card a hovered follower names, in the popup slot that is not already showing that follower.
    /// Placed outside the follower and its keyword reminders.
    /// </summary>
    public void ShowFollowerReferencedCard(Follower follower, Transform anchor, PopupSlot primarySlot, Camera worldCamera = null)
    {
        if (anchor == null)
            return;

        if (_followerReferenceAnchor != null && _followerReferenceAnchor != anchor)
            HideFollowerReferencedCard(_followerReferenceAnchor);

        _followerReferenceCards.Clear();
        if (follower != null)
            follower.CollectReferencedCards(_followerReferenceCards);

        Card preview = null;
        for (int i = 0; i < _followerReferenceCards.Count; i++)
        {
            if (_followerReferenceCards[i] != null)
            {
                preview = _followerReferenceCards[i];
                break;
            }
        }

        if (preview == null)
        {
            HideFollowerReferencedCard(anchor);
            return;
        }

        PopupSlot referenceSlot = primarySlot == PopupSlot.One ? PopupSlot.Two : PopupSlot.One;
        int key = preview.GetType().GetHashCode();
        if (preview.Costs != null && preview.Costs.TryGetValue(OfferingType.Gold, out int gold))
            key = key * 31 + gold;

        RectTransform existing = GetImagePopup(referenceSlot);
        bool reuse = key == _followerReferenceKey
            && _followerReferenceAnchor == anchor
            && GetImageAnchor(referenceSlot) == anchor
            && existing != null
            && existing.gameObject.activeSelf;

        _followerReferenceAnchor = anchor;
        _followerReferenceSlot = referenceSlot;
        _followerReferenceKey = key;

        if (!reuse)
            ShowCardPopup(preview, anchor, PopupPosition.Right, referenceSlot, worldCamera);

        RectTransform popup = GetImagePopup(referenceSlot);
        if (popup == null || !popup.gameObject.activeSelf)
            return;

        worldCamera = ResolveWorldCamera(worldCamera);
        Rect anchorRect = GetAnchorRectInContainer(anchor, worldCamera);
        RectTransform primaryPopup = GetImagePopup(primarySlot);
        bool primaryPopupShown = primaryPopup != null
            && primaryPopup.gameObject.activeSelf
            && GetImageAnchor(primarySlot) == anchor;
        Rect host = primaryPopupShown ? GetRectInContainer(primaryPopup) : anchorRect;
        Rect alignTo = host;
        if (primaryPopupShown)
        {
            Rect visible = GetVisiblePopupCardRect(primarySlot, primaryPopup);
            if (visible.width > 1f && visible.height > 1f)
                alignTo = visible;
        }

        Rect sourceReminders = GetActiveReminderBounds(primarySlot);
        Rect blocked = UnionRect(UnionRect(host, anchorRect), sourceReminders);
        if (blocked.width <= 0f || blocked.height <= 0f)
            blocked = host.width > 0f ? host : anchorRect;

        bool preferRight = true;
        if (anchorRect.width > 0f && host.width > 0f && Mathf.Abs(host.center.x - anchorRect.center.x) > 1f)
            preferRight = host.center.x >= anchorRect.center.x;
        else
        {
            Rect screenBounds = GetContainerLocalScreenBounds();
            if (screenBounds.width > 0f && host.width > 0f)
                preferRight = screenBounds.xMax - host.xMax >= host.xMin - screenBounds.xMin;
        }

        PlaceFollowerReferenceCard(popup, blocked, alignTo, preferRight);
        popup.SetAsLastSibling();

        Rect referenceRect = GetRectInContainer(popup);
        if (OverlapsReminders(primarySlot, referenceRect))
        {
            Rect beside = alignTo.width > 0f ? alignTo : host;
            Rect boardRect = ApproximatelySameRect(beside, anchorRect) ? Rect.zero : anchorRect;
            RepositionReminders(primarySlot, beside, referenceRect, boardRect);
        }

        Rect referenceVisible = GetVisiblePopupCardRect(referenceSlot, popup);
        if (referenceVisible.width <= 1f || referenceVisible.height <= 1f)
            referenceVisible = referenceRect;

        RepositionReminders(referenceSlot, referenceVisible, UnionRect(host, anchorRect), GetActiveReminderBounds(primarySlot));
        BringRemindersToFront(primarySlot);
        BringRemindersToFront(referenceSlot);
    }

    public void HideFollowerReferencedCard(Transform anchor)
    {
        if (anchor == null || _followerReferenceAnchor != anchor)
            return;

        PopupSlot slot = _followerReferenceSlot;
        _followerReferenceAnchor = null;
        _followerReferenceKey = 0;
        HideImagePopup(slot);
    }

    public void HideFollowerReferencedCardUnder(Transform root)
    {
        if (_followerReferenceAnchor == null || root == null)
            return;

        if (_followerReferenceAnchor == root || _followerReferenceAnchor.IsChildOf(root))
            HideFollowerReferencedCard(_followerReferenceAnchor);
    }

    void PlaceFollowerReferenceCard(RectTransform popup, Rect blocked, Rect alignTo, bool preferRight)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(popup);
        Vector2 size = GetRectInContainer(popup).size;
        if (size.x <= 0f || size.y <= 0f)
            return;

        Rect screenBounds = GetContainerLocalScreenBounds();
        float alignY = alignTo.height > 0f ? alignTo.center.y : blocked.center.y;
        RectTransform[] popups = { popup };

        float Place(bool onRight)
        {
            float x = onRight
                ? blocked.xMax + KeywordReminderPopupGap + size.x * 0.5f
                : blocked.xMin - KeywordReminderPopupGap - size.x * 0.5f;
            MovePopupToContainerPoint(popup, new Vector2(x, alignY));
            ClampPopupsIntoBounds(popups, 1, screenBounds);
            Rect rect = GetRectInContainer(popup);
            return OffScreenAmount(rect, screenBounds) * 100000f
                + OverlapArea(rect, blocked) * 100000f;
        }

        float firstScore = Place(preferRight);
        Vector2 firstPosition = popup.anchoredPosition;
        float secondScore = Place(!preferRight);
        if (firstScore <= secondScore)
            popup.anchoredPosition = firstPosition;
    }

    void RepositionReminders(PopupSlot slot, Rect beside, Rect avoidA, Rect avoidB)
    {
        List<KeywordReminderPopup> reminders = GetKeywordReminders(slot);
        int count = ActiveReminderCount(reminders);
        if (count == 0 || beside.width <= 0f || beside.height <= 0f)
            return;

        Rect screenBounds = GetContainerLocalScreenBounds();
        bool preferRight = avoidA.width <= 0f || beside.center.x >= avoidA.center.x;
        float bestScore = float.MaxValue;
        Vector2[] bestPositions = null;

        void Consider(System.Action place)
        {
            place();
            ClampRemindersIntoBounds(reminders, count, screenBounds, beside, avoidA, avoidB);
            float score = RitualReminderScore(reminders, count, beside, avoidA, avoidB, screenBounds);
            if (score >= bestScore)
                return;

            bestScore = score;
            bestPositions = CopyReminderPositions(reminders, count);
        }

        Consider(() => PlaceKeywordReminders(reminders, count, beside, preferRight));
        Consider(() => PlaceKeywordReminders(reminders, count, beside, !preferRight));
        Consider(() => PlaceReminderStack(reminders, count, beside.center.x, beside.yMax + KeywordReminderPopupGap, upward: true));
        Consider(() => PlaceReminderStack(reminders, count, beside.center.x, beside.yMin - KeywordReminderPopupGap, upward: false));

        if (bestPositions != null)
            ApplyReminderPositions(reminders, count, bestPositions);
    }

    void BringRemindersToFront(PopupSlot slot)
    {
        List<KeywordReminderPopup> reminders = GetKeywordReminders(slot);
        int count = ActiveReminderCount(reminders);
        for (int i = 0; i < count; i++)
            reminders[i].transform.SetAsLastSibling();
    }

    bool OverlapsReminders(PopupSlot slot, Rect other)
    {
        List<KeywordReminderPopup> reminders = GetKeywordReminders(slot);
        return ReminderOverlap(reminders, ActiveReminderCount(reminders), other) > 0f;
    }

    int ActiveReminderCount(List<KeywordReminderPopup> reminders)
    {
        int count = 0;
        while (count < reminders.Count && reminders[count] != null && reminders[count].gameObject.activeSelf)
            count++;
        return count;
    }

    Rect GetActiveReminderBounds(PopupSlot slot)
    {
        List<KeywordReminderPopup> reminders = GetKeywordReminders(slot);
        int count = ActiveReminderCount(reminders);
        if (count == 0)
            return Rect.zero;

        GetReminderUnion(reminders, count, out float xMin, out float xMax, out float yMin, out float yMax);
        if (xMin == float.MaxValue)
            return Rect.zero;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    static Rect UnionRect(Rect a, Rect b)
    {
        if (a.width <= 0f || a.height <= 0f)
            return b;
        if (b.width <= 0f || b.height <= 0f)
            return a;

        return Rect.MinMaxRect(
            Mathf.Min(a.xMin, b.xMin),
            Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax),
            Mathf.Max(a.yMax, b.yMax));
    }

    static bool ApproximatelySameRect(Rect a, Rect b)
    {
        return Mathf.Approximately(a.xMin, b.xMin)
            && Mathf.Approximately(a.yMin, b.yMin)
            && Mathf.Approximately(a.width, b.width)
            && Mathf.Approximately(a.height, b.height);
    }

    public Transform GetImageAnchor(PopupSlot slot)
    {
        return slot == PopupSlot.One ? _imageAnchorOne : _imageAnchorTwo;
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

    RitualPopup GetOrCreateRitualPopup()
    {
        if (_activeRitualPopup != null)
            return _activeRitualPopup;

        RitualPopup prefab = Resources.Load<RitualPopup>(RitualPopupResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"PopupScreenHandler: could not load RitualPopup at Resources/{RitualPopupResourcePath}");
            return null;
        }

        _activeRitualPopup = Instantiate(prefab, PopupContainer);
        return _activeRitualPopup;
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

        Vector2 popupSize = PopupSizeOnScreen(popupRect);

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
            Vector2 placed = new Vector2(candidate.x, candidate.y + verticalScreenOffset);
            Rect popupScreenRect = ClampRectToBounds(CenteredScreenRect(placed, popupSize), screenBounds);

            Vector2 localCenter = ScreenPointToContainerLocal(popupScreenRect.center);
            if (localCenter == Vector2.negativeInfinity)
                continue;

            // Score the rect that is actually drawn. Clamping an off-screen spot back
            // onto the screen can drop the popup on top of an anchor in the corner.
            float score = Overlaps(popupScreenRect, targetScreenRect, gap)
                ? -RectOverlapArea(popupScreenRect, targetScreenRect)
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

    static Vector2 PopupSizeOnScreen(RectTransform popupRect)
    {
        Vector2 size = popupRect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            size = popupRect.sizeDelta;

        // rect.size is in canvas units. Placement math is in screen pixels.
        Canvas canvas = popupRect.GetComponentInParent<Canvas>();
        if (canvas == null)
            return size;

        Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
        if (root.renderMode == RenderMode.WorldSpace)
        {
            Vector3 lossy = popupRect.lossyScale;
            return new Vector2(size.x * Mathf.Abs(lossy.x), size.y * Mathf.Abs(lossy.y));
        }

        float scale = root.scaleFactor;
        if (scale > 0.001f)
            size *= scale;

        return size;
    }

    static Rect GetScreenRect(Transform target, Camera camera)
    {
        if (target == null || camera == null)
            return Rect.zero;

        if (!TryGetAnchorBounds(target, out Bounds bounds))
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(target.position);
            float size = 48f;
            return new Rect(screenPoint.x - size * 0.5f, screenPoint.y - size * 0.5f, size, size);
        }

        return BoundsToScreenRect(bounds, camera);
    }

    // Collider plus drawn meshes. The ritual collider is a tall upright box, while the altar
    // mesh lies flat and the camera looks along the board, so either one alone misses the
    // visible silhouette.
    static bool TryGetAnchorBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        Collider anchorCollider = target.GetComponent<Collider>();
        if (anchorCollider != null && anchorCollider.enabled)
        {
            bounds = anchorCollider.bounds;
            hasBounds = true;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!IsAnchorRenderer(renderer))
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

        return hasBounds;
    }

    static bool IsAnchorRenderer(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            return false;

        return renderer is not ParticleSystemRenderer
            && renderer is not TrailRenderer
            && renderer is not LineRenderer;
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

        float near = Mathf.Max(camera.nearClipPlane, 0.01f);
        bool hasPoint = false;
        float xMin = 0f;
        float xMax = 0f;
        float yMin = 0f;
        float yMax = 0f;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screen = camera.WorldToScreenPoint(corners[i]);
            // A pitched camera puts some of a tall box behind the near plane.
            // Those points mirror across the screen and throw the rect into a corner.
            if (screen.z <= near)
                continue;

            if (!hasPoint)
            {
                xMin = xMax = screen.x;
                yMin = yMax = screen.y;
                hasPoint = true;
                continue;
            }

            xMin = Mathf.Min(xMin, screen.x);
            xMax = Mathf.Max(xMax, screen.x);
            yMin = Mathf.Min(yMin, screen.y);
            yMax = Mathf.Max(yMax, screen.y);
        }

        if (!hasPoint)
        {
            Vector3 center = camera.WorldToScreenPoint(bounds.center);
            float size = 48f;
            return new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    void ShowStaticEffectReminders(PopupSlot slot, Follower follower, Transform anchor, Camera worldCamera, bool besidePopup, string extraReminder = null)
    {
        StaticEffectReminders.Collect(follower, _reminderTextBuffer);
        if (!string.IsNullOrEmpty(extraReminder))
            _reminderTextBuffer.Add(extraReminder);

        PresentReminderBuffer(slot, anchor, worldCamera, besidePopup);
    }

    void ShowReminderText(PopupSlot slot, string text, Transform anchor, Camera worldCamera, bool besidePopup)
    {
        _reminderTextBuffer.Clear();
        if (!string.IsNullOrEmpty(text))
            _reminderTextBuffer.Add(text);

        PresentReminderBuffer(slot, anchor, worldCamera, besidePopup);
    }

    void PresentReminderBuffer(PopupSlot slot, Transform anchor, Camera worldCamera, bool besidePopup)
    {
        RectTransform cardPopup = GetImagePopup(slot);
        if (besidePopup && (cardPopup == null || !cardPopup.gameObject.activeSelf))
        {
            HideStaticEffectReminders(slot);
            return;
        }

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
        return LimitReminderShift(xMin, xMax, yMin, yMax, forbidden, shiftX, vertical: false);
    }

    static float LimitReminderShift(float xMin, float xMax, float yMin, float yMax, Rect forbidden, float shift, bool vertical)
    {
        if (shift == 0f || forbidden.width <= 0f || forbidden.height <= 0f)
            return shift;

        float alongMin = vertical ? yMin : xMin;
        float alongMax = vertical ? yMax : xMax;
        float crossMin = vertical ? xMin : yMin;
        float crossMax = vertical ? xMax : yMax;
        float blockMin = vertical ? forbidden.yMin : forbidden.xMin;
        float blockMax = vertical ? forbidden.yMax : forbidden.xMax;
        float crossBlockMin = vertical ? forbidden.xMin : forbidden.yMin;
        float crossBlockMax = vertical ? forbidden.xMax : forbidden.yMax;

        bool crossOverlap = crossMin < crossBlockMax && crossMax > crossBlockMin;
        if (!crossOverlap)
            return shift;

        if (shift < 0f && alongMin >= blockMax)
        {
            float minShift = blockMax + KeywordReminderPopupGap - alongMin;
            return Mathf.Max(shift, minShift);
        }

        if (shift > 0f && alongMax <= blockMin)
        {
            float maxShift = blockMin - KeywordReminderPopupGap - alongMax;
            return Mathf.Min(shift, maxShift);
        }

        float movedMin = alongMin + shift;
        float movedMax = alongMax + shift;
        if (movedMin < blockMax && movedMax > blockMin)
            return 0f;

        return shift;
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
