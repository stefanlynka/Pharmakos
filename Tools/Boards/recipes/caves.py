"""The Gorgon's cave: a damp limestone floor worn smooth, grit and small stones, faint green slime in the damp at the
margins (a generated tile, calmed in the arena; no puddles: they caught the eye beside the portraits). Back edge (3D):
the cave wall closing in, wet dark rock with flowstone runnels rising out of frame (a generated rock tile carved
into the mesh), fallen rocks at its foot, lit only at the foot. Lanes: the same floor, packed calmer. (Statues and
bodies were tried and dropped: unconvincing and distracting.)"""
from boardkit import *  # noqa
from genkit import tile, two_scale, norm_lum, tile_height, edge_fade, tile_lanes, falloff, RockFace, boulder

NAME = 'Caves'
FOOT_Y = 35.2


def fallen():
    """Rocks fallen from the wall: (x, y, radius, height), clear of the enemy portrait and its offerings."""
    rng = np.random.default_rng(571)
    out = []
    for x in rng.uniform(-66, 64, 40):
        if -16 < x < 35 or any(abs(x - o[0]) < 7 for o in out):
            continue
        r = rng.uniform(1.0, 2.2)
        out.append((float(x), FOOT_Y - rng.uniform(0.4, 1.8), r, r * rng.uniform(0.7, 1.1)))
        if len(out) >= 6:
            break
    return out


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(53)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)

    floor = tile('Caves_FloorTile.png') * C(0.95, 1.0, 1.0)
    alb = norm_lum(two_scale(n, floor, X, Y, 13.0), 0.125)
    calm = smoothstep(5.0, -1.0, ad) * 0.6
    alb = lerp(alb, np.broadcast_to(alb.reshape(-1, 3).mean(0), alb.shape), calm)
    big = n.fbm(X, Y, 16, 4, offset=1)
    alb = alb * (0.85 + 0.3 * big)[..., None]
    hgt = (big * 0.4 + tile_height(alb, g, 0.15) * (1 - calm * 0.7)).astype(F32)

    # slime in the damp, out in the margins only
    damp = smoothstep(0.55, 0.72, n.fbm(X, Y, 6, 4, offset=5)) * smoothstep(4.0, 12.0, ad)
    alb = lerp(alb, alb * C(0.7, 1.0, 0.7), damp * 0.5)

    # the wall's foot: contact shadow and the fallen rocks' shadows
    alb = alb * (1 - smoothstep(FOOT_Y - 1.6, FOOT_Y, Y)[..., None] * 0.5)
    sh = Layer(g)
    for x, y, r, h in fallen():
        sh.ellipse(x, y - 0.3, r * 1.3, r * 0.9)
    alb = alb * (1 - blurf(sh.read(), 0.6, g)[..., None] * 0.55)

    alb = edge_fade(X, Y, alb)
    cav = cavity(hgt, g, 0.6, 2.5)
    alb = desaturate(alb * (1 - cav[..., None] * 0.4), 0.1)
    smooth = (0.18 + damp * 0.12).astype(F32)

    lalb, lhgt, lsmooth = tile_lanes(n, lane, floor, size=13.0, mean=0.15, calm=0.55)
    lalb = desaturate(lalb, 0.2)

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    from backedge import BackEdge, Material, save_material
    folder = os.path.join(OUT, NAME)
    n = Noise(57)
    be = BackEdge(NAME)
    rk = be.material(Material('CaveRock', tex_h=16.0, self_light=0.0))
    rock = tile('Caves_RockTile.png')
    face = RockFace(rk, n, rock, FOOT_Y, top=16.0, lean=0.3, bulk=2.4, bulk_scale=6.0, disp=0.7, size=6.0,
                    foot_wave=1.4, offset=400)
    face.mesh(be)
    alb, hgt, em, sm = face.paint(alb=norm_lum(face.alb, 0.24), top=4.0, glow=0.6, floor=0.06)
    save_material(folder, rk, alb, hgt, sm, emission=em)

    fm = be.material(Material('Fallen', tex_h=4.0, self_light=0.0))
    for i, (x, y, r, h) in enumerate(fallen()):
        boulder(be, fm, n, x, y, r, h, seed=7.0 * i)
    c = fm.elev_canvas()
    falb = norm_lum(lerp(sample_tiled(rock, c.X, c.Y, 2.4, 0.9), sample_tiled(rock, c.X + 5.0, c.Y, 2.0, 2.3), 0.5), 0.2)
    em_k, am = falloff(n, c.X, c.Y, top=4.0, glow=0.5, floor=0.3)
    save_material(folder, fm, falb * am[..., None], tile_height(falb, c, 0.15), np.full(c.X.shape, 0.15, F32),
                  emission=falb * em_k[..., None])
    be.save(folder)
