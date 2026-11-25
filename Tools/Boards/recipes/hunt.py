"""Artemis' hunt: an old forest floor of moss and leaf litter, roots snaking in from the edges, fern fronds and
fallen twigs, a shed antler and a few spent arrows. Lanes: a muddy game trail cut with deer slots."""
from boardkit import *  # noqa

NAME = 'Hunt'


def fern(layer, rng, x, y, length, angle, v=255):
    ca, sa = math.cos(angle), math.sin(angle)
    curve = rng.uniform(-0.25, 0.25)
    pts = []
    for i in range(21):
        t = i / 20
        a = angle + curve * t
        pts.append((x + math.cos(a) * length * t, y + math.sin(a) * length * t))
    layer.line(pts, length * 0.025, v)
    for i in range(2, 20):
        t = i / 20
        px, py = pts[i]
        a = angle + curve * t
        ln = length * 0.28 * math.sin(math.pi * min(1.0, t * 1.1)) + 0.05
        for side in (-1, 1):
            la = a + side * 1.2
            leaf(layer, px, py, ln, la, 0.22, 0, v)


def antler(layer, rng, x, y, s, angle, v=255):
    a = angle
    pts = [(x, y)]
    for i in range(10):
        a += 0.12
        x += math.cos(a) * 0.45 * s
        y += math.sin(a) * 0.45 * s
        pts.append((x, y))
        if i in (3, 6, 9):
            tine = [(x, y), (x + math.cos(a - 1.1) * 1.0 * s, y + math.sin(a - 1.1) * 1.0 * s)]
            taper(layer, tine, 0.16 * s, 0.05 * s, v)
    taper(layer, pts, 0.26 * s, 0.12 * s, v)


def arrow(layer, fletch, x, y, length, angle, v=255):
    ca, sa = math.cos(angle), math.sin(angle)
    tail = (x - ca * length / 2, y - sa * length / 2)
    tip = (x + ca * length / 2, y + sa * length / 2)
    layer.line([tail, tip], 0.07, v)
    px, py = -sa, ca
    layer.poly([tip, (tip[0] - ca * 0.35 + px * 0.12, tip[1] - sa * 0.35 + py * 0.12),
                (tip[0] - ca * 0.35 - px * 0.12, tip[1] - sa * 0.35 - py * 0.12)], v)
    for side in (-1, 1):
        fletch.poly([tail, (tail[0] + ca * 0.6, tail[1] + sa * 0.6),
                     (tail[0] + ca * 0.45 + px * 0.16 * side, tail[1] + sa * 0.45 + py * 0.16 * side),
                     (tail[0] + px * 0.14 * side, tail[1] + py * 0.14 * side)], v)


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(61)
    rng = np.random.default_rng(61)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 12, ad)

    big = n.fbm(X, Y, 15, 4, offset=1)
    grain = n.fbm(X, Y, 0.5, 5, offset=2)
    soil = lerp(C(0.06, 0.045, 0.03), C(0.15, 0.11, 0.07), np.clip(big * 0.6 + grain * 0.4, 0, 1))
    moss = smoothstep(0.45, 0.65, n.fbm(X, Y, 5, 4, offset=3)) * (0.3 + 0.7 * margin)
    mossy = lerp(C(0.05, 0.075, 0.025), C(0.15, 0.2, 0.06), tufts(n, X, Y, 0.3, 30, offset=4) * 0.7 + grain * 0.3)
    alb = lerp(soil, mossy, moss)
    alb = alb + ((brush(n, X, Y) - 0.5) * 0.03)[..., None]
    alb = lerp(alb, C(0.17, 0.13, 0.09), smoothstep(2.0, -4.0, ad) * 0.45)
    hgt = (big * 0.4 + grain * 0.06 + moss * 0.1).astype(F32)

    # roots snaking in from the edges
    roots = Layer(g)
    for i in range(14):
        side = i % 4
        t = rng.uniform(0.05, 0.95)
        x, y = [(-68 + 136 * t, 44.5), (-68 + 136 * t, -24.5), (-68.5, -24 + 68 * t), (68.5, -24 + 68 * t)][side]
        a = math.atan2(11.6 - y, -x) + rng.normal(0, 0.6)
        w = rng.uniform(0.5, 0.95)
        pts = [(x, y)]
        for k in range(rng.integers(30, 80)):
            a += rng.normal(0, 0.18)
            x += math.cos(a) * 0.4
            y += math.sin(a) * 0.4
            if float(arena_dist(np.array([[x]], F32), np.array([[y]], F32))[0, 0]) < 4:
                break
            pts.append((x, y))
            if k % 11 == 5 and rng.random() < 0.6:
                ba = a + rng.choice([-1, 1]) * rng.uniform(0.5, 1.1)
                bx, by = x, y
                bpts = [(bx, by)]
                for _ in range(rng.integers(6, 16)):
                    ba += rng.normal(0, 0.25)
                    bx += math.cos(ba) * 0.35
                    by += math.sin(ba) * 0.35
                    bpts.append((bx, by))
                taper(roots, bpts, w * 0.45, 0.06)
        taper(roots, pts, w, 0.12)
    rtm = roots.read()
    rdome = np.clip(blurf(rtm, 0.12, g) * 1.6, 0, 1) * rtm
    rcol = lerp(C(0.09, 0.065, 0.045), C(0.25, 0.19, 0.13), rdome * (0.6 + 0.5 * n.fbm(X * 0.4, Y * 0.4, 0.6, 3, offset=5)))
    rcol = lerp(rcol, C(0.1, 0.14, 0.05), smoothstep(0.6, 0.75, n.fbm(X, Y, 1.2, 3, offset=6)) * 0.6)
    alb, hgt = apply_object(g, alb, hgt, rtm, rdome * 0.45, rcol, shadow=0.6, shadow_blur=0.3)

    # leaf litter: oak-like leaves in drifts, browns and ochres
    lv = Layer(g)
    tint = Layer(g)
    pts = dense_points(rng, 5200)
    dens = n.fbm(pts[:, 0:1].astype(F32), pts[:, 1:2].astype(F32), 6, 3, offset=7)[:, 0]
    d = arena_dist(pts[:, 0:1].astype(F32), pts[:, 1:2].astype(F32))[:, 0]
    keep = (rng.random(len(pts)) < smoothstep(0.35, 0.65, dens) * np.clip(d / 8, 0.05, 1)) & (d > 0.5)
    for x, y in pts[keep]:
        s = rng.uniform(0.7, 1.3)
        a = rng.uniform(0, 6.28)
        leaf(lv, x, y, s, a, 0.32, 4)
        leaf(tint, x, y, s, a, 0.32, 4, int(rng.uniform(20, 255)))
    lm = lv.read()
    tv = np.clip(tint.read() / np.maximum(lm, 1e-3), 0, 1)
    leafcol = lerp(C(0.13, 0.08, 0.04), C(0.3, 0.2, 0.08), tv)
    leafcol = lerp(leafcol, C(0.2, 0.08, 0.04), smoothstep(0.85, 1.0, tv) * 0.6)
    leafcol = leafcol * (0.8 + 0.35 * grain)[..., None]
    alb, hgt = apply_object(g, alb, hgt, lm, lm * 0.05, leafcol, shadow=0.45, shadow_blur=0.12)

    # ferns at the very edges
    fl = Layer(g)
    for x, y in scatter_points(rng, 34, (-68, 68, -24, 44), margin_keep(14.0), 4.0):
        for k in range(rng.integers(3, 7)):
            fern(fl, rng, x, y, rng.uniform(2.5, 4.5), rng.uniform(0, 6.28))
    fm = fl.read()
    fcol = lerp(C(0.05, 0.09, 0.03), C(0.14, 0.22, 0.06), n.fbm(X, Y, 0.5, 3, offset=8))
    alb, hgt = apply_object(g, alb, hgt, fm, fm * 0.12, fcol, shadow=0.55, shadow_blur=0.3)

    # twigs, a shed antler, spent arrows
    tw = Layer(g)
    strokes(tw, rng, scatter_points(rng, 300, (-66, 66, -23, 43), margin_keep(1.0), 0.8), 0.9, 0.08)
    twm = tw.read()
    alb, hgt = apply_object(g, alb, hgt, twm, twm * 0.05, C(0.17, 0.12, 0.08), shadow=0.4, shadow_blur=0.06)
    ant = Layer(g)
    for x, y in scatter_points(rng, 3, (-62, 62, -21, 41), margin_keep(8.0), 25):
        antler(ant, rng, x, y, rng.uniform(1.0, 1.3), rng.uniform(0, 6.28))
    am = ant.read()
    adome = np.clip(blurf(am, 0.06, g) * 1.5, 0, 1) * am
    alb, hgt = apply_object(g, alb, hgt, am, adome * 0.2, lerp(C(0.28, 0.24, 0.18), C(0.52, 0.47, 0.38), adome), shadow=0.55)
    ar = Layer(g)
    fl2 = Layer(g)
    for x, y in scatter_points(rng, 7, (-62, 62, -21, 41), margin_keep(3.0, 14.0), 6):
        arrow(ar, fl2, x, y, rng.uniform(3.0, 3.8), rng.uniform(0, 6.28))
    arm = ar.read()
    ftm = fl2.read()
    alb, hgt = apply_object(g, alb, hgt, arm, arm * 0.05, C(0.3, 0.22, 0.13), shadow=0.45, shadow_blur=0.08)
    alb, hgt = apply_object(g, alb, hgt, ftm, ftm * 0.03, C(0.45, 0.42, 0.38), shadow=0.3, shadow_blur=0.06)

    cav = cavity(hgt, g, 0.5, 2.2)
    alb = desaturate(alb * (1 - cav[..., None] * 0.45), 0.1)
    smooth = 0.12 + moss * 0.05 + lm * 0.08

    # lanes: muddy game trail with deer slots and the odd leaf
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, C(0.12, 0.09, 0.06), C(0.21, 0.16, 0.11), offset=960)
    wear = lane_wear(lane)
    wet = smoothstep(0.55, 0.75, n.fbm(LX, LY, 3, 4, offset=961)) * wear
    lalb = lerp(lalb, C(0.08, 0.06, 0.04), wet * 0.5)
    pr = Layer(lane)
    for _ in range(170):
        x, y = rng.uniform(-29.5, 29.5), rng.choice([rng.uniform(1.5, 8.5), rng.uniform(11.5, 18.5)])
        hoofprint(pr, x, y, rng.uniform(0.45, 0.6), rng.choice([0.0, math.pi]) + rng.normal(0, 0.3))
    pm = blurf(pr.read(), 0.04, lane)
    lalb = lerp(lalb, C(0.07, 0.05, 0.035), pm * 0.45)
    lhgt = (lhgt - pm * 0.1 - wet * 0.03).astype(F32)
    ll = Layer(lane)
    for _ in range(110):
        x, y = rng.uniform(-29.8, 29.8), rng.uniform(0.1, 19.9)
        leaf(ll, x, y, rng.uniform(0.6, 1.1), rng.uniform(0, 6.28), 0.32, 4)
    llm = ll.read() * (1 - wear * 0.6)
    lalb, lhgt = apply_object(lane, lalb, lhgt, llm, llm * 0.03, lerp(C(0.15, 0.09, 0.04), C(0.27, 0.18, 0.08),
                                                                      n.fbm(LX, LY, 0.5, 2, offset=962)), shadow=0.35, shadow_blur=0.1)
    lcav = cavity(lhgt, lane, 0.4, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.3), 0.1)
    lsmooth = 0.15 + wet * 0.35

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """The edge of Artemis' forest: a low bank of earth and roots, and old trunks rising out of frame behind it,
    staggered in depth, their roots spreading into the bank."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    folder = os.path.join(OUT, NAME)
    n = Noise(63)
    rng = np.random.default_rng(63)
    be = BackEdge(NAME)
    bark = be.material(Material('Bark', tex_h=20.0, self_light=0.35))
    bank = be.material(Material('Bank', tex_h=4.0, self_light=0.35))
    f = lambda x, h, sc, o: n.fbm(np.array([[x]], F32), np.array([[h]], F32), sc, 4, offset=o)[0, 0]
    be.relief(bank, -75, 75, lambda x: 34.8 + 0.8 * f(x, 0, 6, 51), lambda x: 0.8 + 1.4 * f(x, 0, 5, 52),
              lambda x, h: 0.6 * f(x, h, 1.0, 53) + h * 0.9, step=0.5, h_step=0.3, back=5.0)
    x = -73.0
    while x < 75:
        y = 36.2 + rng.uniform(0, 3.5)
        r = rng.uniform(0.9, 1.8)
        seed = rng.uniform(0, 100)
        wob = lambda t, h, s_=seed: 0.85 + 0.3 * n.value(np.array([[math.cos(t * 3) + s_]], F32), np.array([[h * 0.3 + s_]], F32), 1.0, 54)[0, 0]
        be.lathe(bark, x, y, [(0.0, r * 2.1), (0.4, r * 1.5), (1.2, r * 1.12), (3.0, r), (20.0, r * 0.82)], segments=16, wobble=wob)
        x += rng.uniform(5.5, 11.0)
    be.save(folder)
    c = bark.elev_canvas()
    ridges = n.value(c.X * 4.0, c.Y * 0.25, 1.0, 61) * 0.6 + n.fbm(c.X, c.Y, 0.6, 3, offset=62) * 0.4
    alb = lerp(C(0.09, 0.075, 0.06), C(0.28, 0.24, 0.18), ridges)
    moss = smoothstep(0.58, 0.72, n.fbm(c.X, c.Y, 1.5, 4, offset=63)) * (1 - smoothstep(1.0, 6.0, c.Y))
    alb = lerp(alb, C(0.08, 0.12, 0.04), moss * 0.7)
    save_material(folder, bark, alb.astype(F32), (ridges * 0.12).astype(F32), np.full(c.X.shape, 0.12, F32))
    c = bank.elev_canvas()
    alb, hgt, sm = bp.rock(c, n, C(0.07, 0.05, 0.035), C(0.17, 0.13, 0.09), strata=0.0, offset=64)
    green = smoothstep(0.45, 0.65, n.fbm(c.X, c.Y, 0.9, 4, offset=65))
    alb = lerp(alb, lerp(C(0.05, 0.08, 0.03), C(0.13, 0.18, 0.06), n.fbm(c.X, c.Y, 0.2, 3, offset=66)), green * 0.8)
    ta, th, ts = bp.tops(bank, n, C(0.06, 0.08, 0.03), C(0.14, 0.16, 0.07), offset=67)
    save_material(folder, bank, alb, hgt, sm, talb=ta, thgt=th, tsmooth=ts)
