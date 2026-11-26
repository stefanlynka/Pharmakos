"""The skull shown over a follower when it dies (ViewFollower.SkullRenderer), in black-figure style.

A Kiln Black silhouette with the eye sockets and nose cut through (the card shows behind), incised bone-white
lines for temples, dome and teeth, and a thin bone-white rim so it reads on dark art.

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

SILHOUETTE = (
    "M50 4 C78 4 95 22 95 46 C95 58 91 66 86 71 C84 73 83 76 83 80 L81 86 C80 90 77 92 73 93 "
    "L72 105 C71 111 67 115 61 115 L39 115 C33 115 29 111 28 105 L27 93 C23 92 20 90 19 86 "
    "L17 80 C17 76 16 73 14 71 C9 66 5 58 5 46 C5 22 22 4 50 4 Z"
)
# Deep orbits, cut through so the (charred) card shows behind.
SOCKETS = (
    "M55 47 C55 41 61 38 68 38 C76 38 81 42 81 49 C81 57 77 63 69 63 C61 63 55 57 55 47 Z "
    "M45 47 C45 41 39 38 32 38 C24 38 19 42 19 49 C19 57 23 63 31 63 C39 63 45 57 45 47 Z"
)
NOSE = "M50 65 C48 70 45 74 46 78 C47 80 49 79 50 77 C51 79 53 80 54 78 C55 74 52 70 50 65 Z"

INCISIONS = [
    # temples
    "M11 50 C12 58 15 64 19 68",
    "M89 50 C88 58 85 64 81 68",
    # dome, as a potter would incise it
    "M28 15 C39 9 61 9 72 15",
    # bite line, upper and lower teeth
    "M30 101 C40 103 60 103 70 101",
    "M37 93 L37.4 101.4 M43.5 93 L43.7 102.2 M50 93 L50 102.6 M56.5 93 L56.3 102.2 M63 93 L62.6 101.4",
    "M40 102.4 L40 110 M46.5 102.7 L46.5 112.5 M53.5 102.7 L53.5 112.5 M60 102.4 L60 110",
]
STROKE = 1.25
RIM = 1.9


def build_svg():
    incisions = "".join(
        f'<path d="{d}" fill="none" stroke="{BONE}" stroke-width="{STROKE}" stroke-linecap="round"/>'
        for d in INCISIONS
    )
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 100 120">'
        f'<path d="{SILHOUETTE} {SOCKETS} {NOSE}" fill="{BLACK}" fill-rule="evenodd"/>'
        f'<path d="{SOCKETS} {NOSE}" fill="none" stroke="{BONE}" stroke-width="{STROKE}" stroke-linejoin="round"/>'
        f"{incisions}"
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
