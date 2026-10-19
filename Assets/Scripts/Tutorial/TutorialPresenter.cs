using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPresenter : MonoBehaviour
{
    public static TutorialPresenter Instance { get; private set; }
    public static bool IsBlocking { get; private set; }

    const float ArrowBobHeight = 0.02f;
    const float ArrowBobCyclesPerSecond = 0.5f;
    const float FadeDuration = 0.25f;

    [SerializeField] CanvasGroup _group;
    [SerializeField] Image _catcher;
    [SerializeField] Image _background;
    [SerializeField] Image _coverBottom;
    [SerializeField] Image _coverTop;
    [SerializeField] Image _coverLeft;
    [SerializeField] Image _coverRight;
    [SerializeField] TextMeshProUGUI _text;
    [SerializeField] RectTransform _textFrame;
    [SerializeField] Image _spriteTemplate;

    readonly List<Image> _sprites = new List<Image>();
    readonly Queue<TutorialBeat> _queue = new Queue<TutorialBeat>();
    TutorialSpriteCue[] _activeCues = System.Array.Empty<TutorialSpriteCue>();
    Coroutine _routine;
    bool _dismissRequested;
    float _maxTextWidth = -1f;

    public void DismissForOverworldTravel()
    {
        _dismissRequested = true;

        if (_queue.Count == 0)
            return;

        var kept = new Queue<TutorialBeat>();
        while (_queue.Count > 0)
        {
            TutorialBeat beat = _queue.Dequeue();
            if (beat.Trigger != TutorialTrigger.OverworldShown)
                kept.Enqueue(beat);
        }

        while (kept.Count > 0)
            _queue.Enqueue(kept.Dequeue());
    }

    void Update()
    {
        if (_routine != null || IsBlocking)
            return;
        if (Controller.Instance == null || !Controller.Instance.IsTutorialRun)
            return;
        if (TutorialSpotlight.IsPointerOverEndTurn())
            TutorialDirector.Raise(TutorialTrigger.EndTurnHovered);
    }

    public static TutorialPresenter Ensure()
    {
        if (Instance != null)
            return Instance;

        Instance = FindObjectOfType<TutorialPresenter>(true);
        if (Instance == null)
            Debug.LogError("TutorialPresenter: TutorialScreen is missing from the scene.");
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (_spriteTemplate != null)
            _spriteTemplate.gameObject.SetActive(false);
        HideAll();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (IsBlocking)
            IsBlocking = false;
    }

    public void ResetRun()
    {
        _queue.Clear();
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        IsBlocking = false;
        _dismissRequested = false;
        HideAll();
    }

    public void Enqueue(TutorialBeat beat)
    {
        if (beat == null)
            return;

        _queue.Enqueue(beat);
        if (_routine == null)
            _routine = StartCoroutine(PlayQueue());
    }

    IEnumerator PlayQueue()
    {
        while (_queue.Count > 0)
            yield return Play(_queue.Dequeue());

        _routine = null;
    }

    IEnumerator Play(TutorialBeat beat)
    {
        if (beat.Delay > 0f)
        {
            float waited = 0f;
            while (waited < beat.Delay)
            {
                if (_dismissRequested)
                {
                    _dismissRequested = false;
                    yield break;
                }

                waited += Time.deltaTime;
                yield return null;
            }
        }

        if (_dismissRequested)
        {
            _dismissRequested = false;
            yield break;
        }

        if (TutorialSpotlight.TryResolve(beat.Focus, out Rect focused))
            beat.Hole = focused;

        IsBlocking = beat.ShowBlackBlocker;
        if (_group != null)
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = IsBlocking;
        }
        if (_catcher != null)
        {
            _catcher.gameObject.SetActive(IsBlocking);
            _catcher.raycastTarget = IsBlocking;
        }

        ApplyCover(beat);
        ApplyText(beat);
        CreateSprites(beat);

        bool typing = _text != null && !string.IsNullOrEmpty(beat.Text);
        int visible = 0;
        float typeTimer = 0f;
        float elapsed = 0f;
        float shown = 0f;
        bool waitForClick = beat.Duration < 0f;
        float hold = waitForClick ? float.PositiveInfinity : Mathf.Max(0.05f, beat.Duration);
        int textLength = typing ? beat.Text.Length : 0;

        while (true)
        {
            if (_dismissRequested)
            {
                _dismissRequested = false;
                break;
            }

            if (typing && visible < textLength)
            {
                typeTimer += Time.deltaTime;
                float step = Mathf.Max(0.01f, beat.SecondsPerCharacter);
                while (typeTimer >= step && visible < textLength)
                {
                    typeTimer -= step;
                    visible++;
                }

                _text.maxVisibleCharacters = visible;
            }

            bool textShown = !typing || visible >= textLength;
            if (textShown)
                shown += Time.deltaTime;
            if (textShown && (Input.GetMouseButtonDown(0) || shown >= hold))
                break;

            if (_group != null)
                _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, Time.deltaTime / FadeDuration);

            UpdateSprites(beat, elapsed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        while (_group != null && _group.alpha > 0f)
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, Time.deltaTime / FadeDuration);
            UpdateSprites(beat, elapsed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        HideAll();
        IsBlocking = false;
    }

    void ApplyCover(TutorialBeat beat)
    {
        bool show = beat.ShowCover;
        Rect hole = beat.Hole;
        bool hasHole = show && hole.width > 0.001f && hole.height > 0.001f;
        Color color = new Color(0f, 0f, 0f, Mathf.Clamp01(beat.CoverAlpha));

        if (_background != null)
        {
            bool showBlocker = beat.ShowBlackBlocker && !hasHole;
            _background.color = color;
            _background.raycastTarget = showBlocker;
            _background.gameObject.SetActive(showBlocker);
        }

        SetCoverActive(hasHole);
        if (!hasHole)
            return;

        if (_coverBottom != null)
            _coverBottom.color = color;
        if (_coverTop != null)
            _coverTop.color = color;
        if (_coverLeft != null)
            _coverLeft.color = color;
        if (_coverRight != null)
            _coverRight.color = color;

        float xMin = Mathf.Clamp01(hole.x);
        float yMin = Mathf.Clamp01(hole.y);
        float xMax = Mathf.Clamp01(hole.x + hole.width);
        float yMax = Mathf.Clamp01(hole.y + hole.height);

        if (_coverBottom != null)
            Stretch(_coverBottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, yMin));
        if (_coverTop != null)
            Stretch(_coverTop.rectTransform, new Vector2(0f, yMax), new Vector2(1f, 1f));
        if (_coverLeft != null)
            Stretch(_coverLeft.rectTransform, new Vector2(0f, yMin), new Vector2(xMin, yMax));
        if (_coverRight != null)
            Stretch(_coverRight.rectTransform, new Vector2(xMax, yMin), new Vector2(1f, yMax));
    }

    void SetCoverActive(bool active)
    {
        if (_coverBottom != null)
            _coverBottom.gameObject.SetActive(active);
        if (_coverTop != null)
            _coverTop.gameObject.SetActive(active);
        if (_coverLeft != null)
            _coverLeft.gameObject.SetActive(active);
        if (_coverRight != null)
            _coverRight.gameObject.SetActive(active);
    }

    void ApplyText(TutorialBeat beat)
    {
        if (_text == null)
            return;

        bool show = !string.IsNullOrEmpty(beat.Text);
        if (_textFrame != null)
            _textFrame.gameObject.SetActive(show);
        _text.gameObject.SetActive(show);
        if (!show)
            return;

        _text.text = beat.Text;
        _text.maxVisibleCharacters = 0;
        FitTextFrame();
        Place(_textFrame != null ? _textFrame : _text.rectTransform, beat.TextAnchor);
    }

    void FitTextFrame()
    {
        if (_textFrame == null)
            return;

        if (_maxTextWidth < 0f)
            _maxTextWidth = Mathf.Max(1f, _textFrame.sizeDelta.x);

        _text.enableAutoSizing = false;

        float width = Mathf.Min(_text.GetPreferredValues(_text.text).x, _maxTextWidth);
        float height = _text.GetPreferredValues(_text.text, width, 0f).y;
        _textFrame.sizeDelta = new Vector2(Mathf.Ceil(width), Mathf.Ceil(height));
    }

    void CreateSprites(TutorialBeat beat)
    {
        ClearSprites();
        _activeCues = BuildCues(beat);
        if (_activeCues.Length == 0 || _spriteTemplate == null)
            return;

        for (int i = 0; i < _activeCues.Length; i++)
        {
            TutorialSpriteCue cue = _activeCues[i];
            if (cue == null)
                continue;

            Image image = Instantiate(_spriteTemplate, _spriteTemplate.transform.parent);
            image.gameObject.SetActive(true);
            image.name = "TutorialSprite";
            image.raycastTarget = false;
            if (!string.IsNullOrEmpty(cue.ResourcePath))
            {
                Sprite sprite = Resources.Load<Sprite>(cue.ResourcePath);
                if (sprite == null)
                    Debug.LogError("TutorialPresenter: missing sprite at Resources/" + cue.ResourcePath);
                else
                    image.sprite = sprite;
            }

            image.preserveAspect = cue.Fill;
            image.rectTransform.localEulerAngles = new Vector3(0f, 0f, cue.Fill ? 0f : cue.Rotation);
            if (cue.StayOutsideHole)
                cue.From = ArrowRest(image.rectTransform, beat.Hole, cue.Rotation);
            if (cue.Fill)
                Stretch(image.rectTransform, cue.From, cue.To);
            else
                Place(image.rectTransform, cue.From);

            image.transform.SetAsLastSibling();
            _sprites.Add(image);
        }

        if (_textFrame != null)
            _textFrame.SetAsLastSibling();
    }

    static TutorialSpriteCue[] BuildCues(TutorialBeat beat)
    {
        bool point = beat.PointAtHole && beat.Hole.width > 0.01f && beat.Hole.height > 0.01f;
        int spriteCount = beat.Sprites != null ? beat.Sprites.Length : 0;
        if (!point && spriteCount == 0)
            return System.Array.Empty<TutorialSpriteCue>();

        var cues = new TutorialSpriteCue[spriteCount + (point ? 1 : 0)];
        int index = 0;
        if (point)
        {
            bool pointUp = beat.Hole.yMax > 0.72f;
            cues[index++] = new TutorialSpriteCue
            {
                Rotation = pointUp ? 90f : -90f,
                StayOutsideHole = true,
            };
        }

        for (int i = 0; i < spriteCount; i++)
            cues[index++] = beat.Sprites[i];

        return cues;
    }

    void UpdateSprites(TutorialBeat beat, float elapsed)
    {
        if (_activeCues == null)
            return;

        int spriteIndex = 0;
        for (int i = 0; i < _activeCues.Length && spriteIndex < _sprites.Count; i++)
        {
            TutorialSpriteCue cue = _activeCues[i];
            if (cue == null)
                continue;

            Image image = _sprites[spriteIndex];
            spriteIndex++;
            if (image == null)
                continue;

            bool showing = elapsed >= cue.Start && (cue.Duration <= 0f || elapsed <= cue.Start + cue.Duration);
            image.enabled = showing;
            if (!showing || cue.Fill)
                continue;

            float bob = Mathf.Sin(elapsed * ArrowBobCyclesPerSecond * Mathf.PI * 2f) * ArrowBobHeight;
            Place(image.rectTransform, cue.From + new Vector2(0f, bob));
        }
    }

    void HideAll()
    {
        if (_group != null)
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }
        if (_catcher != null)
        {
            _catcher.raycastTarget = false;
            _catcher.gameObject.SetActive(false);
        }

        if (_background != null)
            _background.gameObject.SetActive(false);
        SetCoverActive(false);
        if (_textFrame != null)
            _textFrame.gameObject.SetActive(false);
        if (_text != null)
            _text.gameObject.SetActive(false);
        if (_spriteTemplate != null)
            _spriteTemplate.gameObject.SetActive(false);
        ClearSprites();
    }

    void ClearSprites()
    {
        for (int i = 0; i < _sprites.Count; i++)
        {
            if (_sprites[i] != null)
                Destroy(_sprites[i].gameObject);
        }

        _sprites.Clear();
        _activeCues = System.Array.Empty<TutorialSpriteCue>();
    }

    static Vector2 ArrowRest(RectTransform arrow, Rect hole, float rotation)
    {
        Vector3[] corners = new Vector3[4];
        arrow.GetWorldCorners(corners);
        float halfPixels = Mathf.Max(
            Vector3.Distance(corners[0], corners[1]),
            Vector3.Distance(corners[1], corners[2])) * 0.5f;
        float half = halfPixels / Mathf.Max(1f, UnityEngine.Screen.height);
        const float gap = 0.01f;
        float offset = half + ArrowBobHeight + gap;
        float x = hole.x + hole.width * 0.5f;
        bool pointUp = rotation > 0f;
        float y = pointUp ? hole.yMin - offset : hole.yMax + offset;
        return new Vector2(x, y);
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void Place(RectTransform rt, Vector2 normalized)
    {
        rt.anchorMin = normalized;
        rt.anchorMax = normalized;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }
}
