# Styx Node Feature — Test Checklist & Setup Notes

## What was added

### New scripts
| File | Purpose |
|---|---|
| `Assets/Scripts/Controller/StyxRunState.cs` | Tracks removed cards/rituals/trinkets; builds the Styx deck (10 Peltasts + 10 Throw Stones, replaced in removal order, extras appended). |
| `Assets/Scripts/Controller/StyxUnlocks.cs` | PlayerPrefs persistence for the two permanent trinket unlocks (`StyxUnlock_StringsOfFate`, `StyxUnlock_GatesBeyond`). |
| `Assets/Scripts/Styx/StyxUI.cs` | Runtime UGUI factory used by all four new screens (rough placeholder visuals). |
| `Assets/Scripts/Styx/StyxScreenHandler.cs` | The Styx node screen (deck swap display, trinket list, ritual pick: 1st = Major, 2nd = Minor). |
| `Assets/Scripts/Styx/TrinketUnlockHandler.cs` | First-kill trinket unlock reveal screen. |
| `Assets/Scripts/Styx/StyxTrinketSelectHandler.cs` | Run-start screen to choose which unlocked Styx trinkets to take (both/one/neither). |
| `Assets/Scripts/Styx/StatusScreenHandler.cs` | Two-tab overworld status screen (Current / Styx) with Gates Beyond sacrifice. |
| `Assets/Scripts/Styx/StatusButtonScreenHandler.cs` | Builds the "Status" button overlay shown on the overworld. |

All new scripts ship with hand-written `.meta` files so the scene references resolve on first import. Unity will generate the `Assets/Scripts/Styx` folder meta itself.

### Modified scripts
- `Controller.cs` — `StyxRunState` field (reset in `StartGame`), removal recording in `RemoveCardsFromPlayerDeck` / `SetRituals`, `RemoveTrinket`, `TrySacrificeTrinketForHeartstring`, `TrySacrificeRitualForHeartstring`, `ApplyStyxExchange`, Styx encounter routing, boss-unlock interception in `ApplyPostEncounterProgression`, `GoToTrinketUnlockScreen`, `GoToStyxTrinketSelectScreen`, `Toggle/Open/CloseStatusScreen`, StatusButton shown when entering the overworld.
- `ScreenHandler.cs` — new `ScreenName` entries (`Styx`, `TrinketUnlock`, `StyxTrinketSelect`, `Status`, `StatusButton`, appended so existing serialized values stay valid) and auto-registration of scene `Screen` components not in the serialized list.
- `StarterBundleHandler.cs` — routes to the Styx trinket select screen when any unlock exists.
- `OverworldMapNode.cs` — Styx hex visuals (Temple mesh + Blue/White materials as placeholder).

### Scene additions (`GameScene.unity`, all additive)
- 5 screens under **UniversalCanvas** (inserted before `PopupUI` so popups stay on top): `StyxScreen`, `TrinketUnlockScreen`, `StyxTrinketSelectScreen`, `StatusScreen`, `StatusButtonScreen`. Each = RectTransform (stretched) + CanvasGroup + `Screen` + handler component.
- 2 root 3D areas (start inactive): `StyxArea` at (700, 300, 0) and `StatusArea` at (800, 300, 0), each containing a `CardGrid` child with a `ViewCardScroller` and a disabled `Camera` (depth 10, solid color clear) wired to the matching `Screen.Camera`.
- `UniversalCanvas` children list and `SceneRoots` were extended; nothing existing was modified otherwise.
- Screens are *not* added to the ScreenHandler's serialized list — they're picked up by the new auto-registration at Awake.

## Test checklist

### 0. Scene loads
- [ ] Open `GameScene` in the editor. Console must show no "Broken text PPtr" / missing-script errors. The five new screens appear under UniversalCanvas; `StyxArea`/`StatusArea` are inactive scene roots.

### 1. Removal tracking
- [ ] Start a run, visit a Temple, sacrifice 3 cards. Open Status → Styx tab: the Styx deck shows those 3 cards replacing the first base slots (rest Peltasts/Throw Stones).
- [ ] Replace a ritual at a ritual reward screen → it appears under "Removed Rituals" on the Styx tab.

### 2. Status screen
- [ ] "Status" button appears top-right on the overworld (after devotion pick, and after returning from encounters); hidden inside encounters.
- [ ] Current tab: deck (3D card grid, scroll wheel works), Major/Minor rituals, trinkets.
- [ ] Styx tab: Styx deck, removed rituals, removed trinkets; no sacrifice buttons.
- [ ] Close returns to the overworld with the map and Status button intact.

### 3. Boss unlocks
- [ ] Beat Fates with no prior unlock → reveal screen for **Strings of Fate**, Continue resumes the normal card reward flow. `PlayerPrefs` key persists across play sessions.
- [ ] Beat Gate → same for **Gates Beyond**.
- [ ] Beating the same boss again shows no reveal screen.
- [ ] To re-test from a clean slate, delete the PlayerPrefs keys `StyxUnlock_StringsOfFate` / `StyxUnlock_GatesBeyond` (registry on Windows) or call `PlayerPrefs.DeleteKey` from a debug script.

### 4. Run-start selection
- [ ] With ≥1 unlock, after picking the starter devotion you get the "Gifts Of The Styx" screen. Take both / one / neither, then Begin Run. Chosen trinkets are active in combat (Strings of Fate removes the first discarded card each combat — and that removal shows up on the Styx tab).
- [ ] With no unlocks, the run starts directly as before.

### 5. Styx node
- [ ] After beating Fates or Gate, the next node (R=12) is the Styx hex (blue/white temple placeholder visuals).
- [ ] Entering it opens the Styx screen: Styx deck grid, "Your New Trinkets" strip (trinkets removed this run), ritual list (click to select up to 2 — first shows "Major", second "Minor"; clicking a third drops the oldest pick).
- [ ] "Cross The Styx": your deck becomes the Styx deck, trinkets become the removed trinkets, rituals become your picks. Your *old* deck/trinkets/rituals move into the removed pool (verify on the Status → Styx tab afterwards).
- [ ] Gates Beyond gained/lost via the swap correctly enables/disables sacrificing.

### 6. Gates Beyond sacrifice
- [ ] With Gates Beyond, the Current tab shows "Sacrifice" buttons on rituals and trinkets. Each click: item moves to the Styx tab, +1 heartstring (visible in next combat).
- [ ] Buttons are disabled at 5/5 heartstrings.
- [ ] Sacrificing Gates Beyond itself removes the ability to sacrifice further.

## Known gaps / things I couldn't do
1. **Trinket icons** — there are no `StringsOfFate.png` / `GatesBeyond.png` in `Assets/Resources/Images/Icons/Trinkets/`, so the unlock/select/status screens show a purple placeholder square. Drop PNGs with those exact names into that folder and they'll be picked up automatically (`Trinket.GetDescriptionData()` strips the "Trinket" suffix from the class name).
2. **Visual polish** — all new UI is generated at runtime by `StyxUI` (flat colored panels, default TMP font). Positions/sizes are defined in the handler scripts; tweak the constants there, or replace the runtime UI with authored UI later. The 3D areas/cameras can be repositioned freely in the editor — the handlers only reference them.
3. **Styx hex art** — placeholder (Temple mesh, Blue/White materials). Add a `Styx.fbx` mesh / dedicated materials and update `OverworldMapNode.GetMeshKeyForEncounter` + `MaterialsByEncounterType`.
4. **Play-mode verification** — I cannot run Unity; the YAML edits follow the existing block formats exactly, but please load the scene before committing further work.

## Fallback: rebuilding the scene objects by hand
If the scene fails to load or objects look wrong, delete the five `*Screen` objects under UniversalCanvas and the `StyxArea`/`StatusArea` roots, then recreate in the editor:

1. **Five screens** (children of `UniversalCanvas`, each stretched full-rect, layer UI):
   - Components: `CanvasGroup` + `Screen` + the matching handler (`StyxScreenHandler`, `TrinketUnlockHandler`, `StyxTrinketSelectHandler`, `StatusScreenHandler`, `StatusButtonScreenHandler`).
   - `Screen.Name`: Styx / TrinketUnlock / StyxTrinketSelect / Status / StatusButton. `Screen.UIHolder` = its own CanvasGroup. `ManualHide` = off.
   - Keep them ordered **before** `PopupUI`.
2. **Two areas** (scene roots, start inactive), far from other content (e.g. x=700/800, y=300):
   - Child `CardGrid` at local (-34, 14, 10) with `ViewCardScroller` (Width 40, Height 30, CardWidth 7.64, CardHeight 9.17, CardMargin 1.32, CardScale 1, scrollIncrement 5, scrollSpeed 40).
   - Child `Camera` at local (0, 0, -36): disabled, solid-color clear (dark), depth 10, plus `UniversalAdditionalCameraData`.
3. **Wiring**: `StyxScreen` → `Screen.Camera` = StyxArea camera; `StyxScreenHandler.StyxArea` = StyxArea, `.CardScroller` = its grid. Same pattern for `StatusScreen`/`StatusArea`.
4. Nothing needs to be added to the ScreenHandler list or the Controller inspector — both are resolved in code at runtime.
