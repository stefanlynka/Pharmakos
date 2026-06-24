using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References for a single row/entry on the Styx feature screens (status screen,
/// Styx node, trinket select). Lives on the inactive row templates in the scene;
/// the screen handlers clone and fill them. Unused fields may be left empty.
/// </summary>
public class StatusRowView : MonoBehaviour
{
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI DescriptionText;
    public Image Icon;
    public Button SacrificeButton;

    [Header("Selectable rows (Styx node / trinket select)")]
    public Image Background;
    public Button RowButton;
    public TextMeshProUGUI TagText;
}
