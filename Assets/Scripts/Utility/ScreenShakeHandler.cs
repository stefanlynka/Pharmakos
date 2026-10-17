using System.Collections;
using UnityEngine;

public class ScreenShakeHandler : MonoBehaviour
{
    private static ScreenShakeHandler instance;
    private Coroutine currentShakeCoroutine;
    private Camera shakeCamera;
    private Vector3 originalCameraPosition;
    private bool hasOriginalPosition;

    private void Awake()
    {
        // Singleton pattern - ensure only one instance exists
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Static function to shake a specific camera with intensity and duration
    public static void Shake(Camera camera, float intensity, float duration)
    {
        if (instance == null)
        {
            Debug.LogWarning("ScreenShakeHandler: No instance found! Make sure ScreenShakeHandler is in the scene.");
            return;
        }

        if (camera == null)
        {
            Debug.LogWarning("ScreenShakeHandler: No camera to shake.");
            return;
        }

        instance.StartShake(camera, intensity, duration);
    }

    // Static function to shake the screen with intensity and duration
    public static void Shake(float intensity, float duration)
    {
        Shake(Camera.main, intensity, duration);
    }

    // Overloaded function with default intensity
    public static void Shake(float duration)
    {
        Shake(1f, duration);
    }

    private void StartShake(Camera camera, float intensity, float duration)
    {
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            RestoreCamera();
        }

        shakeCamera = camera;
        originalCameraPosition = camera.transform.position;
        hasOriginalPosition = true;
        currentShakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (shakeCamera == null)
                break;

            float x = Random.Range(-intensity, intensity);
            float y = Random.Range(-intensity, intensity);
            shakeCamera.transform.position = originalCameraPosition + new Vector3(x, y, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        RestoreCamera();
        currentShakeCoroutine = null;
    }

    void RestoreCamera()
    {
        if (!hasOriginalPosition || shakeCamera == null)
        {
            hasOriginalPosition = false;
            return;
        }

        shakeCamera.transform.position = originalCameraPosition;
        hasOriginalPosition = false;
    }

    // Function to stop current shake immediately
    public static void StopShake()
    {
        if (instance == null)
            return;

        if (instance.currentShakeCoroutine != null)
        {
            instance.StopCoroutine(instance.currentShakeCoroutine);
            instance.currentShakeCoroutine = null;
        }

        instance.RestoreCamera();
    }

    // Function to update original camera position (useful if camera moves during gameplay)
    public static void UpdateOriginalPosition()
    {
        if (instance == null || Camera.main == null)
            return;

        instance.shakeCamera = Camera.main;
        instance.originalCameraPosition = Camera.main.transform.position;
        instance.hasOriginalPosition = true;
    }
}
