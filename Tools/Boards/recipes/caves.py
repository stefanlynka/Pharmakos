"""The Gorgon's cave: wet limestone floor with rimstone terraces and black puddles that catch the light, green slime
in the damp, the petrified who met her gaze lying as broken statues with their stone shields and swords. Lanes: damp packed grit
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

    # the petrified: whole statues and broken pieces, pale stone, some fallen apart
    st = Layer(g)
    for x, y in scatter_points(rng, 5, (-62, 62, -21, 41), margin_keep(8.0), min_gap=20):
        whole = rng.random() < 0.5
        parts = None if whole else set(rng.choice(['head', 'torso', 'larm', 'rarm', 'lleg', 'rleg'], 4, replace=False))
        figure(st, rng, x, y, rng.uniform(2.2, 2.8), rng.uniform(0, 6.28), parts)
    for x, y in scatter_points(rng, 24, (-66, 66, -23, 43), margin_keep(3.0), 4.0):
        k = rng.random()
        if k < 0.4:
            st.ellipse(x, y, rng.uniform(0.4, 0.6), rng.uniform(0.45, 0.65), 255, rng.uniform(0, 6.28))   # a head
        else:
            a = rng.uniform(0, 6.28)
            L = rng.uniform(1.2, 2.6)
            st.line([(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], rng.uniform(0.35, 0.6))
    sm_ = st.read()
    sdome = np.clip(blurf(sm_, 0.18, g) * 1.6, 0, 1) * sm_
    cracks = smoothstep(0.9, 0.97, n.ridged(X, Y, 0.9, 3, offset=7)) * sm_
    gy, gx = np.gradient(blurf(sdome, 0.05, g))
    lit = np.clip(0.5 + (-gx + gy) * g.ppu * 0.8, 0, 1)
    scol = lerp(C(0.2, 0.2, 0.19), C(0.5, 0.5, 0.47), np.clip(lit * 0.7 + 0.3 * n.fbm(X, Y, 0.4, 3, offset=8), 0, 1))
    scol = lerp(scol, C(0.12, 0.16, 0.1), smoothstep(0.62, 0.8, n.fbm(X, Y, 0.8, 3, offset=9)) * 0.5)   # moss creeping
    scol = lerp(scol, scol * 0.4, cracks)
    alb, hgt = apply_object(g, alb, hgt, sm_, sdome * 0.6 - cracks * 0.05, scol, shadow=0.6, shadow_blur=0.3)

    # the heroes' gear, turned to stone with them: shields and swords
    gear = Layer(g)
    for x, y in scatter_points(rng, 10, (-64, 64, -22, 42), margin_keep(5.0), 8.0):
        if rng.random() < 0.5:
            gear.ellipse(x, y, rng.uniform(1.1, 1.5), rng.uniform(1.1, 1.5), 255, n=36)
        else:
            a = rng.uniform(0, 6.28)
            L = rng.uniform(2.0, 2.8)
            taper(gear, [(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], 0.28, 0.06)
            ca, sa = math.cos(a), math.sin(a)
            gear.line([(x - sa * 0.5, y + ca * 0.5), (x + sa * 0.5, y - ca * 0.5)], 0.16)
    gm = gear.read()
    gdome = np.clip(blurf(gm, 0.25, g) * 1.5, 0, 1) * gm
    gcol = lerp(C(0.2, 0.2, 0.19), C(0.42, 0.42, 0.39), gdome * (0.6 + 0.5 * n.fbm(X, Y, 0.4, 3, offset=12)))
    alb, hgt = apply_object(g, alb, hgt, gm, gdome * 0.3, gcol, shadow=0.55, shadow_blur=0.25)

    # loose rubble
    rub = [(x, y, rng.uniform(0.12, 0.5)) for x, y in scatter_points(rng, 400, (-66, 66, -23, 43), margin_keep(0.8), 0.6)]
    rm, rh, rc = rocks(g, n, rng, rub, base=C(0.08, 0.085, 0.08), light=C(0.26, 0.27, 0.25))
    alb, hgt = apply_object(g, alb, hgt, rm, rh * 0.4, rc, shadow=0.5, shadow_blur=0.15)

    cav = cavity(hgt, g, 0.6, 2.5)
    alb = desaturate(alb * (1 - cav[..., None] * 0.5), 0.05)
    smooth = 0.3 + damp * 0.25 + pud * 0.65 + sm_ * 0.1

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
