"""Dionysus' revel in a night glade: dark loam and moss strewn with fallen vine leaves (green, yellowing, wine-red),
curling tendrils, grape bunches (some trodden into purple smears), pools of spilt wine that still shine, and broken
black-figure cups and amphorae. Lanes: trampled earth splashed with wine, bare footprints and satyrs' hoofprints."""
from boardkit import *  # noqa

NAME = 'Bacchanalia'


def tendril(layer, rng, x, y, length, width):
    a = rng.uniform(0, 6.28)
    pts = [(x, y)]
    curl = rng.uniform(0.15, 0.35) * rng.choice([-1, 1])
    step = 0.25
    for i in range(int(length / step)):
        t = i / max(1, length / step)
        a += curl * (1 + 3 * t * t) + rng.normal(0, 0.08)
        x += math.cos(a) * step * (1 - 0.6 * t)
        y += math.sin(a) * step * (1 - 0.6 * t)
        pts.append((x, y))
    layer.line(pts, width)


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(41)
    rng = np.random.default_rng(41)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 10, ad)

    big = n.fbm(X, Y, 14, 4, offset=1)
    grain = n.fbm(X, Y, 0.5, 5, offset=2)
    soil = lerp(C(0.07, 0.05, 0.04), C(0.17, 0.12, 0.085), np.clip(big * 0.7 + grain * 0.3, 0, 1))
    moss = smoothstep(0.55, 0.72, n.fbm(X, Y, 4, 4, offset=3)) * margin
    mosscol = lerp(C(0.05, 0.07, 0.03), C(0.12, 0.15, 0.06), tufts(n, X, Y, 0.35, 28, offset=4))
    alb = lerp(soil, mosscol, moss)
    alb = alb + ((brush(n, X, Y) - 0.5) * 0.03)[..., None]
    trodden = smoothstep(2.0, -4.0, ad)
    alb = lerp(alb, C(0.19, 0.14, 0.1), trodden * 0.5)
    hgt = (big * 0.35 + grain * 0.06 + moss * 0.08).astype(F32)

    # spilt wine: pools near the arena, splashes everywhere, all a little glossy
    wine = Layer(g)
    for x, y in scatter_points(rng, 26, (-64, 64, -22, 42), margin_keep(1.5, 16.0), min_gap=6):
        blob(wine, rng, x, y, rng.uniform(0.8, 2.2), 18, 0.35, rng.uniform(0, 6.28), rng.uniform(0.5, 0.9))
        for _ in range(rng.integers(6, 16)):
            a = rng.uniform(0, 6.28)
            d = rng.uniform(1.2, 3.5)
            wine.ellipse(x + math.cos(a) * d, y + math.sin(a) * d, rng.uniform(0.05, 0.22), rng.uniform(0.05, 0.18))
    wm = np.clip(blurf(wine.read(), 0.05, g), 0, 1)
    winecol = lerp(C(0.09, 0.015, 0.03), C(0.2, 0.03, 0.06), n.fbm(X, Y, 0.8, 3, offset=5))
    alb = lerp(alb, winecol, wm * 0.9)

    # vines creeping in from the edges: woody runners with big leaves, tendrils and grape bunches along them,
    # plus loose fallen leaves near them
    wood = Layer(g)
    leaves = Layer(g)
    tint = Layer(g)
    veinl = Layer(g)
    tl = Layer(g)
    grapes = Layer(g)
    starts = []
    for i in range(16):
        side = i % 4
        t = rng.uniform(0.05, 0.95)
        if side == 0:
            starts.append((-68 + 136 * t, 44.5))
        elif side == 1:
            starts.append((-68 + 136 * t, -24.5))
        elif side == 2:
            starts.append((-68.5, -24 + 68 * t))
        else:
            starts.append((68.5, -24 + 68 * t))

    def add_leaf(x, y, size, a):
        v = rng.random()
        tv = 40 if v < 0.62 else (150 if v < 0.88 else 235)
        palmate_leaf(leaves, x, y, size, a, 255)
        palmate_leaf(tint, x, y, size, a, tv + rng.uniform(-30, 15))
        for spread in (-0.9, 0, 0.9):
            veinl.line([(x, y), (x + math.cos(a + spread) * size * 0.75, y + math.sin(a + spread) * size * 0.75)], size * 0.03)

    for sx, sy in starts:
        x, y = sx, sy
        a = math.atan2(11.6 - y, -x) + rng.normal(0, 0.5)
        pts = [(x, y)]
        length = rng.uniform(18, 40)
        for k in range(int(length / 0.4)):
            a += rng.normal(0, 0.12)
            nx_, ny_ = x + math.cos(a) * 0.4, y + math.sin(a) * 0.4
            d = float(arena_dist(np.array([[nx_]], F32), np.array([[ny_]], F32))[0, 0])
            if d < 3.0:
                a += 0.5 * rng.choice([-1, 1])
                continue
            x, y = nx_, ny_
            pts.append((x, y))
            if k % 4 == 0:
                side = 1 if (k // 4) % 2 else -1
                add_leaf(x, y, rng.uniform(1.4, 2.6), a + side * rng.uniform(0.9, 1.6))
            if k % 9 == 4:
                tendril(tl, rng, x, y, rng.uniform(1.5, 3.5), 0.07)
            if k % 17 == 8 and rng.random() < 0.7:
                ga = a + rng.choice([-1, 1]) * 1.3
                for q in range(rng.integers(14, 28)):
                    t = rng.random()
                    gx = x + math.cos(ga) * t * 1.8 + rng.normal(0, 0.3 * (1 - t * 0.7))
                    gy = y + math.sin(ga) * t * 1.8 + rng.normal(0, 0.3 * (1 - t * 0.7))
                    r = rng.uniform(0.15, 0.22)
                    grapes.ellipse(gx, gy, r, r)
        wood.line(pts, rng.uniform(0.22, 0.38))
    for x, y in scatter_points(rng, 110, (-66, 66, -23, 43), margin_keep(3.0), 1.6):
        add_leaf(x, y, rng.uniform(1.0, 2.0), rng.uniform(0, 6.28))

    wdm = wood.read()
    wcol = lerp(C(0.1, 0.07, 0.05), C(0.22, 0.16, 0.11), np.clip(blurf(wdm, 0.06, g) * 1.4, 0, 1) * (0.6 + 0.5 * grain))
    alb, hgt = apply_object(g, alb, hgt, wdm, wdm * 0.25, wcol, shadow=0.55, shadow_blur=0.2)

    lm = leaves.read()
    tv = np.clip(tint.read() / np.maximum(lm, 1e-3), 0, 1)
    vm = veinl.read() * lm
    green = lerp(C(0.045, 0.055, 0.03), C(0.09, 0.105, 0.05), grain)        # muted, shadowed: they frame, not decorate
    yellow = lerp(C(0.1, 0.085, 0.045), C(0.15, 0.12, 0.065), grain)
    red = lerp(C(0.09, 0.035, 0.03), C(0.13, 0.05, 0.04), grain)
    leafcol = lerp(green, yellow, smoothstep(0.35, 0.6, tv))
    leafcol = lerp(leafcol, red, smoothstep(0.75, 0.88, tv))
    leafcol = desaturate(leafcol * (0.85 + 0.25 * n.fbm(X, Y, 0.6, 3, offset=8))[..., None], 0.3)
    leafcol = lerp(leafcol, leafcol * 0.55, vm)
    alb, hgt = apply_object(g, alb, hgt, lm, lm * 0.06 - vm * 0.02, leafcol, shadow=0.5, shadow_blur=0.18)

    tm = tl.read()
    alb, hgt = apply_object(g, alb, hgt, tm, tm * 0.06, C(0.17, 0.15, 0.07), shadow=0.35, shadow_blur=0.08)

    gm = grapes.read()
    gdome = np.clip(blurf(gm, 0.05, g) * 1.5, 0, 1) * gm
    gcol = lerp(C(0.035, 0.015, 0.03), C(0.11, 0.05, 0.1), gdome ** 1.5)
    alb, hgt = apply_object(g, alb, hgt, gm, gdome * 0.18, gcol, shadow=0.55, shadow_blur=0.1)
    smear = Layer(g)
    for x, y in scatter_points(rng, 20, (-66, 66, -23, 43), margin_keep(1.5, 12), 4.0):
        blob(smear, rng, x, y, rng.uniform(0.5, 1.0), 12, 0.5)
    sm = blurf(smear.read(), 0.1, g)
    alb = lerp(alb, C(0.08, 0.025, 0.05), sm * 0.6)

    # broken cups and amphorae: terracotta outside, black glaze in
    sh = Layer(g)
    glaze = Layer(g)
    for x, y in scatter_points(rng, 14, (-66, 66, -23, 43), margin_keep(2.0, 14), 8.0):
        for _ in range(rng.integers(2, 6)):
            px, py = x + rng.normal(0, 0.8), y + rng.normal(0, 0.8)
            size = rng.uniform(0.6, 1.4)
            shard(sh, rng, px, py, size)
            if rng.random() < 0.5:
                shard(glaze, rng, px, py, size * 0.7)
    shm = sh.read()
    glm = glaze.read() * shm
    shcol = lerp(C(0.15, 0.075, 0.045), C(0.22, 0.11, 0.07), n.fbm(X, Y, 0.3, 3, offset=7))
    shcol = lerp(shcol, C(0.05, 0.04, 0.04), glm)
    alb, hgt = apply_object(g, alb, hgt, shm, shm * 0.08, shcol, shadow=0.55, shadow_blur=0.12)

    cav = cavity(hgt, g, 0.5, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.45), 0.1)
    smooth = 0.12 + wm * 0.55 + gm * 0.35 + glm * 0.3 + sm * 0.2

    # lanes: trampled earth, wine splashes, bare feet and cloven hooves
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, C(0.15, 0.11, 0.08), C(0.24, 0.18, 0.13), offset=940)
    wear = lane_wear(lane)
    sp = Layer(lane)
    for _ in range(160):
        x, y = rng.uniform(-29.5, 29.5), rng.uniform(0.3, 19.7)
        sp.ellipse(x, y, rng.uniform(0.05, 0.3), rng.uniform(0.05, 0.25))
    spm = blurf(sp.read(), 0.04, lane)
    lalb = lerp(lalb, C(0.14, 0.04, 0.05), spm * 0.45)
    pr = Layer(lane)
    for _ in range(140):
        x, y = rng.uniform(-29.5, 29.5), rng.choice([rng.uniform(1.5, 8.5), rng.uniform(11.5, 18.5)])
        a = rng.uniform(0, 6.28)
        if rng.random() < 0.5:
            footprint(pr, x, y, rng.uniform(0.9, 1.1), a)
        else:
            hoofprint(pr, x, y, rng.uniform(0.35, 0.45), a)
    pm = blurf(pr.read(), 0.05, lane) * 0.6
    lalb = lerp(lalb, C(0.11, 0.08, 0.06), pm * 0.5)
    lhgt = (lhgt - pm * 0.08).astype(F32)
    lalb = lalb * (0.95 + 0.1 * wear)[..., None]
    lcav = cavity(lhgt, lane, 0.4, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.3), 0.12)
    lsmooth = 0.12 + spm * 0.4

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
