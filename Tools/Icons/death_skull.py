"""The skull shown over a follower when it dies (ViewFollower.SkullRenderer), in black-figure style.

A Kiln Black silhouette with the eye sockets and nose cut through (the card shows behind), incised bone-white
lines for temples and dome, and one row of solid upper teeth hanging below the jaw line (no lower jaw), and a thin bone-white rim so it reads on dark art.

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

# Cranium, cheekbones and upper jaw only (no mandible), with hard planes rather than soft curves below the brow.
SILHOUETTE = (
    "M50 4 C79 4 95 22 95 46 C95 57 92 64 88 69 L85 76 L79 79 L75 86 L25 86 "
    "L21 79 L15 76 L12 69 C8 64 5 57 5 46 C5 22 21 4 50 4 Z"
)
# Angular orbits, cut through so the (charred) card shows behind.
SOCKETS = (
    "M55 46 L60 39 L76 38 L82 44 L80 56 L73 62 L62 62 L56 56 Z "
    "M45 46 L40 39 L24 38 L18 44 L20 56 L27 62 L38 62 L44 56 Z"
)
# A sharp, split nasal opening.
NOSE = "M50 65 L44.5 79 L50 75.5 L55.5 79 Z"

INCISIONS = [
    # temples
    "M11 50 C12 58 14 63 17 67",
    "M89 50 C88 58 86 63 83 67",
    # dome
    "M28 15 C39 9 61 9 72 15",
]
# One row of upper teeth hanging free below the jaw line: solid bone, square-cut, narrowing a little to the tip.
TEETH = []
for i in range(6):
    x0 = 28.6 + i * 7.3
    x1 = x0 + 5.6
    bottom = 99.0 if i in (2, 3) else 97.0
    TEETH.append(f"M{x0:.1f} 85 L{x1:.1f} 85 L{x1 - 0.7:.1f} {bottom} L{x0 + 0.7:.1f} {bottom} Z")
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
        f'<path d="{SILHOUETTE}" fill="none" stroke="{BONE}" stroke-width="{RIM}" stroke-linejoin="round"/>'
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
