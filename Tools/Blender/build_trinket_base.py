"""Builds the trinket base: a squat black-and-gold marble cylinder with the trinket's art set into its top (ViewBuff
prefab → Base, driven by TrinketBase.cs).

Mesh (radius 1, top at z = 0 in Blender, Z up; body down to z = -HEIGHT), three material slots:
  0 Side    the wall and the rounded outer edges. UVs: u once round (tiles), v from the bottom edge (0) to the top
            edge (1).
  1 Top     the flat marble rim round the art, its rounded inner edge and the short wall down to the art. Planar UVs:
            0..1 across the disc's full diameter.
  2 Inlay   the sunken disc that shows the trinket art. Planar UVs that cover only the inner ART_FRACTION of the art
            texture, so the black-gold ring painted into every icon is cropped away and the model's rim replaces it.
            u follows the art's right, v its up.

Textures, all made from the backdrop image (Images/Icons/trinketBackdropBorder.png, the round marble the icons' rings
come from):
  TrinketMarble_Albedo / _Normal / _Mask        2048x256 strip for Side, unwrapped from the disc
  TrinketMarbleTop_Albedo / _Normal / _Mask     1024 square for Top, the disc itself
  The strip's top row samples the disc at the radius where the Top rim begins, so the veins carry on over the edge.
  Masks follow URP Lit: R = metallic (the gold), A = smoothness.

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
HEIGHT = 0.3           # squat: about a seventh of the diameter
BEVEL_TOP = 0.06       # rounded outer top edge
BEVEL_BOTTOM = 0.035
INLAY_RADIUS = 0.8     # the art's opening; the rim is the rest (the icons' own ring is about 18% of their radius)
INLAY_DEPTH = 0.035    # how far the art sits below the rim
BEVEL_INNER = 0.02     # rounded inner edge of the rim
ART_FRACTION = 0.86    # how much of the art's radius shows: a sliver past the icons' white disc (~0.82), so the dark ring
                       # gives the opening a soft inner edge instead of white meeting the rim at a jagged mesh edge
BEVEL_SEGMENTS = 5
SEGMENTS = 128         # round the circumference

SIDE, TOP, INLAY = 0, 1, 2

STRIP_W, STRIP_H = 2048, 256
TOP_SIZE = 1024
DISC_RIM = 0.95        # sample the backdrop disc no further out than this (its antialiased edge is white)


# ------------------------------------------------------------------ mesh ---
def arc(cx, cz, r, a0, a1, n):
    return [(cx + r * math.cos(a0 + (a1 - a0) * i / n), cz + r * math.sin(a0 + (a1 - a0) * i / n)) for i in range(n + 1)]


def side_profile():
    """(radius, z) from the bottom centre round the bottom and up the wall to where the flat top rim begins."""
    pts = [(0.0, -HEIGHT), (RADIUS - BEVEL_BOTTOM - 0.08, -HEIGHT)]
    pts += arc(RADIUS - BEVEL_BOTTOM, -HEIGHT + BEVEL_BOTTOM, BEVEL_BOTTOM, -math.pi / 2, 0, BEVEL_SEGMENTS)
    pts.append((RADIUS, -HEIGHT * 0.5))
    pts += arc(RADIUS - BEVEL_TOP, -BEVEL_TOP, BEVEL_TOP, 0, math.pi / 2, BEVEL_SEGMENTS)
    return pts, 2          # the strip starts at the bottom bevel (index 2)


def top_profile():
    """From where the side ends, across the flat rim, round its inner edge and down the short wall to the art."""
    pts = [(RADIUS - BEVEL_TOP, 0.0)]
    pts += arc(INLAY_RADIUS + BEVEL_INNER, -BEVEL_INNER, BEVEL_INNER, math.pi / 2, math.pi, BEVEL_SEGMENTS)
    pts.append((INLAY_RADIUS, -INLAY_DEPTH))
    return pts


def inlay_profile():
    return [(INLAY_RADIUS, -INLAY_DEPTH), (INLAY_RADIUS - 0.03, -INLAY_DEPTH), (0.0, -INLAY_DEPTH)]


def revolve(bm, uv, pts, slot, uv_of):
    """Revolves a profile into rings of faces. uv_of(i, k, x, y) gives a loop's UV for profile point i at segment k."""
    rings = []
    for r, z in pts:
        if r == 0.0:
            rings.append([bm.verts.new((0.0, 0.0, z))])
        else:
            rings.append([bm.verts.new((r * math.cos(2 * math.pi * k / SEGMENTS), r * math.sin(2 * math.pi * k / SEGMENTS), z))
                          for k in range(SEGMENTS)])
    for i in range(len(pts) - 1):
        a, b = rings[i], rings[i + 1]
        for k in range(SEGMENTS):
            k1 = (k + 1) % SEGMENTS
            if len(a) == 1:            # bottom fan, normal down
                verts, idx = (a[0], b[k1], b[k]), ((i, k + 0.5), (i + 1, k + 1), (i + 1, k))
            elif len(b) == 1:          # top fan
                verts, idx = (a[k], a[k1], b[0]), ((i, k), (i, k + 1), (i + 1, k + 0.5))
            else:
                verts, idx = (a[k], a[k1], b[k1], b[k]), ((i, k), (i, k + 1), (i + 1, k + 1), (i + 1, k))
            f = bm.faces.new(verts)
            f.material_index = slot
            f.smooth = True
            for loop, (pi, kk) in zip(f.loops, idx):
                loop[uv].uv = uv_of(pi, kk, loop.vert.co.x, loop.vert.co.y)
    return rings


def build_mesh():
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")

    # Side: v is arc length along the profile, 0 at the bottom bevel's start and 1 where the top rim begins
    pts, start = side_profile()
    s = [0.0]
    for a, b in zip(pts, pts[1:]):
        s.append(s[-1] + math.dist(a, b))
    v = [max(0.0, (x - s[start]) / (s[-1] - s[start])) for x in s]
    side = revolve(bm, uv, pts, SIDE, lambda i, k, x, y: (k / SEGMENTS, v[i]))

    # Top rim: planar, the disc's full diameter across 0..1
    top = revolve(bm, uv, top_profile(), TOP, lambda i, k, x, y: (0.5 + 0.5 * x / RADIUS, 0.5 + 0.5 * y / RADIUS))
    # weld the rim's outer ring to the side's last ring so the edge shades smoothly
    bmesh.ops.remove_doubles(bm, verts=top[0] + side[-1], dist=1e-5)

    # Inlay: its own vertices (sharp crease at the wall), UVs onto the inner part of the art
    # (negated: the FBX export turns Blender +x/+y into Unity -x/-z, and Base is rotated so Unity +x/+z are the art's
    # right/up)
    scale = 0.5 * ART_FRACTION / INLAY_RADIUS
    revolve(bm, uv, inlay_profile(), INLAY, lambda i, k, x, y: (0.5 - scale * x, 0.5 - scale * y))

    # Unity orders submeshes by first use, so keep faces in slot order
    bm.faces.sort(key=lambda f: f.material_index)
    bm.faces.index_update()
    bm.normal_update()

    mesh = bpy.data.meshes.new("TrinketBase")
    bm.to_mesh(mesh)
    bm.free()
    for name in ("TrinketSide", "TrinketTop", "TrinketInlay"):
        mesh.materials.append(bpy.data.materials.new(name))
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


def write_set(prefix, alb, wrap_x):
    """Albedo, a soft normal map from the marble's own tones, and a mask where the gold is metallic."""
    lum = alb @ np.array((0.3, 0.59, 0.11))
    gold = smoothstep(0.2, 0.45, lum) * smoothstep(0.04, 0.15, alb[..., 0] - alb[..., 2])

    def blur(a):
        l = np.roll(a, 1, 1) if wrap_x else np.hstack([a[:, :1], a[:, :-1]])
        r = np.roll(a, -1, 1) if wrap_x else np.hstack([a[:, 1:], a[:, -1:]])
        return (a + l + r + np.vstack([a[:1], a[:-1]]) + np.vstack([a[1:], a[-1:]])) / 5

    hgt = blur(blur(lum * 0.6 + gold * 0.4))
    gx = ((np.roll(hgt, -1, 1) - np.roll(hgt, 1, 1)) * 0.5 if wrap_x else np.gradient(hgt, axis=1)) * 3.0
    gy = np.gradient(hgt, axis=0) * 3.0
    n = np.dstack([-gx, -gy, np.ones_like(hgt)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)

    out = np.ones(alb.shape[:2] + (4,))
    out[..., :3] = alb
    save(prefix + "_Albedo", out)
    out[..., :3] = n * 0.5 + 0.5
    save(prefix + "_Normal", out)
    mask = np.zeros(alb.shape[:2] + (4,))
    mask[..., 0] = gold * 0.9
    mask[..., 3] = 0.55 + 0.15 * gold          # polished stone, the gold a touch glossier
    save(prefix + "_Mask", mask)


def build_textures():
    src = bpy.data.images.load(backdrop_path)
    w, h = src.size
    img = np.array(src.pixels[:], dtype=np.float32).reshape(h, w, src.channels)[..., :3]
    bpy.data.images.remove(src)

    # find the disc: everything that isn't the white background
    lum = img @ np.array((0.3, 0.59, 0.11))
    ys, xs = np.nonzero(lum < 0.85)
    cx, cy = (xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2
    rd = (xs.max() - xs.min() + ys.max() - ys.min()) / 4 * DISC_RIM     # disc radius used = mesh RADIUS

    # Top: the disc, square, its used radius filling the texture (planar UV 0..1 = mesh -RADIUS..RADIUS)
    jj, ii = np.mgrid[0:TOP_SIZE, 0:TOP_SIZE]
    px = ((ii + 0.5) / TOP_SIZE * 2 - 1) * rd
    py = ((jj + 0.5) / TOP_SIZE * 2 - 1) * rd
    write_set("TrinketMarbleTop", bilinear(img, cx + px, cy + py), wrap_x=False)

    # Side: unwrapped strip. Its top row samples where the Top rim starts (RADIUS - BEVEL_TOP), and it reaches inward
    # by the side profile's length (in disc units, keeping the marble's proportions at the strip's middle)
    pts, start = side_profile()
    length = sum(math.dist(a, b) for a, b in zip(pts[start:], pts[start + 1:]))
    r_top = (RADIUS - BEVEL_TOP) / RADIUS
    span = (length / RADIUS) * r_top / (1 + 0.5 * length / RADIUS)
    jj, ii = np.mgrid[0:STRIP_H, 0:STRIP_W]
    theta = 2 * math.pi * (ii + 0.5) / STRIP_W
    r = (r_top - span * (1 - (jj + 0.5) / STRIP_H)) * rd
    write_set("TrinketMarble", bilinear(img, cx + r * np.cos(theta), cy + r * np.sin(theta)), wrap_x=True)


bpy.ops.wm.read_factory_settings(use_empty=True)
build_mesh()
os.makedirs(tex_dir, exist_ok=True)
build_textures()
bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False, object_types={'MESH'}, mesh_smooth_type='FACE',
                         bake_space_transform=True, axis_forward='-Z', axis_up='Y', add_leaf_bones=False)
print("trinket base written:", fbx_path, tex_dir)
