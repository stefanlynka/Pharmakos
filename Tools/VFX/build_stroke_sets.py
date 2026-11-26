"""Builds recoloured brushstroke sets for the stroke projectiles and cue VFX from the painted water strokes.

Each water stroke's brightness (its blue channel) is mapped through a three-stop gradient, alpha kept, so the new sets
keep the hand-painted bristle texture. Palette from Docs/StyleGuide.md:
    Blood  Blood offering red (#9E1B22) from a near-black clot to a wet highlight   (Hera's buff, Vengeance, Price Of...)
    Ink    Kiln Black (#141110) with Bone (#E8DCC2) streaks: shades, curses, fear  (Erinyes, Haunt, Panic)
    Gold   Votive Gold (#D9B45A) from fired umber to pale gold                     (Twist of Fate, gold cues)
    python Tools/VFX/build_stroke_sets.py
Output: Assets/Resources/Images/Particles/<Set>/<n>.png (import settings are copied from the water sprites' .meta by
Unity defaults; set Texture Type = Sprite if a new folder imports as Default).
Requires numpy and Pillow.
"""
import glob
import os

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PARTICLES = os.path.join(ROOT, "Assets", "Resources", "Images", "Particles")

SETS = {
    #         shadow                 mid                    highlight
    "Blood": ((0.16, 0.01, 0.02), (0.62, 0.10, 0.13), (0.88, 0.30, 0.24)),
    "Ink":   ((0.05, 0.04, 0.04), (0.14, 0.12, 0.11), (0.91, 0.86, 0.76)),
    "Gold":  ((0.36, 0.22, 0.07), (0.85, 0.71, 0.35), (1.00, 0.94, 0.74)),
}


def gradient(t, stops):
    lo, mid, hi = (np.array(s) for s in stops)
    t = t[..., None]
    a = lo + (mid - lo) * np.clip(t / 0.55, 0, 1)
    b = mid + (hi - mid) * np.clip((t - 0.55) / 0.45, 0, 1)
    return np.where(t < 0.55, a, b)


def main():
    sources = sorted(glob.glob(os.path.join(PARTICLES, "Water", "*.png")))
    for name, stops in SETS.items():
        out_dir = os.path.join(PARTICLES, name)
        os.makedirs(out_dir, exist_ok=True)
        for path in sources:
            im = np.asarray(Image.open(path).convert("RGBA")).astype(np.float64) / 255.0
            lum = im[..., 2]
            visible = im[..., 3] > 0.05
            lo, hi = np.percentile(lum[visible], 4), np.percentile(lum[visible], 99)
            t = np.clip((lum - lo) / max(hi - lo, 1e-3), 0, 1)
            if name == "Ink":
                t = t ** 2.2  # mostly black, bone only in the brightest bristle streaks
            rgb = gradient(t, stops)
            out = np.concatenate([rgb, im[..., 3:4]], axis=2)
            target = os.path.join(out_dir, os.path.basename(path))
            Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), "RGBA").save(target)
            print(target)


if __name__ == "__main__":
    main()
