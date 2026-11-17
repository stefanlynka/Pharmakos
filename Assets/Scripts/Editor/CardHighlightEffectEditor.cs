using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(CardHighlightEffect))]
public class CardHighlightEffectEditor : Editor
{
    // Fields that mirror the preset at runtime; hidden while a preset is assigned.
    static readonly HashSet<string> PresetDrivenFields = new HashSet<string>
    {
        "glowCoreColor", "glowOuterColor", "glowSecondaryColor",
        "glowCoreIntensityMultiplier", "glowOuterIntensityMultiplier",
        "glowCoreEmissionStrength", "glowOuterEmissionStrength",
        "glowPulseSpeed", "glowPulseMin", "glowPulseMax",
        "glowMistSpeed", "glowMistScale", "glowFlickerSpeed", "glowFlickerStrength", "glowDrift",
    };

    Editor presetEditor;

    public override void OnInspectorGUI()
    {
        var effect = (CardHighlightEffect)target;
        HighlightPresetInspector.Draw(
            serializedObject,
            PresetDrivenFields.Contains,
            ref presetEditor,
            effect.LivePreset,
            effect.CopyFieldsToSettings);
    }

    void OnDisable()
    {
        if (presetEditor != null)
            DestroyImmediate(presetEditor);
    }
}
