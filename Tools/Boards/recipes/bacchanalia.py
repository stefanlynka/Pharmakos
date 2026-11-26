"""Dionysus' revel in a night glade: a forest floor of fallen oak and vine leaves, ochre, brown and a few wine-red
(a generated tile), trampled toward bare loam in the arena, a few dark wine stains soaked in. Back edge (3D): the
forest's edge, great old trunks rising out of frame in front of a dark wooded bank, lit only at their roots.
Lanes: the same litter, trodden calmer. (The terrace wall, pithoi, pergola and painted leaves were dropped: they
read as low-poly and clay-like.)"""
from boardkit import *  # noqa
from genkit import tile, two_scale, norm_lum, tile_height, edge_fade, tile_lanes, falloff, RockFace

NAME = 'Bacchanalia'
BANK_Y = 39.5        # the wooded bank behind the trunks


def trunks():
    """The trunks along the forest's edge: (x, y, radius). Kept behind the enemy portrait and its offerings."""
    rng = np.random.default_rng(613)
    out = []
    x = -72.0 + rng.uniform(0, 4)
    while x < 72:
        r = rng.uniform(1.0, 2.3)
        y = rng.uniform(35.6, 38.6) + r * 0.3
        out.append((x, y, r))
        x += rng.uniform(7.0, 13.0)
    return out


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(53)
    rng = np.random.default_rng(53)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)

    litter = tile('Bacchanalia_LeafTile.png')
    alb = norm_lum(two_scale(n, litter, X, Y, 12.0), 0.15)
    # the arena trampled: the litter pulled toward its own dark mean, so the leaves don't read under the cards
    calm = smoothstep(5.0, -1.0, ad) * 0.7
    alb = lerp(alb, np.broadcast_to(alb.reshape(-1, 3).mean(0) * 0.85, alb.shape), calm)
    big = n.fbm(X, Y, 16, 4, offset=1)
    alb = alb * (0.8 + 0.4 * big)[..., None]
    hgt = (big * 0.3 + tile_height(alb, g, 0.1) * (1 - calm * 0.7)).astype(F32)

    # wine spilt and soaked in: few, dark, matte
    w = Layer(g)
    for x, y in scatter_points(rng, 9, (-66, 66, -23, 33), margin_keep(3.0), 9.0):
        blob(w, rng, x, y, rng.uniform(0.7, 1.6), 16, 0.45)
    wm = blurf(w.read(), 0.35, g) * (0.5 + 0.5 * n.fbm(X, Y, 0.6, 3, offset=4))
    alb = lerp(alb, alb * C(0.55, 0.3, 0.38), wm * 0.75)

    # roots' shadows on the litter, and the ground behind the trees lost in darkness
    sh = Layer(g)
    for x, y, r in trunks():
        sh.ellipse(x, y - 0.4, r * 1.6, r * 1.0)
    alb = alb * (1 - blurf(sh.read(), 0.9, g)[..., None] * 0.55)
    alb = alb * (1 - smoothstep(34.0, 37.5, Y)[..., None] * 0.6)

    alb = edge_fade(X, Y, alb)
    cav = cavity(hgt, g, 0.5, 2.0)
    alb = alb * (1 - cav[..., None] * 0.3)
    smooth = np.full(X.shape, 0.1, F32)

    lalb, lhgt, lsmooth = tile_lanes(n, lane, litter, size=12.0, mean=0.17, calm=0.65)

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """Old trunks (lathes with a noise wobble and flared roots; generated bark tile) standing along the far edge and
    rising out of frame, in front of a dark bank of earth and litter (a RockFace with the leaf tile)."""
    from backedge import BackEdge, Material, save_material
    folder = os.path.join(OUT, NAME)
    n = Noise(57)
    be = BackEdge(NAME)
    for old in ('Pithos', 'Post', 'Terrace', 'Vine'):
        for suffix in ('_Albedo.jpg', '_Albedo.png', '_Normal.jpg', '_Mask.png', '_Emission.jpg'):
            f = os.path.join(folder, 'BE_' + old + suffix)
            if os.path.exists(f):
                os.remove(f)

    # the bank: dark earth and leaves, far back, running out of frame
    bk = be.material(Material('Bank', tex_h=14.0, self_light=0.0))
    litter = tile('Bacchanalia_LeafTile.png')
    bank = RockFace(bk, n, litter, BANK_Y, top=14.0, lean=0.6, bulk=2.2, bulk_scale=8.0, disp=0.3, size=6.0,
                    foot_wave=1.2, offset=500)
    bank.mesh(be, step=0.3, h_step=0.3)
    alb, hgt, em, sm = bank.paint(alb=norm_lum(bank.alb, 0.12), top=3.5, glow=0.3, floor=0.08, cavity_dark=0.6)
    save_material(folder, bk, alb, hgt, sm, emission=em)

    # the trunks
    tm = be.material(Material('Bark', tex_h=14.0, self_light=0.0))
    for i, (x, y, r) in enumerate(trunks()):
        prof = [(0.0, r * 1.3), (0.35, r * 1.12), (1.0, r * 1.02), (4.0, r * 0.97), (8.0, r * 0.92), (14.0, r * 0.88)]
        seed = 11.0 * i
        wob = lambda t, h, s=seed: 0.86 + 0.28 * n.value(np.array([[math.cos(t) * 1.6 + s]], F32),
                                                        np.array([[math.sin(t) * 1.6 + h * 0.15 + s]], F32), 1.0, 61)[0, 0]
        be.lathe(tm, x, y, prof, segments=20, cap=False, wobble=wob)
    c = tm.elev_canvas()
    bark = tile('Bacchanalia_BarkTile.png')
    balb = norm_lum(sample_tiled(bark, c.X, c.Y, 3.2), 0.16)
    moss = smoothstep(0.6, 0.75, n.fbm(c.X, c.Y, 1.0, 4, offset=62)) * (1 - smoothstep(0.0, 2.5, c.Y))
    balb = lerp(balb, balb * C(0.8, 1.0, 0.6), moss * 0.5)
    bh = tile_height(balb, c, 0.2)
    em_k, am = falloff(n, c.X, c.Y, top=6.5, glow=0.6, floor=0.06)
    save_material(folder, tm, balb * am[..., None], bh, np.full(c.X.shape, 0.12, F32), emission=balb * em_k[..., None])
    be.save(folder)
