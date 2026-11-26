"""The Sirens' rocks: a dark shelf of sea-worn rock (a generated tile, calm under the cards) ending in a jagged cliff
edge that drops to the surf, rocks and stacks in the sea below. Back edge (3D): sea stacks rising out of the surf
and the next headlands at the far corners, in the same rock, lit only at their feet. Lanes: the same rock, calmer.
(Tide pools, kelp, barnacles and salt crust were dropped: they made the shelf splotchy.)"""
from boardkit import *  # noqa
from genkit import tile, two_scale, norm_lum, tile_height, tile_lanes, falloff, edge_fade

NAME = 'Cliffs'


def cliff_profile(rng):
    """The cliff edge's y at a row of knots across the board (linear between them, so it breaks angularly). Must
    be the first use of the recipe's rng so the ground and the back edge agree."""
    knots_x = np.arange(-72, 73, 1.6)
    walk = np.cumsum(rng.normal(0, 0.9, len(knots_x)))
    walk -= np.linspace(walk[0], walk[-1], len(walk))
    coves = np.zeros_like(knots_x)
    for cx_ in rng.uniform(-60, 60, 5):
        coves += -rng.uniform(3, 7) * np.clip(1 - np.abs(knots_x - cx_) / rng.uniform(4, 9), 0, 1)
    prof = 33.0 + walk * 0.9 + coves + np.abs(knots_x) * 0.05
    return knots_x, prof


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(97)
    rng = np.random.default_rng(97)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(1, 12, ad)

    shelf = tile('Cliffs_RockTile.png')
    big = n.fbm(X, Y, 12, 5, offset=1)
    alb = norm_lum(two_scale(n, shelf, X, Y, 11.0), 0.12)
    calm = smoothstep(5.0, -1.0, ad) * 0.5
    alb = lerp(alb, np.broadcast_to(alb.reshape(-1, 3).mean(0), alb.shape), calm)
    alb = alb * (0.85 + 0.3 * big)[..., None]
    hgt = (big * 0.6 + tile_height(alb, g, 0.15) * (1 - calm)).astype(F32)

    # the cliff edge: an angular, broken line (straight runs between sharp breaks, a few deep coves), then the cliff
    # face falling away (seen from above: a band of dark, vertically fractured rock), rocks and stacks in the surf,
    # and the sea below
    knots_x, prof = cliff_profile(rng)
    edge = np.interp(X, knots_x, prof).astype(F32)                      # angular: linear between knots
    wx, wy = n.warp(X, Y, 1.5, 0.5, offset=50)
    edge = edge + (n.fbm(wx, wy, 0.8, 3, offset=51) - 0.5) * 0.9       # crumbled detail on the lip
    drop = 3.2 + 1.5 * n.fbm(X, Y, 6, 2, offset=52)                     # how far the face shows from above
    d = Y - edge                                                         # >0 beyond the lip
    face = smoothstep(0.0, 0.15, d) * (1 - smoothstep(drop - 0.3, drop, d))
    sea = smoothstep(drop - 0.3, drop + 0.2, d)

    fract = n.value(X * 3.0, Y * 0.35, 1.0, 53) * 0.6 + n.fbm(X, Y, 0.5, 3, offset=54) * 0.4
    facecol = lerp(C(0.05, 0.055, 0.06), C(0.13, 0.135, 0.135), fract)
    facecol = facecol * (1 - np.clip(d / np.maximum(drop, 0.1), 0, 1) * 0.75)[..., None]   # darker toward the water
    lip = (1 - smoothstep(0.0, 0.35, np.abs(d))) * (1 - sea)
    alb = lerp(alb, C(0.26, 0.26, 0.24), lip * 0.5)                     # sunlit broken lip
    alb = lerp(alb, facecol, face)

    depth = smoothstep(drop, drop + 8, d)
    water = lerp(C(0.03, 0.085, 0.095), C(0.01, 0.03, 0.045), depth)
    swell = n.fbm(X * 0.5, Y * 1.5, 3, 4, offset=6)
    water = water * (0.8 + 0.4 * swell)[..., None]

    # rocks and sea stacks in the surf, ringed with foam
    stk = Layer(g)
    for _ in range(40):
        x = rng.uniform(-68, 68)
        e = float(np.interp(x, knots_x, prof))
        y = e + rng.uniform(3.5, 10.0)
        blob(stk, rng, x, y, rng.uniform(0.3, 1.6) if rng.random() < 0.85 else rng.uniform(1.8, 2.8), 9, 0.35)
    stm = stk.read() * sea
    ring = np.clip(blurf(stm, 0.5, g) * 2.2 - stm, 0, 1)
    foam_line = (1 - smoothstep(0.0, 1.0, np.abs(d - drop - 0.3))) * smoothstep(0.3, 0.55, n.fbm(X, Y, 0.9, 4, offset=7))
    foam_far = smoothstep(0.72, 0.85, n.fbm(X * 0.4, Y * 2.0, 2, 4, offset=8)) * depth * 0.5
    foam = np.clip(foam_line + foam_far + ring * smoothstep(0.3, 0.6, n.fbm(X, Y, 0.5, 3, offset=55)), 0, 1)
    alb = lerp(alb, water, sea)
    alb = lerp(alb, C(0.5, 0.55, 0.55), foam * sea * 0.7)
    gy_, gx_ = np.gradient(blurf(stm, 0.15, g))
    stlit = np.clip(0.45 + (-gx_ + gy_) * g.ppu * 0.8, 0, 1)
    alb = lerp(alb, lerp(C(0.04, 0.045, 0.05), C(0.18, 0.18, 0.17), stlit), stm)
    hgt = (hgt * (1 - sea) - face * np.clip(d, 0, 4) * 0.4 + sea * (float(hgt.min()) - 1.5 + swell * 0.08)
           + stm * 1.2).astype(F32)
    land = (1 - sea) * (1 - face)

    cav = cavity(hgt, g, 0.6, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.45 * (1 - sea)[..., None]), 0.05)
    alb = edge_fade(X, Y, alb)
    wet = smoothstep(0.4, 0.7, n.fbm(X, Y, 3, 3, offset=13)) * margin
    smooth = (0.2 + wet * 0.15) * (1 - face) + sea * 0.62

    # a faint glimmer on the water (emission) so the sea reads even outside the lights
    emission = C(0.03, 0.07, 0.08) * (sea * (0.4 + 0.6 * swell))[..., None] + C(0.08, 0.1, 0.1) * (foam * sea * 0.5)[..., None]
    # the far sea sinks into darkness toward the top of the frame (spec: the top fades to black)
    far = 1 - smoothstep(35.0, 42.0, Y) * 0.92
    alb = alb * far[..., None]
    emission = emission * far[..., None]

    lalb, lhgt, lsmooth = tile_lanes(n, lane, shelf, size=11.0, mean=0.15, calm=0.55)

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """Beyond the cliff edge: sea stacks rising out of the surf below, and the next headlands along the coast, their
    faces dropping sheer into the sea at the far corners."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    folder = os.path.join(OUT, NAME)
    n = Noise(99)
    rng = np.random.default_rng(97)
    knots_x, prof = cliff_profile(rng)
    edge = lambda x: float(np.interp(x, knots_x, prof))
    rng = np.random.default_rng(199)
    be = BackEdge(NAME)
    rk = be.material(Material('SeaRock', tex_h=16.0, self_light=0.0))
    for _ in range(26):
        x = rng.uniform(-70, 70)
        y = edge(x) + rng.uniform(4.0, 7.5)
        if -12 < x < 32 and y < 37.0:
            continue
        r = rng.uniform(0.8, 2.2)
        top = rng.uniform(2.5, 9.0)
        seed = rng.uniform(0, 100)
        wob = lambda t, h, s_=seed: 0.75 + 0.5 * n.value(np.array([[math.cos(t) * 2 + s_]], F32), np.array([[h * 0.6 + s_]], F32), 1.0, 7)[0, 0]
        prof_ = [(0.0, r * 1.35), (0.4, r * 1.15), (top * 0.5, r * 0.95), (top * 0.85, r * 0.75), (top, r * 0.45)]
        be.lathe(rk, x, y, prof_, segments=14, wobble=wob)
    for side in (-1, 1):
        x0, x1 = (-75.0, -38.0) if side < 0 else (38.0, 75.0)
        be.relief(rk, x0, x1, lambda x: 35.5 + (abs(x) - 38.0) * 0.05,
                  lambda x, s_=side: 9.0 + 6.0 * n.fbm(np.array([[x]], F32), np.array([[s_ * 3.0]], F32), 6, 3, offset=11)[0, 0],
                  lambda x, h: 0.8 * n.fbm(np.array([[x]], F32), np.array([[h]], F32), 1.5, 3, offset=12)[0, 0] + h * 0.08,
                  step=0.6, h_step=0.6, back=3.0)
    be.save(folder)
    c = rk.elev_canvas()
    shelf = tile('Cliffs_RockTile.png')
    alb = norm_lum(lerp(sample_tiled(shelf, c.X, c.Y, 3.0, 0.4), sample_tiled(shelf, c.X + 9.0, c.Y, 2.4, 1.9), 0.5), 0.16)
    wet = 1 - smoothstep(0.4, 2.5, c.Y)
    alb = alb * (1 - wet * 0.35)[..., None]
    foam = (1 - smoothstep(0.1, 0.5, c.Y)) * smoothstep(0.35, 0.6, n.fbm(c.X, c.Y, 0.5, 3, offset=172))
    alb = lerp(alb, C(0.32, 0.35, 0.35), foam * 0.5)
    hgt = tile_height(alb, c, 0.2)
    em_k, am = falloff(n, c.X, c.Y, top=5.0, glow=0.5, floor=0.12)
    save_material(folder, rk, alb * am[..., None], hgt, (0.15 + wet * 0.3).astype(F32), emission=alb * em_k[..., None])
