"""Card backs for the deck piles on the combat board (DeckPile), in black-figure pottery style.

Black-glaze ground, a fired-terracotta border band with a running wave crest (board objects use pottery motifs, never
the UI's Greek key), rosettes in the corners, and a terracotta tondo ringed with tongues. Inside the tondo, the
emblem is painted in black glaze with bone incisions, like a black-figure vase:
    player: a lyre (the hero's instrument)
    enemy:  a crested Corinthian helmet

    python Tools/Icons/card_back.py     # writes Assets/Resources/Images/CardBack/CardBack_Player.png, CardBack_Enemy.png,
                                        # CardBack_Edges.png and the SVGs in Tools/Icons/svg

Rendered with headless Chrome/Edge via summary_mono.render. The edge texture (the side of the stack: one stripe per
card) is drawn with numpy/PIL.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import summary_mono  # noqa: E402

ROOT = summary_mono.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Images", "CardBack")

GLAZE = "#1E1714"       # black glaze (Docs/StyleGuide.md, board objects)
GLAZE_HI = "#2E231D"    # soft sheen in the middle of the ground
CLAY = "#9C4A2A"        # fired terracotta: darker and redder than the UI's terracotta, matte
CLAY_LIGHT = "#AE5832"  # the tondo, a touch lighter so the emblem reads
BONE = "#CDBFA3"        # bone limestone, for incised detail

W, H = 100.0, 140.5     # the cards' own aspect (ViewCard collider 5.4 x 7.59)
PX_W, PX_H = 1024, 1440

BAND_OUT = 5.0          # border band, inset from the card edge
BAND_IN = 11.0
CX, CY = 50.0, H / 2    # tondo centre
EMBLEM_Y = 62.5         # the emblems are drawn round this centre and moved to CY
TONDO_R = 26.5
TONGUE_IN, TONGUE_OUT = 28.0, 32.5


def f(v):
    return f"{v:.2f}".rstrip("0").rstrip(".")


def rounded_rect(inset, radius, fill="none", stroke="none", width=0.0):
    return (
        f'<rect x="{f(inset)}" y="{f(inset)}" width="{f(W - 2 * inset)}" height="{f(H - 2 * inset)}" '
        f'rx="{f(radius)}" fill="{fill}" stroke="{stroke}" stroke-width="{f(width)}"/>'
    )


def wave_band():
    """Black wave crests running clockwise round the clay band. Each crest is drawn in band space (u along, v across
    the band from the outer edge) and mapped onto the side it sits on."""
    depth = BAND_IN - BAND_OUT
    parts = []
    sides = [
        # start point, along direction, inward direction, length
        ((BAND_IN, BAND_OUT), (1, 0), (0, 1), W - 2 * BAND_IN),
        ((W - BAND_OUT, BAND_IN), (0, 1), (-1, 0), H - 2 * BAND_IN),
        ((W - BAND_IN, H - BAND_OUT), (-1, 0), (0, -1), W - 2 * BAND_IN),
        ((BAND_OUT, H - BAND_IN), (0, -1), (1, 0), H - 2 * BAND_IN),
    ]
    for (sx, sy), (ax, ay), (nx, ny), length in sides:
        count = max(1, round(length / 7.5))
        unit = length / count

        def pt(u, v):
            # v = 0 at the inner edge of the band (the crest's base), rising toward the outer edge
            vv = depth - v
            return sx + ax * u + nx * vv, sy + ay * u + ny * vv

        for i in range(count):
            u0 = i * unit
            s = unit / 7.5
            d = depth / 6.0
            # A wave crest: base along the inner edge, rising and curling forward into a small spiral.
            local = [
                ("M", [(0.0, 0.0)]),
                ("L", [(0.0, 1.2)]),
                ("C", [(0.6, 4.2), (3.6, 5.4), (5.4, 4.6)]),
                ("C", [(7.0, 3.9), (6.8, 1.6), (5.2, 1.5)]),
                ("C", [(4.0, 1.4), (3.7, 2.8), (4.6, 3.0)]),
                ("C", [(3.4, 3.4), (2.6, 2.0), (3.6, 1.0)]),
                ("C", [(4.4, 0.4), (6.6, 0.3), (7.5, 0.0)]),
                ("Z", []),
            ]
            d_str = ""
            for cmd, pts in local:
                d_str += cmd
                coords = []
                for (u, v) in pts:
                    x, y = pt(u0 + u * s, v * d)
                    coords.append(f"{f(x)} {f(y)}")
                d_str += " ".join(coords) + " "
            parts.append(d_str)
    return f'<path d="{"".join(parts)}" fill="{GLAZE}"/>'


def corner_rosettes():
    out = []
    size = BAND_IN - BAND_OUT
    for cx, cy in [(BAND_OUT, BAND_OUT), (W - BAND_IN, BAND_OUT), (W - BAND_IN, H - BAND_IN), (BAND_OUT, H - BAND_IN)]:
        mx, my = cx + size / 2, cy + size / 2
        out.append(f'<rect x="{f(cx)}" y="{f(cy)}" width="{f(size)}" height="{f(size)}" fill="{CLAY}"/>')
        petals = []
        for k in range(8):
            a = k * math.pi / 4
            px, py = mx + math.cos(a) * 1.75, my + math.sin(a) * 1.75
            petals.append(f'<circle cx="{f(px)}" cy="{f(py)}" r="0.85" fill="{GLAZE}"/>')
        out.extend(petals)
        out.append(f'<circle cx="{f(mx)}" cy="{f(my)}" r="0.95" fill="{GLAZE}"/>')
    return "".join(out)


def tongues():
    """A ring of tongues round the tondo: clay tongues with black between, their rounded tips pointing out."""
    count = 36
    out = [f'<circle cx="{CX}" cy="{CY}" r="{f(TONGUE_OUT + 0.9)}" fill="{GLAZE}"/>']
    for k in range(count):
        a0 = (k + 0.14) / count * 2 * math.pi
        a1 = (k + 0.86) / count * 2 * math.pi
        am = (a0 + a1) / 2

        def p(a, r):
            return CX + math.cos(a) * r, CY + math.sin(a) * r

        x0, y0 = p(a0, TONGUE_IN)
        x1, y1 = p(a0, TONGUE_OUT - 1.2)
        xt, yt = p(am, TONGUE_OUT + 0.3)
        x2, y2 = p(a1, TONGUE_OUT - 1.2)
        x3, y3 = p(a1, TONGUE_IN)
        out.append(
            f'<path d="M{f(x0)} {f(y0)} L{f(x1)} {f(y1)} Q{f(xt)} {f(yt)} {f(x2)} {f(y2)} L{f(x3)} {f(y3)} Z" fill="{CLAY}"/>'
        )
    out.append(f'<circle cx="{CX}" cy="{CY}" r="{f(TONGUE_IN - 0.2)}" fill="{GLAZE}"/>')
    return "".join(out)


def palmette(x, y, flip):
    """A small palmette (fan of leaves on a pair of volutes) in clay, above or below the tondo."""
    s = -1 if flip else 1
    leaves = []
    for k in range(-3, 4):
        a = math.radians(k * 21)
        length = 6.2 - abs(k) * 0.55
        tx, ty = x + math.sin(a) * length, y - s * math.cos(a) * length
        bx, by = x + math.sin(a) * 1.6, y - s * math.cos(a) * 1.6
        # leaf: a narrow teardrop from near the base to the tip
        nx, ny = math.cos(a) * 0.95, s * math.sin(a) * 0.95
        mx, my = (bx + tx) / 2, (by + ty) / 2
        leaves.append(
            f'<path d="M{f(bx)} {f(by)} Q{f(mx + nx)} {f(my + ny)} {f(tx)} {f(ty)} Q{f(mx - nx)} {f(my - ny)} {f(bx)} {f(by)} Z" fill="{CLAY}"/>'
        )
    volutes = (
        f'<path d="M{f(x - 5.2)} {f(y + s * 0.4)} C{f(x - 5.4)} {f(y + s * 2.6)} {f(x - 2.2)} {f(y + s * 2.6)} {f(x)} {f(y + s * 1.0)} '
        f'C{f(x + 2.2)} {f(y + s * 2.6)} {f(x + 5.4)} {f(y + s * 2.6)} {f(x + 5.2)} {f(y + s * 0.4)}" '
        f'fill="none" stroke="{CLAY}" stroke-width="0.9" stroke-linecap="round"/>'
        f'<circle cx="{f(x - 5.2)}" cy="{f(y + s * 0.4)}" r="0.8" fill="{CLAY}"/>'
        f'<circle cx="{f(x + 5.2)}" cy="{f(y + s * 0.4)}" r="0.8" fill="{CLAY}"/>'
        f'<circle cx="{f(x)}" cy="{f(y + s * 0.6)}" r="1.1" fill="{CLAY}"/>'
    )
    return "".join(leaves) + volutes


def egg_and_dart(y, flip):
    """A short frieze of egg-and-dart in clay between thin clay rules, across the space above or below the tondo."""
    x0, x1 = BAND_IN + 6.0, W - BAND_IN - 6.0
    h = 4.2
    top = y - h / 2
    out = [
        f'<path d="M{f(x0)} {f(top - 0.9)} H{f(x1)} M{f(x0)} {f(top + h + 0.9)} H{f(x1)}" stroke="{CLAY}" stroke-width="0.45"/>'
    ]
    count = 11
    step = (x1 - x0) / count
    for i in range(count):
        cx = x0 + step * (i + 0.5)
        # egg: a clay oval with a black core; dart: a small clay point between the eggs
        out.append(f'<ellipse cx="{f(cx)}" cy="{f(y)}" rx="{f(step * 0.36)}" ry="{f(h * 0.5)}" fill="{CLAY}"/>')
        out.append(f'<ellipse cx="{f(cx)}" cy="{f(y + (0.35 if not flip else -0.35))}" rx="{f(step * 0.18)}" ry="{f(h * 0.27)}" fill="{GLAZE}"/>')
        if i < count - 1:
            dx = x0 + step * (i + 1)
            tip = top + h if not flip else top
            base = top if not flip else top + h
            out.append(f'<path d="M{f(dx - 0.55)} {f(base)} L{f(dx + 0.55)} {f(base)} L{f(dx)} {f(tip)} Z" fill="{CLAY}"/>')
    return "".join(out)


# ---- Emblems (black glaze on the tondo, details incised in bone) ----

def lyre():
    # Sound box: a tortoise-shell bowl.
    box = "M37.5 73 C37.5 67.5 62.5 67.5 62.5 73 C62.5 81.5 56.5 86.5 50 86.5 C43.5 86.5 37.5 81.5 37.5 73 Z"
    arm_l = "M42 71 C35.5 65 31 57 33.5 49.5 C35.2 44.5 37.6 42 35.6 37.2"
    arm_r = "M58 71 C64.5 65 69 57 66.5 49.5 C64.8 44.5 62.4 42 64.4 37.2"
    yoke = "M31.6 43.6 L68.4 43.6"
    strings_top, strings_bottom = 43.6, 78.2
    xs_top = [41.5 + i * (17.0 / 6) for i in range(7)]
    xs_bot = [44.2 + i * (11.6 / 6) for i in range(7)]
    strings = " ".join(f"M{f(a)} {f(strings_top)} L{f(b)} {f(strings_bottom)}" for a, b in zip(xs_top, xs_bot))
    shell = "M40.2 80.2 C44 85 56 85 59.8 80.2"
    return (
        # strings in black over the clay, in bone where they cross the black box
        f'<clipPath id="box"><path d="{box}"/></clipPath>'
        f'<path d="{strings}" stroke="{GLAZE}" stroke-width="0.55" fill="none"/>'
        f'<path d="{arm_l} {arm_r}" stroke="{GLAZE}" stroke-width="3.6" fill="none" stroke-linecap="round"/>'
        f'<path d="{yoke}" stroke="{GLAZE}" stroke-width="2.3" fill="none" stroke-linecap="round"/>'
        f'<circle cx="31.4" cy="43.6" r="1.7" fill="{GLAZE}"/><circle cx="68.6" cy="43.6" r="1.7" fill="{GLAZE}"/>'
        f'<circle cx="35.4" cy="36.4" r="1.9" fill="{GLAZE}"/><circle cx="64.6" cy="36.4" r="1.9" fill="{GLAZE}"/>'
        f'<path d="{box}" fill="{GLAZE}"/>'
        f'<g clip-path="url(#box)"><path d="{strings}" stroke="{BONE}" stroke-width="0.45" fill="none"/></g>'
        f'<path d="{shell}" stroke="{BONE}" stroke-width="0.5" fill="none" stroke-linecap="round" stroke-linejoin="round"/>'
        # the bridge the strings rest on, and an incised line down each arm
        f'<path d="M43.4 78.2 L56.6 78.2" stroke="{BONE}" stroke-width="0.9" stroke-linecap="round"/>'
        f'<path d="M41 69.6 C35.5 64 31.8 56.8 33.9 50.2 M59 69.6 C64.5 64 68.2 56.8 66.1 50.2" '
        f'stroke="{BONE}" stroke-width="0.4" fill="none" stroke-linecap="round"/>'
    )


def helmet():
    # Corinthian helmet seen from the front, with a crest set crosswise over the dome. The eye openings and the slit
    # between the cheek pieces show the clay behind; the nose guard divides the eyes.
    shell = (
        "M50 45 C60.5 45 66.5 52.5 66.5 62 C66.5 70 67.5 78 69.4 85.6 C63 86.8 56 86.2 52 85.2 "
        "L48 85.2 C44 86.2 37 86.8 30.6 85.6 C32.5 78 33.5 70 33.5 62 C33.5 52.5 39.5 45 50 45 Z"
    )
    eyes = (
        "M36.2 60.8 C39.8 57.4 45.8 57.6 49.2 61.4 L49.2 64.4 C45 65.4 39.8 64.4 36.2 60.8 Z "
        "M63.8 60.8 C60.2 57.4 54.2 57.6 50.8 61.4 L50.8 64.4 C55 65.4 60.2 64.4 63.8 60.8 Z "
        "M48.3 61 L51.7 61 L51.7 86.4 L48.3 86.4 Z"
    )
    nose = "M48.5 55.5 L51.5 55.5 L51.5 68.2 Q50 71 48.5 68.2 Z"
    crest = "M30 49.5 C30 32.5 70 32.5 70 49.5 C66.5 44 58.5 42.2 50 42.2 C41.5 42.2 33.5 44 29.5 49.5 Z"
    hair = []
    for i in range(1, 14):
        a = math.pi * (0.1 + 0.8 * i / 14)
        r0, r1 = 12.5, 18.2
        hair.append(
            f"M{f(50 - math.cos(a) * r0)} {f(52 - math.sin(a) * r0 * 0.78)} "
            f"L{f(50 - math.cos(a) * r1)} {f(52 - math.sin(a) * r1 * 0.78)}"
        )
    incisions = (
        "M35.4 57.6 C39.6 53.4 46 53.4 49.4 56.6 M64.6 57.6 C60.4 53.4 54 53.4 50.6 56.6 "  # brows
        "M35.2 66 C35.6 72 35.4 78 33.8 83.6 M64.8 66 C64.4 72 64.6 78 66.2 83.6"          # cheek-piece edges
    )
    return (
        '<g transform="translate(0 2.2)">'
        f'<clipPath id="crest"><path d="{crest}"/></clipPath>'
        f'<path d="{crest}" fill="{GLAZE}"/>'
        f'<g clip-path="url(#crest)"><path d="{" ".join(hair)}" stroke="{BONE}" stroke-width="0.4" fill="none"/></g>'
        f'<path d="M46.6 43 L53.4 43 L52.6 46.6 L47.4 46.6 Z" fill="{GLAZE}"/>'
        f'<path d="{shell}" fill="{GLAZE}"/>'
        f'<path d="{eyes}" fill="{CLAY_LIGHT}"/>'
        f'<path d="{nose}" fill="{GLAZE}"/>'
        f'<path d="{incisions}" stroke="{BONE}" stroke-width="0.5" fill="none" stroke-linecap="round"/>'
        "</g>"
    )


def build_svg(emblem):
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{PX_W}" height="{PX_H}" viewBox="0 0 {f(W)} {f(H)}">'
        f'<defs><radialGradient id="sheen" cx="50%" cy="48%" r="62%">'
        f'<stop offset="0" stop-color="{GLAZE_HI}"/><stop offset="1" stop-color="{GLAZE}"/></radialGradient></defs>'
        # Full bleed: the mesh's rounded corners trim the texture, so the ground runs to the edges.
        f'<rect x="0" y="0" width="{f(W)}" height="{f(H)}" fill="url(#sheen)"/>'
        + rounded_rect(2.4, 3.2, stroke=CLAY, width=0.7)
        + f'<path d="M{f(BAND_OUT)} {f(BAND_OUT)} H{f(W - BAND_OUT)} V{f(H - BAND_OUT)} H{f(BAND_OUT)} Z '
        f'M{f(BAND_IN)} {f(BAND_IN)} V{f(H - BAND_IN)} H{f(W - BAND_IN)} V{f(BAND_IN)} Z" fill="{CLAY}" fill-rule="evenodd"/>'
        + wave_band()
        + corner_rosettes()
        + rounded_rect(BAND_IN + 1.6, 0.6, stroke=CLAY, width=0.45)
        + tongues()
        + f'<circle cx="{CX}" cy="{CY}" r="{f(TONDO_R)}" fill="{CLAY_LIGHT}"/>'
        + f'<circle cx="{CX}" cy="{CY}" r="{f(TONDO_R - 1.3)}" fill="none" stroke="{GLAZE}" stroke-width="0.5"/>'
        + f'<g transform="translate(0 {f(CY - EMBLEM_Y)})">{emblem}</g>'
        + egg_and_dart(BAND_IN + (CY - TONGUE_OUT - 6.0 - BAND_IN) * 0.5, False)
        + egg_and_dart(H - BAND_IN - (CY - TONGUE_OUT - 6.0 - BAND_IN) * 0.5, True)
        + palmette(CX, CY - TONGUE_OUT - 2.6, False)
        + palmette(CX, CY + TONGUE_OUT + 2.6, True)
        + "</svg>"
    )


def build_edges(path):
    """The side of a stack: one card per 8 px row, a lighter card edge between dark seams, slightly uneven along
    its length. Tiles vertically; DeckPile maps one row per card."""
    import numpy as np
    from PIL import Image

    rng = np.random.default_rng(7)
    width, cards, row = 256, 16, 8
    img = np.zeros((cards * row, width, 3), dtype=np.float32)
    seam = np.array([0.075, 0.058, 0.048])     # near-black seams between cards
    edge = np.array([0.31, 0.25, 0.20])        # the card stock: dark warm bone, so the stack never glares
    for c in range(cards):
        tone = 0.86 + rng.random() * 0.22
        wobble = np.cumsum(rng.normal(0, 0.02, width))
        wobble -= wobble.mean()
        for r in range(row):
            t = r / (row - 1)
            # bright in the middle of each card's edge, falling to the seam at both ends
            profile = math.sin(t * math.pi) ** 0.6
            colour = seam + (edge * tone - seam) * profile
            img[c * row + r] = colour[None, :] * (1.0 + wobble[:, None] * 0.6 + rng.normal(0, 0.015, (width, 1)))
    img = np.clip(img, 0, 1) ** (1 / 2.2)  # stored sRGB
    Image.fromarray((img * 255).astype(np.uint8)).save(path)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(summary_mono.SVG_DIR, exist_ok=True)
    browser = summary_mono.find_browser()
    for name, emblem in [("CardBack_Player", lyre()), ("CardBack_Enemy", helmet())]:
        svg_path = os.path.join(summary_mono.SVG_DIR, name.lower() + ".svg")
        with open(svg_path, "w", encoding="utf-8") as fh:
            fh.write(build_svg(emblem))
        png = os.path.join(OUT_DIR, name + ".png")
        summary_mono.render(browser, svg_path, png, PX_W, PX_H)
        print(png)
    edges = os.path.join(OUT_DIR, "CardBack_Edges.png")
    build_edges(edges)
    print(edges)


if __name__ == "__main__":
    main()
