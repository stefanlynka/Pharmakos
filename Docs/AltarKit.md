# Altar kit

Every ritual altar uses one generated mesh, `Assets/Resources/Models/Meshes/AltarKit.fbx` (120 faces, built by
`Tools/Blender/build_altar_kit.py`). It has four material slots (border strips are `TRIM_W` 0.09 wide, corner blocks `CORNER_W` 0.12):

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

Styles (every god in `Rituals.txt` has one; Default is the fallback):

| God | Stone (body) | Border | Corner |
|---|---|---|---|
| Aphrodite | rose marble | rosette vine on wine red | rose |
| Apollo | sunlit gold marble | gold sun rays | sun |
| Ares | oxblood jasper | black spears on terracotta | round shield |
| Athena | grey-green stone | silver olive branch | owl |
| Demeter | wheat sandstone | golden wheat ear on green | wheat ear |
| Dionysus | wine-red marble | green-gold vine on black | grape cluster |
| Hades | black basalt | bones | skull |
| Hephaestus | forge-iron grey, molten cracks | bronze chain | hammer |
| Hera | muted lapis, gold veins | gold peacock feathers on lapis | peacock feather |
| Hermes | weathered sky slate | cream wing feathers on blue | caduceus |
| Hestia | hearth terracotta | hearth fires | fire on a hearth |
| Muse | ivory, ink veins | dark egg-and-dart | lyre |
| Old Ones | dark umber stone | verdigris serpent | serpent coil |
| Poseidon | teal marble | bronze wave scroll | trident |
| Zeus | storm slate | gold lightning | thunderbolt |
| Default | cream marble | carved key | rosette |

Major and Minor rituals of the same god share a style.

## Board-wide rules (cohesion)

The styles follow shared rules so fifteen different altars still read as one set that sits with the pottery board.
They live as constants at the top of `altar_textures.py`, so every style follows them:

- **Matte, weathered stone** (`MATTE_STONE`, `MATTE_RELIEF`, `WEATHER`): low smoothness like the terracotta, grime
  patches, pitting, grime in the recesses beside the carving and rubbed-light highs on the motifs.
- **A light text panel.** A dark or strongly coloured stone's text panel is pulled toward a shared cream
  (`PANEL_CREAM`) while keeping some of the god's hue. This one dial sets the balance between cohesion and individuality:
  - `PANEL_LIFT_MAX` (0 = the god's own stone, 1 = plain cream) and `PANEL_TINT` (how much god hue the cream keeps).
  - Current: lift 0.45, tint 0.3. A plainer, more uniform set: lift 0.75, tint 0.15. Set them per run with the
    `ALTAR_PANEL_LIFT` / `ALTAR_PANEL_TINT` environment variables, or per style with `panel_lift` / `panel_tint`.
  - After changing it, run **Tools → Altar Kit → Reset Text Colours From Panels** to re-pick black or cream text.
- **Bold borders.** The motif's colour fills its whole carving, so the border reads as a coloured band (figure and
  ground, like pottery) at board distance. The band is the god's main signal on the board; the body stone mostly shows on
  the steps.
- **Warm palette.** Prefer warm stones; a cool god's colour goes on the body or the border ground, muted.
- **Warm light, grounded.** The altar spotlight is warm (`ViewRitual3` → `Spot Light`, colour ≈ `#FFE0B8`) and
  `AltarContactShadow` on `Altar2` draws a soft shadow round the footprint (Graphics.DrawMesh, shape from the
  `AltarFlame` footprint; look on `Models/Materials/AltarContactShadow.mat`).

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
blender -b --factory-startup --python Tools/Blender/altar_textures.py -- <absolute path to Assets/Resources/Content/AltarStyles> [<God> ...]
```

The output folder must be an absolute path: with a relative one Blender writes somewhere else and nothing changes.
After regenerating, Unity may need a forced reimport of the folder before the new textures show.

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
