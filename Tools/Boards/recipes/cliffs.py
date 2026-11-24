"""The Sirens' rocks: a dark wet shelf of sea-worn rock above the surf, tide pools that hold the light, barnacles,
limpets and kelp, sea foam caught in the hollows, and the sea itself breaking along the far edge. Lanes: flat ledges
of weathered rock crusted with salt."""
from boardkit import *  # noqa

NAME = 'Cliffs'


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(97)
    rng = np.random.default_rng(97)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 12, ad)

    big = n.fbm(X, Y, 12, 5, offset=1)
    mid = n.fbm(X, Y, 3, 4, offset=2)
    grain = n.fbm(X, Y, 0.4, 5, offset=3)
    strata = n.ridged(X * 0.6, Y * 1.6, 5, 3, offset=4)
    hgt = (big * 0.8 + mid * 0.3 + grain * 0.05 + strata * 0.15).astype(F32)
    rock = lerp(C(0.05, 0.06, 0.065), C(0.17, 0.18, 0.18), np.clip(big * 0.4 + mid * 0.35 + grain * 0.25, 0, 1))
    alb = rock + ((brush(n, X, Y) - 0.5) * 0.03)[..., None]
    alb = lerp(alb, C(0.22, 0.22, 0.2), smoothstep(0.75, 0.9, strata) * 0.4)

    # the sea along the far edge: the shoreline wanders, foam breaks on it
    shore = 31.5 + (n.fbm(X, Y, 10, 3, offset=5) - 0.5) * 8.0 + np.abs(X) * 0.06
    sea = smoothstep(shore - 0.3, shore + 0.3, Y)
    depth = smoothstep(shore, shore + 6, Y)
    water = lerp(C(0.03, 0.09, 0.1), C(0.01, 0.03, 0.05), depth)
    swell = n.fbm(X * 0.5, Y * 1.5, 3, 4, offset=6)
    water = water * (0.8 + 0.4 * swell)[..., None]
    foam_line = (1 - smoothstep(0.0, 1.2, np.abs(Y - shore - 0.6))) * smoothstep(0.35, 0.6, n.fbm(X, Y, 1.2, 4, offset=7))
    foam_far = smoothstep(0.72, 0.85, n.fbm(X * 0.4, Y * 2.0, 2, 4, offset=8)) * depth * 0.6
    foam = np.clip(foam_line + foam_far, 0, 1)
    rock_h = hgt.copy()
    alb = lerp(alb, water, sea)
    alb = lerp(alb, C(0.55, 0.6, 0.6), foam * sea * 0.75)
    hgt = (hgt * (1 - sea) + sea * (float(hgt.min()) - 0.3 + swell * 0.08)).astype(F32)

    # tide pools in the hollows: deep green-blue, shining
    low = 1 - norm01(rock_h)
    pool = smoothstep(0.7, 0.76, low + (n.fbm(X, Y, 2, 3, offset=9) - 0.5) * 0.12) * smoothstep(3, 7, ad) * (1 - sea)
    poolcol = lerp(C(0.04, 0.1, 0.1), C(0.08, 0.17, 0.16), n.fbm(X, Y, 1.0, 3, offset=10))
    alb = lerp(alb, poolcol, pool * 0.85)
    pool_rim = np.clip(blurf(pool, 0.3, g) * 1.6 - pool, 0, 1)
    alb = lerp(alb, C(0.2, 0.22, 0.2), pool_rim * 0.35)
    hgt = (hgt * (1 - pool) + pool * float(np.percentile(hgt, 25))).astype(F32)

    # kelp strands and wrack, mostly near the water and pools
    kelp = Layer(g)
    for x, y in scatter_points(rng, 70, (-66, 66, -23, 43), margin_keep(3.0), 2.5):
        a = rng.uniform(0, 6.28)
        for _ in range(rng.integers(2, 6)):
            pts = [(x, y)]
            ka = a + rng.normal(0, 0.5)
            px, py = x, y
            for k in range(rng.integers(8, 20)):
                ka += rng.normal(0, 0.25)
                px += math.cos(ka) * 0.3
                py += math.sin(ka) * 0.3
                pts.append((px, py))
            taper(kelp, pts, rng.uniform(0.18, 0.32), 0.05)
    km = kelp.read() * (1 - sea)
    kcol = lerp(C(0.09, 0.08, 0.03), C(0.2, 0.18, 0.07), n.fbm(X, Y, 0.5, 3, offset=11))
    alb, hgt = apply_object(g, alb, hgt, km, km * 0.05, kcol, shadow=0.4, shadow_blur=0.12)

    # barnacles and limpets in colonies
    bar = Layer(g)
    for x, y in scatter_points(rng, 60, (-66, 66, -23, 43), margin_keep(2.0), 2.5):
        for _ in range(rng.integers(10, 30)):
            r = rng.uniform(0.05, 0.12)
            bar.ellipse(x + rng.normal(0, 0.8), y + rng.normal(0, 0.8), r, r)
    bm = bar.read() * (1 - sea) * (1 - pool)
    bdome = np.clip(blurf(bm, 0.03, g) * 1.6, 0, 1) * bm
    bcol = lerp(C(0.15, 0.15, 0.13), C(0.3, 0.29, 0.26), bdome)
    alb, hgt = apply_object(g, alb, hgt, bm, bdome * 0.08, bcol, shadow=0.45, shadow_blur=0.05)

    # salt crust high and dry, near the arena
    salt = smoothstep(0.62, 0.75, n.fbm(X, Y, 1.5, 4, offset=12)) * smoothstep(0.5, 0.8, norm01(hgt)) * (1 - sea)
    alb = lerp(alb, C(0.35, 0.35, 0.33), salt * 0.35)

    cav = cavity(hgt, g, 0.6, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.45 * (1 - sea)[..., None]), 0.05)
    wet = smoothstep(0.4, 0.7, n.fbm(X, Y, 3, 3, offset=13)) * margin
    smooth = 0.3 + wet * 0.3 + pool * 0.6 + sea * 0.62 - salt * 0.2

    # a faint glimmer on the water (emission) so the sea reads even outside the lights
    emission = C(0.03, 0.07, 0.08) * (sea * (0.4 + 0.6 * swell))[..., None] + C(0.08, 0.1, 0.1) * (foam * sea * 0.5)[..., None]

    # lanes: flat ledges of weathered rock, salt crust, small pools
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, C(0.1, 0.11, 0.11), C(0.19, 0.2, 0.19), offset=990)
    wear = lane_wear(lane)
    lstrata = n.ridged(LX * 0.4, LY * 2.0, 4, 3, offset=991)
    lalb = lerp(lalb, C(0.24, 0.24, 0.22), smoothstep(0.75, 0.9, lstrata) * 0.35)
    lsalt = smoothstep(0.62, 0.78, n.fbm(LX, LY, 1.2, 4, offset=992))
    lalb = lerp(lalb, C(0.33, 0.33, 0.31), lsalt * 0.3)
    lp = smoothstep(0.74, 0.79, n.fbm(LX, LY, 2.0, 4, offset=993)) * (1 - wear * 0.8)
    lalb = lerp(lalb, C(0.03, 0.07, 0.07), lp * 0.8)
    lhgt = (lhgt + lstrata * 0.08 - lp * 0.06).astype(F32)
    lcav = cavity(lhgt, lane, 0.4, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.3), 0.05)
    lsmooth = 0.28 + lp * 0.6 - lsalt * 0.15

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)
