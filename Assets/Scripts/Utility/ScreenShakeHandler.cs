using System.Collections;
using UnityEngine;

public class ScreenShakeHandler : MonoBehaviour
{
    private static ScreenShakeHandler instance;
    private Coroutine currentShakeCoroutine;
    private Vector3 originalCameraPosition;

    private void Awake()
    {
        // Singleton pattern - ensure only one instance exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Static function to shake the screen with intensity and duration
    public static void Shake(float intensity, float duration)
    {
        if (instance == null)
        {
            Debug.LogWarning("ScreenShakeHandler: No instance found! Make sure ScreenShakeHandler is in the scene.");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning("ScreenShakeHandler: Main camera not found!");
            return;
        }

        instance.StartShake(intensity, duration);
    }

    // Overloaded function with default intensity
    public static void Shake(float duration)
    {
        Shake(1f, duration);
    }

    private void StartShake(float intensity, float duration)
    {
        // Stop any existing shake
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
        }

        // Store original camera position if not already stored
        if (originalCameraPosition == Vector3.zero)
        {
            originalCameraPosition = Camera.main.transform.position;
        }

        // Start new shake
        currentShakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // Generate random offset within intensity range
            float x = Random.Range(-intensity, intensity);
            float y = Random.Range(-intensity, intensity);

            // Apply shake offset to camera position
            Camera.main.transform.position = originalCameraPosition + new Vector3(x, y, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset camera to original position
        Camera.main.transform.position = originalCameraPosition;
        currentShakeCoroutine = null;
    }

    // Function to stop current shake immediately
    public static void StopShake()
    {
        if (instance != null && instance.currentShakeCoroutine != null)
        {
            instance.StopCoroutine(instance.currentShakeCoroutine);
            if (Camera.main != null)
            {
                Camera.main.transform.position = instance.originalCameraPosition;
            }
            instance.currentShakeCoroutine = null;
        }
    }

    // Function to update original camera position (useful if camera moves during gameplay)
    public static void UpdateOriginalPosition()
    {
        if (Camera.main != null)
        {
            instance.originalCameraPosition = Camera.main.transform.position;
        }
    }
}
