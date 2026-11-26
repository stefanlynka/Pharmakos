# Styx / Status screen notes

- **The status screen is a prefab** (`Assets/Prefabs/StatusScreen.prefab`) instanced in `GameScene`. Edit layout in the prefab, not the scene. The scene instance overrides the two tab labels' `fontSize` (15.55); the labels use auto-size, so that override is inert. Don't fight it by editing the scene.
- `StatusScreenHandler` only builds the side sections at runtime: each is a `RawImage` that `ContentScrollView` renders a 3D section into, with a clone of `SectionFrameTemplate` (plaque frame, Fill Center off) stretched over it. Heartstring pips are clones of `HeartstringPipTemplate` in `HeartstringPips`. Titles, subtitles and the Styx tint are inspector fields.
- **Styx tab look:** `ContentScrollView.SetBackdropTint` multiplies the carpet backdrop (main view and side-section copies) through a `MaterialPropertyBlock`. `ContentScrollView.Clear`/`Show*`/`Hide` reset it to white, so the deck viewer and play history never inherit it; call it after showing content.
- Side sections stretch their copy of the carpet (`ContentScrollView.SideCarpetWidthScale`) so the carpet's meander borders stay out of their narrow views.
