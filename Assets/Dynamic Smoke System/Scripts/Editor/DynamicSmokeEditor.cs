using System;
using UnityEditor;
using UnityEngine;

namespace DynamicSmokeSystem {
    public class DynamicSmokeEditor : ShaderGUI {
        private Material material;
        private MaterialEditor materialEditor;
        private MaterialProperty[] properties;
        private bool isAdvancedFoldedOut;
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
            this.materialEditor = materialEditor;
            this.properties = properties;
            material = materialEditor.target as Material;
            
            EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);
            DrawColor("_Color", "Color");
            DrawColor("_ShadowColor", "Shadow Color");
            
            var cellShadingOn = DrawToggle(
                "CELLSHADING_ON",
                "CELLSHADING_OFF",
                "Use cell shading",
                "Turn on for a toony lighting effect");

            if (cellShadingOn) {
                DrawTexture2D("_CellShadingRamp", "Shadow Ramp");    
            }

            DrawToggle(
                "ALPHA_DITHER_ON",
                "ALPHA_DITHER_OFF",
                "Use alpha dithering",
                "Turn on to use a dithering effect to fade between cloud and background, instead of using true transparency. Using this will also improve interaction with other transparent objects.",
                isOn => {
                    material.renderQueue = isOn ? 2000 : 3000;
                    material.SetFloat("_Zwrite", isOn ? 1 : 0);
                });
            
            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("Consistency", EditorStyles.boldLabel);
            DrawFloat("_Scale", "Consistency Scale");
            DrawVector3MultiLine(
                "_noiseWeights", 
                "Consistency Intensity (Large)",
                "Consistency Intensity (Medium)",
                "Consistency Intensity (Small)",
                new Vector3(20, 20, 20));
            DrawFloatMinMax("_noiseMin", "_noiseMax", "Consistency Modifier");
            DrawFloat("_TimeScale", "Animation Speed");
            DrawFloat("_DepthFade", "See-through Distance");
                
            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("Edge", EditorStyles.boldLabel);
            DrawFloatSlider("_EdgeScale", "Edge Scale", 0, 20);
            DrawFloatSlider("_EdgeIntensity", "Edge Intensity", 0, 20);
            DrawFloatSlider("_EdgeSpeed", "Edge Speed", 0, 20);
            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("Holes", EditorStyles.boldLabel);
            DrawFloatSlider("_OmissionStrength", "Hole Strength", 0f, 1f);
            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel);
            DrawInt("_densityStepCount", "Density Step Count", 1, 500);
            DrawInt("_lightMarchSteps", "Light Step Count", 1, 50);
                
            EditorGUILayout.Separator();

            isAdvancedFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(isAdvancedFoldedOut, "Advanced");

            if (isAdvancedFoldedOut) {
                DrawFloat("lightAbsorptionThroughCloud", "Light Absorption Through Clouds");
                DrawFloat("lightAbsorptionTowardSun", "Light Absorption To Light");
                DrawFloat("darknessThreshold", "Darkness Threshold");
                DrawFloat("baseBrightness", "Brightness");
                DrawFloat("forwardScattering", "Scattering (Forward)");
                DrawFloat("backScattering", "Scattering (Backward)");
                DrawFloat("phaseFactor", "Rim Factor");
                DrawFloat("_DepthBias", "Depth Bias");
                DrawFloat("_NoiseSize", "Noise Size");
                
                DrawTexture3D("_NoiseTex", "Noise (Base)");
                DrawTexture3D("_NoiseDetailTex", "Noise (Detail)");
                DrawTexture2D("_BlueNoise", "Noise (Quality)");
                materialEditor.RenderQueueField();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawColor(string property, string label) {
            var prop = FindProperty(property, properties);
            
            EditorGUI.BeginChangeCheck();

            var value = materialEditor.ColorProperty(prop, label);
            
            if (EditorGUI.EndChangeCheck()) {
                materialEditor.RegisterPropertyChangeUndo(label);
            }
        }

        private bool DrawToggle(string keywordOn, string keywordOff, string label, string tooltip, Action<bool> changeCallback = null) {
            var hasKeyword = material.IsKeywordEnabled(keywordOn);
            
            EditorGUI.BeginChangeCheck();
            
            var value = EditorGUILayout.Toggle(new GUIContent(label, tooltip), hasKeyword);
            
            if (EditorGUI.EndChangeCheck()) {
                materialEditor.RegisterPropertyChangeUndo(label);

                if (value) {
                    material.EnableKeyword(keywordOn);
                    material.DisableKeyword(keywordOff);
                }
                else {
                    material.EnableKeyword(keywordOff);
                    material.DisableKeyword(keywordOn);
                }
                
                changeCallback?.Invoke(value);
            }

            return value;
        }
        
        private void DrawFloat(string property, string label) {
            var prop = FindProperty(property, properties);
            
            EditorGUI.BeginChangeCheck();

            var value = materialEditor.FloatProperty(prop, label);
            
            if (EditorGUI.EndChangeCheck()) {
                materialEditor.RegisterPropertyChangeUndo(label);
            }
        }          
        
        private void DrawFloatMinMax(string propertyMin, string propertyMax, string label) {
            var propMin = FindProperty(propertyMin, properties);
            var propMax = FindProperty(propertyMax, properties);
            var min = propMin.floatValue;
            var max = propMax.floatValue;
            
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.MinMaxSlider(label, ref min, ref max, 0, 1);
            
            if (EditorGUI.EndChangeCheck()) {
                materialEditor.RegisterPropertyChangeUndo(label);
                propMin.floatValue = min;
                propMax.floatValue = max;
            }
        }        
        
        private void DrawInt(string property, string label, int minValue, int maxValue) {
            var prop = FindProperty(property, properties);
            
            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUILayout.IntField(label, (int) prop.floatValue);

            if (EditorGUI.EndChangeCheck()) {
                materialEditor.RegisterPropertyChangeUndo(property);
                prop.floatValue = Mathf.Clamp(newValue, minValue, maxValue);
            }
        }

        private void DrawVector3MultiLine(string property, string labelX, string labelY, string labelZ, Vector3 maxValues) {
            var prop = FindProperty(property, properties);
            var value = material.GetVector(property);
            var changed = false;

            changed = DrawVector3MultiLineInternal(labelX, value.x, maxValues.x, out var newX);
            changed |= DrawVector3MultiLineInternal(labelY, value.y, maxValues.y, out var newY);
            changed |= DrawVector3MultiLineInternal(labelZ, value.z, maxValues.z, out var newZ);

            if (changed) {
                materialEditor.RegisterPropertyChangeUndo(property);
                prop.vectorValue = new Vector4(newX, newY, newZ, 0);
            }
        }

        private bool DrawVector3MultiLineInternal(string label, float value, float maxValue, out float newValue) {
            EditorGUI.BeginChangeCheck();

            newValue = EditorGUILayout.Slider(label, value, 0, maxValue);
            //newValue = EditorGUILayout.FloatField(label, value);

            return EditorGUI.EndChangeCheck();
        }        
        
        private void DrawFloatSlider(string property, string label, float minValue, float maxValue) {
            var prop = FindProperty(property, properties);
            
            EditorGUI.BeginChangeCheck();

            var newValue = EditorGUILayout.Slider(label, prop.floatValue, minValue, maxValue);

            if (EditorGUI.EndChangeCheck()) {
                materialEditor.RegisterPropertyChangeUndo(property);
                prop.floatValue = newValue;
            }
        }
        
        private void DrawTexture3D(string property, string label) {
            var prop = FindProperty(property, properties);
            
            EditorGUI.BeginChangeCheck();

            var value = material.GetTexture(property);
            value = (Texture3D) EditorGUILayout.ObjectField(label, value, typeof(Texture3D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            
            if (EditorGUI.EndChangeCheck()) {
                prop.textureValue = value;
                materialEditor.RegisterPropertyChangeUndo(label);
            }
        }        
        
        private void DrawTexture2D(string property, string label) {
            var prop = FindProperty(property, properties);
            
            EditorGUI.BeginChangeCheck();

            var value = material.GetTexture(property);
            value = (Texture2D) EditorGUILayout.ObjectField(label, value, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            
            if (EditorGUI.EndChangeCheck()) {
                prop.textureValue = value;
                materialEditor.RegisterPropertyChangeUndo(label);
            }
        }
    }
}