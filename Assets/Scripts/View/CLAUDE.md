# View notes

- **Every button is a `ViewButton` instance** (`Assets/Resources/Prefabs/UI/Buttons/ViewButton.prefab`), nested inside the screen prefabs. Look and feel (sprite, tint states, `ButtonFeedback` hover settings) come from `ViewButton`; edit it there. New buttons: drag in `ViewButton`, don't build one from scratch.
- Per-instance overrides are expected only for layout and content: RectTransform, label text/size/margins, `pixelsPerUnitMultiplier`, and `onClick`. Don't set `ButtonFeedback` or `Button.colors` on instances; that creates overrides in the screen prefab that mask the base. If they creep in, revert per property with `PrefabUtility.RevertPropertyOverride`.
- Most `onClick` handlers are wired **in the scene** (targets on `GameHandler`), not in the screen prefabs, because the targets live in the scene. Replacing a button object loses that wiring; re-add it with `UnityEventTools.AddPersistentListener`.
- `DeckViewer.MyButton` references the scene's `MyDeckButtonScreen/DeckButton`. `TutorialSpotlight` finds End Turn by its label text "End Turn", so keep that label.
- One `ButtonFeedback` per button. Duplicates fight over the label colour every frame.

## Highlights

- Two glow components: `CardHighlightEffect` (cards, trinkets/`ViewBuff`, starter bundles; sprite-based "mist" shader) and `MeshHighlightEffect` (3D: player portraits, ritual altars, Styx trinkets).
- **The inspector's `settings` preset is what plays.** Both components copy it into their glow fields on `Awake` (and every frame in the editor while glowing, so preset edits show live in play mode). The custom inspectors show the preset inline and hide the mirrored fields. Code never writes `settings`. Assigned presets: bundle = ChoiceLarge (band widened in the prefab: glow sprites scaled up, 9-slice size scaled down), trinkets (`ViewBuff.prefab`) = Choice, portraits (`ViewPlayerPortrait`) = TargetMesh, altars = Altar, Styx trinkets = Hammer1/Shears1 (scene overrides, `Assets/Settings`).
- **Only cards switch preset at runtime**, because their state changes: `ViewCard.SetHighlight(bool, HighlightState)` picks from `HighlightPalette` (`Resources/Content/HighlightSettings`: Follower, Spell, Target, Choice) and calls `CardHighlightEffect.UsePreset`, which sets a runtime-only live preset (`LivePreset`); the inspector shows which one is playing. Selected = Ready preset × `SelectedIntensity` via `IntensityScale`.
- Combat state decisions live in `ViewBattleRow.RefreshPositions` (per-frame: attacker = Selected, attack/spell/ritual targets = Target, can-attack = Ready) and `View.HighlightTargets` / `ViewBattleRow.HighlightTargets` (Target). Hand cards: `ViewHandHandler` (Ready; the spell being aimed, `SelectionHandler.HeldCard`, stays in hand as Selected). Out-of-combat callers pass `HighlightState.Choice` (rewards, shop, temple, deck selection).
- Ritual altars use the Altar preset and `RitualOutlineHighlight` (Linework layer 8) colours.
