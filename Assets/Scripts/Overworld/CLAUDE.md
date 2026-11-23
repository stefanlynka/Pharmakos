# Overworld notes

- `OverworldMapController.CurrentNode` is the last **completed** node. While an encounter is being played, the node the player entered is `Controller._lastOverworldNodeEntered`; `ReturnToMap` makes it current (and cleared) only when the encounter ends.
- `OverworldScreen.Enter/Exit` swap cameras: Enter deactivates Main Camera and activates the overworld camera; Exit does the reverse. `ScreenHandler.ShowScreen(..., hideOthers: true)` calls Exit on every screen, so any screen change reactivates Main Camera; encounter starters turn it off again with `SetMainCombatCameraActive(false)`.

## Map view (Map button)

- The Map button (`UniversalCanvas/MapButtonScreen`, `ScreenName.MapButton`, `MapButtonScreen` component) sits under the Status button. It shows/hides itself every frame from `Controller.ShouldShowMapButton()` (Status button visible, current screen in `MapButtonScreens`, Status not open) and calls `Controller.ToggleMapView`. Its Screen is `ManualHide`, so `HideAll` doesn't touch it.
- `OverworldMapView.Open` doesn't change screens: it suppresses the visible screens' UI (`Screen.SetSuppressed(..., instant: true)`: hides the CanvasGroup at once but keeps the screen entered; open/close never fade) and calls `OverworldScreen.EnterMapView`, which raises the overworld camera's depth to `MapViewCameraDepth` (above Main/Event/Altar/Shop cameras, below Status at 10) instead of touching other cameras. `ShowMapViewOnly` centres on the focus node, puts the lyre there and highlights the next nodes from it; `IsViewOnly` skips hover and clicks.
- While it's open, `Controller.Update` skips player updates and `AnimationUpdate`, so combat and queued transitions are frozen until it closes. Direct-mouse code (`MenuSelectionHandler`, `ViewBuff`, `OfferingLabel`) checks `OverworldMapView.BlocksGameInput`; new direct-mouse code on screens that show the Map button should too.
- Escape closes it (after Status/deck viewers). If something enters/exits the overworld screen for real while it's open, `OverworldScreen` closes the view and calls `OverworldMapView.NotifyClosedExternally`.

## Encounter layout

- `OverworldMapNode.AssignEncountersByFloorRules` decides each floor's mix (shops on R=0/4/7, temples, events, the rest combat). Node encounters are saved per node in `RunSaveData.nodes`, so changing these rules doesn't alter existing saves.
