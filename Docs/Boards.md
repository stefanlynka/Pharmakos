# Combat boards

Every encounter's combat board (the ground under everything plus the two lanes) is painted by a script, not tiled
from stock textures. Each board is a composed picture: the arena (both lanes and the gap) stays worn and calm, the
story lives in the margins (bones, vines, shields, statues, the maze), and the lanes are quiet so cards read on them.

## How it fits together

| Piece | Where |
|---|---|
| Painter | `Tools/Boards/boards.py` (runner), `boardgen.py` (noise, shapes, normals, output), `boardkit.py` (arena distance, scattering, bones, rocks, leaves, prints, flagstones, marble, Greek key), one recipe per board in `Tools/Boards/recipes/` |
| Textures | `Assets/Resources/Content/Boards/<Name>/` `Ground_*` and `Lane_*` (Albedo, Normal, Mask = smoothness in A, optional Emission) |
| Look asset | `BoardStyle` at `Resources/Content/Boards/<Name>/<Name>.asset`: Ground, PlayerLane, EnemyLane materials, plus Flicker for glowing boards |
| Materials | `<Name>_Ground`, `<Name>_Lane`, `<Name>_EnemyLane` (URP Lit), made by **Tools → Boards → Build Board Styles** |
| Runtime | `View.ShowBoard(name)`: uses the BoardStyle when there is one, otherwise the old `Images/Backgrounds/Materials/<Name>_1/_2` pair; bosses with neither fall back to Underworld. `BoardAmbience` makes emission flicker. |

Board names come from `View.backgroundNamesByDeckName`. A name with no BoardStyle and no old material falls back to
Underworld.

## Boards

| Board | Encounter | Ground | Lanes |
|---|---|---|---|
| Underworld | test enemy | cracked basalt and ash, skeletons and bone piles, embers in the far cracks, asphodel | worn slate slabs |
| Cyclops | Cyclops | wind-laid turf, lichened limestone boulders, the giant's footprints | dusty sheep tracks, hoofprints |
| Labyrinth | Labyrinth | the maze itself around a cleared chamber; bones, blood drags, torch scorches | flagstone runners |
| Bacchanalia | Bacchanalia | loam, dark muted vines creeping in with leaves, tendrils and grapes, spilt wine, dull pottery shards | trampled earth, wine, bare feet and satyr hooves |
| Caves | Caves (the Gorgon) | wet green-grey rock, slime, puddles (petrified statues were tried and dropped as unconvincing) | damp grit and puddles |
| Hunt | Hunt (Artemis) | moss and leaf litter, roots from the edges, ferns, an antler, spent arrows | muddy game trail, deer slots |
| Troy | Troy | trampled plain, chariot ruts, a few dust-caked shields half buried, spears, broken wheels; **3D back edge:** the walls of Troy | packed earth, ruts |
| Trials | Trials (Amazons) | raked arena sand inside a stone kerb, straw targets with arrows, dry grass outside | raked sand |
| Cliffs | Seaside Cliffs (Sirens) | dark sea rock, tide pools, kelp, barnacles; a jagged cliff edge with coves dropping to the sea, rocks and stacks in the surf | weathered rock ledges, salt |
| Temple | Delphi | aged marble slabs, an inlaid Greek key round the arena, tripod scorches, laurel | pale marble runners |
| Archiereus | Archiereus | black marble veined red, blood channel and gutters, carved dedication, candles | dark basalt slabs |
| Throne | Hades (boss) | polished obsidian, gold-inlaid Greek key, pomegranate seeds, old stains where supplicants knelt (no bones) | black basalt |
| Fates | the Moirai (boss) | dark stone; a warp-weighted loom laid along the far edge (cloth, warp threads in bundles to clay weights, some red, a few cut), spindles, wool | dark wood planks |
| TheGate | the Gate (boss) | ancient black paving, heaved, cracked, broken and missing in places; Cerberus' claw marks, chains, obols, the glowing Styx along the far edge | wet black flagstones |

The three boss boards are painted by one recipe: `python Tools/Boards/boards.py Bosses`.

## Changing a board

1. Edit the recipe (or `boardkit.py` for something shared) and run
   `python Tools/Boards/boards.py <Name> --preview` (plain Python 3 with numpy and Pillow; about a minute per board;
   `--preview` writes a flat lit preview to `Claude/Boards/preview/`). No argument paints all boards.
2. In Unity, refresh (the textures' import settings are applied on import by `BoardTexturePostprocessor`), then
   **Tools → Boards → Build Board Styles** with GameScene open. It only rewrites textures and UV tiling on existing
   materials, so colours and smoothness tuned in the inspector are kept.
3. In the test chamber, **O** shows the next board and **I** the previous one.

Things to know:
- Everything is laid out in board world units (lanes 60 x 10; player lane y -0.65..9.35, enemy lane 11.92..21.92). The
  ground canvas covers x -68..68, y -24..44: all the board plane the combat camera can see.
- The ground plane's UVs run toward -x/-y, so the painter saves the ground turned 180 degrees (normals flipped to
  match) and the builder works out the tiling from the Background mesh. If NewBackgroundHolder moves or the lane
  mesh changes, rebuild the styles (the lane mesh isn't Read/Write, so its UV square is recorded in the builder).
- Albedos are painted dark and lifted by `GROUND_GAIN` / `LANE_GAIN` in `boardkit.finish`, because the combat lights
  are dim and warm. Raise or lower those to brighten or darken every board at once.
- Ground normals are saved at half size: fine relief barely shows at board distance and it keeps the repo light.
- Emission (embers, torches, candles, the sea's glimmer) flickers by the BoardStyle's Flicker and FlickerSpeed.

## Back edges (3D)

Every board stands a 3D piece along its far edge (`BoardStyle.BackEdge`, a prefab whose mesh is in board world space;
View.ShowBoard instantiates it at the world origin under GameField and swaps it when the board changes).

| Board | Back edge |
|---|---|
| Troy | painted backdrop of the ruined walls (prototype of the generated-art approach; see below) |
| Temple (Delphi) | three-stepped stylobate, Doric colonnade rising out of frame, shadowed cella wall |
| Archiereus | two black marble steps, red-veined black marble wall with pilasters, candle clusters on the ledge |
| Trials | stone seating tiers of the arena |
| Throne | obsidian dais edged in gold, gold-banded obsidian pillars, braziers |
| Labyrinth | the maze wall stepping back and forth, dark passages leading off, torches in iron sconces |
| Fates | the warp-weighted loom standing: posts, cloth with a red meander, warp threads to clay weights (a few red, a few cut) |
| TheGate | massive black pillars, bronze gates rising out of frame, walls running off to either side |
| Cliffs | sea stacks in the surf below the cliff edge, headlands at the far corners |
| Caves | rugged wet cave wall, stalagmites |
| Underworld | jagged basalt ridge with embers glowing in its cracks |
| Cyclops | rocky hillside with grassy ledges, the cave mouth and the great boulder |
| Hunt | bank of earth and roots, old trunks rising out of frame |
| Bacchanalia | dry-stone terrace wall, pithoi, a pergola hung with vines and grapes |

How it's made:
- Each recipe's `back_edge()` (bosses: `throne_edge`, `fates_edge`, `gate_edge`) builds the mesh from primitives in
  `Tools/Boards/backedge.py` (box, lathe, relief, strand) and helpers in `bekit.py`, and paints one texture set per
  material (`BE_<Material>_*` in the board folder) with `bepaint.py`. `boards.py` runs them with the boards; to rerun
  only the back edges: `python Tools/Boards/backedges.py troy:back_edge temple:back_edge ...`.
- The mesh goes to `Claude/Boards/backedge_json/<Name>.json` (regenerated, gitignored: run the painter before building on a fresh clone); **Tools → Boards → Build Back
  Edges** turns it into `<Name>_BackEdge_Mesh.asset`, `BE_<Material>.mat` and `<Name>_BackEdge.prefab` and sets the
  BoardStyle. Materials keep inspector tweaks; only textures and keywords are rewritten.
- **Height budget:** the camera looks almost straight down, so height climbs the screen fast (about 0.016 of the
  screen per unit). Things at the front line (y ~34) show about 6 units before leaving the frame; taller things leave
  the frame on purpose. Keep clear of the enemy portrait (x -9..9) and its offerings (x 8..29) at y below ~34.5.
- **Light:** back edges stand outside the spotlights, so every material emits a share of its own albedo
  (`self_light` per material, times `SELF_LIGHT_GAIN` in backedge.py). Brighten one in Unity with its material's
  Emission colour.
- **Generated art (prototype: Troy).** Source images made with fal.ai (FLUX 2 Pro, through MCP for Unity's
  `generate_image`; it saves into `Assets/`, so move the file to `Tools/Boards/gen/` and delete the copy) are committed
  in `Tools/Boards/gen/`. Floors: `make_tileable` + `sample_tiled` (boardgen.py) mix a tile at two or three scales and
  angles through noise so no repeat reads, then the recipe adds its own large tone, wear, shadows and the edge fade to
  black. Back edge: `BackEdge.backdrop` stands the painting at the far edge leaning back to face the camera; the recipe
  grades it, fades it to black at the sides and lets it carry its own light through the emission map.
- **Layout that made room for them:** the enemy lane and card row moved down 2 units, the enemy portrait was scaled
  from 2 to 1.4, and its resources and altars moved down (in GameScene).

## Keeping boards on spec

The board spec lives in `Docs/StyleGuide.md` ("Board spec"). Two tools enforce it:
- **Grades:** `GRADES` in `boardkit.py` darkens, desaturates or re-tints a whole board (ground, lanes and back edge) in
  one place, and `SMOOTH_CAP` keeps every board matte. Graded today: Troy, Trials, Temple, Cyclops, Labyrinth.
- **Check:** capture the boards in game (1920x1080, one `<Board>.png` per board), then
  `python Tools/Boards/check.py <folder>` reports the gap's brightness, spread and saturation and the margin's
  brightness, and flags anything over the spec's limits. The regions assume the test chamber's layout; an enemy altar
  or card over a region skews it.
- Static flame geometry is off (`FLAMES = False` in `bekit.py`) until there is an animated flame VFX.
