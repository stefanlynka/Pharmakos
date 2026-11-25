"""Polyphemus' island: rough hillside pasture of dry, wind-laid grass, grey limestone boulders with lichen, and the
giant's footprints pressed deep into the turf. Lanes: dusty sheep tracks, dotted with cloven hoofprints."""
from boardkit import *  # noqa

NAME = 'Cyclops'


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(23)
    rng = np.random.default_rng(23)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 12, ad)

    blades = tufts(n, X, Y)
    clumps = n.fbm(X, Y, 1.2, 4, offset=10)
    turf = np.clip(blades * 0.65 + clumps * 0.35, 0, 1)
    dry = n.fbm(X, Y, 9, 4, offset=1)
    big = n.fbm(X, Y, 18, 3, offset=2)
    green = lerp(C(0.06, 0.075, 0.035), C(0.22, 0.24, 0.11), turf)
    straw = lerp(C(0.13, 0.115, 0.06), C(0.38, 0.33, 0.18), turf)
    alb = lerp(green, straw, smoothstep(0.4, 0.75, dry))
    alb = alb * (0.75 + 0.4 * big)[..., None]

    # bare, trodden earth: patches in the turf and most of the arena
    bare = smoothstep(0.62, 0.78, n.fbm(X, Y, 5, 4, offset=3)) * (0.5 + 0.5 * margin)
    bare = np.maximum(bare, smoothstep(2.0, -4.0, ad) * (0.55 + 0.35 * n.fbm(X, Y, 3, 3, offset=4)))
    earth = lerp(C(0.15, 0.115, 0.08), C(0.27, 0.215, 0.15), n.fbm(X, Y, 0.9, 4, offset=5))
    alb = lerp(alb, earth, bare)
    hgt = (big * 0.5 + turf * 0.12 * (1 - bare) + clumps * 0.1 - bare * 0.08).astype(F32)

    # the giant's footprints, pressed into the turf
    fp = Layer(g)
    footprint(fp, -27, 33.0, 13.0, 0.25)
    footprint(fp, 47, 13.0, 12.0, 1.2)
    fpm = fp.read()
    fpd = np.clip(blurf(fpm, 0.25, g) * 1.15, 0, 1)
    rim = np.clip(blurf(fpm, 0.9, g) * 1.8 - fpd * 1.6, 0, 1)
    mud = lerp(C(0.1, 0.075, 0.05), C(0.17, 0.13, 0.09), n.fbm(X, Y, 0.7, 3, offset=6))
    alb = lerp(alb, mud, fpd * 0.9)
    alb = lerp(alb, earth, rim * 0.6)
    hgt = (hgt - fpd * 0.9 + rim * 0.25).astype(F32)

    # limestone: a few big boulders out at the edges, smaller stones, and gravel everywhere off the arena
    place = []
    for x, y in scatter_points(rng, 16, (-66, 66, -23, 43), margin_keep(9.0), min_gap=9):
        place.append((x, y, rng.uniform(1.8, 3.6)))
    for x, y in scatter_points(rng, 70, (-66, 66, -23, 43), margin_keep(3.0), min_gap=2.5):
        place.append((x, y, rng.uniform(0.4, 1.1)))
    for x, y in scatter_points(rng, 500, (-66, 66, -23, 43), margin_keep(0.5), min_gap=0.5):
        place.append((x, y, rng.uniform(0.08, 0.25)))
    rm, rh, rc = rocks(g, n, rng, place, base=C(0.17, 0.165, 0.155), light=C(0.47, 0.45, 0.41), lichen=C(0.38, 0.38, 0.2))
    alb, hgt = apply_object(g, alb, hgt, rm, rh, rc)

    # tufts of fleece snagged on the grass
    wool = Layer(g)
    for x, y in scatter_points(rng, 40, (-66, 66, -23, 43), margin_keep(4.0), 2.0):
        for _ in range(rng.integers(2, 5)):
            blob(wool, rng, x + rng.normal(0, 0.3), y + rng.normal(0, 0.3), rng.uniform(0.15, 0.3), 10, 0.4)
    wm = wool.read()
    alb, hgt = apply_object(g, alb, hgt, wm, wm * 0.15, lerp(C(0.5, 0.47, 0.4), C(0.66, 0.63, 0.55),
                                                                n.fbm(X, Y, 0.2, 2, offset=7)), shadow=0.35)

    cav = cavity(hgt, g, 0.6, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.45), 0.12)
    smooth = 0.12 + fpd * 0.18 + rm * 0.08

    # lanes: dusty sheep tracks with hoofprints and a little grass creeping in at the edges
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, C(0.21, 0.165, 0.115), C(0.31, 0.255, 0.18), offset=900)
    wear = lane_wear(lane)
    lturf = np.clip(tufts(n, LX, LY) * 0.7 + 0.3 * n.fbm(LX, LY, 1.2, 3, offset=903), 0, 1)
    edge_grass = (1 - wear) * smoothstep(0.45, 0.7, n.fbm(LX, LY, 1.5, 3, offset=901))
    lalb = lerp(lalb, lerp(C(0.13, 0.14, 0.07), C(0.27, 0.25, 0.13), lturf), edge_grass * 0.75)
    prints = Layer(lane)
    for i in range(200):
        x = rng.uniform(-29.5, 29.5)
        ly = rng.choice([rng.uniform(1.5, 8.5), rng.uniform(11.5, 18.5)])
        a = rng.choice([0.0, math.pi]) + rng.normal(0, 0.35)
        hoofprint(prints, x, ly, rng.uniform(0.3, 0.42), a)
    pm = blurf(prints.read(), 0.04, lane) * (0.3 + 0.7 * smoothstep(0.3, 0.6, n.fbm(LX, LY, 3, 2, offset=902)))
    lalb = lerp(lalb, C(0.17, 0.13, 0.09), pm * 0.4)
    lhgt = (lhgt - pm * 0.12 + edge_grass * 0.04).astype(F32)
    pebbles = []
    for _ in range(260):
        pebbles.append((rng.uniform(-29.5, 29.5), rng.uniform(0.2, 19.8), rng.uniform(0.06, 0.2)))
    pm2, ph2, pc2 = rocks(lane, n, rng, pebbles, base=C(0.24, 0.22, 0.19), light=C(0.42, 0.39, 0.34))
    lalb, lhgt = apply_object(lane, lalb, lhgt, pm2, ph2 * 0.5, pc2, shadow=0.4)
    lcav = cavity(lhgt, lane, 0.4, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.35), 0.15)
    lsmooth = 0.12 + pm * 0.1

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """Polyphemus' hillside: a rocky slope with grass on its ledges, the black mouth of his cave, and the great
    boulder he rolls across it."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    folder = os.path.join(OUT, NAME)
    n = Noise(27)
    be = BackEdge(NAME)
    rk = be.material(Material('HillRock', tex_h=14.0, self_light=0.38))
    bould = be.material(Material('Boulder', tex_h=8.0, self_light=0.4))
    f = lambda x, h, sc, o: n.fbm(np.array([[x]], F32), np.array([[h]], F32), sc, 4, offset=o)[0, 0]
    cave_x = -30.0

    def depth(x, h):
        d = 1.6 * f(x, h, 2.5, 41) + 0.5 * f(x, h, 0.7, 42) + h * 0.5
        inside = max(0.0, 1 - ((x - cave_x) / 4.2) ** 2 - (h / 4.8) ** 2)
        return d + 5.0 * min(1.0, inside * 3)                                           # the cave mouth recedes
    be.relief(rk, -75, 75, lambda x: 35.0 + 0.8 * f(x, 0, 9, 43), lambda x: 3.2 + 3.4 * f(x, 0, 8, 44) + 1.8 * f(x, 3, 1.5, 45),
              depth, step=0.4, h_step=0.4, back=6.0)
    seed = 3.0
    wob = lambda t, h: 0.85 + 0.3 * n.value(np.array([[math.cos(t) * 1.5 + seed]], F32), np.array([[h + seed]], F32), 1.0, 46)[0, 0]
    be.lathe(bould, cave_x + 8.5, 35.2, [(0.0, 1.5), (0.6, 2.9), (1.8, 3.4), (3.2, 3.1), (4.4, 2.2), (5.1, 0.8), (5.3, 0.0)],
             segments=18, wobble=wob)
    be.save(folder)
    c = rk.elev_canvas()
    alb, hgt, sm = bp.rock(c, n, C(0.22, 0.21, 0.19), C(0.5, 0.48, 0.43), strata=0.4, offset=201)
    grass = smoothstep(0.55, 0.7, n.fbm(c.X, c.Y, 1.2, 4, offset=202)) * smoothstep(1.0, 4.0, c.Y)
    alb = lerp(alb, lerp(C(0.1, 0.12, 0.05), C(0.24, 0.24, 0.11), n.fbm(c.X, c.Y, 0.2, 3, offset=203)), grass * 0.75)
    mouth = np.clip(1 - ((c.X - cave_x) / 4.2) ** 2 - (c.Y / 4.8) ** 2, 0, 1)
    alb = alb * (1 - smoothstep(0.0, 0.25, mouth) * 0.92)[..., None]
    alb = bp.age(n, c.X, c.Y, alb, foot_dark=0.3)
    ta, th, ts = bp.tops(rk, n, C(0.12, 0.13, 0.07), C(0.26, 0.26, 0.13), offset=204)
    save_material(folder, rk, alb, hgt, sm, talb=ta, thgt=th, tsmooth=ts)
    c = bould.elev_canvas()
    alb, hgt, sm = bp.rock(c, n, C(0.22, 0.21, 0.19), C(0.46, 0.44, 0.4), strata=0.1, offset=205)
    lich = smoothstep(0.62, 0.76, n.fbm(c.X, c.Y, 0.5, 3, offset=206))
    save_material(folder, bould, lerp(alb, C(0.36, 0.36, 0.2), lich * 0.5), hgt, sm)
