"""The skull shown over a follower when it dies (ViewFollower.SkullRenderer), in black-figure style.

A narrow, angular Kiln Black skull (hollow temples, sharp cheekbones, stern sockets under a heavy brow) with the
sockets and nose cut through (the card shows behind), incised bone-white brow and cheek lines, and one row
of solid upper teeth hanging below the jaw line (no lower jaw), and a thin bone-white rim so it reads on dark art.

    python Tools/Icons/death_skull.py            # writes Assets/Sprites/DeathSkull.png and Tools/Icons/svg/death_skull.svg
    python Tools/Icons/death_skull.py --out X.png

Rendered with headless Chrome/Edge via summary_mono.render.
"""

import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import summary_mono  # noqa: E402

ROOT = summary_mono.ROOT
OUT = os.path.join(ROOT, "Assets", "Sprites", "DeathSkull.png")
SVG = os.path.join(summary_mono.SVG_DIR, "death_skull.svg")

BLACK = "#141110"
BONE = "#D8D4CC"  # the Bone offering colour: white is death (Docs/StyleGuide.md, offering colours)
WIDTH, HEIGHT = 1024, 1229  # canvas 100 x 120

# Narrow, angular cranium: hollow temples, sharp cheekbones flaring out, the cheek falling away under them to a
# narrow upper jaw. No lower jaw.
SILHOUETTE = (
    "M50 2 C68 2 83 11 86 27 C88 36 87 43 85 49 L89 58 L84 66 L78 70 L76 80 L73 84 L27 84 L24 80 "
    "L22 70 L16 66 L11 58 L15 49 C13 43 12 36 14 27 C17 11 32 2 50 2 Z"
)
# Stern orbits, deep and set close, under a heavy brow that dips toward the nose; cut through so the (charred)
# card shows behind.
SOCKETS = (
    "M56 46 L76 41 L80 48 L75 57 L65 60 L58 55 Z "
    "M44 46 L24 41 L20 48 L25 57 L35 60 L42 55 Z"
)
# A narrow, split nasal opening.
NOSE = "M50 61 L46 72 L50 69.5 L54 72 Z"

INCISIONS = [
    # heavy brow ridges
    "M19 36 L45 41",
    "M81 36 L55 41",
    # cheek hollows under the cheekbones
    "M18 64 L24 68",
    "M82 64 L76 68",
]
# One row of upper teeth hanging free below the jaw line: solid bone, square-cut, uneven.
TEETH = []
for i, bottom in enumerate((91.0, 92.5, 94.0, 93.5, 92.0, 90.5)):
    x0 = 29.2 + i * 7.0
    x1 = x0 + 5.6
    TEETH.append(f"M{x0:.1f} 83.5 L{x1:.1f} 83.5 L{x1 - 0.7:.1f} {bottom} L{x0 + 0.7:.1f} {bottom} Z")
STROKE = 1.25
RIM = 1.9


def build_svg():
    incisions = "".join(
        f'<path d="{d}" fill="none" stroke="{BONE}" stroke-width="{STROKE}" stroke-linecap="butt" stroke-linejoin="miter"/>'
        for d in INCISIONS
    )
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 100 120">'
        f'<path d="{SILHOUETTE} {SOCKETS} {NOSE}" fill="{BLACK}" fill-rule="evenodd"/>'
        f'<path d="{SOCKETS} {NOSE}" fill="none" stroke="{BONE}" stroke-width="{STROKE}" stroke-linejoin="miter"/>'
        f"{incisions}"
        f'<path d="{" ".join(TEETH)}" fill="{BONE}"/>'
        f'<path d="{SILHOUETTE}" fill="none" stroke="{BONE}" stroke-width="{RIM}" stroke-linejoin="miter"/>'
        f"</svg>"
    )


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=OUT)
    args = parser.parse_args()

    os.makedirs(os.path.dirname(SVG), exist_ok=True)
    with open(SVG, "w", encoding="utf-8") as f:
        f.write(build_svg())
    summary_mono.render(summary_mono.find_browser(), SVG, os.path.abspath(args.out), WIDTH, HEIGHT)
    print(args.out)


if __name__ == "__main__":
    main()
