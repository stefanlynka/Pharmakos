using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Paints a damage icon's sprite in with a brushstroke when shown and wipes it out when hidden,
/// revealing the damage number partway through the paint-in. Embed as a serialized field in the owning view.
/// </summary>
[Serializable]
public sealed class DamageIconStroke
{
    const string DefaultMaterialPath = "VFX/Materials/BrushStroke";

    [Tooltip("Material using Pharmakos/BrushStrokeReveal for the damage sprite. Defaults to Resources/" + DefaultMaterialPath + ".")]
    [SerializeField] Material material;
    [SerializeField] float paintInDuration = 0.15f;
    [SerializeField] float wipeOutDuration = 0.2f;
    [Tooltip("Degrees clockwise from up. 0 paints bottom to top, 45 paints bottom-left to top-right.")]
    [SerializeField] float strokeAngle = 45f;
    [Tooltip("Fraction of the paint-in that elapses before the damage number appears.")]
    [SerializeField, Range(0f, 1f)] float textDelay = 0.5f;

    [NonSerialized] BrushStroke stroke;
    [NonSerialized] Coroutine textRoutine;

    public void Show(MonoBehaviour host, GameObject icon, TextMeshPro text, int damage)
    {
        StopTextRoutine(host);
        icon.SetActive(true);
        text.text = damage.ToString();

        BrushStroke brushStroke = GetStroke(icon);
        if (brushStroke == null)
        {
            text.enabled = true;
            return;
        }

        brushStroke.SetStrokeAngle(strokeAngle);
        brushStroke.PaintIn(paintInDuration);

        float delay = paintInDuration * textDelay;
        if (delay > 0f && host.isActiveAndEnabled)
        {
            text.enabled = false;
            textRoutine = host.StartCoroutine(ShowTextAfter(text, delay));
        }
        else
        {
            text.enabled = true;
        }
    }

    public void Hide(MonoBehaviour host, GameObject icon, TextMeshPro text)
    {
        StopTextRoutine(host);
        BrushStroke brushStroke = GetStroke(icon);
        if (brushStroke == null || !icon.activeInHierarchy)
        {
            icon.SetActive(false);
            return;
        }

        text.enabled = false;
        brushStroke.WipeOut(wipeOutDuration, () => icon.SetActive(false));
    }

    public void HideImmediate(MonoBehaviour host, GameObject icon)
    {
        StopTextRoutine(host);
        icon.SetActive(false);
    }

    BrushStroke GetStroke(GameObject icon)
    {
        if (stroke != null)
            return stroke;

        SpriteRenderer spriteRenderer = icon.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
            return null;

        if (material == null)
            material = Resources.Load<Material>(DefaultMaterialPath);
        if (material == null)
            return null;

        spriteRenderer.sharedMaterial = material;
        stroke = spriteRenderer.GetComponent<BrushStroke>();
        if (stroke == null)
            stroke = spriteRenderer.gameObject.AddComponent<BrushStroke>();
        return stroke;
    }

    IEnumerator ShowTextAfter(TextMeshPro text, float delay)
    {
        yield return new WaitForSeconds(delay);
        text.enabled = true;
        textRoutine = null;
    }

    void StopTextRoutine(MonoBehaviour host)
    {
        if (textRoutine != null)
        {
            host.StopCoroutine(textRoutine);
            textRoutine = null;
        }
    }
}
