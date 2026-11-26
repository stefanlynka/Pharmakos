"""Renders the combat cue sounds: triggered abilities, trinkets and ritual buffs, projectile impacts and spell casts.

Rules (see Assets/Scripts/Cues/CLAUDE.md):
  * Trigger cues are short (mostly under 0.4 s) and simple: combos fire the same one many times in a row, and
    CuePlayer climbs a semitone per repeat, so each must still sound right a fifth higher and stacked.
  * No voices, no words. Traditional instruments (VSCO 2 CE / VSCO 1 percussion, CC0, in Tools/Audio/Samples) and
    simple synthesis (noise, sines), in the same small dark stone room as the death and ritual sounds.
  * Families share a sound (every gold gain is Gold); spells get their own cast sound. The Price Of... casts share a
    heartbeat (HEARTBEATS, matched by HeartbeatVfx.Beats) with a different coda each; Twist of Fate's threads land on
    FATE_STRIKE (TwistOfFateVfx.GatherTime); Gnaw's jaws close on GNAW_CLOSE (BiteVfx.CloseTime).

    python Tools/Audio/build_cue_sounds.py [--only Gold CastFate ...] [--preview <folder>] [--seed N]
Output: Assets/Resources/Audio/SFX/Cues/<Id>.wav (44.1 kHz, 16-bit stereo). --preview writes an index.html with
players and spectrograms. Levels per sound are in LEVELS; AudioHandler.CueVolume / SpellCastVolume scale them in game.
Requires numpy (and Pillow for --preview).
"""
import argparse
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_ritual_sounds as rs  # noqa: E402
import build_death_sounds as ds  # noqa: E402

SR = rs.SR
OUT = os.path.join(rs.ROOT, "Assets", "Resources", "Audio", "SFX", "Cues")

HEARTBEATS = [0.0, 0.17, 0.62, 0.79]   # HeartbeatVfx.Beats
PRICE_CODA = 0.98
FATE_STRIKE = 1.05                     # TwistOfFateVfx.GatherTime
GNAW_CLOSE = 0.16                      # BiteVfx.CloseTime

HAND = "VSCO 1 Percussion/drums/other/ethnic/giant/hand/EthnicLargeHand_hit_f_1.wav"
HAND_SOFT = "VSCO 1 Percussion/drums/other/ethnic/giant/hand/EthnicLargeHand_hit_mp_1.wav"
TENOR = "VSCO 1 Percussion/drums/tenor/tenor_lower/tenor_fff_1.wav"
TENOR2 = "VSCO 1 Percussion/drums/tenor/tenor_lower/tenor_fff_3.wav"
BASS_DRUM = "VSCO 1 Percussion/drums/bass/bdrum3_fff_1.wav"
GONG_MF = "VSCO 1 Percussion/varMetal/Gong/gong_hit_mf.wav"
GONG_FF = "VSCO 1 Percussion/varMetal/Gong/gong_hit_ff.wav"
TRIANGLE = "VSCO 1 Percussion/varMetal/triangle/1/triangle1_hit_mp.wav"
CYMBAL_SWELL = "VSCO 1 Percussion/varMetal/Cymbals/susp/susp_hit_softmall_roll2_cresc.wav"
ANVIL = "Percussion/Anvil_Hit1_v2_Sum.wav"
ANVIL2 = "Percussion/Anvil_Hit1_v3_Sum.wav"
TIMPANI = "Percussion/Timpani/Timpani1_Hit_v3_rr1_Sum.wav"
TIMPANI_ROLL = "Percussion/Timpani/Rolls/Timpani1_Roll_v3_rr1_Sum.wav"
TUBE_C4 = "Percussion/TB_hit_C4_v4_rr1.wav"
THUNDER = os.path.join(rs.ROOT, "Assets", "Resources", "Audio", "SFX", "Thunder.wav")

# Loudness of each sound's loudest 0.1 s (dBFS RMS). Trigger cues sit under the cast sounds; repeats get quieter in game.
TRIGGER_DB = -19.0
IMPACT_DB = -21.0
CAST_DB = -16.5
LEVELS = {
    "Chime": -22.0, "Boon": -21.0, "Siphon": -21.5, "Draw": -20.5, "Empower": -20.0, "Weaken": -20.0,
    "Doom": -17.5, "Maelstrom": -18.0, "CastFate": -15.0, "CastRoar": -15.0, "CastTitans": -15.5,
    "CastSpell": -19.0, "CastScry": -18.5, "CastHerald": -18.0,
}


# ------------------------------------------------------------------------------------------ helpers ---
def secs(n):
    return np.arange(n) / SR


def stereo(mono):
    return np.stack([mono, mono], axis=1)


def noise(rng, seconds):
    return rng.standard_normal((int(seconds * SR), 2))


def decay(seconds, tau, attack=0.002):
    t = secs(int(seconds * SR))
    return (np.minimum(t / max(attack, 1e-4), 1) * np.exp(-t / tau))[:, None]


def swell(seconds, peak, release):
    """Rises to 1 at `peak`, then falls to 0 over `release`."""
    t = secs(int(seconds * SR))
    up = np.clip(t / max(peak, 1e-3), 0, 1) ** 2
    down = np.clip(1 - (t - peak) / max(release, 1e-3), 0, 1) ** 1.5
    return np.where(t < peak, up, down)[:, None]


def sweep_filter(x, lo0, lo1, hi0, hi1, frame=1024, hop=256):
    """Band-pass whose edges glide from (lo0, hi0) to (lo1, hi1) Hz over the clip (STFT overlap-add)."""
    n = len(x)
    out = np.zeros_like(x)
    norm = np.zeros(n)
    win = np.hanning(frame)
    f = np.fft.rfftfreq(frame, 1 / SR)
    starts = range(0, max(1, n - frame), hop)
    count = max(1, len(starts) - 1)
    for k, i in enumerate(starts):
        a = k / count
        lo = lo0 * (lo1 / lo0) ** a
        hi = hi0 * (hi1 / hi0) ** a
        mask = (1 / (1 + (lo / np.maximum(f, 1)) ** 4)) * (1 / (1 + (f / hi) ** 4))
        for c in range(2):
            seg = x[i:i + frame, c]
            if len(seg) < frame:
                seg = np.pad(seg, (0, frame - len(seg)))
            y = np.fft.irfft(np.fft.rfft(seg * win) * mask, frame) * win
            m = min(frame, n - i)
            out[i:i + m, c] += y[:m]
        norm[i:i + frame] += (win ** 2)[:min(frame, n - i)]
    return out / np.maximum(norm, 1e-3)[:, None]


def chirp(seconds, f0, f1, tau, curve=0.03):
    """A sine gliding f0 -> f1 (exponentially, time constant `curve`) with an exponential decay: a bubble or a zap."""
    t = secs(int(seconds * SR))
    freq = f1 + (f0 - f1) * np.exp(-t / curve)
    phase = 2 * math.pi * np.cumsum(freq) / SR
    return stereo(np.sin(phase) * np.minimum(t / 0.002, 1) * np.exp(-t / tau))


def bubbles(rng, seconds, count, lo=500, hi=1400, spread=None):
    """Water: short rising sine blips (each bubble's resonance climbs as it closes)."""
    out = np.zeros((int(seconds * SR), 2))
    spread = seconds * 0.8 if spread is None else spread
    for _ in range(count):
        at = rng.uniform(0, spread)
        f = rng.uniform(lo, hi)
        b = chirp(0.06, f, f * rng.uniform(1.6, 2.4), rng.uniform(0.012, 0.03), curve=0.012)
        pan = rng.uniform(-0.7, 0.7)
        b = b * np.array([1 - pan, 1 + pan]) * 0.5 * rng.uniform(0.4, 1.0)
        i = int(at * SR)
        n = min(len(b), len(out) - i)
        out[i:i + n] += b[:n]
    return out


def crackle(rng, seconds, density, tau, lo=1800, hi=9000):
    """Fire or crumbling: sparse clicks of filtered noise, thinning out."""
    n = int(seconds * SR)
    out = np.zeros((n, 2))
    t = 0.0
    while True:
        rate = density * math.exp(-t / tau)
        if rate < 3:
            break
        t += rng.exponential(1 / rate)
        i = int(t * SR)
        g = int(rng.uniform(0.0006, 0.003) * SR)
        if i + g >= n:
            break
        grain = rng.standard_normal(g) * np.hanning(g) * rng.uniform(0.3, 1.0)
        pan = rng.uniform(-0.7, 0.7)
        out[i:i + g, 0] += grain * (1 - pan)
        out[i:i + g, 1] += grain * (1 + pan)
    return ds.bandpass(out, lo, hi)


def whoosh(rng, seconds, peak, f_lo, f_hi, up=True):
    """Air moving past: noise through a band that sweeps up (or down) and swells to `peak`."""
    x = noise(rng, seconds)
    if up:
        x = sweep_filter(x, f_lo, f_lo * 2.2, f_lo * 3, f_hi)
    else:
        x = sweep_filter(x, f_lo * 2.2, f_lo, f_hi, f_lo * 3)
    return x * swell(seconds, peak, seconds - peak)


def heartbeat(L, m, at, strength=1.0):
    """One lub-dub: a deep felt thump and a softer second one, as HeartbeatVfx pulses the card."""
    for k, (dt, g) in enumerate(((0.0, 1.0), (0.17, 0.62))):
        m.add(ds.sub(0.35, 78, 42, 0.07), at + dt, gain=0.9 * g * strength, align=False)
        m.add(ds.lowpass(L.file(HAND_SOFT, -9), 380), at + dt, gain=0.55 * g * strength, dur=0.2, release=0.1)


def place(m, x, at, gain=1.0, pan=0.0):
    m.add(x, at, gain=gain, pan=pan, align=False, norm=False)


# Trigger cues fade out after this long (hold, fade) so combos stay crisp; rare ones may ring longer.
BREVITY = {"Doom": (0.8, 0.5), "Maelstrom": (0.6, 0.4), "Revive": (0.5, 0.4), "Hope": (0.45, 0.35),
           "Fate": (0.5, 0.4), "Lyre": (0.45, 0.35), "Petrify": (0.45, 0.3)}
DEFAULT_BREVITY = (0.22, 0.22)


def finish(dry, name, wet=0.14, room=1.2):
    """Small dark room, trimmed, levelled by its loudest 0.1 s (LEVELS / family default), peak-limited to -1 dBFS."""
    if not name.startswith("Cast"):
        hold, fade = BREVITY.get(name, DEFAULT_BREVITY)
        t = secs(len(dry))
        dry = dry * np.clip(1 - (t - hold) / fade, 0, 1)[:, None] ** 2
    ir = rs.temple_ir(room, seed=13, decay=0.55)
    rev = rs.convolve(dry, ir)
    out = np.zeros_like(rev)
    out[:len(dry)] += dry
    out += rev * wet * 2.2
    out = rs.highpass(out, 40.0)
    env = np.abs(out).max(axis=1)
    # Trigger cues and impacts must stay brief (they repeat in combos): cut their tails at -36 dB, casts at -48 dB.
    floor = 0.004 if name.startswith("Cast") else 0.016
    last = len(env) - 1 - int(np.argmax(env[::-1] > env.max() * floor))
    out = out[:min(len(out), last + int(0.03 * SR))]
    fade = min(int(0.08 * SR), len(out) // 3)
    out[-fade:] *= np.linspace(1, 0, fade)[:, None] ** 2
    head = int(0.002 * SR)
    out[:head] *= np.linspace(0, 1, head)[:, None]

    if name in LEVELS:
        target = LEVELS[name]
    elif name.startswith("Impact"):
        target = IMPACT_DB
    elif name.startswith("Cast"):
        target = CAST_DB
    else:
        target = TRIGGER_DB
    win = max(1, min(int(0.1 * SR), len(out) - 1))
    mono = out.mean(axis=1)
    c = np.cumsum(mono ** 2)
    rms = math.sqrt(max((c[win:] - c[:-win]).max() / win, 1e-12))
    gain = min(10 ** (target / 20) / rms, rs.PEAK_CEILING / np.abs(out).max())
    return out * gain


# ------------------------------------------------------------------------------------ trigger cues ---
# Each takes (L, rng) and returns a dry stereo buffer.
def t_gold(L, rng):
    m = rs.Mix(0.9)
    m.add(L.note("Glockenspiel", 88, 80), 0.0, gain=0.8, pan=-0.15, dur=0.25, release=0.3)
    m.add(L.note("Glockenspiel", 95, 70), 0.045, gain=0.55, pan=0.2, dur=0.2, release=0.3)
    m.add(L.file(TRIANGLE, 5), 0.0, gain=0.25, dur=0.25, release=0.2)
    return m.buf


def t_life(L, rng):
    m = rs.Mix(0.9)
    m.add(L.note("Harp", 72, 70), 0.0, gain=0.7, pan=-0.2, dur=0.3, release=0.3)
    m.add(L.note("Harp", 79, 70), 0.07, gain=0.75, pan=0.2, dur=0.3, release=0.35)
    pad = chirp(0.6, 523, 523, 0.25) * swell(0.6, 0.08, 0.5) * 0.15
    place(m, pad, 0.0, gain=0.6)
    return m.buf


def t_lifeloss(L, rng):
    m = rs.Mix(0.8)
    m.add(ds.lowpass(L.file(HAND, -7), 500), 0.0, gain=0.9, dur=0.25, release=0.15)
    m.add(L.note("CelloEnsPizz", 45, 90), 0.02, gain=0.55, dur=0.2, release=0.2)
    m.add(L.note("CelloEnsPizz", 40, 90), 0.12, gain=0.5, dur=0.2, release=0.25)
    return m.buf


def t_siphon(L, rng):
    m = rs.Mix(0.7)
    suck = sweep_filter(noise(rng, 0.22), 300, 1200, 900, 4000) * swell(0.22, 0.2, 0.02)
    place(m, suck, 0.0, gain=0.35)
    m.add(L.note("Harp", 69, 60), 0.18, gain=0.5, dur=0.2, release=0.25)
    m.add(ds.lowpass(L.file(HAND_SOFT, -5), 500), 0.18, gain=0.3, dur=0.15, release=0.1)
    return m.buf


def t_empower(L, rng):
    m = rs.Mix(0.7)
    m.add(L.note("ViolinEnsPizz", 67, 90), 0.0, gain=0.6, pan=-0.2, dur=0.15, release=0.15)
    m.add(L.note("ViolinEnsPizz", 74, 90), 0.06, gain=0.7, pan=0.2, dur=0.18, release=0.2)
    m.add(L.note("Glockenspiel", 91, 50), 0.06, gain=0.2, dur=0.15, release=0.25)
    return m.buf


def t_weaken(L, rng):
    m = rs.Mix(0.7)
    m.add(ds.lowpass(L.note("CelloEnsPizz", 50, 90), 1400), 0.0, gain=0.6, dur=0.15, release=0.15)
    m.add(ds.lowpass(L.note("CelloEnsPizz", 44, 90), 1200), 0.07, gain=0.65, dur=0.2, release=0.2)
    return m.buf


def t_summon(L, rng):
    m = rs.Mix(0.7)
    m.add(ds.lowpass(L.file(TENOR, -5), 1600), 0.0, gain=0.6, dur=0.2, release=0.15)
    m.add(L.note("ContrabassPizz", 45, 100), 0.0, gain=0.5, dur=0.25, release=0.2)
    m.add(ds.knock(rng), 0.0, gain=0.15, align=False)
    return m.buf


def t_draw(L, rng):
    m = rs.Mix(0.6)
    flick = ds.bandpass(noise(rng, 0.06), 2500, 8000) * decay(0.06, 0.012)
    place(m, flick, 0.0, gain=0.25)
    m.add(L.note("Harp", 76, 65), 0.025, gain=0.55, dur=0.18, release=0.25)
    return m.buf


def t_strike(L, rng):
    m = rs.Mix(0.6)
    m.add(L.file(TENOR2, -2), 0.0, gain=0.8, dur=0.18, release=0.12)
    m.add(ds.knock(rng), 0.0, gain=0.3, align=False)
    return m.buf


def t_snap(L, rng):
    m = rs.Mix(0.5)
    for i, dt in enumerate((0.0, 0.055)):
        m.add(ds.knock(rng, 0.04), dt, gain=0.5 - 0.15 * i, align=False)
    m.add(ds.lowpass(L.file(HAND, -3), 900), 0.055, gain=0.5, dur=0.12, release=0.1)
    return m.buf


def t_splash(L, rng):
    m = rs.Mix(0.6)
    place(m, whoosh(rng, 0.22, 0.12, 500, 5000), 0.0, gain=0.25)
    place(m, bubbles(rng, 0.3, 9, 600, 1500, spread=0.18), 0.02, gain=0.6)
    return m.buf


def t_flame(L, rng):
    m = rs.Mix(0.6)
    place(m, whoosh(rng, 0.3, 0.1, 250, 3500), 0.0, gain=0.4)
    place(m, crackle(rng, 0.4, 500, 0.12), 0.02, gain=0.6)
    return m.buf


def t_fury(L, rng):
    m = rs.Mix(0.6)
    m.add(L.note("CelloEnsTrem", 49, 100), 0.0, gain=0.4, dur=0.18, release=0.15)
    m.add(L.note("CelloEnsTrem", 50, 100), 0.0, gain=0.35, dur=0.18, release=0.15)
    place(m, whoosh(rng, 0.25, 0.08, 180, 1800), 0.0, gain=0.35)
    return m.buf


def t_bloodspatter(L, rng):
    m = rs.Mix(0.6)
    m.add(ds.lowpass(L.file(HAND, -9), 600), 0.0, gain=0.6, dur=0.12, release=0.08)
    splat = ds.bandpass(noise(rng, 0.12), 300, 2500) * decay(0.12, 0.03)
    place(m, splat, 0.0, gain=0.5)
    place(m, crackle(rng, 0.15, 700, 0.04, 800, 4000), 0.01, gain=0.35)
    return m.buf


def t_doom(L, rng):
    m = rs.Mix(1.6)
    m.add(ds.lowpass(L.file(GONG_MF, -10), 1500), 0.0, gain=0.6, dur=0.9, release=0.5)
    m.add(L.file(TIMPANI, -5), 0.0, gain=0.7, dur=0.6, release=0.3)
    place(m, ds.sub(0.6, 70, 38, 0.18), 0.0, gain=0.7)
    return m.buf


def t_maelstrom(L, rng):
    m = rs.Mix(1.0)
    x = noise(rng, 0.8)
    swirl = sweep_filter(x, 200, 120, 1600, 500) * swell(0.8, 0.25, 0.55)
    swirl[:, 0] *= 0.6 + 0.4 * np.sin(secs(len(swirl)) * 2 * math.pi * 6)
    swirl[:, 1] *= 0.6 + 0.4 * np.cos(secs(len(swirl)) * 2 * math.pi * 6)
    place(m, swirl, 0.0, gain=0.6)
    m.add(L.note("ContrabassTrem", 38, 100), 0.0, gain=0.4, dur=0.5, release=0.3)
    place(m, bubbles(rng, 0.6, 10, 300, 800), 0.1, gain=0.35)
    return m.buf


def t_petrify(L, rng):
    m = rs.Mix(0.8)
    grind = ds.bandpass(noise(rng, 0.45), 120, 1500) * swell(0.45, 0.15, 0.3)
    grind *= (0.5 + 0.5 * np.abs(np.sin(secs(len(grind)) * 2 * math.pi * 23)))[:, None]
    place(m, grind, 0.0, gain=0.45)
    place(m, crackle(rng, 0.45, 300, 0.2, 600, 3000), 0.0, gain=0.4)
    m.add(ds.lowpass(L.file(ANVIL, -14), 1200), 0.3, gain=0.4, dur=0.25, release=0.2)
    return m.buf


def t_mend(L, rng):
    m = rs.Mix(0.9)
    for i, n in enumerate((67, 71, 74)):
        m.add(L.note("Harp", n, 65), 0.045 * i, gain=0.55, pan=(i - 1) * 0.3, dur=0.25, release=0.3)
    m.add(L.file(TRIANGLE, 2), 0.09, gain=0.15, dur=0.3, release=0.3)
    return m.buf


def t_rally(L, rng):
    m = rs.Mix(0.6)
    m.add(L.note("TromboneStac", 53, 100), 0.0, gain=0.55, dur=0.15, release=0.12)
    m.add(L.note("TromboneStac", 58, 100), 0.07, gain=0.6, dur=0.18, release=0.15)
    m.add(ds.lowpass(L.file(TENOR, -3), 1800), 0.07, gain=0.4, dur=0.15, release=0.1)
    return m.buf


def t_swift(L, rng):
    m = rs.Mix(0.5)
    place(m, whoosh(rng, 0.25, 0.1, 900, 7000), 0.0, gain=0.4)
    m.add(L.note("FluteStac", 86, 80), 0.06, gain=0.4, dur=0.08, release=0.12)
    return m.buf


def t_revive(L, rng):
    m = rs.Mix(1.2)
    m.add(L.note("TubularBells", 67, 70), 0.0, gain=0.5, dur=0.5, release=0.5)
    for i, n in enumerate((60, 64, 67, 72)):
        m.add(L.note("Harp", n, 60), 0.05 + 0.04 * i, gain=0.4, pan=(i - 1.5) * 0.25, dur=0.3, release=0.3)
    return m.buf


def t_chaos(L, rng):
    m = rs.Mix(0.7)
    for i, n in enumerate(rng.choice([84, 86, 89, 90, 93, 96], 5, replace=False)):
        m.add(L.note("Glockenspiel", int(n), 60), 0.035 * i, gain=0.35, pan=rng.uniform(-0.6, 0.6), dur=0.15, release=0.2)
    place(m, whoosh(rng, 0.3, 0.15, 400, 4000), 0.0, gain=0.2)
    return m.buf


def t_hope(L, rng):
    m = rs.Mix(1.0)
    for i, n in enumerate((62, 66, 69, 74, 78)):
        m.add(L.note("Harp", n, 60), 0.035 * i, gain=0.45, pan=(i - 2) * 0.2, dur=0.3, release=0.35)
    m.add(L.note("Glockenspiel", 90, 50), 0.17, gain=0.25, dur=0.2, release=0.3)
    return m.buf


def t_fate(L, rng):
    m = rs.Mix(0.9)
    # Shears: two metal-on-metal ticks, then a low harp string left hanging.
    for i, dt in enumerate((0.0, 0.07)):
        tick = ds.bandpass(noise(rng, 0.03), 3500, 11000) * decay(0.03, 0.004)
        place(m, tick, dt, gain=0.5 - 0.15 * i)
    m.add(ds.lowpass(L.file(ANVIL, 10), 6000), 0.07, gain=0.12, dur=0.05, release=0.08)
    m.add(L.note("Harp", 52, 70), 0.1, gain=0.55, dur=0.35, release=0.3)
    return m.buf


def t_lyre(L, rng):
    m = rs.Mix(0.9)
    for i, n in enumerate((62, 67, 71, 74)):
        m.add(L.note("Harp", n, 70), 0.03 * i, gain=0.45, pan=(i - 1.5) * 0.25, dur=0.3, release=0.35)
    return m.buf


def t_panpipe(L, rng):
    m = rs.Mix(0.6)
    for i, n in enumerate((81, 84, 88)):
        m.add(L.note("FluteStac", n, 80), 0.055 * i, gain=0.45, dur=0.07, release=0.1)
    return m.buf


def t_boon(L, rng):
    m = rs.Mix(0.9)
    m.add(L.note("TubularBells", 72, 50), 0.0, gain=0.4, dur=0.35, release=0.35)
    m.add(L.file(TRIANGLE, 0), 0.0, gain=0.2, dur=0.3, release=0.3)
    return m.buf


def t_chime(L, rng):
    m = rs.Mix(0.7)
    m.add(L.file(TRIANGLE, -2), 0.0, gain=0.5, dur=0.25, release=0.3)
    m.add(L.note("Harp", 74, 55), 0.0, gain=0.25, dur=0.2, release=0.25)
    return m.buf


# --------------------------------------------------------------------------------------- impacts ---
def i_water(L, rng):
    m = rs.Mix(0.5)
    splash = ds.bandpass(noise(rng, 0.25), 600, 7000) * decay(0.25, 0.06)
    place(m, splash, 0.0, gain=0.5)
    place(m, bubbles(rng, 0.3, 7, 700, 1800, spread=0.12), 0.01, gain=0.45)
    return m.buf


def i_fire(L, rng):
    m = rs.Mix(0.5)
    place(m, ds.bandpass(noise(rng, 0.2), 200, 2500) * decay(0.2, 0.05), 0.0, gain=0.45)
    place(m, crackle(rng, 0.3, 900, 0.06), 0.0, gain=0.55)
    return m.buf


def i_blood(L, rng):
    return t_bloodspatter(L, rng)


def i_ink(L, rng):
    m = rs.Mix(0.5)
    m.add(ds.lowpass(L.file(HAND_SOFT, -6), 450), 0.0, gain=0.6, dur=0.15, release=0.1)
    place(m, ds.bandpass(noise(rng, 0.25), 300, 2000) * decay(0.25, 0.08), 0.0, gain=0.3)
    return m.buf


def i_gold(L, rng):
    m = rs.Mix(0.6)
    m.add(L.note("Glockenspiel", 96, 70), 0.0, gain=0.5, dur=0.15, release=0.25)
    return m.buf


# ----------------------------------------------------------------------------------------- casts ---
def c_spell(L, rng):
    m = rs.Mix(1.0)
    place(m, whoosh(rng, 0.35, 0.18, 500, 5000), 0.0, gain=0.25)
    for i, n in enumerate((69, 74, 81)):
        m.add(L.note("Harp", n, 60), 0.05 + 0.05 * i, gain=0.4, pan=(i - 1) * 0.3, dur=0.25, release=0.35)
    return m.buf


def c_earth(L, rng):
    m = rs.Mix(1.4)
    rumble = ds.lowpass(noise(rng, 0.9), 160) * swell(0.9, 0.2, 0.7)
    place(m, rumble, 0.0, gain=1.2)
    m.add(ds.lowpass(L.file(BASS_DRUM, -4), 700), 0.12, gain=0.7, dur=0.5, release=0.3)
    for k in range(3):
        m.add(ds.knock(rng, 0.05), 0.15 + 0.11 * k, gain=0.3, align=False)
    place(m, crackle(rng, 0.6, 250, 0.3, 500, 3000), 0.15, gain=0.4)
    return m.buf


def c_hunt(L, rng):
    m = rs.Mix(1.4)
    m.add(L.note("FHornSus", 53, 100), 0.0, gain=0.7, dur=0.22, release=0.12)
    m.add(L.note("FHornSus", 60, 100), 0.26, gain=0.8, dur=0.45, release=0.35)
    m.add(ds.lowpass(L.file(TENOR, -4), 1500), 0.26, gain=0.3, dur=0.15, release=0.1)
    return m.buf


def c_filth(L, rng):
    m = rs.Mix(1.0)
    for k in range(3):
        sq = ds.bandpass(noise(rng, 0.14), 150, 900) * swell(0.14, 0.04, 0.1)
        sq *= (0.6 + 0.4 * np.sin(secs(len(sq)) * 2 * math.pi * rng.uniform(25, 40)))[:, None]
        place(m, sq, 0.08 * k, gain=0.5)
    m.add(L.note("TubaStac", 34, 90), 0.05, gain=0.5, dur=0.2, release=0.15)
    place(m, bubbles(rng, 0.4, 6, 150, 400), 0.05, gain=0.4)
    return m.buf


def c_eyes(L, rng):
    m = rs.Mix(1.4)
    m.add(L.note("ContrabassTrem", 38, 90), 0.0, gain=0.55, dur=0.7, release=0.4)
    m.add(L.note("CelloEnsTrem", 51, 80), 0.05, gain=0.3, dur=0.6, release=0.4)
    for i, n in enumerate((96, 101)):
        m.add(L.note("Glockenspiel", n, 40), 0.35 + 0.12 * i, gain=0.2, pan=(-0.6, 0.6)[i], dur=0.1, release=0.3)
    return m.buf


def c_macabre(L, rng):
    m = rs.Mix(1.2)
    for k in range(14):
        m.add(ds.knock(rng, 0.035), 0.05 + k * 0.035 + rng.uniform(0, 0.015), gain=rng.uniform(0.15, 0.35),
              pan=rng.uniform(-0.6, 0.6), align=False)
    for k, dt in enumerate((0.0, 0.24, 0.36)):
        m.add(ds.lowpass(L.file(TENOR, -3), 1700), dt, gain=0.5, dur=0.15, release=0.1)
    m.add(L.note("CelloEnsPizz", 40, 100), 0.36, gain=0.5, dur=0.3, release=0.3)
    return m.buf


def c_haunt(L, rng):
    m = rs.Mix(1.2)
    m.add(L.note("FluteSusVib", 70, 60), 0.0, gain=0.4, dur=0.35, release=0.35)
    m.add(L.note("FluteSusVib", 69, 60), 0.0, gain=0.3, dur=0.35, release=0.35)
    place(m, whoosh(rng, 0.6, 0.35, 200, 2500, up=False), 0.0, gain=0.35)
    return m.buf


def c_flame(L, rng):
    m = rs.Mix(1.0)
    place(m, whoosh(rng, 0.55, 0.15, 150, 4000), 0.0, gain=0.6)
    place(m, crackle(rng, 0.8, 600, 0.3), 0.05, gain=0.55)
    place(m, ds.sub(0.4, 90, 50, 0.12), 0.05, gain=0.4)
    return m.buf


def c_vengeance(L, rng):
    m = rs.Mix(1.2)
    m.add(L.note("TromboneSus", 46, 110), 0.0, gain=0.5, dur=0.2, release=0.25)
    m.add(L.note("TubaSus", 34, 110), 0.0, gain=0.5, dur=0.2, release=0.25)
    m.add(L.file(TIMPANI, -2), 0.0, gain=0.8, dur=0.5, release=0.3)
    m.add(L.file(TENOR, 0), 0.0, gain=0.4, dur=0.2, release=0.1)
    return m.buf


def c_styx(L, rng):
    m = rs.Mix(1.8)
    surge = ds.lowpass(noise(rng, 1.3), 900) * swell(1.3, 0.45, 0.85)
    place(m, surge, 0.0, gain=0.6)
    place(m, bubbles(rng, 1.0, 16, 200, 700), 0.2, gain=0.4)
    m.add(ds.lowpass(L.file(GONG_MF, -14), 900), 0.4, gain=0.5, dur=1.0, release=0.5)
    m.add(L.note("ContrabassSusVB", 33, 90), 0.0, gain=0.4, dur=0.9, release=0.5)
    return m.buf


def c_titans(L, rng):
    m = rs.Mix(2.4)
    m.add(ds.lowpass(rs.load(THUNDER), 3000), 0.0, gain=0.55, dur=1.6, release=0.6, norm=True)
    m.add(L.file(GONG_FF, -5), 0.08, gain=0.7, dur=1.4, release=0.7)
    m.add(L.file(TIMPANI, -4), 0.08, gain=0.9, dur=0.8, release=0.4)
    m.add(L.file(BASS_DRUM, -3), 0.08, gain=0.7, dur=0.8, release=0.4)
    place(m, ds.sub(1.0, 60, 30, 0.35), 0.08, gain=0.8)
    return m.buf


def c_drown(L, rng):
    m = rs.Mix(1.3)
    gulp = chirp(0.25, 180, 90, 0.08, curve=0.08)
    place(m, gulp, 0.05, gain=0.5)
    place(m, bubbles(rng, 0.9, 18, 250, 900), 0.0, gain=0.5)
    place(m, ds.lowpass(noise(rng, 0.9), 500) * swell(0.9, 0.3, 0.6), 0.0, gain=0.45)
    return m.buf


def c_gnaw(L, rng):
    m = rs.Mix(0.9)
    place(m, whoosh(rng, GNAW_CLOSE, GNAW_CLOSE * 0.9, 300, 2500), 0.0, gain=0.25)
    m.add(L.file(HAND, -4), GNAW_CLOSE, gain=0.7, dur=0.15, release=0.1)
    for k in range(3):
        m.add(ds.knock(rng, 0.04), GNAW_CLOSE + 0.02 * k, gain=0.45, align=False)
    place(m, crackle(rng, 0.25, 1400, 0.06, 900, 5000), GNAW_CLOSE, gain=0.6)
    place(m, ds.sub(0.3, 90, 45, 0.08), GNAW_CLOSE, gain=0.5)
    return m.buf


def c_bless(L, rng):
    m = rs.Mix(1.2)
    for i, n in enumerate((62, 66, 69, 74, 78)):
        m.add(L.note("Harp", n, 65), 0.04 * i, gain=0.45, pan=(i - 2) * 0.2, dur=0.35, release=0.4)
    m.add(L.note("Glockenspiel", 90, 50), 0.2, gain=0.25, dur=0.2, release=0.4)
    m.add(L.file(TRIANGLE, 3), 0.2, gain=0.2, dur=0.4, release=0.4)
    return m.buf


def c_gears(L, rng):
    m = rs.Mix(0.9)
    t = 0.0
    for k in range(9):
        m.add(ds.knock(rng, 0.03), t, gain=0.35, pan=0.3 * (-1) ** k, align=False)
        t += 0.07 * (0.86 ** k)
    m.add(ds.lowpass(L.file(ANVIL2, -6), 2500), t + 0.02, gain=0.3, dur=0.15, release=0.15)
    return m.buf


def c_confuse(L, rng):
    m = rs.Mix(1.2)
    tone = L.note("OboeSusVib", 66, 70)[:int(0.7 * SR)]
    n = len(tone)
    wobble = 2 ** ((np.sin(secs(n) * 2 * math.pi * 5) * 0.5) / 12)
    pos = np.cumsum(wobble)
    pos = pos[pos < n - 1]
    bent = np.stack([np.interp(pos, np.arange(n), tone[:, c]) for c in range(2)], axis=1)
    m.add(bent, 0.0, gain=0.45, dur=0.45, release=0.2)
    m.add(L.note("ClarinetSus", 61, 70), 0.05, gain=0.3, dur=0.4, release=0.2)
    return m.buf


def c_wings(L, rng):
    m = rs.Mix(0.9)
    flap = noise(rng, 0.5)
    flap = ds.bandpass(flap, 400, 5000) * swell(0.5, 0.2, 0.3)
    flap *= (0.35 + 0.65 * np.abs(np.sin(secs(len(flap)) * math.pi * 14)) ** 3)[:, None]
    place(m, flap, 0.0, gain=0.5)
    m.add(L.note("FluteStac", 88, 70), 0.25, gain=0.3, dur=0.06, release=0.15)
    return m.buf


def c_blade(L, rng):
    m = rs.Mix(1.0)
    place(m, sweep_filter(noise(rng, 0.25), 2000, 6000, 6000, 14000) * swell(0.25, 0.2, 0.05), 0.0, gain=0.3)
    ring = ds.bandpass(L.file(ANVIL, 7), 2500, 12000)
    m.add(ring, 0.18, gain=0.45, dur=0.5, release=0.4)
    return m.buf


def c_shield(L, rng):
    m = rs.Mix(1.2)
    m.add(ds.lowpass(L.file(ANVIL2, -10), 3000), 0.0, gain=0.7, dur=0.6, release=0.4)
    m.add(ds.lowpass(L.file(GONG_MF, 0), 3500), 0.0, gain=0.3, dur=0.5, release=0.4)
    m.add(ds.lowpass(L.file(TENOR, -2), 1500), 0.0, gain=0.4, dur=0.15, release=0.1)
    return m.buf


def c_pact(L, rng):
    m = rs.Mix(1.4)
    m.add(L.note("CelloEnsSusVib", 40, 90), 0.0, gain=0.5, dur=0.6, release=0.4)
    m.add(L.note("CelloEnsSusVib", 41, 90), 0.0, gain=0.35, dur=0.6, release=0.4)
    heartbeat(L, m, 0.12, 0.9)
    return m.buf


def c_dream(L, rng):
    m = rs.Mix(1.6)
    for i, n in enumerate((72, 76, 79, 84)):
        m.add(L.note("Harp", n, 45), 0.09 * i, gain=0.35, pan=(i - 1.5) * 0.3, dur=0.45, release=0.5)
    m.add(L.note("Glockenspiel", 96, 35), 0.3, gain=0.2, dur=0.3, release=0.5)
    return m.buf


def c_restore(L, rng):
    m = rs.Mix(1.6)
    m.add(L.note("ViolinEnsSusVib", 76, 70), 0.0, gain=0.35, dur=0.6, release=0.5)
    m.add(L.note("ViolaEnsSusVib", 69, 70), 0.0, gain=0.3, dur=0.6, release=0.5)
    for i, n in enumerate((64, 69, 72, 76)):
        m.add(L.note("Harp", n, 60), 0.05 + 0.05 * i, gain=0.35, dur=0.3, release=0.4)
    return m.buf


def c_mirror(L, rng):
    m = rs.Mix(1.2)
    shimmer = L.note("Glockenspiel", 91, 60)[:int(0.6 * SR)][::-1]
    m.add(shimmer * swell(len(shimmer) / SR, len(shimmer) / SR * 0.9, 0.05), 0.0, gain=0.35, align=False)
    m.add(L.note("Glockenspiel", 91, 70), 0.55, gain=0.4, dur=0.2, release=0.4)
    m.add(L.note("Glockenspiel", 98, 50), 0.55, gain=0.25, dur=0.2, release=0.4)
    return m.buf


def c_roar(L, rng):
    """A beast's roar, without a throat: low brass bellowing with a noise growl, over a timpani roll."""
    m = rs.Mix(2.0)
    for inst, note, g in (("TubaSus", 31, 0.7), ("TromboneSus", 41, 0.55), ("FHornSus", 46, 0.35)):
        tone = L.note(inst, note, 120)[:int(1.2 * SR)]
        n = len(tone)
        t = secs(n)
        growl = 1 + 0.35 * np.sin(t * 2 * math.pi * 31) * np.clip(t / 0.15, 0, 1)
        bend = 2 ** ((-1.5 * np.clip((t - 0.3) / 0.8, 0, 1)) / 12)
        pos = np.cumsum(bend)
        pos = pos[pos < n - 1]
        bent = np.stack([np.interp(pos, np.arange(n), tone[:, c]) for c in range(2)], axis=1)
        bent *= growl[:len(bent), None]
        m.add(bent, 0.02, gain=g, dur=0.85, release=0.3)
    rasp = ds.bandpass(noise(rng, 1.1), 150, 1300) * swell(1.1, 0.15, 0.95)
    rasp *= (0.5 + 0.5 * np.sin(secs(len(rasp)) * 2 * math.pi * 31))[:, None]
    place(m, rasp, 0.0, gain=0.35)
    m.add(L.file(TIMPANI_ROLL, -3), 0.0, gain=0.45, dur=0.9, release=0.4)
    place(m, ds.sub(0.9, 55, 32, 0.4), 0.02, gain=0.6)
    return m.buf


def c_panic(L, rng):
    m = rs.Mix(1.0)
    for inst, n, g in (("ViolinEnsSusVib", 79, 0.3), ("ViolinEnsSusVib", 80, 0.25), ("CelloEnsTrem", 50, 0.4)):
        stab = L.note(inst, n, 110)[:int(0.5 * SR)]
        stab *= (0.5 + 0.5 * np.sign(np.sin(secs(len(stab)) * 2 * math.pi * 14)))[:, None]
        m.add(stab, 0.0, gain=g, dur=0.4, release=0.15)
    for k in range(4):
        m.add(ds.sub(0.2, 80, 45, 0.05), 0.08 + 0.13 * k, gain=0.5, align=False)
    return m.buf


def c_scry(L, rng):
    m = rs.Mix(1.0)
    for i, n in enumerate((84, 88, 91, 96)):
        m.add(L.note("Glockenspiel", n, 45), 0.06 * i, gain=0.3, pan=(i - 1.5) * 0.3, dur=0.2, release=0.35)
    place(m, whoosh(rng, 0.35, 0.25, 1500, 9000), 0.0, gain=0.12)
    return m.buf


def c_herald(L, rng):
    m = rs.Mix(1.0)
    m.add(L.note("TrumpetSus", 67, 90), 0.0, gain=0.5, dur=0.12, release=0.08)
    m.add(L.note("TrumpetSus", 72, 95), 0.15, gain=0.55, dur=0.3, release=0.25)
    return m.buf


def c_draught(L, rng):
    m = rs.Mix(1.0)
    pour = ds.bandpass(noise(rng, 0.5), 500, 3500) * swell(0.5, 0.15, 0.35)
    place(m, pour, 0.0, gain=0.25)
    place(m, bubbles(rng, 0.5, 14, 500, 1200), 0.05, gain=0.5)
    for i, dt in enumerate((0.42, 0.62)):
        place(m, chirp(0.2, 220, 130, 0.05, curve=0.05), dt, gain=0.45)
    return m.buf


def c_rites(L, rng):
    m = rs.Mix(2.0)
    m.add(ds.lowpass(L.file(TUBE_C4, -7), 2500), 0.0, gain=0.6, dur=1.2, release=0.6)
    m.add(L.note("ContrabassSusVB", 33, 80), 0.0, gain=0.3, dur=0.8, release=0.5)
    for k in range(6):
        m.add(ds.knock(rng, 0.035), 0.25 + 0.05 * k, gain=0.2, pan=rng.uniform(-0.5, 0.5), align=False)
    return m.buf


def c_harvest(L, rng):
    m = rs.Mix(1.4)
    rustle = ds.bandpass(noise(rng, 0.7), 1500, 7000) * swell(0.7, 0.25, 0.45)
    rustle *= (0.5 + 0.5 * np.abs(rng.standard_normal(len(rustle)) * 0.5).clip(0, 1))[:, None]
    place(m, rustle, 0.0, gain=0.25)
    for i, n in enumerate((60, 64, 67, 72)):
        m.add(L.note("Harp", n, 60), 0.15 + 0.05 * i, gain=0.4, dur=0.35, release=0.4)
    return m.buf


# The Price Of... casts: one heartbeat bed, a different coda on PRICE_CODA.
def price(L, rng, coda):
    m = rs.Mix(2.0)
    m.add(L.note("CelloEnsSusVib", 40, 80), 0.0, gain=0.25, dur=0.9, release=0.4)
    heartbeat(L, m, HEARTBEATS[0])
    heartbeat(L, m, HEARTBEATS[2], 1.1)
    coda(L, m, rng, PRICE_CODA)
    return m.buf


def coda_profit(L, m, rng, at):
    m.add(L.note("Glockenspiel", 88, 80), at, gain=0.5, dur=0.2, release=0.3)
    m.add(L.note("Glockenspiel", 95, 70), at + 0.05, gain=0.35, dur=0.2, release=0.3)


def coda_knowledge(L, m, rng, at):
    for i, n in enumerate((72, 76)):
        m.add(L.note("Harp", n, 70), at + 0.06 * i, gain=0.5, dur=0.3, release=0.35)


def coda_inspiration(L, m, rng, at):
    for i, n in enumerate((67, 71, 74, 79, 83)):
        m.add(L.note("Harp", n, 65), at + 0.035 * i, gain=0.4, pan=(i - 2) * 0.2, dur=0.3, release=0.35)


def coda_wealth(L, m, rng, at):
    for i in range(5):
        m.add(L.note("Glockenspiel", int(rng.choice([86, 88, 91, 93, 95])), 70), at + 0.045 * i, gain=0.35,
              pan=rng.uniform(-0.6, 0.6), dur=0.15, release=0.3)


def coda_reprisal(L, m, rng, at):
    m.add(L.file(TENOR2, -2), at, gain=0.7, dur=0.25, release=0.15)
    m.add(L.file(TIMPANI, -3), at, gain=0.6, dur=0.4, release=0.3)
    place(m, whoosh(rng, 0.2, 0.18, 300, 3000), at - 0.18, gain=0.3)


def coda_renewal(L, m, rng, at):
    for i, n in enumerate((60, 64, 67, 72)):
        m.add(L.note("Harp", n, 60), at + 0.03 * i, gain=0.4, dur=0.4, release=0.4)
    m.add(L.file(TRIANGLE, 2), at, gain=0.2, dur=0.4, release=0.4)


def coda_legacy(L, m, rng, at):
    for i in range(3):
        m.add(ds.lowpass(L.file(TENOR, -4), 1800), at + 0.12 * i, gain=0.5, dur=0.12, release=0.08)
    m.add(L.note("FHornSus", 53, 90), at + 0.24, gain=0.35, dur=0.25, release=0.25)


def c_fate(L, rng):
    """Twist of Fate: the gods turn. A low drone and a falling harp while gold threads gather, a cymbal swelling into
    FATE_STRIKE, where shears snip and a bell, gong and timpani land as the buff is revealed."""
    m = rs.Mix(4.0)
    m.add(L.note("OrganLoudPedal", 31, 70), 0.0, gain=0.35, dur=FATE_STRIKE + 0.6, release=1.0)
    m.add(L.note("ContrabassSusVB", 38, 90), 0.0, gain=0.4, dur=FATE_STRIKE + 0.3, release=0.8)
    for i, n in enumerate((84, 81, 77, 74, 72, 69, 65, 62)):
        m.add(L.note("Harp", n, 55), 0.05 + 0.085 * i, gain=0.32, pan=(i % 2 - 0.5) * 0.6, dur=0.25, release=0.35)
    swell_clip = L.file(CYMBAL_SWELL)
    env = np.abs(swell_clip).max(axis=1)
    k = int(0.05 * SR)
    peak = int(np.argmax(np.convolve(env, np.ones(k) / k, mode="same")))
    start = FATE_STRIKE - peak / SR
    piece = swell_clip[max(0, -int(start * SR)):peak + int(0.04 * SR)]
    m.add(piece, max(0.0, start), gain=0.35, align=False)
    # The strike
    for i, dt in enumerate((0.0, 0.06)):
        tick = ds.bandpass(noise(rng, 0.03), 3500, 11000) * decay(0.03, 0.004)
        place(m, tick, FATE_STRIKE - 0.06 + dt, gain=0.5)
    m.add(L.note("TubularBells", 62, 100), FATE_STRIKE, gain=0.7, dur=1.6, release=1.0)
    m.add(ds.lowpass(L.file(GONG_FF, -7), 2200), FATE_STRIKE, gain=0.55, dur=1.8, release=1.0)
    m.add(L.file(TIMPANI, -5), FATE_STRIKE, gain=0.7, dur=1.0, release=0.5)
    place(m, ds.sub(1.0, 62, 34, 0.35), FATE_STRIKE, gain=0.6)
    return m.buf


SOUNDS = {
    # trigger families
    "Gold": t_gold, "Life": t_life, "LifeLoss": t_lifeloss, "Siphon": t_siphon, "Empower": t_empower,
    "Weaken": t_weaken, "Summon": t_summon, "Draw": t_draw, "Strike": t_strike, "Snap": t_snap,
    "Splash": t_splash, "Flame": t_flame, "Fury": t_fury, "BloodSpatter": t_bloodspatter, "Doom": t_doom,
    "Maelstrom": t_maelstrom, "Petrify": t_petrify, "Mend": t_mend, "Rally": t_rally, "Swift": t_swift,
    "Revive": t_revive, "Chaos": t_chaos, "Hope": t_hope, "Fate": t_fate, "Lyre": t_lyre,
    "Panpipe": t_panpipe, "Boon": t_boon, "Chime": t_chime,
    # projectile impacts
    "ImpactWater": i_water, "ImpactFire": i_fire, "ImpactBlood": i_blood, "ImpactInk": i_ink, "ImpactGold": i_gold,
    # spell casts
    "CastSpell": c_spell, "CastEarth": c_earth, "CastHunt": c_hunt, "CastFilth": c_filth, "CastEyes": c_eyes,
    "CastMacabre": c_macabre, "CastHaunt": c_haunt, "CastFlame": c_flame, "CastVengeance": c_vengeance,
    "CastStyx": c_styx, "CastTitans": c_titans, "CastDrown": c_drown, "CastGnaw": c_gnaw, "CastBless": c_bless,
    "CastGears": c_gears, "CastConfuse": c_confuse, "CastWings": c_wings, "CastBlade": c_blade,
    "CastShield": c_shield, "CastPact": c_pact, "CastDream": c_dream, "CastRestore": c_restore,
    "CastMirror": c_mirror, "CastRoar": c_roar, "CastPanic": c_panic, "CastScry": c_scry,
    "CastHerald": c_herald, "CastDraught": c_draught, "CastRites": c_rites, "CastHarvest": c_harvest,
    "CastFate": c_fate,
    "CastPriceProfit": lambda L, r: price(L, r, coda_profit),
    "CastPriceKnowledge": lambda L, r: price(L, r, coda_knowledge),
    "CastPriceInspiration": lambda L, r: price(L, r, coda_inspiration),
    "CastPriceWealth": lambda L, r: price(L, r, coda_wealth),
    "CastPriceReprisal": lambda L, r: price(L, r, coda_reprisal),
    "CastPriceRenewal": lambda L, r: price(L, r, coda_renewal),
    "CastPriceLegacy": lambda L, r: price(L, r, coda_legacy),
}

# Small rooms for the quick ones, a bigger hall for the grand casts.
ROOMS = {"CastFate": (0.3, 2.5), "CastTitans": (0.3, 2.5), "CastRoar": (0.22, 2.0), "Doom": (0.22, 1.8),
         "CastStyx": (0.25, 2.0), "CastRites": (0.25, 2.0)}


def render(L, name, seed):
    rng = np.random.default_rng(seed + sum(map(ord, name)))
    dry = SOUNDS[name](L, rng)
    wet, room = ROOMS.get(name, (0.16, 1.2) if name.startswith("Cast") else (0.06, 0.6))
    return finish(dry, name, wet=wet, room=room)


def write_preview(folder, names, out_dir):
    from PIL import Image
    os.makedirs(folder, exist_ok=True)
    rows = []
    for name in names:
        stereo_buf = rs.read_wav(os.path.join(out_dir, name + ".wav"))
        png = os.path.join(folder, name + ".png")
        rs.spectrogram_png(stereo_buf, png, [])
        rel = os.path.relpath(os.path.join(out_dir, name + ".wav"), folder).replace("\\", "/")
        rows.append('<div class="s"><h3>%s <small>%.2fs</small></h3><audio controls src="%s"></audio><img src="%s"></div>'
                    % (name, len(stereo_buf) / SR, rel, name + ".png"))
    html = ("<!doctype html><meta charset=utf-8><title>Cue sounds</title><style>body{background:#141110;color:#E8DCC2;"
            "font-family:sans-serif}.s{display:inline-block;margin:8px;width:420px}img{width:420px}</style>"
            + "".join(rows))
    open(os.path.join(folder, "index.html"), "w", encoding="utf-8").write(html)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=OUT)
    parser.add_argument("--samples", default=rs.DEFAULT_SAMPLES)
    parser.add_argument("--only", nargs="*")
    parser.add_argument("--preview")
    parser.add_argument("--seed", type=int, default=7)
    args = parser.parse_args()

    os.makedirs(args.out, exist_ok=True)
    L = rs.Library(args.samples)
    names = args.only or list(SOUNDS)
    for name in names:
        path = os.path.join(args.out, name + ".wav")
        rs.write_wav(path, render(L, name, args.seed))
        print("%-22s %.2fs" % (name, rs.read_wav(path).shape[0] / SR))
    if args.preview:
        write_preview(args.preview, names, args.out)


if __name__ == "__main__":
    main()
