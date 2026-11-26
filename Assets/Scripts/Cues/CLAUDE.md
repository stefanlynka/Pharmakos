# Combat cues (trigger flashes, cue sounds, spell casts)

Presentation only: nothing here changes game state, and none of it runs in AI simulations.

## Triggered abilities (`TriggerCue`)

- Every follower trigger already opens `ActionHandler.BeginTrigger` (play history); that also opens a **cue scope**. Start/end-of-turn follower effects (Helios, Scylla, Charybdis, Minotaur, Hydra) get one from `Player.StartTurn` / `DoEndOfTurnActions` via `ActionHandler.BeginCue`.
- Actions added while a scope is open are stamped `GameAction.Cue` (direct results only, not actions they cause later). `UpdateStatsInstance` (stat auras) opts out with the history (`RecordsInPlayHistory`), so auras stay silent; static keywords never trigger.
- A cue whose ability did nothing never plays. Abilities that store a reward for later (Oracle, Cassandra) call `ActionHandler.MarkTriggerActed()`.
- **Placement** (the order is what makes combos readable): just before the first animation of its first action (`AnimationHandler.AddGameActionToQueue` → `TakeAnimationForPlacement`). If no action animates, once they have all run; if it only `MarkTriggerActed`, right after the action that caused it. A follower's **own On Death** cue is queued the moment it fires, so it lands after the lethal damage and before the death animation (developer's rule). Via-ally On Death abilities (Keres, Charon, Melpomene) play after the neighbour's death.
- `AddAction(action, true, true)` is *front of queue*, not forced evaluation.
- `TriggerCueAnimation`: plays the cue sound (`CuePlayer`), flashes the source card in the `HighlightPalette.Triggered` preset (votive gold, `CardHighlightEffect.Flare(preset, duration)`) and pulses its `CardHolder` (`TriggerFlash`), then holds the queue for a short beat (`CuePlayer.Beat`, non-stackable). Dying sources hold until the pulse ends so the death animation gets a clean `CardHolder`.
- The flare runs on **unscaled** time, so it won't show when filming at a slow `Time.timeScale`; film flashes at 1.

## What plays (`CueLibrary`, `SpellCues`)

- One table, keyed by class name. Follower abilities resolve: effect def (`LifeSiphonEffectDef` → Siphon) → the trinket/buff behind an action's `SourceEffect` → card (`"Card.Trigger"` then `"Card"`) → a **family inferred from the actions** (`InferFamily`: damage → Strike, gold → Gold, summon → Summon, stats ± → Empower/Weaken, draw → Draw...). New content works without an entry.
- Projectiles for abilities and buffs are set view side in `CueLibrary.DecorateForView` (damage) / `DecorateAnimations` (Erinyes' life loss), not in content code. Siren water, Chimera fire, Erinyes ink, Hera Major blood, Tunic of Nessus fire.
- Trinkets and ritual buffs: `View.OnPlayerEffectTriggered` → `CueLibrary.PlayEffectCue` (once per 0.3 s per effect). Repeatable ritual buffs need their actions' `SourceEffect = this` to flare and sound (Demeter, Hermes, Hera, Poseidon Majors set it).
- Spells: `PlaySpellAction.GetAnimationActions` → card arrives, `CastSoundAnimation`, VFX (`Spell.GetPlayVfx` wins over `SpellCues`), projectile (`GetThrowStyle` wins), Twist of Fate's buff reveal, wait, remove. DevKill is silent. Spells without an entry play `CastSpell`.
- **Tools → Cues → Coverage Report** writes `Claude/Cues/CueCoverage.md`: every follower / spell / effect and what it resolves to, plus missing or unused sound files. Run it after adding content.

## Sounds (`CuePlayer`)

- Clips: `Resources/Audio/SFX/Cues/<Id>.wav` (variants `<Id>_2`... rotate), rendered by `Tools/Audio/build_cue_sounds.py` from the CC0 VSCO samples in `Tools/Audio/Samples` plus synthesis. No voices. `--preview <folder>` writes an HTML page with players and spectrograms.
- Trigger cues are short (hold ~0.22 s then fade; `BREVITY` lets rare ones ring longer). Spell casts are longer. Levels are mastered per sound (`LEVELS`), then scaled by `AudioHandler.CueVolume` / `SpellCastVolume`.
- Repeats of one sound within 0.75 s climb a semitone each (max 7), get slightly quieter and shorten their beat, so combos read as a run of notes. Played on `AudioHandler.PlayPitched` (a small voice pool; `PlayOneShot` can't pitch per shot).
- Timings shared with VFX are constants on both sides: `HEARTBEATS` ↔ `HeartbeatVfx.Beats` (Price Of...), `FATE_STRIKE` ↔ `TwistOfFateVfx.GatherTime`, `GNAW_CLOSE` ↔ `BiteVfx.CloseTime`.

## VFX (`StrokeProjectile.cs`, `CueVfx.cs`)

- Everything is painted brushstrokes (`BrushStrokeReveal` shader, state in vertex colour), facing the camera. Stroke sets: `Images/Particles/Water`, `Fire` (painted), `Blood`, `Ink`, `Gold` (recoloured from Water by `Tools/VFX/build_stroke_sets.py`; they must import as Sprite, mesh Tight).
- `ArcProjectileStyle.StrokeFolder` makes `ArcProjectileAnimation` paint the projectile instead of instantiating a prefab; it bursts on impact and plays `ImpactCue`.
- Sizes and offsets are written for a card ~2.5 units wide and scaled by `CueVfxUtil.Unit` (board cards are ~6.5 units wide, so Unit ≈ 2.6). Always multiply world offsets by it.
- Bespoke: `FearVfx` (Panic), `RoarVfx`, `BiteVfx` (Gnaw), `HeartbeatVfx` (Price Of...), `TwistOfFateVfx` (+ `PostFXPulse.TwistOfFate`, settings in `PostFXPulseSettings`).

## Testing

`Assets/Scripts/Editor/CueTestBench.cs` from MCP `execute_code` in play mode: `Stage("Hoplite,Keres", "Siren,Charon")` (starts the chamber, 60 health, clears and summons), `Attack`, `Damage`, `Cast(human, "Roar", "player:human")`, `DumpQueue(from)` (the animation order, cues show source/trigger/sound), `Film(step, count, timeScale)` → `Claude/Cues/Shots/film_NN.png`, contact sheet with `python Claude/Cues/film.py name x0 y0 x1 y1 [cols]`. The test chamber starts players at 1 health, so stage before anything can hit a portrait. `Controller.AnimationDebugMode = true` logs every animation as it plays (cues included).
