using TMPro;
using UnityEngine;

public class ShopScreenHandler : MonoBehaviour
{
    public TextMeshProUGUI ShopInstructionText;
    public TextMeshProUGUI HeartstringsText;

    [Tooltip("Optional — wired at runtime if left empty.")]
    public ShopHandler ShopHandler;

    public void ResetForShop()
    {
        if (ShopHandler == null) ShopHandler = ShopHandler.Instance;
        RefreshHeartstringsDisplay();
    }

    public void RefreshHeartstringsDisplay()
    {
        if (HeartstringsText != null && Controller.Instance != null)
            HeartstringsText.text = Controller.Instance.RunHeartStrings.ToString();

        // if (ShopInstructionText != null) ShopInstructionText.text = "Cards cost 1 heartstring · Trinkets cost 2";
    }

    public void LeaveShop()
    {
        Controller.Instance.StartNextLevel();
    }
}
