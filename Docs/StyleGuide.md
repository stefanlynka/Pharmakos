# Visual Style Guide

The standard every visual change is checked against. Goal: one visual language across every screen, so the game reads as a finished, professional release.

**Direction:** *Black-figure pottery in a candlelit temple.* Dark grounds, terracotta and bone accents, warm firelight, restrained gold for anything precious or interactive. The inked main-menu illustration is the tone benchmark: ancient, hand-made, slightly grim.

Status: **draft**. Values marked *(TBD)* get locked as each step lands. Locked so far: typography, buttons, plaques, interaction states.

---

## Palette

Use these, not ad-hoc colours. Hex values are starting points sampled by eye from the current game; refine in-engine, then update here.

| Role | Name | Hex | Use |
|---|---|---|---|
| Ground | Kiln Black | `#141110` | Plaques, panels, card text boxes, button lettering |
| Accent | Terracotta | `#D9733F` | Button fill (the only terracotta *fill*), plaque key borders, pattern bands |
| Accent dark | Fired Clay | `#8A3F1E` | Pressed states, shadows on terracotta |
| Light | Bone | `#E8DCC2` | Body text on dark, parchment, stone highlights |
| Precious | Votive Gold | `#D9B45A` | Costs, counters, rare/important emphasis. Use sparingly |
| Offering | Blood | `#9E1B22` | Blood offering only |
| Offering | Bone (offering) | `#D8D4CC` | Bone offering only |
| Offering | Crop | `#2F6B3A` | Crop offering only |
| Offering | Scroll | `#2E5C8A` | Scroll offering only |

Rules:
- Offering colours mean offerings. Don't reuse them for UI states.
- No cyan, no neon green, no default Unity grey anywhere.
- Text is Bone on dark, or Kiln Black on terracotta/parchment. Never pure white `#FFFFFF` on large areas.

## Typography

Two typefaces only (plus the HECATOMB logo, which is artwork, not a font).

| Role | Face | Use |
|---|---|---|
| Display | **Cinzel** (`Assets/Fonts/Cinzel/Cinzel SDF`) | Screen titles, buttons, card names, numbers, short labels |
| Text | **Alegreya Sans** (`Assets/Fonts/AlegreyaSans/AlegreyaSans-Regular SDF`, Bold wired as its bold weight) | Card rules text, ritual tablet text, descriptions, event prose, tooltips, tutorial text |

Both are SIL Open Font License (licence files alongside the fonts). Cinzel is all small-caps, so keep it to short phrases; anything that wraps onto several lines uses Alegreya Sans.

Rules:
- Never ship LiberationSans or Red Rose. TMP's default font is set to Alegreya Sans; text created from code should take its font from `GameFonts.DisplayFont` / `GameFonts.TextFont` (`Assets/Resources/GameFonts.asset`).
- Alegreya Sans is narrower than Red Rose was: don't use negative character or word spacing with it (it swallows spaces).
- The font assets are **Static** (pre-baked: ASCII, Latin-1, typographic punctuation and every character in the game's text). If new text uses a character outside that set, add it with `TMP_FontAsset.TryAddCharacters` (switch to Dynamic, add, switch back to Static) or it will render from the LiberationSans fallback.
- Card rules text uses **fixed sizes in tiers**, not auto-size: Normal (most cards) and Compact (long text). Auto-size is allowed only as a last-resort floor.
- Keyword names (Sprint, Taunt, Frenzy...) are bold in rules text.
- Popups (keyword reminders, text and ritual popups) set their body in Alegreya Sans with the title in Cinzel. Reminder strings stay `<b>Title</b>
Body`; `GameFonts.WithDisplayTitle` turns the leading bold title into Cinzel, and `GameFonts.InDisplayFont` wraps any other Cinzel run inside Alegreya text.
- Title case for titles and buttons; sentence case for rules and prose.

## Interaction states

**(Locked.)** Glow *colour* says what kind of thing it is; glow *behaviour* says what you can do with it. Code: `HighlightState` + `Assets/Resources/Content/HighlightSettings/HighlightPalette.asset` (alongside its presets). Cards use `ViewCard.SetHighlight(bool, HighlightState)`, which picks the preset from the palette by state. Everything else plays the preset assigned in its inspector. Tune glows by editing the preset assets, never per-prefab colour fields.

| State | Meaning | Treatment |
|---|---|---|
| **Ready** | Can act now: playable card, follower that can attack | Steady glow in the card's **offering colour**: followers = Crop green, spells = Scroll blue (they produce those offerings when played). Sampled from `OfferingHandler.GetOfferingColor`. |
| **Selected** | Currently picked (e.g. the attacking follower) | Its Ready colour at 1.45× intensity |
| **Target** | Valid target while aiming an attack, spell or ritual | **Slowly pulsing ember** (`Target`, `TargetMesh` for portraits; 0.3 Hz, the same rate as the Ready glow), whatever the kind. Ember is deliberately not an offering colour. |
| **Choice** | A pick outside combat: rewards, shop, temple, deck selection, starter bundles, trinkets | Steady ember (`Choice`). Large framed picks (starter bundles) use `ChoiceLarge`: same colours, 1.45× intensity and a widened band, or the thin edge disappears against pale marble. |
| Ritual usable | Ritual on the altar can be used | Fire (`Altar` preset), unchanged |
| Hover | Mouse over | Brighten + lift; no colour change of its own |
| Unavailable | Can't use | No glow |

Never use the five offering colours (Gold, Blood, Bone, Crop, Scroll) for anything except offerings, or for the Ready glow of what produces them.

Overworld: reachable hexes get an **amber** outline (Choice ember brightened to read on terracotta tiles), the current hex a **warm bone** outline (`Assets/Outlines/Free Outline Settings.asset`, rendering layers 2 and 4). The legend icon sheets (`Images/Overworld/2.png`, `3.png`) are baked screenshots; if outline colours change, recolour their rings too.

Targeting arrows are black with orange edges; the edges glow ember (emission masked by the arrow's own texture, `Models/Materials/3DArrow.mat`) so they read on the dark board and match the Target glow. The spell being aimed stays in hand in the Selected state.

## UI components

**Core rule: terracotta is touchable.** Buttons are terracotta with a black Greek key; plaques (anything you read but don't click) are black with a terracotta Greek key. They are mirror images of each other, like red-figure and black-figure pottery. Nothing non-clickable gets a terracotta fill.

- **Button (locked):** sprite `Assets/Resources/Images/UI/Button3.png` (terracotta fill, black key; 9-slice border 230/227/212/200), Image type **Tiled**.
  - Label: Cinzel, Kiln Black `#141110`, TMP margins inset inside the key border (≈ `230/ppum × 0.85` horizontal, `210/ppum × 0.55` vertical).
  - States (Button Color Tint, multiplier **1**; UI colours clamp at 1, so a multiplier above 1 can't brighten): normal `0.85`, highlighted white, pressed `0.7`, disabled `0.5` at 60% alpha. Don't tint buttons with hue colours in code; dim/brighten only (e.g. status tabs: inactive `0.55` grey, active white).
  - Every button has a `ButtonFeedback` component: on hover the label eases to the `LabelHoverColor` set on the `ViewButton` prefab (currently `#FFDCA7`) and the button scales to 1.04 (one component per button; duplicates fight over the label); on press it dips to 0.97. It also blocks double-clicks (`InputGuard`). Add it to any new button. If code recolours a label at runtime, call `ButtonFeedback.CaptureLabelColors()` afterwards.
- **Plaque (locked):** one style for all non-clickable framed info, large or small: screen titles, subheadings, the overworld legend, tutorial popups, text popups. Sprite `Assets/Resources/Images/UI/BlackUIElement.png` (black fill, terracotta key), Image type **Tiled**, colour white. Text in Bone `#E8DCC2`: Cinzel for titles and labels, Alegreya Sans for prose. Size, typeface and position carry the hierarchy, not a different frame. No soft/blurred black boxes.
- **The Greek key (meander) belongs to the UI.** It marks buttons and plaques only. Board objects and 3D props use other pottery motifs (wave scroll, laurel, egg-and-dart, tongues, rosettes).
- **Greek key thickness (locked):** the same *on-screen* thickness on every button and plaque, about 28 px at 1080p. Because canvases are scaled differently, set `Pixels Per Unit Multiplier = 8.2 × (element scale ÷ root-canvas scale)`, and never less than `230 / (0.4 × smaller side)` so the key can't swallow a small element. Most elements end up at 8.2; the 2×-scaled overworld legend is 16.4.
- **Prose box** (event story, long descriptions): a plaque with generous inner padding, never an unframed or blurred black box. Framed boxes must sit fully on screen (≥16px from the edges) and never under buttons.

## Combat board

- **Ground and lanes:** each encounter's board is painted (`Docs/Boards.md`). It follows the board spec below.

### Board spec: the board is the stage, not an actor

The game is dark and grand, and the board must never compete with what the player reads and acts on. References: Hearthstone (a calm, uniform play surface; all ornament pushed to the frame and corners; everything communicates clearly) and Inscryption (one dominant hue per scene, a pool of light on the table, darkness all around, cards and world sharing one palette). Boards judged good in game (2026-11): Hunt, Archiereus, TheGate as wholes; Temple (Delphi) for its floor pattern only, as a whole frame it is too bright.

1. **Hierarchy.** Cards and anything targetable > altars, the pottery frames and resource holders, UI > board. The board is never the brightest or most saturated thing near the arena. Check with the squint test: blur a greyscale capture; the cards must still lead.
2. **Three zones.**
   - **Arena** (both lanes and the gap between them): uniform, low-frequency, worn, with no props and no pattern that reads at board distance. Brightness variation in the gap stays small (in-frame luminance spread ≤ ~0.05).
   - **Margins:** texture and story are allowed, but only in the board's own value family. Detail can grow away from the arena and must fade into darkness toward the screen edges, where the light pool falls off.
   - **Back edge:** silhouette and scale, kept dark; never detail that competes with the enemy portrait. (Its design is still under discussion.)
3. **Value.** In-frame luminance of the gap is about 0.08–0.20 (the liked boards sit at 0.12–0.16; Delphi at 0.33 and Trials at 0.32 read too bright as a whole). Margins are ≤ ~0.12. Cards average ~0.47, so the board stays well below them: dark and grand comes from the board receding.
4. **Colour.**
   - One dominant hue family per board; this is the encounter's identity.
   - Saturation scales with brightness: a mid-value ground stays muted (gap saturation ≤ ~0.40 when its luminance is above ~0.18; darker grounds can carry more). Troy and Trials break this rule: sat 0.6 at luminance 0.22–0.32.
   - Earth grounds go umber, olive or grey, never the terracotta orange of the pottery and the buttons. That orange is the accent hue, and a ground in it swallows the frames.
   - Accents (blood, embers, gold, wine) are small, deliberate and matte.
5. **Props.** Few, large, dark, and sitting in the surface: half-buried, in the ground's value family, with a contact shadow. No small bright isolated objects. Bones, shields, statues and bright leaves all proved distracting. Leaf litter in the ground's own value (Hunt) is fine.
6. **Materials.** Matte (smoothness ≤ ~0.35). Wet or polished looks only at low contrast; no glossy liquids (Archiereus' blood reads too glossy).
7. **Light and motion.** Nothing on the board pulls the eye by moving or glowing.
   - Flames are either properly animated (VFX or flipbook) or absent; never static flame geometry.
   - Emission stays low and far from the arena.
   - Mood per board comes from light colour and temperature rather than from more texture.
8. **Process.** Every board change is checked in a full in-game frame (cards, UI, altars, pottery), with a colour capture, the squint test and the numbers above. A board that passes in isolation can still fail in the frame.
- **Lane edge (locked):** a carved-slab edge on every lane: soft drop shadow on the ground, dark lip, bone bevel, faint inner shade. `LaneEdge` component on each lane, look in `Images/Backgrounds/LaneEdge/LaneEdge.mat` (profile texture `LaneEdgeSlab.png`). No meander on lanes. While a playable follower is held, the player lane's bevel is the drop target: pulsing ember (Target colour), steady and brighter under the cursor. No flat overlays on lanes.
- **Board objects:** one family, black-figure pottery. Black glaze (≈ `#1E1714`, a soft sheen, never mirror-glossy) for bodies and grounds, fired terracotta (darker and redder than the UI's Terracotta, matte) for rims and raised motifs, bone limestone (≈ `#CDBFA3`) only for ritual altars. The sheen/matte split comes from a smoothness map built from the albedo; the material's Smoothness scales it (keep it low: strong gloss draws the eye to frames). No metal; the board has no reflections, so metal reads as mud. The heartstrings dial is the only gold.
  - **Portrait frame (locked):** the Meshy terracotta wave-scroll frame (`Models/Meshes/PortraitFrameTerracotta.fbx`, material `PortraitFrame_Terracotta`, base colour tinted to (0.60, 0.40, 0.31) to darken Meshy's pale clay).
  - **Resource holder (locked):** the matching Meshy wave-scroll set (`Models/Meshes/ResourceHolderTerracotta.fbx`, material `ResourceHolder_Terracotta`, same tint and smoothness as the frame), in `Offerings/Resources4.prefab` (both players). 
  - **Discard bowl (locked):** a black-glaze phiale with the wave scroll on its rim (`Models/Meshes/DiscardBowl.fbx`, material `DiscardBowl_Terracotta`, same tint and smoothness), prefab `Prefabs/View/DiscardBowl.prefab` under `Player Discard`. "DISCARD" arches over the top of its floor in Cinzel Bold, terracotta, via `CurvedText` (only the top arc, so it never reads upside-down). It has its own small spot light, like the offering slots, because that corner of the board is unlit.
  - **Ritual altars:** the one board object that breaks the pottery family on purpose. Each god's altar is dressed in that god's stone and ornament (Poseidon teal marble and bronze waves, Zeus storm slate and gold lightning, Aphrodite rose marble and rosettes), so a ritual's god reads at a glance. Same altar shape for all. What keeps them one set: one material per altar (the text panel is a calmer version of the same stone); muted, matte, weathered stone with soot and old libation stains (never polished or clean); borders carved rather than painted, with faded, flaking pigment and hand-cut irregularity, still coloured enough to be the god's main signal at board distance; warm stones, with cool gods' colour muted; warm spotlight and a soft contact shadow. Nothing on an altar should look like a clean UI decal. See `Docs/AltarKit.md` ("Board-wide rules").
  - **Drop glow for every board drop target** (lanes, Discard): an ember line on the edge (`LaneEdgeGlow.mat`, Target preset colour and pulse speed), pulsing while a valid card is held and steady and brighter on hover, always opening mid-pulse and rising (`DropGlow`). The Discard label also brightens on hover.
  - **Meshy imports:** export through `Tools/Blender/meshy_decimate.py` before use (full detail or decimated) (it keeps Meshy's smoothed normals; see the script header for Unity import settings). Raw exports stay in `Assets/MeshyImports`.

## Art cohesion

- Card art and event art share one colour grade *(TBD)*: warm, slightly desaturated, lifted blacks toward Kiln Black.
- 3D props (trinkets, braziers, lyre) are lit warm from firelight; avoid cool rim light and glossy plastic specular.
- VFX use the palette: embers and fire in Terracotta/Gold, offerings in their offering colour. Particles must be large enough to read at 1080p.
- **Ritual invocations (proposed):** the one place a god's own colour is used as light. Each god's glow comes from its
  altar style, pushed toward light (Zeus pale gold, Poseidon muted sea green, Hades bone, Ares crimson, Hera lapis
  violet...), set in its `RitualTheme` (`Resources/Content/RitualThemes`). Same rules as the altars: muted rather than
  neon, no cyan. The offerings that pay for the ritual stream in their offering colours; the god's sigil is its altar
  corner emblem. Darkness first, light second: the board dims before the god answers. See
  `Assets/Scripts/VFX/Rituals/CLAUDE.md`.
- Nothing important is cut off by the screen edge.
- **Post-processing (proposed):** grade the world, never the UI. UI keeps its palette exactly (combat UI on a post-free overlay camera); the world gets Neutral tonemapping (not ACES: it crushes the dark board), a midtone lift so the board's carving reads, a gentle vignette and fine grain. Gameplay pulses (damage, ritual) may tint the edges briefly. Options and reasoning: `Docs/PostProcessing.md`.
