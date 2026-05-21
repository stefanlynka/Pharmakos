using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EventSummaryView : MonoBehaviour
{
    public EventSummaryPosition Position;
    public GameObject Container;
    public TextMeshProUGUI SummaryText;

}
public enum EventSummaryPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}
