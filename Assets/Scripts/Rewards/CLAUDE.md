# Rewards notes

- Ritual reward offers (`RitualRewardHandler.Load`) prefer rituals not yet seen this run: `ProgressionHandler.MarkRitualSeen/HasSeenRitual`, filled with the player's current rituals and every pair offered (including fixed event/tutorial offers). When unseen ones run out it falls back to seen ones, so two are always offered.
- The seen-set is saved as `RunSaveData.seenRituals` (class names, via `ProgressionHandler.Write/ReadRunProgress`). A new run gets a fresh `ProgressionHandler`, so it resets per run; saves from before the field load with an empty set.
