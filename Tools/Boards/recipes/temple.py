"""Apollo's temple at Delphi: an aged marble floor of great square slabs, dulled with centuries of feet and smoke,
a Greek-key border inlaid in dark stone round the place of combat, scorched rings where bronze tripods stood,
scattered laurel. Lanes: runners of paler, smoother marble."""
from boardkit import *  # noqa

NAME = 'Temple'


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(103)
    rng = np.random.default_rng(103)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 14, ad)

    # slabs: a grid centred on the arena, two marbles in a loose chequer
    S = 6.4
    gx = (X - 0.0) / S
    gy = (Y - 10.64) / S
    cx = np.floor(gx).astype(np.int64)
    cy = np.floor(gy).astype(np.int64)
    edge = np.minimum(np.minimum(gx - cx, cx + 1 - gx), np.minimum(gy - cy, cy + 1 - gy)) * S
    joint = 1 - smoothstep(0.03, 0.1, edge)
    sid = n.cellhash(cx, cy, 2)
    m1, v1 = marble(n, X, Y, C(0.27, 0.265, 0.25), C(0.35, 0.34, 0.32), C(0.13, 0.13, 0.13), 0.9, 0.05, offset=1100)
    m2, v2 = marble(n, X + 50, Y, C(0.29, 0.27, 0.24), C(0.37, 0.34, 0.3), C(0.18, 0.15, 0.12), 0.8, 0.06, offset=1200)
    chequer = ((cx + cy) % 2 == 0).astype(F32)
    alb = lerp(m1, m2, chequer)
    alb = alb * (0.9 + 0.15 * sid)[..., None]
    grime = n.fbm(X, Y, 3, 4, offset=1)
    alb = alb * (1 - 0.25 * smoothstep(0.5, 0.8, grime) * margin)[..., None]
    alb = lerp(alb, C(0.08, 0.075, 0.07), joint * 0.85)
    hgt = ((sid - 0.5) * 0.04 - joint * 0.06 + grime * 0.03).astype(F32)
    # chipped slab corners
    chip = smoothstep(0.82, 0.9, n.fbm(X, Y, 0.8, 3, offset=2)) * (1 - smoothstep(0.2, 0.8, edge))
    alb = lerp(alb, C(0.12, 0.11, 0.1), chip * 0.6)
    hgt = (hgt - chip * 0.05).astype(F32)

    # inlaid Greek key round the arena, worn where feet cross it
    km, band = meander_band(X, Y, (-31.5, 31.5, -2.2, 23.5), 0.8, 2.4)
    km = km * (0.75 + 0.25 * smoothstep(0.3, 0.7, n.fbm(X, Y, 1.2, 3, offset=3)))
    inlay, _ = marble(n, X, Y, C(0.05, 0.06, 0.055), C(0.1, 0.12, 0.1), C(0.18, 0.2, 0.17), 0.5, 0.05, offset=1300)
    alb = lerp(alb, lerp(C(0.3, 0.28, 0.25), alb, 0.4), band * 0.5)
    alb = lerp(alb, inlay, km)
    hgt = (hgt - km * 0.02).astype(F32)

    # scorched rings where tripods stood, ash from offerings
    sc = Layer(g)
    feet = Layer(g)
    for x, y in [(-50, 32), (50, 32), (-52, -6), (52, -6)]:
        sc.ellipse(x, y, 3.2, 3.2)
        for k in range(3):
            a = k * 2.094 + 0.3
            feet.ellipse(x + math.cos(a) * 1.8, y + math.sin(a) * 1.8, 0.28, 0.28)
    scm = blurf(sc.read(), 1.2, g)
    ring = np.clip(scm * 2.2, 0, 1) * (1 - np.clip(blurf(sc.read(), 0.4, g) * 3 - 1.5, 0, 1) * 0.5)
    alb = alb * (1 - ring[..., None] * 0.55)
    ash = scm * smoothstep(0.45, 0.6, n.fbm(X, Y, 0.6, 3, offset=4))
    alb = lerp(alb, C(0.3, 0.29, 0.27), ash * 0.6)
    fm = blurf(feet.read(), 0.08, g)
    alb = lerp(alb, C(0.06, 0.05, 0.04), fm * 0.7)

    # laurel leaves
    lv = Layer(g)
    for x, y in scatter_points(rng, 26, (-66, 66, -23, 43), margin_keep(3.0), 4.0):
        for _ in range(rng.integers(3, 9)):
            leaf(lv, x + rng.normal(0, 0.9), y + rng.normal(0, 0.9), rng.uniform(0.9, 1.4), rng.uniform(0, 6.28), 0.2)
    lm = lv.read()
    lcol = lerp(C(0.1, 0.13, 0.06), C(0.22, 0.25, 0.12), n.fbm(X, Y, 0.4, 3, offset=5))
    alb, hgt = apply_object(g, alb, hgt, lm, lm * 0.04, lcol, shadow=0.45, shadow_blur=0.1)

    cav = cavity(hgt, g, 0.4, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.35), 0.1)
    smooth = 0.42 - joint * 0.3 - grime * 0.12 * margin + km * 0.1 - ring * 0.25

    # lanes: runners of paler, smoother marble in long slabs
    LX, LY = lane.WX, lane.WY
    lm1, lv1 = marble(n, LX, LY, C(0.34, 0.33, 0.31), C(0.44, 0.43, 0.4), C(0.26, 0.25, 0.23), 0.55, 0.05, offset=1400)
    lj, lsid, _ = flagstones(n, LX, LY, 7.5, 5.0, gap=0.05, stagger=0.5, offset=1401)
    wear = lane_wear(lane)
    lalb = lm1 * (0.93 + 0.1 * lsid)[..., None]
    lalb = lerp(lalb, C(0.14, 0.13, 0.12), lj * 0.7)
    lalb = lalb + ((brush(n, LX, LY) - 0.5) * 0.02)[..., None]
    lhgt = ((lsid - 0.5) * 0.03 - lj * 0.05 + n.fbm(LX, LY, 0.6, 3, offset=1402) * 0.02).astype(F32)
    lcav = cavity(lhgt, lane, 0.3, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.25), 0.1)
    lsmooth = 0.5 + wear * 0.1 - lj * 0.35

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """Apollo's temple front: a three-stepped stylobate and a Doric colonnade rising out of frame, the shadowed cella
    wall behind the columns."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    import bekit as bk
    folder = os.path.join(OUT, NAME)
    n = Noise(107)
    be = BackEdge(NAME)
    mar = be.material(Material('Marble', tex_h=16.0, self_light=0.42))
    wall = be.material(Material('Cella', tex_h=16.0, self_light=0.18))
    top_y, top_h = bk.steps(be, mar, -75, 75, 34.0, 3, 0.5, 0.6)
    be.box(mar, -75, 75, top_y - 0.01, 41.5, 0, top_h, sides=False)
    x = -72.8
    while x < 75:
        bk.doric_column(be, mar, x, top_y + 1.3, top_h, 15.0, 1.05)
        x += 5.2
    be.box(wall, -75, 75, 40.0, 42.0, top_h, 18.0, sides=False)
    be.save(folder)

    c = mar.elev_canvas()
    alb, hgt, sm = bp.marble(c, n, C(0.46, 0.44, 0.4), C(0.6, 0.58, 0.53), C(0.32, 0.3, 0.28), 0.5, 0.04, offset=31)
    alb = bp.age(n, c.X, c.Y, alb, foot_dark=0.35, streaks=0.35)
    ta, th, ts = bp.tops(mar, n, C(0.42, 0.4, 0.36), C(0.55, 0.52, 0.47), offset=32)
    save_material(folder, mar, desaturate(alb, 0.1), hgt, sm, talb=ta, thgt=th, tsmooth=ts)
    c = wall.elev_canvas()
    alb, hgt, sm = bp.masonry(c, n, 1.6, 0.7, C(0.1, 0.085, 0.075), C(0.16, 0.14, 0.12), C(0.05, 0.04, 0.035), offset=33)
    fresco = (1 - smoothstep(0.25, 0.35, np.abs(c.Y - 3.2))) * 0.5
    alb = lerp(alb, C(0.22, 0.07, 0.05), fresco * smoothstep(0.3, 0.6, n.fbm(c.X, c.Y, 1.0, 3, offset=34)))
    save_material(folder, wall, alb, hgt, sm)
