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

Board names come from `View.backgroundNamesByDeckName` (Throne, Fates and TheGate have their own names so they can
get their own boards; until then they show Underworld).

## Boards

| Board | Encounter | Ground | Lanes |
|---|---|---|---|
| Underworld | test enemy, bosses | cracked basalt and ash, skeletons and bone piles, embers in the far cracks, asphodel | worn slate slabs |
| Cyclops | Cyclops | wind-laid turf, lichened limestone boulders, the giant's footprints | dusty sheep tracks, hoofprints |
| Labyrinth | Labyrinth | the maze itself around a cleared chamber; bones, blood drags, torch scorches | flagstone runners |
| Bacchanalia | Bacchanalia | loam, vines creeping in with leaves, tendrils and grapes, spilt wine, broken black-figure pottery | trampled earth, wine, bare feet and satyr hooves |
| Caves | Caves (the Gorgon) | wet green-grey rock, puddles, petrified heroes and their stone gear | damp grit and puddles |
| Hunt | Hunt (Artemis) | moss and leaf litter, roots from the edges, ferns, an antler, spent arrows | muddy game trail, deer slots |
| Troy | Troy | trampled plain, chariot ruts, painted shields, spears, broken wheels, the walls along the far edge | packed earth, ruts |
| Trials | Trials (Amazons) | raked arena sand inside a stone kerb, straw targets with arrows, dry grass outside | raked sand |
| Cliffs | Seaside Cliffs (Sirens) | dark sea rock, tide pools, kelp, barnacles, the sea breaking along the far edge | weathered rock ledges, salt |
| Temple | Delphi | aged marble slabs, an inlaid Greek key round the arena, tripod scorches, laurel | pale marble runners |
| Archiereus | Archiereus | black marble veined red, blood channel and gutters, carved dedication, candles | dark basalt slabs |

## Changing a board

1. Edit the recipe (or `boardkit.py` for something shared) and run
   `python Tools/Boards/boards.py <Name> --preview` (plain Python 3 with numpy and Pillow; about a minute per board;
   `--preview` writes a flat lit preview to `Claude/Boards/preview/`). No argument paints all boards.
2. In Unity, refresh (the textures' import settings are applied on import by `BoardTexturePostprocessor`), then
   **Tools → Boards → Build Board Styles** with GameScene open. It only rewrites textures and UV tiling on existing
   materials, so colours and smoothness tuned in the inspector are kept.
3. In the test chamber, **B** cycles through every board.

Things to know:
- Everything is laid out in board world units (lanes 60 x 10; player lane y -0.65..9.35, enemy lane 13.92..23.92). The
  ground canvas covers x -68..68, y -24..44: all the board plane the combat camera can see.
- The ground plane's UVs run toward -x/-y, so the painter saves the ground turned 180 degrees (normals flipped to
  match) and the builder works out the tiling from the Background mesh. If NewBackgroundHolder moves or the lane
  mesh changes, rebuild the styles (the lane mesh isn't Read/Write, so its UV square is recorded in the builder).
- Albedos are painted dark and lifted by `GROUND_GAIN` / `LANE_GAIN` in `boardkit.finish`, because the combat lights
  are dim and warm. Raise or lower those to brighten or darken every board at once.
- Ground normals are saved at half size: fine relief barely shows at board distance and it keeps the repo light.
- Emission (embers, torches, candles, the sea's glimmer) flickers by the BoardStyle's Flicker and FlickerSpeed.
