using TMPro;
using UnityEngine;

/// <summary>
/// Bends a TextMeshPro label along a circular arc, like the lettering on a coin. The text's baseline follows a
/// circle of <see cref="Radius"/> whose centre is <see cref="Radius"/> below the text's pivot, so with Radius &gt; 0
/// the word arches over the top; negative Radius curves it the other way. Use centre/baseline alignment.
/// The text itself (content, font, size, colour) stays editable on the TextMeshPro component as usual.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class CurvedText : MonoBehaviour
{
    [Tooltip("Radius of the baseline arc, in the text's local units. Larger = flatter.")]
    public float Radius = 5f;

    TMP_Text text;

    void OnEnable()
    {
        text = GetComponent<TMP_Text>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        text.havePropertiesChanged = true;
        text.ForceMeshUpdate();
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        if (text != null)
        {
            text.havePropertiesChanged = true;
            text.ForceMeshUpdate();
        }
    }

    void OnValidate()
    {
        if (text != null && isActiveAndEnabled)
        {
            text.havePropertiesChanged = true;
            text.ForceMeshUpdate();
        }
    }

    // TextMeshPro rebuilt its mesh (any text/style change): bend the fresh vertices once.
    void OnTextChanged(Object changed)
    {
        if (changed == text)
            Warp();
    }

    void Warp()
    {
        if (Mathf.Abs(Radius) < 0.0001f)
            return;

        TMP_TextInfo info = text.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible)
                continue;

            Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
            int v = character.vertexIndex;

            // Each glyph pivots around the middle of its baseline, then sits on the arc at the angle its x maps to.
            float midX = (vertices[v].x + vertices[v + 2].x) * 0.5f;
            float baseline = character.baseLine;
            float angle = midX / Radius;
            Quaternion rotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            Vector3 onArc = new Vector3(Radius * Mathf.Sin(angle), baseline + Radius * Mathf.Cos(angle) - Radius, 0f);

            for (int j = 0; j < 4; j++)
            {
                Vector3 local = vertices[v + j] - new Vector3(midX, baseline, 0f);
                vertices[v + j] = rotation * local + onArc;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
