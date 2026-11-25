"""The plain before Troy: wind-rippled sand scored by chariot wheels, dark old blood soaked in, and fallen stone along
the foot of the great walls. The sand is two generated painted tiles (Tools/Boards/gen, fal.ai FLUX 2 Pro) mixed at
different scales and angles; the walls are a painted backdrop (see back_edge). Lanes: the same sand, packed calmer."""
from boardkit import *  # noqa

NAME = 'Troy'

# The wall's foot runs along y = WALL_Y on the board, x -WALL_HALF..WALL_HALF.
WALL_Y = 34.0
WALL_HALF = 75.0
GEN = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'gen')
BACKDROP_V0 = 0.42        # the band of the painting that sits at the wall's foot (skips the rubble and figures below it)
BACKDROP_STRETCH = 0.55    # squashes the painting vertically: only ~6 units of it show above the board


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

    # the wall's foot: its shadow on the sand and fallen stone (the wall itself is the painted backdrop)
    wshadow = smoothstep(WALL_Y - 4.0, WALL_Y, Y)
    alb = alb * (1 - wshadow[..., None] * 0.65)
    rubble = [(x, y + rng.normal(0, 0.8), rng.uniform(0.5, 1.4)) for x, y in scatter_points(rng, 26, (-70, 70, WALL_Y - 1.2, WALL_Y + 0.5), lambda x, y: not (-12 < x < 32), 3.5)]
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


def back_edge():
    """The walls of Troy as a painted backdrop (Tools/Boards/gen/Troy_Wall_A.png, fal.ai FLUX 2 Pro, after the Wall
    of Troy card): a flat painting standing at the far edge and leaning back to face the camera, graded dark, fading
    to black toward the sides and into the shadow at its foot."""
    from backedge import BackEdge, Material
    folder = os.path.join(OUT, NAME)
    be = BackEdge(NAME)
    bd = be.material(Material('Backdrop', tex_h=1.0, self_light=0.0))
    width = 2 * WALL_HALF
    img = load_rgb(os.path.join(GEN, 'Troy_Wall_A.png'))
    h, w, _ = img.shape
    height = width * h / w * BACKDROP_STRETCH
    be.backdrop(bd, -WALL_HALF, WALL_HALF, WALL_Y, height, BACKDROP_V0, 1.0)
    be.save(folder)

    # grade: dark and muted, fade to black at the sides, darken the band at its foot into the sand's shadow
    from boardkit import grade
    W3 = np.array((0.2126, 0.7152, 0.0722), F32)
    lum = (img @ W3)[..., None]
    img = lum + (img - lum) * 0.7
    img = img * 0.9
    xs = np.linspace(-WALL_HALF, WALL_HALF, w, dtype=F32)
    side = 1 - smoothstep(34.0, 70.0, np.abs(xs))
    v = 1 - np.arange(h, dtype=F32) / (h - 1)                  # 0 at the image bottom
    vfoot = smoothstep(BACKDROP_V0, BACKDROP_V0 + 0.08, v)
    img = img * (side[None, :] * (0.55 + 0.45 * vfoot[:, None]))[..., None]
    img = grade(img.astype(F32), NAME)
    out = np.ones((h, w, 4), F32)
    out[..., :3] = img
    from PIL import Image
    # the spotlights only graze it, so the painting carries its own light (emission); its albedo is kept dark so the
    # lights don't add hot spots
    Image.fromarray((np.clip(img * 0.5, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(folder, 'BE_Backdrop_Albedo.jpg'), quality=93)
    flat = np.zeros((h // 4, w // 4, 3), np.uint8)
    flat[...] = (128, 128, 255)
    Image.fromarray(flat).save(os.path.join(folder, 'BE_Backdrop_Normal.jpg'), quality=95)
    mask = np.zeros((h // 4, w // 4, 4), np.uint8)
    mask[..., 3] = 20
    Image.fromarray(mask, 'RGBA').save(os.path.join(folder, 'BE_Backdrop_Mask.png'))
    em = img * 0.85
    Image.fromarray((np.clip(em, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(folder, 'BE_Backdrop_Emission.jpg'), quality=90)
    for old in ('Lime', 'Mud', 'Cap', 'Walk'):
        for suffix in ('_Albedo.jpg', '_Normal.jpg', '_Mask.png', '_Emission.jpg'):
            f = os.path.join(folder, 'BE_' + old + suffix)
            if os.path.exists(f):
                os.remove(f)
