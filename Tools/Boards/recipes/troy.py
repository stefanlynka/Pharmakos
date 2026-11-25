"""The plain before Troy: trampled dusty earth churned by hooves and scored by chariot wheels, the dead's gear left
where it fell (painted round shields, broken spears, arrows, a shattered chariot wheel), dark old blood, and the
foot of the great walls along the far edge (a 3D wall: see paint_wall). Lanes: packed earth with ruts."""
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

    # the walls of Troy are a 3D back edge now (TroyWall, built by Tools -> Boards -> Build Troy Wall from the textures
    # paint_wall() makes): here only the ground at their foot, darkened by their shadow and strewn with fallen stone
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
    paint_wall()


WALL_TEX = (4096, 384)        # u: x -WALL_HALF..WALL_HALF; rows 84..383: elevation (heights 0..WALL_H); rows 0..83: tops
WALL_ELEV_ROWS = 300
WALL_TOP_DEPTH = 84 / (4096 / (2 * WALL_HALF))   # world depth the top band covers (about 6.15 units)


def paint_wall():
    """Textures for the 3D wall: an elevation of the front faces (battered limestone base with Troy VI's shallow vertical
    offsets, a timber tie-beam, mudbrick above, grime streaks, fire scorch) and, in a band above it, the tops (walkway
    and merlon tops) seen from above."""
    n = Noise(73)
    W = WALL_TEX[0]
    el = Canvas((-WALL_HALF, WALL_HALF, 0.0, WALL_H), (W, WALL_ELEV_ROWS))
    X, H = el.X, el.Y

    # limestone base: big ashlar blocks, a slight shift of tone and a dark seam at each offset (every 9 units)
    seg = np.floor((X + 200) / 6.0)
    seg_id = n.cellhash(seg.astype(np.int64), seg.astype(np.int64) * 7, 1)
    j1, s1, _ = flagstones(n, X, H, 1.2, 0.5, gap=0.04, jitter=0.35, offset=3)
    grain = n.fbm(X, H, 0.35, 5, offset=4)
    lime = lerp(C(0.36, 0.33, 0.28), C(0.52, 0.48, 0.41), np.clip(s1 * 0.4 + grain * 0.4 + seg_id * 0.2, 0, 1))
    lime = lerp(lime, C(0.12, 0.1, 0.08), j1 * 0.7)
    offset_seam = 1 - smoothstep(0.0, 0.18, np.abs(((X + 200) % 6.0) - 0.0))
    lime = lerp(lime, C(0.08, 0.07, 0.06), offset_seam * 0.8)

    # mudbrick above, with old plaster clinging in patches
    j2, s2, _ = flagstones(n, X, H, 0.55, 0.24, gap=0.03, jitter=0.3, offset=5)
    mud = lerp(C(0.3, 0.22, 0.14), C(0.44, 0.34, 0.22), np.clip(s2 * 0.45 + grain * 0.55, 0, 1))
    mud = lerp(mud, C(0.15, 0.11, 0.07), j2 * 0.6)
    plaster = smoothstep(0.66, 0.76, n.fbm(X, H, 1.5, 4, offset=6))
    mud = lerp(mud, C(0.5, 0.45, 0.37) * (0.85 + 0.3 * grain)[..., None], plaster * 0.45)

    upper = smoothstep(BASE_H - 0.05, BASE_H + 0.05, H)
    alb = lerp(lime, mud, upper)
    beam = (1 - smoothstep(0.1, 0.15, np.abs(H - BASE_H - 0.12)))
    wood = lerp(C(0.12, 0.08, 0.05), C(0.22, 0.15, 0.09), n.value(X * 0.15, H * 6, 0.5, 7))
    alb = lerp(alb, wood, beam)

    # weathering: grime running down from the top, damp at the foot, soot where Troy burned
    streak = n.value(X * 2.2, H * 0.2, 1.0, 8) * 0.7 + n.fbm(X, H, 1.2, 3, offset=9) * 0.3
    streak = smoothstep(0.55, 0.85, streak) * smoothstep(0.5, 5.0, H)
    alb = alb * (1 - streak * 0.35)[..., None]
    alb = alb * (1 - (1 - smoothstep(0.0, 0.8, H)) * 0.45)[..., None]
    soot = smoothstep(0.62, 0.8, n.fbm(X, H, 4, 4, offset=10)) * smoothstep(BASE_H, 7.0, H)
    alb = lerp(alb, C(0.04, 0.035, 0.03), soot * 0.75)
    alb = alb + ((brush(n, X, H) - 0.5) * 0.04)[..., None]
    alb = desaturate(alb, 0.15)

    hgt = (s1 * 0.02 - j1 * 0.12) * (1 - upper) + (s2 * 0.015 - j2 * 0.07) * upper - beam * 0.02 + grain * 0.04
    hgt = (hgt - offset_seam * (1 - upper) * 0.1 - plaster * upper * -0.02).astype(F32)
    smooth = (0.12 + (1 - upper) * 0.08 - soot * 0.05).astype(F32)

    # tops: the walkway and merlon tops, packed earth over mudbrick, seen from above
    tp = Canvas((-WALL_HALF, WALL_HALF, 0.0, WALL_TOP_DEPTH), (W, WALL_TEX[1] - WALL_ELEV_ROWS))
    TX, TY = tp.X, tp.Y
    tg = n.fbm(TX, TY, 0.5, 5, offset=11)
    talb = lerp(C(0.3, 0.24, 0.16), C(0.45, 0.37, 0.25), np.clip(tg * 0.7 + n.fbm(TX, TY, 3, 3, offset=12) * 0.3, 0, 1))
    tsoot = smoothstep(0.6, 0.78, n.fbm(TX, TY, 6, 4, offset=10))
    talb = lerp(talb, C(0.05, 0.04, 0.035), tsoot * 0.7)
    thgt = (tg * 0.06).astype(F32)

    full = Canvas((-WALL_HALF, WALL_HALF, 0.0, 1.0), WALL_TEX)     # only used for its size in save_set
    alb_all = np.concatenate([talb, alb], 0)
    nrm_all = np.concatenate([normals(thgt, tp, 1.0), normals(hgt, el, 1.0)], 0)
    sm_all = np.concatenate([np.full(thgt.shape, 0.1, F32), smooth], 0)
    save_set(os.path.join(OUT, NAME), 'Wall', full, alb_all, nrm_all, sm_all, None, normal_scale=1.0,
             mask_size=(WALL_TEX[0] // 4, WALL_TEX[1] // 4))
