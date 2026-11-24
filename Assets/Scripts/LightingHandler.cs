using UnityEngine;

public class LightingHandler : MonoBehaviour
{
    public Light LeftLight;
    public Light RightLight;
    public Light EyeLight;

    public float maxEyeLightIntensity = 3000;
    public float defaultLightIntensity = 250;
    public float dimLightIntensity = 0;
    private float ritualFadeDuration = 0.2f;
    [Tooltip("Seconds the board stays dark after a ritual's final note before the lights come back.")]
    public float ritualHoldAfterClimax = 1.0f;
    [Tooltip("Seconds for the lights to come back after a ritual.")]
    public float ritualRestoreDuration = 0.5f;
    Coroutine ritualLighting;
    Color eyeLightColor = Color.white;

    Sequence LightDimmer = new Sequence();

    private void Start()
    {
        defaultLightIntensity = LeftLight.intensity;
        eyeLightColor = EyeLight.color;
    }

    public void SetAllLights(bool active)
    {
        LeftLight.gameObject.SetActive(active);
        RightLight.gameObject.SetActive(active);
        EyeLight.gameObject.SetActive(active);
    }
    /// <summary>
    /// The lighting of a ritual invocation (RitualInvocationVfx): the main lights go out, a spotlight in the god's colour
    /// gathers on the target until the final note, flares on it, then everything returns to normal.
    /// A new invocation takes over from one still playing.
    /// </summary>
    public void PlayRitualLighting(GameObject target, Color spotColor, float climax, bool effectMajor = true)
    {
        if (target == null)
            return;

        // Position in the tilted light rig's local space so the spotlight stays over the target
        // on the tilted gamefield (world-space XY alone is wrong once TiltContainer rotates the field).
        Transform lightRig = EyeLight.transform.parent;
        Vector3 targetLocal = lightRig.InverseTransformPoint(target.transform.position);
        Vector3 eyeLocal = EyeLight.transform.localPosition;
        EyeLight.transform.localPosition = new Vector3(targetLocal.x, targetLocal.y, eyeLocal.z);

        SequenceHandler.Instance.TryStopSequence(LightDimmer);
        if (ritualLighting != null)
            StopCoroutine(ritualLighting);
        ritualLighting = StartCoroutine(RitualLightingRoutine(spotColor, Mathf.Max(0.1f, climax), effectMajor));
    }

    System.Collections.IEnumerator RitualLightingRoutine(Color spotColor, float climax, bool major)
    {
        float startMain = LeftLight.intensity;
        float hold = major ? ritualHoldAfterClimax : ritualHoldAfterClimax * 0.75f;
        float end = climax + hold + ritualRestoreDuration;
        EyeLight.color = spotColor;

        for (float t = 0f; t < end; t += Time.deltaTime)
        {
            // main lights: out quickly, back after the hold
            float main = t < climax + hold
                ? Mathf.Lerp(startMain, 0f, Mathf.Clamp01(t / ritualFadeDuration))
                : Mathf.Lerp(0f, defaultLightIntensity, Mathf.SmoothStep(0f, 1f, (t - climax - hold) / ritualRestoreDuration));
            SetMainLightIntensity(main);

            // spotlight: gathers to half strength, flares on the final note, settles, then fades with the restore
            float eye;
            if (t < climax)
                eye = maxEyeLightIntensity * 0.45f * Mathf.SmoothStep(0f, 1f, t / climax);
            else if (t < climax + 0.12f)
                eye = Mathf.Lerp(maxEyeLightIntensity * 0.45f, maxEyeLightIntensity * 1.3f, (t - climax) / 0.12f);
            else if (t < climax + hold)
                eye = Mathf.Lerp(maxEyeLightIntensity * 1.3f, maxEyeLightIntensity * 0.6f, Mathf.Clamp01((t - climax - 0.12f) / 0.5f));
            else
                eye = Mathf.Lerp(maxEyeLightIntensity * 0.6f, 0f, (t - climax - hold) / ritualRestoreDuration);
            SetSpotlightIntensity(eye);
            yield return null;
        }

        SetMainLightIntensity(defaultLightIntensity);
        SetSpotlightIntensity(0f);
        EyeLight.color = eyeLightColor;
        ritualLighting = null;
    }

    private void SetMainLightIntensity(float progress)
    {
        LeftLight.intensity = progress;
        RightLight.intensity = progress;
    }
    private void SetSpotlightIntensity(float progress)
    {
        EyeLight.intensity = progress;
    }

    // A ritual being aimed or cancelled takes the lights over from an invocation still fading out.
    void StopRitualLighting()
    {
        if (ritualLighting == null)
            return;
        StopCoroutine(ritualLighting);
        ritualLighting = null;
        SetSpotlightIntensity(0f);
        EyeLight.color = eyeLightColor;
    }

    public void DimLights()
    {
        StopRitualLighting();
        SequenceHandler.Instance.TryStopSequence(LightDimmer);

        LightDimmer.Clear();
        LightDimmer.Add(new Tween(SetMainLightIntensity, LeftLight.intensity, dimLightIntensity, ritualFadeDuration*3));
        LightDimmer.Start();
    }
    public void RestoreLights()
    {
        StopRitualLighting();
        SequenceHandler.Instance.TryStopSequence(LightDimmer);

        LightDimmer.Clear();
        LightDimmer.Add(new Tween(SetMainLightIntensity, LeftLight.intensity, defaultLightIntensity, ritualFadeDuration*3));
        LightDimmer.Start();
    }
}
