using UnityEngine;

/// <summary>Shows the invisible click-catcher only while <see cref="InputGuard"/> is blocking.</summary>
public class InputGuardBlocker : MonoBehaviour
{
    public GameObject Catcher;

    void LateUpdate()
    {
        bool blocked = InputGuard.IsBlocked;
        if (Catcher != null && Catcher.activeSelf != blocked)
            Catcher.SetActive(blocked);
    }
}
