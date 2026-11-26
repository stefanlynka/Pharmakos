"""Board-level helpers shared by the recipes in Tools/Boards/recipes. (Original header:)
Paints every encounter's combat board (ground + lanes) into Assets/Resources/Content/Boards/<Name>/.

Usage:
  python Tools/Boards/boards.py                 all boards
  python Tools/Boards/boards.py Underworld Troy  just these
  python Tools/Boards/boards.py --preview ...    also write lit previews to Claude/Boards/preview/
Then in Unity: Tools -> Boards -> Build Board Styles (with GameScene open). See Docs/Boards.md.

Shared rules (so eleven boards still read as one game):
  - The ground is darker and busier than the lanes; lanes are calm and low-contrast so cards read on them.
  - The arena (both lanes and the gap) is worn and clean; story details (bones, debris, stains) live in the margins.
  - Muted, warm-leaning colour; every board has a cavity pass so carved and scattered things sit in the surface.
"""
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
from boardgen import *  # noqa
from boardgen import blurf  # noqa

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Content', 'Boards')
PREVIEW = os.path.join(ROOT, 'Claude', 'Boards', 'preview')


# ---------------------------------------------------------------- helpers ---
def arena_dist(X, Y, grow=0.0):
    """Distance outside the arena (both lanes plus the gap), negative inside."""
    x0, x1, y0, y1 = ARENA
    return sd_box(X, Y, (x0 - grow, x1 + grow, y0 - grow, y1 + grow), 3.0)


def scatter_points(rng, n, rect, keep, min_gap=0.0):
    """n random points in rect (x0, x1, y0, y1) for which keep(x, y) is true, at least min_gap apart."""
    pts = []
    tries = 0
    x0, x1, y0, y1 = rect
    while len(pts) < n and tries < n * 60:
        tries += 1
        x, y = rng.uniform(x0, x1), rng.uniform(y0, y1)
        if not keep(x, y):
            continue
        if min_gap > 0 and any((x - px) ** 2 + (y - py) ** 2 < min_gap ** 2 for px, py in pts):
            continue
        pts.append((x, y))
    return pts


def margin_keep(min_dist, max_dist=999.0):
    def keep(x, y):
        d = float(arena_dist(np.array([[x]], F32), np.array([[y]], F32))[0, 0])
        return min_dist <= d <= max_dist
    return keep


def bone(layer, x, y, length, angle, thick, v=255):
    ca, sa = math.cos(angle), math.sin(angle)
    hx, hy = ca * length / 2, sa * length / 2
    px, py = -sa, ca
    a = (x - hx, y - hy)
    b = (x + hx, y + hy)
    w = thick / 2
    layer.poly([(a[0] + px * w, a[1] + py * w), (b[0] + px * w, b[1] + py * w),
                (b[0] - px * w, b[1] - py * w), (a[0] - px * w, a[1] - py * w)], v)
    k = thick * 0.78
    for e in (a, b):
        layer.ellipse(e[0] + px * k * 0.55, e[1] + py * k * 0.55, k, k, v)
        layer.ellipse(e[0] - px * k * 0.55, e[1] - py * k * 0.55, k, k, v)


def skull(layer, holes, x, y, size, angle):
    ca, sa = math.cos(angle), math.sin(angle)

    def R(dx, dy):
        return x + dx * ca - dy * sa, y + dx * sa + dy * ca
    cx, cy = R(0, size * 0.15)
    layer.ellipse(cx, cy, size * 0.62, size * 0.7, 255, angle)
    jx, jy = R(0, -size * 0.55)
    layer.ellipse(jx, jy, size * 0.36, size * 0.3, 255, angle)
    for side in (-1, 1):
        ex, ey = R(side * size * 0.25, -size * 0.12)
        holes.ellipse(ex, ey, size * 0.17, size * 0.15, 255, angle)
    nx, ny = R(0, -size * 0.36)
    holes.ellipse(nx, ny, size * 0.07, size * 0.1, 255, angle)


def rib(layer, x, y, r, a0, a1, thick, v=255):
    pts = [(x + r * math.cos(a0 + (a1 - a0) * i / 10), y + r * math.sin(a0 + (a1 - a0) * i / 10)) for i in range(11)]
    layer.line(pts, thick, v)


def skeleton(layer, holes, rng, x, y, s, angle):
    """Remains of a whole body lying in the dust: skull, spine, ribcage, pelvis and a few limb bones (some missing)."""
    ca, sa = math.cos(angle), math.sin(angle)

    def R(dx, dy):
        return x + dx * ca - dy * sa, y + dx * sa + dy * ca
    sx, sy = R(0, 3.0 * s)
    skull(layer, holes, sx, sy, 1.0 * s, angle + rng.normal(0, 0.3))
    for i in range(14):                                   # vertebrae
        vx, vy = R(rng.normal(0, 0.04 * s), (2.2 - i * 0.36) * s)
        layer.ellipse(vx, vy, 0.16 * s, 0.12 * s, 255, angle)
    for i in range(6):                                    # ribs, both sides
        for side in (-1, 1):
            if rng.random() < 0.2:
                continue
            ry = (1.7 - i * 0.34) * s
            pts = []
            for k in range(9):
                t = k / 8
                px = side * (0.15 + 1.1 * math.sin(t * 2.2)) * s * (1 - i * 0.06)
                py = ry - t * t * 0.9 * s
                pts.append(R(px, py))
            layer.line(pts, 0.13 * s)
    for side in (-1, 1):                                  # pelvis
        hx, hy = R(side * 0.55 * s, -2.9 * s)
        layer.ellipse(hx, hy, 0.55 * s, 0.4 * s, 255, angle + side * 0.4)
        holes.ellipse(hx, hy, 0.18 * s, 0.13 * s, 255, angle)
    for side in (-1, 1):                                  # legs and an arm or two, scattered a little
        if rng.random() < 0.85:
            lx, ly = R(side * 0.6 * s, -4.6 * s)
            bone(layer, lx, ly, 2.6 * s, angle + math.pi / 2 + side * 0.15 + rng.normal(0, 0.2), 0.32 * s)
        if rng.random() < 0.6:
            ax, ay = R(side * 1.8 * s, 0.6 * s)
            bone(layer, ax, ay, 1.9 * s, angle + math.pi / 2 + rng.normal(0, 0.6), 0.24 * s)


def bone_cluster(layer, holes, rng, x, y, count, spread, with_skull=True):
    if with_skull:
        skull(layer, holes, x + rng.normal(0, spread * 0.3), y + rng.normal(0, spread * 0.3), rng.uniform(0.9, 1.3),
              rng.uniform(0, 6.28))
    for _ in range(count):
        bone(layer, x + rng.normal(0, spread), y + rng.normal(0, spread), rng.uniform(1.2, 3.2), rng.uniform(0, 6.28),
             rng.uniform(0.22, 0.4))


def place_bones(g, n, rng, ash, skeletons=7, piles=22, singles=70, fragments=260, keep_out=1.0):
    """The dead, kept out of the arena: whole skeletons, piles, scattered bones and fragments, rounded, dirty and
    partly buried. Returns (visible mask, eye/socket holes, height contribution, colour)."""
    X, Y = g.X, g.Y
    bones = Layer(g)
    holes = Layer(g)
    for x, y in scatter_points(rng, skeletons, (-62, 62, -21, 41), margin_keep(keep_out + 6.0), min_gap=16):
        skeleton(bones, holes, rng, x, y, rng.uniform(0.75, 0.95), rng.uniform(0, 6.28))
    for x, y in scatter_points(rng, piles, (-64, 64, -22, 42), margin_keep(keep_out + 3.0), min_gap=7):
        bone_cluster(bones, holes, rng, x, y, rng.integers(3, 9), rng.uniform(0.8, 1.6), rng.random() < 0.6)
    for x, y in scatter_points(rng, singles, (-66, 66, -23, 43), margin_keep(keep_out + 1.5), min_gap=2.5):
        bone(bones, x, y, rng.uniform(1.0, 2.8), rng.uniform(0, 6.28), rng.uniform(0.18, 0.34))
    for x, y in scatter_points(rng, fragments, (-66, 66, -23, 43), margin_keep(keep_out), 0.8):
        bone(bones, x, y, rng.uniform(0.3, 0.8), rng.uniform(0, 6.28), rng.uniform(0.1, 0.18))
    bm = bones.read()
    hm = holes.read() * bm
    round_ = np.clip(blurf(bm, 0.09, g) * 1.6, 0, 1) * bm               # bulges along each bone's middle
    buried = smoothstep(0.45, 0.75, n.fbm(X, Y, 2.2, 3, offset=40)) * (0.3 + 0.7 * ash)
    vis = bm * (1 - buried * 0.85)
    dirt = n.fbm(X, Y, 0.5, 3, offset=41)
    col = lerp(C(0.2, 0.18, 0.15), C(0.52, 0.48, 0.4), np.clip(round_ ** 0.8 * (0.55 + 0.6 * dirt), 0, 1))
    h = vis * (round_ * 0.28 + 0.06) - hm * vis * 0.15
    return vis, hm * vis, h.astype(F32), col


def apply_bones(g, alb, hgt, bones, socket=C(0.03, 0.025, 0.02)):
    vis, holes, h, col = bones
    contact = np.clip(blurf(vis, 0.3, g) * 1.5 - vis, 0, 1) * 0.55
    alb = alb * (1 - contact[..., None])
    alb = lerp(alb, col, vis)
    alb = lerp(alb, socket, holes * 0.95)
    return alb, (hgt + h).astype(F32)


def shard(layer, rng, x, y, size, v=255):
    n = rng.integers(4, 7)
    ang = np.sort(rng.uniform(0, 2 * math.pi, n))
    pts = [(x + math.cos(a) * size * rng.uniform(0.45, 1.0), y + math.sin(a) * size * rng.uniform(0.45, 1.0)) for a in ang]
    layer.poly(pts, v)


# Exposure: the combat lights are dim and warm, so albedos are painted dark and lifted here, uniformly, before saving.
GROUND_GAIN = 2.0
LANE_GAIN = 1.6


# Board spec (Docs/StyleGuide.md, "Board spec"): matte, and graded per board so the whole frame (ground, lanes, back
# edge) sits in one value and colour family below the cards. A grade is applied last, after GROUND_GAIN / LANE_GAIN.
SMOOTH_CAP = 0.38
# value: brightness multiplier; sat: saturation multiplier; tint/tint_amt: pull the colour toward a hue at the same
# luminance (used to move warm grounds off the terracotta accent hue toward umber/olive/grey).
GRADES = {
    'Troy': dict(value=0.74, sat=0.9),
    'Trials': dict(value=0.5, sat=0.5, tint=(1.0, 0.94, 0.8), tint_amt=0.45),
    'Temple': dict(value=0.48, sat=0.85),
    'Labyrinth': dict(value=0.78, sat=0.8),
    'Cyclops': dict(value=0.74, sat=0.75),
}


def grade(alb, name):
    """Applies GRADES[name] to an albedo or emission (h, w, 3)."""
    g = GRADES.get(name)
    if g is None:
        return alb
    w = np.array((0.2126, 0.7152, 0.0722), F32)
    lum = (alb @ w)[..., None]
    out = lum + (alb - lum) * g.get('sat', 1.0)
    if 'tint' in g:
        t = np.array(g['tint'], F32)
        t = t / float(t @ w)
        out = lerp(out, lum * t, g.get('tint_amt', 0.0))
    return np.clip(out * g.get('value', 1.0), 0, 1).astype(F32)


def finish(folder, name, g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, lemission=None,
           gstrength=1.0, lstrength=1.0, preview=False, ggain=GROUND_GAIN, lgain=LANE_GAIN):
    alb = grade(np.clip(alb * ggain, 0, 1), name)
    lalb = grade(np.clip(lalb * lgain, 0, 1), name)
    if emission is not None:
        emission = grade(emission, name)
    smooth = np.minimum(smooth, SMOOTH_CAP)
    lsmooth = np.minimum(lsmooth, SMOOTH_CAP)
    save_set(folder, 'Ground', g, alb, normals(hgt, g, gstrength), smooth, emission, rotate=True)
    save_set(folder, 'Lane', lane, lalb, normals(lhgt, lane, lstrength), lsmooth, lemission)
    if preview:
        os.makedirs(PREVIEW, exist_ok=True)
        preview_board(os.path.join(PREVIEW, name + '.png'), g, alb, normals(hgt, g, gstrength), lane, lalb,
                      normals(lhgt, lane, lstrength), emission)


def preview_board(path, g, alb, nrm, lane, lalb, lnrm, emission=None, scale=0.35):
    """Composite: ground with both lanes pasted in place, lit from the upper left, viewed flat."""
    L = np.array((-0.3, 0.4, 0.86), F32)
    L /= np.linalg.norm(L)
    img = alb * (np.clip(nrm @ L, 0, 1)[..., None] * 0.8 + 0.2)
    if emission is not None:
        img = img + emission
    limg = lalb * (np.clip(lnrm @ L, 0, 1)[..., None] * 0.8 + 0.2)
    # paste lanes: lane canvas rows: top half = enemy lane, bottom half = player lane
    h2 = lane.h // 2
    for half, rect in ((limg[:h2], ENEMY_LANE), (limg[h2:], PLAYER_LANE)):
        x0, y1 = g.px(rect[0], rect[3])
        x1, y0 = g.px(rect[1], rect[2])
        from PIL import Image as _I
        tile = _I.fromarray((np.clip(half, 0, 1) * 255).astype(np.uint8)).resize((int(x1 - x0), int(y0 - y1)), _I.LANCZOS)
        img[int(y1):int(y1) + tile.size[1], int(x0):int(x0) + tile.size[0]] = np.asarray(tile, F32) / 255
    from PIL import Image as _I
    out = _I.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8))
    out.resize((int(g.w * scale), int(g.h * scale)), _I.LANCZOS).save(path)


def new_canvases():
    return Canvas(GROUND_RECT, GROUND_SIZE), Canvas(LANE_RECT, LANE_SIZE, lane_canvas=True)




# ------------------------------------------------------------ nature kit ---
def blob(layer, rng, x, y, r, sides=14, rough=0.25, angle=0.0, squash=1.0, v=255):
    """Irregular closed shape (rock, puddle, stain) of radius ~r."""
    pts = []
    phase = rng.uniform(0, 6.28)
    for i in range(sides):
        a = 2 * math.pi * i / sides
        rr = r * (1 + rough * (math.sin(a * 2 + phase) * 0.5 + rng.uniform(-1, 1) * 0.6))
        ex, ey = math.cos(a) * rr, math.sin(a) * rr * squash
        pts.append((x + ex * math.cos(angle) - ey * math.sin(angle), y + ex * math.sin(angle) + ey * math.cos(angle)))
    layer.poly(pts, v)


def rocks(g, n, rng, placements, base=C(0.3, 0.29, 0.27), light=C(0.5, 0.48, 0.44), lichen=None, offset=500):
    """Boulders/stones: placements = [(x, y, r)]. Irregular, rounded (multi-scale dome), cracked and pitted, lit a
    little from the upper left (baked, so they read even outside the spotlights). Returns (mask, height, colour)."""
    L = Layer(g)
    for x, y, r in placements:
        blob(L, rng, x, y, r, sides=rng.integers(11, 18), rough=0.3, angle=rng.uniform(0, 6.28), squash=rng.uniform(0.55, 1.0))
    m = L.read()
    X, Y = g.X, g.Y
    dome = (np.clip(blurf(m, 0.12, g) * 1.6, 0, 1) + np.clip(blurf(m, 0.45, g) * 1.5, 0, 1)
            + np.clip(blurf(m, 1.2, g) * 1.4, 0, 1)) / 3 * m
    rough = n.fbm(X, Y, 0.7, 5, offset=offset + 3)
    crk = smoothstep(0.9, 0.97, n.ridged(X, Y, 1.4, 3, offset=offset + 4))
    h = (dome * 1.0 + (rough - 0.5) * 0.25 * m - crk * 0.12 * m).astype(F32)
    gy, gx = np.gradient(blurf(h, 0.05, g))
    lit = np.clip(0.5 + (-gx + gy) * g.ppu * 0.9, 0, 1)            # light from the upper left
    tone = np.clip(lit * 0.65 + dome * 0.15 + 0.3 * (rough - 0.3), 0, 1)
    col = lerp(base, light, tone)
    col = col * (0.75 + 0.45 * n.fbm(X, Y, 2.5, 3, offset=offset + 2))[..., None]
    col = lerp(col, col * 0.45, crk)
    if lichen is not None:
        lm = smoothstep(0.55, 0.72, n.fbm(X, Y, 0.45, 4, offset=offset + 1)) * smoothstep(0.15, 0.6, dome)
        col = lerp(col, lichen, lm * 0.55)
    return m, h, col


def tufts(n, X, Y, cell=0.55, blades=34, offset=750):
    """Top-down grass: overlapping tufts whose blades radiate from each tuft's root. 0..1, bright = blade tips."""
    out = np.zeros(X.shape, F32)
    for k, size in enumerate((cell, cell * 0.62)):
        f1, f2, cid, px, py = n.voronoi(X + k * 37.1, Y + k * 11.3, size, 0.9)
        dx = X + k * 37.1 - px
        dy = Y + k * 11.3 - py
        r = np.sqrt(dx * dx + dy * dy) / size
        th = np.arctan2(dy, dx) / (2 * math.pi) + 0.5
        stripe = n.value(th * blades + cid * 97.0, r * 1.5, 1.0, offset + k)
        stripe = smoothstep(0.35, 0.85, stripe)
        tip = smoothstep(0.1, 0.75, r) * (1 - smoothstep(0.75, 1.15, r))
        out = np.maximum(out, stripe * tip * (0.6 + 0.4 * cid))
    return out


def apply_object(g, alb, hgt, mask, h, col, shadow=0.6, shadow_blur=0.35, shadow_offset=(0.0, -0.15)):
    """Composites an object layer: soft contact shadow (offset toward the camera), colour, height."""
    sh = blurf(mask, shadow_blur, g)
    dx = int(round(shadow_offset[0] * g.ppu))
    dy = int(round(-shadow_offset[1] * g.ppu))
    sh = np.roll(np.roll(sh, dy, 0), dx, 1)
    sh = np.clip(sh * 1.4 - mask, 0, 1) * shadow
    alb = alb * (1 - sh[..., None])
    alb = lerp(alb, col, mask)
    return alb, (hgt + h).astype(F32)


def leaf(layer, x, y, length, angle, width=0.42, lobes=0, v=255):
    """Leaf outline (lobes > 0 gives a vine/oak-like lobed edge) with a stem."""
    ca, sa = math.cos(angle), math.sin(angle)
    pts = []
    for i in range(25):
        t = i / 24
        w = math.sin(math.pi * t) ** 0.8 * width * length
        if lobes:
            w *= 0.75 + 0.25 * abs(math.sin(t * math.pi * lobes))
        pts.append((t * length, w))
    pts += [(t, -w) for t, w in reversed(pts)]
    out = [(x + px * ca - py * sa, y + px * sa + py * ca) for px, py in pts]
    layer.poly(out, v)
    layer.line([(x, y), (x - ca * length * 0.2, y - sa * length * 0.2)], length * 0.04, v)


def palmate_leaf(layer, x, y, size, angle, v=255):
    """Grape/fig style leaf: five lobes from the stem point."""
    for k, spread in enumerate((-1.1, -0.55, 0, 0.55, 1.1)):
        ln = size * (1.0 if k == 2 else 0.85 if k in (1, 3) else 0.6)
        leaf(layer, x, y, ln, angle + spread, 0.38, 0, v)


def hoofprint(layer, x, y, size, angle, v=255):
    """Cloven print (deer, sheep, goat): two teardrops."""
    ca, sa = math.cos(angle), math.sin(angle)
    for side in (-1, 1):
        px, py = -sa * side * size * 0.22, ca * side * size * 0.22
        layer.ellipse(x + px, y + py, size * 0.5, size * 0.2, v, angle)


def footprint(layer, x, y, length, angle, v=255):
    """Bare human-shaped foot (sole, heel, five toes), pointing along angle."""
    ca, sa = math.cos(angle), math.sin(angle)

    def R(dx, dy):
        return x + dx * ca - dy * sa, y + dx * sa + dy * ca
    hx, hy = R(-length * 0.3, 0)
    layer.ellipse(hx, hy, length * 0.2, length * 0.16, v, angle)
    bx, by = R(length * 0.12, length * 0.02)
    layer.ellipse(bx, by, length * 0.27, length * 0.2, v, angle)
    mx, my = R(-length * 0.08, -length * 0.06)
    layer.ellipse(mx, my, length * 0.22, length * 0.1, v, angle)
    for i, (tx, ty, tr) in enumerate(((0.43, 0.14, 0.085), (0.42, 0.0, 0.07), (0.39, -0.1, 0.06), (0.35, -0.18, 0.05),
                                      (0.3, -0.25, 0.045))):
        px, py = R(length * tx, length * ty)
        layer.ellipse(px, py, length * tr, length * tr, v)


def grass(n, X, Y, offset=700):
    """Top-down turf texture (0..1): fine blades whose lie drifts, plus clumping."""
    blades = brush(n, X, Y, size=0.18, stretch=6.0, offset=offset)
    clumps = n.fbm(X, Y, 1.1, 4, offset=offset + 10)
    return (blades * 0.55 + clumps * 0.45).astype(F32), clumps


def strokes(layer, rng, pts, length, width, jitter=0.6, v=255, angle_fn=None):
    """Short strokes (blades, twigs, straw) at points."""
    for x, y in pts:
        a = angle_fn(x, y) if angle_fn else rng.uniform(0, 6.28)
        a += rng.normal(0, jitter)
        ln = length * rng.uniform(0.6, 1.3)
        layer.line([(x, y), (x + math.cos(a) * ln, y + math.sin(a) * ln)], width * rng.uniform(0.7, 1.2), v)


def dense_points(rng, count, rect=(-67, 67, -23.5, 43.5)):
    x0, x1, y0, y1 = rect
    return np.stack([rng.uniform(x0, x1, count), rng.uniform(y0, y1, count)], 1)


def arena_mask_points(pts, min_d=None, max_d=None):
    xs = pts[:, 0:1].astype(F32)
    ys = pts[:, 1:2].astype(F32)
    d = arena_dist(xs, ys)[:, 0]
    keep = np.ones(len(pts), bool)
    if min_d is not None:
        keep &= d >= min_d
    if max_d is not None:
        keep &= d <= max_d
    return pts[keep]


def lane_base(n, lane, dark, light, scale=7.0, grain_amt=0.04, offset=900):
    """Calm lane ground: soft tonal drift plus fine grain and brush texture. Returns (albedo, height)."""
    LX, LY = lane.WX, lane.WY
    big = n.fbm(LX, LY, scale, 4, offset=offset)
    grain_ = n.fbm(LX, LY, 0.6, 4, offset=offset + 1)
    st = brush(n, LX, LY, offset=offset + 2)
    alb = lerp(dark, light, norm01(big))
    alb = alb + ((grain_ - 0.5) * grain_amt)[..., None] + ((st - 0.5) * 0.03)[..., None]
    h = (big * 0.12 + grain_ * 0.04).astype(F32)
    return alb, h


def lane_wear(lane):
    """0..1: strongest along each lane's middle (where cards sit and fights happen), fading at the edges."""
    ly = np.where(lane.Y < 10.0, lane.Y, lane.Y - 10.0)
    across = 1 - np.abs(ly - 5.0) / 5.0
    along = 1 - smoothstep(24, 30, np.abs(lane.X))
    return (smoothstep(0.1, 0.7, across) * along).astype(F32)


# ------------------------------------------------------------ built kit ---
def flagstones(n, X, Y, w, h, gap=0.06, stagger=0.5, jitter=0.15, offset=0):
    """Rectangular paving in running bond. Returns (joint 0..1 where the joints are, stone id 0..1, edge distance)."""
    row = np.floor(Y / h).astype(np.int64)
    shift = (row % 2) * stagger * w + (n.cellhash(row, row * 3 + offset, 0) - 0.5) * jitter * w
    xs = X + shift
    col = np.floor(xs / w).astype(np.int64)
    lx = xs - col * w
    ly = Y - row * h
    edge = np.minimum(np.minimum(lx, w - lx), np.minimum(ly, h - ly)).astype(F32)
    joint = 1 - smoothstep(gap * 0.3, gap, edge)
    sid = n.cellhash(col + offset, row, 2)
    return joint.astype(F32), sid.astype(F32), edge


def wet_sheen(n, X, Y, scale=4.0, offset=0):
    return smoothstep(0.55, 0.75, n.fbm(X, Y, scale, 3, offset=offset))


def taper(layer, pts, w0, w1, v=255):
    """Polyline whose width runs from w0 to w1."""
    for i in range(len(pts) - 1):
        t = i / max(1, len(pts) - 2)
        layer.line([pts[i], pts[i + 1]], w0 + (w1 - w0) * t, v)


def figure(layer, rng, x, y, s, angle, pieces=None, v=255):
    """A human body lying on its back seen from above (statue or corpse): head and neck, a torso narrowing to the
    waist, tapered arms and legs. pieces: the parts to keep (default all), for broken statues."""
    ca, sa = math.cos(angle), math.sin(angle)

    def R(dx, dy):
        return x + dx * ca - dy * sa, y + dx * sa + dy * ca
    keep = pieces or {'head', 'torso', 'larm', 'rarm', 'lleg', 'rleg'}
    if 'head' in keep:
        hx, hy = R(0, 2.55 * s)
        layer.ellipse(hx, hy, 0.3 * s, 0.37 * s, v, angle)
        taper(layer, [R(0, 2.2 * s), R(0, 1.95 * s)], 0.22 * s, 0.26 * s, v)
    if 'torso' in keep:
        layer.poly([R(-0.68 * s, 1.85 * s), R(0.68 * s, 1.85 * s), R(0.42 * s, 0.35 * s), R(0.5 * s, -0.25 * s),
                    R(-0.5 * s, -0.25 * s), R(-0.42 * s, 0.35 * s)], v)
        for side in (-1, 1):
            sx, sy = R(side * 0.62 * s, 1.72 * s)
            layer.ellipse(sx, sy, 0.2 * s, 0.17 * s, v, angle)
    for side, name in ((-1, 'larm'), (1, 'rarm')):
        if name in keep:
            bend = rng.uniform(-0.3, 0.9)
            taper(layer, [R(side * 0.72 * s, 1.72 * s), R(side * (0.95 + 0.25 * bend) * s, 0.75 * s),
                          R(side * (0.9 + 0.7 * bend) * s, -0.15 * s)], 0.26 * s, 0.15 * s, v)
    for side, name in ((-1, 'lleg'), (1, 'rleg')):
        if name in keep:
            spread = rng.uniform(0, 0.35)
            taper(layer, [R(side * 0.28 * s, -0.2 * s), R(side * (0.33 + spread * 0.5) * s, -1.5 * s),
                          R(side * (0.35 + spread) * s, -2.75 * s)], 0.34 * s, 0.16 * s, v)
            fx, fy = R(side * (0.37 + spread) * s, -2.9 * s)
            layer.ellipse(fx, fy, 0.11 * s, 0.2 * s, v, angle)


def snake_skin(layer, rng, x, y, length, width, v=255):
    a = rng.uniform(0, 6.28)
    pts = []
    for i in range(int(length / 0.2)):
        t = i * 0.2
        a += math.sin(t * 0.9 + rng.uniform(-0.2, 0.2)) * 0.18
        x += math.cos(a) * 0.2
        y += math.sin(a) * 0.2
        pts.append((x, y))
    for i in range(len(pts) - 1):
        t = i / len(pts)
        layer.line([pts[i], pts[i + 1]], width * (1.0 - 0.75 * t), v)


def marble(n, X, Y, dark, light, vein, vein_amount=0.8, width=0.06, warp=1.4, scale=6.0, offset=1100):
    """Veined marble (colour, vein mask) in world units."""
    w = (n.fbm(X, Y, scale * 0.8, 4, offset=offset) - 0.5) * warp
    phase = 2 * math.pi * (X / scale * 0.7 + Y / scale * 1.3 + w * 3)
    v1 = np.exp(-(np.sin(phase) / width) ** 2)
    w2 = (n.fbm(X, Y, scale * 0.5, 4, offset=offset + 1) - 0.5) * warp
    phase2 = 2 * math.pi * (X / scale * 1.9 - Y / scale * 0.8 + w2 * 3)
    v2 = np.exp(-(np.sin(phase2) / (width * 0.5)) ** 2) * 0.5
    fade = smoothstep(0.3, 0.7, n.fbm(X, Y, scale * 0.6, 3, offset=offset + 2))
    veins = np.clip((v1 * fade + v2 * (1 - fade * 0.5)) * vein_amount, 0, 1).astype(F32)
    cloud = n.fbm(X, Y, scale * 0.25, 5, offset=offset + 3)
    col = lerp(dark, light, smoothstep(0.15, 0.85, cloud * 0.6 + n.fbm(X, Y, scale * 0.06, 3, offset=offset + 4) * 0.4))
    col = lerp(col, vein, veins)
    return col, veins


# Greek key (meander): one repeat, 8 cells wide x 7 tall, '#' = inlay. Rows listed top to bottom.
MEANDER = [
    "########",
    "........",
    "#####.#.",
    "#...#.#.",
    "#.#.#.#.",
    "#.###.#.",
    "#.....#.",
]


def meander_band(X, Y, rect, offset_out, width, cells_x=8, cells_y=7):
    """Mask (0..1) of a Greek-key band running round rect (x0, x1, y0, y1) at distance offset_out..+width outside it.
    Each side gets its own run of the pattern; the corners are filled solid."""
    x0, x1, y0, y1 = rect
    d = sd_box(X, Y, rect, 0.0)
    inband = (d >= offset_out) & (d <= offset_out + width)
    v = np.clip((d - offset_out) / width, 0, 0.9999)          # 0 at the inner edge .. 1 at the outer edge
    # which side: compare distances outside each edge
    dx = np.maximum(x0 - X, X - x1)
    dy = np.maximum(y0 - Y, Y - y1)
    horizontal = dy >= dx
    u = np.where(horizontal, X - x0, Y - y0)
    cell = width / cells_y
    cu = np.floor(u / cell).astype(np.int64) % cells_x
    cv = np.clip((v * cells_y).astype(np.int64), 0, cells_y - 1)
    pat = np.array([[1 if ch == '#' else 0 for ch in row] for row in reversed(MEANDER)], np.float32)   # row 0 = inner
    m = pat[cv, cu]
    corner = (dx > 0) & (dy > 0)
    m = np.where(corner, 1.0, m)
    return (m * inband).astype(F32), inband.astype(F32)
