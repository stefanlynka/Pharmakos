"""Copies just the samples build_ritual_sounds.py uses out of a full VSCO 2 CE folder into Tools/Audio/Samples, trimmed
to their first TRIM_SECONDS (the renders never read further), together with the .sfz maps and the CC0 licence.

Run it again after adding or changing a score that needs new notes or instruments:
    python Tools/Audio/extract_samples.py --full <full VSCO 2 CE folder> [--trim 4.5]
Then check the renders are unchanged (or changed only where you meant): build_ritual_sounds.py --out <temp folder>.
Samples outside Assets are never imported by Unity or shipped; the game only uses the rendered clips.
"""
import argparse
import os
import shutil
import wave

import numpy as np

import build_ritual_sounds as b

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_DEST = os.path.join(HERE, "Samples")


def copy_trimmed(src, dst, seconds):
    """Keeps the first `seconds`, and never cuts before the file's loudest point: build_ritual_sounds lines each note up
    against its peak (onset), so dropping a late peak (slow-swelling sustains) would shift the note."""
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    data = b.read_wav(src)  # resampled to SR; only used to find the peak
    peak_seconds = float(np.argmax(np.abs(data).max(axis=1))) / b.SR
    with wave.open(src) as r:
        params = r.getparams()
        keep = max(seconds, peak_seconds + 0.05)
        frames = r.readframes(min(r.getnframes(), int(r.getframerate() * keep)))
    with wave.open(dst, "wb") as w:
        w.setparams(params)
        w.writeframes(frames)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--full", required=True, help="the full VSCO 2 CE folder")
    ap.add_argument("--dest", default=DEFAULT_DEST)
    ap.add_argument("--trim", type=float, default=4.5)
    a = ap.parse_args()

    lib = b.Library(a.full)
    for name in b.SCORES:
        b.render(lib, name)

    full = os.path.normpath(a.full)
    total = 0
    for path in sorted(b._cache):
        rel = os.path.relpath(path, full)
        if rel.startswith(".."):
            raise SystemExit("sample outside the VSCO folder: " + path)
        dst = os.path.join(a.dest, rel)
        copy_trimmed(path, dst, a.trim)
        total += os.path.getsize(dst)
    for inst in lib.sfz:
        shutil.copy2(os.path.join(full, inst + ".sfz"), os.path.join(a.dest, inst + ".sfz"))
    for extra in ("LICENSE", "README.md", "Readme.txt"):
        if os.path.exists(os.path.join(full, extra)):
            shutil.copy2(os.path.join(full, extra), os.path.join(a.dest, extra))
    print("%d samples, %d instruments, %.1f MB -> %s" % (len(b._cache), len(lib.sfz), total / 1e6, a.dest))


if __name__ == "__main__":
    main()
