"""Renders the follower death sounds, timed to FollowerDeathAnimation's two beats.

    DeathStrike   the skull lands on the card: a dull, heavy drum strike with a bone knock and a sub thump.
    DeathCollapse the card sinks and breaks into ash: a deep low impact under a gong pitched down an octave,
                  a falling sub, and dry ash crumbling away.

Both play in the same dark stone hall as the ritual invocations (build_ritual_sounds.temple_ir / master), so the
game's generated sounds sit together. Percussion: VSCO 2 CE / VSCO 1 percussion (CC0) in Tools/Audio/Samples.
    python Tools/Audio/build_death_sounds.py [--out <folder>] [--seed N]
Output: Assets/Resources/Audio/SFX/Death/DeathStrike.wav, DeathCollapse.wav (44.1 kHz, 16-bit stereo).
Requires numpy.
"""
import argparse
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_ritual_sounds as rs  # noqa: E402

SR = rs.SR
OUT = os.path.join(rs.ROOT, "Assets", "Resources", "Audio", "SFX", "Death")

HAND = "VSCO 1 Percussion/drums/other/ethnic/giant/hand/EthnicLargeHand_hit_ff_1.wav"
BASS_DRUM = "VSCO 1 Percussion/drums/bass/bdrum3_fff_1.wav"
TENOR = "VSCO 1 Percussion/drums/tenor/tenor_lower/tenor_fff_2.wav"
GONG = "VSCO 1 Percussion/varMetal/Gong/gong_hit_fff.wav"
TIMPANI = "Percussion/Timpani/Timpani1_Hit_v3_rr1_Sum.wav"


def lowpass(x, hz):
    """Brick-wall-ish low-pass in the frequency domain with a soft shoulder (offline, so cost doesn't matter)."""
    n = len(x)
    size = 1 << (n - 1).bit_length()
    f = np.fft.rfftfreq(size, 1 / SR)
    mask = 1 / (1 + (f / hz) ** 4)
    return np.stack([np.fft.irfft(np.fft.rfft(x[:, c], size) * mask, size)[:n] for c in range(x.shape[1])], axis=1)


def bandpass(x, lo, hi):
    n = len(x)
    size = 1 << (n - 1).bit_length()
    f = np.fft.rfftfreq(size, 1 / SR)
    mask = (1 / (1 + (lo / np.maximum(f, 1)) ** 4)) * (1 / (1 + (f / hi) ** 4))
    return np.stack([np.fft.irfft(np.fft.rfft(x[:, c], size) * mask, size)[:n] for c in range(x.shape[1])], axis=1)


def stereo(mono):
    return np.stack([mono, mono], axis=1)


def sub(seconds, f0, f1, tau, attack=0.003):
    """A sine sweeping f0 -> f1 Hz with an exponential decay: the felt part of a heavy hit."""
    t = np.arange(int(seconds * SR)) / SR
    freq = f1 + (f0 - f1) * np.exp(-t / 0.12)
    phase = 2 * math.pi * np.cumsum(freq) / SR
    env = np.minimum(t / attack, 1) * np.exp(-t / tau)
    return stereo(np.sin(phase) * env)


def knock(rng, seconds=0.05):
    """A short dry crack: band-passed noise with a very fast decay (bone on stone)."""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    noise = rng.standard_normal((n, 2)) * np.exp(-t / 0.008)[:, None]
    return bandpass(noise, 900, 3500)


def crumble(rng, seconds, density=260.0, tau=0.45):
    """Ash breaking up: many tiny grains of filtered noise, thinning out, over a soft hiss."""
    n = int(seconds * SR)
    out = np.zeros((n, 2))
    t = 0.0
    while t < seconds:
        rate = density * math.exp(-t / tau)
        if rate < 2:
            break
        t += rng.exponential(1 / rate)
        i = int(t * SR)
        g = int(rng.uniform(0.002, 0.007) * SR)
        if i + g >= n:
            break
        grain = rng.standard_normal(g) * np.hanning(g) * math.exp(-t / tau) * rng.uniform(0.3, 1.0)
        pan = rng.uniform(-0.6, 0.6)
        out[i:i + g, 0] += grain * (1 - pan) * 0.5
        out[i:i + g, 1] += grain * (1 + pan) * 0.5
    grains = bandpass(out, 1500, 7000)
    tt = np.arange(n) / SR
    hiss = bandpass(rng.standard_normal((n, 2)), 400, 3000) * (np.exp(-tt / (tau * 1.4)) * np.minimum(tt / 0.02, 1))[:, None]
    return grains * 1.0 + hiss * 0.08


def strike(lib, rng):
    m = rs.Mix(2.0)
    m.add(lib.file(HAND, -5), 0.0, gain=1.0)
    m.add(lowpass(lib.file(BASS_DRUM, -3), 900), 0.0, gain=0.55)
    m.add(lowpass(lib.file(TIMPANI, -7), 1200), 0.005, gain=0.35, dur=0.5, release=0.4)
    m.add(sub(0.6, 85, 46, 0.17), 0.0, gain=0.9, align=False)
    m.add(knock(rng), 0.0, gain=0.35, align=False)
    return rs.master(m.buf, rs.temple_ir(2.0, seed=5, decay=0.8), wet=0.22)


def collapse(lib, rng):
    m = rs.Mix(3.5)
    m.add(lowpass(lib.file(BASS_DRUM, -5), 700), 0.0, gain=0.9)
    m.add(lowpass(lib.file(TENOR, -9), 1100), 0.0, gain=0.45)
    m.add(lowpass(lib.file(GONG, -12), 1800), 0.02, gain=0.32, dur=1.6, release=1.0)
    m.add(sub(1.0, 70, 36, 0.32), 0.0, gain=1.0, align=False)
    m.add(crumble(rng, 1.6), 0.03, gain=0.5, align=False, norm=True)
    return rs.master(m.buf, rs.temple_ir(3.0, seed=7, decay=1.0), wet=0.3)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=OUT)
    parser.add_argument("--samples", default=rs.DEFAULT_SAMPLES)
    parser.add_argument("--seed", type=int, default=11)
    args = parser.parse_args()

    os.makedirs(args.out, exist_ok=True)
    lib = rs.Library(args.samples)
    rng = np.random.default_rng(args.seed)
    for name, render in (("DeathStrike", strike), ("DeathCollapse", collapse)):
        path = os.path.join(args.out, name + ".wav")
        rs.write_wav(path, render(lib, rng))
        print(path)


if __name__ == "__main__":
    main()
