# Play history

**Model** (`PlayHistoryHandler.cs`, canon game only, never in AI simulations):
- `Controller.PlayHistoryHandler` keeps this turn's and last turn's entries, newest first (`ProgressPlayHistory` at end of turn drops the older turn).
- Plays, attacks and rituals: their actions (`PlayFollowerAction`, `PlaySpellAction`, `AttackWithFollowerAction`, `PlayRitualAction`) return an item from `MakePlayHistoryItem()`, added in `GameAction.Execute` (non-simulated). They also set `AddsPlayHistoryEntry`.
- Triggered abilities, trinkets and ritual buffs are `TriggeredEffectPlayHistory` records:
  - Follower abilities: every trigger site calls `TriggeredFollowerEffectInstance.TriggerAll(...)` (or `TriggerFromEvent` for the OnAny* events), which opens `ActionHandler.BeginTrigger(...)`. Actions added while the scope is open get the record in `GameAction.HistoryRecord`; actions added while a stamped action executes inherit it.
  - Source is `EffectDef.EffectSource` (the follower whose ability it is), not the follower the instance sits on: Melpomene's "adjacent dies" is an On Death instance on each neighbour, so its record is labelled "Ally Died".
  - Trinkets / ritual buffs: actions with a `SourceEffect` get the effect's record when they execute (`PlayHistoryHandler.GetEffectRecord`, one record per effect per action chain). Effects that act without an action call `StaticPlayerEffect.MarkTriggered()`, which records too; the animation path uses `FlareTriggered()` (flare only).
  - A record enters the history when its first action resolves; each action adds what it hit or made via `AddPlayHistoryResults` (damage target, card added to hand, summoned follower...). Records with no resolved action never show. `UpdateStatsInstance` (aura recalculation) opts out via `RecordsInPlayHistory`.
- Ordering: reactions can resolve before the play that caused them (a spell's On Targeted triggers, Siren's On Attacked). `Spell.Play` (`BeginPlay`/`EndPlay`), `Follower.AttackTarget` and any `AddsPlayHistoryEntry` action call `ExpectEntry`; records are held until that entry arrives and are inserted above it, or released when the action queue empties.
- Entries are grouped by `TurnPlayer` (stamped from the canon current player when added), not `Owner`, so an enemy's reaction stays under "Your Turn".
- Twist of Fate sets `Spell.GrantedTrinket`; `PlaySpellPlayHistory` reads it lazily (it is set after the entry is made) and shows the buff's icon.

**View** (`ContentScrollView` + `ViewPlayHistoryItemNew`, prefab `Resources/Prefabs/PlayHistory/PlayHistoryItem`):
- A 3D scene (`DeckViewerArea`, shared with the deck viewer) rendered into the PlayHistoryScreen's RawImage; the camera looks straight down.
- Rows read as a sentence in fixed columns: source, verb icon + label, results. A header row ("Your Turn" / "Enemy Turn") starts each turn. Row/header depths, columns and scales are fields on the prefab (it overrides the code defaults). The main section lays rows out with per-item depths (`Section.ItemDepths`); `PlayHistoryLayout.ItemDepth` is ignored.
- Raised objects (altar, portrait frame) are squashed along the view axis by their holder (`DepthFlatten`) and the portrait's board lean is undone, or perspective shows their sides. Rituals have their spotlight disabled.
- Trinket / buff icons and trigger icons (`Images/Icons/SummaryMono`) are built in code; names reuse the verb label's style.
- To test without playing: in play mode, `StartTestChamber()`, then add items with `PlayHistoryHandler.AddPlayHistoryEntry` / real actions and call `Controller.Instance.LoadPlayHistory()`. Scrolling for screenshots: set `_mainSection`'s scroll offsets by reflection and call `ApplyCameraScroll`.
