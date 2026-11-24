"""Renders every ritual's invocation sound: a 2-3 note motif on traditional instruments, timed to the ritual VFX.

Each ritual (Major = 3 notes, Minor = 2 notes) plays its god's instruments over a shared temple bed (cymbal swell into
the last note, a timpani/drum hit and a long dark reverb), so the set reads as one family the way the offering notes do.
The last note always lands on the VFX climax, so the beat times below must match RitualInvocationVfx:
    Major beats: 0.00, 0.40, 0.90 s     Minor beats: 0.00, 0.60 s      (RitualInvocationVfx.MajorBeats / MinorBeats)

Samples: VSCO 2 Community Edition (CC0), read through its .sfz mappings. The set was dropped into
Assets/Resources/Audio/Instruments for this; once it is removed from the project, keep a copy elsewhere and pass it:
    python Tools/Audio/build_ritual_sounds.py [--samples <VSCO folder>] [--out <folder>] [--only ZeusMajor ...]
              [--preview <folder for spectrogram pngs + index.html>]
Output: Assets/Resources/Audio/SFX/Rituals/Invocations/<RitualClass>.wav (44.1 kHz, 16-bit stereo).
Requires numpy (and Pillow for --preview).
"""
import argparse
import math
import os
import re
import wave

import numpy as np

SR = 44100
MAJOR_BEATS = [0.0, 0.40, 0.90]
MINOR_BEATS = [0.0, 0.60]

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DEFAULT_SAMPLES = os.path.join(ROOT, "Assets", "Resources", "Audio", "Instruments")
DEFAULT_OUT = os.path.join(ROOT, "Assets", "Resources", "Audio", "SFX", "Rituals", "Invocations")

# Loudest half second of every clip is mastered to this RMS (dBFS); AudioHandler.RitualInvocationVolume scales it in game.
TARGET_RMS_DB = -14.0
PEAK_CEILING = 0.89  # -1 dBFS


# ------------------------------------------------------------------ io ---
def read_wav(path):
    w = wave.open(path)
    sr, ch, sw, n = w.getframerate(), w.getnchannels(), w.getsampwidth(), w.getnframes()
    raw = w.readframes(n)
    w.close()
    if sw == 2:
        d = np.frombuffer(raw, np.int16).astype(np.float64) / 32768.0
    elif sw == 3:
        b = np.frombuffer(raw, np.uint8).reshape(-1, 3).astype(np.int32)
        v = b[:, 0] | (b[:, 1] << 8) | (b[:, 2] << 16)
        v = np.where(v >= 1 << 23, v - (1 << 24), v)
        d = v.astype(np.float64) / float(1 << 23)
    elif sw == 4:
        d = np.frombuffer(raw, np.int32).astype(np.float64) / 2.0 ** 31
    else:
        d = (np.frombuffer(raw, np.uint8).astype(np.float64) - 128.0) / 128.0
    d = d.reshape(-1, ch)
    if ch == 1:
        d = np.repeat(d, 2, axis=1)
    elif ch > 2:
        d = d[:, :2]
    if sr != SR:
        d = resample(d, sr / SR)
    return d


def write_wav(path, stereo):
    data = np.clip(stereo, -1, 1)
    pcm = (data * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def resample(d, ratio):
    """Plays d at `ratio` speed (ratio 2 = an octave up, half as long). Linear interpolation per channel."""
    if abs(ratio - 1.0) < 1e-6:
        return d
    n_out = int(len(d) / ratio)
    pos = np.arange(n_out) * ratio
    idx = np.arange(len(d))
    return np.stack([np.interp(pos, idx, d[:, c]) for c in range(d.shape[1])], axis=1)


_cache = {}


def load(path):
    if path not in _cache:
        _cache[path] = read_wav(path)
    return _cache[path]


# ----------------------------------------------------------------- sfz ---
NOTE_NAMES = {"c": 0, "d": 2, "e": 4, "f": 5, "g": 7, "a": 9, "b": 11}


def parse_key(v):
    v = v.strip().lower()
    if re.fullmatch(r"-?\d+", v):
        return int(v)
    m = re.fullmatch(r"([a-g])([#b]?)(-?\d+)", v)
    if not m:
        raise ValueError(v)
    n = NOTE_NAMES[m.group(1)] + (1 if m.group(2) == "#" else -1 if m.group(2) == "b" else 0)
    return n + (int(m.group(3)) + 1) * 12


class Region:
    def __init__(self, ops, base):
        self.path = os.path.join(base, ops["default_path"].replace("\\", os.sep), ops["sample"].replace("\\", os.sep))
        key = ops.get("key")
        self.lokey = parse_key(ops.get("lokey", key if key is not None else "0"))
        self.hikey = parse_key(ops.get("hikey", key if key is not None else "127"))
        self.center = parse_key(ops.get("pitch_keycenter", key if key is not None else str(self.lokey)))
        self.lovel = int(ops.get("lovel", 0))
        self.hivel = int(ops.get("hivel", 127))
        self.tune = float(ops.get("tune", 0)) / 100.0 + float(ops.get("transpose", 0))
        self.trigger = ops.get("trigger", "attack")


class Sfz:
    def __init__(self, path, base):
        text = open(path, encoding="utf-8", errors="ignore").read()
        text = re.sub(r"//[^\n]*", "", text)
        self.regions = []
        control = {"default_path": ""}
        glob, group, cur, section = {}, {}, None, None
        for part in re.split(r"(<\w+>)", text):
            if part.startswith("<") and part.endswith(">"):
                if cur is not None:
                    self._add(cur, control, glob, group, base)
                    cur = None
                section = part[1:-1]
                if section == "group":
                    group = {}
                elif section == "global":
                    glob = {}
                elif section == "region":
                    cur = {}
                continue
            target = {"control": control, "global": glob, "group": group, "region": cur}.get(section)
            if target is None:
                continue
            for line in part.splitlines():
                for mt in re.finditer(r"([\w#]+)=(.+?)(?=\s+[\w#]+=|\s*$)", line.strip()):
                    target[mt.group(1)] = mt.group(2).strip()
        if cur is not None:
            self._add(cur, control, glob, group, base)

    def _add(self, ops, control, glob, group, base):
        merged = dict(control)
        merged.update(glob)
        merged.update(group)
        merged.update(ops)
        if "sample" not in merged:
            return
        r = Region(merged, base)
        if r.trigger == "attack" and os.path.exists(r.path):
            self.regions.append(r)

    def pick(self, note, vel):
        cands = [r for r in self.regions if r.lokey <= note <= r.hikey]
        if not cands:
            cands = sorted(self.regions, key=lambda r: abs(r.center - note))[:4]
        best = [r for r in cands if r.lovel <= vel <= r.hivel] or cands
        return min(best, key=lambda r: abs(r.center - note))


# ------------------------------------------------------------- synthesis ---
def onset(d, frac=0.06):
    env = np.abs(d).max(axis=1)
    peak = env.max()
    if peak <= 0:
        return 0
    i = int(np.argmax(env > peak * frac))
    return max(0, i - int(0.004 * SR))


def envelope(n, dur, release):
    """1 for dur seconds, then a cosine fade over release seconds."""
    t = np.arange(n) / SR
    e = np.ones(n)
    fade = (t - dur) / max(release, 1e-3)
    e = np.where(t > dur, 0.5 + 0.5 * np.cos(np.pi * np.clip(fade, 0, 1)), e)
    return e


class Mix:
    def __init__(self, seconds):
        self.buf = np.zeros((int(seconds * SR), 2))

    def add(self, d, at, gain=1.0, pan=0.0, dur=None, release=0.35, align=True, norm=True):
        if align:
            d = d[onset(d):]
        if dur is not None:
            n = min(len(d), int((dur + release) * SR))
            d = d[:n] * envelope(n, dur, release)[:, None]
        if norm:
            # Level each note by its loudest 0.2 s, so instruments sit by the gains in the score, not their recording.
            win = int(0.2 * SR)
            mono = d.mean(axis=1)
            if len(mono) > win:
                c = np.cumsum(mono ** 2)
                rms = math.sqrt(max((c[win:] - c[:-win]).max() / win, 1e-12))
            else:
                rms = math.sqrt(max(np.mean(mono ** 2), 1e-12))
            d = d * (0.12 / rms)
        a = (pan + 1) * math.pi / 4
        d = d * np.array([math.cos(a), math.sin(a)]) * math.sqrt(2) * gain
        i = int(at * SR)
        if i >= len(self.buf):
            return
        n = min(len(d), len(self.buf) - i)
        self.buf[i:i + n] += d[:n]


class Library:
    def __init__(self, base):
        self.base = base
        self.sfz = {}

    def inst(self, name):
        if name not in self.sfz:
            self.sfz[name] = Sfz(os.path.join(self.base, name + ".sfz"), self.base)
        return self.sfz[name]

    def note(self, inst, midi, vel=90):
        r = self.inst(inst).pick(midi, vel)
        d = load(r.path)
        return resample(d, 2 ** ((midi - r.center - r.tune) / 12.0))

    def file(self, rel, semis=0.0):
        path = rel if os.path.isabs(rel) else os.path.join(self.base, rel.replace("/", os.sep))
        d = load(path)
        return resample(d, 2 ** (semis / 12.0)) if semis else d


# ---------------------------------------------------------------- reverb ---
def temple_ir(seconds=3.0, seed=3):
    """A large dark stone hall: early reflections, then a long decay that loses its highs as it goes."""
    rng = np.random.default_rng(seed)
    n = int(seconds * SR)
    t = np.arange(n) / SR
    ir = np.zeros((n, 2))
    for ch in range(2):
        bright = rng.standard_normal(n) * np.exp(-t / 0.16)
        dark = rng.standard_normal(n)
        # crude low-pass: moving averages
        for k in (9, 23):
            dark = np.convolve(dark, np.ones(k) / k, mode="same")
        dark *= np.exp(-t / 0.42) * 3.2
        ir[:, ch] = bright * 0.35 + dark
        for d_ms, g in ((17, 0.5), (29, 0.38), (43, 0.3), (61, 0.24), (83, 0.18)):
            j = int((d_ms + ch * 3.7) * SR / 1000)
            ir[j, ch] += g * (1 if (d_ms + ch) % 2 else -1)
    pre = int(0.018 * SR)
    ir = np.vstack([np.zeros((pre, 2)), ir])
    return ir / np.sqrt((ir ** 2).sum(axis=0)).max()


def convolve(x, ir):
    n = len(x) + len(ir) - 1
    size = 1 << (n - 1).bit_length()
    out = np.zeros((n, 2))
    for c in range(2):
        out[:, c] = np.fft.irfft(np.fft.rfft(x[:, c], size) * np.fft.rfft(ir[:, c], size), size)[:n]
    return out


def highpass(x, hz=32.0):
    """One-pole high-pass (x minus its exponential low-pass), done as a convolution."""
    a = math.exp(-2 * math.pi * hz / SR)
    k = (1 - a) * a ** np.arange(int(12 / (1 - a)))
    return x - np.stack([np.convolve(x[:, c], k)[:len(x)] for c in range(2)], axis=1)


def master(dry, ir, wet=0.32):
    rev = convolve(dry, ir)
    out = np.zeros_like(rev)
    out[:len(dry)] += dry
    out += rev * wet * 2.2
    out = highpass(out)
    # trim the tail once it is inaudible
    env = np.abs(out).max(axis=1)
    last = len(env) - 1 - int(np.argmax(env[::-1] > env.max() * 0.003))
    out = out[:min(len(out), last + int(0.05 * SR))]
    fade = int(0.25 * SR)
    out[-fade:] *= np.linspace(1, 0, fade)[:, None] ** 2
    win = int(0.5 * SR)
    mono = out.mean(axis=1)
    c = np.cumsum(mono ** 2)
    rms = math.sqrt((c[win:] - c[:-win]).max() / win)
    gain = 10 ** (TARGET_RMS_DB / 20) / rms
    gain = min(gain, PEAK_CEILING / np.abs(out).max())
    return out * gain


# ----------------------------------------------------------------- score ---
# Helpers shared by every score. b = beat time in seconds.
def temple_bed(L, m, climax, grand):
    """The shared ritual bed: a soft cymbal crescendo that peaks on the climax, and a deep drum on it."""
    swell = L.file("VSCO 1 Percussion/varMetal/Cymbals/susp/susp_hit_softmall_roll2_cresc.wav")
    # Find the crescendo's peak and line it up with the climax.
    env = np.abs(swell).max(axis=1)
    k = int(0.05 * SR)
    sm = np.convolve(env, np.ones(k) / k, mode="same")
    peak = int(np.argmax(sm))
    start = climax - peak / SR
    piece = swell[:peak + int(0.06 * SR)]
    if start < 0:
        piece = piece[int(-start * SR):]
        start = 0.0
    m.add(piece * envelope(len(piece), len(piece) / SR - 0.06, 0.06)[:, None], start,
          gain=0.55 if grand else 0.35, align=False, norm=True)
    m.add(L.note("Timpani", 38, 120 if grand else 90), climax, gain=1.3 if grand else 0.9, dur=2.2, release=0.8)
    if grand:
        m.add(L.file("VSCO 1 Percussion/drums/bass/bdrum3_fff_1.wav"), climax, gain=0.9, dur=1.5, release=0.6)


def chord(L, m, inst, notes, at, vel, gain, dur, release=0.6, roll=0.0, pan_spread=0.5):
    for i, n in enumerate(notes):
        pan = 0 if len(notes) == 1 else (i / (len(notes) - 1) - 0.5) * 2 * pan_spread
        m.add(L.note(inst, n, vel), at + i * roll, gain=gain, pan=pan, dur=dur, release=release)


def gong(L, m, at, level="ff", gain=0.8):
    m.add(L.file("VSCO 1 Percussion/varMetal/Gong/gong_hit_%s.wav" % level), at, gain=gain, dur=3.0, release=1.0)


def crash(L, m, at, gain=0.6):
    m.add(L.file("VSCO 1 Percussion/varMetal/Cymbals/clash/crash_hit_ff_loose.wav"), at, gain=gain, dur=2.5, release=1.0)


# MIDI: 38 D2, 45 A2, 50 D3, 53 F3, 55 G3, 57 A3, 62 D4, 64 E4, 65 F4, 66 F#4, 67 G4, 69 A4, 70 Bb4, 74 D5
def s_zeus_major(L, m, b):
    chord(L, m, "TrumpetSus", [57], b[0], 80, 0.8, 0.34)
    chord(L, m, "FHornSus", [45], b[0], 80, 0.6, 0.34)
    chord(L, m, "TrumpetSus", [62], b[1], 95, 0.9, 0.42)
    chord(L, m, "FHornSus", [50], b[1], 95, 0.7, 0.42)
    chord(L, m, "TrumpetSus", [62, 69], b[2], 120, 1.0, 1.5, 0.9)
    chord(L, m, "FHornSus", [50, 57], b[2], 120, 0.8, 1.5, 0.9)
    chord(L, m, "TromboneSus", [38], b[2], 120, 0.8, 1.5, 0.9)
    crash(L, m, b[2], 0.55)


def s_zeus_minor(L, m, b):
    chord(L, m, "TrumpetSus", [62], b[0], 85, 0.8, 0.5)
    chord(L, m, "FHornSus", [50], b[0], 85, 0.6, 0.5)
    chord(L, m, "TrumpetSus", [69], b[1], 115, 1.0, 1.2, 0.8)
    chord(L, m, "FHornSus", [57, 50], b[1], 110, 0.7, 1.2, 0.8)


def s_poseidon_major(L, m, b):
    chord(L, m, "FHornSus", [45], b[0], 80, 0.8, 0.36)
    chord(L, m, "TubaSus", [33], b[0], 80, 0.7, 0.36)
    chord(L, m, "FHornSus", [48], b[1], 90, 0.8, 0.46)
    chord(L, m, "TubaSus", [36], b[1], 90, 0.7, 0.46)
    chord(L, m, "FHornSus", [50, 57], b[2], 120, 0.9, 1.6, 1.0)
    chord(L, m, "TubaSus", [38], b[2], 120, 0.9, 1.6, 1.0)
    # the wave: a harp glissando up through D minor into the climax
    for i, n in enumerate([62, 64, 65, 67, 69, 70, 72, 74]):
        m.add(L.note("Harp", n, 90), b[2] - 0.20 + i * 0.03, gain=0.45, pan=-0.4 + i * 0.1, dur=0.9, release=0.6)
    crash(L, m, b[2], 0.5)


def s_poseidon_minor(L, m, b):
    m.add(L.file("Percussion/Timpani/Rolls/Timpani1_Roll_v3_rr1_Sum.wav"), b[0], gain=0.5, dur=0.6, release=0.15)
    chord(L, m, "TubaSus", [38], b[0], 85, 0.8, 0.55)
    chord(L, m, "FHornSus", [50], b[0], 85, 0.7, 0.55)
    chord(L, m, "TubaSus", [33], b[1], 115, 1.0, 1.3, 0.9)
    chord(L, m, "FHornSus", [45, 52], b[1], 110, 0.8, 1.3, 0.9)
    crash(L, m, b[1], 0.4)


def s_hades_major(L, m, b):
    chord(L, m, "ContrabassSusVB", [38], b[0], 90, 0.9, 0.36)
    chord(L, m, "BassoonSus", [50], b[0], 80, 0.5, 0.36)
    chord(L, m, "ContrabassSusVB", [39], b[1], 100, 0.9, 0.46)
    chord(L, m, "BassoonSus", [51], b[1], 90, 0.5, 0.46)
    chord(L, m, "TubaSus", [26], b[2], 120, 1.0, 1.8, 1.0)
    chord(L, m, "ContrabassSusVB", [38], b[2], 120, 0.8, 1.8, 1.0)
    chord(L, m, "BassoonSus", [45], b[2], 110, 0.5, 1.8, 1.0)
    gong(L, m, b[2], "ff", 0.9)


def s_hades_minor(L, m, b):
    chord(L, m, "ContrabassPizz", [45], b[0], 100, 0.9, 0.5)
    chord(L, m, "BassoonSus", [57], b[0], 80, 0.45, 0.5)
    chord(L, m, "TubaSus", [38], b[1], 115, 1.0, 1.4, 0.9)
    chord(L, m, "ContrabassSusVB", [38], b[1], 110, 0.7, 1.4, 0.9)
    gong(L, m, b[1], "mf", 0.8)


def s_ares_major(L, m, b):
    tenor = "VSCO 1 Percussion/drums/tenor/tenor_lower/tenor_fff_%d.wav"
    for i, t in enumerate([b[0], b[0] + 0.2, b[1], b[1] + 0.25]):
        m.add(L.file(tenor % (i % 3 + 1)), t, gain=0.7 if i % 2 == 0 else 0.45, pan=-0.3 + 0.2 * i, dur=0.4, release=0.2)
    chord(L, m, "TromboneStac", [50], b[0], 110, 0.9, 0.3, 0.2)
    chord(L, m, "TubaStac", [38], b[0], 110, 0.7, 0.3, 0.2)
    chord(L, m, "TromboneStac", [50], b[1], 115, 0.9, 0.3, 0.2)
    chord(L, m, "TubaStac", [38], b[1], 115, 0.7, 0.3, 0.2)
    chord(L, m, "TromboneSus", [50, 57], b[2], 125, 1.0, 1.3, 0.8)
    chord(L, m, "TubaSus", [38], b[2], 125, 0.9, 1.3, 0.8)
    crash(L, m, b[2], 0.6)


def s_ares_minor(L, m, b):
    tenor = "VSCO 1 Percussion/drums/tenor/tenor_lower/tenor_fff_%d.wav"
    m.add(L.file(tenor % 1), b[0], gain=0.7, dur=0.4, release=0.2)
    m.add(L.file(tenor % 2), b[0] + 0.3, gain=0.45, pan=0.3, dur=0.4, release=0.2)
    m.add(L.file(tenor % 3), b[0] + 0.45, gain=0.5, pan=-0.3, dur=0.4, release=0.2)
    chord(L, m, "TromboneStac", [45], b[0], 110, 0.9, 0.3, 0.2)
    chord(L, m, "TubaStac", [33], b[0], 110, 0.6, 0.3, 0.2)
    chord(L, m, "TromboneSus", [50], b[1], 125, 1.0, 1.0, 0.7)
    chord(L, m, "TubaSus", [38], b[1], 125, 0.8, 1.0, 0.7)


def s_athena_major(L, m, b):
    chord(L, m, "FHornSus", [50], b[0], 85, 0.8, 0.36)
    chord(L, m, "FluteSusVib", [74], b[0], 80, 0.35, 0.36)
    chord(L, m, "FHornSus", [55], b[1], 95, 0.8, 0.46)
    chord(L, m, "FluteSusVib", [79], b[1], 90, 0.35, 0.46)
    chord(L, m, "FHornSus", [50, 57], b[2], 115, 0.9, 1.6, 1.0)
    chord(L, m, "FluteSusVib", [81], b[2], 100, 0.4, 1.6, 1.0)
    chord(L, m, "CelloEnsSusVib", [38], b[2], 110, 0.6, 1.6, 1.0)
    m.add(L.note("TubularBells", 62, 100), b[2], gain=0.5, pan=0.3, dur=2.0, release=1.0)


def s_apollo_major(L, m, b):
    chord(L, m, "Harp", [62], b[0], 100, 0.9, 0.9)
    chord(L, m, "Glockenspiel", [86], b[0], 80, 0.35, 0.9)
    chord(L, m, "Harp", [66], b[1], 105, 0.9, 0.9)
    chord(L, m, "Glockenspiel", [90], b[1], 85, 0.35, 0.9)
    chord(L, m, "Harp", [50, 57, 62, 66, 69, 74], b[2] - 0.1, 120, 0.7, 2.0, 1.0, roll=0.02)
    chord(L, m, "Glockenspiel", [93], b[2], 100, 0.45, 1.5)
    chord(L, m, "ViolinEnsSusVib", [74, 78], b[2], 100, 0.6, 1.5, 1.0)
    chord(L, m, "FHornSus", [50], b[2], 100, 0.5, 1.5, 1.0)


def s_apollo_minor(L, m, b):
    chord(L, m, "Harp", [69], b[0], 100, 0.9, 0.9)
    chord(L, m, "Glockenspiel", [93], b[0], 80, 0.35, 0.9)
    chord(L, m, "Harp", [50, 62, 66, 69, 74], b[1] - 0.08, 120, 0.75, 1.6, 0.9, roll=0.02)
    chord(L, m, "Glockenspiel", [98], b[1], 95, 0.4, 1.2)
    chord(L, m, "ViolinEnsSusVib", [74], b[1], 95, 0.45, 1.2, 0.9)


def s_aphrodite_major(L, m, b):
    chord(L, m, "Harp", [62], b[0], 95, 0.85, 0.9)
    chord(L, m, "ViolinEnsSusVib", [74], b[0], 80, 0.5, 0.36)
    chord(L, m, "Harp", [70], b[1], 100, 0.85, 0.9)
    chord(L, m, "ViolinEnsSusVib", [82], b[1], 90, 0.55, 0.46)
    chord(L, m, "Harp", [50, 57, 62, 65, 69], b[2] - 0.08, 115, 0.7, 1.8, 1.0, roll=0.02)
    chord(L, m, "ViolinEnsSusVib", [81], b[2], 105, 0.6, 1.6, 1.0)
    chord(L, m, "FluteSusVib", [81], b[2], 95, 0.35, 1.6, 1.0)
    chord(L, m, "CelloEnsSusVib", [50], b[2], 105, 0.55, 1.6, 1.0)


def s_aphrodite_minor(L, m, b):
    chord(L, m, "Harp", [65], b[0], 95, 0.85, 0.9)
    chord(L, m, "FluteSusVib", [77], b[0], 80, 0.35, 0.5)
    chord(L, m, "Harp", [50, 57, 62, 65], b[1] - 0.06, 110, 0.7, 1.4, 0.9, roll=0.02)
    chord(L, m, "ViolinEnsSusVib", [74], b[1], 100, 0.55, 1.3, 0.9)
    chord(L, m, "CelloEnsSusVib", [50], b[1], 95, 0.45, 1.3, 0.9)


def s_demeter_major(L, m, b):
    chord(L, m, "OboeSusVib", [62], b[0], 85, 0.7, 0.36)
    chord(L, m, "CelloEnsPizz", [50], b[0], 100, 0.8, 0.5)
    chord(L, m, "OboeSusVib", [64], b[1], 95, 0.7, 0.46)
    chord(L, m, "CelloEnsPizz", [45], b[1], 105, 0.8, 0.5)
    chord(L, m, "OboeSusVib", [69], b[2], 110, 0.75, 1.5, 1.0)
    chord(L, m, "FHornSus", [57, 50], b[2], 105, 0.7, 1.5, 1.0)
    chord(L, m, "CelloEnsSusVib", [38], b[2], 105, 0.6, 1.5, 1.0)


def s_demeter_minor(L, m, b):
    chord(L, m, "OboeSusVib", [64], b[0], 85, 0.7, 0.5)
    chord(L, m, "CelloEnsPizz", [45], b[0], 100, 0.8, 0.5)
    chord(L, m, "OboeSusVib", [62], b[1], 105, 0.75, 1.2, 0.9)
    chord(L, m, "FHornSus", [50], b[1], 100, 0.6, 1.2, 0.9)
    chord(L, m, "CelloEnsSusVib", [38], b[1], 100, 0.55, 1.2, 0.9)


def s_dionysus_minor(L, m, b):
    hand = "VSCO 1 Percussion/drums/other/ethnic/giant/hand/EthnicLargeHand_hit_%s.wav"
    for t, lvl, g, p in ((b[0], "ff_1", 0.7, -0.2), (b[0] + 0.3, "mp_1", 0.4, 0.3), (b[0] + 0.45, "f_1", 0.5, -0.3)):
        m.add(L.file(hand % lvl), t, gain=g, pan=p, dur=0.5, release=0.2)
    chord(L, m, "OboeSusVib", [63], b[0], 95, 0.75, 0.5)
    chord(L, m, "ClarinetSus", [51], b[0], 85, 0.45, 0.5)
    chord(L, m, "CelloEnsPizz", [39], b[0], 100, 0.6, 0.5)
    chord(L, m, "OboeSusVib", [62], b[1], 110, 0.8, 1.2, 0.9)
    chord(L, m, "ClarinetSus", [50, 57], b[1], 100, 0.5, 1.2, 0.9)
    m.add(L.file(hand % "ff_2"), b[1], gain=0.6, dur=0.6, release=0.3)


def s_hephaestus_major(L, m, b):
    anvil = "Percussion/Anvil_Hit1_v%d_Sum.wav"
    m.add(L.file(anvil % 2), b[0], gain=0.7, pan=-0.2, dur=0.8, release=0.4)
    m.add(L.file(anvil % 2, -1), b[1], gain=0.75, pan=0.2, dur=0.8, release=0.4)
    m.add(L.file(anvil % 3, -3), b[2], gain=0.9, dur=1.4, release=0.8)
    chord(L, m, "TromboneStac", [50], b[0], 110, 0.8, 0.3, 0.2)
    chord(L, m, "TromboneStac", [53], b[1], 115, 0.8, 0.3, 0.2)
    chord(L, m, "TromboneSus", [50, 45], b[2], 125, 0.9, 1.4, 0.9)
    chord(L, m, "TubaSus", [26], b[2], 125, 0.9, 1.4, 0.9)


def s_hera_major(L, m, b):
    chord(L, m, "FHornSus", [53], b[0], 85, 0.8, 0.36)
    chord(L, m, "ViolaEnsSusVib", [65], b[0], 80, 0.45, 0.36)
    chord(L, m, "FHornSus", [57], b[1], 95, 0.8, 0.46)
    chord(L, m, "ViolinEnsSusVib", [69], b[1], 90, 0.5, 0.46)
    chord(L, m, "FHornSus", [50, 62], b[2], 120, 0.9, 1.6, 1.0)
    chord(L, m, "TrumpetSus", [62], b[2], 110, 0.6, 1.6, 1.0)
    chord(L, m, "ViolinEnsSusVib", [74, 81], b[2], 110, 0.6, 1.6, 1.0)
    m.add(L.note("TubularBells", 74, 110), b[2], gain=0.5, pan=-0.3, dur=2.0, release=1.0)
    gong(L, m, b[2], "mf", 0.6)


def s_hermes_major(L, m, b):
    tri = "VSCO 1 Percussion/varMetal/triangle/1/triangle1_hit_mp.wav"
    chord(L, m, "FluteStac", [81], b[0], 100, 0.7, 0.25, 0.3)
    chord(L, m, "ViolinEnsPizz", [69], b[0], 100, 0.55, 0.4)
    chord(L, m, "FluteStac", [86], b[1], 105, 0.7, 0.25, 0.3)
    chord(L, m, "ViolinEnsPizz", [74], b[1], 105, 0.55, 0.4)
    m.add(L.file(tri), b[1], gain=0.35, pan=0.4, dur=1.2, release=0.6)
    chord(L, m, "FluteSusVib", [81], b[2], 110, 0.6, 1.3, 0.9)
    chord(L, m, "PiccoloSus", [86], b[2], 100, 0.3, 1.3, 0.9)
    chord(L, m, "Harp", [62, 69, 74], b[2] - 0.04, 115, 0.6, 1.4, 0.9, roll=0.02)
    chord(L, m, "FHornSus", [50], b[2], 100, 0.5, 1.3, 0.9)
    m.add(L.file(tri), b[2], gain=0.45, pan=-0.4, dur=1.5, release=0.8)


def s_hermes_minor(L, m, b):
    tri = "VSCO 1 Percussion/varMetal/triangle/1/triangle1_hit_mp.wav"
    chord(L, m, "FluteStac", [86], b[0], 100, 0.7, 0.25, 0.3)
    chord(L, m, "ViolinEnsPizz", [74], b[0], 100, 0.55, 0.4)
    chord(L, m, "FluteSusVib", [81], b[1], 110, 0.6, 1.1, 0.8)
    chord(L, m, "Harp", [57, 62, 69], b[1] - 0.04, 110, 0.6, 1.2, 0.8, roll=0.02)
    m.add(L.file(tri), b[1], gain=0.45, pan=0.4, dur=1.4, release=0.7)


def s_hestia_minor(L, m, b):
    chord(L, m, "CelloEnsSusVib", [50], b[0], 85, 0.75, 0.5)
    chord(L, m, "Harp", [62], b[0], 95, 0.7, 0.9)
    chord(L, m, "CelloEnsSusVib", [45], b[1], 105, 0.7, 1.2, 0.9)
    chord(L, m, "FHornSus", [57], b[1], 100, 0.6, 1.2, 0.9)
    chord(L, m, "Harp", [50, 57, 62, 69], b[1] - 0.06, 110, 0.6, 1.3, 0.9, roll=0.02)


def s_muse_minor(L, m, b):
    chord(L, m, "Harp", [69], b[0], 100, 0.85, 0.9)
    chord(L, m, "Glockenspiel", [93], b[0], 70, 0.25, 0.8)
    chord(L, m, "Harp", [57, 62, 69, 74], b[1] - 0.06, 115, 0.75, 1.4, 0.9, roll=0.025)
    chord(L, m, "Glockenspiel", [98], b[1], 85, 0.3, 1.2)
    chord(L, m, "FluteSusVib", [74], b[1], 90, 0.35, 1.1, 0.8)


def s_oldones_minor(L, m, b):
    chord(L, m, "ContrabassTrem", [38], b[0], 100, 0.8, 0.58, 0.15)
    chord(L, m, "CelloEnsTrem", [50, 51], b[0], 95, 0.5, 0.58, 0.15)
    chord(L, m, "OrganLoudPedal", [26], b[0], 100, 0.5, 0.58, 0.15)
    chord(L, m, "TubaSus", [26], b[1], 125, 1.0, 1.9, 1.0)
    chord(L, m, "OrganLoudPedal", [26, 27], b[1], 120, 0.6, 1.9, 1.0)
    chord(L, m, "ContrabassSusVB", [39], b[1], 120, 0.6, 1.9, 1.0)
    chord(L, m, "TromboneSus", [38, 39], b[1], 120, 0.6, 1.9, 1.0)
    gong(L, m, b[1], "fff", 1.0)


SCORES = {
    "ZeusMajor": s_zeus_major, "ZeusMinor": s_zeus_minor,
    "PoseidonMajor": s_poseidon_major, "PoseidonMinor": s_poseidon_minor,
    "HadesMajor": s_hades_major, "HadesMinor": s_hades_minor,
    "AresMajor": s_ares_major, "AresMinor": s_ares_minor,
    "AthenaMajor": s_athena_major,
    "ApolloMajor": s_apollo_major, "ApolloMinor": s_apollo_minor,
    "AphroditeMajor": s_aphrodite_major, "AphroditeMinor": s_aphrodite_minor,
    "DemeterMajor": s_demeter_major, "DemeterMinor": s_demeter_minor,
    "DionysusMinor": s_dionysus_minor,
    "HephaestusMajor": s_hephaestus_major,
    "HeraMajor": s_hera_major,
    "HermesMajor": s_hermes_major, "HermesMinor": s_hermes_minor,
    "HestiaMinor": s_hestia_minor,
    "MuseMinor": s_muse_minor,
    "OldOnesMinor": s_oldones_minor,
}
# Rituals whose bed is the grand one (bass drum under the timpani, louder swell) even though they are Minor.
GRAND_MINORS = {"OldOnesMinor"}


def render(L, name, ir):
    major = name.endswith("Major")
    beats = MAJOR_BEATS if major else MINOR_BEATS
    m = Mix(6.0)
    temple_bed(L, m, beats[-1], major or name in GRAND_MINORS)
    SCORES[name](L, m, beats)
    return master(m.buf, ir)


# --------------------------------------------------------------- preview ---
def spectrogram_png(stereo, path, beats):
    from PIL import Image, ImageDraw
    mono = stereo.mean(axis=1)
    hop, nfft = 256, 2048
    frames = max(1, (len(mono) - nfft) // hop)
    win = np.hanning(nfft)
    spec = np.array([np.abs(np.fft.rfft(mono[i * hop:i * hop + nfft] * win)) for i in range(frames)]).T
    freqs = np.fft.rfftfreq(nfft, 1 / SR)
    # log frequency axis 30 Hz .. 8 kHz, 256 rows
    rows = np.geomspace(30, 8000, 256)
    idx = np.searchsorted(freqs, rows).clip(0, len(freqs) - 1)
    img = 20 * np.log10(spec[idx] + 1e-6)
    img = np.clip((img - img.max() + 70) / 70, 0, 1)[::-1]
    im = Image.fromarray((img * 255).astype(np.uint8)).resize((frames * 2, 256)).convert("RGB")
    d = ImageDraw.Draw(im)
    for bt in beats:
        x = int(bt * SR / hop * 2)
        d.line([(x, 0), (x, 255)], fill=(220, 120, 60))
    im.save(path)


def write_preview(folder, names, out_dir):
    rows = "\n".join(
        '<div class="r"><h2>%s</h2><audio controls src="%s"></audio><img src="%s.png"></div>'
        % (n, os.path.relpath(os.path.join(out_dir, n + ".wav"), folder).replace(os.sep, "/"), n) for n in names)
    html = """<!doctype html><meta charset="utf-8"><title>Ritual sounds</title>
<style>body{background:#141110;color:#E8DCC2;font-family:sans-serif;margin:24px}.r{margin:0 0 22px}
h2{font-size:16px;margin:0 0 6px}img{display:block;margin-top:6px;max-width:100%%;border:1px solid #333}</style>
<h1>Ritual invocation sounds</h1><p>Orange lines are the VFX beats (Major 0 / 0.4 / 0.9 s, Minor 0 / 0.6 s).</p>
""" + rows
    open(os.path.join(folder, "index.html"), "w", encoding="utf-8").write(html)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--samples", default=DEFAULT_SAMPLES)
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--only", nargs="*")
    ap.add_argument("--preview")
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    L = Library(a.samples)
    ir = temple_ir()
    names = a.only or list(SCORES)
    for n in names:
        audio = render(L, n, ir)
        write_wav(os.path.join(a.out, n + ".wav"), audio)
        peak = np.abs(audio).max()
        print("%-16s %.2fs peak %.2f" % (n, len(audio) / SR, peak))
        if a.preview:
            os.makedirs(a.preview, exist_ok=True)
            spectrogram_png(audio, os.path.join(a.preview, n + ".png"),
                            MAJOR_BEATS if n.endswith("Major") else MINOR_BEATS)
    if a.preview:
        write_preview(a.preview, names, a.out)


if __name__ == "__main__":
    main()
