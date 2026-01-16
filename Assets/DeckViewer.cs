using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckViewer : MonoBehaviour
{
    public ViewCardScroller MyScroller;
    public RectTransform MyButton;
    private float buttonHiddenZ = 0;
    private float buttonShownZ = 0;

    public void Load(List<Card> cards)
    {
        MyButton.localPosition = new Vector3(MyButton.localPosition.x, MyButton.localPosition.y, buttonShownZ);

        MyScroller.Load(cards);
    }
    public void Exit()
    {
        MyButton.localPosition = new Vector3(MyButton.localPosition.x, MyButton.localPosition.y, buttonHiddenZ);

        MyScroller.Exit();
    }
}
