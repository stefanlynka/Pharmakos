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
        Sprite sprite = Resources.Load<Sprite>("Images/Offerings/" + type.ToString());
        if (sprite == null) Debug.LogError("Sprite: " + type.ToString() + " not found in resources");
        return sprite;
    }

    public Vector3 GetOfferingPosition(Player player, OfferingType type)
    {
        if (player.IsHuman)
        {
            switch (type)
            {
                case OfferingType.Gold:
                    return new Vector3(-40.2f, -20, 30);
                case OfferingType.Blood:
                    return new Vector3(-30, -18, 30);
                case OfferingType.Bone:
                    return new Vector3(-21.7f, -18, 30);
                case OfferingType.Crop:
                    return new Vector3(-30, -22, 30);
                case OfferingType.Scroll:
                    return new Vector3(-21.7f, -22, 30);
                default:
                    Debug.LogError("Offering type not recognized: " + type.ToString());
                    return new Vector3(0, -11, 30);
            }
        }
        else
        {
            return new Vector3(0, 19, 30);
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
    Gold,
    Blood,
    Bone,
    Crop,
    Scroll
}