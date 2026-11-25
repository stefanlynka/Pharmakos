"""The Minotaur's labyrinth: the fight happens in a cleared central chamber of worn flagstones; all around it the
maze itself, seen from above, its wall tops catching the light over corridors sunk in shadow. Bones of the lost lie
in the passages near the chamber; a few torch scorches glow on the walls. Lanes: long worn flagstone runners."""
from boardkit import *  # noqa

NAME = 'Labyrinth'

CELL = 4.2
WALL = 1.45


def make_maze(rng, cols, rows, open_cell):
    """Recursive backtracker over the grid; cells where open_cell(c, r) is true are the chamber (no walls).
    Returns sets of open passages between cells."""
    passages = set()
    seen = np.zeros((rows, cols), bool)
    for r in range(rows):
        for c in range(cols):
            if open_cell(c, r):
                seen[r, c] = True
    # start from every unvisited region so the whole board is maze
    for sr in range(rows):
        for sc in range(cols):
            if seen[sr, sc]:
                continue
            stack = [(sc, sr)]
            seen[sr, sc] = True
            while stack:
                c, r = stack[-1]
                nbrs = [(c + dc, r + dr) for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1))
                        if 0 <= c + dc < cols and 0 <= r + dr < rows and not seen[r + dr, c + dc]]
                if not nbrs:
                    stack.pop()
                    continue
                nc, nr = nbrs[rng.integers(len(nbrs))]
                passages.add(frozenset(((c, r), (nc, nr))))
                seen[nr, nc] = True
                stack.append((nc, nr))
    return passages


def paint(preview=False):
    g, lane = new_canvases()
    n = Noise(37)
    rng = np.random.default_rng(37)
    X, Y = g.X, g.Y
    ad = arena_dist(X, Y)

    x0, y0 = -68.0, -24.0
    cols = int(math.ceil(136 / CELL)) + 1
    rows = int(math.ceil(68 / CELL)) + 1

    def cell_centre(c, r):
        return x0 + (c + 0.5) * CELL, y0 + (r + 0.5) * CELL

    def chamber(c, r):
        cx, cy = cell_centre(c, r)
        return float(arena_dist(np.array([[cx]], F32), np.array([[cy]], F32))[0, 0]) < 4.5

    passages = make_maze(rng, cols, rows, chamber)
    # openings from the chamber into the maze
    for r in range(rows):
        for c in range(cols):
            if chamber(c, r):
                continue
            for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if 0 <= c + dc < cols and 0 <= r + dr < rows and chamber(c + dc, r + dr) and rng.random() < 0.12:
                    passages.add(frozenset(((c, r), (c + dc, r + dr))))

    walls = Layer(g)
    hw = WALL / 2
    for r in range(rows + 1):
        for c in range(cols + 1):
            gx, gy = x0 + c * CELL, y0 + r * CELL
            # a pillar at every grid corner that touches a non-chamber cell
            touch = any(0 <= c + dc < cols and 0 <= r + dr < rows and not chamber(c + dc, r + dr)
                        for dc in (-1, 0) for dr in (-1, 0))
            if touch:
                walls.poly([(gx - hw, gy - hw), (gx + hw, gy - hw), (gx + hw, gy + hw), (gx - hw, gy + hw)])
    for r in range(rows):
        for c in range(cols):
            for dc, dr in ((1, 0), (0, 1)):
                c2, r2 = c + dc, r + dr
                a_in = not chamber(c, r)
                b_in = 0 <= c2 < cols and 0 <= r2 < rows and not chamber(c2, r2)
                if not (a_in or b_in):
                    continue
                if 0 <= c2 < cols and 0 <= r2 < rows and frozenset(((c, r), (c2, r2))) in passages:
                    continue
                if dc:   # wall on the right edge of (c, r)
                    gx = x0 + (c + 1) * CELL
                    ya, yb = y0 + r * CELL, y0 + (r + 1) * CELL
                    walls.poly([(gx - hw, ya), (gx + hw, ya), (gx + hw, yb), (gx - hw, yb)])
                else:    # wall on the top edge
                    gy = y0 + (r + 1) * CELL
                    xa, xb = x0 + c * CELL, x0 + (c + 1) * CELL
                    walls.poly([(xa, gy - hw), (xb, gy - hw), (xb, gy + hw), (xa, gy + hw)])
    wm = walls.read()
    # weathered wall tops: blur and re-threshold with noise so edges crumble
    wsoft = blurf(wm, 0.12, g)
    crumble = n.fbm(X, Y, 0.6, 4, offset=1)
    wm = smoothstep(0.35, 0.65, wsoft + (crumble - 0.5) * 0.45)

    # floor everywhere: flagstones, worn and dark in the passages, larger in the chamber
    j1, s1, _ = flagstones(n, X, Y, 2.6, 1.7, gap=0.08, offset=3)
    grain = n.fbm(X, Y, 0.8, 5, offset=4)
    big = n.fbm(X, Y, 16, 3, offset=5)
    floor = lerp(C(0.12, 0.11, 0.1), C(0.22, 0.2, 0.18), np.clip(s1 * 0.5 + grain * 0.5, 0, 1))
    floor = lerp(floor, C(0.06, 0.055, 0.05), j1 * 0.8)
    alb = floor * (0.8 + 0.4 * big)[..., None]
    hgt = (grain * 0.05 - j1 * 0.05 + (s1 - 0.5) * 0.03).astype(F32)

    # passages lie in the walls' shadow: darker the deeper into the maze and the closer to a wall
    near_wall = np.clip(blurf(wm, 0.9, g) * 1.6, 0, 1)
    depth = smoothstep(0, 22, ad)
    alb = alb * (1 - (near_wall * 0.55 + depth * 0.35)[..., None] * (1 - wm[..., None]))

    # wall tops: rough dressed stone, lit
    wtone = n.fbm(X, Y, 0.45, 5, offset=6)
    wj, ws, _ = flagstones(n, X, Y, 1.6, 1.0, gap=0.05, offset=7)
    wallcol = lerp(C(0.2, 0.185, 0.165), C(0.37, 0.34, 0.3), np.clip(wtone * 0.6 + ws * 0.4, 0, 1))
    wallcol = lerp(wallcol, C(0.1, 0.09, 0.08), wj * 0.6)
    moss = smoothstep(0.6, 0.78, n.fbm(X, Y, 1.4, 4, offset=8)) * depth
    wallcol = lerp(wallcol, C(0.13, 0.15, 0.08), moss * 0.6)
    ws_shadow = np.roll(np.roll(blurf(wm, 0.5, g), int(0.5 * g.ppu), 0), int(0.35 * g.ppu), 1)
    alb = alb * (1 - np.clip(ws_shadow * 1.2 - wm, 0, 1)[..., None] * 0.5)
    alb = lerp(alb, wallcol, wm)
    hgt = (hgt + blurf(wm, 0.08, g) * 1.4 + wm * wtone * 0.1 - wj * wm * 0.06).astype(F32)

    # (bones of the lost were tried and removed: small bright props distract; board spec)

    # straw and dried blood drag-marks in the passages close to the chamber
    blood = smoothstep(0.66, 0.8, n.fbm(X * 0.6, Y * 1.6, 3, 4, offset=9)) * (1 - wm) * (1 - smoothstep(3, 12, ad))
    alb = lerp(alb, C(0.16, 0.035, 0.03), blood * 0.7)

    # torch scorch on a few wall ends near the chamber, the brands still glowing faintly
    emission = np.zeros_like(alb)
    torch = Layer(g)
    for x, y in scatter_points(rng, 8, (-60, 60, -20, 40), margin_keep(4.0, 9.0), min_gap=18):
        torch.ellipse(x, y, 0.22, 0.22)
    tm = torch.read()
    scorch = np.clip(blurf(tm, 1.6, g) * 3.0, 0, 1)
    alb = alb * (1 - scorch[..., None] * 0.6)
    glow = np.clip(blurf(tm, 0.25, g) * 2.5, 0, 1)
    emission = emission + C(0.55, 0.22, 0.05) * glow[..., None] + C(0.3, 0.1, 0.02) * np.clip(blurf(tm, 1.4, g) * 2.2, 0, 1)[..., None]

    cav = cavity(hgt, g, 0.6, 2.0)
    alb = desaturate(alb * (1 - cav[..., None] * 0.4), 0.15)
    smooth = 0.14 + blood * 0.35 + (1 - wm) * 0.06

    # lanes: long worn runners of flagstone
    LX, LY = lane.WX, lane.WY
    lj, lsid, _ = flagstones(n, LX, LY, 4.2, 2.5, gap=0.07, offset=30)
    lalb, lhgt = lane_base(n, lane, C(0.15, 0.14, 0.125), C(0.24, 0.22, 0.2), offset=930)
    wear = lane_wear(lane)
    lalb = lalb * (0.9 + 0.2 * lsid)[..., None]
    lalb = lerp(lalb, C(0.08, 0.075, 0.07), lj * (0.7 - 0.3 * wear))
    lalb = lalb * (0.95 + 0.12 * wear)[..., None]
    lhgt = (lhgt - lj * 0.06 + (lsid - 0.5) * 0.03).astype(F32)
    lcav = cavity(lhgt, lane, 0.4, 2.0)
    lalb = desaturate(lalb * (1 - lcav[..., None] * 0.3), 0.15)
    lsmooth = 0.25 + wear * 0.12 - lj * 0.15

    finish(os.path.join(OUT, NAME), NAME, g, alb, hgt, smooth, emission, lane, lalb, lhgt, lsmooth, preview=preview)
    back_edge()


def back_edge():
    """The maze goes on: a tall wall of dressed stone with dark passages leading off into the labyrinth, set back
    walls glimpsed through them, torches burning in iron sconces beside each opening."""
    from backedge import BackEdge, Material, save_material
    import bepaint as bp
    import bekit as bk
    folder = os.path.join(OUT, NAME)
    n = Noise(41)
    rng = np.random.default_rng(41)
    be = BackEdge(NAME)
    st = be.material(Material('Stone', tex_h=14.0, self_light=0.4))
    deep = be.material(Material('Deep', tex_h=14.0, self_light=0.12))
    iron = be.material(Material('Iron', tex_h=6.0, self_light=0.3))
    fl = be.material(bk.flame_material())
    doors = [-58.0, -36.0, -17.0, 19.0, 37.0, 57.0]
    dw = 3.2
    x = -75.0
    edges = []
    for d in doors:
        edges.append((x, d - dw / 2))
        x = d + dw / 2
    edges.append((x, 75.0))
    for i, (x0, x1) in enumerate(edges):
        y0 = 34.6 + (0.0 if i % 2 == 0 else 1.2)          # the wall steps back and forth like the maze
        be.box(st, x0, x1, y0, y0 + 2.2, 0, 13.0, back=False)
    for d in doors:
        be.box(st, d - dw / 2 - 0.01, d + dw / 2 + 0.01, 34.6, 37.0, 3.6, 13.0, sides=False)      # lintel and wall above
        be.box(deep, d - dw / 2, d + dw / 2, 41.0, 43.0, 0, 13.0, sides=False)                    # far wall of the passage
        for side in (-1, 1):
            be.box(deep, d + side * dw / 2 - 0.01 * side, d + side * dw / 2, 36.8, 41.0, 0, 3.6, sides=True)
            tx = d + side * (dw / 2 + 0.7)
            be.box(iron, tx - 0.08, tx + 0.08, 34.3, 34.6, 2.0, 2.9)
            be.lathe(iron, tx, 34.2, [(2.9, 0.12), (3.05, 0.28), (3.2, 0.3)], segments=8)
            if bk.FLAMES:
                be.lathe(fl, tx, 34.2, [(3.15, 0.25), (3.45, 0.24), (3.8, 0.14), (4.3, 0.0)], segments=8, cap=False)
    be.save(folder)

    c = st.elev_canvas()
    alb, hgt, sm = bp.masonry(c, n, 1.8, 0.75, C(0.16, 0.14, 0.12), C(0.27, 0.24, 0.2), C(0.06, 0.05, 0.04), offset=81)
    moss = smoothstep(0.62, 0.78, n.fbm(c.X, c.Y, 1.4, 4, offset=82)) * (1 - smoothstep(0.0, 3.0, c.Y))
    alb = lerp(alb, C(0.08, 0.1, 0.05), moss * 0.6)
    soot = sum(np.exp(-(((c.X - (d + s_ * (dw / 2 + 0.7))) / 0.6) ** 2) - (((c.Y - 4.5) / 1.6) ** 2))
               for d in doors for s_ in (-1, 1))
    alb = alb * (1 - np.clip(soot, 0, 1) * 0.6)[..., None]
    glow = sum(np.exp(-(((c.X - (d + s_ * (dw / 2 + 0.7))) / 1.6) ** 2) - (((c.Y - 3.4) / 1.4) ** 2))
               for d in doors for s_ in (-1, 1))
    alb = bp.age(n, c.X, c.Y, alb, foot_dark=0.4)
    em = C(0.5, 0.22, 0.06) * np.clip(glow, 0, 1)[..., None] * 0.35
    ta, th, ts = bp.tops(st, n, C(0.12, 0.1, 0.09), C(0.2, 0.18, 0.15), offset=83)
    save_material(folder, st, alb, hgt, sm, emission=em, talb=ta, thgt=th, tsmooth=ts)
    c = deep.elev_canvas()
    alb, hgt, sm = bp.masonry(c, n, 1.8, 0.75, C(0.04, 0.035, 0.03), C(0.08, 0.07, 0.06), C(0.02, 0.015, 0.01), offset=84)
    save_material(folder, deep, alb, hgt, sm)
    c = iron.elev_canvas()
    alb, hgt, sm = bp.metal(c, n, C(0.05, 0.045, 0.04), C(0.14, 0.12, 0.1), offset=85)
    save_material(folder, iron, alb, hgt, sm)
    bk.paint_flame(folder, fl, n)
