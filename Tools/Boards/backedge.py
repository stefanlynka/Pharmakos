"""3D back edges: the piece that stands along the far edge of a combat board (Troy's wall, Delphi's colonnade, the Fates'
loom...). A recipe builds a BackEdge (mesh primitives in board world units, each tagged with a material) and paints one
texture set per material; save() writes the mesh as JSON and the textures into the board's folder.
In Unity, Tools -> Boards -> Build Back Edges turns them into a mesh, materials and a prefab and plugs the prefab into
the board's BoardStyle.BackEdge (see Assets/Scripts/Editor/BoardBackEdgeBuilder.cs and Docs/Boards.md). Rebuild only these: Tools/Boards/backedges.py.

Coordinates: x across the board, y up the board (the enemy side is high y), h = height out of the board toward the
camera. The board plane is world z = 33, so a point is world (x, y, 33 - h).

The camera looks almost straight down: height climbs up the screen at about 0.016 of the screen per unit, and the top
of the frame is around y 41.8 at the board plane. Anything whose foot is at y ~34 shows about 6 units of height before
it leaves the frame; taller things leave the frame (fine for columns, trees, gates: it suggests their size).
Keep everything at y >= ~33.5 around x -9..9 (the enemy portrait) and y >= ~34.5 over x 8..29 (its offerings).

Texture layout, per material: u runs x -HALF..HALF across the full width; v 0..ELEV is an elevation (height 0..tex_h)
of the material as seen from the front; v ELEV..1 is a band for surfaces seen from above (tops). Side faces run the
elevation sideways (u = x + depth), lathed shapes wrap u around their circumference.
"""
import json
import math
import os

import numpy as np

from boardgen import Canvas, F32, normals, save_set

HALF = 75.0
TEX_W = 4096
PPU = TEX_W / (2 * HALF)          # texels per unit, the same across and up
TOP_ROWS = 96
SELF_LIGHT_GAIN = 1.7   # back edges stand outside the spotlights; scales every material's self-light
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
JSON_DIR = os.path.join(ROOT, 'Claude', 'Boards', 'backedge_json')   # regenerated input for the Unity builder


class Material:
    """name, height the elevation texture covers, self_light (0..1: emission from the albedo, standing in for bounce
    light the spotlights don't give vertical faces), cutout (alpha-tested albedo), smoothness scale."""

    def __init__(self, name, tex_h=12.0, self_light=0.4, cutout=False, emission_strength=1.0):
        self.name = name
        self.tex_h = tex_h
        self.self_light = self_light
        self.cutout = cutout
        self.emission_strength = emission_strength
        self.elev_rows = int(round(tex_h * PPU))
        self.rows = self.elev_rows + TOP_ROWS
        self.elev_v = self.elev_rows / self.rows
        self.top_depth = TOP_ROWS / PPU

    def elev_canvas(self):
        return Canvas((-HALF, HALF, 0.0, self.tex_h), (TEX_W, self.elev_rows))

    def top_canvas(self):
        return Canvas((-HALF, HALF, 0.0, self.top_depth), (TEX_W, TOP_ROWS))

    def uv_elev(self, along, h):
        return ((along + HALF) / (2 * HALF), min(max(h / self.tex_h, 0.0), 1.0) * self.elev_v)

    def uv_top(self, x, y):
        d = (y % self.top_depth) / self.top_depth
        return ((x + HALF) / (2 * HALF), self.elev_v + (1 - self.elev_v) * d)


class BackEdge:
    def __init__(self, name):
        self.name = name
        self.materials = {}
        self.parts = {}           # material name -> (verts, normals, uvs, tris)

    def material(self, mat):
        self.materials[mat.name] = mat
        self.parts.setdefault(mat.name, ([], [], [], []))
        return mat

    # ------------------------------------------------------------ primitives ---
    def _quad(self, mat, a, b, c, d, outward, uv_mode='auto', along_shift=0.0):
        """a..d: board points (x, y, h). outward: rough outward direction in board space (x, y, h)."""
        verts, norms, uvs, tris = self.parts[mat.name]
        A, B, Cc, D = (np.array(p, F32) for p in (a, b, c, d))
        n = np.cross(B - A, Cc - A)
        ln = float(np.linalg.norm(n))
        if ln < 1e-9:
            return
        n /= ln
        if float(np.dot(n, np.array(outward, F32))) < 0:
            B, D = D, B
            n = -n
        i = len(verts)
        for p in (A, B, Cc, D):
            verts.append(p)
            norms.append(n)
            if uv_mode == 'top' or (uv_mode == 'auto' and abs(n[2]) > 0.8):
                uvs.append(mat.uv_top(p[0], p[1]))
            elif uv_mode == 'side' or (uv_mode == 'auto' and abs(n[0]) > 0.7):
                uvs.append(mat.uv_elev(p[0] + (p[1] - 30.0) + along_shift, p[2]))
            else:
                uvs.append(mat.uv_elev(p[0] + along_shift, p[2]))
        tris.extend([i, i + 1, i + 2, i, i + 2, i + 3])

    def box(self, mat, x0, x1, y0, y1, h0, h1, batter=0.0, sides=True, back=False, bottom=False, top_mat=None,
            top=True):
        """Block from (x0, y0) to (x1, y1) and height h0..h1. batter slopes the front face back by batter per unit.
        top_mat: a different material for the top face (e.g. a pale coping on a merlon)."""
        yt = y0 + batter * (h1 - h0)
        P = lambda x, y, h: (x, y, h)
        self._quad(mat, P(x0, y0, h0), P(x1, y0, h0), P(x1, yt, h1), P(x0, yt, h1), (0, -1, batter))      # front
        if top:
            self._quad(top_mat or mat, P(x0, yt, h1), P(x1, yt, h1), P(x1, y1, h1), P(x0, y1, h1), (0, 0, 1))
        if sides:
            self._quad(mat, P(x0, y0, h0), P(x0, yt, h1), P(x0, y1, h1), P(x0, y1, h0), (-1, 0, 0))
            self._quad(mat, P(x1, y0, h0), P(x1, yt, h1), P(x1, y1, h1), P(x1, y1, h0), (1, 0, 0))
        if back:
            self._quad(mat, P(x0, y1, h0), P(x1, y1, h0), P(x1, y1, h1), P(x0, y1, h1), (0, 1, 0))
        if bottom:
            self._quad(mat, P(x0, y0, h0), P(x1, y0, h0), P(x1, y1, h0), P(x0, y1, h0), (0, 0, -1))

    def lathe(self, mat, cx, cy, profile, segments=20, flutes=0, flute_depth=0.0, cap=True, wobble=None):
        """Turned shape standing at (cx, cy): profile = [(h, radius), ...] bottom to top. flutes cut shallow grooves
        (columns). wobble(theta, h) -> radius factor for irregular things (trunks, stalagmites).
        u wraps the circumference starting from the front."""
        verts, norms, uvs, tris = self.parts[mat.name]
        rings = []
        circ_u = lambda r, t: cx + (t / (2 * math.pi) - 0.5) * 2 * math.pi * max(r, 0.3)
        for h, r in profile:
            ring = []
            for k in range(segments + 1):
                t = 2 * math.pi * k / segments
                a = t - math.pi / 2           # k = 0 faces the camera side (-y)
                rr = r
                if flutes:
                    rr = r * (1 - flute_depth * max(0.0, math.cos(t * flutes)) ** 2)
                if wobble is not None:
                    rr *= wobble(t, h)
                ring.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr, h, t, r))
            rings.append(ring)
        for j in range(len(rings) - 1):
            for k in range(segments):
                quad = [rings[j][k], rings[j][k + 1], rings[j + 1][k + 1], rings[j + 1][k]]
                i = len(verts)
                for (x, y, h, t, r) in quad:
                    verts.append(np.array((x, y, h), F32))
                    nx, ny = x - cx, y - cy
                    dr = (rings[j + 1][k][4] - rings[j][k][4])
                    dh = max(rings[j + 1][k][2] - rings[j][k][2], 1e-4)
                    nv = np.array((nx, ny, -dr / dh * math.hypot(nx, ny)), F32)
                    nv /= max(float(np.linalg.norm(nv)), 1e-6)
                    norms.append(nv)
                    uvs.append(mat.uv_elev(circ_u(r, t), h))
                tris.extend([i, i + 1, i + 2, i, i + 2, i + 3])
        if cap and profile[-1][1] > 0.01:
            top = rings[-1]
            c = len(verts)
            verts.append(np.array((cx, cy, profile[-1][0]), F32))
            norms.append(np.array((0, 0, 1), F32))
            uvs.append(mat.uv_top(cx, cy))
            for k in range(segments + 1):
                x, y, h, t, r = top[k]
                verts.append(np.array((x, y, h), F32))
                norms.append(np.array((0, 0, 1), F32))
                uvs.append(mat.uv_top(x, y))
            for k in range(segments):
                tris.extend([c, c + 1 + k, c + 2 + k])

    def relief(self, mat, x0, x1, y_of, top_of, depth_of, step=0.5, h_step=0.5, h0=0.0, back=0.0):
        """A rock face (heightfield standing up from the board): for each x, the foot sits at y_of(x), the top at
        height top_of(x); depth_of(x, h) pushes points back (+y) to make it rugged. Smooth normals."""
        verts, norms, uvs, tris = self.parts[mat.name]
        xs = np.arange(x0, x1 + 1e-6, step)
        hmax = max(top_of(x) for x in xs)
        nh = int(math.ceil((hmax - h0) / h_step)) + 1
        grid = np.zeros((len(xs), nh, 3), F32)
        for i, x in enumerate(xs):
            top = top_of(x)
            for j in range(nh):
                t = j / (nh - 1)
                h = h0 + (top - h0) * t
                grid[i, j] = (x, y_of(x) + depth_of(x, h), h)
        # normals from the grid
        gx = np.gradient(grid, axis=0)
        gh = np.gradient(grid, axis=1)
        n = np.cross(gx, gh)
        n /= np.maximum(np.linalg.norm(n, axis=2, keepdims=True), 1e-6)
        n[n[..., 1] > 0] *= -1                                   # face the camera side (-y)
        base = len(verts)
        for i in range(len(xs)):
            for j in range(nh):
                p = grid[i, j]
                verts.append(p)
                norms.append(n[i, j])
                uvs.append(mat.uv_elev(p[0], p[2]))
        W = nh
        for i in range(len(xs) - 1):
            for j in range(nh - 1):
                a = base + i * W + j
                b = base + (i + 1) * W + j
                c = b + 1
                d = a + 1
                tris.extend([a, b, c, a, c, d])
        if back > 0:     # a lid over the top edge back to the far side, so the silhouette reads solid
            for i in range(len(xs) - 1):
                p0, p1 = grid[i, -1], grid[i + 1, -1]
                self._quad(mat, tuple(p0), tuple(p1), (p1[0], p1[1] + back, p1[2]), (p0[0], p0[1] + back, p0[2]), (0, 0, 1))

    def strand(self, mat, x, y, h0, h1, width, along_shift=0.0, sway=0.0):
        """A thin vertical strip (thread, bar) facing the camera, double-sided."""
        self._quad(mat, (x - width / 2, y, h0), (x + width / 2, y, h0), (x + width / 2 + sway, y, h1),
                   (x - width / 2 + sway, y, h1), (0, -1, 0), uv_mode='elev', along_shift=along_shift)
        self._quad(mat, (x - width / 2, y + 0.01, h0), (x + width / 2, y + 0.01, h0),
                   (x + width / 2 + sway, y + 0.01, h1), (x - width / 2 + sway, y + 0.01, h1), (0, 1, 0), uv_mode='elev',
                   along_shift=along_shift)

    # ------------------------------------------------------------------ save ---
    def save(self, folder):
        os.makedirs(JSON_DIR, exist_ok=True)
        subs = []
        allv, alln, allu = [], [], []
        for name, (v, n, u, t) in self.parts.items():
            if not t:
                continue
            off = len(allv)
            allv.extend(v)
            alln.extend(n)
            allu.extend(u)
            m = self.materials[name]
            # triangles are wound so cross(b - a, c - a) points outward in board space; the map to Unity (h -> -z)
            # mirrors, and Unity's front side is cross(b - a, c - a), so swap two corners of every triangle
            tt = np.array(t, np.int64).reshape(-1, 3)[:, [0, 2, 1]].ravel() + off
            subs.append({'material': name, 'triangles': [int(i) for i in tt], 'selfLight': m.self_light,
                         'cutout': m.cutout, 'emission': m.emission_strength})
        # board (x, y, h) -> Unity world (x, y, 33 - h); normals likewise flip h
        V = np.array(allv, F32)
        N = np.array(alln, F32)
        U = np.array(allu, F32)
        data = {
            'name': self.name,
            'vertices': [round(float(c), 4) for p in V for c in (p[0], p[1], 33.0 - p[2])],
            'normals': [round(float(c), 4) for p in N for c in (p[0], p[1], -p[2])],
            'uvs': [round(float(c), 5) for p in U for c in p],
            'submeshes': subs,
        }
        with open(os.path.join(JSON_DIR, self.name + '.json'), 'w') as f:
            json.dump(data, f)
        print('  back edge', self.name, len(V), 'verts,', len(subs), 'materials')


def save_material(folder, mat, alb, hgt, smooth, emission=None, talb=None, thgt=None, tsmooth=None, alpha=None):
    """Stacks the tops band over the elevation and writes BE_<name>_Albedo/_Normal/_Mask(/_Emission). The board's
    grade and the smoothness cap (boardkit.GRADES, SMOOTH_CAP) apply here too."""
    from boardkit import grade, SMOOTH_CAP
    board = os.path.basename(os.path.normpath(folder))
    alb = grade(np.asarray(alb, F32), board)
    if talb is not None:
        talb = grade(np.asarray(talb, F32), board)
    if emission is not None:
        emission = grade(np.asarray(emission, F32), board)
    smooth = np.minimum(smooth, SMOOTH_CAP)
    if tsmooth is not None:
        tsmooth = np.minimum(tsmooth, SMOOTH_CAP)
    el = mat.elev_canvas()
    tp = mat.top_canvas()
    if talb is None:
        talb = np.broadcast_to(alb[:1].mean(axis=(0, 1)), (TOP_ROWS, TEX_W, 3)).astype(F32)
    if thgt is None:
        thgt = np.zeros((TOP_ROWS, TEX_W), F32)
    if tsmooth is None:
        tsmooth = np.full((TOP_ROWS, TEX_W), float(np.mean(smooth)), F32)
    full = Canvas((-HALF, HALF, 0.0, 1.0), (TEX_W, mat.rows))
    A = np.concatenate([talb, alb], 0)
    N = np.concatenate([normals(thgt, tp, 1.0), normals(hgt, el, 1.0)], 0)
    S = np.concatenate([tsmooth, smooth], 0)
    # emission = the material's self-light (a share of its own albedo, standing in for bounce light) plus any glow
    E = A * mat.self_light * SELF_LIGHT_GAIN
    if emission is not None:
        E = E + np.concatenate([np.zeros_like(talb), emission], 0)
    prefix = 'BE_' + mat.name
    save_set(folder, prefix, full, A, N, S, E, normal_scale=1.0, mask_size=(TEX_W // 4, max(mat.rows // 4, 4)))
    if mat.cutout and alpha is not None:
        from PIL import Image
        a = np.concatenate([np.ones((TOP_ROWS, TEX_W), F32), alpha], 0)
        rgba = np.dstack([np.clip(A, 0, 1), np.clip(a, 0, 1)])
        Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), 'RGBA').save(os.path.join(folder, prefix + '_Albedo.png'))
        jpg = os.path.join(folder, prefix + '_Albedo.jpg')
        if os.path.exists(jpg):
            os.remove(jpg)
