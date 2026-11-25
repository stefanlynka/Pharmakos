"""Building blocks for back edges (see backedge.py): steps, colonnades, candles, braziers, rock ridges, trees.
All in board units; y_foot ~34 is the usual front line (just behind the enemy portrait)."""
import math

import numpy as np

from boardgen import F32, C, Noise, lerp, smoothstep, desaturate, brush, blurf
from backedge import Material, save_material


def steps(be, mat, x0, x1, y0, count, rise, tread, top_mat=None):
    """Tiers rising away from the board. Returns (y, h) of the top surface's front edge."""
    for i in range(count):
        be.box(mat, x0, x1, y0 + i * tread, y0 + (i + 1) * tread + 0.01, i * rise, (i + 1) * rise, sides=True,
               top_mat=top_mat)
    for i in range(count):      # fill under the higher tiers so nothing shows through
        pass
    return y0 + count * tread, count * rise


def doric_column(be, mat, x, y, h0, height, r, flutes=20):
    prof = [(h0, r * 1.02), (h0 + height * 0.5, r * 0.96), (h0 + height * 0.85, r * 0.88),
            (h0 + height * 0.92, r * 1.18), (h0 + height * 0.95, r * 1.25), (h0 + height, r * 1.25)]
    be.lathe(mat, x, y, prof, segments=24, flutes=flutes, flute_depth=0.06)


def candle(be, wax, flame, x, y, h0, height, r):
    be.lathe(wax, x, y, [(h0, r * 1.15), (h0 + 0.05, r), (h0 + height, r * 0.95)], segments=10)
    fh = r * 2.6
    be.lathe(flame, x, y, [(h0 + height + 0.02, 0.0), (h0 + height + fh * 0.25, r * 0.6), (h0 + height + fh * 0.55, r * 0.5),
                           (h0 + height + fh, 0.0)], segments=8, cap=False)


def brazier(be, metal, flame, x, y, h0, r=1.2):
    be.lathe(metal, x, y, [(h0, r * 0.35), (h0 + 1.2, r * 0.18), (h0 + 1.25, r * 0.5), (h0 + 1.6, r * 0.95), (h0 + 1.75, r)],
             segments=16)
    for k, (dx, dy, s) in enumerate([(0, 0, 1.0), (-0.35, 0.1, 0.7), (0.4, -0.05, 0.75)]):
        be.lathe(flame, x + dx * r, y + dy * r, [(h0 + 1.6, r * 0.55 * s), (h0 + 2.1, r * 0.5 * s), (h0 + 2.7, r * 0.25 * s),
                                                 (h0 + 3.3 * s + 0.2, 0.0)], segments=10, cap=False)


def paint_flame(folder, mat, n):
    c = mat.elev_canvas()
    X, H = c.X, c.Y
    flick = n.fbm(X, H, 0.4, 3, offset=700)
    alb = lerp(C(0.9, 0.45, 0.1), C(1.0, 0.85, 0.45), flick)
    em = lerp(C(1.0, 0.42, 0.08), C(1.0, 0.8, 0.45), flick) * 1.0
    save_material(folder, mat, alb.astype(F32), np.zeros(X.shape, F32), np.zeros(X.shape, F32), emission=em.astype(F32))


def flame_material(name='Flame', tex_h=8.0):
    return Material(name, tex_h=tex_h, self_light=0.0, emission_strength=1.6)
