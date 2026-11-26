"""The skull shown over a follower when it dies (ViewFollower.SkullRenderer), in black-figure style.

A Kiln Black skull whose cheeks pinch in hard below the sockets to a narrow jaw ending in a solid block of upper
teeth (no lower jaw), with the sockets and nose cut through (the card shows behind), the teeth incised in
bone white, and a thin bone-white rim so it reads on dark art, and a thin bone-white rim so it reads on dark art.

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

# Rounded cranium; below the sockets the cheeks pinch in hard to a narrow upper jaw, which ends in a solid block of
# teeth joined to the skull (no lower jaw).
SILHOUETTE = (
    "M50 3 C73 3 90 18 90 40 C90 49 87 55 83 58 "
    "C80 60 78 63 75 67 C71 72 66 74 63 76 "
    "L63 92 C63 95 61 96 58 96 L42 96 C39 96 37 95 37 92 L37 76 "
    "C34 74 29 72 25 67 C22 63 20 60 17 58 C13 55 10 49 10 40 C10 18 27 3 50 3 Z"
)
# Sockets set level and apart, rounded, cut through so the (charred) card shows behind.
SOCKETS = (
    "M57 44 C57 40 62 38 67 38 C73 38 76 41 76 46 C76 51 72 54 67 54 C61 54 57 50 57 44 Z "
    "M43 44 C43 40 38 38 33 38 C27 38 24 41 24 46 C24 51 28 54 33 54 C39 54 43 50 43 44 Z"
)
# A sharp, narrow nasal opening.
NOSE = "M50 57 L46 67 L54 67 Z"

INCISIONS = [
    # the bite line and the gaps between the teeth, cut into the jaw
    "M37 81 L63 81",
    "M42.2 81 L42.2 96 M47.4 81 L47.4 96 M52.6 81 L52.6 96 M57.8 81 L57.8 96",
]
TEETH = []
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
