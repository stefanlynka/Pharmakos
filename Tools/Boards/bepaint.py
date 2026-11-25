"""Painters for back-edge materials. Each takes a Material (or a Canvas) and returns (albedo, height, smoothness) for
its elevation canvas; tops() paints the band seen from above. All colours are display values, already at final
brightness (no board gain)."""
import math

import numpy as np

from boardgen import F32, C, Noise, lerp, smoothstep, norm01, desaturate, brush, blurf
from boardkit import flagstones, marble as _marble


def age(n, X, H, alb, foot_dark=0.45, streaks=0.3, foot_h=0.8, offset=900):
    """Darkens the foot and runs grime down from the top."""
    st = n.value(X * 2.2, H * 0.2, 1.0, offset) * 0.7 + n.fbm(X, H, 1.2, 3, offset=offset + 1) * 0.3
    st = smoothstep(0.55, 0.85, st) * smoothstep(0.3, 4.0, H)
    alb = alb * (1 - st * streaks)[..., None]
    alb = alb * (1 - (1 - smoothstep(0.0, foot_h, H)) * foot_dark)[..., None]
    return alb


def masonry(c, n, bw, bh, dark, light, joint_col, gap=0.04, jitter=0.35, offset=10, rough=0.4):
    X, H = c.X, c.Y
    j, sid, edge = flagstones(n, X, H, bw, bh, gap=gap, jitter=jitter, offset=offset)
    grain = n.fbm(X, H, 0.35, 5, offset=offset + 1)
    alb = lerp(dark, light, np.clip(sid * 0.45 + grain * 0.4 + n.fbm(X, H, 4, 3, offset=offset + 2) * 0.15, 0, 1))
    chip = smoothstep(0.6, 0.78, n.fbm(X, H, 0.3, 3, offset=offset + 3)) * (1 - smoothstep(0.03, 0.15, edge))
    j = np.clip(j + chip, 0, 1)
    alb = lerp(alb, joint_col, j * 0.75)
    alb = alb + ((brush(n, X, H) - 0.5) * 0.04)[..., None]
    hgt = (sid * 0.02 - j * 0.1 + grain * 0.04 * rough).astype(F32)
    return alb.astype(F32), hgt, np.full(X.shape, 0.15, F32)


def rock(c, n, dark, light, crack_col=C(0.02, 0.02, 0.02), scale=1.0, strata=0.3, offset=20):
    X, H = c.X, c.Y
    big = n.fbm(X, H, 5 * scale, 4, offset=offset)
    mid = n.fbm(X, H, 1.2 * scale, 4, offset=offset + 1)
    grain = n.fbm(X, H, 0.3 * scale, 4, offset=offset + 2)
    strat = n.ridged(X * 0.35, H * 1.6, 2.5 * scale, 3, offset=offset + 3)
    crack = smoothstep(0.88, 0.96, n.ridged(X * 0.6, H, 1.6 * scale, 3, offset=offset + 4))
    alb = lerp(dark, light, np.clip(big * 0.35 + mid * 0.35 + grain * 0.3, 0, 1))
    alb = lerp(alb, alb * 1.25, smoothstep(0.7, 0.9, strat) * strata)
    alb = lerp(alb, crack_col, crack * 0.8)
    alb = alb + ((brush(n, X, H) - 0.5) * 0.04)[..., None]
    hgt = (big * 0.3 + mid * 0.15 + grain * 0.05 + strat * 0.06 * strata - crack * 0.12).astype(F32)
    return alb.astype(F32), hgt, (0.2 + 0.1 * mid).astype(F32)


def marble(c, n, dark, light, vein, amount=0.6, width=0.05, offset=30):
    X, H = c.X, c.Y
    col, veins = _marble(n, X, H, dark, light, vein, amount, width, offset=offset)
    return col.astype(F32), (n.fbm(X, H, 0.5, 3, offset=offset + 9) * 0.02).astype(F32), np.full(X.shape, 0.45, F32)


def wood(c, n, dark, light, offset=40, vertical=True):
    X, H = c.X, c.Y
    a, b = (X, H) if vertical else (H, X)
    grain = n.value(a * 3.0, b * 0.15, 1.0, offset) * 0.6 + n.fbm(a * 2.0, b * 0.3, 0.5, 3, offset=offset + 1) * 0.4
    knots = smoothstep(0.8, 0.9, n.fbm(X, H, 0.6, 3, offset=offset + 2))
    alb = lerp(dark, light, grain)
    alb = lerp(alb, dark * 0.6, knots * 0.6)
    return alb.astype(F32), (grain * 0.05 - knots * 0.03).astype(F32), np.full(X.shape, 0.2, F32)


def metal(c, n, dark, light, patina=None, patina_amt=0.5, offset=50):
    X, H = c.X, c.Y
    g = n.fbm(X, H, 0.6, 4, offset=offset)
    alb = lerp(dark, light, g)
    if patina is not None:
        p = smoothstep(0.55, 0.75, n.fbm(X, H, 1.5, 4, offset=offset + 1))
        alb = lerp(alb, patina, p * patina_amt)
    return alb.astype(F32), (g * 0.03).astype(F32), np.full(X.shape, 0.55, F32)


def tops(mat, n, dark, light, offset=60):
    tp = mat.top_canvas()
    g = n.fbm(tp.X, tp.Y, 0.5, 5, offset=offset)
    alb = lerp(dark, light, np.clip(g * 0.7 + n.fbm(tp.X, tp.Y, 3, 3, offset=offset + 1) * 0.3, 0, 1))
    return alb.astype(F32), (g * 0.05).astype(F32), np.full(tp.X.shape, 0.15, F32)
