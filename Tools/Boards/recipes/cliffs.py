"""The Sirens' rocks: a dark wet shelf of sea-worn rock above the surf, tide pools that hold the light, barnacles,
limpets and kelp, sea foam caught in the hollows, and a jagged cliff edge dropping to the sea, rocks and stacks in the surf below. Lanes: flat ledges
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

    # the cliff edge: an angular, broken line (straight runs between sharp breaks, a few deep coves), then the cliff
    # face falling away (seen from above: a band of dark, vertically fractured rock), rocks and stacks in the surf,
    # and the sea below
    knots_x = np.arange(-72, 73, 1.6)
    walk = np.cumsum(rng.normal(0, 0.9, len(knots_x)))
    walk -= np.linspace(walk[0], walk[-1], len(walk))
    coves = np.zeros_like(knots_x)
    for cx_ in rng.uniform(-60, 60, 5):
        coves += -rng.uniform(3, 7) * np.clip(1 - np.abs(knots_x - cx_) / rng.uniform(4, 9), 0, 1)
    prof = 33.0 + walk * 0.9 + coves + np.abs(knots_x) * 0.05
    edge = np.interp(X, knots_x, prof).astype(F32)                      # angular: linear between knots
    wx, wy = n.warp(X, Y, 1.5, 0.5, offset=50)
    edge = edge + (n.fbm(wx, wy, 0.8, 3, offset=51) - 0.5) * 0.9       # crumbled detail on the lip
    drop = 3.2 + 1.5 * n.fbm(X, Y, 6, 2, offset=52)                     # how far the face shows from above
    d = Y - edge                                                         # >0 beyond the lip
    face = smoothstep(0.0, 0.15, d) * (1 - smoothstep(drop - 0.3, drop, d))
    sea = smoothstep(drop - 0.3, drop + 0.2, d)

    fract = n.value(X * 3.0, Y * 0.35, 1.0, 53) * 0.6 + n.fbm(X, Y, 0.5, 3, offset=54) * 0.4
    facecol = lerp(C(0.05, 0.055, 0.06), C(0.13, 0.135, 0.135), fract)
    facecol = facecol * (1 - np.clip(d / np.maximum(drop, 0.1), 0, 1) * 0.75)[..., None]   # darker toward the water
    lip = (1 - smoothstep(0.0, 0.35, np.abs(d))) * (1 - sea)
    rock_h = hgt.copy()
    alb = lerp(alb, C(0.26, 0.26, 0.24), lip * 0.5)                     # sunlit broken lip
    alb = lerp(alb, facecol, face)

    depth = smoothstep(drop, drop + 8, d)
    water = lerp(C(0.03, 0.085, 0.095), C(0.01, 0.03, 0.045), depth)
    swell = n.fbm(X * 0.5, Y * 1.5, 3, 4, offset=6)
    water = water * (0.8 + 0.4 * swell)[..., None]

    # rocks and sea stacks in the surf, ringed with foam
    stk = Layer(g)
    for _ in range(40):
        x = rng.uniform(-68, 68)
        e = float(np.interp(x, knots_x, prof))
        y = e + rng.uniform(3.5, 10.0)
        blob(stk, rng, x, y, rng.uniform(0.3, 1.6) if rng.random() < 0.85 else rng.uniform(1.8, 2.8), 9, 0.35)
    stm = stk.read() * sea
    ring = np.clip(blurf(stm, 0.5, g) * 2.2 - stm, 0, 1)
    foam_line = (1 - smoothstep(0.0, 1.0, np.abs(d - drop - 0.3))) * smoothstep(0.3, 0.55, n.fbm(X, Y, 0.9, 4, offset=7))
    foam_far = smoothstep(0.72, 0.85, n.fbm(X * 0.4, Y * 2.0, 2, 4, offset=8)) * depth * 0.5
    foam = np.clip(foam_line + foam_far + ring * smoothstep(0.3, 0.6, n.fbm(X, Y, 0.5, 3, offset=55)), 0, 1)
    alb = lerp(alb, water, sea)
    alb = lerp(alb, C(0.5, 0.55, 0.55), foam * sea * 0.7)
    gy_, gx_ = np.gradient(blurf(stm, 0.15, g))
    stlit = np.clip(0.45 + (-gx_ + gy_) * g.ppu * 0.8, 0, 1)
    alb = lerp(alb, lerp(C(0.04, 0.045, 0.05), C(0.18, 0.18, 0.17), stlit), stm)
    hgt = (hgt * (1 - sea) - face * np.clip(d, 0, 4) * 0.4 + sea * (float(hgt.min()) - 1.5 + swell * 0.08)
           + stm * 1.2).astype(F32)
    land = (1 - sea) * (1 - face)

    # tide pools in the hollows: deep green-blue, shining
    low = 1 - norm01(rock_h)
    pool = smoothstep(0.7, 0.76, low + (n.fbm(X, Y, 2, 3, offset=9) - 0.5) * 0.12) * smoothstep(3, 7, ad) * land
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
    km = kelp.read() * land
    kcol = lerp(C(0.09, 0.08, 0.03), C(0.2, 0.18, 0.07), n.fbm(X, Y, 0.5, 3, offset=11))
    alb, hgt = apply_object(g, alb, hgt, km, km * 0.05, kcol, shadow=0.4, shadow_blur=0.12)

    # barnacles and limpets in colonies
    bar = Layer(g)
    for x, y in scatter_points(rng, 60, (-66, 66, -23, 43), margin_keep(2.0), 2.5):
        for _ in range(rng.integers(10, 30)):
            r = rng.uniform(0.05, 0.12)
            bar.ellipse(x + rng.normal(0, 0.8), y + rng.normal(0, 0.8), r, r)
    bm = bar.read() * land * (1 - pool)
    bdome = np.clip(blurf(bm, 0.03, g) * 1.6, 0, 1) * bm
    bcol = lerp(C(0.15, 0.15, 0.13), C(0.3, 0.29, 0.26), bdome)
    alb, hgt = apply_object(g, alb, hgt, bm, bdome * 0.08, bcol, shadow=0.45, shadow_blur=0.05)

    # salt crust high and dry, near the arena
    salt = smoothstep(0.62, 0.75, n.fbm(X, Y, 1.5, 4, offset=12)) * smoothstep(0.5, 0.8, norm01(hgt)) * land
    alb = lerp(alb, C(0.35, 0.35, 0.33), salt * 0.35)

    cav = cavity(hgt, g, 0.6, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.45 * (1 - sea)[..., None]), 0.05)
    wet = smoothstep(0.4, 0.7, n.fbm(X, Y, 3, 3, offset=13)) * margin
    smooth = (0.3 + wet * 0.3) * (1 - face) + pool * 0.6 + sea * 0.62 - salt * 0.2

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
