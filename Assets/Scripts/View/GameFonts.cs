using TMPro;
using UnityEngine;

/// <summary>
/// The project's two typefaces (see Docs/StyleGuide.md). Use these for any text created
/// from code so it matches the authored UI instead of falling back to TMP's default.
/// Lives at Assets/Resources/GameFonts.asset.
/// </summary>
[CreateAssetMenu(fileName = "GameFonts", menuName = "Hecatomb/Game Fonts")]
public class GameFonts : ScriptableObject
{
    [Tooltip("Titles, buttons, card names, numbers.")]
    public TMP_FontAsset Display;

    [Tooltip("Rules text, descriptions, prose, tooltips.")]
    public TMP_FontAsset Text;

    static GameFonts instance;

    public static GameFonts Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<GameFonts>("GameFonts");
            return instance;
        }
    }

    public static TMP_FontAsset DisplayFont => Instance != null ? Instance.Display : null;
    public static TMP_FontAsset TextFont => Instance != null ? Instance.Text : null;
}
