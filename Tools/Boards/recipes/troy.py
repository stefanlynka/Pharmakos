"""The plain before Troy: wind-rippled sand scored by chariot wheels, dark old blood soaked in, and fallen blocks along
the foot of the great walls. The sand is two generated painted tiles (Tools/Boards/gen, fal.ai FLUX 2 Pro) mixed at
different scales and angles. The walls are 3D (see back_edge): a battered base of huge irregular limestone blocks
with Troy VI's shallow sawtooth offsets, lit only at its foot and sinking into darkness above."""
from boardkit import *  # noqa

NAME = 'Troy'

# The wall's foot runs along y = WALL_Y on the board, x -WALL_HALF..WALL_HALF (heights are out of the board).
WALL_Y = 34.0
WALL_HALF = 75.0
GEN = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'gen')
BASE_H = 3.4          # the battered limestone base; its top is a ledge, the mudbrick above stands back from it
BATTER = 0.35         # the base leans back this much per unit of height
LEDGE = 0.7
WALL_TOP = 16.0       # well out of frame
OFFSET_EVERY = 8.5    # Troy VI's sawtooth plan: each stretch of face runs slightly forward, then steps back
OFFSET_DEPTH = 0.3
OFFSET_X0 = -2.0


def saw(x):
    """How far forward (-y) the face stands at x: 0 just after an offset, OFFSET_DEPTH just before the next."""
    return -OFFSET_DEPTH * (((x - OFFSET_X0) % OFFSET_EVERY) / OFFSET_EVERY)


def fallen_blocks():
    """Blocks fallen from the wall, lying in the sand at its foot: (x, y, w, d, h, yaw, tilt_x, tilt_y). Kept clear of
    the enemy portrait and its offerings. Shared by the floor (contact shadows) and the back edge (the blocks)."""
    rng = np.random.default_rng(1871)
    out = []
    tries = 0
    while len(out) < 9 and tries < 500:
        tries += 1
        x = rng.uniform(-60, 58)
        if -16 < x < 35:
            continue
        y = WALL_Y - rng.uniform(0.7, 2.6)
        if any(math.hypot(x - o[0], y - o[1]) < 4.0 for o in out):
            continue
        w, d = rng.uniform(1.3, 2.6), rng.uniform(1.0, 1.6)
        out.append((x, y, w, d, rng.uniform(0.7, 1.2), rng.uniform(-0.5, 0.5), rng.uniform(-0.25, 0.25),
                    rng.uniform(-0.2, 0.2)))
    return out


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(71)
    rng = np.random.default_rng(71)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 12, ad)

    # sand: generated painted tiles (Tools/Boards/gen, made with fal.ai FLUX 2 Pro), mixed so no repeat reads:
    # the clean tile B at two scales and angles, with patches of tile A's trodden tracks; then the board's own
    # large-scale tone, ruts, old blood and the shadow at the wall's foot on top
    sand_b = make_tileable(load_rgb(os.path.join(GEN, 'Troy_SandTile_B.png')))
    sand_a = make_tileable(load_rgb(os.path.join(GEN, 'Troy_SandTile_A.png')))
    s1 = sample_tiled(sand_b, X, Y, 6.0, 0.3)
    s2 = sample_tiled(sand_b, X + 31.0, Y + 17.0, 8.5, 2.2)
    s3 = sample_tiled(sand_a, X - 13.0, Y + 7.0, 8.0, 1.1)
    m12 = smoothstep(0.35, 0.65, n.fbm(X, Y, 10, 3, offset=11))
    m3 = smoothstep(0.6, 0.78, n.fbm(X, Y, 14, 3, offset=12)) * smoothstep(2.0, 8.0, ad)     # tracks off the arena
    sand = lerp(lerp(s1, s2, m12), s3, m3 * 0.7)
    W3 = np.array((0.2126, 0.7152, 0.0722), F32)
    slum = sand @ W3
    sand = sand * (0.175 / float(slum.mean()))                  # painted dark; GROUND_GAIN and the grade lift it
    big = n.fbm(X, Y, 16, 4, offset=1)
    earth = sand * (0.8 + 0.4 * big)[..., None]
    alb = earth.copy()
    hgt = (big * 0.3 + blurf((slum - slum.mean()).astype(F32), 0.03, g) * 0.08).astype(F32)

    # chariot ruts sweeping across the field, kept off the arena and soft
    ruts = Layer(g)
    for _ in range(7):
        cx, cy = rng.uniform(-60, 60), rng.uniform(-30, 70)
        R0 = rng.uniform(30, 80)
        a0 = rng.uniform(0, 6.28)
        span = rng.uniform(0.4, 0.9)
        for off in (0, 1.6):
            pts = [(cx + math.cos(a0 + t * span) * (R0 + off), cy + math.sin(a0 + t * span) * (R0 + off)) for t in np.linspace(0, 1, 120)]
            ruts.line(pts, 0.55)
    rut = blurf(ruts.read() * smoothstep(1, 6, ad), 0.4, g)
    alb = alb * (1 - rut * 0.18)[..., None]
    hgt = (hgt - rut * 0.06).astype(F32)

    # old blood where men fell: few, dark, matte, soaked into the sand
    blood = Layer(g)
    for x, y in scatter_points(rng, 10, (-66, 66, -23, 33), margin_keep(4.0), 9.0):
        blob(blood, rng, x, y, rng.uniform(0.7, 1.6), 16, 0.45)
    bm = blurf(blood.read(), 0.35, g) * (0.5 + 0.5 * n.fbm(X, Y, 0.6, 3, offset=4))
    alb = lerp(alb, alb * C(0.55, 0.35, 0.3), bm * 0.8)

    # the wall's foot: a narrow contact shadow (the foot of the wall catches the last of the light), fallen blocks'
    # shadows, and small stone between them
    wshadow = smoothstep(WALL_Y - 1.4, WALL_Y, Y)
    alb = alb * (1 - wshadow[..., None] * 0.45)
    fb = Layer(g)
    for x, y, w, d, h, yaw, tx, ty in fallen_blocks():
        fb.ellipse(x, y + 0.25, w * 0.62, d * 0.62, angle=yaw)
    fbs = blurf(fb.read(), 0.45, g)
    alb = alb * (1 - fbs[..., None] * 0.55)
    rubble = [(x, y + rng.normal(0, 0.8), rng.uniform(0.3, 0.8)) for x, y in scatter_points(rng, 30, (-70, 70, WALL_Y - 2.2, WALL_Y + 0.3), lambda x, y: not (-14 < x < 33), 2.5)]
    rbm, rbh, rbc = rocks(g, n, rng, rubble, base=C(0.12, 0.105, 0.08), light=C(0.26, 0.23, 0.18))
    alb, hgt = apply_object(g, alb, hgt, rbm, rbh * 0.5, rbc, shadow=0.5, shadow_blur=0.15)

    # fade toward the screen edges so the board sinks into darkness beyond the light
    fade = smoothstep(30.0, 62.0, np.abs(X)) * 0.55 + smoothstep(-6.0, -22.0, Y) * 0.4
    alb = alb * (1 - np.clip(fade, 0, 0.75))[..., None]
    cav = cavity(hgt, g, 0.5, 2.0)
    alb = alb * (1 - cav[..., None] * 0.3)
    smooth = np.full(X.shape, 0.1, F32)

    # lanes: the same sand, packed flat by the fighting: softer (blended toward its own blur) and calmer
    LX, LY = lane.WX, lane.WY
    l1 = sample_tiled(sand_b, LX, LY, 6.0, 0.3)
    lalb = lerp(l1, np.broadcast_to(l1.reshape(-1, 3).mean(0), l1.shape), 0.35)
    llum = lalb @ W3
    lalb = lalb * (0.2 / float(llum.mean()))
    lalb = lalb * (0.9 + 0.2 * n.fbm(LX, LY, 7, 3, offset=970))[..., None]
    lhgt = (blurf((llum - llum.mean()).astype(F32), 0.03, lane) * 0.05).astype(F32)
    lsmooth = np.full(LX.shape, 0.1, F32)

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def _wall_layout(rng):
    """Courses of the battered base, bottom to top: (h0, h1, joints, slants). Joints are the x of each vertical joint at
    mid-course; slants tilt them (irregular ashlar: trapezoid blocks). The sawtooth offsets are always joints."""
    courses = []
    h = 0.0
    while h < BASE_H - 1e-3:
        ch = float(rng.uniform(0.95, 1.5))
        if BASE_H - (h + ch) < 0.7:
            ch = BASE_H - h
        courses.append((h, h + ch))
        h += ch
    offsets = [OFFSET_X0 + OFFSET_EVERY * k for k in range(-12, 13)]
    offsets = [o for o in offsets if -WALL_HALF - 6 < o < WALL_HALF + 6]
    layout = []
    for h0, h1 in courses:
        joints = []
        x = -WALL_HALF - 6 + float(rng.uniform(0, 2))
        while x < WALL_HALF + 6:
            joints.append(x)
            x += float(rng.uniform(2.0, 5.5))
        joints = [j for j in joints if min(abs(j - o) for o in offsets) > 0.9]
        sl = [float(rng.uniform(-0.3, 0.3)) for _ in joints] + [0.0] * len(offsets)
        order = np.argsort(joints + offsets)
        layout.append((h0, h1, np.array(joints + offsets, F32)[order], np.array(sl, F32)[order]))
    return layout


def back_edge():
    """The walls of Troy (after the Wall of Troy card: huge irregular ashlar, a battered base, no battlements), as real
    geometry so the scene lights and shadows it like the floor. The base's blocks are carved into the mesh (joints,
    bulging faces, blocks standing proud) from the same height map that makes the normal map; the stone surface is the
    generated limestone tile (Tools/Boards/gen/Troy_StoneTile.png) sampled at a different spot per block. Fallen blocks
    lie at the foot. Light: only the foot is lit (emission falling off with height and toward the sides), the mudbrick
    above sinks into darkness, so the wall is felt more than seen."""
    from backedge import BackEdge, Material, save_material, HALF, PPU
    from bepaint import age
    folder = os.path.join(OUT, NAME)
    rng = np.random.default_rng(717)
    n = Noise(717)
    be = BackEdge(NAME)
    wall = be.material(Material('Wall', tex_h=WALL_TOP, self_light=0.0))
    W3 = np.array((0.2126, 0.7152, 0.0722), F32)

    stone = make_tileable(load_rgb(os.path.join(GEN, 'Troy_StoneTile.png')))
    stone = stone * C(1.0, 0.92, 0.78)                                  # warm it toward the sand under the same light
    stone = stone * (0.34 / float((stone @ W3).mean()))
    sand = make_tileable(load_rgb(os.path.join(GEN, 'Troy_SandTile_B.png')))
    sand_col = sand.reshape(-1, 3).mean(0)
    sand_col = sand_col * (0.34 / float(sand_col @ W3))

    # --- height map of the base (D: how far each point sits back from the face, in units) and per-block colour
    el = wall.elev_canvas()
    X, H = el.X, el.Y
    edge = np.full(X.shape, 9.0, F32)
    bid = np.full(X.shape, -1, np.int32)
    layout = _wall_layout(rng)
    nblocks = sum(len(c[2]) + 1 for c in layout)
    bval = rng.uniform(0.8, 1.12, nblocks).astype(F32)
    bwarm = rng.uniform(-0.04, 0.05, nblocks).astype(F32)
    bbulge = rng.uniform(0.03, 0.12, nblocks).astype(F32)
    bproud = rng.uniform(-0.07, 0.06, nblocks).astype(F32)
    box_, boy = rng.uniform(0, 60, nblocks).astype(F32), rng.uniform(0, 60, nblocks).astype(F32)
    # bed joints step up and down from block to block (each block of a course has its own top), as in Troy VI's
    # masonry; the steps fall on the course's own vertical joints
    xs = X[0]
    bnd = [np.zeros_like(xs)]
    for ci, (h0, h1, joints, slants) in enumerate(layout):
        if ci == len(layout) - 1:
            bnd.append(np.full_like(xs, BASE_H))
        else:
            idx = np.clip(np.searchsorted(joints, xs), 1, len(joints) - 1)
            toff = rng.uniform(-0.2, 0.2, len(joints) + 1).astype(F32)
            bnd.append(np.maximum(h1 + toff[idx], bnd[-1] + 0.45))
    starts = np.cumsum([0] + [len(c[2]) + 1 for c in layout])
    for r in np.where(H[:, 0] <= BASE_H + 1e-3)[0]:
        hr = float(H[r, 0])
        xr = X[r]
        for ci, (h0, h1, joints, slants) in enumerate(layout):
            lo, hi = bnd[ci], bnd[ci + 1]
            top_course = ci == len(layout) - 1
            cols = (hr >= lo) & ((hr < hi) | top_course)
            if not cols.any():
                continue
            t = (hr - (h0 + h1) / 2) / (h1 - h0)
            jx = joints + slants * t
            xc = xr[cols]
            idx = np.clip(np.searchsorted(jx, xc), 1, len(jx) - 1)
            dl = xc - jx[idx - 1]
            dr = jx[idx] - xc
            dh = hr - lo[cols] if top_course else np.minimum(hr - lo[cols], hi[cols] - hr)
            edge[r, cols] = np.minimum(np.minimum(dl, dr), dh)
            bid[r, cols] = starts[ci] + idx
    inbase = bid >= 0
    b = np.where(inbase, bid, 0)
    joint = 1 - smoothstep(0.0, 0.13, edge)
    chip = smoothstep(0.6, 0.78, n.fbm(X, H, 0.35, 3, offset=3)) * (1 - smoothstep(0.04, 0.4, edge))
    D = np.where(inbase, 0.15 * joint + 0.07 * chip - bbulge[b] * smoothstep(0.05, 0.9, edge)
                 + bproud[b] * smoothstep(0.0, 0.12, edge), 0.0).astype(F32)

    # --- albedo: limestone per block; mudbrick above
    flip = np.where(b % 2 == 0, 1.0, -1.0).astype(F32)
    salb = sample_tiled(stone, X * flip + box_[b], H + boy[b], 3.2)
    salb = salb * bval[b][..., None]
    salb = salb * (1 + bwarm[b][..., None] * np.array((1.0, 0.2, -1.0), F32))
    mud = sample_tiled(stone, X + 11.0, H * 1.7, 2.6) * C(0.62, 0.5, 0.38)
    course = np.floor((H - BASE_H) / 0.42)
    brick = (np.abs(((H - BASE_H) / 0.42) % 1 - 0.5) > 0.46) | (np.abs(((X + 0.6 * course) / 1.1) % 1 - 0.5) > 0.47)
    mud = mud * (1 - brick[..., None] * 0.35) * (0.8 + 0.4 * n.fbm(X, H, 3, 3, offset=9))[..., None]
    alb = np.where(inbase[..., None], salb, mud).astype(F32)
    alb = alb * (1 - np.clip(D * 4.5, 0, 0.75))[..., None]          # joints and chips in shadow
    alb = age(n, X, H, alb, foot_dark=0.0, streaks=0.3)
    dust = (1 - smoothstep(0.0, 0.75, H)) * (0.55 + 0.35 * n.fbm(X, H, 0.8, 3, offset=5))
    alb = lerp(alb, np.broadcast_to(sand_col, alb.shape), dust * 0.85)
    ldust = smoothstep(BASE_H - 0.25, BASE_H, H) * inbase * 0.5       # dust collected along the ledge's lip
    alb = lerp(alb, np.broadcast_to(sand_col, alb.shape), ldust)

    # --- light: the spotlights already fall off across the wall; on top of that only the foot glows a little (bounce
    # from the sand) and the stone darkens upward, so the wall sinks into darkness above its lowest courses
    lit = 1 - smoothstep(0.4, 5.8, H)
    side = (1 - smoothstep(8.0, 55.0, np.abs(X))) * (0.55 + 0.45 * n.fbm(X, H * 0.2, 9, 3, offset=31))
    em = alb * (lit * side * 0.7)[..., None]
    alb = alb * ((0.35 + 0.65 * lit) * (0.5 + 0.5 * side))[..., None]

    grain = (stone @ W3)
    grain3 = np.repeat(grain[..., None], 3, 2)
    grain_h = sample_tiled(grain3, X * flip + box_[b], H + boy[b], 3.2)[..., 0]
    hgt = (-D + (grain_h - float(grain.mean())) * 0.05).astype(F32)
    smooth = np.full(X.shape, 0.12, F32)

    # tops band: the ledge (dusty stone) as seen from above
    tp = wall.top_canvas()
    talb = lerp(sample_tiled(stone, tp.X, tp.Y, 3.2), np.broadcast_to(sand_col, (tp.h, tp.w, 3)), 0.45) * 0.35
    save_material(folder, wall, alb, hgt, smooth, emission=em, talb=talb.astype(F32))

    # --- geometry. D sampled (softened) for the mesh; the normal map carries the fine detail
    Dm = blurf(D, 0.06, el)

    def dlook(x, h):
        ix = int(min(max((x + HALF) * PPU, 0), el.w - 1))
        iy = int(min(max((WALL_TOP - h) * PPU, 0), el.h - 1))
        return float(Dm[iy, ix])

    def base_depth(x, h):
        return h * BATTER + saw(x) + dlook(x, min(h, BASE_H - 0.01))

    step = 0.12
    be.relief(wall, -WALL_HALF, WALL_HALF, lambda x: WALL_Y, lambda x: BASE_H, base_depth, step=step, h_step=0.1)
    back_y = WALL_Y + BASE_H * BATTER + LEDGE
    for x in np.arange(-WALL_HALF, WALL_HALF - 1e-6, step):
        x1 = x + step
        f0 = (x, WALL_Y + base_depth(x, BASE_H), BASE_H)
        f1 = (x1, WALL_Y + base_depth(x1, BASE_H), BASE_H)
        be._quad(wall, f0, f1, (x1, back_y + saw(x1), BASE_H), (x, back_y + saw(x), BASE_H), (0, 0, 1), uv_mode='top')
    ux = np.arange(-WALL_HALF, WALL_HALF + 1, 0.5, dtype=F32)[None, :]
    upper = n.fbm(ux, np.zeros_like(ux), 2.0, 2, offset=21)[0]

    def upper_depth(x, h):
        i = int(min(max((x + WALL_HALF) / 0.5, 0), len(upper) - 1))
        return saw(x) + 0.04 * (h - BASE_H) + 0.08 * float(upper[i]) * smoothstep(0.0, 1.0, h - BASE_H)

    be.relief(wall, -WALL_HALF, WALL_HALF, lambda x: back_y, lambda x: WALL_TOP, upper_depth, step=0.5, h_step=0.5,
              h0=BASE_H)

    # --- fallen blocks lying in the sand
    fm = be.material(Material('Fallen', tex_h=2.0, self_light=0.1))
    for x, y, w, d, h, yaw, tx, ty in fallen_blocks():
        _block(be, fm, x, y, w, d, h, yaw, tx, ty)
    fe = fm.elev_canvas()
    fx, fh = fe.X, fe.Y
    falb = sample_tiled(stone, fx, fh, 3.2) * 0.55
    fdust = (1 - smoothstep(0.0, 0.45, fh)) * 0.7
    falb = lerp(falb, np.broadcast_to(sand_col, falb.shape), fdust)
    fhgt = (sample_tiled(grain3, fx, fh, 3.2)[..., 0] - float(grain.mean())) * 0.05
    ft = fm.top_canvas()
    ftalb = lerp(sample_tiled(stone, ft.X, ft.Y, 3.2), np.broadcast_to(sand_col, (ft.h, ft.w, 3)), 0.5) * 0.4
    save_material(folder, fm, falb.astype(F32), fhgt.astype(F32), np.full(fx.shape, 0.12, F32), talb=ftalb.astype(F32))

    be.save(folder)
    for old in ('Lime', 'Mud', 'Cap', 'Walk', 'Backdrop'):
        for suffix in ('_Albedo.jpg', '_Normal.jpg', '_Mask.png', '_Emission.jpg'):
            f = os.path.join(folder, 'BE_' + old + suffix)
            if os.path.exists(f):
                os.remove(f)


def _block(be, mat, cx, cy, w, d, h, yaw, tx, ty, sink=0.35):
    """A squared block lying on the board, turned by yaw and tipped (tx, ty: height change per unit along its own
    x and y), sunk a little into the sand."""
    c, s_ = math.cos(yaw), math.sin(yaw)

    def P(lx, ly, top):
        hh = (h - sink + lx * tx + ly * ty) if top else -sink
        return (cx + lx * c - ly * s_, cy + lx * s_ + ly * c, hh)
    hw, hd = w / 2, d / 2
    be._quad(mat, P(-hw, -hd, 1), P(hw, -hd, 1), P(hw, hd, 1), P(-hw, hd, 1), (0, 0, 1))
    be._quad(mat, P(-hw, -hd, 0), P(hw, -hd, 0), P(hw, -hd, 1), P(-hw, -hd, 1), (s_, -c, 0))
    be._quad(mat, P(-hw, hd, 0), P(hw, hd, 0), P(hw, hd, 1), P(-hw, hd, 1), (-s_, c, 0))
    be._quad(mat, P(-hw, -hd, 0), P(-hw, hd, 0), P(-hw, hd, 1), P(-hw, -hd, 1), (-c, -s_, 0))
    be._quad(mat, P(hw, -hd, 0), P(hw, hd, 0), P(hw, hd, 1), P(hw, -hd, 1), (c, s_, 0))
