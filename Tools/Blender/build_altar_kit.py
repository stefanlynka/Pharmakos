"""Builds the ritual altar mesh for the altar kit (Assets/Resources/Models/Meshes/AltarKit.fbx).

The altar is generated, not sculpted, so every face has clean UVs and a known material slot:
  0 Stone   body, steps and slab sides  (world-scale tiling UVs, 1 texture repeat per STONE_TILE units)
  1 Panel   the two text faces (table top, ramp)  (world-scale tiling UVs)
  2 Trim    the raised border strips round each text face  (u runs along the strip in repeats, v = 0 outer edge .. 1 inner edge)
  3 Corner  the raised corner blocks  (0..1 square)
Each god's look is then just four materials (see AltarStyle.cs). Dimensions match the old Meshy altar
(Altar2.fbx) so the cost badges and rules text that sit on it stay in place.

Usage:
  blender -b --factory-startup --python Tools/Blender/build_altar_kit.py -- <reference Altar2.fbx> <out.fbx> [preview.png]
The reference FBX is only used to copy its object transform, so the new mesh imports into Unity in the same space.
"""
import math
import sys

import bmesh
import bpy
import mathutils
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
ref_path, out_path = argv[0], argv[1]
preview_path = argv[2] if len(argv) > 2 else None

STONE_TILE = 0.55      # stone texture repeats every this many units
TRIM_W = 0.068         # width of the raised border strips
TRIM_REPEAT = 0.272    # length of one repeat of the trim pattern (texture is 4:1, so 4 x TRIM_W)
TRIM_H = 0.006         # trim height above the panel
CORNER_W = 0.09        # corner blocks are a little larger than the trim width
CORNER_H = 0.012       # corner block height above the panel

# Footprint (Blender world, Z up, ramp at -Y), matched to Altar2.fbx.
Z0 = -0.186            # ground
PLINTH_TOP = 0.09
STEPS = 6
STEP_INSET = 0.035
PLINTH_X = 0.79
PLINTH_BACK = 0.876
PLINTH_FRONT = -0.25
SLAB_X = 0.735
SLAB_FRONT, SLAB_BACK = -0.07, 0.815
SLAB_TOP = 0.172
RAMP_X = 0.53
RAMP_FRONT, RAMP_BACK = -0.95, -0.03
RAMP_FRONT_TOP, RAMP_BACK_TOP = -0.15, 0.160

STONE, PANEL, TRIM, CORNER = 0, 1, 2, 3

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=ref_path)
ref = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]
ref_matrix = ref.matrix_world.copy()

bm = bmesh.new()
uv_layer = bm.loops.layers.uv.new("UVMap")


def add_face(points, material, uvs, outward=None):
    """outward: a direction the face must point along. Every face is its own island, so its winding has to be set
    explicitly (letting Blender recalculate normals guesses per face and flips about half of them)."""
    points = [Vector(p) for p in points]
    uvs = list(uvs)
    if outward is not None:
        normal = (points[1] - points[0]).cross(points[2] - points[0])
        if normal.dot(Vector(outward)) < 0:
            points.reverse()
            uvs.reverse()
    verts = [bm.verts.new(p) for p in points]
    face = bm.faces.new(verts)
    face.material_index = material
    for loop, uv in zip(face.loops, uvs):
        loop[uv_layer].uv = uv
    return face


def stone_uvs(points, normal):
    """Cube projection onto the plane the face mostly faces, at world scale."""
    n = Vector(normal)
    ax = max(range(3), key=lambda i: abs(n[i]))
    if ax == 0:
        return [(p[1] / STONE_TILE, p[2] / STONE_TILE) for p in points]
    if ax == 1:
        return [(p[0] / STONE_TILE, p[2] / STONE_TILE) for p in points]
    return [(p[0] / STONE_TILE, p[1] / STONE_TILE) for p in points]


def quad(points, away_from, material=STONE, uvs=None):
    """A face pointing away from the point away_from (the centre of the solid it belongs to)."""
    pts = [Vector(p) for p in points]
    centroid = sum(pts, Vector()) / len(pts)
    outward = centroid - Vector(away_from)
    if uvs is None:
        uvs = stone_uvs(pts, outward)
    return add_face(pts, material, uvs, outward)


def box(x0, x1, y0, y1, z0, z1, top=True, bottom=False):
    """Axis-aligned stone block, every face pointing out of it."""
    c = ((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2)
    if top:
        quad([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], c)
    if bottom:
        quad([(x0, y1, z0), (x1, y1, z0), (x1, y0, z0), (x0, y0, z0)], c)
    quad([(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], c)   # front (-Y)
    quad([(x1, y1, z0), (x0, y1, z0), (x0, y1, z1), (x1, y1, z1)], c)   # back (+Y)
    quad([(x0, y1, z0), (x0, y0, z0), (x0, y0, z1), (x0, y1, z1)], c)   # left (-X)
    quad([(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], c)   # right (+X)


def framed_face(origin, u_axis, v_axis, width, height):
    """A text face with a raised border: inner Panel, four Trim strips, four Corner blocks.
    origin is the face's bottom-left corner; u_axis/v_axis are unit vectors across and up the face."""
    o, U, V = Vector(origin), Vector(u_axis), Vector(v_axis)
    N = U.cross(V).normalized()
    w = TRIM_W

    def P(u, v, h=0.0):
        return o + U * u + V * v + N * h

    def world_uv(u, v):
        # Panels use the face's own axes, so the grain lies flat on the ramp's slope too.
        return ((u + o.x) / STONE_TILE, (v + o.y) / STONE_TILE)

    # Panel
    corners = [(w, w), (width - w, w), (width - w, height - w), (w, height - w)]
    add_face([P(u, v) for u, v in corners], PANEL, [world_uv(u, v) for u, v in corners], N)

    def raised_rect(u0, v0, u1, v1, h, material, top_uvs, side_material=STONE):
        a, b, c, d = P(u0, v0, h), P(u1, v0, h), P(u1, v1, h), P(u0, v1, h)
        add_face([a, b, c, d], material, top_uvs, N)
        # side walls down to the panel plane, pointing out of the block
        centre = P((u0 + u1) / 2, (v0 + v1) / 2, h / 2)
        base = [P(u0, v0), P(u1, v0), P(u1, v1), P(u0, v1)]
        top = [a, b, c, d]
        for i in range(4):
            j = (i + 1) % 4
            quad([base[i], base[j], top[j], top[i]], centre, side_material)

    # Trim strips run between the corner blocks. Texture v: 0 at the face's outer edge, 1 at the inner edge.
    c = CORNER_W
    L = width - 2 * c
    r = L / TRIM_REPEAT
    raised_rect(c, 0, width - c, w, TRIM_H, TRIM, [(0, 0), (r, 0), (r, 1), (0, 1)])                     # bottom
    raised_rect(c, height - w, width - c, height, TRIM_H, TRIM, [(r, 1), (0, 1), (0, 0), (r, 0)])       # top
    L = height - 2 * c
    r = L / TRIM_REPEAT
    raised_rect(0, c, w, height - c, TRIM_H, TRIM, [(r, 0), (r, 1), (0, 1), (0, 0)])                    # left
    raised_rect(width - w, c, width, height - c, TRIM_H, TRIM, [(0, 1), (0, 0), (r, 0), (r, 1)])        # right

    # Corner blocks share one 0..1 square, rotated so the design keeps the same orientation to the face.
    sq = [(0, 0), (1, 0), (1, 1), (0, 1)]
    raised_rect(0, 0, c, c, CORNER_H, CORNER, sq)
    raised_rect(width - c, 0, width, c, CORNER_H, CORNER, sq)
    raised_rect(width - c, height - c, width, height, CORNER_H, CORNER, sq)
    raised_rect(0, height - c, c, height, CORNER_H, CORNER, sq)


# --- Plinth: stacked steps --------------------------------------------------
step_h = (PLINTH_TOP - Z0) / STEPS
for i in range(STEPS):
    inset = i * STEP_INSET
    box(-PLINTH_X + inset, PLINTH_X - inset,
        PLINTH_FRONT + inset, PLINTH_BACK - inset,
        Z0 + i * step_h, Z0 + (i + 1) * step_h,
        top=True, bottom=(i == 0))

# --- Table slab with framed top -------------------------------------------
box(-SLAB_X, SLAB_X, SLAB_FRONT, SLAB_BACK, PLINTH_TOP, SLAB_TOP, top=False)
framed_face((-SLAB_X, SLAB_FRONT, SLAB_TOP), (1, 0, 0), (0, 1, 0), 2 * SLAB_X, SLAB_BACK - SLAB_FRONT)

# --- Ramp (wedge) with framed sloped face -----------------------------------
fb, ft = Vector((0, RAMP_FRONT, Z0)), Vector((0, RAMP_FRONT, RAMP_FRONT_TOP))
bt, bb = Vector((0, RAMP_BACK, RAMP_BACK_TOP)), Vector((0, RAMP_BACK, Z0))
ramp_centre = (0, (RAMP_FRONT + RAMP_BACK) / 2, (Z0 + RAMP_BACK_TOP) / 2)
for sx in (-RAMP_X, RAMP_X):
    quad([Vector((sx, p.y, p.z)) for p in (fb, ft, bt, bb)], ramp_centre)
quad([(-RAMP_X, RAMP_FRONT, Z0), (RAMP_X, RAMP_FRONT, Z0), (RAMP_X, RAMP_FRONT, RAMP_FRONT_TOP), (-RAMP_X, RAMP_FRONT, RAMP_FRONT_TOP)], ramp_centre)
slope = (bt - ft)
slope_len = slope.length
framed_face((-RAMP_X, RAMP_FRONT, RAMP_FRONT_TOP), (1, 0, 0), tuple(slope.normalized()), 2 * RAMP_X, slope_len)

# --- Finish ------------------------------------------------------------------
mesh = bpy.data.meshes.new("AltarKit")
bm.to_mesh(mesh)
bm.free()
for poly in mesh.polygons:
    poly.use_smooth = False
for name in ("Stone", "Panel", "Trim", "Corner"):
    mesh.materials.append(bpy.data.materials.new(name))

obj = bpy.data.objects.new("AltarKit", mesh)
bpy.context.scene.collection.objects.link(obj)
# Same object transform as the reference, with the mesh moved into its local space. The half turn about Z
# matches the reference's export: without it the ramp comes out at the back in Unity.
obj.matrix_world = ref_matrix
mesh.transform(ref_matrix.inverted() @ mathutils.Matrix.Rotation(math.pi, 4, 'Z'))
bpy.data.objects.remove(ref)
print("AltarKit: %d faces, %d verts" % (len(mesh.polygons), len(mesh.vertices)))

if preview_path:
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 900, 700
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_backface_culling = True   # holes show up here exactly as in Unity
    colors = {"Stone": (0.75, 0.72, 0.66, 1), "Panel": (0.9, 0.88, 0.8, 1), "Trim": (0.7, 0.5, 0.25, 1), "Corner": (0.3, 0.5, 0.6, 1)}
    for m in mesh.materials:
        m.diffuse_color = colors[m.name]
    cam = bpy.data.cameras.new("cam")
    cam_obj = bpy.data.objects.new("cam", cam)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    centre = Vector((0, -0.04, 0))
    # A high three-quarter view, plus low views from the front and both sides where gaps would show.
    # (The mesh has been turned half round about Z above, so its ramp now faces +Y.)
    views = [("", (-1.2, 1.6, 1.3)), ("_low_front_right", (-1.0, 1.4, 0.35)), ("_low_left", (1.6, 0.6, 0.3)), ("_back", (-0.6, -1.6, 0.8))]
    stem, ext = preview_path.rsplit(".", 1)
    for suffix, direction in views:
        cam_obj.location = centre + Vector(direction).normalized() * 3.6
        cam_obj.rotation_euler = (centre - cam_obj.location).to_track_quat('-Z', 'Y').to_euler()
        scene.render.filepath = stem + suffix + "." + ext
        bpy.ops.render.render(write_still=True)

bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=out_path, use_selection=True, object_types={'MESH'}, apply_unit_scale=True,
                         path_mode='STRIP', mesh_smooth_type='FACE', use_tspace=True)
