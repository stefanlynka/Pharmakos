"""Generates the texture sets for altar kit styles (see build_altar_kit.py and AltarStyle.cs).

For each style it writes, into <out>/<Style>/:
  Stone_Albedo / Stone_Normal / Stone_Mask      1024 tileable marble for the body and steps
  Panel_Albedo                                  calmer version of the stone for the text faces
  Trim_Albedo / Trim_Normal / Trim_Mask         1024x256 carved border strip, tiles along u, v=0 is the outer edge
  Corner_Albedo / Corner_Normal / Corner_Mask   256 corner medallion
Masks follow URP Lit's metallic map: R = metallic, A = smoothness.

These are procedural placeholders. Any of them can be replaced with painted or AI-made art of the same layout
(e.g. from Gemini) without touching the mesh or code.

Usage:
  blender -b --factory-startup --python Tools/Blender/altar_textures.py -- <out dir> [Style ...]
"""
import math
import os
import sys

import bpy
import numpy as np

argv = sys.argv[sys.argv.index("--") + 1:]
OUT = argv[0]
ONLY = set(argv[1:])

rng = np.random.default_rng(7)


# ---------------------------------------------------------------- helpers ---
def save(name, folder, rgba):
    """rgba: float array (h, w, 4), row 0 = bottom (v = 0)."""
    h, w, _ = rgba.shape
    img = bpy.data.images.new(name, w, h, alpha=True, float_buffer=False, is_data=True)
    img.pixels.foreach_set(np.clip(rgba, 0, 1).astype(np.float32).ravel())
    img.filepath_raw = os.path.join(folder, name + ".png")
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)


def fbm(h, w, beta=2.0, seed=0):
    """Tileable fractal noise in 0..1 via spectral synthesis (periodic by construction)."""
    r = np.random.default_rng(seed)
    white = r.standard_normal((h, w))
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    spec = np.fft.fft2(white) / (f ** (beta / 2.0))
    spec[0, 0] = 0
    n = np.real(np.fft.ifft2(spec))
    n -= n.min()
    return n / max(n.max(), 1e-6)


def normal_from_height(hgt, strength, wrap_x=True, wrap_y=True):
    def d(a, axis, wrap):
        if wrap:
            return (np.roll(a, -1, axis) - np.roll(a, 1, axis)) * 0.5
        g = np.gradient(a, axis=axis)
        return g
    dx = d(hgt, 1, wrap_x) * strength
    dy = d(hgt, 0, wrap_y) * strength
    n = np.dstack([-dx, -dy, np.ones_like(hgt)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    out = np.ones(hgt.shape + (4,))
    out[..., :3] = n * 0.5 + 0.5
    return out


def lerp(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return np.asarray(a) * (1 - t) + np.asarray(b) * t


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def rgba(rgb, a=1.0):
    out = np.ones(rgb.shape[:2] + (4,))
    out[..., :3] = rgb
    out[..., 3] = a
    return out


def mask(metal, smooth):
    out = np.zeros(np.shape(smooth) + (4,))
    out[..., 0] = metal
    out[..., 3] = smooth
    return out


def seg_dist(px, py, ax, ay, bx, by):
    vx, vy = bx - ax, by - ay
    wx, wy = px - ax, py - ay
    t = np.clip((wx * vx + wy * vy) / max(vx * vx + vy * vy, 1e-9), 0, 1)
    dx, dy = wx - t * vx, wy - t * vy
    return np.sqrt(dx * dx + dy * dy)


def poly_dist(px, py, pts):
    d = np.full(px.shape, 1e9)
    for (ax, ay), (bx, by) in zip(pts[:-1], pts[1:]):
        d = np.minimum(d, seg_dist(px, py, ax, ay, bx, by))
    return d


def inside_polygon(px, py, pts):
    inside = np.zeros(px.shape, bool)
    n = len(pts)
    for i in range(n):
        x1, y1 = pts[i]
        x2, y2 = pts[(i + 1) % n]
        cond = ((y1 > py) != (y2 > py))
        xint = (x2 - x1) * (py - y1) / ((y2 - y1) + 1e-12) + x1
        inside ^= cond & (px < xint)
    return inside


def filled_polygon_height(px, py, pts, soft):
    edge = poly_dist(px, py, pts + [pts[0]])
    ins = inside_polygon(px, py, pts)
    return np.where(ins, smoothstep(0, soft, edge), 0.0)


def stroke_height(d, thickness):
    """Rounded carved stroke: 1 on the centre line, falling to 0 at thickness."""
    t = np.clip(d / thickness, 0, 1)
    return np.sqrt(np.clip(1 - t * t, 0, 1))


def spiral(cx, cy, r0, r1, turns, start_angle, clockwise=True, n=80):
    pts = []
    for i in range(n + 1):
        t = i / n
        a = start_angle + (-1 if clockwise else 1) * t * turns * 2 * math.pi
        r = r0 + (r1 - r0) * t
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


# --------------------------------------------------------------- marble ---
def marble(size, s, seed):
    """Tileable marble: a smooth cloudy base with veins along warped bands. s: style dict. Returns albedo rgb, height."""
    h = w = size
    y, x = np.mgrid[0:h, 0:w] / size
    warp = (fbm(h, w, 3.4, seed) - 0.5) * s.get("warp", 1.2)
    fx, fy = s.get("vein_dir", (1, 2))
    phase = 2 * math.pi * (fx * x + fy * y + warp)
    width = s.get("vein_width", 0.08)
    veins = np.exp(-(np.sin(phase) / width) ** 2)
    # thinner secondary veins at another angle
    warp2 = (fbm(h, w, 3.0, seed + 1) - 0.5) * s.get("warp", 1.2) * 1.3
    phase2 = 2 * math.pi * (2 * fx * x - fy * y + warp2)
    veins2 = np.exp(-(np.sin(phase2) / (width * 0.45)) ** 2) * s.get("fine_veins", 0.4)
    # break the veins up so they fade in and out along their length
    fade = smoothstep(0.25, 0.7, fbm(h, w, 2.8, seed + 4))
    v = np.clip((veins * fade + veins2 * (1 - fade * 0.5)) * s.get("vein_amount", 0.9), 0, 1)
    cloud = fbm(h, w, 2.8, seed + 2)
    base = lerp(s["stone_dark"], s["stone_light"], smoothstep(0.15, 0.85, cloud))
    alb = lerp(base, s["vein"], v)
    if "vein2" in s:
        p3 = 2 * math.pi * (fx * x + 3 * fy * y + (fbm(h, w, 3.2, seed + 5) - 0.5) * 1.5)
        alb = lerp(alb, s["vein2"], np.exp(-(np.sin(p3) / (width * 0.35)) ** 2) * 0.6 * fade)
    grain = fbm(h, w, 2.0, seed + 3)
    alb = alb * (0.96 + 0.08 * grain[..., None])
    height = 0.7 * cloud + 0.3 * grain - 0.15 * v
    return alb, height


def stone_set(s, folder, seed):
    alb, hgt = marble(1024, s, seed)
    save("Stone_Albedo", folder, rgba(alb))
    save("Stone_Normal", folder, normal_from_height(hgt, s.get("stone_bump", 2.0)))
    gloss = s.get("stone_smooth", 0.45) * (0.85 + 0.3 * fbm(1024, 1024, 1.5, seed + 9))
    save("Stone_Mask", folder, mask(0.0, gloss))
    # Panel: same stone, pulled toward its average colour so text stays readable.
    mean = alb.reshape(-1, 3).mean(0)
    calm = lerp(alb, np.broadcast_to(mean, alb.shape) * s.get("panel_tone", 1.0), np.full(alb.shape[:2], s.get("panel_calm", 0.7)))
    save("Panel_Albedo", folder, rgba(calm))


# ---------------------------------------------------------------- motifs ---
# Trim coordinates: x in [0, 4) along one repeat, y in [0, 1] across (0 = outer edge).
def trim_grid():
    h, w = 256, 1024
    y, x = np.mgrid[0:h, 0:w].astype(float)
    return (x + 0.5) / w * 4.0, (y + 0.5) / h


def wrapped(fn, x, y):
    """Evaluate a motif so it tiles along x: take the max over neighbouring repeats."""
    return np.maximum(np.maximum(fn(x, y), fn(x - 4, y)), fn(x + 4, y))


def fillets(y):
    return np.maximum(smoothstep(0.11, 0.07, y), smoothstep(0.89, 0.93, y))


def motif_waves(x, y):
    """Running-dog wave scroll: a sea line that rises into a crest and curls forward over itself."""
    cx, cy, R = 2.62, 0.52, 0.27
    rise = []
    for i in range(41):
        t = i / 40
        px = 0.7 + t * (cx - 0.7)
        e = 0.5 - 0.5 * math.cos(t * math.pi)            # ease in/out
        rise.append((px, 0.26 + e * (cy + R - 0.26)))
    curl = [(0.0, 0.26)] + rise + spiral(cx, cy, R, 0.05, 1.35, math.pi / 2, clockwise=True)[1:]
    sea = [(cx, cy - R + 0.02), (4.0, 0.26)]
    d = np.minimum(poly_dist(x, y, curl), poly_dist(x, y, sea))
    return stroke_height(d, 0.075)


def motif_lightning(x, y):
    pts = [(0.0, 0.5), (0.55, 0.78), (0.95, 0.38), (1.55, 0.78), (2.0, 0.22), (2.45, 0.62), (3.05, 0.22), (3.45, 0.62), (4.0, 0.5)]
    d = poly_dist(x, y, pts)
    dots = np.minimum(np.hypot(x - 1.25, y - 0.27), np.hypot(x - 2.75, y - 0.75))
    return np.maximum(stroke_height(d, 0.085), stroke_height(dots, 0.07))


def rosette_height(x, y, cx, cy, r, petals=5, rot=0.0):
    dx, dy = x - cx, y - cy
    rr = np.hypot(dx, dy)
    a = np.arctan2(dy, dx) + rot
    edge = r * (0.55 + 0.45 * np.abs(np.cos(petals * a / 2)))
    petal = smoothstep(edge, edge - 0.04, rr)
    centre = smoothstep(r * 0.28, r * 0.2, rr)
    vein = 1 - 0.35 * (np.abs(np.sin(petals * a / 2)) ** 8) * (rr < edge)
    return np.maximum(petal * vein * 0.85, centre)


def motif_rosettes(x, y):
    vine_pts = [(i / 40 * 4, 0.5 + 0.16 * math.sin(i / 40 * 4 * math.pi)) for i in range(41)]
    vine = stroke_height(poly_dist(x, y, vine_pts), 0.04) * 0.7
    leaves = np.zeros_like(x)
    for lx, ly, ang in [(0.55, 0.7, 0.7), (1.45, 0.3, -0.7), (2.55, 0.7, 0.7), (3.45, 0.3, -0.7)]:
        dx, dy = x - lx, y - ly
        u = dx * math.cos(ang) + dy * math.sin(ang)
        v = -dx * math.sin(ang) + dy * math.cos(ang)
        leaves = np.maximum(leaves, smoothstep(1.0, 0.8, (u / 0.2) ** 2 + (v / 0.08) ** 2) * 0.75)
    r = np.maximum(rosette_height(x, y, 1.0, 0.5, 0.36), rosette_height(x, y, 3.0, 0.5, 0.36, rot=0.6))
    return np.maximum(np.maximum(vine, leaves), r)


def motif_key(x, y):
    a = [(0.0, 0.22), (4.0, 0.22)]
    b = [(3.0, 0.22), (3.0, 0.8), (0.95, 0.8), (0.95, 0.46), (2.15, 0.46), (2.15, 0.62)]
    d = np.minimum(poly_dist(x, y, a), poly_dist(x, y, b))
    return stroke_height(d, 0.07)


MOTIFS = {"waves": motif_waves, "lightning": motif_lightning, "rosettes": motif_rosettes, "key": motif_key}


def corner_grid():
    n = 256
    y, x = np.mgrid[0:n, 0:n].astype(float)
    return (x + 0.5) / n, (y + 0.5) / n


def corner_trident(x, y):
    h = stroke_height(poly_dist(x, y, [(0.5, 0.16), (0.5, 0.8)]), 0.045)
    prongs = [[(0.28, 0.82), (0.28, 0.56), (0.72, 0.56), (0.72, 0.82)]]
    for p in prongs:
        h = np.maximum(h, stroke_height(poly_dist(x, y, p), 0.04))
    for tip in [(0.28, 0.82), (0.5, 0.84), (0.72, 0.82)]:
        h = np.maximum(h, filled_polygon_height(x, y, [(tip[0] - 0.06, tip[1] - 0.02), (tip[0] + 0.06, tip[1] - 0.02), (tip[0], tip[1] + 0.1)], 0.03))
    return h


def corner_bolt(x, y):
    pts = [(0.62, 0.9), (0.34, 0.52), (0.5, 0.52), (0.3, 0.1), (0.7, 0.56), (0.53, 0.56), (0.74, 0.9)]
    return filled_polygon_height(x, y, pts, 0.05)


def corner_rose(x, y):
    outer = rosette_height(x, y, 0.5, 0.5, 0.36, petals=5)
    inner = rosette_height(x, y, 0.5, 0.5, 0.2, petals=5, rot=0.63)
    return np.maximum(outer * 0.8, inner)


def corner_boss(x, y):
    return rosette_height(x, y, 0.5, 0.5, 0.33, petals=8)


CORNERS = {"trident": corner_trident, "bolt": corner_bolt, "rose": corner_rose, "boss": corner_boss}


def relief_set(s, folder, prefix, hgt, wrap_x):
    """Shared colouring for trims and corners: raised motif in the relief colour over a recessed ground."""
    noise = fbm(hgt.shape[0], hgt.shape[1], 1.6, 11)
    raised = smoothstep(0.15, 0.6, hgt)
    ground = lerp(s["relief_ground"], s["relief_ground"] * 0.75, noise)
    top = lerp(s["relief"], s["relief_hi"], smoothstep(0.6, 1.0, hgt) * (0.6 + 0.4 * noise))
    if "patina" in s:
        top = lerp(top, s["patina"], np.clip(smoothstep(0.55, 0.8, noise) * (1 - smoothstep(0.7, 1.0, hgt)), 0, 1) * 0.8)
    alb = lerp(ground, top, raised)
    save(prefix + "_Albedo", folder, rgba(alb))
    save(prefix + "_Normal", folder, normal_from_height(hgt, s.get("relief_bump", 6.0), wrap_x=wrap_x, wrap_y=False))
    smooth = s.get("ground_smooth", 0.25) * (1 - raised) + s.get("relief_smooth", 0.5) * raised
    save(prefix + "_Mask", folder, mask(s.get("relief_metal", 0.0) * raised, smooth))


def trim_set(s, folder):
    x, y = trim_grid()
    motif = MOTIFS[s["trim"]]
    hgt = np.maximum(wrapped(motif, x, y), fillets(y))
    relief_set(s, folder, "Trim", hgt, True)


def corner_set(s, folder):
    x, y = corner_grid()
    rim = np.maximum(smoothstep(0.1, 0.05, np.minimum.reduce([x, y, 1 - x, 1 - y])), 0)
    hgt = np.maximum(CORNERS[s["corner"]](x, y), rim * 0.8)
    relief_set(s, folder, "Corner", hgt, False)


# ---------------------------------------------------------------- styles ---
C = lambda *v: np.array(v, float)
STYLES = {
    "Default": dict(
        stone_dark=C(0.70, 0.66, 0.58), stone_light=C(0.86, 0.83, 0.76), vein=C(0.55, 0.52, 0.48), vein_amount=0.35, vein_width=0.06, fine_veins=0.15, stone_smooth=0.35, panel_calm=0.6,
        trim="key", corner="boss", relief=C(0.80, 0.76, 0.68), relief_hi=C(0.90, 0.87, 0.80), relief_ground=C(0.55, 0.51, 0.45),
        relief_bump=5.0, relief_smooth=0.35, ground_smooth=0.2),
    "Poseidon": dict(
        stone_dark=C(0.04, 0.16, 0.20), stone_light=C(0.08, 0.30, 0.35), vein=C(0.70, 0.88, 0.86), vein_amount=0.85, vein_width=0.09,
        fine_veins=0.5, warp=1.4, vein_dir=(1, 1), stone_smooth=0.6, panel_calm=0.75, panel_tone=0.85,
        trim="waves", corner="trident", relief=C(0.55, 0.45, 0.25), relief_hi=C(0.78, 0.66, 0.40), relief_ground=C(0.10, 0.20, 0.20),
        patina=C(0.30, 0.58, 0.50), relief_metal=0.3, relief_smooth=0.45, relief_bump=6.0),
    "Zeus": dict(
        stone_dark=C(0.20, 0.22, 0.27), stone_light=C(0.36, 0.38, 0.45), vein=C(0.82, 0.88, 1.0), vein_amount=0.95, vein_width=0.035,
        fine_veins=0.35, warp=0.9, vein_dir=(2, 1), stone_smooth=0.55, panel_calm=0.75, panel_tone=0.9,
        trim="lightning", corner="bolt", relief=C(0.80, 0.60, 0.22), relief_hi=C(0.98, 0.84, 0.45), relief_ground=C(0.10, 0.10, 0.13),
        relief_metal=0.45, relief_smooth=0.6, relief_bump=6.0),
    "Aphrodite": dict(
        stone_dark=C(0.66, 0.38, 0.40), stone_light=C(0.86, 0.62, 0.60), vein=C(0.97, 0.92, 0.88), vein2=C(0.45, 0.10, 0.14),
        vein_amount=0.8, vein_width=0.08, fine_veins=0.35, warp=1.3, vein_dir=(1, 2), stone_smooth=0.55, panel_calm=0.7,
        trim="rosettes", corner="rose", relief=C(0.88, 0.62, 0.52), relief_hi=C(0.98, 0.84, 0.76), relief_ground=C(0.38, 0.10, 0.13),
        relief_metal=0.35, relief_smooth=0.55, relief_bump=6.0),
}

for i, (name, style) in enumerate(STYLES.items()):
    if ONLY and name not in ONLY:
        continue
    folder = os.path.join(OUT, name)
    os.makedirs(folder, exist_ok=True)
    stone_set(style, folder, 100 + i * 10)
    trim_set(style, folder)
    corner_set(style, folder)
    print("altar style", name, "->", folder)
