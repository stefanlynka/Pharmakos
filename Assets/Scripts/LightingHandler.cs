using UnityEngine;

public class LightingHandler : MonoBehaviour
{
    public Light LeftLight;
    public Light RightLight;
    public Light EyeLight;

    public float maxEyeLightIntensity = 3000;
    public float defaultLightIntensity = 5600;
    private float ritualFadeDuration = 0.2f;
    private float ritualWaitDuration = 1f;

    Sequence LightDimmer = new Sequence();

    private void Start()
    {
        defaultLightIntensity = LeftLight.intensity;
    }

    public void SetAllLights(bool active)
    {
        LeftLight.gameObject.SetActive(active);
        RightLight.gameObject.SetActive(active);
        EyeLight.gameObject.SetActive(active);
    }
    public void DoRitualAnimation(GameObject Target)
    {
        // Position in the tilted light rig's local space so the spotlight stays over the target
        // on the tilted gamefield (world-space XY alone is wrong once TiltContainer rotates the field).
        Transform lightRig = EyeLight.transform.parent;
        Vector3 targetLocal = lightRig.InverseTransformPoint(Target.transform.position);
        Vector3 eyeLocal = EyeLight.transform.localPosition;
        EyeLight.transform.localPosition = new Vector3(targetLocal.x, targetLocal.y, eyeLocal.z);

        Sequence spotlightSequence = new Sequence();
        spotlightSequence.Add(new Tween(SetSpotlightIntensity, 0, maxEyeLightIntensity, ritualFadeDuration));
        spotlightSequence.Add(new Tween(Wait, 0, 0, ritualWaitDuration));
        spotlightSequence.Add(new Tween(SetSpotlightIntensity, maxEyeLightIntensity, 0, ritualFadeDuration));
        spotlightSequence.Start();

        // Control main lights
        Sequence mainLightSequence = new Sequence();
        mainLightSequence.Add(new Tween(SetMainLightIntensity, LeftLight.intensity, 0, ritualFadeDuration));
        mainLightSequence.Add(new SequenceAction(ShakeScreen));
        mainLightSequence.Add(new Tween(Wait, 0, 0, ritualWaitDuration));
        mainLightSequence.Add(new Tween(SetMainLightIntensity, 0, defaultLightIntensity, ritualFadeDuration));
        mainLightSequence.Start();
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
    private void Wait(float duration)
    {

    }
    private void ShakeScreen()
    {
        ScreenShakeHandler.Shake(1f, ritualWaitDuration);
        View.Instance.AudioHandler.PlaySoundEffect(AudioHandler.SoundEffectType.Whoosh);
    }

    public void DimLights()
    {
        SequenceHandler.Instance.TryStopSequence(LightDimmer);

        LightDimmer.Clear();
        LightDimmer.Add(new Tween(SetMainLightIntensity, LeftLight.intensity, defaultLightIntensity/3, ritualFadeDuration*3));
        LightDimmer.Start();
        //mainLightSequence.Add(new SequenceAction(ShakeScreen));
        //mainLightSequence.Add(new Tween(Wait, 0, 0, ritualWaitDuration));
        //mainLightSequence.Add(new Tween(SetMainLightIntensity, 0, defaultLightIntensity/2, ritualFadeDuration));
    }
    public void RestoreLights()
    {
        SequenceHandler.Instance.TryStopSequence(LightDimmer);

        LightDimmer.Clear();
        LightDimmer.Add(new Tween(SetMainLightIntensity, LeftLight.intensity, defaultLightIntensity, ritualFadeDuration*3));
        LightDimmer.Start();
    }
}
