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
