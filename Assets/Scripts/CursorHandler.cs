using UnityEngine;

/// <summary>
/// Sets the hardware/software cursor from Resources images.
/// cursor1 while idle; cursor2 while the left mouse button is held.
/// </summary>
public class CursorHandler : MonoBehaviour
{
    public static CursorHandler Instance { get; private set; }

    const string DefaultCursorPath = "Images/UI/cursors/cursor1";
    const string PressedCursorPath = "Images/UI/cursors/cursor2";
    // Longest side after load; Windows software cursors cap at 128x128.
    const int MaxCursorSize = 48;
    // Tip of the arrow in the source art (pixels from top-left).
    static readonly Vector2 SourceHotspot = new Vector2(2f, 1f);

    Texture2D defaultCursor;
    Texture2D pressedCursor;
    Vector2 defaultHotspot;
    Vector2 pressedHotspot;
    Vector2 hotspot;
    bool isPressed;

    /// <summary>Pixel size of the cursor texture currently applied.</summary>
    public Vector2Int ActiveCursorPixelSize { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        defaultCursor = LoadCursorTexture(DefaultCursorPath, out float defaultScale);
        pressedCursor = LoadCursorTexture(PressedCursorPath, out float pressedScale);
        defaultHotspot = SourceHotspot * defaultScale;
        pressedHotspot = SourceHotspot * pressedScale;

        ApplyCursor(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        bool pressed = Input.GetMouseButton(0);
        if (pressed == isPressed)
            return;

        ApplyCursor(pressed);
    }

    void ApplyCursor(bool pressed)
    {
        isPressed = pressed;
        Texture2D texture = pressed ? pressedCursor : defaultCursor;
        hotspot = pressed ? pressedHotspot : defaultHotspot;
        if (texture == null)
            return;

        ActiveCursorPixelSize = new Vector2Int(texture.width, texture.height);
        Cursor.SetCursor(texture, hotspot, CursorMode.ForceSoftware);
    }

    /// <summary>
    /// Screen position of the cursor texture's center. The mouse position is the
    /// hotspot (arrow tip), which sits in the top-left of the cursor image.
    /// </summary>
    public bool TryGetCursorImageCenter(out Vector2 screenPosition)
    {
        screenPosition = Input.mousePosition;
        if (ActiveCursorPixelSize.x <= 0 || ActiveCursorPixelSize.y <= 0)
            return false;

        float centerX = ActiveCursorPixelSize.x * 0.5f;
        float centerY = ActiveCursorPixelSize.y * 0.5f;
        // Hotspot is measured from the texture's top-left, with Y down.
        screenPosition.x += centerX - hotspot.x;
        screenPosition.y += hotspot.y - centerY;
        return true;
    }

    static Texture2D LoadCursorTexture(string resourcePath, out float scale)
    {
        scale = 1f;
        Texture2D source = Resources.Load<Texture2D>(resourcePath);
        if (source == null)
        {
            Debug.LogError("Cursor texture not found at Resources/" + resourcePath);
            return null;
        }

        if (!source.isReadable)
        {
            Debug.LogError("Cursor texture must have Read/Write Enabled: Resources/" + resourcePath);
            return null;
        }

        return CreateCursorTexture(source, MaxCursorSize, out scale);
    }

    /// <summary>
    /// Builds a cursor-ready texture: optional downscale, then linear→gamma so
    /// Cursor.SetCursor displays correctly in a Linear color-space project.
    /// </summary>
    static Texture2D CreateCursorTexture(Texture2D source, int maxSize, out float scale)
    {
        int width = source.width;
        int height = source.height;
        scale = Mathf.Min(1f, (float)maxSize / Mathf.Max(width, height));
        int newWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
        int newHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));

        bool convertToGamma = QualitySettings.activeColorSpace == ColorSpace.Linear;

        Texture2D cursor = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        cursor.filterMode = FilterMode.Bilinear;
        cursor.wrapMode = TextureWrapMode.Clamp;
        cursor.name = source.name + "_Cursor";

        bool sameSize = newWidth == width && newHeight == height;
        if (sameSize)
        {
            Color[] pixels = source.GetPixels();
            if (convertToGamma)
            {
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = pixels[i].gamma;
            }
            cursor.SetPixels(pixels);
        }
        else
        {
            for (int y = 0; y < newHeight; y++)
            {
                float v = (y + 0.5f) / newHeight;
                for (int x = 0; x < newWidth; x++)
                {
                    float u = (x + 0.5f) / newWidth;
                    Color color = source.GetPixelBilinear(u, v);
                    if (convertToGamma)
                        color = color.gamma;
                    cursor.SetPixel(x, y, color);
                }
            }
        }

        cursor.Apply();
        return cursor;
    }
}
