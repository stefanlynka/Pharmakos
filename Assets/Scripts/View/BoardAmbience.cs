using UnityEngine;

/// <summary>
/// Makes the glowing parts of a painted board (embers, torches, candles, the sea's glimmer) breathe: scales the
/// ground material's emission with smooth noise. View.ShowBoard adds it to NewBackgroundHolder/Background and feeds
/// it the BoardStyle's Flicker settings; it writes only a MaterialPropertyBlock, never the material asset.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class BoardAmbience : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    MeshRenderer meshRenderer;
    MaterialPropertyBlock block;
    Color baseEmission;
    float amount;
    float speed;
    float seed;

    public void Configure(Material ground, float flickerAmount, float flickerSpeed)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (block == null)
            block = new MaterialPropertyBlock();

        amount = ground != null && ground.IsKeywordEnabled("_EMISSION") ? flickerAmount : 0f;
        speed = flickerSpeed;
        baseEmission = ground != null && ground.HasProperty(EmissionColorId) ? ground.GetColor(EmissionColorId) : Color.black;
        seed = Random.value * 100f;

        meshRenderer.SetPropertyBlock(null);
        enabled = amount > 0f;
    }

    void Update()
    {
        float t = Time.time * speed;
        // two octaves of noise: a slow breath and a quicker gutter
        float n = Mathf.PerlinNoise(seed, t) * 0.65f + Mathf.PerlinNoise(seed + 37f, t * 3.1f) * 0.35f;
        float scale = 1f + (n - 0.5f) * 2f * amount;
        meshRenderer.GetPropertyBlock(block);
        block.SetColor(EmissionColorId, baseEmission * scale);
        meshRenderer.SetPropertyBlock(block);
    }

    void OnDisable()
    {
        if (meshRenderer != null)
            meshRenderer.SetPropertyBlock(null);
    }
}
