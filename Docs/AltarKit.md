# Altar kit

Every ritual altar uses one generated mesh, `Assets/Resources/Models/Meshes/AltarKit.fbx` (120 faces, built by
`Tools/Blender/build_altar_kit.py`). It has four material slots:

| Slot | Covers | Texture layout |
|---|---|---|
| 0 Stone | body, steps, slab sides | tileable square (repeats every ~0.55 altar units) |
| 1 Panel | the two faces that carry the rules text and costs | tileable square; keep it calm so text reads |
| 2 Trim | raised border strips round both faces | 4:1 strip, tiles left–right; bottom edge of the image is the outer edge of the altar |
| 3 Corner | raised corner blocks | square, no tiling |

A god's look is an **AltarStyle** asset: those four materials plus the colour of the rules text.

- Styles live in `Assets/Resources/Content/AltarStyles/<God>/<God>.asset`, next to their textures and materials.
- The god comes from the ritual's class name (`AphroditeMajor` → `Aphrodite`, see `Ritual.God`).
- Rituals with no style for their god use `Default`.
- `ViewRitual.Refresh` applies the style, so it works on the board, in rewards, in starter bundles and in events.

Styles so far: **Default** (cream marble, carved key), **Poseidon** (teal marble, bronze wave scroll, tridents),
**Zeus** (storm slate, gold lightning, thunderbolts), **Aphrodite** (rose marble, rosette vine on wine red, roses).

## Tuning a style

Edit its four materials and the style asset in the inspector. That is the source of truth: nothing overwrites them.

- Use the material colour and Smoothness for quick changes.
- Use **Text Color** on the style asset to switch between light and dark text.
  - Use pure black, not a dark tint: the altar text's TMP material renders dark tints washed out.

## Adding a god

1. Make a folder `Assets/Resources/Content/AltarStyles/<God>/` with these textures (names matter):
   - `Stone_Albedo`, `Stone_Normal`, `Stone_Mask`
   - `Panel_Albedo`
   - `Trim_Albedo`, `Trim_Normal`, `Trim_Mask`
   - `Corner_Albedo`, `Corner_Normal`, `Corner_Mask`

   Masks are URP metallic maps: R = metallic, A = smoothness. Normals and masks are optional; without them the
   material is flat.
2. In Unity, run **Tools → Altar Kit → Build Missing Altar Styles**. It sets the texture import settings, creates the
   four materials and the style asset, and picks light or dark text from the panel's brightness. It only creates
   what's missing, so it never touches a style you've tuned.

Placeholder textures can be generated procedurally:

```
blender -b --factory-startup --python Tools/Blender/altar_textures.py -- Assets/Resources/Content/AltarStyles <God>
```

To add a god there, add an entry to `STYLES` in the script (colours, a trim motif and a corner motif).

## Making better textures with Gemini

The procedural textures are placeholders. Any texture can be swapped for painted or AI-made art of the same
layout: replace the PNG, keep the name. Suggested prompts (replace the god-specific parts):

- **Stone:** "Seamless tileable texture, flat top-down photo of polished [deep teal sea-green marble with white foam-like
  veins], even lighting, no shadows, no perspective, square."
- **Panel:** the same stone, calmer: "…very subtle, low-contrast veining, mostly even colour", or take the stone
  texture and lower its contrast in an image editor. Rules text sits on it, so it must stay quiet.
- **Trim:** "Seamless horizontally tileable decorative border strip, 4:1, [bronze relief of a running wave scroll]
  on a [dark teal] recessed ground, raised thin fillets along top and bottom edges, flat front view, even lighting."
  - The **bottom edge** of the image becomes the **outer edge** of the altar.
- **Corner:** "Square bronze relief medallion of a [trident] on a dark recessed ground with a raised square rim, flat
  front view, even lighting."

Normal maps can be derived from the albedo (e.g. in Materialize or an online normal-map tool), or left out.

## Rebuilding the mesh

```
blender -b --factory-startup --python Tools/Blender/build_altar_kit.py -- Assets/Resources/Models/Meshes/Altar2.fbx Assets/Resources/Models/Meshes/AltarKit.fbx [preview.png]
```

- **Proportions:** the dimensions live at the top of the script. Border width is `TRIM_W` and `CORNER_W`, steps are
  `STEPS` and `STEP_INSET`.
- **Rounded edges:** every block (steps, slab, ramp, border strips, corners) is built with shared vertices and bevelled. The widths are the `BEVEL_*` constants. `SMOOTH_ANGLE` keeps the bevels smooth-shaded and the real corners sharp.
- **Ramp and slab:** the ramp stops at the slab's front face and below its top, so the ramp's border can't poke into the slab.
- **Reference mesh:** the old Meshy altar (`Altar2.fbx`) is only read for its transform, so the new mesh lands in the
  same space and the cost badges and text stay put. Keep that file.
- **Unity import settings:**
  - Scale Factor 100
  - Normals: Import
  - Material Creation: None

## Ready flame

When a ritual is usable its altar burns: `AltarFlame` (on the prefab's `Altar2`) draws a skirt of flame rising from the altar's footprint and leaning outward, so tongues show past every side without touching the text faces.

- **Shape and timing** (component): `Height`, `Lean`, `Inset`, `FadeSpeed`, `Intensity`. Tick **Preview In Editor** to see it while tuning.
- **Look** (`Models/Materials/AltarFlame.mat`): base/body/tip colours, noise scale, rise speed, tongue sharpness, base glow.
- If the altar's outline changes, update `Footprint` (local x/z, in order round the base).
