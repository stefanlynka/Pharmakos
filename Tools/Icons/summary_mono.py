"""Minimal one-colour follower summary icons (the glyph at the bottom of a follower).

Each icon is an SVG on a 100x100 canvas: solid shapes in one colour, details cut out
through a mask (black-figure style incision), so the whole set reads as one family.

    python Tools/Icons/summary_mono.py                 # Terracotta, the UI accent
    python Tools/Icons/summary_mono.py --color E8DCC2  # any other hex

Writes SVG sources to Tools/Icons/svg/ and 216x216 PNGs (the size of the painted
originals in Images/Icons/Summary) to Assets/Resources/Images/Icons/SummaryMono/,
with the same lower-case names so CardHandler.GetSummaryIcon can point at either folder.
Rendering uses headless Chrome or Edge (no Python SVG dependencies).
"""

import argparse
import os
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SVG_DIR = os.path.join(ROOT, "Tools", "Icons", "svg")
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Images", "Icons", "SummaryMono")
SIZE = 216
TERRACOTTA = "D9733F"

BROWSERS = [
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
]

# Each icon: (solid shapes, cut-outs). Both are SVG fragments on a 100x100 canvas.
# Solids are drawn white into the mask, cut-outs black; the mask then shows the colour.
# Main weight is ~8 units, cut-outs ~3.5, so every glyph has the same density.
ICONS = {
    # OnDrawBlood
    "blood": (
        '<path d="M50 6 C50 6 21 42 21 63 A29 29 0 0 0 79 63 C79 42 50 6 50 6 Z"/>',
        '<path d="M33 62 C33 72 38 79 46 82" fill="none" stroke-width="4.5" stroke-linecap="round"/>',
    ),
    # Sprint
    "bolt": (
        '<path d="M60 4 L22 56 L46 56 L36 96 L78 40 L54 40 L66 4 Z" stroke-linejoin="round" stroke-width="2" stroke="white"/>',
        "",
    ),
    # Ranged attacker
    "bow": (
        '<path d="M34 8 Q86 50 34 92" fill="none" stroke-width="8" stroke-linecap="round"/>'
        '<path d="M34 8 L34 92" stroke-width="2.5"/>'
        '<path d="M14 50 L82 50" stroke-width="5"/>'
        '<path d="M96 50 L80 41 L80 59 Z"/>'
        '<path d="M8 41 L20 41 L26 50 L20 59 L8 59 L14 50 Z"/>',
        '<path d="M14 50 L22 50" stroke-width="2"/>',
    ),
    # OnDamage (+ life siphon)
    "fangs": (
        '<path d="M10 20 Q50 4 90 20 L90 28 Q50 14 10 28 Z"/>'
        '<path d="M15 25 L31 22 C31 48 29 70 26 92 C18 72 14 50 15 25 Z"/>'
        '<path d="M85 25 L69 22 C69 48 71 70 74 92 C82 72 86 50 85 25 Z"/>'
        '<path d="M39 22 L48 20.5 L44 36 Z"/>'
        '<path d="M61 22 L52 20.5 L56 36 Z"/>',
        "",
    ),
    # OnEnter
    "horn": (
        '<path d="M10 88 C38 84 62 60 68 16 L92 28 C84 70 48 94 10 88 Z" stroke-linejoin="round" stroke-width="3" stroke="white"/>',
        '<path d="M64 34 L86 44" stroke-width="3.5" stroke-linecap="round"/>'
        '<path d="M56 54 L74 64" stroke-width="3.5" stroke-linecap="round"/>',
    ),
    # OnSpellCast
    "scroll": (
        '<rect x="26" y="18" width="48" height="64"/>'
        '<rect x="16" y="9" width="68" height="15" rx="7.5"/>'
        '<rect x="16" y="76" width="68" height="15" rx="7.5"/>',
        '<path d="M26 26 L74 26 M26 74 L74 74" stroke-width="3.5"/>'
        '<path d="M36 39 L64 39 M36 50 L64 50 M36 61 L56 61" stroke-width="4" stroke-linecap="round"/>',
    ),
    # Damage reduction
    "shield": (
        '<path d="M50 6 L88 18 C88 56 74 80 50 95 C26 80 12 56 12 18 Z" stroke-linejoin="round" stroke-width="2" stroke="white"/>',
        '<path d="M50 17 L78 26 C77 55 67 73 50 84 C33 73 23 55 22 26 Z" fill="none" stroke-width="3.5" stroke-linejoin="round"/>'
        '<path d="M50 17 L50 84" stroke-width="3.5"/>',
    ),
    # OnKill
    "sickle": (
        '<path d="M30 62 C18 34 38 8 66 10 C86 12 96 30 92 46 C86 28 70 20 54 24 C38 28 30 44 36 60 Z"/>'
        '<path d="M34 58 L14 90" stroke-width="10" stroke-linecap="round"/>',
        '<path d="M22 64 L34 71.5" stroke-width="3.5"/>',
    ),
    # OnDeath
    "skull": (
        '<circle cx="50" cy="44" r="34"/>'
        '<rect x="31" y="58" width="38" height="32" rx="6"/>',
        '<ellipse cx="37" cy="48" rx="9" ry="10"/>'
        '<ellipse cx="63" cy="48" rx="9" ry="10"/>'
        '<path d="M50 60 L44 70 L56 70 Z"/>'
        '<path d="M31 77 L69 77 M42 77 L42 90 M50 77 L50 90 M58 77 L58 90" stroke-width="3.5"/>',
    ),
    # Other
    "star": (
        '<path d="M50 4 Q55 45 96 50 Q55 55 50 96 Q45 55 4 50 Q45 45 50 4 Z"/>',
        "",
    ),
    # Turn start / end
    "sundial": (
        '<circle cx="50" cy="50" r="40" fill="none" stroke-width="8"/>'
        '<g stroke-width="4" stroke-linecap="round">'
        '<path d="M50 18 L50 26 M50 74 L50 82 M18 50 L26 50 M74 50 L82 50"/>'
        '<path d="M27.4 27.4 L31.6 31.6 M72.6 27.4 L68.4 31.6 M27.4 72.6 L31.6 68.4 M72.6 72.6 L68.4 68.4"/>'
        "</g>"
        '<path d="M50 50 L64 22 L56 52 Z" stroke-linejoin="round" stroke-width="2" stroke="white"/>'
        '<circle cx="50" cy="50" r="6"/>',
        "",
    ),
    # OnAttack (+ cleave): a leaf-bladed xiphos
    "sword": (
        '<g transform="rotate(45 50 50)">'
        '<path d="M50 2 C57 18 59 40 55 64 L45 64 C41 40 43 18 50 2 Z"/>'
        '<rect x="34" y="63" width="32" height="7" rx="2"/>'
        '<rect x="46" y="69" width="8" height="19"/>'
        '<circle cx="50" cy="91" r="6"/>'
        "</g>",
        '<g transform="rotate(45 50 50)"><path d="M50 14 L50 56" stroke-width="3" stroke-linecap="round"/></g>',
    ),
    # Taunt
    "target": (
        '<circle cx="50" cy="50" r="38" fill="none" stroke-width="10"/>'
        '<circle cx="50" cy="50" r="13"/>'
        '<path d="M50 2 L50 22 M50 78 L50 98 M2 50 L22 50 M78 50 L98 50" stroke-width="6"/>',
        "",
    ),
}


def build_svg(name, color, size):
    solid, cut = ICONS[name]
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 100 100">'
        f'<defs><mask id="m" maskUnits="userSpaceOnUse" x="0" y="0" width="100" height="100">'
        f'<g fill="white" stroke="white">{solid}</g>'
        f'<g fill="black" stroke="black">{cut}</g>'
        f"</mask></defs>"
        f'<rect width="100" height="100" fill="#{color}" mask="url(#m)"/>'
        f"</svg>"
    )


def find_browser():
    for path in BROWSERS:
        if os.path.exists(path):
            return path
    sys.exit("Need Chrome or Edge to render the SVGs.")


def render(browser, svg_path, png_path, size):
    # Wrap in a page so the SVG fills the viewport exactly, on a transparent background.
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False, encoding="utf-8") as page:
        page.write(
            "<html><body style='margin:0;background:transparent;overflow:hidden'>"
            f"<img src='file:///{svg_path.replace(os.sep, '/')}' width='{size}' height='{size}' style='display:block'>"
            "</body></html>"
        )
        page_path = page.name
    try:
        subprocess.run(
            [
                browser, "--headless=new", "--disable-gpu", "--hide-scrollbars",
                "--force-device-scale-factor=1", "--default-background-color=00000000",
                "--allow-file-access-from-files",
                f"--window-size={size},{size}", f"--screenshot={png_path}",
                "file:///" + page_path.replace(os.sep, "/"),
            ],
            check=True, capture_output=True,
        )
    finally:
        os.remove(page_path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--color", default=TERRACOTTA, help="hex colour without #")
    parser.add_argument("--out", default=OUT_DIR)
    parser.add_argument("--size", type=int, default=SIZE)
    parser.add_argument("--only", nargs="*", help="icon names to render (default: all)")
    args = parser.parse_args()

    os.makedirs(SVG_DIR, exist_ok=True)
    os.makedirs(args.out, exist_ok=True)
    browser = find_browser()
    args.out = os.path.abspath(args.out)
    for name in args.only or ICONS:
        svg_path = os.path.join(SVG_DIR, name + ".svg")
        with open(svg_path, "w", encoding="utf-8") as f:
            f.write(build_svg(name, args.color, args.size))
        render(browser, svg_path, os.path.join(args.out, name + ".png"), args.size)
        print(name)


if __name__ == "__main__":
    main()
