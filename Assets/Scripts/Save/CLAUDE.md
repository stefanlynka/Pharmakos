# Save notes

- Saves are `JsonUtility` JSON. Adding a field to `RunSaveData` is backward compatible: older saves just leave it at its initializer value. Mark such fields with an "Added after v1" comment and make the default mean "nothing recorded".
- Don't bump `RunSaveData.CurrentVersion` for additive fields. `RunSave.TryLoad` rejects any save whose version doesn't match exactly, so a bump makes every existing save un-continuable. Bump only for changes old data can't survive (renamed or retyped fields, changed meaning), and add a migration if those saves should still load.
