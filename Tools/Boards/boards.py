"""Paints every encounter's combat board (ground + lanes) into Assets/Resources/Content/Boards/<Name>/.

Usage:
  python Tools/Boards/boards.py                  all boards
  python Tools/Boards/boards.py Underworld Troy  just these
  python Tools/Boards/boards.py --preview ...    also write flat lit previews to Claude/Boards/preview/
Then in Unity: Tools -> Boards -> Build Board Styles (with GameScene open). See Docs/Boards.md.

Each board is a recipe module in Tools/Boards/recipes/ (NAME + paint(preview)). Shared pieces: boardgen.py (noise,
shapes, normals, output) and boardkit.py (arena distance, scattering, bones, previews).
"""
import importlib
import os
import pkgutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import recipes  # noqa: E402


def load_recipes():
    found = {}
    for info in pkgutil.iter_modules(recipes.__path__):
        mod = importlib.import_module('recipes.' + info.name)
        if hasattr(mod, 'NAME') and hasattr(mod, 'paint'):
            found[mod.NAME] = mod.paint
    return found


if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    pv = '--preview' in sys.argv
    if args:   # import only the recipes asked for (module name = board name in lower case)
        boards = {a: importlib.import_module('recipes.' + a.lower()).paint for a in args}
    else:
        boards = load_recipes()
    for name in (args or sorted(boards)):
        print('painting', name, flush=True)
        boards[name](preview=pv)
