"""Checks in-game captures of the combat board against the board spec (Docs/StyleGuide.md, "Board spec").

  python Tools/Boards/check.py <folder of 1920x1080 captures named <Board>.png>

Measures the gap between the lanes (arena), a right-hand margin and the player's cards in each capture, and flags what
breaks the spec: gap luminance above 0.20, a mid-value gap (L > 0.18) with saturation above 0.40, margins above 0.12,
gap luminance spread above 0.05, or a top of frame that doesn't fade to black (top 3% above 0.025). Regions assume the
default combat layout (enemy lane moved down 2 units)."""
import glob
import os
import sys

import numpy as np
from PIL import Image

REGIONS = {
    'gap': (0.30, 0.395, 0.70, 0.425),
    'margin': (0.86, 0.18, 0.97, 0.32),
    'cards': (0.30, 0.46, 0.66, 0.55),
    'top': (0.25, 0.0, 0.75, 0.03),          # the strip along the top edge, which must fade to black
}
W = np.array((0.2126, 0.7152, 0.0722))


def measure(path):
    im = np.asarray(Image.open(path).convert('RGB')).astype(float) / 255
    h, w, _ = im.shape
    out = {}
    for name, (x0, y0, x1, y1) in REGIONS.items():
        p = im[int(y0 * h):int(y1 * h), int(x0 * w):int(x1 * w)].reshape(-1, 3)
        lum = p @ W
        mx, mn = p.max(1), p.min(1)
        sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
        out[name] = (float(lum.mean()), float(lum.std()), float(sat.mean()))
    return out


def problems(m):
    gl, gs, gsat = m['gap']
    ml = m['margin'][0]
    p = []
    if gl > 0.20:
        p.append('gap too bright (%.2f)' % gl)
    if gl > 0.18 and gsat > 0.40:
        p.append('gap too saturated for its value (%.2f)' % gsat)
    if ml > 0.12:
        p.append('margin too bright (%.2f)' % ml)
    if gs > 0.05:
        p.append('gap too busy (spread %.2f)' % gs)
    if m['top'][0] > 0.025:
        p.append('top of frame not fading to black (%.3f)' % m['top'][0])
    return p


if __name__ == '__main__':
    folder = sys.argv[1]
    for path in sorted(glob.glob(os.path.join(folder, '*.png'))):
        m = measure(path)
        p = problems(m)
        print('%-12s gap L%.2f±%.2f S%.2f  margin L%.2f  cards L%.2f  top L%.3f  %s' % (
            os.path.splitext(os.path.basename(path))[0], *m['gap'], m['margin'][0], m['cards'][0], m['top'][0],
            'OK' if not p else '; '.join(p)))
