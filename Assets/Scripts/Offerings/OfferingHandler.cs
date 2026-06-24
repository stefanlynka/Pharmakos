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