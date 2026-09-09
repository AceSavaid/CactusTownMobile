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

# rows: y = Sprite2D position.y (centre). building svgs are ~300-352 tall.
FRONT_Y, FRONT_S = -170, 1.12   # base ~ +30 (just above main street)
BACK_Y,  BACK_S  = -470, 0.92   # base ~ -220, peeks between front row
SOUTH_Y, SOUTH_S =  405, 0.78   # base ~ +600, lines the south side of the street
TALL_Y = -530                   # townhall / big apartment landmarks, back

def F(n, x, s=FRONT_S): return (n, x, FRONT_Y, s)
def B(n, x, s=BACK_S):  return (n, x, BACK_Y, s)
def S(n, x, s=SOUTH_S): return (n, x, SOUTH_Y, s)
def T(n, x, s=1.28):    return (n, x, TALL_Y, s)

FX = [-1120, -740, -360, 360, 740, 1120]
BX = [-930, -560, 560, 930]
SX = [-980, -560, -140, 560, 980]

BUILDINGS = {
 "MainSquare": [F("house_clay",FX[0]),F("house_sage",FX[1]),("townhall",FX[2],FRONT_Y,1.14),F("house_blue",FX[3]),F("house_sage",FX[4]),F("house_clay",FX[5]),
                B("house_blue",BX[0]),B("house_clay",BX[1]),T("apartment",560,1.12),B("house_sage",BX[3]),
                S("house_sage",SX[0]),S("house_clay",SX[1]),S("shop",SX[2]),S("house_blue",SX[3]),S("house_sage",SX[4])],
 "Garden": [F("house_sage",FX[0]),F("house_clay",FX[1]),F("house_sage",FX[2]),F("house_blue",FX[3]),F("house_clay",FX[4]),F("house_sage",FX[5]),
            B("house_blue",BX[0]),B("house_sage",BX[1]),B("house_clay",BX[2]),B("house_blue",BX[3]),
            S("house_clay",SX[0]),S("house_sage",SX[1]),S("house_blue",SX[2]),S("house_sage",SX[3]),S("house_clay",SX[4])],
 "Park": [F("house_blue",FX[0]),F("house_sage",FX[1]),F("house_clay",FX[2]),F("house_sage",FX[3]),F("house_blue",FX[4]),F("house_clay",FX[5]),
          B("house_sage",BX[0]),T("apartment",-470,1.1),B("house_clay",BX[2]),B("house_sage",BX[3]),
          S("house_sage",SX[0]),S("house_blue",SX[1]),S("house_clay",SX[2]),S("house_sage",SX[3]),S("house_blue",SX[4])],
 "Shopping": [F("shop",FX[0]),F("house_clay",FX[1]),F("shop",FX[2]),F("shop",FX[3]),F("house_clay",FX[4]),F("shop",FX[5]),
              B("house_clay",BX[0]),T("apartment",-470,1.16),T("apartment",470,1.16),B("house_clay",BX[3]),
              S("shop",SX[0]),S("house_clay",SX[1]),S("shop",SX[2]),S("shop",SX[3]),S("house_clay",SX[4])],
 "Housing": [F("house_sage",FX[0]),F("house_clay",FX[1]),F("house_blue",FX[2]),F("house_sage",FX[3]),F("house_clay",FX[4]),F("house_blue",FX[5]),
             B("house_clay",BX[0]),B("house_blue",BX[1]),B("house_sage",BX[2]),T("apartment",930,1.06),
             S("house_blue",SX[0]),S("house_sage",SX[1]),S("house_clay",SX[2]),S("house_blue",SX[3]),S("house_sage",SX[4])],
 "Business": [F("shop",FX[0]),F("apartment",FX[1],1.06),("townhall",FX[2],FRONT_Y,1.12),F("apartment",FX[3],1.06),F("shop",FX[4]),F("apartment",FX[5],1.06),
              B("shop",BX[1]),T("apartment",-560,1.24),T("apartment",560,1.24),B("shop",BX[0]),
              S("shop",SX[0]),S("apartment",SX[1],0.72),S("shop",SX[2]),S("apartment",SX[3],0.72),S("shop",SX[4])],
}

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
    objs = list(re.finditer(r'\[node name="\w+" parent="World" instance=ExtResource\("3_obj"\)\]\n(?:(?!\[node).*\n)*', s))
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
