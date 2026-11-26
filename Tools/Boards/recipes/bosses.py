"""The three Underworld bosses, each with its own board (module paints all three; run as `boards.py Bosses`).

Throne:  Hades' hall. Polished obsidian slabs, a gold-inlaid Greek key round the arena, 
         old stains where supplicants knelt, cold green-gold glimmer in the gold. Lanes: black basalt.
Fates:   the Moirai's chamber. Dark worn stone, a few cut red threads, spindles; the warp-weighted loom
         stands along the far edge as a 3D back edge (fates_edge). Lanes: dark wood-grained floor of the loom room.
TheGate: the gate of Hades on the Styx. Ancient black paving, heaved, cracked and broken, scored with Cerberus' claw marks, chains, obols scattered for
         the ferryman, and the river along the far edge, faintly glowing. Lanes: wet black flagstones."""
from boardkit import *  # noqa

NAME = 'Bosses'
BOARDS = ('Throne', 'Fates', 'TheGate')


def slabs(n, X, Y, S, cy=10.64):
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

    km, band = meander_band(X, Y, (-31.5, 31.5, -2.2, 23.5), 0.8, 2.4)
    gold = lerp(C(0.2, 0.14, 0.05), C(0.42, 0.31, 0.12), n.fbm(X, Y, 0.4, 3, offset=1))
    alb = lerp(alb, gold, km * 0.9)
    hgt = (hgt + km * 0.01).astype(F32)

    seeds = Layer(g)
    for x, y in scatter_points(rng, 14, (-64, 64, -22, 42), margin_keep(3.0, 18.0), 7.0):
        for _ in range(rng.integers(12, 40)):
            seeds.ellipse(x + rng.normal(0, 0.9), y + rng.normal(0, 0.9), 0.11, 0.15, 255, rng.uniform(0, 6.28))
    # (the pomegranate seeds are still laid out but not applied: the red dots read as noise; rng sequence kept)
    # no bones in the king's hall: only old stains worn into the stone where supplicants knelt
    st = Layer(g)
    for x, y in scatter_points(rng, 10, (-64, 64, -22, 42), margin_keep(3.0, 16.0), 9.0):
        blob(st, rng, x, y, rng.uniform(1.0, 2.2), 16, 0.4)
    stm = blurf(st.read(), 0.6, g) * smoothstep(0.35, 0.65, n.fbm(X, Y, 0.8, 3, offset=5))
    alb = lerp(alb, C(0.045, 0.012, 0.015), stm * 0.7)

    cav = cavity(hgt, g, 0.4, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.3), 0.05)
    smooth = 0.7 - joint * 0.5 + km * 0.1 - stm * 0.2
    emission = C(0.18, 0.13, 0.04) * (km * smoothstep(0.55, 0.8, n.fbm(X, Y, 4, 3, offset=2)))[..., None]
    lalb, lhgt, lsmooth = dark_lane(n, lane, rng, C(0.05, 0.045, 0.05), C(0.1, 0.09, 0.1), offset=2150)
    finish(os.path.join(OUT, 'Throne'), 'Throne', g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)
    throne_edge()


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

    # a few threads cut and fallen near the arena
    loose = Layer(g)
    for x, y in scatter_points(rng, 10, (-60, 60, -20, 30), margin_keep(1.5, 8.0), 8.0):
        a = rng.uniform(0, 6.28)
        pts = [(x, y)]
        for _ in range(30):
            a += rng.normal(0, 0.12)
            x += math.cos(a) * 0.2
            y += math.sin(a) * 0.2
            pts.append((x, y))
        loose.line(pts, 0.05)
    lom = loose.read()
    alb, hgt = apply_object(g, alb, hgt, lom, lom * 0.02, C(0.4, 0.07, 0.05), shadow=0.4, shadow_blur=0.06)

    # spindles and wool
    sp = Layer(g)
    wool = Layer(g)
    for x, y in scatter_points(rng, 5, (-64, 64, -22, 26), margin_keep(6.0), 16.0):
        a = rng.uniform(0, 6.28)
        L = rng.uniform(2.5, 3.5)
        taper(sp, [(x - math.cos(a) * L / 2, y - math.sin(a) * L / 2), (x, y), (x + math.cos(a) * L / 2, y + math.sin(a) * L / 2)], 0.12, 0.05)
        wool.ellipse(x + math.cos(a) * 0.3, y + math.sin(a) * 0.3, 0.55, 0.42, 255, a)
        for _ in range(rng.integers(2, 5)):
            blob(wool, rng, x + rng.normal(0, 1.2), y + rng.normal(0, 1.2), rng.uniform(0.3, 0.6), 12, 0.4)
    spm = sp.read()
    wm = wool.read()
    alb, hgt = apply_object(g, alb, hgt, spm, spm * 0.1, C(0.3, 0.2, 0.11), shadow=0.55, shadow_blur=0.1)
    # (the wool tufts are still laid out but not applied: the white splotches read as unclear; rng sequence kept)

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
    fates_edge()


def the_gate(preview):
    g, lane = new_canvases()
    n = Noise(139)
    rng = np.random.default_rng(139)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    # ancient paving, heaved and broken: irregular stones (warped joints), each tilted its own way, cracked across,
    # chipped at the edges, some sunk or missing (ash and rubble in the holes), ash and moss packed in the joints
    wx, wy = n.warp(X, Y, 3.0, 0.45, offset=60)
    j, sid, edge = flagstones(n, wx, wy, 4.0, 2.6, gap=0.16, jitter=0.5, offset=1)
    chip = smoothstep(0.55, 0.75, n.fbm(X, Y, 0.35, 3, offset=61)) * (1 - smoothstep(0.1, 0.45, edge))
    j = np.clip(j + chip, 0, 1)
    tilt_a = n.cellhash((wx / 4.0).astype(np.int64) * 3 + 1, (wy / 2.6).astype(np.int64), 3) * 6.28
    tilt = (np.cos(tilt_a) * (wx % 4.0) / 4.0 + np.sin(tilt_a) * (wy % 2.6) / 2.6) * 0.18
    missing = (sid > 0.93).astype(F32) * smoothstep(1.0, 4.0, ad)
    sunk = ((sid > 0.82) & (sid <= 0.93)).astype(F32)
    crack = smoothstep(0.86, 0.95, n.ridged(X, Y, 2.2, 3, offset=62)) * (1 - j)
    big = n.fbm(X, Y, 14, 4, offset=2)
    grain = n.fbm(X, Y, 0.4, 4, offset=63)
    alb = lerp(C(0.035, 0.04, 0.04), C(0.1, 0.105, 0.1), np.clip(big * 0.35 + sid * 0.35 + grain * 0.3, 0, 1))
    alb = alb + ((brush(n, X, Y) - 0.5) * 0.025)[..., None]
    alb = alb * (1 - 0.3 * smoothstep(0.5, 0.8, n.fbm(X, Y, 3, 3, offset=64)))[..., None]   # grime and soot
    joint_fill = lerp(C(0.1, 0.1, 0.095), C(0.06, 0.08, 0.05), smoothstep(0.55, 0.75, n.fbm(X, Y, 1.5, 3, offset=65)))
    alb = lerp(alb, joint_fill, j * 0.8)
    alb = lerp(alb, C(0.01, 0.012, 0.012), crack * 0.85)
    hole = lerp(C(0.06, 0.055, 0.05), C(0.13, 0.12, 0.11), n.fbm(X, Y, 0.3, 4, offset=66))
    alb = lerp(alb, hole, missing)
    alb = alb * (1 - sunk * 0.25)[..., None]
    hgt = (big * 0.2 + tilt * (1 - j) + (sid - 0.5) * 0.04 - j * 0.12 - crack * 0.06 - missing * 0.3 - sunk * 0.12).astype(F32)
    rub = [(x, y, rng.uniform(0.15, 0.5)) for x, y in scatter_points(rng, 260, (-66, 66, -23, 33), margin_keep(1.0), 0.7)]
    rbm, rbh, rbc = rocks(g, n, rng, rub, base=C(0.04, 0.045, 0.045), light=C(0.15, 0.155, 0.15))
    alb, hgt = apply_object(g, alb, hgt, rbm, rbh * 0.4, rbc, shadow=0.5, shadow_blur=0.12)

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
    for x, y in scatter_points(rng, 18, (-66, 66, -23, 32), margin_keep(4.0), 3.0):
        ob.ellipse(x, y, 0.32, 0.32, 255, n=16)
    obm = ob.read()
    alb, hgt = apply_object(g, alb, hgt, obm, obm * 0.04, lerp(C(0.1, 0.075, 0.04), C(0.17, 0.13, 0.07), n.fbm(X, Y, 0.2, 2, offset=5)), shadow=0.5, shadow_blur=0.05)

    cav = cavity(hgt, g, 0.4, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.4), 0.05)
    smooth = 0.3 + river * 0.1 - j * 0.15
    lalb, lhgt, lsmooth = dark_lane(n, lane, rng, C(0.065, 0.07, 0.07), C(0.12, 0.13, 0.125), 5.0, 3.3, offset=2300)
    finish(os.path.join(OUT, 'TheGate'), 'TheGate', g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)
    gate_edge()


def throne_edge():
    """Hades' hall: an obsidian dais of three steps edged in gold, great obsidian pillars banded in gold rising out of
    frame, braziers burning between them, black wall behind."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    import bekit as bk
    folder = os.path.join(OUT, 'Throne')
    n = Noise(133)
    be = BackEdge('Throne')
    obs = be.material(Material('Obsidian', tex_h=16.0, self_light=0.3))
    gold = be.material(Material('Gold', tex_h=16.0, self_light=0.45))
    bronze = be.material(Material('Bronze', tex_h=4.0, self_light=0.35))
    fl = be.material(bk.flame_material())
    top_y, top_h = bk.steps(be, obs, -75, 75, 34.0, 3, 0.45, 0.65)
    for i in range(3):
        be.box(gold, -75, 75, 34.0 + i * 0.65 - 0.02, 34.0 + i * 0.65 + 0.1, (i + 1) * 0.45 - 0.06, (i + 1) * 0.45 + 0.02,
               sides=False)
    be.box(obs, -75, 75, top_y - 0.01, 42.5, 0, top_h, sides=False)
    be.box(obs, -75, 75, 41.0, 43.0, top_h, 18.0, sides=False)
    for x in np.arange(-72, 75, 9.0):
        be.lathe(obs, x, top_y + 1.8, [(top_h, 1.7), (top_h + 0.5, 1.45), (top_h + 16, 1.3)], segments=20, flutes=12,
                 flute_depth=0.05)
        for hb in (top_h + 0.5, top_h + 3.5, top_h + 6.5):
            be.lathe(gold, x, top_y + 1.8, [(hb, 1.5), (hb + 0.18, 1.52), (hb + 0.36, 1.5)], segments=20, cap=False)
        bx = x + 4.5
        if bx < 74 and not (-10 < bx < 10):
            bk.brazier(be, bronze, fl, bx, top_y + 0.9, top_h, 0.9)
    be.save(folder)
    c = obs.elev_canvas()
    alb, hgt, sm = bp.marble(c, n, C(0.02, 0.018, 0.024), C(0.055, 0.05, 0.065), C(0.14, 0.13, 0.16), 0.35, 0.03, offset=61)
    ta, th, ts = bp.tops(obs, n, C(0.02, 0.018, 0.022), C(0.05, 0.045, 0.055), offset=62)
    save_material(folder, obs, alb, hgt, np.full(c.X.shape, 0.75, F32), talb=ta, thgt=th, tsmooth=ts)
    c = gold.elev_canvas()
    alb, hgt, sm = bp.metal(c, n, C(0.35, 0.24, 0.08), C(0.62, 0.46, 0.18), offset=63)
    save_material(folder, gold, alb, hgt, sm)
    c = bronze.elev_canvas()
    alb, hgt, sm = bp.metal(c, n, C(0.16, 0.1, 0.05), C(0.32, 0.2, 0.1), C(0.12, 0.2, 0.15), offset=64)
    save_material(folder, bronze, alb, hgt, sm)
    bk.paint_flame(folder, fl, n)


def fates_edge():
    """The Moirai's loom, a Greek warp-weighted loom standing along the far edge: posts and a beam out of frame, the
    woven cloth with a red meander, the warp hanging below it in bundles tied to clay weights, a few red threads among
    the white and a few cut short."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    from boardkit import meander_band
    folder = os.path.join(OUT, 'Fates')
    n = Noise(141)
    rng = np.random.default_rng(141)
    be = BackEdge('Fates')
    wood = be.material(Material('Wood', tex_h=16.0, self_light=0.35))
    cloth = be.material(Material('Cloth', tex_h=16.0, self_light=0.45))
    thread = be.material(Material('Thread', tex_h=6.0, self_light=0.15))
    red = be.material(Material('RedThread', tex_h=6.0, self_light=0.2))
    clay = be.material(Material('Clay', tex_h=3.0, self_light=0.4))
    ly = 35.6
    cloth_h = 4.2
    for px in np.arange(-72, 75, 24.0):
        be.box(wood, px - 0.45, px + 0.45, ly + 0.3, ly + 1.2, 0, 16.0)
    be.box(cloth, -75, 75, ly, ly + 0.25, cloth_h, 16.0, sides=False)
    be.box(wood, -75, 75, ly - 0.1, ly + 0.5, cloth_h - 0.35, cloth_h, sides=False)       # the shed bar
    bundle_w = 1.6
    for b in np.arange(-74.2, 75, bundle_w):
        wx = b + rng.normal(0, 0.05)
        wh = 0.9 + 0.25 * math.sin(b * 1.3) + rng.normal(0, 0.06)
        be.box(clay, wx - 0.38, wx + 0.38, ly - 0.45, ly + 0.05, wh - 0.75, wh, top=True)       # loom weight
        for k in range(5):
            tx = b - bundle_w / 2 + (k + 0.5) * bundle_w / 5
            mat = red if rng.random() < 0.12 else thread
            bottom = wh if rng.random() > 0.05 else rng.uniform(1.8, 3.4)                 # a few cut short
            be.strand(mat, tx, ly - 0.15, bottom, cloth_h - 0.3, 0.06, sway=(wx - tx) * 0.0)
    be.save(folder)

    c = wood.elev_canvas()
    alb, hgt, sm = bp.wood(c, n, C(0.1, 0.065, 0.04), C(0.22, 0.15, 0.09), offset=151)
    save_material(folder, wood, alb, hgt, sm)
    c = cloth.elev_canvas()
    weave = 0.5 + 0.25 * np.sin(c.X * 2 * math.pi / 0.2) * np.sin(c.Y * 2 * math.pi / 0.2)
    alb = lerp(C(0.34, 0.31, 0.26), C(0.48, 0.44, 0.37), weave * n.fbm(c.X, c.Y, 1.0, 3, offset=152))
    km, _ = meander_band(c.X, c.Y, (-90, 90, -10, cloth_h + 0.3), 0.0, 1.3)
    alb = lerp(alb, C(0.36, 0.05, 0.04), km)
    alb = alb * (1 - (1 - smoothstep(cloth_h, cloth_h + 0.6, c.Y)) * 0.3)[..., None]
    save_material(folder, cloth, alb.astype(F32), (weave * 0.02).astype(F32), np.full(c.X.shape, 0.1, F32))
    c = thread.elev_canvas()
    save_material(folder, thread, lerp(C(0.55, 0.52, 0.46), C(0.66, 0.63, 0.56), n.fbm(c.X, c.Y, 0.3, 2, offset=153)),
                  np.zeros(c.X.shape, F32), np.full(c.X.shape, 0.1, F32))
    save_material(folder, red, lerp(C(0.34, 0.05, 0.04), C(0.46, 0.08, 0.06), n.fbm(c.X, c.Y, 0.3, 2, offset=154)),
                  np.zeros(c.X.shape, F32), np.full(c.X.shape, 0.1, F32))
    c = clay.elev_canvas()
    alb, hgt, sm = bp.rock(c, n, C(0.22, 0.11, 0.06), C(0.36, 0.2, 0.11), offset=155)
    save_material(folder, clay, alb, hgt, sm)


def gate_edge():
    """The gate of Hades beyond the Styx: two massive pillars of cracked black stone, bronze gates of bars and
    crossbars between them rising out of frame, the walls running off to either side, the river at their foot."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    folder = os.path.join(OUT, 'TheGate')
    n = Noise(143)
    be = BackEdge('TheGate')
    stone = be.material(Material('Stone', tex_h=16.0, self_light=0.3))
    bronze = be.material(Material('Bronze', tex_h=16.0, self_light=0.25))
    gy = 35.6
    be.box(stone, -75, -24.0, gy + 0.8, gy + 3.0, 0, 14.0, batter=0.15)
    be.box(stone, 24.0, 75, gy + 0.8, gy + 3.0, 0, 14.0, batter=0.15)
    for px in (-21.5, 21.5):
        be.box(stone, px - 2.6, px + 2.6, gy - 0.4, gy + 3.2, 0, 18.0, batter=0.08)
        be.box(stone, px - 3.0, px + 3.0, gy - 0.7, gy + 3.4, 0, 1.2)                          # plinth
    for bx in np.arange(-18.6, 18.7, 0.85):
        be.box(bronze, bx - 0.13, bx + 0.13, gy + 0.9, gy + 1.15, 0, 16.0)
        be.lathe(bronze, bx, gy + 1.0, [(0.0, 0.0), (0.1, 0.22), (0.3, 0.0)], segments=6, cap=False)
    for hh in (1.4, 4.6, 7.8):
        be.box(bronze, -18.9, 18.9, gy + 0.7, gy + 1.35, hh, hh + 0.45, sides=False)
    be.save(folder)
    c = stone.elev_canvas()
    alb, hgt, sm = bp.masonry(c, n, 2.2, 1.0, C(0.08, 0.09, 0.09), C(0.19, 0.2, 0.19), C(0.02, 0.025, 0.025), offset=161)
    crack = smoothstep(0.86, 0.95, n.ridged(c.X * 0.7, c.Y, 1.8, 3, offset=162))
    alb = lerp(alb, C(0.0, 0.0, 0.0), crack * 0.8)
    alb = alb + C(0.02, 0.09, 0.07) * (1 - smoothstep(0.0, 1.5, c.Y))[..., None]      # the river's glow on the foot
    alb = bp.age(n, c.X, c.Y, alb, foot_dark=0.2)
    ta, th, ts = bp.tops(stone, n, C(0.04, 0.045, 0.045), C(0.09, 0.1, 0.095), offset=163)
    save_material(folder, stone, alb, hgt - crack * 0.1, sm, talb=ta, thgt=th, tsmooth=ts)
    c = bronze.elev_canvas()
    alb, hgt, sm = bp.metal(c, n, C(0.24, 0.16, 0.07), C(0.48, 0.33, 0.15), C(0.14, 0.26, 0.2), 0.5, offset=164)
    save_material(folder, bronze, alb, hgt, sm)


def paint(preview=False):
    throne(preview)
    fates(preview)
    the_gate(preview)
