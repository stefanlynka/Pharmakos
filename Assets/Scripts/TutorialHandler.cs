using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialHandler : MonoBehaviour
{
    public List<GameObject> Pages = new List<GameObject>();
    private int currentPage = 0;

    public void Load()
    {
        currentPage = 0;

        foreach (GameObject page in Pages)
        {
            page.SetActive(false);
        }
        Pages[0].SetActive(true);
    }

    public void Next()
    {
        if (currentPage < Pages.Count - 1)
        {
            Pages[currentPage].SetActive(false);
            currentPage++;
            Pages[currentPage].SetActive(true);
        }
        else
        {
            Controller.Instance.HideTutorial();
        }
    }
    public void Previous()
    {
        if (currentPage > 0)
        {
            Pages[currentPage].SetActive(false);
            currentPage--;
            Pages[currentPage].SetActive(true);
        }
        else
        {
            Controller.Instance.HideTutorial();
        }
    }
}
