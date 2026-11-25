"""The high priest's sanctum: polished black marble veined in red, a blood channel cut round the place of sacrifice
with gutters running off to the dark, a dedication carved in the floor, candle stubs guttering in pools of wax, and
the stains of what was offered. Lanes: slabs of dark basalt, smooth with use."""
from boardkit import *  # noqa
from PIL import ImageFont

NAME = 'Archiereus'

INSCRIPTION = "ΘΥΣΙΑ ΤΟΙΣ ΘΕΟΙΣ · ΑΙΜΑ ΚΑΙ ΟΣΤΑ · "     # sacrifice to the gods · blood and bones ·


def carve_text(layer, text, x0, x1, y, height, flip=False):
    """Writes text repeated along a horizontal run (world units), upright from below (flip turns it 180 degrees)."""
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/timesbd.ttf", int(height * layer.c.ppu * layer.ss))
    except OSError:
        font = ImageFont.load_default()
    from PIL import Image as _I, ImageDraw as _D
    px0, py = layer.P(x0, y)
    px1, _ = layer.P(x1, y)
    w = int(px1 - px0)
    hpx = int(height * layer.c.ppu * layer.ss * 1.5)
    strip = _I.new('L', (w, hpx), 0)
    d = _D.Draw(strip)
    full = text * 20
    tw = d.textlength(text, font=font)
    xx = 0
    while xx < w:
        d.text((xx, 0), text, font=font, fill=255)
        xx += tw
    if flip:
        strip = strip.rotate(180)
    layer.img.paste(strip, (int(px0), int(py - hpx * 0.6)), strip)


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(113)
    rng = np.random.default_rng(113)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 14, ad)

    S = 8.0
    gx, gy = X / S, (Y - 10.64) / S
    cx, cy = np.floor(gx).astype(np.int64), np.floor(gy).astype(np.int64)
    edge = np.minimum(np.minimum(gx - cx, cx + 1 - gx), np.minimum(gy - cy, cy + 1 - gy)) * S
    joint = 1 - smoothstep(0.03, 0.09, edge)
    sid = n.cellhash(cx, cy, 2)
    alb, veins = marble(n, X, Y, C(0.03, 0.026, 0.026), C(0.085, 0.07, 0.065), C(0.3, 0.05, 0.04), 0.6, 0.035, offset=1500)
    alb = alb * (0.85 + 0.25 * sid)[..., None]
    alb = alb + ((brush(n, X, Y) - 0.5) * 0.015)[..., None]
    alb = lerp(alb, C(0.015, 0.012, 0.012), joint * 0.8)
    hgt = ((sid - 0.5) * 0.03 - joint * 0.05).astype(F32)

    # the blood channel round the arena, and gutters running off from it
    ch = Layer(g)
    ox0, ox1, oy0, oy1 = -33.0, 33.0, -3.6, 25.2
    ch.line([(ox0, oy0), (ox1, oy0), (ox1, oy1), (ox0, oy1), (ox0, oy0)], 0.7)
    for (x, y), (tx, ty) in [((ox0, oy0), (-68, -24)), ((ox1, oy0), (68, -24)), ((ox0, oy1), (-68, 46)), ((ox1, oy1), (68, 46)),
                             ((ox0, 10.6), (-70, 10.6)), ((ox1, 10.6), (70, 10.6))]:
        pts = []
        for t in np.linspace(0, 1, 40):
            pts.append((x + (tx - x) * t + math.sin(t * 9) * 0.3 * t, y + (ty - y) * t))
        ch.line(pts, 0.55)
    chm = ch.read()
    chd = np.clip(blurf(chm, 0.12, g) * 1.4, 0, 1)
    blood_in = chd * smoothstep(0.35, 0.55, n.fbm(X, Y, 3, 3, offset=1)) * (1 - smoothstep(10, 30, ad))
    alb = lerp(alb, C(0.02, 0.015, 0.015), chd * 0.85)
    alb = lerp(alb, C(0.18, 0.02, 0.02), blood_in * 0.9)
    hgt = (hgt - chd * 0.25 + blood_in * 0.12).astype(F32)

    # the dedication, carved in a band outside the channel (top and bottom, reading from each side)
    tx = Layer(g)
    carve_text(tx, INSCRIPTION, -32, 32, -6.3, 1.5)
    carve_text(tx, INSCRIPTION, -32, 32, 30.4, 1.5, flip=True)
    tm = tx.read()
    alb = lerp(alb, C(0.11, 0.1, 0.09), np.clip(blurf(tm, 0.06, g) * 1.5 - tm, 0, 1) * 0.6)     # worn rim
    alb = lerp(alb, C(0.012, 0.01, 0.01), tm * 0.9)
    hgt = (hgt - tm * 0.08).astype(F32)

    # stains of what was offered: pools near the arena, spatter and drag marks
    bl = Layer(g)
    for x, y in scatter_points(rng, 18, (-62, 62, -21, 41), margin_keep(1.0, 12.0), 7.0):
        blob(bl, rng, x, y, rng.uniform(0.8, 2.0), 18, 0.4)
        for _ in range(rng.integers(5, 14)):
            a, d = rng.uniform(0, 6.28), rng.uniform(1.2, 3.5)
            bl.ellipse(x + math.cos(a) * d, y + math.sin(a) * d, rng.uniform(0.05, 0.2), rng.uniform(0.05, 0.16))
    blm = blurf(bl.read(), 0.08, g)
    alb = lerp(alb, lerp(C(0.07, 0.01, 0.01), C(0.16, 0.02, 0.02), n.fbm(X, Y, 0.7, 3, offset=2)), blm * 0.85)

    # candles in pools of their own wax, flames still burning
    wax = Layer(g)
    stub = Layer(g)
    flame = Layer(g)
    for cxp, cyp in scatter_points(rng, 9, (-64, 64, -22, 42), margin_keep(6.0, 26.0), 14.0):
        for _ in range(rng.integers(3, 8)):
            x, y = cxp + rng.normal(0, 1.1), cyp + rng.normal(0, 1.1)
            r = rng.uniform(0.25, 0.45)
            blob(wax, rng, x, y, r * rng.uniform(1.5, 2.3), 14, 0.45)
            stub.ellipse(x, y, r, r, 255, n=24)
            if rng.random() < 0.75:
                flame.ellipse(x, y, r * 0.38, r * 0.38)
    wm = blurf(wax.read(), 0.08, g)
    sm_ = stub.read()
    fm = flame.read()
    waxcol = lerp(C(0.13, 0.11, 0.085), C(0.22, 0.19, 0.15), n.fbm(X, Y, 0.3, 3, offset=3))
    alb, hgt = apply_object(g, alb, hgt, wm * 0.8, wm * 0.05, waxcol, shadow=0.0)
    sdome = np.clip(blurf(sm_, 0.08, g) * 1.4, 0, 1) * sm_
    alb, hgt = apply_object(g, alb, hgt, sm_, sdome * 0.4, lerp(C(0.2, 0.17, 0.13), C(0.36, 0.31, 0.24), sdome), shadow=0.5)
    wick = fm
    alb = lerp(alb, C(0.05, 0.03, 0.02), wick)
    halo = np.clip(blurf(fm, 1.4, g) * 6.0, 0, 1)
    emission = C(1.0, 0.62, 0.25) * np.clip(blurf(fm, 0.06, g) * 2.5, 0, 1)[..., None] + C(0.45, 0.2, 0.06) * halo[..., None] * 0.7
    alb = lerp(alb, C(0.12, 0.07, 0.035), halo * 0.3)          # candlelight warming the floor around them

    cav = cavity(hgt, g, 0.4, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.35), 0.05)
    smooth = 0.6 - joint * 0.4 - margin * 0.12 * smoothstep(0.5, 0.8, n.fbm(X, Y, 3, 3, offset=4)) + blm * 0.2 + blood_in * 0.25 - tm * 0.3

    # lanes: dark basalt slabs, smooth with use
    LX, LY = lane.WX, lane.WY
    lalb, lhgt = lane_base(n, lane, C(0.06, 0.055, 0.055), C(0.12, 0.11, 0.105), offset=1600)
    lj, lsid, _ = flagstones(n, LX, LY, 6.0, 5.0, gap=0.05, offset=1601)
    wear = lane_wear(lane)
    lalb = lalb * (0.9 + 0.2 * lsid)[..., None]
    lalb = lerp(lalb, C(0.02, 0.018, 0.018), lj * 0.7)
    lhgt = (lhgt - lj * 0.05).astype(F32)
    lcav = cavity(lhgt, lane, 0.3, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.25), 0.05)
    lsmooth = 0.45 + wear * 0.12 - lj * 0.3

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """The high priest's sanctum wall: two black marble steps, then a wall of red-veined black marble divided by
    pilasters, candles guttering in clusters on the ledge between them."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    import bekit as bk
    folder = os.path.join(OUT, NAME)
    n = Noise(117)
    rng = np.random.default_rng(117)
    be = BackEdge(NAME)
    blk = be.material(Material('Black', tex_h=14.0, self_light=0.45))
    wax = be.material(Material('Wax', tex_h=4.0, self_light=0.5))
    fl = be.material(bk.flame_material())
    top_y, top_h = bk.steps(be, blk, -75, 75, 34.0, 2, 0.4, 0.6)
    be.box(blk, -75, 75, top_y - 0.01, 37.6, 0, top_h, sides=False)
    be.box(blk, -75, 75, 37.5, 39.5, top_h, 15.0, sides=False)
    x = -73.5
    while x < 75:
        be.box(blk, x - 0.8, x + 0.8, 36.3, 37.6, top_h, 15.0)
        cx = x + 3.5
        if cx < 74 and not (-10 < cx < 10):
            for _ in range(rng.integers(4, 8)):
                hh = rng.uniform(0.7, 2.2)
                bk.candle(be, wax, fl, cx + rng.normal(0, 0.9), 37.0 + rng.uniform(-0.3, 0.3), top_h, hh, rng.uniform(0.16, 0.26))
        x += 7.0
    be.save(folder)

    c = blk.elev_canvas()
    alb, hgt, sm = bp.marble(c, n, C(0.035, 0.03, 0.03), C(0.09, 0.075, 0.07), C(0.35, 0.06, 0.05), 0.5, 0.03, offset=41)
    glow = np.exp(-((c.Y - 2.0) / 1.6) ** 2) * 0.12
    alb = alb + C(0.5, 0.25, 0.08) * glow[..., None]
    ta, th, ts = bp.tops(blk, n, C(0.03, 0.025, 0.025), C(0.07, 0.06, 0.055), offset=42)
    save_material(folder, blk, alb, hgt, np.full(c.X.shape, 0.6, F32), talb=ta, thgt=th, tsmooth=ts)
    c = wax.elev_canvas()
    wa = lerp(C(0.5, 0.44, 0.33), C(0.7, 0.62, 0.48), n.fbm(c.X, c.Y, 0.2, 3, offset=43))
    save_material(folder, wax, wa.astype(F32), np.zeros(c.X.shape, F32), np.full(c.X.shape, 0.4, F32))
    bk.paint_flame(folder, fl, n)
