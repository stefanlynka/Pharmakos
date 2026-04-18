using UnityEngine;

public class OverworldLightController : MonoBehaviour
{
    public Light MainLight;
    public GameObject LeftLightGameObject;
    public Light LeftLight;
    public GameObject RightLightGameObject;
    public Light RightLight;
    public Light CenterLight;
    public Light EndLight;

    public void SetAllLightsEnabled(bool enabled)
    {
        SetLight(MainLight, enabled);
        SetLight(LeftLight, enabled);
        SetLight(RightLight, enabled);
        SetLight(CenterLight, enabled);
        SetLight(EndLight, enabled);
    }

    private static void SetLight(Light light, bool enabled)
    {
        if (light != null)
            light.enabled = enabled;
    }
}
