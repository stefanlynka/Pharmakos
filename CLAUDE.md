# Hecatomb (repo: Pharmakos)

The game's release title is **Hecatomb**; "Pharmakos" is only the repo/project name. Single-player roguelike deckbuilder with a Greek-myth theme, built in Unity (2022.3.62f2, URP 14). The player runs an overworld map of nodes (combat, temple, shop, event, River Styx), building a deck of Followers and Spells plus Rituals and Trinkets, and fights an AI opponent in card combat paid for with Offerings (Gold, Blood, Bone, Crop, Scroll).

**Status:** shipping on Steam. A demo is live (released for Steam NextFest) and its bugs have been fixed. **Current priority: visual cohesion, polish and feel**, so the game looks and plays like a professional release. Weigh changes by how much they improve presentation without destabilising gameplay.

**Who does what:** the developer (Stefan) owns game design and most content changes (cards, rituals, trinkets, events, balance). Agents mostly work on visuals, UI, VFX, animation, tooling and code quality. Don't change card text, stats or balance unless asked.

**Visual changes** must follow [Docs/StyleGuide.md](Docs/StyleGuide.md) (palette, two typefaces, interaction states, UI components). If a change needs something the guide doesn't cover, propose an addition to the guide.

## Working with the editor (MCP for Unity)

- The Unity editor is reachable through the `UnityMCP` server (`http://127.0.0.1:8080/mcp`). It only works while the editor is open with MCP for Unity started; if the tools are missing, ask the user to open Unity / start the server and reconnect.
- After editing any `.cs` file, check `read_console` for compile errors before claiming the change works. Unity recompiles on focus/refresh; use `refresh_unity` if needed.
- Everything lives in one scene: `Assets/Scenes/GameScene.unity`. Screens are toggled, not loaded. Don't create new scenes.
- Prefer editing C# over hand-editing `.unity` / `.prefab` / `.asset` YAML. If a scene or prefab change is needed, make it through MCP tools and save the scene.
- New scripts need `.meta` files; let Unity generate them (refresh after creating the file) rather than hand-writing GUIDs. Never delete or regenerate existing `.meta` files — scene references break.

## Code layout

All gameplay code is in `Assets/Scripts` (default `Assembly-CSharp`, no asmdefs, mostly global namespace).

| Area | What's there |
|---|---|
| `Controller/Controller.cs` | `Controller.Instance` singleton: run flow, screen navigation, node routing, run state (seed, heartstrings, Styx state). Very large — search before adding. |
| `Controller/GameState.cs` | Combat state: both players, `ActionHandler`, `TargetsByID`, RNG, game events (`FollowerDies`, `SpellPlayed`, ...). Deep-copyable. |
| `Actions/` | `GameAction` subclasses (`Actions/Actions/`), `ActionHandler` queue, AI `PlayerDecision`s, and the view-side `AnimationAction`/`AnimationHandler`. |
| `Cards/` | `Card` → `Follower` / `Spell`. Every card is its own class in `CardList.cs`; decks are built in `CardHandler.cs`. |
| `Rituals/` | Each god's Major/Minor ritual is a class in `RitualList.cs` (see `Rituals.txt` at repo root for the design list). |
| `EffectSystem/` | Follower/player effect defs + instances; trinkets in `TrinketList.cs`. |
| `Players/` | `Player`, `HumanPlayer`, `AIPlayer` (search-based planning), `ProgressionHandler` (decks/encounters). |
| `View/`, `VFX/`, `Screens/` | MonoBehaviour presentation: `View.Instance`, screen handlers, highlights, animations. |
| `Overworld/`, `Styx/`, `Rewards/`, `Tutorial/`, `Save/` | Map, River Styx node, reward screens, scripted tutorial, run save/load. |
| `Assets/Events/` | `EventDefinition` ScriptableObjects (`Pharmakos.Events` namespace); instances in `Assets/Resources/Content/Events`. |

## Core architecture rules

**Model / view split.** Game logic (`GameState`, `Player`, `Card`, `GameAction`) is plain C#. Visuals happen only through `GameAction.GetAnimationActions()` → `AnimationHandler` queue. Don't touch GameObjects from game logic.

**Everything that changes combat state is a `GameAction`.** Add it via `GameState.ActionHandler.AddAction(...)`. A new action must:
- implement `DeepCopy(Player newOwner)` — remap every `ITarget`/`Card` reference through `newOwner.GameState.GetTargetByID<T>(id)`; never share references across copies;
- call `base.Execute(simulated)` at the end of `Execute` (queues animation + play history when not simulated);
- return any visuals from `GetAnimationActions()`.

**The AI simulates on deep copies, off the main thread.** `AIPlayer` plans by copying `GameState` (`new GameState(original, simulated: true)`) and running actions with `simulated = true` in a background `Task`. Therefore:
- any new state field on `GameState`/`Player`/`Card`/effects must be handled in the deep-copy constructors, or the AI will read stale/shared data;
- code reachable during simulation must not call Unity APIs (`GameObject`, `Transform`, `View.Instance`, `Debug.Log` spam in hot paths) and must use `GameState.RNG`, not `UnityEngine.Random`;
- guard view/history side effects with `if (!simulated)` / `IsSimulated`.

**Determinism.** Use the seeded `CustomRandom` (`GameState.RNG`, `Controller.MetaRNG`) for anything gameplay-affecting so seeds and saves reproduce.

**Click protection.** `Utility/InputGuard` blocks clicks while a `ScreenTransitionAnimation` is in flight (from construction until the new screen is revealed, with a 5 s safety cap) and for 0.35 s after any button click (via `ButtonFeedback`). UI is blocked automatically by an invisible top-most overlay; any code that reads the mouse directly (`Input.GetMouseButtonDown` + raycasts) must check `InputGuard.IsBlocked` before acting on a press.

## Adding content (usually done by the developer; here for reference)

- **Card:** new class in `Cards/CardList.cs` deriving from `Follower` or `Spell`; set `Costs`, stats, `Text`, `Icon`; add effects in `SetupInnateEffects()`; register it in the relevant deck/pool in `CardHandler.cs`.
- **Ritual:** class in `Rituals/RitualList.cs` (+ `StaticPlayerEffect` def if it has a lasting effect). Keep `Rituals.txt` in sync. Its altar's look comes from its god (`Ritual.God`, read from the class name) via an `AltarStyle` in `Resources/Content/AltarStyles`; see `Docs/AltarKit.md`.
- **Trinket:** `Trinket<TEffectDef>` + `TrinketPlayerEffect` pair in `EffectSystem/TrinketList.cs`.
- **Event:** `EventDefinition` asset under `Assets/Resources/Content/Events`.

## Saves — don't break them

Run saves (`Save/RunSave.cs`, JSON in `Application.persistentDataPath/run-save.json`) store cards, rituals and trinkets **by class name** and are rebuilt by reflection in `RunContent`. Renaming or removing one of those classes breaks existing saves. Adding a field to `RunSaveData` is safe (older saves load it as its default), so don't bump `RunSaveData.CurrentVersion` for that: `RunSave.TryLoad` rejects any version mismatch, so a bump makes every existing save un-continuable. Bump only for changes old data can't survive (renamed/retyped fields, changed meaning), ideally with a migration; see `Assets/Scripts/Save/CLAUDE.md`. Permanent unlocks live in `PlayerPrefs` (`StyxUnlocks`). Tutorial runs must stay save-compatible.

## Debugging

- Static flags on `Controller`: `ActionDebugMode`, `AnimationDebugMode`, `AIDebugMode`, `ShowCardIDs`.
- `Controller.StartTestChamber()` starts a combat directly with the `TestPlayer` deck (skips overworld/progression). The main menu has a Test Chamber button that the developer deactivates before release builds — never leave it active, and keep any dev-only shortcuts out of player builds.
- Dev hotkeys live in `Controller.HandleDevHotkeys()`, wrapped in `#if UNITY_EDITOR || DEVELOPMENT_BUILD` so release builds strip them. **Ctrl+Shift+T** on the main menu starts the test chamber. Add new dev shortcuts there.
- **Test chamber = ritual test bench:** your rituals are free there (`Player.FreeRituals`, set only for the human in the test chamber), and **P** swaps your two altars to the next god's Minor + Major (`TestChamberRitualPairs`; gods with only one ritual share a pair). **I** / **O** step back / forward through every encounter board (ground, lanes, back edge).
- To verify behaviour: enter play mode via MCP (`manage_editor`), reach the state you need (calling methods like `StartTestChamber()` via `execute_code` is easier than clicking through UI), and read the console.
- **Working folder:** put screenshots, previews, comparison pages and other agent working files in `Claude/` at the repo root (e.g. `Claude/FontPreview/`). Never use `Temp/` — Unity wipes it when the editor closes. The developer also drops reference screenshots in `Claude/`.
- **Screenshots are allowed freely** for checking visual work (the developer said so). `CaptureScreenshot` writes at the end of the frame: capture in one call and read the file in a later one, and use a new filename each time or you may read the previous image. `ScreenCapture.CaptureScreenshot` via `execute_code` captures exactly what the Game view shows, including overlay UI and 3D cameras; `manage_camera` screenshots can miss overlay UI or stacked cameras.
- **Run saves are fair game** for testing: agents may load, overwrite or delete `run-save.json` without backing it up. `Controller.Instance.ContinueRun()` resumes it; private encounter starters (e.g. `BeginTempleEncounter`) can be invoked by reflection to reach a screen directly.

## Conventions

- **The inspector is the source of truth.** Don't write code that silently overrides values the developer sees and edits in the inspector (e.g. replacing a component's serialized preset at runtime). If something must change at runtime, leave the authored field alone and make the runtime value clearly separate.
- **Content lives together** under `Assets/Resources/Content` (events, follower layouts, highlight presets and the `HighlightPalette`). Put new content ScriptableObjects there.
- C# style: PascalCase public members, `camelCase`/`_camelCase` private, Allman braces, 4-space indent. Match surrounding code.
- Remove stray auto-added usings like `using static Unity.VisualScripting.Member;` / `UnityEngine.GraphicsBuffer` when touching a file — they're IDE accidents.
- `FindObjectOfType` is used in places; fine on 2022.3, but prefer serialized references or existing singletons (`Controller.Instance`, `View.Instance`, `ScreenHandler.Instance`).
- There are no automated tests yet.
- **Git:** work directly on `main`. Agents may commit finished, verified chunks of work (one logical change per commit, existing message style). Never push.
- One-off working notes (feature checklists, plans) are temporary: delete them once the feature is finished. Keep lasting design references like `Rituals.txt`.
- **Subsystem notes:** when you learn something non-obvious about a subsystem (how it flows, where state lives, gotchas), add it to a `CLAUDE.md` in that subsystem's folder (e.g. `Assets/Scripts/Rituals/CLAUDE.md`) before finishing; create the file if needed. These load automatically when a session works in that folder. Keep them short and verified, and fix or delete anything you find stale. Project-wide rules stay in this root file.

## AFK mode

When the developer says they're going AFK (e.g. "Do X, I'm going afk"), they can't answer questions and you have as much time as you need. Work autonomously:

- **No questions — decide and log.** Make the call with best judgement and record each judgement call (what you chose, the alternative, how to undo it). Between close options pick the most reversible. For visual choices it's fine to build both variants and leave side-by-side screenshots in `Claude/` for the developer to pick.
- **Finish X properly first.** Compiles cleanly, verified in play mode, screenshots taken and actually looked at, edge cases tried. Thorough verification is the main value of unattended time.
- **Then keep going, in this order:**
  1. Natural follow-ons to X (the same fix on a sister screen, gaps found along the way).
  2. Items from `Claude/Backlog.md` (developer-approved ideas), top first.
  3. Low-risk cleanup in the agent lane: subsystem `CLAUDE.md` notes, small code-quality fixes.
  Don't invent large self-directed features beyond these.
- **One commit per finished, verified chunk**, so any part can be reverted on its own. Stage only your own files — never commit the developer's uncommitted work in progress.
- **Hard limits, regardless of judgement:** no card text/stat/balance changes, no save-format changes, no deleting the developer's work, no pushing, nothing sent or posted outside the repo, nothing that risks the release build (dev shortcuts, Test Chamber). If the next step crosses one of these, stop that thread and move to the next.
- **If something blocks you** (Unity/MCP down, a permission prompt you can avoid), switch to work that doesn't need it and note what couldn't be verified.
- **Before going quiet:** ask the app to keep the machine awake for the session if that's available.
- **When done:** send a push notification if available, and finish with a report: what was done, how it was verified, the decision log, anything needing the developer's eyes, and what you'd do next.

## Third-party assets

Purchased store assets, free to use, edit or remake as needed: `Assets/Vefects`, `Assets/Hovl Studio`, `Assets/Stylized Terrain Textures`, and Linework Lite's Free Outline (`Packages/dev.ameye.linework-lite`, settings in `Assets/Outlines`; it already has Unity 6 render-graph support). Prefer copying before heavily modifying a shared asset that many prefabs use.

Leave alone unless asked: `Assets/ai.meshy`, `Assets/TextMesh Pro`, `Packages/com.merry-yellow.code-assist`.
