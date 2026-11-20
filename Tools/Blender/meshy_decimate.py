"""Decimate a Meshy export for use in game, keeping its look.

Meshy models are ~1-2M triangles and ship smoothed custom vertex normals that hide lumpy raw geometry.
Plain decimation drops those normals and the glossy glaze shows the lumps, so this script copies the
original's custom normals onto the decimated mesh (Data Transfer), which keeps Meshy's own normal map valid.

Usage (Blender 4.1+; tested with 5.1):
  blender -b --factory-startup --python Tools/Blender/meshy_decimate.py -- <in.fbx> <out.fbx> <ratio> <MeshName>

  ratio 1 keeps full detail and only renames the mesh (the portrait frame ships like this for now: 0.03 / 58k
  triangles was visibly softer in game). Mind GitHub's 100 MB file limit: the full frame exports at ~75 MB.
  MeshName becomes the mesh sub-asset name in Unity. Keep it the same on re-export, or prefab references break.

Unity import settings for the output: Scale Factor 100 (matches Meshy's FBX), Normals = Import,
Tangents = Calculate Mikktspace, Material Creation Mode = None.
"""
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
src, dst, ratio, name = argv[0], argv[1], float(argv[2]), argv[3]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
high = [o for o in bpy.context.scene.objects if o.type == 'MESH'][0]

low = high.copy()
low.data = high.data.copy()
low.data.materials.clear()
bpy.context.scene.collection.objects.link(low)
bpy.context.view_layer.objects.active = low

if ratio < 1.0:
    decimate = low.modifiers.new("decimate", 'DECIMATE')
    decimate.ratio = ratio
    decimate.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier="decimate")

for polygon in low.data.polygons:
    polygon.use_smooth = True

transfer = low.modifiers.new("normals", 'DATA_TRANSFER')
transfer.object = high
transfer.use_loop_data = True
transfer.data_types_loops = {'CUSTOM_NORMAL'}
transfer.loop_mapping = 'POLYINTERP_NEAREST'
bpy.ops.object.modifier_apply(modifier="normals")

bpy.data.objects.remove(high)
low.name = name
low.data.name = name
print(f"{name}: {len(low.data.polygons)} triangles")

bpy.ops.object.select_all(action='DESELECT')
low.select_set(True)
bpy.ops.export_scene.fbx(filepath=dst, use_selection=True, object_types={'MESH'}, apply_unit_scale=True,
                         path_mode='STRIP', mesh_smooth_type='OFF')
