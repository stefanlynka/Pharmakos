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
    back_edge()


def back_edge():
    """The Gorgon's cave closes in: a rugged wall of wet rock rising out of frame, stalagmites at its foot."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    folder = os.path.join(OUT, NAME)
    n = Noise(57)
    rng = np.random.default_rng(57)
    be = BackEdge(NAME)
    rk = be.material(Material('CaveRock', tex_h=16.0, self_light=0.32))
    f = lambda x, h, sc, o: n.fbm(np.array([[x]], F32), np.array([[h]], F32), sc, 3, offset=o)[0, 0]
    be.relief(rk, -75, 75, lambda x: 35.3 + 1.6 * f(x, 0, 9, 21), lambda x: 15.0,
              lambda x, h: 2.4 * f(x, h, 3.0, 22) + 0.8 * f(x, h, 0.8, 23) + h * 0.12, step=0.5, h_step=0.5)
    for _ in range(30):
        x = rng.uniform(-72, 72)
        y = rng.uniform(34.6, 35.6) if -11 < x < 31 else rng.uniform(33.0, 35.4)
        r = rng.uniform(0.35, 1.0)
        top = r * rng.uniform(2.5, 5.0)
        seed = rng.uniform(0, 100)
        wob = lambda t, h, s_=seed: 0.8 + 0.4 * n.value(np.array([[math.cos(t) + s_]], F32), np.array([[h * 1.5 + s_]], F32), 1.0, 9)[0, 0]
        be.lathe(rk, x, y, [(0.0, r * 1.3), (top * 0.3, r * 0.9), (top * 0.75, r * 0.45), (top, 0.05)], segments=10, wobble=wob)
    be.save(folder)
    c = rk.elev_canvas()
    alb, hgt, sm = bp.rock(c, n, C(0.07, 0.085, 0.075), C(0.2, 0.23, 0.2), strata=0.6, offset=181)
    slime = smoothstep(0.6, 0.76, n.fbm(c.X, c.Y, 2.0, 4, offset=182))
    alb = lerp(alb, C(0.06, 0.1, 0.05), slime * 0.6)
    flow = smoothstep(0.7, 0.9, n.value(c.X * 3.0, c.Y * 0.15, 1.0, 183))      # wet runnels down the face
    ta, th, ts = bp.tops(rk, n, C(0.04, 0.05, 0.045), C(0.1, 0.12, 0.1), offset=184)
    save_material(folder, rk, alb, hgt, sm + flow * 0.4 + slime * 0.2, talb=ta, thgt=th, tsmooth=ts)
