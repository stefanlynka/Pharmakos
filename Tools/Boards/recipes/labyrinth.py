"""The Minotaur's labyrinth, from inside: the fight happens in a chamber of old worn paving (a generated stone tile,
calmed in the arena), the floor sinking into darkness toward the edges, a few old stains. Back edge (3D): the
maze's walls of dark dressed stone running out of frame, stepping back and forth, broken by black passages leading
off into the maze. Lanes: the same paving, packed calmer. (The maze seen from above was dropped: it confused the
scale, as if looking down on a map.)"""
from boardkit import *  # noqa
from genkit import tile, two_scale, norm_lum, tile_height, edge_fade, tile_lanes, AshlarWall

NAME = 'Labyrinth'
FOOT_Y = 34.6
STEP_Y = 1.2                        # alternate stretches of wall stand this much further back
OPENINGS = [(-46.0, 4.4), (-19.0, 4.0), (22.0, 4.0), (48.0, 4.4)]      # passages leading off: (x, width)
WALL_H = 12.0


def foot(x):
    """The wall steps back and forth between passages, like the maze behind it."""
    k = sum(1 for ox, _ in OPENINGS if x > ox)
    return FOOT_Y + (STEP_Y if k % 2 else 0.0)


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(37)
    rng = np.random.default_rng(37)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)

    paving = tile('Labyrinth_StoneTile.png') * C(0.95, 0.95, 1.0)
    alb = norm_lum(two_scale(n, paving, X, Y, 10.0), 0.15)
    # the arena worn smooth: its paving pattern pulled toward its mean so nothing reads under the cards
    calm = smoothstep(5.0, -1.0, ad) * 0.8
    alb = lerp(alb, np.broadcast_to(alb.reshape(-1, 3).mean(0), alb.shape), calm)
    big = n.fbm(X, Y, 16, 4, offset=1)
    alb = alb * (0.82 + 0.36 * big)[..., None]
    hgt = (big * 0.25 + tile_height(alb, g, 0.12) * (1 - calm)).astype(F32)

    # old stains: few, dark, soaked in
    st = Layer(g)
    for x, y in scatter_points(rng, 8, (-66, 66, -23, 33), margin_keep(4.0), 10.0):
        blob(st, rng, x, y, rng.uniform(0.8, 1.8), 16, 0.45)
    sm_ = blurf(st.read(), 0.4, g) * (0.5 + 0.5 * n.fbm(X, Y, 0.6, 3, offset=4))
    alb = lerp(alb, alb * C(0.6, 0.45, 0.4), sm_ * 0.7)

    # the wall's foot: contact shadow, and the floor beyond it (seen through the passages) lost in darkness
    fy = np.array([foot(float(x)) for x in X[0]], F32)[None, :]
    alb = alb * (1 - smoothstep(fy - 1.4, fy, Y)[..., None] * 0.45)
    alb = alb * (1 - smoothstep(fy, fy + 1.5, Y)[..., None] * 0.9)

    alb = edge_fade(X, Y, alb)
    cav = cavity(hgt, g, 0.5, 2.0)
    alb = alb * (1 - cav[..., None] * 0.3)
    smooth = np.full(X.shape, 0.12, F32)

    lalb, lhgt, lsmooth = tile_lanes(n, lane, paving, size=10.0, mean=0.17, calm=0.55)

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """The maze's walls: dark dressed stone (Troy's limestone tile, darkened and cooled) in regular courses, running
    out of frame, stepping back and forth, with black passages between the stretches."""
    from backedge import BackEdge, Material, save_material
    folder = os.path.join(OUT, NAME)
    rng = np.random.default_rng(373)
    n = Noise(373)
    be = BackEdge(NAME)
    for old in ('Stone', 'Deep', 'Iron', 'Flame'):
        for suffix in ('_Albedo.jpg', '_Albedo.png', '_Normal.jpg', '_Mask.png', '_Emission.jpg'):
            f = os.path.join(folder, 'BE_' + old + suffix)
            if os.path.exists(f):
                os.remove(f)
    wm = be.material(Material('Wall', tex_h=WALL_H + 1.0, self_light=0.0))
    stone = norm_lum(tile('Troy_StoneTile.png') * C(0.9, 0.9, 0.94), 0.26)
    wall = AshlarWall(wm, n, rng, stone, WALL_H, course=(1.1, 1.6), width=(2.4, 4.6), slant=0.12, beds=0.1,
                      bulge=(0.02, 0.08), proud=(-0.05, 0.05), block_size=3.0, openings=OPENINGS)
    wall.mesh(be, foot, batter=0.04)
    dust = norm_lum(tile('Labyrinth_StoneTile.png'), 0.2).reshape(-1, 3).mean(0)
    alb, hgt, em, sm = wall.paint(dust_col=dust, dust_h=0.5, top=6.5, glow=0.85, floor=0.15)
    save_material(folder, wm, alb, hgt, sm, emission=em)
    be.save(folder)
