"""The Amazons' proving ground: an arena floor of fine pale sand, compacted, raked and scuffed by running feet (a
generated tile, graded down from its pale ochre to a muted grey-beige so it stays dark under the light). Back edge
(3D): the arena's podium wall, big pale ashlar running out of frame (the seating is above it, unseen), with a dark
gateway into the arena's passages, lit only at the foot. Lanes: the same sand, smoothed. (The kerb ring, straw
targets, arrows and javelins were dropped: from above they read as clutter, and the procedural sand as blurry.)"""
from boardkit import *  # noqa
from genkit import tile, two_scale, norm_lum, tile_height, edge_fade, tile_lanes, AshlarWall

NAME = 'Trials'
FOOT_Y = 34.8
GATE = (-34.0, 4.6)          # the gateway: x, width
WALL_H = 12.0


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(83)
    rng = np.random.default_rng(83)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)

    sand = tile('Trials_SandTile.png')
    alb = norm_lum(two_scale(n, sand, X, Y, 9.0), 0.17)
    big = n.fbm(X, Y, 16, 4, offset=1)
    alb = alb * (0.85 + 0.3 * big)[..., None]
    hgt = (big * 0.25 + tile_height(alb, g, 0.08)).astype(F32)

    # sand drifted against the wall's foot, its contact shadow, and the gateway's darkness spilling out
    alb = alb * (1 - smoothstep(FOOT_Y - 1.4, FOOT_Y, Y)[..., None] * 0.45)
    gx, gw = GATE
    gate = np.clip(1 - np.abs(X - gx) / (gw * 0.8), 0, 1) * smoothstep(FOOT_Y - 3.0, FOOT_Y, Y)
    alb = alb * (1 - gate[..., None] * 0.5)

    alb = edge_fade(X, Y, alb)
    cav = cavity(hgt, g, 0.5, 2.0)
    alb = alb * (1 - cav[..., None] * 0.25)
    smooth = np.full(X.shape, 0.1, F32)

    lalb, lhgt, lsmooth = tile_lanes(n, lane, sand, size=9.0, mean=0.19, calm=0.4)

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    from backedge import BackEdge, Material, save_material
    folder = os.path.join(OUT, NAME)
    rng = np.random.default_rng(871)
    n = Noise(871)
    be = BackEdge(NAME)
    for old in ('Tiers',):
        for suffix in ('_Albedo.jpg', '_Normal.jpg', '_Mask.png', '_Emission.jpg'):
            f = os.path.join(folder, 'BE_' + old + suffix)
            if os.path.exists(f):
                os.remove(f)
    wm = be.material(Material('Wall', tex_h=WALL_H + 1.0, self_light=0.0))
    stone = norm_lum(tile('Troy_StoneTile.png') * C(1.0, 0.96, 0.88), 0.3)
    wall = AshlarWall(wm, n, rng, stone, WALL_H, course=(0.9, 1.3), width=(2.6, 5.0), slant=0.06, beds=0.05,
                      bulge=(0.02, 0.07), proud=(-0.04, 0.04), block_size=3.2, openings=[GATE])
    wall.mesh(be, FOOT_Y, batter=0.0)
    dust = norm_lum(tile('Trials_SandTile.png'), 0.26).reshape(-1, 3).mean(0)
    alb, hgt, em, sm = wall.paint(dust_col=dust, dust_h=0.7, top=6.0, glow=0.7, floor=0.15)
    save_material(folder, wm, alb, hgt, sm, emission=em)
    be.save(folder)
