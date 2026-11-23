"""Builds the trinket base: a squat black-and-gold marble cylinder the trinket art sits on (ViewBuff prefab).

Writes:
  <mesh out>                 TrinketBase.fbx, a cylinder of radius 1 whose top face is at z = 0 (Blender Z up) and whose
                             body runs down to z = -HEIGHT; rounded top and bottom edges. One material slot.
  <texture dir>/TrinketMarble_Albedo / _Normal / _Mask
                             a 2048x256 strip unwrapped from the backdrop image (the round marble disc in
                             Images/Icons/trinketBackdropBorder.png), so the side wall is the same marble as the art's ring.
                             u runs once round the cylinder (tiles), v runs from the bottom edge (0) up to the top rim (1).
                             v = 1 samples the disc near its rim and v = 0 further in, so the veins on the art's ring carry
                             on over the rim like a mirror. Mask follows URP Lit: R = metallic (the gold), A = smoothness.

Usage:
  blender -b --factory-startup --python Tools/Blender/build_trinket_base.py -- <backdrop.png> <absolute out.fbx> <absolute texture dir>
  (paths must be absolute: Blender resolves relative ones somewhere else and nothing gets written)
"""
import math
import os
import sys

import bmesh
import bpy
import numpy as np

argv = sys.argv[sys.argv.index("--") + 1:]
backdrop_path, fbx_path, tex_dir = argv[0], argv[1], argv[2]

RADIUS = 1.0
HEIGHT = 0.3           # squat: about a sixth of the diameter
BEVEL_TOP = 0.07       # rounded top rim, the edge that catches the light round the art
BEVEL_BOTTOM = 0.035
BEVEL_SEGMENTS = 5
SEGMENTS = 128         # round the circumference

STRIP_W, STRIP_H = 2048, 256
DISC_RIM = 0.95        # sample the backdrop disc no further out than this (its antialiased edge is white)


# ------------------------------------------------------------------ mesh ---
def arc(cx, cz, r, a0, a1, n):
    return [(cx + r * math.cos(a0 + (a1 - a0) * i / n), cz + r * math.sin(a0 + (a1 - a0) * i / n)) for i in range(n + 1)]


def profile():
    """(radius, z) points from the bottom centre round the edge to the top centre, plus the index range that the
    texture strip covers (bottom bevel start .. top bevel end)."""
    pts = [(0.0, -HEIGHT), (RADIUS - BEVEL_BOTTOM - 0.08, -HEIGHT)]
    start = len(pts)
    pts += arc(RADIUS - BEVEL_BOTTOM, -HEIGHT + BEVEL_BOTTOM, BEVEL_BOTTOM, -math.pi / 2, 0, BEVEL_SEGMENTS)
    pts.append((RADIUS, -HEIGHT * 0.5))
    pts += arc(RADIUS - BEVEL_TOP, -BEVEL_TOP, BEVEL_TOP, 0, math.pi / 2, BEVEL_SEGMENTS)
    end = len(pts) - 1
    pts += [(RADIUS - BEVEL_TOP - 0.08, 0.0), (0.0, 0.0)]
    return pts, start, end


def build_mesh():
    pts, start, end = profile()
    # v: arc length along the profile, 0 at the bottom bevel's start and 1 at the top bevel's end (clamped beyond)
    s = [0.0]
    for a, b in zip(pts, pts[1:]):
        s.append(s[-1] + math.dist(a, b))
    v = [(x - s[start]) / (s[end] - s[start]) for x in s]

    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    rings = []
    for i, (r, z) in enumerate(pts):
        if r == 0.0:
            rings.append([bm.verts.new((0.0, 0.0, z))])
        else:
            rings.append([bm.verts.new((r * math.cos(2 * math.pi * k / SEGMENTS), r * math.sin(2 * math.pi * k / SEGMENTS), z))
                          for k in range(SEGMENTS)])

    def set_uv(face, us, vs):
        for loop, uu, vv in zip(face.loops, us, vs):
            loop[uv].uv = (uu, vv)

    for i in range(len(pts) - 1):
        a, b = rings[i], rings[i + 1]
        for k in range(SEGMENTS):
            k1 = (k + 1) % SEGMENTS
            u0, u1 = k / SEGMENTS, (k + 1) / SEGMENTS
            if len(a) == 1:            # bottom fan, wound so the normal faces down
                f = bm.faces.new((a[0], b[k1], b[k]))
                set_uv(f, ((u0 + u1) / 2, u1, u0), (v[i], v[i + 1], v[i + 1]))
            elif len(b) == 1:          # top fan
                f = bm.faces.new((a[k], a[k1], b[0]))
                set_uv(f, (u0, u1, (u0 + u1) / 2), (v[i], v[i], v[i + 1]))
            else:
                f = bm.faces.new((a[k], a[k1], b[k1], b[k]))
                set_uv(f, (u0, u1, u1, u0), (v[i], v[i], v[i + 1], v[i + 1]))
            f.smooth = True
    bm.normal_update()

    mesh = bpy.data.meshes.new("TrinketBase")
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(bpy.data.materials.new("TrinketMarble"))
    obj = bpy.data.objects.new("TrinketBase", mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


# -------------------------------------------------------------- textures ---
def save(name, rgba):
    h, w, _ = rgba.shape
    img = bpy.data.images.new(name, w, h, alpha=True, float_buffer=False, is_data=True)
    img.pixels.foreach_set(np.clip(rgba, 0, 1).astype(np.float32).ravel())
    img.filepath_raw = os.path.join(tex_dir, name + ".png")
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def bilinear(img, x, y):
    h, w, _ = img.shape
    x = np.clip(x, 0, w - 1.001)
    y = np.clip(y, 0, h - 1.001)
    x0, y0 = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = (x - x0)[..., None], (y - y0)[..., None]
    a = img[y0, x0] * (1 - fx) + img[y0, x0 + 1] * fx
    b = img[y0 + 1, x0] * (1 - fx) + img[y0 + 1, x0 + 1] * fx
    return a * (1 - fy) + b * fy


def build_textures():
    src = bpy.data.images.load(backdrop_path)
    w, h = src.size
    img = np.array(src.pixels[:], dtype=np.float32).reshape(h, w, src.channels)[..., :3]
    bpy.data.images.remove(src)

    # find the disc: everything that isn't the white background
    lum = img @ np.array((0.3, 0.59, 0.11))
    ys, xs = np.nonzero(lum < 0.85)
    cx, cy = (xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2
    rd = (xs.max() - xs.min() + ys.max() - ys.min()) / 4

    # strip proportions: the profile's length over the circumference decides how much radius the strip spans
    pts, start, end = profile()
    length = sum(math.dist(a, b) for a, b in zip(pts[start:end], pts[start + 1:end + 1]))
    # keep the marble's proportions at the strip's middle radius: span = length / RADIUS * (r_top - span / 2)
    r_top = DISC_RIM
    span = (length / RADIUS) * r_top / (1 + 0.5 * length / RADIUS)
    jj, ii = np.mgrid[0:STRIP_H, 0:STRIP_W]
    theta = 2 * math.pi * (ii + 0.5) / STRIP_W
    r = (r_top - span * (1 - (jj + 0.5) / STRIP_H)) * rd
    alb = bilinear(img, cx + r * np.cos(theta), cy + r * np.sin(theta))

    # gold = warm and bright; the rest is black stone
    lum = alb @ np.array((0.3, 0.59, 0.11))
    warmth = alb[..., 0] - alb[..., 2]
    gold = smoothstep(0.2, 0.45, lum) * smoothstep(0.04, 0.15, warmth)

    def wrap_blur(a):
        return (a + np.roll(a, 1, 1) + np.roll(a, -1, 1) + np.vstack([a[:1], a[:-1]]) + np.vstack([a[1:], a[-1:]])) / 5

    hgt = wrap_blur(wrap_blur(lum * 0.6 + gold * 0.4))
    gx = (np.roll(hgt, -1, 1) - np.roll(hgt, 1, 1)) * 0.5 * 3.0
    gy = np.gradient(hgt, axis=0) * 3.0
    n = np.dstack([-gx, -gy, np.ones_like(hgt)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)

    out = np.ones((STRIP_H, STRIP_W, 4))
    out[..., :3] = alb
    save("TrinketMarble_Albedo", out)
    out[..., :3] = n * 0.5 + 0.5
    save("TrinketMarble_Normal", out)
    mask = np.zeros((STRIP_H, STRIP_W, 4))
    mask[..., 0] = gold * 0.9
    mask[..., 3] = 0.55 + 0.15 * gold          # polished stone, the gold a touch glossier
    save("TrinketMarble_Mask", mask)


bpy.ops.wm.read_factory_settings(use_empty=True)
build_mesh()
os.makedirs(tex_dir, exist_ok=True)
build_textures()
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False, object_types={'MESH'}, mesh_smooth_type='FACE',
                         bake_space_transform=True, axis_forward='-Z', axis_up='Y', add_leaf_bones=False)
print("trinket base written:", fbx_path, tex_dir)
