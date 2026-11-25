"""The Gorgon's cave: wet limestone floor with rimstone terraces and black puddles that catch the light, green slime
in the damp (statues and bodies were tried and dropped: they read as unconvincing and distracting). Lanes: damp packed grit
with shallow puddles."""
from boardkit import *  # noqa

NAME = 'Caves'


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(53)
    rng = np.random.default_rng(53)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 12, ad)

    big = n.fbm(X, Y, 16, 4, offset=1)
    mid = n.fbm(X, Y, 3.5, 4, offset=2)
    grain = n.fbm(X, Y, 0.5, 5, offset=3)
    # rimstone: terraces following the contours of the floor
    terr = n.fbm(X, Y, 8, 3, offset=4) * 9.0
    rim = smoothstep(0.75, 0.97, 1 - np.abs((terr % 1.0) - 0.5) * 2) * smoothstep(0.45, 0.65, mid) * margin * 0.0
    hgt = (big * 0.7 + mid * 0.25 + grain * 0.05 + rim * 0.12).astype(F32)
    stone = lerp(C(0.075, 0.09, 0.08), C(0.19, 0.21, 0.18), np.clip(big * 0.5 + mid * 0.3 + grain * 0.2, 0, 1))
    alb = stone + ((brush(n, X, Y) - 0.5) * 0.03)[..., None]
    alb = lerp(alb, C(0.23, 0.23, 0.2), rim * 0.5)

    # slime and the damp
    damp = smoothstep(0.5, 0.7, n.fbm(X, Y, 6, 4, offset=5)) * (0.4 + 0.6 * margin)
    alb = lerp(alb, C(0.07, 0.11, 0.06), damp * 0.65)

    # puddles in the low places
    low = 1 - norm01(hgt)
    pud = smoothstep(0.74, 0.79, low + (n.fbm(X, Y, 2.0, 3, offset=6) - 0.5) * 0.15) * smoothstep(2, 6, ad)
    alb = lerp(alb, alb * 0.45 + C(0.02, 0.035, 0.04), pud)
    wet_rim = np.clip(blurf(pud, 0.25, g) * 1.8 - pud, 0, 1)
    alb = alb * (1 - wet_rim[..., None] * 0.25)
    hgt = (hgt * (1 - pud) + pud * float(np.percentile(hgt, 30))).astype(F32)

    # loose rubble
    rub = [(x, y, rng.uniform(0.12, 0.5)) for x, y in scatter_points(rng, 400, (-66, 66, -23, 43), margin_keep(0.8), 0.6)]
    rm, rh, rc = rocks(g, n, rng, rub, base=C(0.08, 0.085, 0.08), light=C(0.26, 0.27, 0.25))
    alb, hgt = apply_object(g, alb, hgt, rm, rh * 0.4, rc, shadow=0.5, shadow_blur=0.15)

    cav = cavity(hgt, g, 0.6, 2.5)
    alb = desaturate(alb * (1 - cav[..., None] * 0.5), 0.05)
    smooth = 0.3 + damp * 0.25 + pud * 0.65

    # lanes: damp packed grit
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, C(0.11, 0.115, 0.105), C(0.19, 0.195, 0.18), offset=950)
    wear = lane_wear(lane)
    lg = n.fbm(LX, LY, 0.25, 3, offset=951)
    lalb = lalb * (0.85 + 0.3 * lg)[..., None]
    lp = smoothstep(0.7, 0.76, n.fbm(LX, LY, 2.5, 4, offset=952)) * (1 - wear * 0.7)
    lalb = lerp(lalb, C(0.04, 0.05, 0.05), lp * 0.7)
    peb = [(rng.uniform(-29.5, 29.5), rng.uniform(0.2, 19.8), rng.uniform(0.05, 0.16)) for _ in range(300)]
    pm, ph, pc = rocks(lane, n, rng, peb, base=C(0.1, 0.1, 0.095), light=C(0.27, 0.27, 0.25))
    lalb, lhgt = apply_object(lane, lalb, lhgt, pm, ph * 0.4, pc, shadow=0.4)
    lcav = cavity(lhgt, lane, 0.4, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.3), 0.05)
    lsmooth = 0.32 + lp * 0.55

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
