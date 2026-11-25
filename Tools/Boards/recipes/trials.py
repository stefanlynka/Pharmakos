"""The Amazons' proving ground: an arena of raked ochre sand ringed by a low kerb of worn stone blocks, scuffed by
running feet, straw butts bristling with arrows and spent javelins at the edges, dry grass beyond the kerb.
Lanes: smooth compacted sand with fine rake lines running their length."""
from boardkit import *  # noqa

NAME = 'Trials'


def target(layer, rings, rng, x, y, r):
    layer.ellipse(x, y, r, r, 255, n=48)
    for k, rr in enumerate((0.75, 0.5, 0.25)):
        rings.ellipse(x, y, r * rr, r * rr, 255 if k % 2 == 0 else 120, n=40)


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(83)
    rng = np.random.default_rng(83)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)

    # the kerb: a rounded rectangle of stone blocks around the arena
    kerb_d = sd_box(X, Y, (-44, 44, -14.5, 35.5), 12.0)
    inside = smoothstep(0.5, -0.5, kerb_d)

    big = n.fbm(X, Y, 12, 4, offset=2)
    grain = n.fbm(X, Y, 0.35, 5, offset=3)
    sand = lerp(C(0.27, 0.2, 0.12), C(0.43, 0.33, 0.21), np.clip(big * 0.5 + grain * 0.5, 0, 1))
    # rake lines: concentric with the kerb, wavering
    rake = 0.5 + 0.5 * np.sin((kerb_d + (n.fbm(X, Y, 4, 3, offset=4) - 0.5) * 0.8) * 2 * math.pi / 0.7)
    rake_fade = smoothstep(0.3, 0.6, n.fbm(X, Y, 5, 3, offset=5)) * inside * smoothstep(-1.0, 2.0, ad)
    alb = sand * (1 - (rake * 0.07 * rake_fade))[..., None]
    alb = alb + ((brush(n, X, Y) - 0.5) * 0.03)[..., None]
    hgt = (big * 0.3 + grain * 0.04 + rake * 0.05 * rake_fade).astype(F32)

    # outside the kerb: harder ground and dry grass
    outside = 1 - inside
    dryg = tufts(n, X, Y, 0.4, 30, offset=6)
    ground_out = lerp(C(0.17, 0.13, 0.08), C(0.32, 0.27, 0.15), np.clip(dryg * 0.6 + big * 0.4, 0, 1))
    alb = lerp(alb, ground_out, outside * smoothstep(0.5, 3.0, kerb_d))
    hgt = (hgt + outside * dryg * 0.06).astype(F32)

    # kerb stones: individual blocks laid along the line, some shifted or missing, sand drifted over others
    kl = Layer(g)
    kx0, kx1, ky0, ky1, kr = -44.0, 44.0, -14.5, 35.5, 12.0

    def perim(t):
        # rounded rectangle parameterised by arc length t (0..L)
        sx, sy = (kx1 - kx0) - 2 * kr, (ky1 - ky0) - 2 * kr
        seg = [sx, math.pi * kr / 2, sy, math.pi * kr / 2, sx, math.pi * kr / 2, sy, math.pi * kr / 2]
        t = t % sum(seg)
        cx = [(kx0 + kr, ky0), (kx1 - kr, ky0 + kr), (kx1, ky0 + kr), (kx1 - kr, ky1 - kr),
              (kx1 - kr, ky1), (kx0 + kr, ky1 - kr), (kx0, ky1 - kr), (kx0 + kr, ky0 + kr)]
        for i, L in enumerate(seg):
            if t <= L:
                if i % 2 == 0:
                    d = [(1, 0), (0, 1), (-1, 0), (0, -1)][i // 2]
                    return cx[i][0] + d[0] * t, cx[i][1] + d[1] * t, math.atan2(d[1], d[0])
                a0 = [-math.pi / 2, 0, math.pi / 2, math.pi][i // 2]
                a = a0 + t / kr
                ox, oy = cx[i]
                return ox + math.cos(a) * kr, oy + math.sin(a) * kr, a + math.pi / 2
            t -= L
        return kx0, ky0, 0.0
    total = 2 * ((kx1 - kx0) - 2 * kr) + 2 * ((ky1 - ky0) - 2 * kr) + 2 * math.pi * kr
    t = 0.0
    while t < total:
        bl = rng.uniform(1.6, 2.6)
        if rng.random() > 0.06:
            px, py, ang = perim(t + bl / 2)
            px += rng.normal(0, 0.08)
            py += rng.normal(0, 0.08)
            ang += rng.normal(0, 0.05)
            hw, hd = bl / 2 - 0.08, rng.uniform(0.45, 0.6)
            ca, sa = math.cos(ang), math.sin(ang)
            kl.poly([(px - ca * hw - sa * hd, py - sa * hw + ca * hd), (px + ca * hw - sa * hd, py + sa * hw + ca * hd),
                     (px + ca * hw + sa * hd, py + sa * hw - ca * hd), (px - ca * hw + sa * hd, py - sa * hw - ca * hd)])
        t += bl
    kerb = kl.read()
    drift = smoothstep(0.62, 0.8, n.fbm(X, Y, 2.5, 3, offset=9))
    kerb = kerb * (1 - drift * 0.85)
    bid = n.fbm(X, Y, 1.0, 3, offset=7)
    kcol = lerp(C(0.27, 0.24, 0.19), C(0.45, 0.41, 0.34), np.clip(bid * 0.5 + grain * 0.5, 0, 1))
    kdome = np.clip(blurf(kerb, 0.1, g) * 1.5, 0, 1) * kerb
    alb, hgt = apply_object(g, alb, hgt, kerb, kdome * 0.6, kcol, shadow=0.5, shadow_blur=0.3)

    # footprints scuffing the sand
    fp = Layer(g)
    for _ in range(700):
        x, y = rng.uniform(-62, 62), rng.uniform(-20, 40)
        if not (inside[int(g.px(x, y)[1]) % g.h, int(g.px(x, y)[0]) % g.w] > 0.5):
            continue
        footprint(fp, x, y, rng.uniform(0.85, 1.05), rng.uniform(0, 6.28))
    fpm = blurf(fp.read(), 0.04, g) * smoothstep(-2.0, 3.0, ad)
    alb = lerp(alb, alb * 0.8, fpm * 0.5)
    hgt = (hgt - fpm * 0.05).astype(F32)

    # straw targets bristling with arrows, and spent javelins
    tl = Layer(g)
    rings = Layer(g, ss=1)
    tpos = [(-56, 30), (56, 30), (-58, -4), (58, -6)]
    for x, y in tpos:
        target(tl, rings, rng, x, y, 2.4)
    tm = tl.read()
    rv = blurf(rings.read(), 0.03, g) * tm
    straw = lerp(C(0.33, 0.27, 0.13), C(0.5, 0.41, 0.2), brush(n, X, Y, 0.3, 4.0, offset=8))
    tcol = lerp(straw, C(0.33, 0.1, 0.05), smoothstep(0.35, 0.5, rv) * (1 - smoothstep(0.7, 0.9, rv)))
    tcol = lerp(tcol, C(0.07, 0.05, 0.04), smoothstep(0.7, 0.9, rv))
    tdome = np.clip(blurf(tm, 0.4, g) * 1.5, 0, 1) * tm
    alb, hgt = apply_object(g, alb, hgt, tm, tdome * 0.7, tcol, shadow=0.6, shadow_blur=0.4)
    ar = Layer(g)
    for x, y in tpos:
        for _ in range(rng.integers(5, 10)):
            a = rng.uniform(0, 6.28)
            d = rng.uniform(0, 1.8)
            ax, ay = x + math.cos(a) * d, y + math.sin(a) * d
            ta = rng.uniform(0, 6.28)
            ar.line([(ax, ay), (ax + math.cos(ta) * 1.1, ay + math.sin(ta) * 1.1)], 0.07)
            ar.ellipse(ax + math.cos(ta) * 1.1, ay + math.sin(ta) * 1.1, 0.12, 0.12)
    for x, y in scatter_points(rng, 30, (-66, 66, -23, 43), margin_keep(3.0), 3.0):
        a = rng.uniform(0, 6.28)
        L = rng.uniform(2.0, 4.5)
        ar.line([(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], rng.uniform(0.06, 0.12))
    am = ar.read()
    alb, hgt = apply_object(g, alb, hgt, am, am * 0.06, lerp(C(0.22, 0.16, 0.1), C(0.4, 0.3, 0.18), grain), shadow=0.5, shadow_blur=0.1)

    cav = cavity(hgt, g, 0.5, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.35), 0.12)
    smooth = 0.1 + kerb * 0.08

    # lanes: smooth compacted sand, fine rake lines lengthwise
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, C(0.31, 0.24, 0.15), C(0.42, 0.33, 0.21), offset=980)
    wear = lane_wear(lane)
    lrake = 0.5 + 0.5 * np.sin((LY + (n.fbm(LX, LY, 6, 3, offset=981) - 0.5) * 0.6) * 2 * math.pi / 0.4)
    lfade = smoothstep(0.35, 0.65, n.fbm(LX, LY, 4, 3, offset=982)) * (1 - wear * 0.7)
    lalb = lalb * (1 - lrake * 0.09 * lfade)[..., None]
    lhgt = (lhgt + lrake * 0.04 * lfade).astype(F32)
    fp2 = Layer(lane)
    for _ in range(160):
        footprint(fp2, rng.uniform(-29.5, 29.5), rng.choice([rng.uniform(1.5, 8.5), rng.uniform(11.5, 18.5)]),
                  rng.uniform(0.85, 1.0), rng.uniform(0, 6.28))
    fpm2 = blurf(fp2.read(), 0.04, lane) * wear
    lalb = lerp(lalb, lalb * 0.82, fpm2 * 0.6)
    lhgt = (lhgt - fpm2 * 0.04).astype(F32)
    lcav = cavity(lhgt, lane, 0.4, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.25), 0.12)
    lsmooth = 0.1 + 0 * wear

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """The Amazons' arena: stone seating tiers rising behind the kerb."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    import bekit as bk
    folder = os.path.join(OUT, NAME)
    n = Noise(87)
    be = BackEdge(NAME)
    st = be.material(Material('Tiers', tex_h=8.0, self_light=0.4))
    bk.steps(be, st, -75, 75, 34.8, 9, 0.8, 1.3)
    be.save(folder)
    c = st.elev_canvas()
    alb, hgt, sm = bp.masonry(c, n, 2.4, 0.8, C(0.38, 0.33, 0.25), C(0.52, 0.46, 0.36), C(0.13, 0.11, 0.08), offset=51)
    alb = bp.age(n, c.X, c.Y, alb, foot_dark=0.3, streaks=0.2)
    ta, th, ts = bp.tops(st, n, C(0.4, 0.35, 0.27), C(0.55, 0.48, 0.37), offset=52)
    save_material(folder, st, alb, hgt, sm, talb=ta, thgt=th, tsmooth=ts)
