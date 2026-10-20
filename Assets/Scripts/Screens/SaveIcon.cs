using System.Collections;
using UnityEngine;

public class SaveIcon : MonoBehaviour
{
    public static SaveIcon Instance { get; private set; }

    const float VisibleSeconds = 2f;

    Coroutine _hideRoutine;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Show()
    {
        if (Instance == null)
            Instance = FindObjectOfType<SaveIcon>(true);
        if (Instance != null)
            Instance.ShowInternal();
    }

    void ShowInternal()
    {
        gameObject.SetActive(true);
        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(VisibleSeconds);
        _hideRoutine = null;
        gameObject.SetActive(false);
    }
}
