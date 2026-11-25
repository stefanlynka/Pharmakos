"""Toolkit for painting the combat board backgrounds (see boards.py for the per-encounter recipes).

Everything is worked out in board world units (the same x/y the combat board uses: lanes are 60 x 10, the player lane
spans y -0.65..9.35, the enemy lane 11.92..21.92), so the ground and the lanes share noise fields and features line up
across them. Two canvases:
  ground: x -68..68, y -24..44 (everything the combat camera can see of the board plane), 4096 x 2048
  lanes:  x -30..30, two lanes stacked (enemy on top), 3072 x 1024

Plain Python 3 with numpy + Pillow (no Blender needed).
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

F32 = np.float32

GROUND_RECT = (-68.0, 68.0, -24.0, 44.0)     # x0, x1, y0, y1
GROUND_SIZE = (4096, 2048)
LANE_RECT = (-30.0, 30.0, 0.0, 20.0)         # lane canvas: local y 0..10 = player lane, 10..20 = enemy lane
LANE_SIZE = (3072, 1024)

PLAYER_LANE = (-30.0, 30.0, -0.65, 9.35)
ENEMY_LANE = (-30.0, 30.0, 11.92, 21.92)   # moved down 2 to make room for back edges (Troy's wall)
ARENA = (-30.0, 30.0, -0.65, 21.92)           # both lanes and the gap between them


# ------------------------------------------------------------------ canvas ---
class Canvas:
    """A raster over a world-space rectangle. X/Y hold each pixel's world coordinates (row 0 = top = y1)."""

    def __init__(self, rect, size, lane_canvas=False):
        self.x0, self.x1, self.y0, self.y1 = rect
        self.w, self.h = size
        self.ppu = self.w / (self.x1 - self.x0)
        xs = (self.x0 + (np.arange(self.w, dtype=F32) + 0.5) / self.ppu).astype(F32)
        ys = (self.y1 - (np.arange(self.h, dtype=F32) + 0.5) / self.ppu).astype(F32)
        self.X, self.Y = np.meshgrid(xs, ys)
        self.lane_canvas = lane_canvas
        if lane_canvas:
            # lane canvas local y 0..20 -> board world y (player lane 0..10, enemy lane 10..20)
            ly = self.Y
            self.WY = np.where(ly < 10.0, PLAYER_LANE[2] + ly, ENEMY_LANE[2] + (ly - 10.0)).astype(F32)
        else:
            self.WY = self.Y
        self.WX = self.X

    def px(self, x, y):
        return (x - self.x0) * self.ppu, (self.y1 - y) * self.ppu

    def zeros(self):
        return np.zeros((self.h, self.w), F32)


def lerp(a, b, t):
    t = np.asarray(t, F32)
    if t.ndim == 2:
        t = t[..., None]
    return (np.asarray(a, F32) * (1 - t) + np.asarray(b, F32) * t).astype(F32)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return (t * t * (3 - 2 * t)).astype(F32)


def C(*rgb):
    return np.array(rgb, F32)


def hexc(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], F32)


# ------------------------------------------------------------------- noise ---
class Noise:
    """Smooth value noise and helpers in world units. Deterministic per seed."""

    N = 1024

    def __init__(self, seed):
        self.seed = seed
        r = np.random.default_rng(seed)
        self.tab = r.random((self.N, self.N), dtype=F32)
        self.tab2 = r.random((self.N, self.N), dtype=F32)

    def _lookup(self, tab, ix, iy):
        return tab[iy % self.N, ix % self.N]

    def value(self, X, Y, scale, offset=0):
        fx = X / scale + 17.31 * offset
        fy = Y / scale + 9.17 * offset
        ix = np.floor(fx).astype(np.int64)
        iy = np.floor(fy).astype(np.int64)
        tx = (fx - ix).astype(F32)
        ty = (fy - iy).astype(F32)
        tx = tx * tx * tx * (tx * (tx * 6 - 15) + 10)
        ty = ty * ty * ty * (ty * (ty * 6 - 15) + 10)
        a = self._lookup(self.tab, ix, iy)
        b = self._lookup(self.tab, ix + 1, iy)
        c = self._lookup(self.tab, ix, iy + 1)
        d = self._lookup(self.tab, ix + 1, iy + 1)
        return (a + (b - a) * tx + (c - a) * ty + (a - b - c + d) * tx * ty).astype(F32)

    def fbm(self, X, Y, scale, octaves=5, gain=0.5, lac=2.03, offset=0):
        total = np.zeros(X.shape, F32)
        amp, norm, s = 1.0, 0.0, scale
        for o in range(octaves):
            total += amp * self.value(X, Y, s, offset + o * 3)
            norm += amp
            amp *= gain
            s /= lac
        return (total / norm).astype(F32)

    def ridged(self, X, Y, scale, octaves=4, offset=0):
        total = np.zeros(X.shape, F32)
        amp, norm, s = 1.0, 0.0, scale
        for o in range(octaves):
            n = 1 - np.abs(self.value(X, Y, s, offset + o * 5) * 2 - 1)
            total += amp * n * n
            norm += amp
            amp *= 0.5
            s /= 2.0
        return (total / norm).astype(F32)

    def warp(self, X, Y, scale, amount, offset=50):
        dx = (self.fbm(X, Y, scale, 3, offset=offset) - 0.5) * 2 * amount
        dy = (self.fbm(X, Y, scale, 3, offset=offset + 7) - 0.5) * 2 * amount
        return X + dx, Y + dy

    def cellhash(self, ix, iy, k=0):
        tab = self.tab if k % 2 == 0 else self.tab2
        return self._lookup(tab, ix * 7 + k * 131, iy * 13 + k * 71)

    def voronoi(self, X, Y, size, jitter=0.85):
        """Jittered-grid Voronoi. Returns F1, F2 (world units), cell id hash (0..1) and the nearest seed point."""
        gx = X / size
        gy = Y / size
        cx = np.floor(gx).astype(np.int64)
        cy = np.floor(gy).astype(np.int64)
        f1 = np.full(X.shape, 1e9, F32)
        f2 = np.full(X.shape, 1e9, F32)
        cid = np.zeros(X.shape, F32)
        px = np.zeros(X.shape, F32)
        py = np.zeros(X.shape, F32)
        for oy in (-1, 0, 1):
            for ox in (-1, 0, 1):
                nx = cx + ox
                ny = cy + oy
                sx = (nx + 0.5 + (self.cellhash(nx, ny, 0) - 0.5) * jitter) * size
                sy = (ny + 0.5 + (self.cellhash(nx, ny, 1) - 0.5) * jitter) * size
                d = np.sqrt((X - sx) ** 2 + (Y - sy) ** 2).astype(F32)
                closer = d < f1
                f2 = np.where(closer, f1, np.minimum(f2, d))
                f1 = np.where(closer, d, f1)
                cid = np.where(closer, self.cellhash(nx, ny, 2), cid)
                px = np.where(closer, sx, px)
                py = np.where(closer, sy, py)
        return f1, f2, cid, px, py


def brush(n, X, Y, size=0.9, stretch=3.0, offset=300):
    """Painterly stroke texture (0..1): overlapping dabs, each with its own stroke direction (drifting across the
    board), like the oil-paint grain of the card art. Use for a few percent of tone variation."""
    out = np.zeros(X.shape, F32)
    wsum = np.zeros(X.shape, F32)
    for k in range(3):
        ox, oy = k * 5.3, k * 2.9
        f1, f2, cid, px, py = n.voronoi(X + ox, Y + oy, size * 2.2, 0.9)
        ang = n.fbm(px, py, 14, 3, offset=offset) * math.pi * 2 + (cid - 0.5) * 1.6     # one direction per dab
        ca, sa = np.cos(ang), np.sin(ang)
        dx, dy = X + ox - px, Y + oy - py
        u = (dx * ca + dy * sa) / stretch + cid * 31.0
        v = -dx * sa + dy * ca + cid * 17.0
        st = n.value(u, v, size * 0.35, offset + 1 + k) * 0.7 + n.value(u, v, size * 0.15, offset + 4 + k) * 0.3
        w = np.clip(1 - f1 / (size * 1.6), 0.0, 1) ** 2 + 1e-3
        out += st * w
        wsum += w
    return (out / wsum).astype(F32)


def norm01(a, lo=1, hi=99):
    l, h = np.percentile(a, [lo, hi])
    return np.clip((a - l) / max(h - l, 1e-6), 0, 1).astype(F32)


# ---------------------------------------------------------------- shapes ---
def sd_box(X, Y, rect, radius=0.0):
    """Signed distance (world units) to a rounded rectangle (x0, x1, y0, y1); negative inside."""
    x0, x1, y0, y1 = rect
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2 - radius, (y1 - y0) / 2 - radius
    qx = np.abs(X - cx) - hx
    qy = np.abs(Y - cy) - hy
    out = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2)
    inside = np.minimum(np.maximum(qx, qy), 0)
    return (out + inside - radius).astype(F32)


class Layer:
    """Supersampled drawing layer for shapes (bones, leaves, shards...). Draw in world units, read back as 0..1."""

    def __init__(self, canvas, ss=2):
        self.c = canvas
        self.ss = ss
        self.img = Image.new('L', (canvas.w * ss, canvas.h * ss), 0)
        self.d = ImageDraw.Draw(self.img)

    def P(self, x, y):
        px, py = self.c.px(x, y)
        return px * self.ss, py * self.ss

    def poly(self, pts, v=255):
        self.d.polygon([self.P(x, y) for x, y in pts], fill=int(v))

    def ellipse(self, x, y, rx, ry, v=255, angle=0.0, n=28):
        pts = []
        ca, sa = math.cos(angle), math.sin(angle)
        for i in range(n):
            t = 2 * math.pi * i / n
            ex, ey = rx * math.cos(t), ry * math.sin(t)
            pts.append((x + ex * ca - ey * sa, y + ex * sa + ey * ca))
        self.poly(pts, v)

    def line(self, pts, width, v=255):
        self.d.line([self.P(x, y) for x, y in pts], fill=int(v), width=max(1, int(width * self.c.ppu * self.ss)), joint='curve')
        r = width / 2
        for x, y in (pts[0], pts[-1]):
            self.ellipse(x, y, r, r, v, n=12)

    def read(self, blur_units=0.0):
        im = self.img.resize((self.c.w, self.c.h), Image.LANCZOS) if self.ss > 1 else self.img
        if blur_units > 0:
            im = im.filter(ImageFilter.GaussianBlur(blur_units * self.c.ppu))
        return (np.asarray(im, F32) / 255.0).astype(F32)


def blurf(a, units, canvas):
    """Higher-precision blur: separable box blurs (3 passes ~ gaussian) in numpy."""
    r = max(1, int(units * canvas.ppu / 1.7))
    out = a.astype(F32)
    for _ in range(3):
        out = _box(out, r, 1)
        out = _box(out, r, 0)
    return out


def _box(a, r, axis):
    c = np.cumsum(a, axis=axis, dtype=np.float64)
    n = a.shape[axis]
    pad_shape = list(a.shape)
    pad_shape[axis] = 1
    c = np.concatenate([np.zeros(pad_shape), c], axis=axis)
    idx_hi = np.clip(np.arange(n) + r + 1, 0, n)
    idx_lo = np.clip(np.arange(n) - r, 0, n)
    if axis == 1:
        s = c[:, idx_hi] - c[:, idx_lo]
        cnt = (idx_hi - idx_lo)[None, :]
    else:
        s = c[idx_hi, :] - c[idx_lo, :]
        cnt = (idx_hi - idx_lo)[:, None]
    return (s / cnt).astype(F32)


# ---------------------------------------------------------------- shading ---
def normals(height, canvas, strength=1.0):
    """Height in world units -> normal vectors (h, w, 3), x right / y up in board world space."""
    step = 1.0 / canvas.ppu
    gy, gx = np.gradient(height.astype(F32), step)
    nx = -gx * strength
    ny = gy * strength           # rows run downward in world y
    nz = np.ones_like(nx)
    n = np.stack([nx, ny, nz], -1)
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n.astype(F32)


def cavity(height, canvas, radius_units, amount):
    """Darkening for recesses: how far below its blurred surroundings each point sits."""
    hb = blurf(height, radius_units, canvas)
    return np.clip((hb - height) * amount, 0, 1).astype(F32)


def desaturate(col, amount):
    g = (col @ np.array((0.3, 0.59, 0.11), F32))[..., None]
    return (g + (col - g) * (1 - amount)).astype(F32)


# ------------------------------------------------------------------ output ---
def _rgb8(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8))


def to_srgb(lin):
    return np.where(lin <= 0.0031308, lin * 12.92, 1.055 * np.power(np.clip(lin, 0, None), 1 / 2.4) - 0.055).astype(F32)


def save_set(folder, prefix, canvas, albedo, normal, smooth, emission=None, rotate=False, mask_size=None, normal_scale=0.5):
    """Writes <prefix>_Albedo.jpg, _Normal.jpg, _Mask.png (A = smoothness) and optional _Emission.jpg.
    Albedo/emission are authored in display (sRGB) values. rotate=True turns everything 180 degrees and flips the
    normal's x/y, for the ground plane (its UVs run toward -x and -y)."""
    os.makedirs(folder, exist_ok=True)
    alb = albedo
    nrm = normal
    sm = smooth
    em = emission
    if rotate:
        alb = alb[::-1, ::-1]
        nrm = nrm[::-1, ::-1].copy()
        nrm[..., 0] *= -1
        nrm[..., 1] *= -1
        sm = sm[::-1, ::-1]
        em = em[::-1, ::-1] if em is not None else None
    _rgb8(alb).save(os.path.join(folder, prefix + '_Albedo.jpg'), quality=93, subsampling=0)
    nimg = _rgb8(nrm * 0.5 + 0.5)
    if normal_scale != 1.0:      # fine relief barely shows at board distance; half size keeps the repo light
        nimg = nimg.resize((int(canvas.w * normal_scale), int(canvas.h * normal_scale)), Image.LANCZOS)
    nimg.save(os.path.join(folder, prefix + '_Normal.jpg'), quality=93, subsampling=0)
    ms = mask_size or (canvas.w // 4, canvas.h // 4)
    m = np.zeros(sm.shape + (4,), np.uint8)
    m[..., 3] = (np.clip(sm, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(m, 'RGBA').resize(ms, Image.LANCZOS).save(os.path.join(folder, prefix + '_Mask.png'))
    if em is not None:
        _rgb8(em).save(os.path.join(folder, prefix + '_Emission.jpg'), quality=92)
    elif os.path.exists(os.path.join(folder, prefix + '_Emission.jpg')):
        os.remove(os.path.join(folder, prefix + '_Emission.jpg'))


def preview(path, canvas, albedo, normal, light=(-0.35, 0.45, 0.82), scale=0.25):
    """Quick lit preview (lambert from one direction) for eyeballing without Unity."""
    L = np.array(light, F32)
    L /= np.linalg.norm(L)
    shade = np.clip(normal @ L, 0, 1)[..., None] * 0.85 + 0.15
    img = _rgb8(albedo * shade)
    img = img.resize((int(canvas.w * scale), int(canvas.h * scale)), Image.LANCZOS)
    img.save(path)
