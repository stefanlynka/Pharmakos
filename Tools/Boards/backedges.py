"""Rebuilds only back edges (meshes + textures), without repainting the grounds:
  python Tools/Boards/backedges.py troy:back_edge temple:back_edge bosses:fates_edge ...
Then in Unity: Tools -> Boards -> Build Back Edges."""
import importlib
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, 'recipes'))
for spec in sys.argv[1:]:
    mod, fn = spec.split(':')
    getattr(importlib.import_module('recipes.' + mod), fn)()
