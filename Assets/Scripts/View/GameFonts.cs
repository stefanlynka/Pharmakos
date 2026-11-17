using System.Text.RegularExpressions;
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

    static readonly Regex LeadingBoldTitle = new Regex(@"^\s*<b>(.*?)</b>", RegexOptions.Singleline);

    /// <summary>
    /// Wraps text in a rich-text tag that sets it in the Display face (Cinzel) inside a text set in Alegreya Sans.
    /// </summary>
    public static string InDisplayFont(string text)
    {
        TMP_FontAsset display = DisplayFont;
        if (display == null || string.IsNullOrEmpty(text))
            return text;

        // <font="..."> finds fonts by name through TMP's reference manager; register ours so it never
        // needs a Resources lookup (the font assets live outside Resources).
        MaterialReferenceManager.AddFontAsset(display);
        return $"<font=\"{display.name}\">{text}</font>";
    }

    /// <summary>
    /// Popup prose: a leading "&lt;b&gt;Title&lt;/b&gt;" (keyword reminders) becomes a Cinzel title; the rest stays in the text face.
    /// </summary>
    public static string WithDisplayTitle(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        Match match = LeadingBoldTitle.Match(text);
        if (!match.Success)
            return text;

        return InDisplayFont(match.Groups[1].Value) + text.Substring(match.Length);
    }
}
