"""The three Underworld bosses, each with its own board (module paints all three; run as `boards.py Bosses`).

Throne:  Hades' hall. Polished obsidian slabs, a gold-inlaid Greek key round the arena, spilt pomegranate seeds,
         bones of supplicants at the edges, cold green-gold glimmer in the gold. Lanes: black basalt.
Fates:   the Moirai's chamber. Dark worn stone strung with threads (white, red, gold) running in from every edge, cut
         ends lying loose, spindles and tufts of wool. Lanes: dark wood-grained floor of the loom room.
TheGate: the gate of Hades on the Styx. Black flagstones scored with Cerberus' claw marks, chains, obols scattered for
         the ferryman, and the river along the far edge, faintly glowing. Lanes: wet black flagstones."""
from boardkit import *  # noqa

NAME = 'Bosses'
BOARDS = ('Throne', 'Fates', 'TheGate')


def slabs(n, X, Y, S, cy=11.64):
    gx, gy = X / S, (Y - cy) / S
    cx, cyy = np.floor(gx).astype(np.int64), np.floor(gy).astype(np.int64)
    edge = np.minimum(np.minimum(gx - cx, cx + 1 - gx), np.minimum(gy - cyy, cyy + 1 - gy)) * S
    return (1 - smoothstep(0.03, 0.09, edge)).astype(F32), n.cellhash(cx, cyy, 2)


def dark_lane(n, lane, rng, dark, light, w=6.0, h=5.0, offset=2000):
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, dark, light, offset=offset)
    lj, lsid, _ = flagstones(n, LX, LY, w, h, gap=0.05, offset=offset + 1)
    lalb = lalb * (0.9 + 0.2 * lsid)[..., None]
    lalb = lerp(lalb, dark * 0.3, lj * 0.7)
    lhgt = (lhgt - lj * 0.05).astype(F32)
    lcav = cavity(lhgt, lane, 0.3, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.25), 0.05)
    return lalb, lhgt, 0.4 + lane_wear(lane) * 0.1 - lj * 0.3


def throne(preview):
    g, lane = new_canvases()
    n = Noise(131)
    rng = np.random.default_rng(131)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    joint, sid = slabs(n, X, Y, 7.0)
    alb, veins = marble(n, X, Y, C(0.022, 0.02, 0.025), C(0.06, 0.055, 0.065), C(0.16, 0.15, 0.17), 0.45, 0.03, offset=2100)
    alb = alb * (0.85 + 0.25 * sid)[..., None] + ((brush(n, X, Y) - 0.5) * 0.012)[..., None]
    alb = lerp(alb, C(0.01, 0.01, 0.012), joint * 0.8)
    hgt = ((sid - 0.5) * 0.03 - joint * 0.05).astype(F32)

    km, band = meander_band(X, Y, (-31.5, 31.5, -2.2, 25.5), 0.8, 2.4)
    gold = lerp(C(0.2, 0.14, 0.05), C(0.42, 0.31, 0.12), n.fbm(X, Y, 0.4, 3, offset=1))
    alb = lerp(alb, gold, km * 0.9)
    hgt = (hgt + km * 0.01).astype(F32)

    seeds = Layer(g)
    for x, y in scatter_points(rng, 14, (-64, 64, -22, 42), margin_keep(3.0, 18.0), 7.0):
        for _ in range(rng.integers(12, 40)):
            seeds.ellipse(x + rng.normal(0, 0.9), y + rng.normal(0, 0.9), 0.11, 0.15, 255, rng.uniform(0, 6.28))
    sm = seeds.read()
    sdome = np.clip(blurf(sm, 0.03, g) * 1.6, 0, 1) * sm
    alb, hgt = apply_object(g, alb, hgt, sm, sdome * 0.05, lerp(C(0.25, 0.02, 0.04), C(0.55, 0.06, 0.1), sdome), shadow=0.4, shadow_blur=0.05)
    bones = place_bones(g, n, rng, np.zeros_like(X), skeletons=3, piles=10, singles=30, fragments=90, keep_out=6.0)
    alb, hgt = apply_bones(g, alb, hgt, bones)

    cav = cavity(hgt, g, 0.4, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.3), 0.05)
    smooth = 0.7 - joint * 0.5 + km * 0.1 + sm * 0.2
    emission = C(0.18, 0.13, 0.04) * (km * smoothstep(0.55, 0.8, n.fbm(X, Y, 4, 3, offset=2)))[..., None]
    lalb, lhgt, lsmooth = dark_lane(n, lane, rng, C(0.05, 0.045, 0.05), C(0.1, 0.09, 0.1), offset=2150)
    finish(os.path.join(OUT, 'Throne'), 'Throne', g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)


def fates(preview):
    g, lane = new_canvases()
    n = Noise(137)
    rng = np.random.default_rng(137)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    joint, sid = slabs(n, X, Y, 5.0)
    big = n.fbm(X, Y, 12, 4, offset=1)
    alb = lerp(C(0.06, 0.05, 0.045), C(0.13, 0.11, 0.095), np.clip(big * 0.6 + sid * 0.4, 0, 1))
    alb = alb + ((brush(n, X, Y) - 0.5) * 0.03)[..., None]
    alb = lerp(alb, C(0.025, 0.02, 0.02), joint * 0.7)
    hgt = (big * 0.2 - joint * 0.05).astype(F32)

    # threads: long gentle curves running in from the edges and stopping short of the arena, some cut
    cols = [C(0.62, 0.6, 0.55), C(0.45, 0.06, 0.05), C(0.55, 0.42, 0.15)]
    th = [Layer(g) for _ in cols]
    for i in range(70):
        side = i % 4
        t = rng.uniform(0.02, 0.98)
        x, y = [(-68 + 136 * t, 44.5), (-68 + 136 * t, -24.5), (-68.5, -24 + 68 * t), (68.5, -24 + 68 * t)][side]
        a = math.atan2(11.6 - y, -x) + rng.normal(0, 0.35)
        pts = [(x, y)]
        bend = rng.normal(0, 0.012)
        for k in range(400):
            a += bend
            x += math.cos(a) * 0.3
            y += math.sin(a) * 0.3
            if float(arena_dist(np.array([[x]], F32), np.array([[y]], F32))[0, 0]) < rng.uniform(2.5, 9.0):
                break
            pts.append((x, y))
        if len(pts) > 2:
            th[rng.integers(0, 3)].line(pts, rng.uniform(0.05, 0.09))
    for layer, col in zip(th, cols):
        m = layer.read()
        alb, hgt = apply_object(g, alb, hgt, m, m * 0.03, col * (0.8 + 0.3 * n.fbm(X, Y, 0.5, 2, offset=3))[..., None],
                                shadow=0.5, shadow_blur=0.06, shadow_offset=(0.05, -0.08))

    # spindles and wool
    sp = Layer(g)
    wool = Layer(g)
    for x, y in scatter_points(rng, 9, (-64, 64, -22, 42), margin_keep(6.0), 12.0):
        a = rng.uniform(0, 6.28)
        L = rng.uniform(2.5, 3.5)
        taper(sp, [(x - math.cos(a) * L / 2, y - math.sin(a) * L / 2), (x, y), (x + math.cos(a) * L / 2, y + math.sin(a) * L / 2)], 0.12, 0.05)
        wool.ellipse(x + math.cos(a) * 0.3, y + math.sin(a) * 0.3, 0.55, 0.42, 255, a)
        for _ in range(rng.integers(2, 5)):
            blob(wool, rng, x + rng.normal(0, 1.2), y + rng.normal(0, 1.2), rng.uniform(0.3, 0.6), 12, 0.4)
    spm = sp.read()
    wm = wool.read()
    alb, hgt = apply_object(g, alb, hgt, spm, spm * 0.1, C(0.3, 0.2, 0.11), shadow=0.55, shadow_blur=0.1)
    wdome = np.clip(blurf(wm, 0.15, g) * 1.4, 0, 1) * wm
    alb, hgt = apply_object(g, alb, hgt, wm, wdome * 0.25, lerp(C(0.35, 0.33, 0.29), C(0.6, 0.57, 0.5), wdome * brush(n, X, Y, 0.2, 2.0)), shadow=0.5, shadow_blur=0.2)

    cav = cavity(hgt, g, 0.4, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.35), 0.1)
    smooth = 0.2 - joint * 0.1
    # lanes: dark wood boards
    LX, LY = lane.WX, lane.WY
    plank = np.floor(LY / 1.25)
    pid = n.cellhash(plank.astype(np.int64), plank.astype(np.int64) * 3, 2)
    grainw = n.value(LX * 0.08 + pid * 13, LY * 3.0, 0.3, 2201) * 0.6 + n.fbm(LX * 0.2, LY * 2.0, 0.6, 3, offset=2202) * 0.4
    lalb = lerp(C(0.07, 0.045, 0.03), C(0.15, 0.1, 0.065), np.clip(grainw * 0.7 + pid * 0.3, 0, 1))
    seam = 1 - smoothstep(0.0, 0.04, np.abs((LY / 1.25) % 1.0 - 0.5) * 2 - 0.94)
    lalb = lerp(lalb, C(0.02, 0.015, 0.01), seam * 0.0 + (1 - smoothstep(0.0, 0.03, 1.25 * np.minimum(LY / 1.25 % 1.0, 1 - LY / 1.25 % 1.0))) * 0.8)
    lhgt = (grainw * 0.04 - (1 - smoothstep(0.0, 0.03, 1.25 * np.minimum(LY / 1.25 % 1.0, 1 - LY / 1.25 % 1.0))) * 0.05).astype(F32)
    lsmooth = 0.3 + lane_wear(lane) * 0.1
    finish(os.path.join(OUT, 'Fates'), 'Fates', g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)


def the_gate(preview):
    g, lane = new_canvases()
    n = Noise(139)
    rng = np.random.default_rng(139)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    j, sid, _ = flagstones(n, X, Y, 4.0, 2.6, gap=0.1, offset=1)
    big = n.fbm(X, Y, 14, 4, offset=2)
    alb = lerp(C(0.04, 0.045, 0.045), C(0.1, 0.11, 0.105), np.clip(big * 0.5 + sid * 0.5, 0, 1))
    alb = alb + ((brush(n, X, Y) - 0.5) * 0.02)[..., None]
    alb = lerp(alb, C(0.015, 0.02, 0.02), j * 0.75)
    hgt = (big * 0.2 + (sid - 0.5) * 0.04 - j * 0.07).astype(F32)

    # the Styx along the far edge
    shore = 33.5 + (n.fbm(X, Y, 9, 3, offset=3) - 0.5) * 5.0
    river = smoothstep(shore - 0.2, shore + 0.2, Y)
    swirl = n.fbm(X * 0.4, Y * 1.2, 2.5, 4, offset=4)
    water = lerp(C(0.01, 0.025, 0.025), C(0.03, 0.07, 0.06), swirl)
    alb = lerp(alb, water, river)
    hgt = (hgt * (1 - river) + river * (float(hgt.min()) - 0.2)).astype(F32)
    emission = C(0.02, 0.09, 0.07) * (river * smoothstep(0.55, 0.8, swirl))[..., None]

    # Cerberus: sets of three/four parallel gouges
    cl = Layer(g)
    for x, y in scatter_points(rng, 16, (-64, 64, -22, 31), margin_keep(2.0), 6.0):
        a = rng.uniform(0, 6.28)
        L = rng.uniform(4.5, 7.5)
        for k in range(rng.integers(3, 5)):
            off = (k - 1.5) * 0.7
            sx, sy = x - math.sin(a) * off, y + math.cos(a) * off
            taper(cl, [(sx, sy), (sx + math.cos(a + 0.05) * L * 0.5, sy + math.sin(a + 0.05) * L * 0.5),
                       (sx + math.cos(a + 0.12) * L, sy + math.sin(a + 0.12) * L)], 0.1, 0.38)
    cm = cl.read() * (1 - river)
    alb = lerp(alb, C(0.17, 0.17, 0.16), np.clip(blurf(cm, 0.08, g) * 1.6 - cm, 0, 1) * 0.6)
    alb = lerp(alb, C(0.0, 0.0, 0.0), cm * 0.8)
    hgt = (hgt - cm * 0.12).astype(F32)

    # chains and obols
    ch = Layer(g)
    for x, y in scatter_points(rng, 7, (-64, 64, -22, 31), margin_keep(5.0), 12.0):
        a = rng.uniform(0, 6.28)
        for k in range(rng.integers(8, 18)):
            a += rng.normal(0, 0.18)
            x += math.cos(a) * 0.55
            y += math.sin(a) * 0.55
            ch.ellipse(x, y, 0.32, 0.18, 255, a + (k % 2) * 1.5708)
    chm = ch.read()
    chd = np.clip(blurf(chm, 0.04, g) * 1.5, 0, 1) * chm
    alb, hgt = apply_object(g, alb, hgt, chm, chd * 0.15, lerp(C(0.08, 0.07, 0.06), C(0.25, 0.22, 0.18), chd), shadow=0.5, shadow_blur=0.08)
    ob = Layer(g)
    for x, y in scatter_points(rng, 60, (-66, 66, -23, 32), margin_keep(1.5), 1.5):
        ob.ellipse(x, y, 0.32, 0.32, 255, n=16)
    obm = ob.read()
    alb, hgt = apply_object(g, alb, hgt, obm, obm * 0.04, lerp(C(0.25, 0.17, 0.07), C(0.45, 0.33, 0.14), n.fbm(X, Y, 0.2, 2, offset=5)), shadow=0.5, shadow_blur=0.05)

    cav = cavity(hgt, g, 0.4, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.4), 0.05)
    smooth = 0.35 + river * 0.55 - j * 0.2 + obm * 0.3
    lalb, lhgt, lsmooth = dark_lane(n, lane, rng, C(0.065, 0.07, 0.07), C(0.12, 0.13, 0.125), 5.0, 3.3, offset=2300)
    finish(os.path.join(OUT, 'TheGate'), 'TheGate', g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)


def paint(preview=False):
    throne(preview)
    fates(preview)
    the_gate(preview)
