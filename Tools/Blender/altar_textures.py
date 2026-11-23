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
  blender -b --factory-startup --python Tools/Blender/altar_textures.py -- <absolute out dir> [Style ...]
  (the out dir must be absolute: Blender resolves a relative one somewhere else and the textures silently don't update)
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

# Board-wide finish, so every altar sits with the matte terracotta and rugs instead of looking polished.
MATTE_STONE = 0.45     # scales each style's stone smoothness
MATTE_RELIEF = 0.6     # scales trim/corner smoothness (metal inlays keep a little more sheen via relief_metal)
WEATHER = 1.0          # grime patches, pitting and worn edges; 0 = clean stone
PANEL_CREAM = np.array((0.86, 0.81, 0.71))   # the shared tablet colour dark stones are lifted toward
# The one dial between cohesion and individuality: how far a dark or coloured text panel is pulled toward the shared
# cream tablet (0 = the god's own stone, 1 = plain cream), and how much of the god's hue the cream keeps.
PANEL_LIFT_MAX = float(os.environ.get("ALTAR_PANEL_LIFT", 0.0))
PANEL_TINT = float(os.environ.get("ALTAR_PANEL_TINT", 0.3))
# Carved, aged ornament instead of clean painted bands.
CARVE = float(os.environ.get("ALTAR_CARVE", 0.35))         # how far border colours move toward the stone's own colour
PIGMENT_LOSS = float(os.environ.get("ALTAR_PIGMENT_LOSS", 0.4))  # how much of the old paint/gilding has flaked off
WOBBLE = 1.0           # hand-cut irregularity in the motif lines (0 = geometric)
CHIPS = 1.0            # chips and erosion breaking the carving
RELIEF_DEPTH = 1.5     # scales every style's relief_bump: deeper carving, read by light rather than colour
STAINS = float(os.environ.get("ALTAR_STAINS", 1.0))        # soot, libation streaks and old stains; 0 = none
STONE_SAT = float(os.environ.get("ALTAR_STONE_SAT", 0.7))  # saturation kept by every stone (1 = as authored)


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
    # Muted, aged colour: every stone loses some saturation so the gods share one weathered palette.
    grey = (alb @ np.array((0.3, 0.59, 0.11)))[..., None]
    alb = grey + (alb - grey) * STONE_SAT
    height = 0.7 * cloud + 0.3 * grain - 0.15 * v
    return alb, height


def weather(alb, hgt, seed):
    """Age the stone: soft grime patches, fine pitting and a dusty lift, so it reads as old carved stone."""
    n = alb.shape[0]
    grime = smoothstep(0.45, 0.9, fbm(n, n, 2.6, seed + 20)) * 0.22
    pits = smoothstep(0.80, 0.93, fbm(n, n, 0.6, seed + 21)) * 0.35
    dust = smoothstep(0.55, 0.95, fbm(n, n, 2.2, seed + 22)) * 0.05
    k = WEATHER
    alb = alb * (1 - k * (grime + pits))[..., None]
    alb = lerp(alb, np.broadcast_to(np.array((0.70, 0.62, 0.50)), alb.shape), k * dust)
    hgt = hgt - k * pits * 0.6
    rough = k * (grime + pits)        # dirty and pitted areas are duller
    return alb, hgt, rough


def ritual_stains(alb, seed):
    """Sacrificial wear: soot clouding, dark libation streaks running down (texture v runs up the altar's sides,
    and along the steps on their tops), and a few old pooled stains."""
    n = alb.shape[0]
    r = np.random.default_rng(seed + 40)
    soot = smoothstep(0.55, 0.95, fbm(n, n, 3.0, seed + 30)) * 0.35
    streaks = np.zeros((n, n))
    rows = np.arange(n)[:, None]
    cols = np.arange(n)[None, :]
    wander = (fbm(n, n, 3.2, seed + 31) - 0.5) * 24          # streaks drift sideways as they run
    for _ in range(9):
        cx = r.uniform(0, n)
        width = r.uniform(6, 18)
        top = r.uniform(0, n)
        length = r.uniform(120, 520)
        strength = r.uniform(0.25, 0.55)
        dy = (top - rows) % n                                 # distance below the start, wrapping so it tiles
        along = np.clip(dy / length, 0, 1)
        dx = np.abs(((cols - cx - wander + n / 2) % n) - n / 2)
        taper = width * (1 - 0.6 * along)
        body = smoothstep(taper, 0.0, dx) * (dy < length)
        streaks = np.maximum(streaks, body * strength * (1 - along) ** 0.7)
    streaks *= 0.6 + 0.4 * fbm(n, n, 1.4, seed + 32)
    pools = smoothstep(0.78, 0.92, fbm(n, n, 2.6, seed + 33)) * 0.45
    k = STAINS
    alb = lerp(alb, alb * C(0.12, 0.09, 0.08), k * soot)
    alb = lerp(alb, alb * C(0.50, 0.30, 0.24), k * np.maximum(streaks, pools))
    return alb


def stone_set(s, folder, seed):
    alb, hgt = marble(1024, s, seed)
    alb, hgt, rough = weather(alb, hgt, seed)
    alb = ritual_stains(alb, seed)
    save("Stone_Albedo", folder, rgba(alb))
    save("Stone_Normal", folder, normal_from_height(hgt, s.get("stone_bump", 2.0)))
    gloss = s.get("stone_smooth", 0.45) * MATTE_STONE * (0.85 + 0.3 * fbm(1024, 1024, 1.5, seed + 9)) * (1 - rough)
    save("Stone_Mask", folder, mask(0.0, gloss))
    # Panel: same stone, pulled toward its average colour so text stays readable.
    mean = alb.reshape(-1, 3).mean(0)
    calm = lerp(alb, np.broadcast_to(mean, alb.shape) * s.get("panel_tone", 1.0), np.full(alb.shape[:2], s.get("panel_calm", 0.7)))
    # Every altar gets a light tablet (the text faces), like a cream marble inlay set into the god's stone. This is
    # what ties the set together and keeps the rules text dark-on-light everywhere; the god's colour stays on the body.
    lum = float(mean @ C(0.3, 0.59, 0.11))
    lift = min(s.get("panel_lift", float(np.clip((0.62 - lum) / 0.4, 0, 1))), PANEL_LIFT_MAX)
    if lift > 0:
        hue = mean / max(mean.mean(), 1e-3)
        cream = PANEL_CREAM * (1 + (hue - 1) * s.get("panel_tint", PANEL_TINT))
        calm = lerp(calm, np.broadcast_to(cream, calm.shape), np.full(calm.shape[:2], lift))
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


# ------------------------------------------------------- shape helpers ---
def ellipse_height(x, y, cx, cy, rx, ry, ang=0.0):
    """Soft dome filling an ellipse (1 at the centre, 0 at the rim)."""
    dx, dy = x - cx, y - cy
    u = dx * math.cos(ang) + dy * math.sin(ang)
    v = -dx * math.sin(ang) + dy * math.cos(ang)
    r = np.sqrt((u / rx) ** 2 + (v / ry) ** 2)
    return np.sqrt(np.clip(1 - r * r, 0, 1)) ** 0.5


def ring_height(x, y, cx, cy, rx, ry, thickness, ang=0.0):
    dx, dy = x - cx, y - cy
    u = dx * math.cos(ang) + dy * math.sin(ang)
    v = -dx * math.sin(ang) + dy * math.cos(ang)
    d = np.abs(np.sqrt((u / rx) ** 2 + (v / ry) ** 2) - 1) * min(rx, ry)
    return stroke_height(d, thickness)


def leaf(x, y, cx, cy, length, width, ang):
    return ellipse_height(x, y, cx, cy, length, width, ang)


def polygon(x, y, pts, soft=0.04):
    return filled_polygon_height(x, y, pts, soft)


def flame_points(cx, by, w, h, lean=0.0, n=24):
    """A flame tongue: base centre (cx, by), width w, height h, tip leaning by lean.
    Widest low down, tapering in an S-curve to a sharp tip."""
    left, right = [], []
    for i in range(n + 1):
        t = i / n
        half = w * 0.5 * min(1.0, t * 6.0) * (1 - t) ** 1.3
        mid = cx + lean * t * t + 0.18 * w * math.sin(t * math.pi * 1.6) * t
        left.append((mid - half, by + h * t))
        right.append((mid + half, by + h * t))
    return left + right[::-1]


def teardrop_points(cx, cy, r, h, n=32):
    """Round at the bottom (centre cx, cy, radius r), pointed at the top (h above the centre)."""
    pts = [(cx, cy + h)]
    for i in range(n + 1):
        a = math.radians(30 - i / n * 240)   # from upper right, down round the bottom, to upper left
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


# ----------------------------------------------------- more trim motifs ---
def motif_rays(x, y):
    h = np.zeros_like(x)
    for k in range(8):
        cx = 0.25 + 0.5 * k
        tall = 0.8 if k % 2 == 0 else 0.52
        h = np.maximum(h, polygon(x, y, [(cx - 0.17, 0.14), (cx + 0.17, 0.14), (cx, tall)]))
    return h


def motif_spears(x, y):
    shaft = stroke_height(poly_dist(x, y, [(0.15, 0.5), (3.05, 0.5)]), 0.045)
    head = polygon(x, y, [(3.0, 0.5), (3.25, 0.7), (3.88, 0.5), (3.25, 0.3)])
    butt = polygon(x, y, [(0.04, 0.5), (0.25, 0.6), (0.25, 0.4)])
    return np.maximum.reduce([shaft, head, butt])


def motif_olive(x, y):
    stem_pts = [(i / 40 * 4, 0.5 + 0.08 * math.sin(i / 40 * 4 * math.pi)) for i in range(41)]
    h = stroke_height(poly_dist(x, y, stem_pts), 0.035) * 0.8
    for k in range(8):
        cx = 0.25 + 0.5 * k
        up = k % 2 == 0
        cy = 0.5 + (0.17 if up else -0.17)
        h = np.maximum(h, leaf(x, y, cx + 0.08, cy, 0.24, 0.07, 0.55 if up else -0.55) * 0.85)
    for ox in (1.0, 3.0):
        h = np.maximum(h, ellipse_height(x, y, ox, 0.3, 0.07, 0.09))
    return h


def motif_wheat(x, y):
    h = stroke_height(poly_dist(x, y, [(0.0, 0.5), (4.0, 0.5)]), 0.03) * 0.8
    for k in range(13):
        gx = 0.15 + 0.3 * k
        h = np.maximum(h, leaf(x, y, gx, 0.6, 0.13, 0.055, 0.6))
        h = np.maximum(h, leaf(x, y, gx, 0.4, 0.13, 0.055, -0.6))
    return h


def motif_vine(x, y):
    stem_pts = [(i / 40 * 4, 0.5 + 0.16 * math.sin(i / 40 * 4 * math.pi + 1.0)) for i in range(41)]
    h = stroke_height(poly_dist(x, y, stem_pts), 0.035) * 0.8
    for gx, gy in [(1.1, 0.3), (3.1, 0.3)]:
        for dx, dy in [(-0.12, 0.08), (0.0, 0.08), (0.12, 0.08), (-0.06, -0.03), (0.06, -0.03), (0.0, -0.13)]:
            h = np.maximum(h, ellipse_height(x, y, gx + dx, gy + dy, 0.065, 0.065) * 0.95)
    for lx, ly in [(0.25, 0.72), (2.25, 0.72)]:
        h = np.maximum(h, rosette_height(x, y, lx, ly, 0.24, petals=5) * 0.75)
    return h


def bone(x, y, x0, x1, cy):
    shaft = stroke_height(poly_dist(x, y, [(x0, cy), (x1, cy)]), 0.06)
    knobs = np.maximum.reduce([ellipse_height(x, y, x0, cy + dy, 0.08, 0.08) for dy in (-0.07, 0.07)] +
                              [ellipse_height(x, y, x1, cy + dy, 0.08, 0.08) for dy in (-0.07, 0.07)])
    return np.maximum(shaft, knobs)


def motif_bones(x, y):
    return np.maximum(bone(x, y, 0.3, 1.7, 0.5), bone(x, y, 2.3, 3.7, 0.5))


def motif_chain(x, y):
    h = np.zeros_like(x)
    for k in range(4):
        cx = 0.5 + k
        ry = 0.22 if k % 2 == 0 else 0.08
        h = np.maximum(h, ring_height(x, y, cx, 0.5, 0.42, ry, 0.05))
    return h


def feather(x, y, cx, cy, length, width, ang):
    """A feather: leaf-shaped vane with angled barb grooves and a raised quill."""
    vane = leaf(x, y, cx, cy, length, width, ang)
    dx, dy = x - cx, y - cy
    u = dx * math.cos(ang) + dy * math.sin(ang)
    v = -dx * math.sin(ang) + dy * math.cos(ang)
    barbs = 0.75 + 0.25 * np.cos((u - np.abs(v) * 1.2) * 70)
    quill = stroke_height(np.abs(v) + np.where(np.abs(u) < length * 0.95, 0, 1), 0.012)
    return np.maximum(vane * barbs * 0.85, quill * (vane > 0.05))


def motif_feathers(x, y):
    # Wing feathers laid in a row, each tilted and overlapping the next.
    h = np.zeros_like(x)
    for k in range(5):
        h = np.maximum(h, feather(x, y, 0.4 + 0.8 * k, 0.5, 0.48, 0.16, 0.3))
    return h


def motif_eggdart(x, y):
    h = np.zeros_like(x)
    for k in range(4):
        cx = 0.5 + k
        h = np.maximum(h, ring_height(x, y, cx, 0.5, 0.33, 0.3, 0.035) * 0.8)
        h = np.maximum(h, ellipse_height(x, y, cx, 0.5, 0.24, 0.22))
        h = np.maximum(h, polygon(x, y, [(cx + 0.5, 0.82), (cx + 0.45, 0.4), (cx + 0.5, 0.2), (cx + 0.55, 0.4)], 0.02))
    return h


def fire(x, y, cx, by, w, h):
    """A hearth fire: a tall wavy central tongue flanked by two shorter tongues curling outward."""
    centre = polygon(x, y, flame_points(cx, by, w * 0.55, h, 0.05))
    left = polygon(x, y, flame_points(cx - w * 0.3, by, w * 0.4, h * 0.62, -w * 0.25))
    right = polygon(x, y, flame_points(cx + w * 0.3, by, w * 0.4, h * 0.62, w * 0.25))
    return np.maximum.reduce([centre, left * 0.85, right * 0.85])


def motif_flames(x, y):
    h = np.zeros_like(x)
    for k in range(4):
        h = np.maximum(h, fire(x, y, 0.5 + k, 0.14, 0.62, 0.72))
    return h


def peacock_eye(x, y, cx, cy, r, h):
    outer = filled_polygon_height(x, y, teardrop_points(cx, cy, r, h), 0.03) * 0.55
    ring = ring_height(x, y, cx, cy, r * 0.62, r * 0.62, 0.03)
    dot = ellipse_height(x, y, cx, cy, r * 0.32, r * 0.32)
    return np.maximum.reduce([outer, ring, dot])


def peacock_feather(x, y, cx, cy, r, h):
    """Eye on a quill with fine barbs fanning off it, so it reads as a feather, not a drop."""
    eye = peacock_eye(x, y, cx, cy, r, h)
    quill = stroke_height(poly_dist(x, y, [(cx, cy - r), (cx, 0.1)]), 0.015) * 0.8
    barbs = np.zeros_like(x)
    for k in range(4):
        yy = cy - r - 0.05 - k * 0.06
        if yy < 0.12:
            break
        for sgn in (-1, 1):
            barbs = np.maximum(barbs, stroke_height(poly_dist(x, y, [(cx, yy), (cx + sgn * 0.16, yy + 0.07)]), 0.01) * 0.55)
    return np.maximum.reduce([eye, quill, barbs])


def motif_peacock(x, y):
    return np.maximum.reduce([peacock_feather(x, y, 0.5 + k, 0.5, 0.17, 0.34) for k in range(4)])


def motif_serpent(x, y):
    pts = [(i / 60 * 4, 0.5 + 0.22 * math.sin(i / 60 * 4 * math.pi)) for i in range(61)]
    body = stroke_height(poly_dist(x, y, pts), 0.11)
    scales = 0.85 + 0.15 * np.sin(x * 40) * np.sin(y * 40)
    return body * scales


MOTIFS.update({"rays": motif_rays, "spears": motif_spears, "olive": motif_olive, "wheat": motif_wheat, "vine": motif_vine,
               "bones": motif_bones, "chain": motif_chain, "feathers": motif_feathers, "eggdart": motif_eggdart,
               "flames": motif_flames, "peacock": motif_peacock, "serpent": motif_serpent})


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


# --------------------------------------------------- more corner emblems ---
def corner_sun(x, y):
    h = ellipse_height(x, y, 0.5, 0.5, 0.16, 0.16)
    for k in range(8):
        a = k / 8 * 2 * math.pi
        tip = (0.5 + 0.37 * math.cos(a), 0.5 + 0.37 * math.sin(a))
        l = (0.5 + 0.2 * math.cos(a - 0.3), 0.5 + 0.2 * math.sin(a - 0.3))
        r = (0.5 + 0.2 * math.cos(a + 0.3), 0.5 + 0.2 * math.sin(a + 0.3))
        h = np.maximum(h, polygon(x, y, [l, tip, r], 0.03) * 0.85)
    return h


def corner_shield(x, y):
    return np.maximum.reduce([ring_height(x, y, 0.5, 0.5, 0.3, 0.3, 0.05), ellipse_height(x, y, 0.5, 0.5, 0.12, 0.12),
                              ring_height(x, y, 0.5, 0.5, 0.2, 0.2, 0.02) * 0.6])


def corner_owl(x, y):
    head = ellipse_height(x, y, 0.5, 0.48, 0.32, 0.3) * 0.45
    eyes = np.maximum(ring_height(x, y, 0.37, 0.55, 0.1, 0.1, 0.035), ring_height(x, y, 0.63, 0.55, 0.1, 0.1, 0.035))
    pupils = np.maximum(ellipse_height(x, y, 0.37, 0.55, 0.045, 0.045), ellipse_height(x, y, 0.63, 0.55, 0.045, 0.045))
    beak = polygon(x, y, [(0.45, 0.42), (0.55, 0.42), (0.5, 0.3)], 0.02)
    ears = np.maximum(polygon(x, y, [(0.2, 0.7), (0.32, 0.72), (0.22, 0.86)], 0.02), polygon(x, y, [(0.8, 0.7), (0.68, 0.72), (0.78, 0.86)], 0.02))
    return np.maximum.reduce([head, eyes, pupils, beak, ears])


def corner_wheat(x, y):
    h = stroke_height(poly_dist(x, y, [(0.5, 0.12), (0.5, 0.88)]), 0.03)
    for k in range(5):
        gy = 0.42 + 0.1 * k
        h = np.maximum(h, leaf(x, y, 0.43, gy, 0.1, 0.045, 1.0))
        h = np.maximum(h, leaf(x, y, 0.57, gy, 0.1, 0.045, -1.0))
    return h


def corner_grapes(x, y):
    h = stroke_height(poly_dist(x, y, [(0.5, 0.72), (0.56, 0.86)]), 0.025)
    h = np.maximum(h, leaf(x, y, 0.66, 0.78, 0.13, 0.06, 0.4) * 0.8)
    for row, n in enumerate([4, 3, 2, 1]):
        for i in range(n):
            h = np.maximum(h, ellipse_height(x, y, 0.5 + (i - (n - 1) / 2) * 0.13, 0.62 - row * 0.12, 0.065, 0.065))
    return h


def corner_skull(x, y):
    cranium = ellipse_height(x, y, 0.5, 0.56, 0.26, 0.24)
    jaw = polygon(x, y, [(0.36, 0.4), (0.64, 0.4), (0.6, 0.22), (0.4, 0.22)], 0.03)
    holes = np.maximum.reduce([ellipse_height(x, y, 0.41, 0.55, 0.075, 0.065), ellipse_height(x, y, 0.59, 0.55, 0.075, 0.065),
                               polygon(x, y, [(0.47, 0.46), (0.53, 0.46), (0.5, 0.39)], 0.01)])
    return np.maximum(cranium, jaw) * (1 - 0.85 * smoothstep(0.05, 0.3, holes))


def corner_hammer(x, y):
    handle = stroke_height(poly_dist(x, y, [(0.26, 0.18), (0.62, 0.66)]), 0.045)
    c, s = math.cos(-0.93), math.sin(-0.93)
    hx, hy = 0.64, 0.68
    head = [(hx + dx * c - dy * s, hy + dx * s + dy * c) for dx, dy in [(-0.24, -0.08), (0.24, -0.08), (0.24, 0.08), (-0.24, 0.08)]]
    return np.maximum(handle, polygon(x, y, head, 0.03))


def corner_caduceus(x, y):
    staff = stroke_height(poly_dist(x, y, [(0.5, 0.12), (0.5, 0.84)]), 0.03)
    s1 = [(0.5 + 0.13 * math.sin(t / 30 * 2.5 * math.pi), 0.18 + 0.55 * t / 30) for t in range(31)]
    s2 = [(0.5 - 0.13 * math.sin(t / 30 * 2.5 * math.pi), 0.18 + 0.55 * t / 30) for t in range(31)]
    snakes = np.maximum(stroke_height(poly_dist(x, y, s1), 0.03), stroke_height(poly_dist(x, y, s2), 0.03))
    wings = np.maximum(polygon(x, y, [(0.5, 0.8), (0.16, 0.9), (0.3, 0.75)], 0.02), polygon(x, y, [(0.5, 0.8), (0.84, 0.9), (0.7, 0.75)], 0.02))
    return np.maximum.reduce([staff, snakes, wings])


def corner_lyre(x, y):
    left = [(0.38, 0.22), (0.26, 0.4), (0.26, 0.6), (0.34, 0.76), (0.26, 0.86)]
    right = [(0.62, 0.22), (0.74, 0.4), (0.74, 0.6), (0.66, 0.76), (0.74, 0.86)]
    frame = np.maximum.reduce([stroke_height(poly_dist(x, y, left), 0.04), stroke_height(poly_dist(x, y, right), 0.04),
                               stroke_height(poly_dist(x, y, [(0.36, 0.22), (0.64, 0.22)]), 0.04),
                               stroke_height(poly_dist(x, y, [(0.3, 0.72), (0.7, 0.72)]), 0.035)])
    strings = np.maximum.reduce([stroke_height(poly_dist(x, y, [(sx, 0.24), (sx, 0.7)]), 0.012) * 0.7 for sx in (0.42, 0.5, 0.58)])
    return np.maximum(frame, strings)


def corner_flame(x, y):
    # A fire on a small hearth ledge.
    ledge = polygon(x, y, [(0.22, 0.16), (0.78, 0.16), (0.74, 0.24), (0.26, 0.24)], 0.02) * 0.8
    return np.maximum(ledge, fire(x, y, 0.5, 0.24, 0.5, 0.62))


def corner_peacock(x, y):
    return peacock_feather(x, y, 0.5, 0.55, 0.2, 0.34)


def corner_coil(x, y):
    body = stroke_height(poly_dist(x, y, spiral(0.5, 0.5, 0.32, 0.06, 2.2, 0.0, clockwise=True)), 0.06)
    return np.maximum(body, ellipse_height(x, y, 0.83, 0.5, 0.07, 0.05))


CORNERS.update({"sun": corner_sun, "shield": corner_shield, "owl": corner_owl, "wheat": corner_wheat, "grapes": corner_grapes,
                "skull": corner_skull, "hammer": corner_hammer, "caduceus": corner_caduceus, "lyre": corner_lyre,
                "flame": corner_flame, "peacock": corner_peacock, "coil": corner_coil})


def relief_set(s, folder, prefix, hgt, wrap_x, seed=11):
    """Shared colouring for trims and corners: carved stone with the remains of old paint or gilding. The god's colours
    survive as faded pigment (mostly in the recesses); where it has flaked off, the bare stone shows."""
    h, w = hgt.shape
    noise = fbm(h, w, 1.6, seed)
    stone = (s["stone_dark"] + s["stone_light"]) / 2
    raised = smoothstep(0.04, 0.3, hgt)
    fig_paint = lerp(s["relief"], s["relief_hi"], smoothstep(0.6, 1.0, hgt) * (0.6 + 0.4 * noise))
    if "patina" in s:
        fig_paint = lerp(fig_paint, s["patina"], np.clip(smoothstep(0.55, 0.8, noise) * (1 - smoothstep(0.7, 1.0, hgt)), 0, 1) * 0.8)
    gnd_paint = lerp(s["relief_ground"], s["relief_ground"] * 0.75, noise)
    # Pull the paint toward the stone (CARVE), then flake it off in patches (PIGMENT_LOSS). Raised parts lose more:
    # they get rubbed, while paint survives in the recesses.
    fig_bare = np.broadcast_to(np.clip(stone * 1.12 + 0.03, 0, 1), (h, w, 3))
    gnd_bare = np.broadcast_to(stone * 0.55, (h, w, 3))
    fig = lerp(fig_paint, fig_bare, np.full((h, w), CARVE))
    gnd = lerp(gnd_paint, gnd_bare, np.full((h, w), CARVE * 0.6))
    flake = smoothstep(0.35, 0.75, fbm(h, w, 2.2, seed + 1))
    fig_loss = np.clip(PIGMENT_LOSS * 1.3 * flake + 0.15 * smoothstep(0.7, 1.0, hgt), 0, 1)
    gnd_loss = np.clip(PIGMENT_LOSS * 0.7 * flake, 0, 1)
    fig = lerp(fig, fig_bare, fig_loss)
    gnd = lerp(gnd, gnd_bare, gnd_loss)
    # Grime settles in the ground right next to the carving, like ambient occlusion.
    near = blur(hgt, 10, wrap_x)
    gnd = gnd * (1 - WEATHER * 0.55 * np.clip(near - hgt, 0, 1) * 2)[..., None]
    # Worn highs: the very tops of the carving rubbed a little lighter.
    fig = lerp(fig, np.minimum(fig * 1.12 + 0.04, 1.0), WEATHER * smoothstep(0.85, 1.0, hgt) * smoothstep(0.4, 0.8, noise))
    alb = lerp(gnd, fig, raised)
    # Old stains pooled in the recesses, and a little soot over everything.
    pool = smoothstep(0.72, 0.9, fbm(h, w, 2.4, seed + 2)) * (1 - raised) * 0.55
    alb = lerp(alb, alb * C(0.40, 0.15, 0.11), STAINS * pool)
    alb = lerp(alb, alb * C(0.25, 0.2, 0.18), STAINS * smoothstep(0.6, 0.95, fbm(h, w, 2.8, seed + 3)) * 0.3)
    save(prefix + "_Albedo", folder, rgba(alb))
    save(prefix + "_Normal", folder, normal_from_height(hgt, s.get("relief_bump", 6.0) * RELIEF_DEPTH, wrap_x=wrap_x, wrap_y=False))
    smooth = (s.get("ground_smooth", 0.25) * (1 - raised) + s.get("relief_smooth", 0.5) * raised) * MATTE_RELIEF
    metal = s.get("relief_metal", 0.0) * raised * (1 - fig_loss)          # gilding only where it survives
    save(prefix + "_Mask", folder, mask(metal, smooth * (1 - 0.5 * fig_loss)))


def hand_cut(hgt, seed):
    """Break the geometric perfection: uneven depth (so strokes vary in width once thresholded), eroded patches and
    chips knocked out of the carving."""
    h, w = hgt.shape
    depth = 0.8 + 0.4 * fbm(h, w, 2.5, seed + 4)
    erosion = smoothstep(0.55, 0.88, fbm(h, w, 1.9, seed + 5))
    chips = smoothstep(0.80, 0.86, fbm(h, w, 2.1, seed + 6))
    out = hgt * (WOBBLE * depth + (1 - WOBBLE))
    out = out * (1 - CHIPS * 0.4 * erosion)
    out = out * (1 - CHIPS * 0.9 * chips) - CHIPS * 0.05 * chips
    return np.clip(out, -0.05, 1)


def blur(a, passes, wrap_x):
    """Cheap soft blur (repeated 3x3 box); wraps along x for trims so the tile stays seamless."""
    out = a.copy()
    for _ in range(passes):
        if wrap_x:
            hx = (np.roll(out, 1, 1) + out + np.roll(out, -1, 1)) / 3
        else:
            p = np.pad(out, ((0, 0), (1, 1)), mode="edge")
            hx = (p[:, :-2] + p[:, 1:-1] + p[:, 2:]) / 3
        p = np.pad(hx, ((1, 1), (0, 0)), mode="edge")
        out = (p[:-2] + p[1:-1] + p[2:]) / 3
    return out


def trim_set(s, folder, seed):
    x, y = trim_grid()
    h, w = x.shape
    # Hand-cut wobble: the noise spans the whole strip (all four repeats), so no two repeats are quite alike.
    x = x + WOBBLE * 0.035 * (fbm(h, w, 3.2, seed + 7) - 0.5) * 2
    y = y + WOBBLE * 0.025 * (fbm(h, w, 3.2, seed + 8) - 0.5) * 2
    motif = MOTIFS[s["trim"]]
    hgt = np.maximum(wrapped(motif, x, y), fillets(y))
    relief_set(s, folder, "Trim", hand_cut(hgt, seed), True, seed)


def corner_set(s, folder, seed):
    x, y = corner_grid()
    h, w = x.shape
    x = x + WOBBLE * 0.012 * (fbm(h, w, 3.0, seed + 9) - 0.5) * 2
    y = y + WOBBLE * 0.012 * (fbm(h, w, 3.0, seed + 10) - 0.5) * 2
    # The rim breaks unevenly, so the block's edges read as chipped rather than machined.
    edge = np.minimum.reduce([x, y, 1 - x, 1 - y]) - CHIPS * 0.03 * fbm(h, w, 1.8, seed + 11)
    rim = np.maximum(smoothstep(0.1, 0.05, edge), 0)
    hgt = np.maximum(CORNERS[s["corner"]](x, y), rim * 0.8)
    relief_set(s, folder, "Corner", hand_cut(hgt, seed + 20), False, seed + 50)


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
    "Apollo": dict(
        stone_dark=C(0.80, 0.70, 0.50), stone_light=C(0.94, 0.88, 0.72), vein=C(0.72, 0.55, 0.28), vein_amount=0.5, vein_width=0.05,
        fine_veins=0.3, warp=1.2, vein_dir=(1, 2), stone_smooth=0.5, panel_calm=0.7,
        trim="rays", corner="sun", relief=C(0.86, 0.64, 0.24), relief_hi=C(1.0, 0.88, 0.52), relief_ground=C(0.55, 0.40, 0.18),
        relief_metal=0.5, relief_smooth=0.6, relief_bump=6.0),
    "Ares": dict(
        stone_dark=C(0.30, 0.08, 0.06), stone_light=C(0.50, 0.15, 0.10), vein=C(0.14, 0.04, 0.03), vein_amount=0.65, vein_width=0.07,
        fine_veins=0.4, warp=1.6, vein_dir=(2, 1), stone_smooth=0.45, panel_calm=0.75, panel_tone=0.9, panel_tint=0.05,
        trim="spears", corner="shield", relief=C(0.12, 0.10, 0.09), relief_hi=C(0.26, 0.22, 0.20), relief_ground=C(0.62, 0.30, 0.18),
        relief_metal=0.3, relief_smooth=0.45, relief_bump=6.0),
    "Athena": dict(
        stone_dark=C(0.30, 0.33, 0.30), stone_light=C(0.48, 0.51, 0.46), vein=C(0.78, 0.80, 0.74), vein_amount=0.55, vein_width=0.05,
        fine_veins=0.3, warp=1.0, vein_dir=(1, 1), stone_smooth=0.45, panel_calm=0.75, panel_tone=0.9,
        trim="olive", corner="owl", relief=C(0.70, 0.72, 0.70), relief_hi=C(0.90, 0.92, 0.90), relief_ground=C(0.18, 0.22, 0.18),
        relief_metal=0.55, relief_smooth=0.55, relief_bump=6.0),
    "Demeter": dict(
        stone_dark=C(0.70, 0.55, 0.30), stone_light=C(0.86, 0.74, 0.48), vein=C(0.55, 0.40, 0.18), vein_amount=0.35, vein_width=0.05,
        fine_veins=0.2, warp=1.4, vein_dir=(1, 3), stone_smooth=0.3, panel_calm=0.7,
        trim="wheat", corner="wheat", relief=C(0.88, 0.72, 0.36), relief_hi=C(0.98, 0.88, 0.58), relief_ground=C(0.34, 0.38, 0.16),
        relief_metal=0.2, relief_smooth=0.4, relief_bump=6.0),
    "Dionysus": dict(
        stone_dark=C(0.24, 0.08, 0.14), stone_light=C(0.40, 0.15, 0.24), vein=C(0.62, 0.40, 0.48), vein_amount=0.55, vein_width=0.06,
        fine_veins=0.35, warp=1.3, vein_dir=(1, 2), stone_smooth=0.5, panel_calm=0.75, panel_tone=0.9, panel_tint=0.05,
        trim="vine", corner="grapes", relief=C(0.62, 0.64, 0.34), relief_hi=C(0.80, 0.80, 0.50), relief_ground=C(0.10, 0.03, 0.06),
        relief_metal=0.0, relief_smooth=0.4, relief_bump=6.0),
    "Hades": dict(
        stone_dark=C(0.09, 0.08, 0.08), stone_light=C(0.20, 0.18, 0.18), vein=C(0.42, 0.36, 0.32), vein_amount=0.55, vein_width=0.04,
        fine_veins=0.3, warp=1.0, vein_dir=(2, 1), stone_smooth=0.65, panel_calm=0.8, panel_tone=1.1,
        trim="bones", corner="skull", relief=C(0.82, 0.77, 0.66), relief_hi=C(0.95, 0.91, 0.82), relief_ground=C(0.05, 0.04, 0.04),
        relief_metal=0.0, relief_smooth=0.35, relief_bump=6.0),
    "Hephaestus": dict(
        stone_dark=C(0.24, 0.23, 0.23), stone_light=C(0.40, 0.38, 0.37), vein=C(0.95, 0.42, 0.08), vein_amount=1.0, vein_width=0.06,
        fine_veins=0.6, warp=1.8, vein_dir=(1, 2), stone_smooth=0.35, panel_calm=0.55, panel_tone=0.85,
        trim="chain", corner="hammer", relief=C(0.62, 0.42, 0.22), relief_hi=C(0.86, 0.64, 0.38), relief_ground=C(0.08, 0.06, 0.05),
        relief_metal=0.7, relief_smooth=0.4, relief_bump=6.0),
    "Hermes": dict(
        stone_dark=C(0.34, 0.42, 0.48), stone_light=C(0.50, 0.58, 0.63), vein=C(0.82, 0.84, 0.82), vein_amount=0.5, vein_width=0.05,
        fine_veins=0.35, warp=1.8, vein_dir=(2, 1), stone_smooth=0.45, panel_calm=0.7, panel_lift=0.65,
        trim="feathers", corner="caduceus", relief=C(0.92, 0.88, 0.78), relief_hi=C(1.0, 0.97, 0.90), relief_ground=C(0.16, 0.28, 0.40),
        relief_metal=0.0, relief_smooth=0.45, relief_bump=6.0),
    "Muse": dict(
        stone_dark=C(0.84, 0.78, 0.66), stone_light=C(0.95, 0.91, 0.82), vein=C(0.20, 0.16, 0.14), vein_amount=0.45, vein_width=0.025,
        fine_veins=0.25, warp=1.1, vein_dir=(1, 2), stone_smooth=0.4, panel_calm=0.75,
        trim="eggdart", corner="lyre", relief=C(0.22, 0.18, 0.16), relief_hi=C(0.38, 0.32, 0.28), relief_ground=C(0.74, 0.66, 0.52),
        relief_metal=0.0, relief_smooth=0.4, relief_bump=5.0),
    "Hestia": dict(
        stone_dark=C(0.55, 0.30, 0.18), stone_light=C(0.72, 0.46, 0.30), vein=C(0.90, 0.74, 0.56), vein_amount=0.35, vein_width=0.05,
        fine_veins=0.2, warp=1.6, vein_dir=(1, 1), stone_smooth=0.3, panel_calm=0.7,
        trim="flames", corner="flame", relief=C(0.92, 0.80, 0.60), relief_hi=C(1.0, 0.92, 0.78), relief_ground=C(0.30, 0.14, 0.08),
        relief_metal=0.0, relief_smooth=0.35, relief_bump=6.0),
    "Hera": dict(
        stone_dark=C(0.10, 0.15, 0.30), stone_light=C(0.18, 0.25, 0.44), vein=C(0.82, 0.64, 0.32), vein_amount=0.5, vein_width=0.025,
        fine_veins=0.45, warp=1.4, vein_dir=(1, 2), stone_smooth=0.5, panel_calm=0.75, panel_tone=0.9,
        trim="peacock", corner="peacock", relief=C(0.84, 0.64, 0.26), relief_hi=C(0.98, 0.84, 0.48), relief_ground=C(0.06, 0.12, 0.30),
        relief_metal=0.5, relief_smooth=0.6, relief_bump=6.0),
    "OldOnes": dict(
        stone_dark=C(0.20, 0.17, 0.12), stone_light=C(0.34, 0.30, 0.22), vein=C(0.12, 0.10, 0.07), vein_amount=0.6, vein_width=0.06,
        fine_veins=0.5, warp=2.0, vein_dir=(1, 1), stone_smooth=0.5, panel_calm=0.8, panel_tone=1.0,
        trim="serpent", corner="coil", relief=C(0.36, 0.56, 0.44), relief_hi=C(0.56, 0.74, 0.60), relief_ground=C(0.07, 0.06, 0.04),
        relief_metal=0.3, relief_smooth=0.5, relief_bump=6.0),
}

for i, (name, style) in enumerate(STYLES.items()):
    if ONLY and name not in ONLY:
        continue
    folder = os.path.join(OUT, name)
    os.makedirs(folder, exist_ok=True)
    stone_set(style, folder, 100 + i * 10)
    trim_set(style, folder, 200 + i * 10)
    corner_set(style, folder, 300 + i * 10)
    print("altar style", name, "->", folder)
