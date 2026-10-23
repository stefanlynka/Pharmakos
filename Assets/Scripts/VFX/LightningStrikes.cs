using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the yellow lightning bolt from an origin to one or more targets.
/// A multi-target call strikes every target at once, with one thunder clap and one light flash.
/// </summary>
public class LightningStrikes : MonoBehaviour
{
    public static LightningStrikes Instance;

    [SerializeField] GameObject lightningPrefab;
    [SerializeField] Vector3 strikePoint = new Vector3(0f, 10f, 37f);
    [SerializeField] float duration = 0.5f;
    [SerializeField] AudioClip thunderClip;
    [SerializeField] TorchFlickerLight mainLight;
    [SerializeField] float lightFlashExtra = 700f;
    const string ThunderResourcePath = "Audio/SFX/Thunder";

    void Awake()
    {
        Instance = this;
    }

    public void Strike(Vector3 boltOrigin, Vector3 target)
    {
        Strike(boltOrigin, new Vector3[] { target });
    }

    public void Strike(Vector3 boltOrigin, IList<Vector3> targets)
    {
        if (lightningPrefab == null)
        {
            Debug.LogWarning("LightningStrikes has no lightning prefab assigned.");
            return;
        }

        if (targets == null || targets.Count == 0)
            return;

        bool struck = false;
        for (int i = 0; i < targets.Count; i++)
        {
            if (PlayBolt(boltOrigin, targets[i]))
                struck = true;
        }

        if (!struck)
            return;

        PlayThunder();
        if (mainLight != null)
            mainLight.Flash(lightFlashExtra, duration);
    }

    bool PlayBolt(Vector3 boltOrigin, Vector3 target)
    {
        Vector3 alongBolt = boltOrigin - target;
        float distance = alongBolt.magnitude;
        if (distance < 0.01f)
            return false;

        Vector3 direction = alongBolt / distance;
        GameObject instance = Instantiate(lightningPrefab, target, Quaternion.identity);

        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // The visible bolt runs opposite the cone axis, so aim that opposite direction at the origin.
        Vector3 emission = BoltEmissionDirection(systems);
        instance.transform.rotation = Quaternion.FromToRotation(-emission, direction);

        for (int i = 0; i < systems.Length; i++)
            ClampToDuration(systems[i], duration);

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i].gameObject.activeInHierarchy)
                systems[i].Play(false);
        }

        Destroy(instance, duration);
        return true;
    }

    static Vector3 BoltEmissionDirection(ParticleSystem[] systems)
    {
        for (int i = 0; i < systems.Length; i++)
        {
            if (!IsBolt(systems[i])) continue;

            ParticleSystem.ShapeModule shape = systems[i].shape;
            Vector3 emission = Quaternion.Euler(shape.rotation) * Vector3.forward;
            if (emission.sqrMagnitude > 0.0001f)
                return emission.normalized;
        }

        return Vector3.up;
    }

    static bool IsBolt(ParticleSystem ps)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        if (!shape.enabled || shape.shapeType != ParticleSystemShapeType.Cone)
            return false;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        return renderer != null && renderer.renderMode == ParticleSystemRenderMode.Stretch;
    }

    static void ClampToDuration(ParticleSystem ps, float duration)
    {
        ParticleSystem.MainModule main = ps.main;
        if (main.duration > duration)
            main.duration = duration;

        ParticleSystem.MinMaxCurve life = main.startLifetime;
        switch (life.mode)
        {
            case ParticleSystemCurveMode.Constant:
                if (life.constant > duration)
                    main.startLifetime = duration;
                break;
            case ParticleSystemCurveMode.TwoConstants:
                float min = life.constantMin;
                float max = life.constantMax;
                if (min > duration || max > duration)
                    main.startLifetime = new ParticleSystem.MinMaxCurve(Mathf.Min(min, duration), Mathf.Min(max, duration));
                break;
            default:
                if (life.curveMultiplier > duration)
                {
                    life.curveMultiplier = duration;
                    main.startLifetime = life;
                }
                break;
        }
    }

    void PlayThunder()
    {
        AudioClip clip = thunderClip != null ? thunderClip : Resources.Load<AudioClip>(ThunderResourcePath);
        if (clip == null)
        {
            Debug.LogWarning("LightningStrikes could not find the Thunder sound effect.");
            return;
        }

        if (View.Instance != null && View.Instance.AudioHandler != null)
            View.Instance.AudioHandler.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, strikePoint);
    }
}
