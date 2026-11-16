# View notes

- **Every button is a `ViewButton` instance** (`Assets/Resources/Prefabs/UI/Buttons/ViewButton.prefab`), nested inside the screen prefabs. Look and feel (sprite, tint states, `ButtonFeedback` hover settings) come from `ViewButton`; edit it there. New buttons: drag in `ViewButton`, don't build one from scratch.
- Per-instance overrides are expected only for layout and content: RectTransform, label text/size/margins, `pixelsPerUnitMultiplier`, and `onClick`. Don't set `ButtonFeedback` or `Button.colors` on instances; that creates overrides in the screen prefab that mask the base. If they creep in, revert per property with `PrefabUtility.RevertPropertyOverride`.
- Most `onClick` handlers are wired **in the scene** (targets on `GameHandler`), not in the screen prefabs, because the targets live in the scene. Replacing a button object loses that wiring; re-add it with `UnityEventTools.AddPersistentListener`.
- `DeckViewer.MyButton` references the scene's `MyDeckButtonScreen/DeckButton`. `TutorialSpotlight` finds End Turn by its label text "End Turn", so keep that label.
- One `ButtonFeedback` per button. Duplicates fight over the label colour every frame.

## Highlights

- Two glow components: `CardHighlightEffect` (cards, trinkets/`ViewBuff`, starter bundles; sprite-based "mist" shader) and `MeshHighlightEffect` (3D: player portraits, ritual altars, Styx trinkets). Both take a `MeshHighlightSettings` preset via `ApplySettings`. The serialized `settings` field on the components is **not** applied at runtime (only by an editor button); the live colours are the serialized colour fields, unless code calls `ApplySettings`.
- So colours are driven from code: `HighlightPalette.Instance` (Resources) holds Follower, Spell, Target, Choice and TargetMesh presets. `ViewCard.SetHighlight(bool, HighlightState)` applies the right preset when the state changes; `ViewPlayer.SetHighlight` uses TargetMesh (portraits are only highlighted as targets); `ViewBuff` and `StarterBundleBucket` apply Choice once.
- Combat state decisions live in `ViewBattleRow.RefreshPositions` (per-frame: attacker = Selected, attack/spell/ritual targets = Target, can-attack = Ready) and `View.HighlightTargets` / `ViewBattleRow.HighlightTargets` (Target). Hand cards: `ViewHandHandler` (Ready). Out-of-combat callers pass `HighlightState.Choice` (rewards, shop, temple, deck selection).
- Ritual altars keep their own fire preset and `RitualOutlineHighlight` (Linework layer 8) colours; not palette-driven.
