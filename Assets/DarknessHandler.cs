using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DarknessHandler : MonoBehaviour
{
    public Image Image;

    // 0 no darkness. 1 complete darkness
    public void SetDarkness(float darkness = 1f)
    {
        Image.color = new Color(0, 0, 0, darkness);
    }
}
