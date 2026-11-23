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
| Aphrodite | pale white marble, blood running down the steps (red text) | blood-red roses on black | rose |
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

The styles follow shared rules so fifteen different altars still read as one set of old, used, carved stone that
sits with the dark board. They live as constants at the top of `altar_textures.py`, so every style follows them.
The ones marked (env) can also be set per run with an environment variable of the same name prefixed `ALTAR_`.

- **One material.** Each style's Panel slot points at its **Stone** material, so the text faces are the same rough
  stone as the body (the calmer `<God>_Panel` material still exists; drag it back into the Panel slot to undo).
  - **Exception, Tablet:** a style whose stone carries stains that mustn't run under the rules text (Aphrodite's
    `blood`) also gets `Tablet_Albedo` / `Tablet_Mask`: the same stone without them. The builder makes
    `<God>_Tablet.mat` from them (a copy of Stone) and puts it in the Panel slot.
  - Text colour: run **Tools → Altar Kit → Reset Text Colours From Panels**. It reads the texture of whatever
    material is in the Panel slot; light text unless the panel is genuinely pale (brightness ≥ 0.62 gets black).
    It overwrites hand-picked colours, so re-apply them afterwards (Aphrodite: red, 0.8, 0, 0).
  - `PANEL_LIFT_MAX` / `PANEL_TINT` (env `ALTAR_PANEL_LIFT`) only affect the unused Panel textures now.
- **Muted, aged colour** (`STONE_SAT` 0.7, env): every stone loses some saturation.
- **Matte, weathered stone** (`MATTE_STONE`, `MATTE_RELIEF`, `WEATHER`): low smoothness like the terracotta, grime
  patches, pitting, grime in the recesses beside the carving and rubbed-light highs on the motifs.
- **Carved ornament, not painted bands.** The border is still the god's main signal on the board, but it reads as
  relief with the remains of paint or gilding:
  - `CARVE` 0.35 (env): how far border colours move toward the stone's own colour. Higher is more tone-on-tone and
    gritty but less distinguishable at board distance (0.6 was too far: gods blurred together).
  - `PIGMENT_LOSS` 0.4 (env): how much paint or gilding has flaked off, more on the raised parts.
  - `WOBBLE`, `CHIPS`: hand-cut irregularity (no two repeats alike), eroded patches and chips; corner rims break
    unevenly.
  - `RELIEF_DEPTH` 1.5: deeper normal maps, so the carving reads by light.
- **Sacrificial wear** (`STAINS` 1, env): soot clouding, dark libation streaks running down the sides and along the
  steps, old stains pooled in the recesses. A style can add `blood=dict(count, width, length, amount)`: streams of
  fresh (glossy) and dried blood with rounded starts and drops, on the body only (see Tablet).
- **Per-style overrides:** `carve` and `pigment_loss` override the global values (Aphrodite keeps its red and black
  crisp with `carve=0.1, pigment_loss=0.15`).
- **Warm palette.** Prefer warm stones; a cool god's colour is muted onto the body and border ground.
- **Warm light, grounded.** The altar spotlight is warm (`ViewRitual3` → `Spot Light`, colour ≈ `#FFE0B8`) and
  `AltarContactShadow` on `Altar2` draws a soft shadow round the footprint (Graphics.DrawMesh, shape from the
  `AltarFlame` footprint; look on `Models/Materials/AltarContactShadow.mat`).
- **Tried and dropped:** a cream text panel on every altar (readable, but looked like a label stuck on the block);
  carved-looking rules text via a TMP underlay (invisible on dark stone, reads as raised on light stone).

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
