# View notes

- **Button hover settings come from `Assets/Resources/Prefabs/UI/Buttons/ViewButton.prefab`.** 41 of 53 buttons are nested `ViewButton` instances inside the screen prefabs. Don't set `ButtonFeedback` fields on those instances (it creates overrides in the screen prefab that mask the base); change `ViewButton` instead. If overrides creep in, revert them per property with `PrefabUtility.RevertPropertyOverride`.
- 12 buttons are standalone (not `ViewButton` instances): StartScreen/TestChamber1, TutorialScreen/TryAgainButton+QuitButton, PlayHistoryButtonScreen/PlayHistoryButton, OptionsScreen/ResumeButton+TryAgainButton, GameScreen/EndTurnButton+DiscardAllButton+DeckButton, DeckViewerScreen/DeckButton+Button, plus one scene-only DeckButton. They don't inherit from `ViewButton`; update them alongside it.
- One `ButtonFeedback` per button. Duplicates fight over the label colour every frame.
