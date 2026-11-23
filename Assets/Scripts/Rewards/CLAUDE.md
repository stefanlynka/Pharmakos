# Rewards notes

- Ritual reward offers (`RitualRewardHandler.Load`) prefer rituals not yet seen this run: `ProgressionHandler.MarkRitualSeen/HasSeenRitual`, filled with the player's current rituals and every pair offered (including fixed event/tutorial offers). When unseen ones run out it falls back to seen ones, so two are always offered.
- That seen-set is in memory only. A new run gets a fresh `ProgressionHandler`; a resumed save starts with an empty set (persisting it would need a `RunSaveData` change and version bump).
