"""Regenerate the six town section scenes, each with its own road layout.

Every section keeps its hand-authored RepairableObjects, NPCs and UI — this only
rewrites the road/sidewalk ColorRects (all named Rd*/Wk*/Ln*/Pv*/Gr*), the
Backdrop of building sprites, the Player spawn and the object positions.

  python tools/gridtown.py    # then re-import in Godot

Layouts (loosely after RPG-town road plans):
  MainSquare  central plaza with four road spokes
  Garden      a lane that opens into a planted cul-de-sac
  Park        a ring road around a central lawn
  Shopping    one straight high street, shops packed both sides
  Housing     a T-junction with a residential close off the top
  Business    a divided boulevard crossed by one avenue (a grid)
"""
import re, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BLD = "res://assets/sprites/buildings"

ROAD = "Color(0.435, 0.415, 0.395, 1)"
WALK = "Color(0.72, 0.7, 0.65, 1)"
LINE = "Color(0.9, 0.86, 0.66, 0.5)"
PAVE = "Color(0.6, 0.585, 0.55, 1)"
GRASS = "Color(0.44, 0.58, 0.36, 1)"

HOUSES = ["house_sage", "house_clay", "house_blue", "house_brick", "house_cream", "house_lavender",
          "house_mint", "house_butter", "house_rose", "house_slate", "house_olive", "house_teal"]
COTTAGES = ["cottage_thatch", "cottage_stone", "cottage_rose", "cottage_sage"]
ROWS = ["rowhouse_brick", "rowhouse_cream", "rowhouse_blue", "rowhouse_green"]
SHOPS = ["shop", "shop_blue", "shop_green", "shop_orange", "shop_teal", "shop_plum"]
APARTS = ["apartment", "apartment_brick", "apartment_grey", "apartment_blue", "apartment_cream"]
OFFICES = ["office_teal", "office_slate", "office_warm"]


def R(name, l, t, r, b, col):
    return (name, (l, t, r, b), col)


# ------- per-section layout --------------------------------------------------
# rects: list of R(); spawn: (x, y); slots: 6 object anchor points;
# rows: (xs, centre_y, scale) for the front / back / south building rows;
# landmark: (name, x, y, scale) placed after the rows, or None.

def crossroads():
    return [
        R("RdMain", -3000, 95, 3000, 265, ROAD),
        R("RdCross", -95, -900, 95, 1400, ROAD),
        R("WkMainT", -3000, 79, 3000, 95, WALK),
        R("WkMainB", -3000, 265, 3000, 281, WALK),
        R("WkCrossL", -111, -900, -95, 1400, WALK),
        R("WkCrossR", 95, -900, 111, 1400, WALK),
        R("LnMain", -3000, 176, 3000, 184, LINE),
        R("LnCross", -4, -900, 4, 1400, LINE),
    ]


LAYOUTS = {
 "MainSquare": dict(
    pool=HOUSES + COTTAGES + SHOPS[:2] + ROWS[:2],
    rects=[
        R("RdN", -95, -1100, 95, -330, ROAD),
        R("RdS", -95, 40, 95, 1300, ROAD),
        R("RdW", -3000, -200, -540, -60, ROAD),
        R("RdE", 540, -200, 3000, -60, ROAD),
        R("PvSquare", -540, -330, 540, 40, PAVE),
        R("WkSquare", -560, -350, 560, -330, WALK),
        R("WkSquareB", -560, 40, 560, 60, WALK),
        R("WkNW", -111, -1100, -95, -330, WALK),
        R("WkNE", 95, -1100, 111, -330, WALK),
        R("LnW", -3000, -134, -540, -126, LINE),
        R("LnE", 540, -134, 3000, -126, LINE),
        R("LnS", -4, 60, 4, 1300, LINE),
    ],
    spawn=(0, -110),
    slots=[(0, -150), (-380, -30), (380, -30), (-380, -280), (360, -280), (-470, -110)],
    front=([-1000, -600, 600, 1000], -500, 1.05),
    back=([-1300, -820, 820, 1300], -600, 0.9),
    south=([-780, -400, 400, 780], 300, 0.82),
    landmark=("townhall", 0, -560, 1.18),
 ),
 "Garden": dict(
    pool=["cottage_sage", "cottage_thatch", "cottage_rose", "house_mint", "house_sage",
          "house_olive", "house_teal", "house_butter", "house_cream"],
    rects=[
        R("RdLane", -95, 120, 95, 1300, ROAD),
        R("RdTurn", -430, -170, 430, 175, ROAD),
        R("GsGreen", -250, -70, 250, 150, GRASS),
        R("WkTurn", -452, -192, 452, -170, WALK),
        R("WkTurnB", -452, 175, 452, 197, WALK),
        R("WkLaneL", -113, 197, -95, 1300, WALK),
        R("WkLaneR", 95, 197, 113, 1300, WALK),
        R("LnLane", -4, 260, 4, 1300, LINE),
    ],
    spawn=(0, 400),
    slots=[(-320, -50), (320, -50), (-250, 210), (270, 210), (0, -130), (-380, 90)],
    front=([-640, -360, 360, 640], -360, 1.0),
    back=([-880, 0, 880], -470, 0.86),
    south=([-560, 560], 260, 0.78),
    landmark=None,
 ),
 "Park": dict(
    pool=["house_sage", "house_blue", "house_mint", "house_teal", "house_lavender",
          "house_cream", "cottage_stone", "cottage_sage", "house_slate"],
    rects=[
        R("RdLoopT", -780, -300, 780, -160, ROAD),
        R("RdLoopB", -780, 220, 780, 360, ROAD),
        R("RdLoopL", -780, -300, -640, 360, ROAD),
        R("RdLoopR", 640, -300, 780, 360, ROAD),
        R("RdInN", -70, -1100, 70, -300, ROAD),
        R("RdInS", -70, 360, 70, 1300, ROAD),
        R("GsLawn", -640, -160, 640, 220, GRASS),
        R("WkLawnIn", -640, -160, 640, -142, WALK),
        R("WkLawnInB", -640, 202, 640, 220, WALK),
        R("LnLoopN", -640, -234, 640, -226, LINE),
        R("LnLoopS", -640, 286, 640, 294, LINE),
    ],
    spawn=(0, 300),
    slots=[(-380, 30), (380, 30), (0, -70), (-460, 300), (460, 300), (0, 300)],
    front=([-1050, -650, 650, 1050], -470, 1.0),
    back=([-1300, -780, 780, 1300], -560, 0.86),
    south=([-880, -460, 460, 880], 640, 0.78),
    landmark=("clocktower_stone", -420, -70, 1.02),
 ),
 "Shopping": dict(
    pool=SHOPS + SHOPS + ROWS + ["apartment_cream", "house_clay"],
    rects=[
        R("RdHigh", -3200, 60, 3200, 320, ROAD),
        R("WkHighT", -3200, -60, 3200, 60, WALK),
        R("WkHighB", -3200, 320, 3200, 452, WALK),
        R("LnHigh", -3200, 186, 3200, 194, LINE),
    ],
    spawn=(0, 392),
    slots=[(-720, -10), (-40, -10), (700, -10), (-520, 392), (240, 392), (860, 392)],
    front=([-1500, -1100, -700, -300, 300, 700, 1100, 1500], -140, 1.12),
    back=None,
    south=([-1400, -1000, -600, -200, 200, 600, 1000, 1400], 492, 0.92),
    landmark=None,
 ),
 "Housing": dict(
    pool=HOUSES + ROWS + COTTAGES + ["apartment", "apartment_cream"],
    rects=[
        R("RdMain", -3200, 150, 3200, 320, ROAD),
        R("RdBranch", -95, -470, 95, 150, ROAD),
        R("RdClose", -360, -640, 360, -470, ROAD),
        R("PvClose", -280, -600, 280, -470, PAVE),
        R("WkMainT", -3200, 134, 3200, 150, WALK),
        R("WkMainB", -3200, 320, 3200, 336, WALK),
        R("WkBranchL", -113, -470, -95, 150, WALK),
        R("WkBranchR", 95, -470, 113, 150, WALK),
        R("WkClose", -382, -662, 382, -640, WALK),
        R("LnMain", -3200, 231, 3200, 239, LINE),
    ],
    spawn=(-40, 392),
    slots=[(-620, 130), (520, 130), (-250, -560), (250, -560), (0, -630), (-320, -360)],
    front=([-1150, -720, -300, 300, 720, 1150], -30, 1.0),
    back=([-1420, -900, 900, 1420], -430, 0.86),
    south=([-950, -520, 520, 950], 600, 0.78),
    landmark=None,
 ),
 "Business": dict(
    pool=OFFICES * 2 + APARTS + SHOPS[:3],
    rects=[
        R("RdBlvdN", -3200, 55, 3200, 170, ROAD),
        R("RdBlvdS", -3200, 215, 3200, 330, ROAD),
        R("GsMedian", -3200, 170, 3200, 215, GRASS),
        R("RdAve", -115, -1000, 115, 1300, ROAD),
        R("WkBlvdT", -3200, 39, 3200, 55, WALK),
        R("WkBlvdB", -3200, 330, 3200, 346, WALK),
        R("WkAveL", -131, -1000, -115, 1300, WALK),
        R("WkAveR", 115, -1000, 131, 1300, WALK),
        R("LnBlvdN", -3200, 108, 3200, 116, LINE),
        R("LnBlvdS", -3200, 268, 3200, 276, LINE),
    ],
    spawn=(-40, 402),
    slots=[(-540, 30), (470, 30), (-540, 360), (470, 360), (-260, 402), (330, -40)],
    front=([-1150, -750, -350, 350, 750, 1150], -170, 1.08),
    back=([-1450, -950, 950, 1450], -500, 0.88),
    south=([-980, -560, 560, 980], 600, 0.78),
    landmark=("townhall", 0, -520, 1.16),
 ),
}


def _seed(text):
    v = 2166136261
    for c in text:
        v = ((v ^ ord(c)) * 16777619) & 0xffffffff
    return v


def _fill(pool, xs, seed, y, scale):
    out, prev = [], None
    for i, x in enumerate(xs):
        h = (seed * 2654435761 + i * 40503) & 0xffffffff
        cand = pool[h % len(pool)]
        if cand == prev:
            cand = pool[(h + 1) % len(pool)]
        prev = cand
        out.append((cand, x, y, scale))
    return out


def buildings_for(sec):
    cfg = LAYOUTS[sec]
    sd = _seed(sec)
    rows = []
    fx, fy, fs = cfg["front"]
    rows.append(("front", _fill(cfg["pool"], fx, sd, fy, fs)))
    if cfg["back"]:
        bx, by, bs = cfg["back"]
        rows.append(("back", _fill(cfg["pool"], bx, sd ^ 0x55, by, bs)))
    if cfg["south"]:
        sx, sy, ss = cfg["south"]
        rows.append(("south", _fill(cfg["pool"], sx, sd ^ 0xAA, sy, ss)))

    ordered = []
    for key in ("south", "back", "front"):
        for k, lst in rows:
            if k == key:
                ordered += lst
    if cfg["landmark"]:
        name, x, y, s = cfg["landmark"]
        ordered.append((name, x, y, s))
    return ordered


BUILDINGS = {sec: buildings_for(sec) for sec in LAYOUTS}

OLD_RECT_NAMES = (r"Rd|Wk|Ln|Pv|Gs|Walk|Plaza|Path|Street|Lawn|Green|Square|"
                  r"MainStreet|BackStreet|CrossStreet|MainLine|CrossLine")


def build(sec):
    p = os.path.join(ROOT, "scenes", "town", sec + ".tscn")
    s = open(p, encoding="utf-8").read()
    cfg = LAYOUTS[sec]

    s = re.sub(r'^\[ext_resource[^\]]*id="bld_[^"]*"\]\n', "", s, flags=re.M)
    s = re.sub(r'\[node name="Backdrop" type="Node2D" parent="\."\]\n(?:.*?\n)*?(?=\[node name="World")', "", s)
    s = re.sub(r'\[node name="(?:' + OLD_RECT_NAMES + r')\w*" type="ColorRect" parent="\."\]\n(?:(?!\[node).*\n)*', "", s)

    used = []
    for b in BUILDINGS[sec]:
        if b[0] not in used:
            used.append(b[0])
    exts = "".join(f'[ext_resource type="Texture2D" path="{BLD}/{n}.svg" id="bld_{n}"]\n' for n in used)
    last = list(re.finditer(r'^\[ext_resource[^\]]*\]\n', s, re.M))[-1]
    s = s[:last.end()] + exts + s[last.end():]

    streets = ""
    for name, (l, t, r, b), col in cfg["rects"]:
        streets += (f'[node name="{name}" type="ColorRect" parent="."]\n'
                    f'offset_left = {l}.0\noffset_top = {t}.0\noffset_right = {r}.0\noffset_bottom = {b}.0\n'
                    f'color = {col}\n\n')
    gm = re.search(r'\[node name="Ground" type="ColorRect" parent="\."\]\n(?:(?!\[node).*\n)*\n?', s)
    s = s[:gm.end()] + streets + s[gm.end():]

    bd = '[node name="Backdrop" type="Node2D" parent="."]\n\n'
    for i, (n, x, y, sc) in enumerate(BUILDINGS[sec]):
        bd += (f'[node name="B{i}" type="Sprite2D" parent="Backdrop"]\n'
               f'texture_filter = 1\nposition = Vector2({x}, {y})\nscale = Vector2({sc}, {sc})\n'
               f'texture = ExtResource("bld_{n}")\n\n')
    wi = s.index('[node name="World" type="Node2D" parent="."]')
    s = s[:wi] + bd + s[wi:]

    def setpos(block, xy):
        return re.sub(r'position = Vector2\([^)]*\)', f'position = Vector2({xy[0]}, {xy[1]})', block, count=1)

    s = re.sub(r'(\[node name="Player" parent="World"[^\]]*\]\n(?:(?!\[node).*\n)*)',
               lambda m: setpos(m.group(1), cfg["spawn"]), s, count=1)

    objs = [m for m in re.finditer(
        r'\[node name="\w+" parent="World" instance=ExtResource\("3_obj"\)\]\n(?:(?!\[node).*\n)*', s)
        if "ExcludeFromCompletion = true" not in m.group(0)]
    parts, cur = [], 0
    for i, m in enumerate(objs):
        parts.append(s[cur:m.start()])
        parts.append(setpos(m.group(0), cfg["slots"][i % len(cfg["slots"])]))
        cur = m.end()
    parts.append(s[cur:])
    s = "".join(parts)

    n = len(re.findall(r'^\[ext_resource', s, re.M)) + len(re.findall(r'^\[sub_resource', s, re.M))
    s = re.sub(r'\[gd_scene load_steps=\d+', f'[gd_scene load_steps={n + 1}', s, count=1)
    open(p, "w", encoding="utf-8", newline="\n").write(s)
    print(f"{sec}: {len(BUILDINGS[sec])} buildings, {len(objs)} objects, {len(cfg['rects'])} road rects")


for sec in LAYOUTS:
    build(sec)
