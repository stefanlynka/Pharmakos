using UnityEngine;
using UnityEngine.UI;

public class ContentScrollScreenHandler : MonoBehaviour
{
    const string RenderTextureResourcePath = "RenderTextures/DeckViewer";

    public RawImage ContentImage;

    public RectTransform ContentView =>
        ContentImage != null ? ContentImage.rectTransform : transform as RectTransform;

    public void BindRenderTexture()
    {
        if (ContentImage == null)
            ContentImage = GetComponentInChildren<RawImage>(true);

        if (ContentImage == null)
            return;

        RenderTexture texture = Resources.Load<RenderTexture>(RenderTextureResourcePath);
        if (texture != null)
            ContentImage.texture = texture;
    }

    void Awake()
    {
        Screen screen = GetComponent<Screen>();
        if (screen != null)
        {
            screen.InstantEnter = true;
            screen.InstantExit = true;
        }

        EnsureBlockerRaycasts();
        BindRenderTexture();
    }

    void EnsureBlockerRaycasts()
    {
        // Sprite fallbackSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        foreach (Transform child in transform)
        {
            if (child.name != "Blocker")
                continue;

            Image image = child.GetComponent<Image>();
            if (image == null)
                continue;

            image.raycastTarget = true;
            // if (image.sprite == null && fallbackSprite != null)
            //     image.sprite = fallbackSprite;
        }
    }
}
