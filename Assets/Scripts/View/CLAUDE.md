# View notes

- **Every button is a `ViewButton` instance** (`Assets/Resources/Prefabs/UI/Buttons/ViewButton.prefab`), nested inside the screen prefabs. Look and feel (sprite, tint states, `ButtonFeedback` hover settings) come from `ViewButton`; edit it there. New buttons: drag in `ViewButton`, don't build one from scratch.
- Per-instance overrides are expected only for layout and content: RectTransform, label text/size/margins, `pixelsPerUnitMultiplier`, and `onClick`. Don't set `ButtonFeedback` or `Button.colors` on instances; that creates overrides in the screen prefab that mask the base. If they creep in, revert per property with `PrefabUtility.RevertPropertyOverride`.
- Most `onClick` handlers are wired **in the scene** (targets on `GameHandler`), not in the screen prefabs, because the targets live in the scene. Replacing a button object loses that wiring; re-add it with `UnityEventTools.AddPersistentListener`.
- `DeckViewer.MyButton` references the scene's `MyDeckButtonScreen/DeckButton`. `TutorialSpotlight` finds End Turn by its label text "End Turn", so keep that label.
- One `ButtonFeedback` per button. Duplicates fight over the label colour every frame.
