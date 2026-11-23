# Events

- Definitions are `EventDefinition` assets in `Assets/Resources/Content/Events`, listed on `EventHandler.Events` in the scene. Outcomes are dispatched in `EventHandler` by `EventOutcomeType`.
- `EventHandler.BeginRandomEvent` filters the pool by `IsEventAvailable`: any event with a `BecomeLastEnemy` option (Pygmalion) only appears once `ProgressionHandler.HasLastFoughtEnemy` is true.
- "Last fought enemy" is recorded in `ProgressionHandler.RecordLastFoughtEnemy` (called from `Controller.LoadLevel` after `LoadEnemy`). Tutorial runs, tutorial decks and the test chamber are skipped. Archiereus (Rebuke the Gods) counts.
- Archiereus rolls its rituals at fight time, so the fought rituals are snapshotted by class name (`LastFoughtMajor/MinorRitualName`, saved in `RunSaveData`). `ApplyBecomeLastEnemy` uses the snapshot; older saves without it fall back to the enemy's details.
