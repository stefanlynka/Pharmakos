# VFX notes

- **Post-processing pulses:** `PostFXPulse` makes its own GameObject and runtime global Volume (priority 100) on first use
  and animates that volume's weight; settings come from `Resources/Content/PostFXPulseSettings`. Triggers today:
  human damage in `ChangeStatsAnimation`, ritual moment in `LightingHandler.ShakeScreen`. View code only: never call it
  from game logic (the AI simulates off the main thread). Full post-processing guide: `Docs/PostProcessing.md`.
