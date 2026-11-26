"""The Amazons' proving ground in the Greek scrub (phrygana): dry golden grass and low green shrubs of thyme, mastic
and sage over pale soil (a generated tile) in the margins, the arena trodden to bare compacted sand. Back edge (3D):
a hillside of low dry-stone terraces climbing out of frame, scrub on every step, each step a little darker so the
frame fades to black at the top. Lanes: the trodden sand, smoothed. (The kerb ring, straw targets, arrows and
javelins were dropped: from above they read as clutter; the podium wall read plain.)"""
from boardkit import *  # noqa
from genkit import patch_mix, tile, two_scale, norm_lum, tile_height, edge_fade, tile_lanes, falloff

NAME = 'Trials'
FOOT_Y = 34.8
TERRACES = 7
RISE = 1.0          # height of each dry-stone retaining wall
TREAD = 2.4         # depth of each step


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(83)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)

    scrub = tile('Trials_ScrubTile.png')
    sand = tile('Trials_SandTile.png')
    salb = norm_lum(two_scale(n, scrub, X, Y, 5.5) * C(0.9, 1.0, 0.82), 0.18)            # sage-olive, off the accent
    aalb = norm_lum(two_scale(n, sand, X, Y, 9.0, offset=13) * C(0.97, 0.97, 0.93), 0.16)
    trod = 0.2 + 0.65 * smoothstep(8.0, -2.0, ad)
    alb, bare = patch_mix(n, salb, aalb, X, Y, trod, scale=5.0, softness=0.12, offset=14)
    big = n.fbm(X, Y, 16, 4, offset=1)
    alb = alb * (0.85 + 0.3 * big)[..., None]
    hgt = (big * 0.25 + tile_height(alb, g, 0.12) + (1 - bare) * 0.08).astype(F32)

    # the foot of the terraces: contact shadow
    alb = alb * (1 - smoothstep(FOOT_Y - 1.4, FOOT_Y, Y)[..., None] * 0.45)

    alb = edge_fade(X, Y, alb)
    cav = cavity(hgt, g, 0.5, 2.0)
    alb = alb * (1 - cav[..., None] * 0.3)
    smooth = np.full(X.shape, 0.1, F32)

    lalb, lhgt, lsmooth = tile_lanes(n, lane, sand * C(0.97, 0.97, 0.93), size=9.0, mean=0.18, calm=0.45)

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, None, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """Dry-stone terraces: each a run of boxes whose front is a rough stone retaining wall and whose top is scrub. The
    runs break every few units with a small jog in depth and height, so the terrace lines aren't ruled straight.
    Each terrace's top is its own material, darker the higher it is (top faces can't carry a height falloff)."""
    from backedge import BackEdge, Material, save_material
    folder = os.path.join(OUT, NAME)
    for old in ('Tiers', 'Wall'):
        for suffix in ('_Albedo.jpg', '_Normal.jpg', '_Mask.png', '_Emission.jpg'):
            f = os.path.join(folder, 'BE_' + old + suffix)
            if os.path.exists(f):
                os.remove(f)
    rng = np.random.default_rng(871)
    n = Noise(871)
    be = BackEdge(NAME)
    stone = be.material(Material('DryStone', tex_h=TERRACES * RISE + 1.0, self_light=0.0))
    tops = [be.material(Material('Scrub%d' % k, tex_h=1.0, self_light=0.0)) for k in range(TERRACES)]
    for k in range(TERRACES):
        x = -75.0
        while x < 75.0:
            w = float(rng.uniform(5.0, 11.0))
            jog_y = float(rng.uniform(-0.35, 0.35))
            jog_h = float(rng.uniform(-0.15, 0.15))
            y0 = FOOT_Y + k * TREAD + jog_y
            h1 = (k + 1) * RISE + jog_h
            be.box(stone, x, min(x + w, 75.0), y0, FOOT_Y + (k + 1) * TREAD + 3.0, 0.0, h1, batter=0.12,
                   top_mat=tops[k])
            x += w
    be.save(folder)

    # retaining walls: rough limestone (Cyclops' rock tile), dusty at the foot, lit only low down
    rock = tile('Cyclops_RockTile.png')
    scrub = tile('Trials_ScrubTile.png')
    c = stone.elev_canvas()
    salb = norm_lum(lerp(sample_tiled(rock, c.X, c.Y, 2.2, 0.2), sample_tiled(rock, c.X + 7.0, c.Y, 1.8, 1.7), 0.5), 0.2)
    course = np.abs(((c.Y % RISE) / RISE) - 0.5) * 2
    salb = salb * (1 - smoothstep(0.85, 1.0, course) * 0.4)[..., None]       # the shadowed lip under each tread
    em_k, am = falloff(n, c.X, c.Y, top=5.0, glow=0.9, floor=0.15)
    tp = stone.top_canvas()
    stal = norm_lum(sample_tiled(scrub, tp.X, tp.Y, 4.0), 0.14)
    save_material(folder, stone, salb * am[..., None], tile_height(salb, c, 0.15), np.full(c.X.shape, 0.12, F32),
                  emission=salb * em_k[..., None], talb=stal)

    # treads: scrub, darker step by step and toward the sides
    for k, m in enumerate(tops):
        tp = m.top_canvas()
        talb = norm_lum(two_scale(n, scrub, tp.X, tp.Y + k * 13.0, 3.0, offset=40 + k) * C(0.9, 1.0, 0.82), 0.18)
        fade = (1.1 * 0.62 ** k) * (1 - smoothstep(10.0, 60.0, np.abs(tp.X)) * 0.8)
        talb = talb * fade[..., None]
        ec = m.elev_canvas()
        save_material(folder, m, np.zeros((ec.h, ec.w, 3), F32), np.zeros((ec.h, ec.w), F32),
                      np.full((ec.h, ec.w), 0.1, F32), talb=talb.astype(F32), thgt=tile_height(talb, tp, 0.1))
