"""Regenerate the six town section scenes as street-block layouts.

Rewrites scenes/town/*.tscn in place: street ColorRects, a Backdrop of building
sprites in three rows, and the RepairableObject / Player positions on the street.
Run from anywhere:  python tools/gridtown.py
Then re-import in Godot. Tweak the constants below to reshape the block.
"""
import re, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BLD = "res://assets/sprites/buildings"

ROAD = "Color(0.435, 0.415, 0.395, 1)"
WALK = "Color(0.72, 0.7, 0.65, 1)"
LINE = "Color(0.9, 0.86, 0.66, 0.5)"

# main street y[95,265] (170), cross street x[-95,95]
STREET_RECTS = [
    ("MainStreet",  (-3000, 95, 3000, 265),   ROAD),
    ("CrossStreet", (-95, -900, 95, 1400),     ROAD),
    ("WalkMainT",   (-3000, 79, 3000, 95),     WALK),
    ("WalkMainB",   (-3000, 265, 3000, 281),   WALK),
    ("WalkCrossL",  (-111, -900, -95, 1400),   WALK),
    ("WalkCrossR",  (95, -900, 111, 1400),     WALK),
    ("MainLine",    (-3000, 176, 3000, 184),   LINE),
    ("CrossLine",   (-4, -900, 4, 1400),       LINE),
]

OBJ_SLOTS = [
    (-640, 80),    # north walk, in front of a house
    ( 480, 80),    # north walk, right
    (-280, 276),   # south walk, left of the crossing
    ( 360, 276),   # south walk, right of the crossing
    (-330, 80),    # north walk, centre-left
    ( 880, 80),    # north walk, far right
]
PLAYER_SPAWN = (-40, 340)

# rows: y = Sprite2D position.y (centre). building svgs are ~260-384 tall.
FRONT_Y, FRONT_S = -170, 1.12   # base ~ +30 (just above main street)
BACK_Y,  BACK_S  = -470, 0.92   # base ~ -220, peeks between front row
SOUTH_Y, SOUTH_S =  405, 0.78   # base ~ +600, lines the south side of the street

FX = [-1120, -740, -360, 360, 740, 1120]
BX = [-930, -560, 560, 930]
SX = [-980, -560, -140, 560, 980]

HOUSES  = ["house_sage","house_clay","house_blue","house_brick","house_cream","house_lavender",
           "house_mint","house_butter","house_rose","house_slate","house_olive","house_teal"]
COTTAGES = ["cottage_thatch","cottage_stone","cottage_rose","cottage_sage"]
ROWS     = ["rowhouse_brick","rowhouse_cream","rowhouse_blue","rowhouse_green"]
SHOPS    = ["shop","shop_blue","shop_green","shop_orange","shop_teal","shop_plum"]
APARTS   = ["apartment","apartment_brick","apartment_grey","apartment_blue","apartment_cream"]
OFFICES  = ["office_teal","office_slate","office_warm"]

# per section: a pool to draw the three rows from, and an optional front-centre landmark
SECTIONS = {
 "MainSquare": dict(pool=HOUSES+COTTAGES+SHOPS[:2]+ROWS[:2], landmark=("townhall", 1.16)),
 "Garden":     dict(pool=["cottage_sage","cottage_thatch","cottage_rose","house_mint","house_sage",
                          "house_olive","house_teal","house_butter","house_cream"], landmark=None),
 "Park":       dict(pool=["house_sage","house_blue","house_mint","house_teal","house_lavender",
                          "house_cream","cottage_stone","cottage_sage","house_slate"], landmark=("clocktower_stone", 1.05)),
 "Shopping":   dict(pool=SHOPS+SHOPS+ROWS+["apartment_cream","house_clay"], landmark=None),
 "Housing":    dict(pool=HOUSES+ROWS+COTTAGES+["apartment","apartment_cream"], landmark=None),
 "Business":   dict(pool=OFFICES*2+APARTS+SHOPS[:3], landmark=("townhall", 1.14)),
}

def _fill(pool, xs, seed, y, scale):
    """deterministic pick from pool per x-slot, no two adjacent the same."""
    out, prev = [], None
    for i, x in enumerate(xs):
        h = (seed * 2654435761 + i * 40503) & 0xffffffff
        cand = pool[h % len(pool)]
        if cand == prev:
            cand = pool[(h + 1) % len(pool)]
        prev = cand
        out.append((cand, x, y, scale))
    return out

def _seed(text):
    v = 2166136261
    for c in text:
        v = ((v ^ ord(c)) * 16777619) & 0xffffffff
    return v

def buildings_for(sec):
    cfg = SECTIONS[sec]
    pool = cfg["pool"]
    sd = _seed(sec)
    front = _fill(pool, FX, sd,        FRONT_Y, FRONT_S)
    back  = _fill(pool, BX, sd ^ 0x55, BACK_Y,  BACK_S)
    south = _fill(pool, SX, sd ^ 0xAA, SOUTH_Y, SOUTH_S)
    if cfg["landmark"]:
        name, sc = cfg["landmark"]
        front[2] = (name, FX[2], FRONT_Y, sc)          # front-centre landmark
    else:
        back[1] = ("apartment", BX[1], BACK_Y, 1.05)   # a taller block anchors the back row
    return south + back + front   # south renders first (behind), then back, then front

BUILDINGS = {sec: buildings_for(sec) for sec in SECTIONS}

def build(sec):
    p = os.path.join(ROOT, "scenes", "town", sec + ".tscn")
    s = open(p, encoding="utf-8").read()
    s = re.sub(r'^\[ext_resource[^\]]*id="bld_[^"]*"\]\n', "", s, flags=re.M)
    s = re.sub(r'\[node name="Backdrop" type="Node2D" parent="\."\]\n(?:.*?\n)*?(?=\[node name="World")', "", s)
    s = re.sub(r'\[node name="(?:Plaza|Path|Street|Lawn|Green|Square|MainStreet|BackStreet|CrossStreet|Walk\w+|MainLine|CrossLine)" type="ColorRect" parent="\."\]\n(?:(?!\[node).*\n)*', "", s)

    used = []
    for b in BUILDINGS[sec]:
        if b[0] not in used: used.append(b[0])
    exts = "".join(f'[ext_resource type="Texture2D" path="{BLD}/{n}.svg" id="bld_{n}"]\n' for n in used)
    last = list(re.finditer(r'^\[ext_resource[^\]]*\]\n', s, re.M))[-1]
    s = s[:last.end()] + exts + s[last.end():]

    streets = ""
    for name,(l,t,r,b),col in STREET_RECTS:
        streets += (f'[node name="{name}" type="ColorRect" parent="."]\n'
                    f'offset_left = {l}.0\noffset_top = {t}.0\noffset_right = {r}.0\noffset_bottom = {b}.0\n'
                    f'color = {col}\n\n')
    gm = re.search(r'\[node name="Ground" type="ColorRect" parent="\."\]\n(?:(?!\[node).*\n)*\n?', s)
    s = s[:gm.end()] + streets + s[gm.end():]

    # south buildings first (render behind), then the back/landmark row, then the front row
    order = ([b for b in BUILDINGS[sec] if b[2] >= 300] +
             [b for b in BUILDINGS[sec] if b[2] < FRONT_Y] +
             [b for b in BUILDINGS[sec] if b[2] == FRONT_Y])
    bd = '[node name="Backdrop" type="Node2D" parent="."]\n\n'
    for i,(n,x,y,sc) in enumerate(order):
        bd += (f'[node name="B{i}" type="Sprite2D" parent="Backdrop"]\n'
               f'texture_filter = 1\nposition = Vector2({x}, {y})\nscale = Vector2({sc}, {sc})\n'
               f'texture = ExtResource("bld_{n}")\n\n')
    wi = s.index('[node name="World" type="Node2D" parent="."]')
    s = s[:wi] + bd + s[wi:]

    def setpos(block, xy):
        return re.sub(r'position = Vector2\([^)]*\)', f'position = Vector2({xy[0]}, {xy[1]})', block, count=1)
    s = re.sub(r'(\[node name="Player" parent="World"[^\]]*\]\n(?:(?!\[node).*\n)*)',
              lambda m: setpos(m.group(1), PLAYER_SPAWN), s, count=1)
    objs = [m for m in re.finditer(r'\[node name="\w+" parent="World" instance=ExtResource\("3_obj"\)\]\n(?:(?!\[node).*\n)*', s)
            if "ExcludeFromCompletion = true" not in m.group(0)]  # tutorial props stay put
    parts, cur = [], 0
    for i,m in enumerate(objs):
        parts.append(s[cur:m.start()]); parts.append(setpos(m.group(0), OBJ_SLOTS[i % len(OBJ_SLOTS)])); cur = m.end()
    parts.append(s[cur:]); s = "".join(parts)

    n = len(re.findall(r'^\[ext_resource', s, re.M)) + len(re.findall(r'^\[sub_resource', s, re.M))
    s = re.sub(r'\[gd_scene load_steps=\d+', f'[gd_scene load_steps={n+1}', s, count=1)
    open(p, "w", encoding="utf-8", newline="\n").write(s)
    print(f"{sec}: {len(order)} buildings, {len(objs)} objects")

for sec in BUILDINGS:
    build(sec)
