using UnityEngine;

public class PlayerLyre : MonoBehaviour
{
    public Material LyreStringMaterial;
    public Material LyreStringHighlightMaterial;
    public GameObject[] LyreStrings = new GameObject[6];

    /// <summary>
    /// Activates strings left to right (first <paramref name="activeCount"/> in <see cref="LyreStrings"/>).
    /// Highlights the rightmost <paramref name="highlightedCount"/> active strings.
    /// </summary>
    public void SetHeartstrings(int activeCount, int highlightedCount)
    {
        if (LyreStrings == null)
            return;

        int count = LyreStrings.Length;
        activeCount = Mathf.Clamp(activeCount, 0, count);
        highlightedCount = Mathf.Clamp(highlightedCount, 0, activeCount);

        for (int i = 0; i < count; i++)
        {
            GameObject str = LyreStrings[i];
            if (str == null)
                continue;

            bool active = i < activeCount;
            str.SetActive(active);
            if (!active)
                continue;

            bool highlighted = i >= activeCount - highlightedCount;
            Material mat = highlighted ? LyreStringHighlightMaterial : LyreStringMaterial;
            if (mat == null)
                continue;

            Renderer renderer = str.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = mat;
        }
    }
}
