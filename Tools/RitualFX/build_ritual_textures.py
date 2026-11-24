"""Builds the textures the ritual invocation VFX uses (RitualInvocationVfx / RitualTheme).

  Assets/Resources/Images/RitualFX/Sigils/<God>.png   the god's emblem as a glowing glyph (white; tinted in game)
  Assets/Resources/Images/RitualFX/<Name>.png         particle and ring sprites (white; tinted in game)

The sigils reuse the emblem shapes the altar corners are carved with (Tools/Blender/altar_textures.py, CORNERS), so a
god's sigil always matches its altar. That file is read and run up to its texture-writing code with a stand-in for
Blender's bpy module, so this script needs only numpy and Pillow:
    python Tools/RitualFX/build_ritual_textures.py
"""
import math
import os
import sys
import types

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Images", "RitualFX")
ALTAR_SCRIPT = os.path.join(ROOT, "Tools", "Blender", "altar_textures.py")

# god -> altar corner emblem (see STYLES in altar_textures.py)
GOD_EMBLEMS = {
    "Default": "boss", "Poseidon": "trident", "Zeus": "bolt", "Aphrodite": "rose", "Apollo": "sun", "Ares": "shield",
    "Athena": "owl", "Demeter": "wheat", "Dionysus": "grapes", "Hades": "skull", "Hephaestus": "hammer",
    "Hermes": "caduceus", "Muse": "lyre", "Hestia": "flame", "Hera": "peacock", "OldOnes": "coil",
}


def load_altar_shapes():
    src = open(ALTAR_SCRIPT, encoding="utf-8").read()
    src = src[:src.index("def relief_set")]
    sys.modules.setdefault("bpy", types.ModuleType("bpy"))
    saved = sys.argv
    sys.argv = ["altar_textures.py", "--", OUT]
    ns = {"__name__": "altar_shapes"}
    try:
        exec(compile(src, ALTAR_SCRIPT, "exec"), ns)
    finally:
        sys.argv = saved
    return ns


A = load_altar_shapes()
smoothstep = A["smoothstep"]


def grid(n):
    y, x = np.mgrid[0:n, 0:n].astype(float)
    return (x + 0.5) / n, (y + 0.5) / n


def blur(a, radius_px, passes=3):
    r = max(1, int(radius_px))
    k = np.ones(2 * r + 1) / (2 * r + 1)
    out = a
    for _ in range(passes):
        out = np.apply_along_axis(lambda v: np.convolve(v, k, mode="same"), 0, out)
        out = np.apply_along_axis(lambda v: np.convolve(v, k, mode="same"), 1, out)
    return out


def save_alpha(name, alpha, rgb=None):
    """alpha: (h, w), row 0 = bottom. Saves white (or rgb) with that alpha."""
    a = np.clip(alpha, 0, 1)[::-1]
    h, w = a.shape
    img = np.ones((h, w, 4))
    if rgb is not None:
        img[..., :3] = np.clip(rgb[::-1], 0, 1)
    img[..., 3] = a
    path = os.path.join(OUT, name + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray((img * 255).round().astype(np.uint8), "RGBA").save(path)
    print("wrote", os.path.relpath(path, ROOT))


# ----------------------------------------------------------------- sigils ---
def sigil(god, emblem, n=512):
    x, y = grid(n)
    # A margin so the glow never clips at the edge: the emblem fills the middle 80%.
    u, v = (x - 0.5) / 0.8 + 0.5, (y - 0.5) / 0.8 + 0.5
    h = A["CORNERS"][emblem](u, v)
    shape = smoothstep(0.04, 0.16, h)
    # Brightness follows the carving's height, so low relief (the owl's head, the skull's eye sockets, the gaps in the
    # coil) reads as dimmer glass and the raised strokes burn brightest. Edges are traced so overlapping parts separate.
    lit = np.clip(h, 0, 1) ** 0.9
    gy, gx = np.gradient(blur(h, 1, 1))
    edge = np.clip(np.sqrt(gx * gx + gy * gy) * n * 0.05, 0, 1)
    core = shape * np.clip(0.18 + 0.82 * lit + 0.35 * edge, 0, 1)
    glow = blur(shape, n * 0.022)
    wide = blur(shape, n * 0.06)
    # the halo only outside the glyph, so it never washes out the shading inside
    alpha = np.clip(core + (glow * 0.55 + wide * 0.35) * (1 - shape), 0, 1)
    save_alpha("Sigils/" + god, alpha)


# -------------------------------------------------------------- particles ---
def radial(n=128):
    x, y = grid(n)
    return np.sqrt((x - 0.5) ** 2 + (y - 0.5) ** 2) * 2, x, y


def p_glow():
    r, _, _ = radial()
    save_alpha("Glow", np.exp(-(r * 2.6) ** 2) * smoothstep(1.0, 0.85, r))


def p_ember():
    r, x, y = radial()
    noise = A["fbm"](128, 128, 2.2, 5)
    a = np.exp(-(r * 3.2) ** 2) + 0.35 * np.exp(-(r * 1.8) ** 2) * noise
    save_alpha("Ember", np.clip(a, 0, 1) * smoothstep(1.0, 0.8, r))


def p_mote():
    r, x, y = radial()
    dx, dy = np.abs(x - 0.5) * 2, np.abs(y - 0.5) * 2
    star = np.exp(-dx * 14) * np.exp(-dy * 2.2) + np.exp(-dy * 14) * np.exp(-dx * 2.2)
    a = np.clip(np.exp(-(r * 5) ** 2) + star * 0.8, 0, 1)
    save_alpha("Mote", a * smoothstep(1.0, 0.8, r))


def p_petal():
    x, y = grid(128)
    h = A["leaf"](x, y, 0.5, 0.5, 0.4, 0.22, math.pi / 2)
    a = smoothstep(0.0, 0.25, h)
    shade = 0.75 + 0.25 * y
    save_alpha("Petal", a, np.stack([shade] * 3, axis=-1))


def p_leaf():
    x, y = grid(128)
    h = A["leaf"](x, y, 0.5, 0.5, 0.42, 0.13, math.pi / 2)
    vein = 1 - 0.35 * np.exp(-((x - 0.5) * 60) ** 2)
    save_alpha("Leaf", smoothstep(0.0, 0.2, h), np.stack([vein] * 3, axis=-1))


def p_feather():
    x, y = grid(128)
    h = A["feather"](x, y, 0.5, 0.5, 0.44, 0.15, math.pi / 2)
    save_alpha("Feather", smoothstep(0.02, 0.2, h) * 0.95)


def p_droplet():
    x, y = grid(128)
    pts = A["teardrop_points"](0.5, 0.38, 0.2, 0.5)
    h = A["filled_polygon_height"](x, y, pts, 0.08)
    hi = np.exp(-(((x - 0.44) / 0.05) ** 2 + ((y - 0.42) / 0.08) ** 2))
    shade = np.clip(0.8 + 0.4 * hi, 0, 1)
    save_alpha("Droplet", smoothstep(0, 0.6, h), np.stack([shade] * 3, axis=-1))


def p_shard():
    x, y = grid(128)
    pts = [(0.5, 0.92), (0.66, 0.42), (0.55, 0.1), (0.36, 0.38)]
    h = A["filled_polygon_height"](x, y, pts, 0.03)
    save_alpha("Shard", smoothstep(0, 0.5, h))


def p_smoke():
    r, x, y = radial(256)
    noise = A["fbm"](256, 256, 2.0, 9)
    a = np.clip((noise - 0.25) * 1.6, 0, 1) * np.exp(-(r * 1.6) ** 2) * 1.3
    save_alpha("Smoke", np.clip(a, 0, 1) * smoothstep(1.0, 0.75, r))


def p_ring():
    r, _, _ = radial(512)
    a = np.exp(-((r - 0.9) / 0.035) ** 2) + 0.35 * np.exp(-((r - 0.84) / 0.09) ** 2)
    save_alpha("Ring", np.clip(a, 0, 1))


def p_halo():
    """The ground seal under the target: two rings, a band of short radial strokes, and dots. No meander (that's UI)."""
    n = 1024
    r, x, y = radial(n)
    ang = np.arctan2(y - 0.5, x - 0.5)
    a = np.exp(-((r - 0.94) / 0.008) ** 2) + 0.8 * np.exp(-((r - 0.80) / 0.006) ** 2)
    ticks = (np.cos(ang * 48) > 0.93) * smoothstep(0.82, 0.84, r) * smoothstep(0.92, 0.90, r)
    dots = np.zeros_like(r)
    for k in range(12):
        t = k / 12 * 2 * math.pi
        dots = np.maximum(dots, np.exp(-(((x - 0.5 - 0.435 * math.cos(t)) ** 2 + (y - 0.5 - 0.435 * math.sin(t)) ** 2) / 0.0001)))
    inner = 0.5 * np.exp(-((r - 0.62) / 0.005) ** 2)
    glow = 0.18 * np.exp(-((r - 0.85) / 0.12) ** 2)
    save_alpha("Halo", np.clip(a + ticks * 0.9 + dots + inner + glow, 0, 1))


def p_flash():
    r, x, y = radial(512)
    ang = np.arctan2(y - 0.5, x - 0.5)
    rays = np.clip(np.cos(ang * 6) * 0.5 + 0.5, 0, 1) ** 18 * np.exp(-r * 2.2)
    core = np.exp(-(r * 3.2) ** 2) + 0.4 * np.exp(-(r * 1.4) ** 2)
    save_alpha("Flash", np.clip(core + rays * 0.8, 0, 1) * smoothstep(1.0, 0.85, r))


def p_beam():
    n = 256
    x, y = grid(n)
    across = np.exp(-((x - 0.5) / 0.22) ** 2)
    along = smoothstep(1.0, 0.35, y) * smoothstep(0.0, 0.06, y)
    save_alpha("Beam", across * along)


def main():
    for god, emblem in GOD_EMBLEMS.items():
        sigil(god, emblem)
    for fn in (p_glow, p_ember, p_mote, p_petal, p_leaf, p_feather, p_droplet, p_shard, p_smoke, p_ring, p_halo,
               p_flash, p_beam):
        fn()


if __name__ == "__main__":
    main()
