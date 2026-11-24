using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets up the ritual invocation VFX content: the two glow materials, the import settings of Images/RitualFX, and one
/// RitualTheme per god with its starting look. Existing themes are never overwritten, so hand-tuning in the inspector is
/// safe; delete a theme asset to get its starting values back.
/// </summary>
public static class RitualThemeBuilder
{
    const string ThemeFolder = "Assets/Resources/Content/RitualThemes";
    const string MaterialFolder = "Assets/Resources/VFX/Materials";
    const string TextureFolder = "Assets/Resources/Images/RitualFX";

    [MenuItem("Tools/Rituals/Create Missing Ritual Themes")]
    public static void CreateMissing()
    {
        ConfigureTextures();
        EnsureMaterial("RitualGlowAdd", UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One);
        EnsureMaterial("RitualGlowAlpha", UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        Directory.CreateDirectory(ThemeFolder);

        int created = 0;
        foreach (string god in new[] { "Default", "Zeus", "Poseidon", "Hades", "Ares", "Athena", "Apollo", "Aphrodite", "Demeter",
                     "Dionysus", "Hephaestus", "Hera", "Hermes", "Hestia", "Muse", "OldOnes" })
        {
            string path = ThemeFolder + "/" + god + ".asset";
            if (AssetDatabase.LoadAssetAtPath<RitualTheme>(path) != null)
                continue;
            RitualTheme theme = ScriptableObject.CreateInstance<RitualTheme>();
            theme.Sigil = Tex("Sigils/" + god);
            Define(god, theme);
            AssetDatabase.CreateAsset(theme, path);
            created++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("RitualThemeBuilder: created " + created + " ritual theme(s) in " + ThemeFolder);
    }

    static void ConfigureTextures()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            bool changed = importer.textureType != TextureImporterType.Default || importer.wrapMode != TextureWrapMode.Clamp
                           || !importer.alphaIsTransparency || !importer.mipmapEnabled;
            if (!changed)
                continue;
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
    }

    static void EnsureMaterial(string name, UnityEngine.Rendering.BlendMode src, UnityEngine.Rendering.BlendMode dst)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            return;
        Material m = new Material(Shader.Find("Pharmakos/RitualGlow"));
        m.SetFloat("_SrcBlend", (float)src);
        m.SetFloat("_DstBlend", (float)dst);
        m.SetTexture("_MainTex", Tex("Glow"));
        AssetDatabase.CreateAsset(m, path);
    }

    static Texture2D Tex(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + name + ".png");
    }

    static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

    static RitualParticleLayer Layer(string texture, bool additive, RitualParticleShape shape, int count,
        Vector2 speed, Vector2 life, Vector2 size, Color a, Color b, float gravity = 0f, float drag = 2f,
        float spin = 90f, bool stretch = false, float duration = 0f, float delay = 0f, float intensity = 1.5f)
    {
        return new RitualParticleLayer
        {
            Texture = Tex(texture), Additive = additive, Shape = shape, Count = count, Speed = speed, Lifetime = life,
            Size = size, ColorA = a, ColorB = b, Gravity = gravity, Drag = drag, Spin = new Vector2(-spin, spin),
            Stretch = stretch, Duration = duration, Delay = delay, Intensity = intensity,
        };
    }

    static RitualParticleLayer Off() => new RitualParticleLayer { Enabled = false };

    static Vector2 V(float a, float b) => new Vector2(a, b);

    // Starting looks. Colours follow the altar styles (Docs/AltarKit.md) pushed toward light; no cyan or neon.
    static void Define(string god, RitualTheme t)
    {
        // Rising embers in the god's colour: the shared tail most gods use.
        RitualParticleLayer Embers(Color c, int count = 22) =>
            Layer("Ember", true, RitualParticleShape.Rise, count, V(1.5f, 5f), V(1.2f, 2.2f), V(0.5f, 1.2f), c, Color.Lerp(c, Color.white, 0.4f),
                gravity: -0.12f, drag: 1f, spin: 30f, duration: 1.1f, intensity: 2f);

        switch (god)
        {
            case "Zeus":
                t.Glow = C(1f, 0.86f, 0.55f); t.Accent = C(0.86f, 0.9f, 1f); t.GlowIntensity = 2.6f;
                t.Burst = Layer("Mote", true, RitualParticleShape.Radial, 70, V(18f, 40f), V(0.25f, 0.6f), V(0.4f, 1f),
                    C(0.9f, 0.93f, 1f), C(1f, 0.85f, 0.5f), drag: 3f, stretch: true, intensity: 3f);
                t.Drift = Embers(C(1f, 0.82f, 0.45f));
                t.Extra = Off();
                t.ShakeStrength = 1.3f; t.LightningStrike = true;
                break;
            case "Poseidon":
                t.Glow = C(0.36f, 0.78f, 0.72f); t.Accent = C(0.85f, 1f, 0.95f);
                t.Burst = Layer("Droplet", false, RitualParticleShape.Radial, 70, V(10f, 26f), V(0.6f, 1.1f), V(0.6f, 1.4f),
                    C(0.35f, 0.7f, 0.72f, 0.9f), C(0.75f, 0.92f, 0.9f, 0.9f), gravity: 2.2f, drag: 1.5f, spin: 0f, stretch: true);
                t.Drift = Layer("Glow", true, RitualParticleShape.Rise, 24, V(1.5f, 4f), V(1f, 2f), V(0.6f, 1.6f),
                    C(0.3f, 0.7f, 0.66f), C(0.6f, 0.9f, 0.85f), gravity: -0.1f, duration: 1.2f, intensity: 1.8f);
                t.Extra = Off();
                t.ShakeStrength = 1.2f;
                break;
            case "Hades":
                t.Glow = C(0.84f, 0.8f, 0.7f); t.Accent = C(0.95f, 0.92f, 0.85f); t.GlowIntensity = 1.9f; t.SpotlightTint = 0.4f;
                t.Burst = Layer("Smoke", false, RitualParticleShape.Sphere, 30, V(7f, 16f), V(1.2f, 2f), V(3.5f, 7f),
                    C(0.05f, 0.04f, 0.04f, 0.85f), C(0.12f, 0.1f, 0.09f, 0.75f), drag: 2.5f, spin: 40f);
                t.Drift = Layer("Ember", true, RitualParticleShape.Rise, 26, V(1f, 3.5f), V(1.4f, 2.4f), V(0.5f, 1.1f),
                    C(0.82f, 0.8f, 0.72f), C(0.6f, 0.62f, 0.55f), gravity: -0.08f, drag: 1f, spin: 20f, duration: 1.4f, intensity: 1.6f);
                t.Extra = Layer("Shard", true, RitualParticleShape.Radial, 18, V(8f, 18f), V(0.5f, 1f), V(0.5f, 1.1f),
                    C(0.9f, 0.86f, 0.76f), C(0.7f, 0.66f, 0.58f), gravity: 1.5f, drag: 1.5f, spin: 360f, intensity: 1.4f);
                t.ShakeStrength = 1.2f;
                break;
            case "Ares":
                t.Glow = C(0.95f, 0.3f, 0.14f); t.Accent = C(1f, 0.75f, 0.45f);
                t.Burst = Layer("Ember", true, RitualParticleShape.Radial, 80, V(18f, 38f), V(0.3f, 0.7f), V(0.35f, 0.9f),
                    C(1f, 0.35f, 0.12f), C(1f, 0.7f, 0.35f), gravity: 1f, drag: 2.5f, stretch: true, intensity: 3f);
                t.Drift = Embers(C(0.95f, 0.35f, 0.15f));
                t.Extra = Layer("Shard", false, RitualParticleShape.Radial, 20, V(10f, 22f), V(0.5f, 1f), V(0.6f, 1.3f),
                    C(0.55f, 0.38f, 0.22f), C(0.3f, 0.2f, 0.14f), gravity: 2.5f, drag: 1f, spin: 400f);
                t.ShakeStrength = 1.5f;
                break;
            case "Athena":
                t.Glow = C(0.84f, 0.9f, 0.8f); t.Accent = C(1f, 1f, 0.95f); t.GlowIntensity = 2.2f;
                t.Burst = Layer("Leaf", false, RitualParticleShape.Radial, 40, V(8f, 18f), V(0.8f, 1.4f), V(0.8f, 1.5f),
                    C(0.5f, 0.6f, 0.34f, 0.95f), C(0.72f, 0.78f, 0.55f, 0.95f), gravity: 0.4f, drag: 2.5f, spin: 220f);
                t.Drift = Layer("Mote", true, RitualParticleShape.Rise, 24, V(1.5f, 4f), V(1f, 2f), V(0.4f, 1f),
                    C(0.85f, 0.92f, 0.85f), C(1f, 1f, 0.92f), gravity: -0.08f, duration: 1.2f, intensity: 2.2f);
                t.Extra = Layer("Feather", false, RitualParticleShape.Fall, 10, V(0.5f, 2f), V(1.4f, 2.2f), V(1.2f, 2f),
                    C(0.78f, 0.74f, 0.66f, 0.95f), C(0.6f, 0.56f, 0.5f, 0.95f), gravity: 0.15f, drag: 1.5f, spin: 120f, duration: 0.5f);
                break;
            case "Apollo":
                t.Glow = C(1f, 0.8f, 0.34f); t.Accent = C(1f, 0.96f, 0.78f); t.GlowIntensity = 2.6f;
                t.Burst = Layer("Mote", true, RitualParticleShape.Sphere, 60, V(10f, 24f), V(0.5f, 1.1f), V(0.6f, 1.4f),
                    C(1f, 0.85f, 0.45f), C(1f, 0.96f, 0.8f), drag: 2.5f, intensity: 3f);
                t.Drift = Layer("Glow", true, RitualParticleShape.Rise, 26, V(1.5f, 4.5f), V(1.2f, 2.2f), V(0.7f, 1.6f),
                    C(1f, 0.8f, 0.4f), C(1f, 0.92f, 0.65f), gravity: -0.12f, duration: 1.2f, intensity: 2f);
                t.Extra = Off();
                break;
            case "Aphrodite":
                t.Glow = C(0.95f, 0.4f, 0.46f); t.Accent = C(1f, 0.86f, 0.86f);
                t.Burst = Layer("Petal", false, RitualParticleShape.Radial, 46, V(7f, 18f), V(0.9f, 1.6f), V(0.9f, 1.7f),
                    C(0.62f, 0.06f, 0.1f, 0.95f), C(0.92f, 0.36f, 0.44f, 0.95f), gravity: 0.3f, drag: 2.5f, spin: 240f);
                t.Drift = Layer("Petal", false, RitualParticleShape.Fall, 22, V(0.5f, 2f), V(1.6f, 2.4f), V(0.8f, 1.4f),
                    C(0.7f, 0.1f, 0.14f, 0.95f), C(0.95f, 0.5f, 0.56f, 0.95f), gravity: 0.2f, drag: 1.5f, spin: 200f, duration: 1.2f);
                t.Extra = Off();
                break;
            case "Demeter":
                t.Glow = C(0.94f, 0.82f, 0.44f); t.Accent = C(0.8f, 0.95f, 0.6f);
                t.Burst = Layer("Leaf", false, RitualParticleShape.Radial, 44, V(8f, 18f), V(0.8f, 1.5f), V(0.7f, 1.4f),
                    C(0.35f, 0.55f, 0.22f, 0.95f), C(0.82f, 0.7f, 0.3f, 0.95f), gravity: 0.35f, drag: 2.5f, spin: 220f);
                t.Drift = Layer("Glow", true, RitualParticleShape.Rise, 28, V(1f, 3.5f), V(1.4f, 2.4f), V(0.4f, 1f),
                    C(0.95f, 0.85f, 0.45f), C(0.8f, 0.95f, 0.55f), gravity: -0.06f, duration: 1.4f, intensity: 2f);
                t.Extra = Off();
                break;
            case "Dionysus":
                t.Glow = C(0.8f, 0.3f, 0.56f); t.Accent = C(0.8f, 0.88f, 0.42f);
                t.Burst = Layer("Droplet", false, RitualParticleShape.Radial, 60, V(10f, 24f), V(0.6f, 1.1f), V(0.6f, 1.3f),
                    C(0.4f, 0.04f, 0.16f, 0.95f), C(0.6f, 0.1f, 0.3f, 0.95f), gravity: 2.2f, drag: 1.5f, spin: 0f, stretch: true);
                t.Drift = Layer("Mote", true, RitualParticleShape.Rise, 22, V(1.5f, 4f), V(1f, 2f), V(0.4f, 1f),
                    C(0.85f, 0.4f, 0.65f), C(0.8f, 0.85f, 0.45f), gravity: -0.08f, duration: 1.1f, intensity: 2f);
                t.Extra = Layer("Leaf", false, RitualParticleShape.Radial, 18, V(6f, 14f), V(0.9f, 1.4f), V(0.9f, 1.5f),
                    C(0.4f, 0.52f, 0.2f, 0.95f), C(0.62f, 0.66f, 0.3f, 0.95f), gravity: 0.4f, drag: 2.5f, spin: 200f);
                break;
            case "Hephaestus":
                t.Glow = C(1f, 0.52f, 0.16f); t.Accent = C(1f, 0.9f, 0.6f); t.GlowIntensity = 2.6f;
                t.Burst = Layer("Ember", true, RitualParticleShape.Rise, 100, V(16f, 34f), V(0.5f, 1.1f), V(0.3f, 0.7f),
                    C(1f, 0.55f, 0.15f), C(1f, 0.85f, 0.45f), gravity: 3f, drag: 1f, stretch: true, intensity: 3.2f);
                t.Drift = Embers(C(1f, 0.5f, 0.15f), 26);
                t.Extra = Layer("Smoke", false, RitualParticleShape.Rise, 10, V(2f, 5f), V(1.2f, 2f), V(4f, 7f),
                    C(0.08f, 0.07f, 0.06f, 0.6f), C(0.15f, 0.13f, 0.12f, 0.5f), gravity: -0.1f, drag: 1.5f, spin: 30f);
                t.ShakeStrength = 1.3f;
                break;
            case "Hera":
                t.Glow = C(0.55f, 0.58f, 1f); t.Accent = C(1f, 0.84f, 0.45f);
                t.Burst = Layer("Feather", false, RitualParticleShape.Radial, 36, V(8f, 18f), V(0.9f, 1.5f), V(1.2f, 2.2f),
                    C(0.22f, 0.36f, 0.7f, 0.95f), C(0.85f, 0.68f, 0.32f, 0.95f), gravity: 0.3f, drag: 2.5f, spin: 200f);
                t.Drift = Layer("Mote", true, RitualParticleShape.Rise, 24, V(1.5f, 4f), V(1f, 2f), V(0.5f, 1.1f),
                    C(1f, 0.84f, 0.45f), C(0.6f, 0.62f, 1f), gravity: -0.08f, duration: 1.2f, intensity: 2.2f);
                t.Extra = Off();
                break;
            case "Hermes":
                t.Glow = C(0.86f, 0.9f, 1f); t.Accent = C(1f, 0.9f, 0.62f);
                t.Burst = Layer("Feather", false, RitualParticleShape.Radial, 34, V(12f, 28f), V(0.7f, 1.2f), V(1f, 1.8f),
                    C(0.95f, 0.93f, 0.86f, 0.95f), C(0.8f, 0.82f, 0.86f, 0.95f), gravity: 0.25f, drag: 3f, spin: 260f);
                t.Drift = Layer("Mote", true, RitualParticleShape.Rise, 22, V(2f, 6f), V(0.8f, 1.6f), V(0.4f, 0.9f),
                    C(1f, 0.92f, 0.65f), C(0.9f, 0.95f, 1f), gravity: -0.1f, duration: 1f, intensity: 2.2f);
                t.Extra = Off();
                break;
            case "Hestia":
                t.Glow = C(1f, 0.62f, 0.26f); t.Accent = C(1f, 0.88f, 0.62f);
                t.Burst = Layer("Ember", true, RitualParticleShape.Rise, 60, V(8f, 20f), V(0.6f, 1.2f), V(0.4f, 1f),
                    C(1f, 0.55f, 0.2f), C(1f, 0.82f, 0.45f), gravity: 0.6f, drag: 2f, intensity: 2.6f);
                t.Drift = Embers(C(1f, 0.6f, 0.25f), 28);
                t.Extra = Off();
                break;
            case "Muse":
                t.Glow = C(0.96f, 0.9f, 0.78f); t.Accent = C(1f, 0.86f, 0.56f); t.GlowIntensity = 2.1f;
                t.Burst = Layer("Mote", true, RitualParticleShape.Sphere, 44, V(8f, 18f), V(0.5f, 1.1f), V(0.5f, 1.2f),
                    C(1f, 0.92f, 0.78f), C(1f, 0.82f, 0.5f), drag: 2.5f, intensity: 2.6f);
                t.Drift = Layer("Glow", true, RitualParticleShape.Rise, 20, V(1f, 3.5f), V(1.2f, 2.2f), V(0.5f, 1.2f),
                    C(1f, 0.9f, 0.7f), C(1f, 0.8f, 0.5f), gravity: -0.08f, duration: 1.2f, intensity: 1.8f);
                t.Extra = Layer("Smoke", false, RitualParticleShape.Sphere, 10, V(2f, 5f), V(1f, 1.6f), V(2.5f, 4.5f),
                    C(0.06f, 0.05f, 0.06f, 0.7f), C(0.12f, 0.1f, 0.12f, 0.6f), drag: 2.5f, spin: 40f);
                break;
            case "OldOnes":
                t.Glow = C(0.42f, 0.74f, 0.54f); t.Accent = C(0.86f, 1f, 0.82f); t.GlowIntensity = 2.2f;
                t.Burst = Layer("Smoke", false, RitualParticleShape.Sphere, 34, V(4f, 12f), V(1.4f, 2.2f), V(6f, 12f),
                    C(0.07f, 0.06f, 0.04f, 0.9f), C(0.14f, 0.12f, 0.08f, 0.8f), drag: 2.5f, spin: 40f);
                t.Drift = Layer("Ember", true, RitualParticleShape.Rise, 26, V(1f, 3.5f), V(1.4f, 2.4f), V(0.5f, 1.2f),
                    C(0.4f, 0.75f, 0.5f), C(0.7f, 0.9f, 0.65f), gravity: -0.08f, duration: 1.4f, intensity: 1.8f);
                t.Extra = Layer("Shard", false, RitualParticleShape.Radial, 26, V(10f, 24f), V(0.6f, 1.2f), V(0.7f, 1.5f),
                    C(0.32f, 0.27f, 0.2f), C(0.2f, 0.17f, 0.12f), gravity: 2.5f, drag: 1f, spin: 400f);
                t.ShakeStrength = 1.7f;
                break;
            default:
                t.Burst = Layer("Mote", true, RitualParticleShape.Radial, 50, V(10f, 24f), V(0.4f, 0.9f), V(0.5f, 1.2f),
                    C(1f, 0.8f, 0.45f), C(1f, 0.95f, 0.8f), drag: 2.5f, intensity: 2.6f);
                t.Drift = Embers(C(1f, 0.75f, 0.4f));
                t.Extra = Off();
                break;
        }
    }
}
