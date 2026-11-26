"""Polyphemus' island: dry hillside ground of thin sun-dried grass, thyme and limestone grit (a generated tile), the
arena trodden bare, scree at the foot of the hill. Back edge (3D): the rocky hillside rising out of frame, the black
mouth of the giant's cave and the great boulder he rolls across it, lit only at the foot. Lanes: the same ground,
packed calmer. (The giant's footprints and scattered boulders were dropped: seen from above they read as a map.)"""
from boardkit import *  # noqa
from genkit import tile, two_scale, norm_lum, tile_height, edge_fade, tile_lanes, falloff, RockFace, boulder

NAME = 'Cyclops'
FOOT_Y = 34.8          # the hillside's foot (kept clear of the enemy portrait and its offerings)
CAVE_X = -30.0
BOULDER = (-21.0, 34.4, 3.0, 4.6)     # x, y, radius, height


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(23)
    rng = np.random.default_rng(23)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)

    ground = tile('Cyclops_GroundTile.png')
    alb = norm_lum(two_scale(n, ground, X, Y, 11.0) * C(0.95, 1.0, 0.82), 0.17)     # leaned olive: a pasture, not dirt
    big = n.fbm(X, Y, 16, 4, offset=1)
    alb = alb * (0.8 + 0.4 * big)[..., None]
    hgt = (big * 0.3 + tile_height(alb, g, 0.1)).astype(F32)

    # the arena trodden toward bare earth: muted, a little darker, the grass worn down
    bare = smoothstep(3.0, -4.0, ad) * (0.55 + 0.35 * n.fbm(X, Y, 4, 3, offset=4))
    alb = lerp(alb, desaturate(alb, 0.6) * 0.9, bare * 0.55)

    # scree at the hill's foot, a narrow contact shadow, and the boulder's shadow
    alb = alb * (1 - smoothstep(FOOT_Y - 1.6, FOOT_Y, Y)[..., None] * 0.45)
    bs = Layer(g)
    bs.ellipse(BOULDER[0], BOULDER[1] - 0.6, BOULDER[2] * 1.15, BOULDER[2] * 0.7)
    alb = alb * (1 - blurf(bs.read(), 0.8, g)[..., None] * 0.5)
    scree = [(x, y + rng.normal(0, 0.6), rng.uniform(0.2, 0.7)) for x, y in
             scatter_points(rng, 40, (-70, 70, FOOT_Y - 2.4, FOOT_Y + 0.4), lambda x, y: not (-14 < x < 33), 2.0)]
    rm, rh, rc = rocks(g, n, rng, scree, base=C(0.13, 0.125, 0.115), light=C(0.3, 0.29, 0.26))
    alb, hgt = apply_object(g, alb, hgt, rm, rh * 0.5, rc, shadow=0.5, shadow_blur=0.15)

    alb = edge_fade(X, Y, alb)
    cav = cavity(hgt, g, 0.5, 2.0)
    alb = alb * (1 - cav[..., None] * 0.3)
    smooth = np.full(X.shape, 0.1, F32)

    lalb, lhgt, lsmooth = tile_lanes(n, lane, ground, size=11.0, mean=0.19, calm=0.45)
    lalb = desaturate(lalb, 0.35)

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """The hillside: a natural rock face (Tools/Boards/gen/Cyclops_RockTile.png) running out of frame, with grassy
    patches on its ledges, the cave mouth sunk deep into it, and the boulder lying beside the mouth."""
    from backedge import BackEdge, Material, save_material
    folder = os.path.join(OUT, NAME)
    n = Noise(27)
    be = BackEdge(NAME)
    rk = be.material(Material('HillRock', tex_h=14.0, self_light=0.0))
    rock = tile('Cyclops_RockTile.png')
    ground = tile('Cyclops_GroundTile.png')
    face = RockFace(rk, n, rock, FOOT_Y, top=14.0, lean=0.5, bulk=1.8, bulk_scale=7.0, disp=0.6, size=6.0,
                    foot_wave=0.5, recesses=[(CAVE_X, 0.0, 4.2, 4.8, 5.0)])
    face.mesh(be)
    c = face.canvas
    grass = smoothstep(0.6, 0.74, n.fbm(c.X, c.Y, 2.0, 4, offset=202)) * smoothstep(0.5, 2.0, c.Y)
    galb = norm_lum(sample_tiled(ground, c.X, c.Y, 5.0), float(face.alb.mean()) * 0.8)
    alb, hgt, em, sm = face.paint(alb=lerp(norm_lum(face.alb, 0.3), galb, grass * 0.6), top=4.5, floor=0.12)
    save_material(folder, rk, alb, hgt, sm, emission=em)

    bm = be.material(Material('Boulder', tex_h=6.0, self_light=0.0))
    bx, by, br, bh = BOULDER
    boulder(be, bm, n, bx, by, br, bh, seed=3.0)
    c = bm.elev_canvas()
    # two crossed samples so the rock tile's strata don't wrap round the lathe as rings
    balb = norm_lum(lerp(sample_tiled(rock, c.X, c.Y, 2.6, 0.9), sample_tiled(rock, c.X + 7.0, c.Y, 2.1, 2.4), 0.5), 0.24)
    lich = smoothstep(0.62, 0.76, n.fbm(c.X, c.Y, 0.5, 3, offset=206))
    balb = lerp(balb, balb * C(1.05, 1.05, 0.8), lich * 0.5)
    em_k, am = falloff(n, c.X, c.Y, top=5.0, glow=0.55)
    bh_ = tile_height(balb, c, 0.12)
    save_material(folder, bm, balb * am[..., None], bh_, np.full(c.X.shape, 0.12, F32), emission=balb * em_k[..., None])
    be.save(folder)
