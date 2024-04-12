using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        Sprite sprite = Resources.Load<Sprite>("Images/Offerings/" + type.ToString());
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
    Gold,
    Blood,
    Bone,
    Crop,
    Scroll
}