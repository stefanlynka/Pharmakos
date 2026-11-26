"""Shared pieces for boards in the generated-art style (Troy is the reference; see Docs/Boards.md, "Generated art").

Floors: a generated surface tile (Tools/Boards/gen) mixed at two scales and angles through noise so no repeat reads,
normalised to a target brightness, then the recipe's own tone, wear and shadows, and `edge_fade` toward the screen
edges. Lanes: the same tile, calmer.

Back edges: real geometry lit by the scene. `rock_face` builds a natural rock wall from a generated rock tile (the mesh
is displaced by the tile's own light/dark and by large noise forms, so the stone catches the spotlights), running out
of the top of the frame. `falloff` gives the light that only the foot catches: an emission share that fades with
height and toward the sides, and an albedo that darkens upward, so every board fades to black at the top the same way
(the spec's top-strip rule, checked by check.py).
"""
import math
import os

import numpy as np

from boardkit import *  # noqa
from boardgen import F32, blurf, lerp, smoothstep

GEN = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'gen')
W3 = np.array((0.2126, 0.7152, 0.0722), F32)


def tile(name):
    """A generated tile from Tools/Boards/gen, made seamless."""
    return make_tileable(load_rgb(os.path.join(GEN, name)))


def lum(img):
    return (img @ W3).astype(F32)


def norm_lum(img, target):
    return (img * (target / max(float(lum(img).mean()), 1e-4))).astype(F32)


def two_scale(n, t, X, Y, size, offset=11, rot=(0.3, 2.2)):
    """One tile sampled at two scales and angles, mixed through noise so its repeat doesn't read."""
    s1 = sample_tiled(t, X, Y, size, rot[0])
    s2 = sample_tiled(t, X + 31.0, Y + 17.0, size * 1.42, rot[1])
    return lerp(s1, s2, smoothstep(0.35, 0.65, n.fbm(X, Y, 10, 3, offset=offset)))


def tile_height(img, canvas, amount):
    """Fine height from a sampled tile's own light and dark (light = raised)."""
    l = lum(img)
    return (blurf(l - float(l.mean()), 0.03, canvas) * amount).astype(F32)


def edge_fade(X, Y, alb, side=(30.0, 62.0), near=(-6.0, -22.0), amount=(0.55, 0.4), cap=0.75):
    """Darkens the ground toward the left/right screen edges and the near edge, so it sinks into darkness beyond
    the pool of light (the corners fall to black)."""
    fade = smoothstep(side[0], side[1], np.abs(X)) * amount[0] + smoothstep(near[0], near[1], Y) * amount[1]
    return (alb * (1 - np.clip(fade, 0, cap))[..., None]).astype(F32)


def tile_lanes(n, lane, t, size=6.0, mean=0.2, calm=0.35, height=0.05, offset=970):
    """Lanes from the board's tile: one scale, blended toward its mean colour (packed and calmer than the ground)."""
    LX, LY = lane.WX, lane.WY
    l1 = sample_tiled(t, LX, LY, size, 0.3)
    lalb = lerp(l1, np.broadcast_to(l1.reshape(-1, 3).mean(0), l1.shape), calm)
    lalb = norm_lum(lalb, mean)
    lalb = (lalb * (0.9 + 0.2 * n.fbm(LX, LY, 7, 3, offset=offset))[..., None]).astype(F32)
    lhgt = tile_height(lalb, lane, height)
    return lalb, lhgt, np.full(LX.shape, 0.1, F32)


def falloff(n, X, H, top=6.0, foot=0.4, side=(8.0, 55.0), glow=0.7, floor=0.2, offset=31):
    """Back-edge light as two multipliers over a material's elevation canvas (X across, H = height):
    (emission share of the albedo, albedo multiplier). Only the foot glows; the stone darkens upward and toward the
    sides. Troy: top 6, glow 0.7. Lower `top` for geometry that climbs the screen faster (steep faces)."""
    lit = 1 - smoothstep(foot, top, H)
    sd = (1 - smoothstep(side[0], side[1], np.abs(X))) * (0.55 + 0.45 * n.fbm(X, H * 0.2, 9, 3, offset=offset))
    em = (lit * sd * glow).astype(F32)
    am = ((floor + (1 - floor) * lit) * (0.5 + 0.5 * sd)).astype(F32)
    return em, am


class RockFace:
    """A natural rock wall standing along the far edge and running out of the top of the frame. Its surface is a
    generated rock tile; the mesh is pushed back by the tile's dark parts and by large noise forms, so the scene's
    light rakes across real relief. Build with `mesh()`, then paint with `paint()` (which returns albedo, height,
    emission share and albedo multiplier for save_material)."""

    def __init__(self, mat, n, rock, foot_y, top=12.0, lean=0.45, bulk=1.6, bulk_scale=6.0, disp=0.5, size=5.0,
                 offset=300, foot_wave=0.8, recesses=()):
        """foot_y: y of the foot (a number or a function of x). recesses: [(x, h, rx, rh, depth)] ellipses pushed
        deep into the face (a cave mouth, a dark cleft)."""
        self.mat, self.n, self.rock = mat, n, rock
        self.foot = foot_y if callable(foot_y) else (lambda x, v=foot_y: v)
        self.top, self.lean, self.bulk, self.bulk_scale = top, lean, bulk, bulk_scale
        self.disp, self.size, self.offset, self.foot_wave = disp, size, offset, foot_wave
        self.recesses = recesses
        c = mat.elev_canvas()
        self.canvas = c
        X, H = c.X, c.Y
        self.alb = two_scale(n, rock, X, H, size, offset=offset)
        l = lum(self.alb)
        fine = l - float(l.mean())
        self.big = (n.fbm(X, H, bulk_scale, 4, offset=offset + 1) - 0.5).astype(F32)
        # D: how far each point sits back (+y), in units: dark rock recedes, light rock stands out
        D = -blurf(fine, 0.25, c) * disp * 2.5 + self.big * bulk
        for (rx0, rh0, rx, rh, dep) in recesses:
            inside = np.clip(1 - ((X - rx0) / rx) ** 2 - ((H - rh0) / rh) ** 2, 0, 1)
            D = D + dep * np.minimum(1.0, inside * 3)
        self.D = D.astype(F32)
        self.fine = fine.astype(F32)
        self.Dm = blurf(self.D, 0.08, c)

    def depth(self, x, h):
        from backedge import HALF, PPU
        c = self.canvas
        ix = int(min(max((x + HALF) * PPU, 0), c.w - 1))
        iy = int(min(max((self.mat.tex_h - h) * PPU, 0), c.h - 1))
        return h * self.lean + float(self.Dm[iy, ix])

    def mesh(self, be, x0=-75.0, x1=75.0, step=0.2, h_step=0.2):
        n, o = self.n, self.offset
        wave = lambda x: self.foot_wave * float(n.fbm(np.array([[x]], F32), np.array([[0.0]], F32), 9, 3, offset=o + 2)[0, 0] - 0.5)
        be.relief(self.mat, x0, x1, lambda x: self.foot(x) + wave(x), lambda x: self.top, self.depth, step=step,
                  h_step=h_step)

    def paint(self, alb=None, top=6.0, glow=0.7, side=(8.0, 55.0), floor=0.2, cavity_dark=0.5):
        """Returns (albedo, height, emission, smoothness) ready for save_material."""
        c = self.canvas
        X, H = c.X, c.Y
        a = self.alb if alb is None else alb
        # crevices (points sitting back from their surroundings) go dark
        cav = np.clip((self.D - blurf(self.D, 0.6, c)) * 2.0, 0, 1)
        a = a * (1 - cav * cavity_dark)[..., None]
        for (rx0, rh0, rx, rh, dep) in self.recesses:
            inside = np.clip(1 - ((X - rx0) / rx) ** 2 - ((H - rh0) / rh) ** 2, 0, 1)
            a = a * (1 - smoothstep(0.0, 0.3, inside) * 0.92)[..., None]
        em_k, am = falloff(self.n, X, H, top=top, glow=glow, side=side, floor=floor)
        em = a * em_k[..., None]
        a = a * am[..., None]
        hgt = (-self.D * 0.6 + self.fine * 0.12).astype(F32)
        return a.astype(F32), hgt, em.astype(F32), np.full(X.shape, 0.12, F32)


def boulder(be, mat, n, x, y, r, h, seed=0.0, segments=18):
    """A lumpy boulder lying at (x, y), radius r and height h (a lathe with noise wobble)."""
    prof = [(0.0, r * 0.75), (h * 0.15, r * 0.97), (h * 0.45, r), (h * 0.75, r * 0.82), (h * 0.95, r * 0.4), (h, 0.0)]
    wob = lambda t, hh: 0.82 + 0.36 * n.value(np.array([[math.cos(t) * 1.3 + seed]], F32), np.array([[math.sin(t) * 1.3 + hh * 0.7 + seed]], F32), 1.0, 46)[0, 0]
    be.lathe(mat, x, y, prof, segments=segments, wobble=wob)


class AshlarWall:
    """A wall of irregular ashlar standing along the far edge (after Troy's wall): courses of varying height with
    stepped beds, blocks of varying width with slightly slanted joints, each block's face bulging or standing proud
    and its surface the generated stone tile sampled at its own spot. One height map (D: how far each point sits
    back) drives both the mesh and the normal map. `openings` cut dark passages through it: [(x, width)].
    Build with `mesh()`, paint with `paint()`."""

    def __init__(self, mat, n, rng, stone, height, course=(0.95, 1.5), width=(2.0, 5.5), slant=0.3, beds=0.2,
                 bulge=(0.03, 0.12), proud=(-0.07, 0.06), block_size=3.2, openings=(), opening_depth=7.0):
        from backedge import HALF
        self.mat, self.n, self.height, self.openings, self.opening_depth = mat, n, height, openings, opening_depth
        c = mat.elev_canvas()
        self.canvas = c
        X, H = c.X, c.Y
        forced = []
        for ox, ow in openings:
            forced += [ox - ow / 2, ox + ow / 2]
        lim = HALF + 6
        courses = []
        h = 0.0
        while h < height - 1e-3:
            ch = float(rng.uniform(*course))
            if height - (h + ch) < course[0] * 0.7:
                ch = height - h
            courses.append((h, h + ch))
            h += ch
        layout = []
        for h0, h1 in courses:
            joints = []
            x = -lim + float(rng.uniform(0, 2))
            while x < lim:
                joints.append(x)
                x += float(rng.uniform(*width))
            joints = [j for j in joints if not forced or min(abs(j - o) for o in forced) > width[0] * 0.45]
            sl = [float(rng.uniform(-slant, slant)) for _ in joints] + [0.0] * len(forced)
            order = np.argsort(joints + forced)
            layout.append((h0, h1, np.array(joints + forced, F32)[order], np.array(sl, F32)[order]))
        nblocks = sum(len(cc[2]) + 1 for cc in layout)
        bval = rng.uniform(0.8, 1.12, nblocks).astype(F32)
        bbulge = rng.uniform(*bulge, nblocks).astype(F32)
        bproud = rng.uniform(*proud, nblocks).astype(F32)
        box_, boy = rng.uniform(0, 60, nblocks).astype(F32), rng.uniform(0, 60, nblocks).astype(F32)
        xs = X[0]
        bnd = [np.zeros_like(xs)]
        for ci, (h0, h1, joints, slants) in enumerate(layout):
            if ci == len(layout) - 1:
                bnd.append(np.full_like(xs, height))
            else:
                idx = np.clip(np.searchsorted(joints, xs), 1, len(joints) - 1)
                toff = rng.uniform(-beds, beds, len(joints) + 1).astype(F32)
                bnd.append(np.maximum(h1 + toff[idx], bnd[-1] + 0.45))
        starts = np.cumsum([0] + [len(cc[2]) + 1 for cc in layout])
        edge = np.full(X.shape, 9.0, F32)
        bid = np.full(X.shape, -1, np.int32)
        for r in np.where(H[:, 0] <= height + 1e-3)[0]:
            hr = float(H[r, 0])
            xr = X[r]
            for ci, (h0, h1, joints, slants) in enumerate(layout):
                lo, hi = bnd[ci], bnd[ci + 1]
                top_course = ci == len(layout) - 1
                cols = (hr >= lo) & ((hr < hi) | top_course)
                if not cols.any():
                    continue
                t = (hr - (h0 + h1) / 2) / (h1 - h0)
                jx = joints + slants * t
                xc = xr[cols]
                idx = np.clip(np.searchsorted(jx, xc), 1, len(jx) - 1)
                dl = xc - jx[idx - 1]
                dr = jx[idx] - xc
                dh = hr - lo[cols] if top_course else np.minimum(hr - lo[cols], hi[cols] - hr)
                edge[r, cols] = np.minimum(np.minimum(dl, dr), dh)
                bid[r, cols] = starts[ci] + idx
        inwall = bid >= 0
        b = np.where(inwall, bid, 0)
        joint = 1 - smoothstep(0.0, 0.13, edge)
        chip = smoothstep(0.6, 0.78, n.fbm(X, H, 0.35, 3, offset=3)) * (1 - smoothstep(0.04, 0.4, edge))
        D = np.where(inwall, 0.15 * joint + 0.07 * chip - bbulge[b] * smoothstep(0.05, 0.9, edge)
                     + bproud[b] * smoothstep(0.0, 0.12, edge), 0.0).astype(F32)
        self.hole = np.zeros(X.shape, F32)
        for ox, ow in openings:
            self.hole = np.maximum(self.hole, (np.abs(X - ox) < ow / 2).astype(F32))
        D = np.where(self.hole > 0, opening_depth, D).astype(F32)
        self.D = D
        self.Dm = blurf(np.where(self.hole > 0, 0.0, D).astype(F32), 0.06, c)
        flip = np.where(b % 2 == 0, 1.0, -1.0).astype(F32)
        salb = sample_tiled(stone, X * flip + box_[b], H + boy[b], block_size) * bval[b][..., None]
        g = lum(stone)
        g3 = np.repeat(g[..., None], 3, 2)
        grain = sample_tiled(g3, X * flip + box_[b], H + boy[b], block_size)[..., 0] - float(g.mean())
        self.alb = (salb * (1 - np.clip(D * 4.5, 0, 0.75))[..., None]).astype(F32)
        self.hgt = np.where(self.hole > 0, 0.0, -D + grain * 0.05).astype(F32)
        self.inwall = inwall

    def depth(self, x, h):
        from backedge import HALF, PPU
        c = self.canvas
        ix = int(min(max((x + HALF) * PPU, 0), c.w - 1))
        iy = int(min(max((self.mat.tex_h - min(h, self.height - 0.01)) * PPU, 0), c.h - 1))
        d = float(self.Dm[iy, ix])
        for ox, ow in self.openings:
            if abs(x - ox) < ow / 2:
                d = self.opening_depth
        return d

    def mesh(self, be, foot_y, batter=0.0, x0=-75.0, x1=75.0, step=0.12, h_step=0.1):
        """foot_y: number or function of x."""
        fy = foot_y if callable(foot_y) else (lambda x, v=foot_y: v)
        be.relief(self.mat, x0, x1, fy, lambda x: self.height, lambda x, h: h * batter + self.depth(x, h),
                  step=step, h_step=h_step)

    def paint(self, dust_col=None, dust_h=0.75, top=6.0, glow=0.7, side=(8.0, 55.0), floor=0.2, streaks=0.3):
        """Returns (albedo, height, emission, smoothness) for save_material."""
        from bepaint import age
        n = self.n
        c = self.canvas
        X, H = c.X, c.Y
        a = age(n, X, H, self.alb, foot_dark=0.0, streaks=streaks)
        if dust_col is not None:
            dust = (1 - smoothstep(0.0, dust_h, H)) * (0.55 + 0.35 * n.fbm(X, H, 0.8, 3, offset=5))
            a = lerp(a, np.broadcast_to(dust_col, a.shape), dust * 0.85)
        a = a * (1 - self.hole * 0.97)[..., None]                     # passages: black
        em_k, am = falloff(n, X, H, top=top, glow=glow, side=side, floor=floor)
        em = a * em_k[..., None]
        a = a * am[..., None]
        return a.astype(F32), self.hgt, em.astype(F32), np.full(X.shape, 0.12, F32)


def patch_mix(n, a, b, X, Y, amount, scale=6.0, softness=0.12, offset=21):
    """Mixes two sampled tiles (a, b) in patches through noise: `amount` (a number or a per-pixel array, 0..1) is the
    share of b. Returns (mix, mask)."""
    f = n.fbm(X, Y, scale, 4, offset=offset)
    thr = 1 - np.asarray(amount, F32)
    m = smoothstep(thr - softness, thr + softness, f)
    return lerp(a, b, m), m.astype(F32)
