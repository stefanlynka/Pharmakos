# Post-processing

How the game's post-processing is set up, the look options, and the gameplay pulses. Visual review page with
screenshots: `Claude/PostFX/index.html`.

## Current setup (as found)

- URP 14, Forward+, Linear colour, HDR on, colour grading LDR mode (LUT 32).
- **Only `Main Camera` renders post-processing.** It covers combat, the trinket reward screen and anything else drawn
  by the main camera. The other screen cameras (Shop, RitualReward/temple, StarterBundle, CardReward, Altar, Styx,
  Status) have *Post Processing* off, so they get no bloom or grade.
- One global volume, `Global Post Processing Volume`, profile `Assets/Settings/GameBloomVolumeProfile` (bloom only:
  threshold 0.9, intensity 0.48, scatter 0.72).
- Combat UI (`GameCanvas`) is Screen Space - Camera on `Main Camera`, so it is post-processed with the world (bloom,
  and any grade/vignette/grain). `UniversalCanvas` / `OverworldScreen` are Screen Space - Overlay and never are.
- Anti-aliasing was **off everywhere** (MSAA 1x, no FXAA/SMAA). Now 4x MSAA (see below).

## Anti-aliasing: 4x MSAA (applied)

`Assets/URP/New Universal Render Pipeline Asset` → MSAA 4x (Unity mirrors it into QualitySettings). It cleans the
stair-stepping on 3D edges (trinket rims, altars, lanes, portrait frame) without blurring card text. SMAA High
(per-camera, post) was also tried: it smooths edges too but softens the small card text slightly. FXAA was not
considered (softer still).

## Look options (D applied)

Built by **Tools → Post FX → Build Option Profiles** (`Assets/Scripts/Editor/PostFXProfileBuilder.cs`) into
`Assets/Settings/PostFX/`. To try one, drag it into the `Global Post Processing Volume`'s Profile. Rebuilding overwrites
the PostFX_* assets in place (same GUIDs), so after hand-tuning one, don't rebuild, or copy it first.

| Profile | What it does | Notes |
|---|---|---|
| A Subtle | Current bloom + gentle vignette, +8 contrast, +4 saturation. No tonemapping. | Closest to today. |
| B Graded | Neutral tonemapping, +0.35 exposure, +14 contrast, warm-highlight / cool-violet-shadow split toning, vignette, fine grain. | Shadows read a little purple. |
| C Cinematic | Neutral tonemapping, +14 contrast, heavier vignette, warm bloom tint, medium grain, a touch of chromatic aberration. | Moodiest. |
| **D Balanced** (in use) | Neutral tonemapping (+0.3 exposure), midtone lift (gamma +0.12) so the board's carved skulls and bones come up out of the black, +6 contrast, slight warm gain, vignette 0.25, fine grain 0.1. | **Recommended.** |

Lessons from building them:
- **ACES tonemapping is wrong for this game:** its toe crushes the mostly-dark board to pure black. Use Neutral.
- **Lift/Gamma/Gain colours are offsets.** Keep RGB within about ±0.01 of 1; `1.04` blue tinted every shadow navy.
- Strong contrast only deepens the near-black board; lifting midtones (gamma) reads as "more detail" instead.
- No visible banding in the dark gradients (the board texture's noise hides it); grain is there for texture, not banding.
- SSAO was tried (runtime-added feature) and broke rendering. If wanted, add *Screen Space Ambient Occlusion* to the
  renderer asset in the editor (needs depth normals); most of the board is sprites, so expect little benefit.

## Keep combat UI out of post (applied)

With a vignette or grade, the corner UI (End Turn, Discard All) darkens and shifts colour: End Turn's red drops from
178 to 130 under profile B. **Tools → Post FX → Add UI Camera (keep combat UI out of post)** adds an Overlay camera
`UICamera` under Main Camera. That camera renders only the UI layer, with post off, and `GameCanvas` is pointed at it,
so UI colours match the style guide exactly. Verified: End Turn stays (178, 96, 53) under profile D. The menu command
is undoable; save the scene to keep it. Screen shake still moves the UI (the camera is a child of Main Camera).

## Consistency across screens (applied)

Post Processing is on for the screen cameras in their area prefabs: ShopCamera, RitualRewardCamera,
StarterBundleCamera, CardRewardCamera and StyxCamera. Combat, the temple and the trinket reward screen use Main Camera,
which already had it. Their UI is Screen Space - Overlay, so it is unaffected. Checked each screen: the shop's
lyre strings and the starter bundle's braziers now bloom, edges get the gentle vignette, and nothing blows out.
Still off: StatusCamera, the AltarCameras, and the render-texture cameras (popups, deck viewers), which draw into
textures or overlays.

## Gameplay pulses (applied)

`PostFXPulse` (`Assets/Scripts/VFX/`) plays short post-processing pulses on its own runtime global Volume (priority
100), layered over whatever look is active and never touching the authored profile. Tune it in
`Assets/Resources/Content/PostFXPulseSettings`. Untick **Enabled** to turn every pulse off.

| Pulse | Trigger | Look |
|---|---|---|
| Damage Taken | the human player loses health (`ChangeStatsAnimation`) | red vignette, slight fringing, saturation dip; 0.5 s; strength 0.45 + 0.12 per damage point |
| Ritual Used | the ritual lighting moment (`LightingHandler.ShakeScreen`) | dark vignette, slight fringing, saturation lift; 1.1 s |

Pulses don't hold the animation queue. A weaker pulse never cuts off a stronger one that is still playing. Call
`PostFXPulse.Play(pulse, strength)` from view code to add more.
