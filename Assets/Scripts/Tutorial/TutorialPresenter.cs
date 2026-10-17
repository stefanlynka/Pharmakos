using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPresenter : MonoBehaviour
{
    public static TutorialPresenter Instance { get; private set; }
    public static bool IsBlocking { get; private set; }

    const int CanvasSortOrder = 5000;

    Canvas _canvas;
    Image _catcher;
    Image _coverBottom;
    Image _coverTop;
    Image _coverLeft;
    Image _coverRight;
    TextMeshProUGUI _text;
    readonly List<Image> _sprites = new List<Image>();
    readonly Queue<TutorialBeat> _queue = new Queue<TutorialBeat>();
    Coroutine _routine;

    public static TutorialPresenter Ensure()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("TutorialPresenter");
        if (Controller.Instance != null)
            go.transform.SetParent(Controller.Instance.transform, false);
        return go.AddComponent<TutorialPresenter>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUi();
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
        IsBlocking = beat.BlockInput;
        _catcher.gameObject.SetActive(beat.BlockInput);
        _catcher.raycastTarget = beat.BlockInput;
        ApplyCover(beat);
        ApplyText(beat);
        CreateSprites(beat);

        bool typing = !string.IsNullOrEmpty(beat.Text);
        int visible = 0;
        float typeTimer = 0f;
        float elapsed = 0f;
        float hold = Mathf.Max(0.05f, beat.Duration);
        int textLength = typing ? beat.Text.Length : 0;

        while (elapsed < hold || (typing && visible < textLength))
        {
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

            UpdateSprites(beat, elapsed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        HideAll();
        IsBlocking = false;
    }

    void BuildUi()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CanvasSortOrder;
        _canvas.overrideSorting = true;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        _catcher = CreateImage("InputCatcher", new Color(0f, 0f, 0f, 0f));
        Stretch(_catcher.rectTransform, Vector2.zero, Vector2.one);
        _catcher.raycastTarget = true;

        _coverBottom = CreateImage("CoverBottom", Color.black);
        _coverTop = CreateImage("CoverTop", Color.black);
        _coverLeft = CreateImage("CoverLeft", Color.black);
        _coverRight = CreateImage("CoverRight", Color.black);

        _text = CreateText();
    }

    Image CreateImage(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    TextMeshProUGUI CreateText()
    {
        var go = new GameObject("TutorialText", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = ResolveFont();
        text.fontSize = 36f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        var rt = text.rectTransform;
        rt.sizeDelta = new Vector2(980f, 220f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return text;
    }

    static TMP_FontAsset ResolveFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    void ApplyCover(TutorialBeat beat)
    {
        bool show = beat.ShowCover;
        SetCoverActive(show);
        if (!show)
            return;

        Color color = new Color(0f, 0f, 0f, Mathf.Clamp01(beat.CoverAlpha));
        _coverBottom.color = color;
        _coverTop.color = color;
        _coverLeft.color = color;
        _coverRight.color = color;

        Rect hole = beat.Hole;
        bool hasHole = hole.width > 0.001f && hole.height > 0.001f;
        if (!hasHole)
        {
            Stretch(_coverBottom.rectTransform, Vector2.zero, Vector2.one);
            _coverTop.gameObject.SetActive(false);
            _coverLeft.gameObject.SetActive(false);
            _coverRight.gameObject.SetActive(false);
            return;
        }

        float xMin = Mathf.Clamp01(hole.x);
        float yMin = Mathf.Clamp01(hole.y);
        float xMax = Mathf.Clamp01(hole.x + hole.width);
        float yMax = Mathf.Clamp01(hole.y + hole.height);

        Stretch(_coverBottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, yMin));
        Stretch(_coverTop.rectTransform, new Vector2(0f, yMax), new Vector2(1f, 1f));
        Stretch(_coverLeft.rectTransform, new Vector2(0f, yMin), new Vector2(xMin, yMax));
        Stretch(_coverRight.rectTransform, new Vector2(xMax, yMin), new Vector2(1f, yMax));
        _coverTop.gameObject.SetActive(true);
        _coverLeft.gameObject.SetActive(true);
        _coverRight.gameObject.SetActive(true);
    }

    void SetCoverActive(bool active)
    {
        _coverBottom.gameObject.SetActive(active);
        _coverTop.gameObject.SetActive(active);
        _coverLeft.gameObject.SetActive(active);
        _coverRight.gameObject.SetActive(active);
    }

    void ApplyText(TutorialBeat beat)
    {
        bool show = !string.IsNullOrEmpty(beat.Text);
        _text.gameObject.SetActive(show);
        if (!show)
            return;

        _text.text = beat.Text;
        _text.maxVisibleCharacters = 0;
        Place(_text.rectTransform, beat.TextAnchor);
    }

    void CreateSprites(TutorialBeat beat)
    {
        ClearSprites();
        if (beat.Sprites == null)
            return;

        for (int i = 0; i < beat.Sprites.Length; i++)
        {
            TutorialSpriteCue cue = beat.Sprites[i];
            if (cue == null)
                continue;

            string path = string.IsNullOrEmpty(cue.ResourcePath) ? "Images/Tutorial/Arrow" : cue.ResourcePath;
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError("TutorialPresenter: missing sprite at Resources/" + path);
                _sprites.Add(null);
                continue;
            }

            Image image = CreateImage("TutorialSprite", Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = new Vector2(140f, 140f);
            image.rectTransform.localEulerAngles = new Vector3(0f, 0f, cue.Rotation);
            Place(image.rectTransform, cue.From);
            _sprites.Add(image);
        }
    }

    void UpdateSprites(TutorialBeat beat, float elapsed)
    {
        if (beat.Sprites == null)
            return;

        int spriteIndex = 0;
        for (int i = 0; i < beat.Sprites.Length && spriteIndex < _sprites.Count; i++)
        {
            TutorialSpriteCue cue = beat.Sprites[i];
            if (cue == null)
                continue;

            Image image = _sprites[spriteIndex];
            spriteIndex++;
            if (image == null)
                continue;

            float span = cue.Duration > 0f ? cue.Duration : beat.Duration;
            if (cue.Duration > 0f && elapsed > cue.Duration)
            {
                image.enabled = false;
                continue;
            }

            image.enabled = true;
            float t = span <= 0.001f ? 1f : Mathf.Clamp01(elapsed / span);
            Place(image.rectTransform, Vector2.Lerp(cue.From, cue.To, t));
        }
    }

    void HideAll()
    {
        if (_catcher != null)
        {
            _catcher.raycastTarget = false;
            _catcher.gameObject.SetActive(false);
        }

        SetCoverActive(false);
        if (_text != null)
            _text.gameObject.SetActive(false);
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
