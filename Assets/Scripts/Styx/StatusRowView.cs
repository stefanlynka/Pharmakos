using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// References for a single ritual/trinket row on the status screen. Lives on the
/// inactive row templates in the scene; StatusScreenHandler clones and fills them.
/// </summary>
public class StatusRowView : MonoBehaviour
{
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI DescriptionText;
    public Image Icon;
    public Button SacrificeButton;
}
