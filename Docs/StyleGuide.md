# Visual Style Guide

The standard every visual change is checked against. Goal: one visual language across every screen, so the game reads as a finished, professional release.

**Direction:** *Black-figure pottery in a candlelit temple.* Dark grounds, terracotta and bone accents, warm firelight, restrained gold for anything precious or interactive. The inked main-menu illustration is the tone benchmark: ancient, hand-made, slightly grim.

Status: **draft**. Values marked *(TBD)* get locked as each step lands. Locked so far: typography, buttons, plaques.

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
- No cyan, no saturated pure green, no default Unity grey anywhere.
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
- Card rules text uses **fixed sizes in tiers**, not auto-size: Normal (most cards) and Compact (long text). Auto-size is allowed only as a last-resort floor.
- Keyword names (Sprint, Taunt, Frenzy...) are bold in rules text.
- Title case for titles and buttons; sentence case for rules and prose.

## Interaction states

Three states, used identically on cards, followers, rituals, tiles, trinkets and buttons.

| State | Meaning | Treatment |
|---|---|---|
| **Available** | Can be played / used / chosen now | Soft Votive Gold glow *(TBD: exact width/intensity)* |
| **Selected** | Currently picked or being dragged | Stronger gold glow + slight lift/scale |
| **Target** | Valid target for the current action | Terracotta glow (pulsing) |
| Hover | Mouse over | Brighten + lift; no colour change of its own |
| Unavailable | Can't use | Desaturate/darken ~40%, no glow |

Targeting arrows use the Target colour and must read clearly against the board.

## UI components

**Core rule: terracotta is touchable.** Buttons are terracotta with a black Greek key; plaques (anything you read but don't click) are black with a terracotta Greek key. They are mirror images of each other, like red-figure and black-figure pottery. Nothing non-clickable gets a terracotta fill.

- **Button (locked):** sprite `Assets/Resources/Images/UI/Button3.png` (terracotta fill, black key; 9-slice border 230/227/212/200), Image type **Tiled**.
  - Label: Cinzel, Kiln Black `#141110`, TMP margins inset inside the key border (≈ `230/ppum × 0.85` horizontal, `210/ppum × 0.55` vertical).
  - States (Button Color Tint, multiplier **1**; UI colours clamp at 1, so a multiplier above 1 can't brighten): normal `0.85`, highlighted white, pressed `0.7`, disabled `0.5` at 60% alpha. Don't tint buttons with hue colours in code; dim/brighten only (e.g. status tabs: inactive `0.55` grey, active white).
  - Every button has a `ButtonFeedback` component: on hover the label eases to the `LabelHoverColor` set on the `ViewButton` prefab (currently `#FFDCA7`) and the button scales to 1.04 (one component per button; duplicates fight over the label); on press it dips to 0.97. It also blocks double-clicks (`InputGuard`). Add it to any new button. If code recolours a label at runtime, call `ButtonFeedback.CaptureLabelColors()` afterwards.
- **Plaque (locked):** one style for all non-clickable framed info, large or small: screen titles, subheadings, the overworld legend, tutorial popups, text popups. Sprite `Assets/Resources/Images/UI/BlackUIElement.png` (black fill, terracotta key), Image type **Tiled**, colour white. Text in Bone `#E8DCC2`: Cinzel for titles and labels, Alegreya Sans for prose. Size, typeface and position carry the hierarchy, not a different frame. No soft/blurred black boxes.
- **Greek key thickness (locked):** the same *on-screen* thickness on every button and plaque, about 28 px at 1080p. Because canvases are scaled differently, set `Pixels Per Unit Multiplier = 8.2 × (element scale ÷ root-canvas scale)`, and never less than `230 / (0.4 × smaller side)` so the key can't swallow a small element. Most elements end up at 8.2; the 2×-scaled overworld legend is 16.4.
- **Prose box** (event story, long descriptions): a plaque with generous inner padding, never an unframed black rectangle. *(TBD: event story box not converted yet.)*

## Art cohesion

- Card art and event art share one colour grade *(TBD)*: warm, slightly desaturated, lifted blacks toward Kiln Black.
- 3D props (trinkets, braziers, lyre) are lit warm from firelight; avoid cool rim light and glossy plastic specular.
- VFX use the palette: embers and fire in Terracotta/Gold, offerings in their offering colour. Particles must be large enough to read at 1080p.
- Nothing important is cut off by the screen edge.
