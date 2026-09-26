using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class OfferingHandler : MonoBehaviour
{
    public static OfferingHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public Sprite GetOfferingSprite(OfferingType type)
    {
        Sprite sprite = Resources.Load<Sprite>("Images/Icons/Offerings/Tokens/" + type.ToString());
        if (sprite == null) Debug.LogError("Sprite: " + type.ToString() + " not found in resources");
        
        return sprite;
    }
    public Sprite GetOfferingFrameSprite(OfferingType type)
    {
        Sprite sprite = Resources.Load<Sprite>("Images/Icons/Offerings/Frames/" + type.ToString());
        if (sprite == null) Debug.LogError("Sprite: " + type.ToString() + " not found in resources");
        
        return sprite;
    }

    public static Color GetOfferingColor(OfferingType type)
    {
        switch (type)
        {
            case OfferingType.Gold:
                return new Color(0.95f, 0.75f, 0.22f, 1f);
            case OfferingType.Blood:
                return new Color(0.72f, 0.05f, 0.08f, 1f);
            case OfferingType.Bone:
                return new Color(0.86f, 0.84f, 0.78f, 1f);
            case OfferingType.Crop:
                return new Color(0.28f, 0.58f, 0.2f, 1f);
            case OfferingType.Scroll:
                return new Color(0.32f, 0.52f, 0.75f, 1f);
            default:
                return Color.white;
        }
    }

}


public class Offering
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public OfferingType Type { get; private set; } = OfferingType.Gold;
}

public enum OfferingType
{
    None,
    Gold,
    Blood,
    Bone,
    Crop,
    Scroll
}