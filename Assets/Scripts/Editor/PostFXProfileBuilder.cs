using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds the post-processing option profiles in Assets/Settings/PostFX (Tools → Post FX → Build Option Profiles).
/// Each profile is a complete look; assign one to the scene's Global Post Processing Volume to use it. Rebuilding
/// overwrites the PostFX_* assets, so tune them in the inspector afterwards and don't rebuild, or change the numbers
/// here. See Docs/PostProcessing.md.
/// </summary>
public static class PostFXProfileBuilder
{
    const string Folder = "Assets/Settings/PostFX";

    [MenuItem("Tools/Post FX/Build Option Profiles")]
    public static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Settings", "PostFX");

        BuildSubtle();
        BuildGraded();
        BuildCinematic();
        BuildBalanced();
        AssetDatabase.SaveAssets();
        Debug.Log("Post FX option profiles built in " + Folder);
    }

    // A: today's bloom plus a gentle vignette and a little contrast. No tonemapping, so UI colours stay exactly as authored.
    static void BuildSubtle()
    {
        VolumeProfile p = NewProfile("PostFX_A_Subtle");
        Bloom bloom = Add<Bloom>(p);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.5f);
        bloom.scatter.Override(0.72f);
        bloom.highQualityFiltering.Override(true);

        ColorAdjustments adjust = Add<ColorAdjustments>(p);
        adjust.contrast.Override(8f);
        adjust.saturation.Override(4f);

        Vignette vignette = Add<Vignette>(p);
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.5f);
        vignette.color.Override(new Color(0.03f, 0.01f, 0.01f));
    }

    // B: filmic (Neutral) tonemapping so bright spots roll off instead of clipping, exposure lifted to compensate, a
    // warm-highlight / cool-violet-shadow split, vignette and fine grain (which also hides banding in the dark gradients).
    static void BuildGraded()
    {
        VolumeProfile p = NewProfile("PostFX_B_Graded");
        Bloom bloom = Add<Bloom>(p);
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.6f);
        bloom.scatter.Override(0.7f);
        bloom.highQualityFiltering.Override(true);

        Tonemapping tone = Add<Tonemapping>(p);
        tone.mode.Override(TonemappingMode.Neutral);

        ColorAdjustments adjust = Add<ColorAdjustments>(p);
        adjust.postExposure.Override(0.35f);
        adjust.contrast.Override(14f);
        adjust.saturation.Override(6f);

        SplitToning split = Add<SplitToning>(p);
        split.shadows.Override(new Color(0.40f, 0.36f, 0.46f));
        split.highlights.Override(new Color(0.62f, 0.52f, 0.40f));
        split.balance.Override(-15f);

        Vignette vignette = Add<Vignette>(p);
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(new Color(0.04f, 0.0f, 0.01f));

        FilmGrain grain = Add<FilmGrain>(p);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.12f);
        grain.response.Override(0.8f);
    }

    // C: moodier and more "lens-like" than D: heavier vignette, more contrast and grain, a warm bloom tint and a touch of
    // fringing at the edges. (ACES tonemapping was tried here and dropped: its toe crushes this mostly-dark board to
    // black, and lift/gamma/gain colours act as offsets, so even 1.04 casts the whole shadow range blue.)
    static void BuildCinematic()
    {
        VolumeProfile p = NewProfile("PostFX_C_Cinematic");
        Bloom bloom = Add<Bloom>(p);
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.75f);
        bloom.scatter.Override(0.75f);
        bloom.tint.Override(new Color(1f, 0.88f, 0.75f));
        bloom.highQualityFiltering.Override(true);

        Tonemapping tone = Add<Tonemapping>(p);
        tone.mode.Override(TonemappingMode.Neutral);

        ColorAdjustments adjust = Add<ColorAdjustments>(p);
        adjust.postExposure.Override(0.3f);
        adjust.contrast.Override(14f);
        adjust.saturation.Override(8f);

        Vignette vignette = Add<Vignette>(p);
        vignette.intensity.Override(0.36f);
        vignette.smoothness.Override(0.42f);
        vignette.color.Override(new Color(0.05f, 0.0f, 0.0f));

        FilmGrain grain = Add<FilmGrain>(p);
        grain.type.Override(FilmGrainLookup.Medium1);
        grain.intensity.Override(0.16f);
        grain.response.Override(0.75f);

        ChromaticAberration chroma = Add<ChromaticAberration>(p);
        chroma.intensity.Override(0.06f);
    }

    // D (recommended): filmic Neutral tonemapping so glows and lit card faces roll off instead of clipping; exposure and
    // a midtone lift (gamma) bring the board's carved texture up out of the black without lifting true black; low
    // contrast change so card art keeps its authored punch; slightly warm gain; gentle vignette; fine grain.
    static void BuildBalanced()
    {
        VolumeProfile p = NewProfile("PostFX_D_Balanced");
        Bloom bloom = Add<Bloom>(p);
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.55f);
        bloom.scatter.Override(0.72f);
        bloom.highQualityFiltering.Override(true);

        Tonemapping tone = Add<Tonemapping>(p);
        tone.mode.Override(TonemappingMode.Neutral);

        ColorAdjustments adjust = Add<ColorAdjustments>(p);
        adjust.postExposure.Override(0.3f);
        adjust.contrast.Override(6f);
        adjust.saturation.Override(6f);

        LiftGammaGain lgg = Add<LiftGammaGain>(p);
        lgg.gamma.Override(new Vector4(1f, 0.99f, 0.98f, 0.12f));
        lgg.gain.Override(new Vector4(1.02f, 1f, 0.97f, 0f));

        Vignette vignette = Add<Vignette>(p);
        vignette.intensity.Override(0.25f);
        vignette.smoothness.Override(0.5f);
        vignette.color.Override(new Color(0.03f, 0f, 0f));

        FilmGrain grain = Add<FilmGrain>(p);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.1f);
        grain.response.Override(0.8f);
    }

    /// <summary>
    /// Keeps the combat UI (GameCanvas, Screen Space - Camera) out of post-processing, so grading, vignette and grain
    /// never shift its authored colours: adds an Overlay camera "UICamera" under Main Camera that renders only the UI
    /// layer without post, stacks it on Main Camera and points GameCanvas at it. Undoable; save the scene to keep it.
    /// </summary>
    [MenuItem("Tools/Post FX/Add UI Camera (keep combat UI out of post)")]
    public static void AddUICamera()
    {
        Camera main = Camera.main;
        GameObject canvasObject = GameObject.Find("GameCanvas");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;
        if (main == null || canvas == null)
        {
            Debug.LogError("Add UI Camera: needs Main Camera and GameCanvas in the open scene.");
            return;
        }
        if (canvas.worldCamera != null && canvas.worldCamera != main)
        {
            Debug.Log("Add UI Camera: GameCanvas already uses " + canvas.worldCamera.name + ".");
            return;
        }

        GameObject go = new GameObject("UICamera");
        Undo.RegisterCreatedObjectUndo(go, "Add UI Camera");
        go.transform.SetParent(main.transform, false);
        Camera cam = go.AddComponent<Camera>();
        cam.CopyFrom(main);
        cam.cullingMask = 1 << canvas.gameObject.layer;
        cam.clearFlags = CameraClearFlags.Nothing;
        UniversalAdditionalCameraData data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderType = CameraRenderType.Overlay;
        data.renderPostProcessing = false;

        UniversalAdditionalCameraData mainData = main.GetUniversalAdditionalCameraData();
        Undo.RecordObject(mainData, "Add UI Camera");
        mainData.cameraStack.Add(cam);
        Undo.RecordObject(canvas, "Add UI Camera");
        canvas.worldCamera = cam;
        Debug.Log("Added UICamera: GameCanvas now renders after post-processing. Save the scene to keep it.");
    }

    static VolumeProfile NewProfile(string name)
    {
        string path = Folder + "/" + name + ".asset";
        VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (existing != null)
        {
            // clear in place so the asset's GUID (and any Volume pointing at it) survives a rebuild
            foreach (Object sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                Object.DestroyImmediate(sub, true);
            existing.components.Clear();
            EditorUtility.SetDirty(existing);
            return existing;
        }
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        return profile;
    }

    static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = profile.Add<T>(false);
        component.name = typeof(T).Name;
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        return component;
    }
}
