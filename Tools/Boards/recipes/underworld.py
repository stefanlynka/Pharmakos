"""Hades' realm (test enemy): broken basalt under drifted ash and cinder (a generated tile under the procedural cracks), the remains of
the dead in the margins (whole skeletons, piles, scattered fragments), embers glowing deep in the widest cracks far
from the fight, a little pale asphodel. Lanes: worn slate slabs with ash packed in the joints."""
from boardkit import *  # noqa
from genkit import tile, two_scale, norm_lum, edge_fade, RockFace

NAME = 'Underworld'


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(11)
    rng = np.random.default_rng(11)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)
    margin = smoothstep(2, 14, ad)
    stroke = brush(n, X, Y)

    # basalt: broad relief, a partial crack network (big plates) and fine crazing
    big = n.fbm(X, Y, 20, 4, offset=1)
    mid = n.fbm(X, Y, 4.5, 4, offset=2)
    grain = n.fbm(X, Y, 0.7, 4, offset=9)
    wx, wy = n.warp(X, Y, 7, 2.4)
    f1, f2, cid, _, _ = n.voronoi(wx, wy, 9.0)
    width = 0.04 + 0.22 * n.fbm(X, Y, 3, 3, offset=4)
    crack = 1 - smoothstep(0.0, 1.0, (f2 - f1) / width)
    crack *= smoothstep(0.38, 0.62, n.fbm(X, Y, 9, 3, offset=3))       # the network breaks up
    fine = smoothstep(0.88, 0.97, n.ridged(X, Y, 3.2, 3, offset=5)) * smoothstep(0.3, 0.6, mid)
    hgt = (big * 0.6 + mid * 0.25 + grain * 0.06 + (cid - 0.5) * 0.08 - crack * 0.45 - fine * 0.08).astype(F32)

    base = norm_lum(two_scale(n, tile('Underworld_AshTile.png'), X, Y, 10.0) * C(0.95, 0.97, 1.0), 0.105) * (0.75 + 0.5 * norm01(big * 0.6 + mid * 0.4))[..., None]
    alb = base + ((cid - 0.5) * 0.025)[..., None]
    alb = lerp(alb, C(0.03, 0.028, 0.03), np.clip(crack * 0.9 + fine * 0.5, 0, 1))

    # ash settles in the low places and is trodden flat across the arena
    low = smoothstep(0.55, 0.3, norm01(hgt))
    ash = np.clip(low * smoothstep(0.4, 0.65, n.fbm(X, Y, 6, 4, offset=21)) * 1.3, 0, 1) * (0.4 + 0.6 * margin)
    ash = np.maximum(ash, smoothstep(1.0, -5.0, ad) * 0.35)
    ashcol = lerp(C(0.2, 0.19, 0.185), C(0.33, 0.31, 0.3), n.fbm(X, Y, 1.5, 3, offset=22))
    alb = lerp(alb, ashcol, ash * 0.8)
    hgt = (hgt * (1 - ash * 0.6) + ash * 0.25).astype(F32)

    # (bones were tried and removed: small bright props distract; board spec)

    # asphodel: pale six-pointed flowers in a few loose clumps, far from the fight
    flowers = Layer(g)
    for x, y in scatter_points(rng, 26, (-66, 66, -23, 43), margin_keep(10.0), 5.0):
        for _ in range(rng.integers(5, 12)):
            fx, fy = x + rng.normal(0, 0.9), y + rng.normal(0, 0.9)
            s = rng.uniform(0.13, 0.22)
            rot = rng.uniform(0, 1)
            pts6 = [(fx + math.cos(a + rot) * s * (1 if i % 2 == 0 else 0.38), fy + math.sin(a + rot) * s * (1 if i % 2 == 0 else 0.38))
                    for i, a in enumerate(np.linspace(0, 2 * math.pi, 12, endpoint=False))]
            flowers.poly(pts6)
    fm = flowers.read()
    alb = lerp(alb, C(0.26, 0.25, 0.23), fm * 0.6)                      # dim, in the ground's value family
    hgt += fm * 0.04

    # embers: only in the widest cracks, far out in the margins, broken into glowing stretches
    core = (1 - smoothstep(0.0, 0.35, (f2 - f1) / width)) * crack
    ember = core * smoothstep(8, 22, ad) * smoothstep(0.5, 0.75, n.fbm(X, Y, 12, 3, offset=60))
    ember *= smoothstep(0.35, 0.65, n.fbm(X, Y, 1.8, 3, offset=61))
    halo = blurf(ember, 0.5, g)
    alb = lerp(alb, C(0.3, 0.09, 0.03), np.clip(halo * 1.2, 0, 1) * 0.6)
    emission = lerp(np.zeros_like(alb), C(1.0, 0.33, 0.06), np.clip(ember * 1.2, 0, 1))
    emission = emission + C(0.35, 0.08, 0.01) * np.clip(halo * 0.8, 0, 1)[..., None]

    cav = cavity(hgt, g, 0.7, 2.5)
    alb = alb * (1 - cav[..., None] * 0.55)
    alb = desaturate(alb, 0.1)
    alb = edge_fade(X, Y, alb)
    smooth = 0.16 - 0.08 * ash + 0.06 * (1 - margin)

    # lanes: worn slate slabs, ash in the joints
    LX, LY = lane.WX, lane.WY
    lstroke = brush(n, LX, LY)
    lf1, lf2, lcid, _, _ = n.voronoi(LX * 0.5, LY, 2.4, jitter=0.6)
    joint = 1 - smoothstep(0.015, 0.08, lf2 - lf1)
    lgrain = n.fbm(LX, LY, 0.8, 5, offset=70)
    lbig = n.fbm(LX, LY, 8, 4, offset=71)
    lalb = lerp(C(0.12, 0.115, 0.115), C(0.185, 0.175, 0.17), norm01(lbig))
    lalb = lalb + ((lcid - 0.5) * 0.04)[..., None] + ((lgrain - 0.5) * 0.035)[..., None] + ((lstroke - 0.5) * 0.03)[..., None]
    lalb = lerp(lalb, C(0.25, 0.235, 0.225), joint * 0.55)
    lhgt = (lbig * 0.15 + lgrain * 0.04 + (lcid - 0.5) * 0.05 - joint * 0.1).astype(F32)
    lcav = cavity(lhgt, lane, 0.5, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.35), 0.2)
    lsmooth = 0.3 - joint * 0.18 + lbig * 0.1

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """A wall of jagged black basalt closing the far edge (a generated rock tile carved into the mesh, genkit.RockFace),
    rising out of frame, embers glowing in its lowest cracks, lit only at its foot."""
    from backedge import BackEdge, Material, save_material
    folder = os.path.join(OUT, NAME)
    n = Noise(13)
    be = BackEdge(NAME)
    rk = be.material(Material('Basalt', tex_h=14.0, self_light=0.0))
    face = RockFace(rk, n, tile('Underworld_BasaltTile.png'), 35.0, top=14.0, lean=0.35, bulk=2.6, bulk_scale=5.0,
                    disp=0.8, size=5.0, foot_wave=1.6, offset=130)
    face.mesh(be)
    c = face.canvas
    alb, hgt, em, sm = face.paint(alb=norm_lum(face.alb, 0.16), top=4.0, glow=0.5, floor=0.08)
    crack = smoothstep(0.86, 0.95, n.ridged(c.X * 0.8, c.Y, 1.6, 3, offset=192))
    hot = crack * smoothstep(0.35, 0.6, n.fbm(c.X, c.Y, 3.0, 3, offset=193)) * (1 - smoothstep(0.5, 2.5, c.Y))
    alb = lerp(alb, C(0.3, 0.08, 0.02), hot)
    em = em + C(1.0, 0.32, 0.06) * hot[..., None] + C(0.3, 0.07, 0.01) * np.clip(blurf(hot, 0.4, c) * 1.5, 0, 1)[..., None]
    save_material(folder, rk, alb, hgt, sm, emission=em.astype(F32))
    be.save(folder)
