# Ritual invocations (sound + VFX)

What plays when a ritual is used. Entry point: `Ritual.GetPlayVfx` → `VfxLibrary.RitualInvocation` →
`RitualInvocationVfx` (queued by `PlayRitualAction` as a `PlayVfxAnimation`; AI rituals get `RitualUsePulseAnimation` first).

- **Timeline is shared by sound and VFX.** Notes fall on `RitualInvocationVfx.MajorBeats` (0 / 0.40 / 0.90 s) or
  `MinorBeats` (0 / 0.60 s); the last beat is the climax (flash, shockwave, particles, shake, tinted `PostFXPulse.Ritual`,
  lightning if the theme asks). The queue is released `ReleaseAfterClimax` (0.15 s) after it, so the ritual's own effect
  animates during the tail. Change the beats in **both** `RitualInvocationVfx` and `Tools/Audio/build_ritual_sounds.py`.
- **Pace:** `RitualTheme.Pace` (1 = standard) scales everything for that god: beats, tail, particles, shake, lights,
  post pulse. Its clips must be rendered at the same pace: `TEMPO` in `build_ritual_sounds.py` (pace, last-note
  scale; the last-note scale shortens the final notes further). Poseidon is at 0.75 as the trial for a faster set.
- "Major" = class name ends in `Major`, plus `OldOnesMinor` (`RitualInvocationVfx.IsMajor`). Minors play smaller
  (`MinorScale`), with 60% of the particles and `AudioHandler.MinorRitualVolumeScale`.
- **Look per god:** `RitualTheme` assets in `Resources/Content/RitualThemes/<God>.asset` (god from `Ritual.God`, fallback
  `Default`): glow colour, sigil texture, three particle layers (Burst / Drift / Extra), shake, lightning. Tune them in the
  inspector. `Tools → Rituals → Create Missing Ritual Themes` (`Editor/RitualThemeBuilder.cs`) only creates themes that
  don't exist, so delete an asset to get its starting values back.
- **Sound per ritual:** `Resources/Audio/SFX/Rituals/Invocations/<RitualClass>.wav`, played by
  `AudioHandler.PlayRitualInvocation` on its own AudioSource at `RitualInvocationVolume` (falls back to the old whoosh if a
  clip is missing). Rendered offline from VSCO 2 CE samples (CC0) by `Tools/Audio/build_ritual_sounds.py`; the samples
  are not needed at runtime. A new ritual needs a score function there (and an entry in `SCORES`).
- **Textures:** `Resources/Images/RitualFX` (sigils + particle sprites), built by `Tools/RitualFX/build_ritual_textures.py`.
  Sigils reuse the altar corner emblems from `Tools/Blender/altar_textures.py` (`CORNERS`), so a god's sigil matches its
  altar. Shader `Pharmakos/RitualGlow`; materials `Resources/VFX/Materials/RitualGlowAdd` / `RitualGlowAlpha`.
- **Lighting:** `LightingHandler.PlayRitualLighting` dims Left/Right lights and drives the EyeLight spotlight in the god's
  colour; the board's centre lights belong to a flicker script, so the real darkening comes from
  `PostFXPulse.RitualGather` (settings: `PostFXPulseSettings.RitualGather`), which hands over to the climax pulse.
  `DimLights`/`RestoreLights` (ritual aiming) cancel an invocation's lighting still fading out.
- The sigil hangs `SigilLift` toward the camera from the target and is clamped on screen (`KeepOnScreen`), so targets at
  the top edge (the enemy portrait) don't cut it off.
- **Preview (editor only):** in a running combat, `Tools → Rituals → Preview Every Ritual Invocation`, or
  `RitualInvocationPreview.Play("ZeusMajor", targetId, captureTimes, folder)` via execute_code (screenshots at those times).
