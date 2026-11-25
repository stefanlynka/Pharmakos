"""The plain before Troy: trampled dusty earth churned by hooves and scored by chariot wheels, the dead's gear left
where it fell (painted round shields, broken spears, arrows, a shattered chariot wheel), dark old blood, and the
foot of the great walls along the far edge (a 3D wall: see back_edge). Lanes: packed earth with ruts."""
from boardkit import *  # noqa

NAME = 'Troy'

# The wall (keep in sync with Assets/Scripts/Editor/BoardBackEdgeBuilder.cs): its foot runs along y = WALL_Y on the board,
# x -WALL_HALF..WALL_HALF; heights are out of the board toward the camera. The elevation texture covers heights 0..WALL_H.
WALL_Y = 34.0
WALL_HALF = 75.0
WALL_H = 300 / (4096 / 150.0)   # 300 texture rows of height at the same texel size as across (about 11 units)
BASE_H = 2.4          # battered limestone base; mudbrick above
TOWER_X = 44.0
TOWER_W = 9.0


def shield(layer, paint, rng, x, y, r):
    """Round aspis: bronze rim (layer) and a painted face (paint layer: 128 = terracotta ground, 255 = black-figure
    motif; later shapes overwrite earlier ones)."""
    layer.ellipse(x, y, r, r, 255, n=48)
    paint.ellipse(x, y, r * 0.84, r * 0.84, 128, n=48)
    k = rng.integers(0, 5)
    rot = rng.uniform(0, 6.28)
    if k == 0:      # rings and a boss
        paint.ellipse(x, y, r * 0.62, r * 0.62, 255, n=40)
        paint.ellipse(x, y, r * 0.5, r * 0.5, 128, n=40)
        paint.ellipse(x, y, r * 0.16, r * 0.16, 255)
    elif k == 1:    # a wheel of eight spokes
        for i in range(8):
            a = rot + i * math.pi / 4
            paint.line([(x, y), (x + math.cos(a) * r * 0.62, y + math.sin(a) * r * 0.62)], r * 0.08, 255)
        paint.ellipse(x, y, r * 0.17, r * 0.17, 255)
    elif k == 2:    # a crescent
        paint.ellipse(x, y, r * 0.52, r * 0.52, 255, n=40)
        paint.ellipse(x + math.cos(rot) * r * 0.2, y + math.sin(rot) * r * 0.2, r * 0.46, r * 0.46, 128, n=40)
    elif k == 3:    # a running spiral of snakes
        for j in range(3):
            pts = []
            for t in np.linspace(0, 1, 30):
                a = rot + j * 2.094 + t * 3.0
                rr = r * (0.12 + 0.55 * t)
                pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
            paint.line(pts, r * 0.1, 255)
    else:           # a bull's head: horns and a head
        paint.ellipse(x, y - r * 0.05, r * 0.22, r * 0.32, 255)
        for side in (-1, 1):
            pts = [(x + side * r * (0.15 + 0.4 * t), y + r * (0.2 + 0.35 * t * t)) for t in np.linspace(0, 1, 10)]
            paint.line(pts, r * 0.08, 255)


def spear(layer, tip, x, y, length, angle, broken=False):
    ca, sa = math.cos(angle), math.sin(angle)
    a = (x - ca * length / 2, y - sa * length / 2)
    b = (x + ca * length / 2, y + sa * length / 2)
    layer.line([a, b], 0.12)
    if not broken:
        px, py = -sa, ca
        tip.poly([(b[0] + ca * 0.9, b[1] + sa * 0.9), (b[0] + px * 0.18, b[1] + py * 0.18),
                  (b[0] - ca * 0.2, b[1] - sa * 0.2), (b[0] - px * 0.18, b[1] - py * 0.18)])


def wheel(layer, x, y, r, angle, missing):
    for i in range(48):
        if i in missing:
            continue
        a0, a1 = i / 48 * 2 * math.pi, (i + 1) / 48 * 2 * math.pi
        layer.line([(x + math.cos(a0) * r, y + math.sin(a0) * r), (x + math.cos(a1) * r, y + math.sin(a1) * r)], r * 0.1)
    for i in range(4):
        a = angle + i * math.pi / 2
        if i == 3:
            continue
        layer.line([(x, y), (x + math.cos(a) * r * 0.95, y + math.sin(a) * r * 0.95)], r * 0.07)
    layer.ellipse(x, y, r * 0.15, r * 0.15)


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(71)
    rng = np.random.default_rng(71)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 12, ad)

    big = n.fbm(X, Y, 14, 4, offset=1)
    grain = n.fbm(X, Y, 0.45, 5, offset=2)
    churn = n.fbm(X, Y, 1.4, 4, offset=3)
    earth = lerp(C(0.15, 0.115, 0.075), C(0.32, 0.255, 0.17), np.clip(big * 0.55 + churn * 0.3 + grain * 0.15, 0, 1))
    alb = earth + ((brush(n, X, Y) - 0.5) * 0.035)[..., None]
    hgt = (big * 0.35 + churn * 0.12 + grain * 0.05).astype(F32)

    # hoof churn and wheel ruts sweeping across the field
    ruts = Layer(g)
    for _ in range(9):
        cx, cy = rng.uniform(-60, 60), rng.uniform(-30, 70)
        R0 = rng.uniform(30, 80)
        a0 = rng.uniform(0, 6.28)
        span = rng.uniform(0.4, 0.9)
        for off in (0, 1.6):
            pts = [(cx + math.cos(a0 + t * span) * (R0 + off), cy + math.sin(a0 + t * span) * (R0 + off)) for t in np.linspace(0, 1, 120)]
            ruts.line(pts, 0.22)
    rm_ = ruts.read() * smoothstep(1, 5, ad)
    rut = blurf(rm_, 0.1, g)
    alb = lerp(alb, C(0.11, 0.085, 0.06), rut * 0.5)
    hgt = (hgt - rut * 0.15).astype(F32)
    hp = Layer(g)
    for x, y in dense_points(rng, 2500):
        hp.ellipse(x, y, 0.28, 0.32, 255, rng.uniform(0, 6.28))
    hpm = blurf(hp.read(), 0.05, g) * margin * 0.6
    alb = lerp(alb, C(0.12, 0.09, 0.06), hpm * 0.35)
    hgt = (hgt - hpm * 0.06).astype(F32)

    # blood, old and dark, where men fell
    blood = Layer(g)
    for x, y in scatter_points(rng, 30, (-66, 66, -23, 43), margin_keep(2.0), 5.0):
        blob(blood, rng, x, y, rng.uniform(0.6, 1.8), 16, 0.45)
    bm = blurf(blood.read(), 0.2, g) * (0.6 + 0.4 * n.fbm(X, Y, 0.6, 3, offset=4))
    alb = lerp(alb, C(0.12, 0.03, 0.025), bm * 0.75)

    # the walls of Troy are a 3D back edge now (built by Tools -> Boards -> Build Back Edges from back_edge()):
    # here only the ground at their foot, darkened by their shadow and strewn with fallen stone
    foot = WALL_Y - 1.6 * smoothstep(-0.5, 0.5, -np.abs(np.abs(X) - TOWER_X) + TOWER_W / 2)   # towers stand forward
    wshadow = smoothstep(foot - 3.0, foot, Y)
    alb = alb * (1 - wshadow[..., None] * 0.6)
    rubble = [(x, y, rng.uniform(0.12, 0.45)) for x, y in scatter_points(rng, 140, (-70, 70, WALL_Y - 3, WALL_Y), lambda x, y: True, 0.5)]
    rbm, rbh, rbc = rocks(g, n, rng, rubble, base=C(0.17, 0.15, 0.11), light=C(0.38, 0.34, 0.26))
    alb, hgt = apply_object(g, alb, hgt, rbm, rbh * 0.5, rbc, shadow=0.5, shadow_blur=0.15)

    # the fallen's gear
    rim = Layer(g)
    pnt = Layer(g, ss=1)
    for x, y in scatter_points(rng, 4, (-64, 64, -22, 38), margin_keep(8.0), min_gap=25):
        shield(rim, pnt, rng, x, y, rng.uniform(1.5, 1.9))
    sr = rim.read()
    pv = blurf(pnt.read(), 0.02, g) * sr
    sf = smoothstep(0.3, 0.45, pv)
    smot = smoothstep(0.7, 0.9, pv)
    sdome = np.clip(blurf(sr, 0.5, g) * 1.4, 0, 1) * sr
    gy, gx = np.gradient(blurf(sdome, 0.08, g))
    lit = np.clip(0.5 + (-gx + gy) * g.ppu * 0.8, 0, 1)
    bronze = lerp(C(0.22, 0.15, 0.07), C(0.55, 0.4, 0.2), lit)
    bronze = lerp(bronze, C(0.17, 0.25, 0.2), smoothstep(0.6, 0.8, n.fbm(X, Y, 0.5, 3, offset=7)) * 0.5)   # verdigris
    paint_ = lerp(C(0.4, 0.17, 0.08), C(0.5, 0.24, 0.12), lit)                                           # terracotta face
    scol = lerp(bronze, paint_, sf * (1 - smot))
    scol = lerp(scol, C(0.05, 0.04, 0.035), smot)                                                        # black-figure motif
    scol = scol * (0.75 + 0.35 * n.fbm(X, Y, 0.3, 3, offset=8))[..., None]
    scol = lerp(desaturate(scol, 0.5) * 0.6, earth, 0.35)                                          # dulled, dust-caked
    buried = smoothstep(0.4, 0.7, n.fbm(X, Y, 1.0, 3, offset=9))                                  # half under the dust
    sr = sr * (1 - buried * 0.8)
    alb, hgt = apply_object(g, alb, hgt, sr, sdome * 0.2 * sr, scol, shadow=0.35, shadow_blur=0.3)

    wood = Layer(g)
    tips = Layer(g)
    for x, y in scatter_points(rng, 26, (-66, 66, -23, 39), margin_keep(2.5), 3.0):
        spear(wood, tips, x, y, rng.uniform(3.5, 7.0) * (0.5 if rng.random() < 0.4 else 1.0), rng.uniform(0, 6.28),
              broken=rng.random() < 0.35)
    for x, y in scatter_points(rng, 60, (-66, 66, -23, 39), margin_keep(1.5), 1.5):
        a = rng.uniform(0, 6.28)
        L = rng.uniform(1.6, 2.4)
        wood.line([(x, y), (x + math.cos(a) * L, y + math.sin(a) * L)], 0.06)
    wheel(wood, -55, 8, 2.6, 0.4, set(range(30, 41)))
    wheel(wood, 50, 26, 2.4, 1.1, set(range(5, 13)))
    wm = wood.read()
    tm = tips.read()
    wcol = lerp(C(0.16, 0.11, 0.07), C(0.33, 0.24, 0.15), np.clip(blurf(wm, 0.04, g) * 1.4, 0, 1) * (0.6 + 0.5 * grain))
    alb, hgt = apply_object(g, alb, hgt, wm, wm * 0.12, wcol, shadow=0.55, shadow_blur=0.12)
    alb, hgt = apply_object(g, alb, hgt, tm, tm * 0.08, lerp(C(0.3, 0.2, 0.1), C(0.5, 0.36, 0.18), grain), shadow=0.45)

    cav = cavity(hgt, g, 0.5, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.4), 0.12)
    smooth = 0.12 + bm * 0.25 + sr * (1 - sf) * 0.3 + tm * 0.3

    # lanes: packed earth with ruts
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, C(0.2, 0.16, 0.11), C(0.3, 0.24, 0.165), offset=970)
    wear = lane_wear(lane)
    lr = Layer(lane)
    for half in (0, 10):
        for k in range(3):
            y0 = half + rng.uniform(2, 8)
            pts = [(x, y0 + math.sin(x * 0.05 + k) * 0.8) for x in np.linspace(-30, 30, 100)]
            lr.line(pts, 0.5)
    lrm = blurf(lr.read(), 0.25, lane) * 0.6
    lalb = lerp(lalb, C(0.16, 0.125, 0.085), lrm * 0.25)
    hp2 = Layer(lane)
    for _ in range(200):
        hp2.ellipse(rng.uniform(-29.5, 29.5), rng.uniform(0.3, 19.7), 0.26, 0.3, 255, rng.uniform(0, 6.28))
    hpm2 = blurf(hp2.read(), 0.04, lane) * 0.5
    lalb = lerp(lalb, C(0.15, 0.115, 0.08), hpm2 * 0.4)
    lhgt = (lhgt - lrm * 0.1 - hpm2 * 0.05).astype(F32)
    lcav = cavity(lhgt, lane, 0.4, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.3), 0.12)
    lsmooth = 0.12 + wear * 0.05

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """The walls of Troy: a battered limestone base with Troy VI's shallow vertical offsets, a timber tie-beam,
    mudbrick above, merlons with pale limestone caps against a walkway in the parapet's shadow, and two towers that
    run out of frame."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    folder = os.path.join(OUT, NAME)
    n = Noise(73)
    be = BackEdge(NAME)
    lime = be.material(Material('Lime', tex_h=3.0, self_light=0.45))
    mud = be.material(Material('Mud', tex_h=9.0, self_light=0.42))
    cap = be.material(Material('Cap', tex_h=1.0, self_light=0.5))
    walk = be.material(Material('Walk', tex_h=1.0, self_light=0.25))

    batter, wall_h, thick = 0.45, 4.6, 2.0
    mw, mg, mh, md = 0.9, 0.65, 1.3, 0.55
    front = WALL_Y + batter * BASE_H

    def merlons(x0, x1, yf, h):
        cnt = max(1, int((x1 - x0 + mg) // (mw + mg)))
        start = x0 + ((x1 - x0) - (cnt * mw + (cnt - 1) * mg)) / 2
        for i in range(cnt):
            mx = start + i * (mw + mg)
            be.box(mud, mx, mx + mw, yf, yf + md, h, h + mh, top_mat=cap)

    xs = [-WALL_HALF, -TOWER_X - TOWER_W / 2, -TOWER_X + TOWER_W / 2, TOWER_X - TOWER_W / 2, TOWER_X + TOWER_W / 2, WALL_HALF]
    for k in range(len(xs) - 1):
        x0, x1 = xs[k], xs[k + 1]
        if k in (1, 3):    # towers
            ty = WALL_Y - 1.6
            tf = ty + batter * BASE_H
            be.box(lime, x0, x1, ty, tf + thick + 1.6, 0, BASE_H, batter=batter, top=False)
            be.box(mud, x0, x1, tf, tf + thick + 1.6, BASE_H, 7.5, top_mat=walk)
            merlons(x0 + 0.2, x1 - 0.2, tf, 7.5)
        else:
            be.box(lime, x0, x1, WALL_Y, front + thick, 0, BASE_H, batter=batter, sides=False, top=False)
            be.box(mud, x0, x1, front, front + thick, BASE_H, wall_h, sides=False, top_mat=walk)
            merlons(x0 + 0.4, x1 - 0.4, front, wall_h)
    be.save(folder)

    # limestone base: ashlar with a seam and a change of tone at each offset (every 6 units)
    c = lime.elev_canvas()
    alb, hgt, sm = bp.masonry(c, n, 1.2, 0.5, C(0.36, 0.33, 0.28), C(0.54, 0.5, 0.43), C(0.12, 0.1, 0.08), offset=3)
    seam = 1 - smoothstep(0.0, 0.18, np.abs((c.X + 200) % 6.0))
    alb = lerp(alb, C(0.08, 0.07, 0.06), seam * 0.8)
    alb = bp.age(n, c.X, c.Y, alb, foot_dark=0.5, foot_h=0.6)
    save_material(folder, lime, desaturate(alb, 0.15), hgt - seam * 0.1, sm)

    # mudbrick with clinging plaster, a timber tie-beam along its foot, grime and soot where Troy burned
    c = mud.elev_canvas()
    X, H = c.X, c.Y
    alb, hgt, sm = bp.masonry(c, n, 0.55, 0.24, C(0.28, 0.2, 0.13), C(0.42, 0.32, 0.21), C(0.14, 0.1, 0.07), gap=0.03, offset=5)
    plaster = smoothstep(0.66, 0.76, n.fbm(X, H, 1.5, 4, offset=6))
    alb = lerp(alb, C(0.5, 0.45, 0.37), plaster * 0.4)
    beam = 1 - smoothstep(0.1, 0.15, np.abs(H - BASE_H - 0.12))
    alb = lerp(alb, lerp(C(0.12, 0.08, 0.05), C(0.22, 0.15, 0.09), n.value(X * 0.15, H * 6, 0.5, 7)), beam)
    soot = smoothstep(0.62, 0.8, n.fbm(X, H, 4, 4, offset=10)) * smoothstep(BASE_H, 7.0, H)
    alb = lerp(alb, C(0.04, 0.035, 0.03), soot * 0.7)
    alb = bp.age(n, X, H, alb, foot_dark=0.0)
    save_material(folder, mud, desaturate(alb, 0.15), hgt - beam * 0.02, sm)

    # caps: pale worn limestone coping on the merlons (seen from above) so the teeth read against the walkway
    ta, th, ts = bp.tops(cap, n, C(0.5, 0.47, 0.41), C(0.66, 0.62, 0.54), offset=11)
    c = cap.elev_canvas()
    save_material(folder, cap, np.broadcast_to(C(0.5, 0.47, 0.41), c.X.shape + (3,)).astype(F32), np.zeros(c.X.shape, F32),
                  np.full(c.X.shape, 0.15, F32), talb=ta, thgt=th, tsmooth=ts)
    # walkway: packed earth behind the parapet, in its shadow
    ta, th, ts = bp.tops(walk, n, C(0.07, 0.055, 0.04), C(0.13, 0.1, 0.07), offset=12)
    c = walk.elev_canvas()
    save_material(folder, walk, np.broadcast_to(C(0.1, 0.08, 0.06), c.X.shape + (3,)).astype(F32), np.zeros(c.X.shape, F32),
                  np.full(c.X.shape, 0.1, F32), talb=ta, thgt=th, tsmooth=ts)
