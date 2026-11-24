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
