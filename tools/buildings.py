"""Generate the town building sprite set into assets/sprites/buildings/.

Flat vector SVGs matching the game's prop style. Houses / shops / apartments are
one template each in many colourways; cottage / rowhouse / office / clocktower are
bespoke shapes. Run:  python tools/buildings.py   then re-import in Godot.
"""
import os

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                   "assets", "sprites", "buildings")
os.makedirs(OUT, exist_ok=True)

CHIM = "#6b6b66"
KNOB = "#d8b26a"


def house(wall, roof, glass, door):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="280" height="320" viewBox="0 0 280 320">
  <ellipse cx="140" cy="306" rx="120" ry="14" fill="#000" opacity="0.13"/>
  <rect x="46" y="150" width="188" height="156" rx="4" fill="{wall}"/>
  <path d="M32 152 L140 66 L248 152 Z" fill="{roof}"/>
  <rect x="150" y="60" width="20" height="46" rx="3" fill="{CHIM}"/>
  <rect x="108" y="214" width="64" height="92" rx="4" fill="{door}"/>
  <circle cx="158" cy="262" r="5" fill="{KNOB}"/>
  <rect x="64" y="176" width="46" height="46" rx="3" fill="{glass}"/>
  <rect x="64" y="176" width="46" height="46" rx="3" fill="none" stroke="{roof}" stroke-width="5"/>
  <path d="M87 176 V222 M64 199 H110" stroke="{roof}" stroke-width="4"/>
  <rect x="184" y="176" width="46" height="46" rx="3" fill="{glass}"/>
  <rect x="184" y="176" width="46" height="46" rx="3" fill="none" stroke="{roof}" stroke-width="5"/>
  <path d="M207 176 V222 M184 199 H230" stroke="{roof}" stroke-width="4"/>
</svg>
'''


def shop(wall, band, awn, awn_lt, frame, glass, door):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="320" height="300" viewBox="0 0 320 300">
  <ellipse cx="160" cy="288" rx="140" ry="14" fill="#000" opacity="0.13"/>
  <rect x="34" y="120" width="252" height="168" rx="4" fill="{wall}"/>
  <rect x="34" y="104" width="252" height="26" rx="4" fill="{band}"/>
  <path d="M30 150 h260 l-14 34 h-232 z" fill="{awn}"/>
  <path d="M30 150 h260 l-6 15 h-248 z" fill="{awn_lt}"/>
  <g fill="{band}"><rect x="46" y="150" width="26" height="34"/><rect x="98" y="150" width="26" height="34"/><rect x="150" y="150" width="26" height="34"/><rect x="202" y="150" width="26" height="34"/><rect x="254" y="150" width="20" height="34"/></g>
  <rect x="54" y="196" width="120" height="92" rx="3" fill="{glass}"/>
  <rect x="54" y="196" width="120" height="92" rx="3" fill="none" stroke="{frame}" stroke-width="6"/>
  <path d="M114 196 V288 M54 242 H174" stroke="{frame}" stroke-width="5"/>
  <rect x="204" y="206" width="60" height="82" rx="3" fill="{door}"/>
  <circle cx="252" cy="248" r="5" fill="#e8c98a"/>
</svg>
'''


def apartment(wall, trim, glass, frame, door, floors=3):
    rows = ""
    for r in range(floors):
        y = 98 + r * 66
        rows += (f'<rect x="58" y="{y}" width="34" height="42"/>'
                 f'<rect x="112" y="{y}" width="34" height="42"/>'
                 f'<rect x="166" y="{y}" width="30" height="42"/>')
    top = 70 - (floors - 3) * 0
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="240" height="360" viewBox="0 0 240 360">
  <ellipse cx="120" cy="348" rx="98" ry="12" fill="#000" opacity="0.13"/>
  <rect x="36" y="70" width="168" height="278" rx="4" fill="{wall}"/>
  <rect x="30" y="56" width="180" height="20" rx="4" fill="{trim}"/>
  <g fill="{glass}" stroke="{frame}" stroke-width="4">{rows}</g>
  <rect x="104" y="272" width="48" height="76" rx="3" fill="{door}"/>
  <circle cx="140" cy="312" r="4.5" fill="{KNOB}"/>
</svg>
'''


def cottage(wall, roof, glass, door):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="300" height="264" viewBox="0 0 300 264">
  <ellipse cx="150" cy="250" rx="128" ry="12" fill="#000" opacity="0.13"/>
  <rect x="44" y="140" width="212" height="108" rx="8" fill="{wall}"/>
  <path d="M24 146 Q150 58 276 146 Z" fill="{roof}"/>
  <rect x="196" y="70" width="18" height="40" rx="3" fill="{CHIM}"/>
  <rect x="122" y="176" width="56" height="72" rx="4" fill="{door}"/>
  <circle cx="167" cy="214" r="5" fill="{KNOB}"/>
  <rect x="62" y="162" width="46" height="44" rx="3" fill="{glass}"/>
  <rect x="62" y="162" width="46" height="44" rx="3" fill="none" stroke="{roof}" stroke-width="5"/>
  <path d="M85 162 V206 M62 184 H108" stroke="{roof}" stroke-width="4"/>
  <rect x="196" y="162" width="42" height="44" rx="3" fill="{glass}"/>
  <rect x="196" y="162" width="42" height="44" rx="3" fill="none" stroke="{roof}" stroke-width="5"/>
</svg>
'''


def rowhouse(wall, roof, glass, door):
    win = ""
    for y in (116, 180):
        win += (f'<rect x="40" y="{y}" width="70" height="42" rx="3" fill="{glass}"/>'
                f'<rect x="40" y="{y}" width="70" height="42" rx="3" fill="none" stroke="{roof}" stroke-width="5"/>'
                f'<path d="M75 {y} V{y+42} M40 {y+21} H110" stroke="{roof}" stroke-width="4"/>')
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="150" height="342" viewBox="0 0 150 342">
  <ellipse cx="75" cy="330" rx="64" ry="10" fill="#000" opacity="0.13"/>
  <rect x="22" y="92" width="106" height="238" rx="3" fill="{wall}"/>
  <rect x="14" y="74" width="122" height="22" rx="3" fill="{roof}"/>
  <rect x="30" y="60" width="18" height="18" fill="{CHIM}"/>
  {win}
  <rect x="52" y="262" width="46" height="68" rx="3" fill="{door}"/>
  <circle cx="88" cy="298" r="4.5" fill="{KNOB}"/>
</svg>
'''


def office(wall, glass, accent):
    win = ""
    for r in range(4):
        for c in range(3):
            win += f'<rect x="{54+c*58}" y="{92+r*54}" width="42" height="38" rx="2" fill="{glass}"/>'
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="260" height="332" viewBox="0 0 260 332">
  <ellipse cx="130" cy="320" rx="110" ry="12" fill="#000" opacity="0.13"/>
  <rect x="30" y="72" width="200" height="248" rx="4" fill="{wall}"/>
  <rect x="24" y="58" width="212" height="18" rx="3" fill="{accent}"/>
  <g>{win}</g>
  <rect x="100" y="272" width="60" height="48" rx="3" fill="{accent}"/>
  <rect x="110" y="282" width="40" height="38" rx="2" fill="{glass}"/>
</svg>
'''


def clocktower(wall, roof, face):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="160" height="384" viewBox="0 0 160 384">
  <ellipse cx="80" cy="372" rx="60" ry="10" fill="#000" opacity="0.13"/>
  <rect x="40" y="104" width="80" height="268" rx="3" fill="{wall}"/>
  <path d="M30 106 L80 42 L130 106 Z" fill="{roof}"/>
  <rect x="74" y="30" width="12" height="18" fill="{roof}"/>
  <circle cx="80" cy="150" r="26" fill="{face}"/>
  <circle cx="80" cy="150" r="26" fill="none" stroke="{roof}" stroke-width="4"/>
  <path d="M80 150 V132 M80 150 L94 158" stroke="#2a2a28" stroke-width="4" stroke-linecap="round"/>
  <rect x="60" y="300" width="40" height="72" rx="3" fill="{roof}"/>
  <rect x="54" y="210" width="52" height="40" rx="3" fill="{face}"/>
</svg>
'''


HOUSES = {
    "house_sage":     ("#8ba888", "#5f7d63", "#cfe3d9", "#6b5744"),
    "house_clay":     ("#c98b62", "#8a5a3c", "#f2e2c8", "#5c4636"),
    "house_blue":     ("#7f9bb5", "#54708a", "#e4ecf3", "#3f4f61"),
    "house_brick":    ("#b5665a", "#7d3f37", "#e8d5c0", "#4a3830"),
    "house_cream":    ("#e1d3ac", "#b39b6a", "#f4ecd6", "#6b5442"),
    "house_lavender": ("#a99bc4", "#6f5f8f", "#e6e0f0", "#4a4260"),
    "house_mint":     ("#9dc9a8", "#6aa07d", "#e2f0e6", "#5a4b40"),
    "house_butter":   ("#e6cf87", "#bfa04f", "#f6efc9", "#6b5a3a"),
    "house_rose":     ("#d79aa4", "#a86a74", "#f2e0e4", "#5c4048"),
    "house_slate":    ("#8a959c", "#5c666d", "#dfe6e8", "#3f4448"),
    "house_olive":    ("#9aa06a", "#6c7248", "#e8ead0", "#4d4a30"),
    "house_teal":     ("#6fa8a0", "#4a7d76", "#dff0ec", "#3a5450"),
}

SHOPS = {
    "shop":         ("#d9c9a6", "#b8a57e", "#c65b53", "#e0837b", "#8a7a58", "#a9c6cf", "#6b5744"),
    "shop_blue":    ("#d9c9a6", "#b8a57e", "#4d7fa8", "#7aa8cc", "#8a7a58", "#c6d9df", "#6b5744"),
    "shop_green":   ("#e0d6b8", "#b7a97e", "#5a9161", "#82b98a", "#8a7a58", "#c6d9df", "#6b5744"),
    "shop_orange":  ("#ded0a8", "#bda87c", "#d98a4a", "#e8b07a", "#8a7a58", "#c6d9df", "#6b5744"),
    "shop_teal":    ("#ddd0b8", "#b8a980", "#3f8f8a", "#6fb5b0", "#8a7a58", "#cfe0dc", "#6b5744"),
    "shop_plum":    ("#dccdb2", "#b6a37f", "#8a5a7a", "#b085a0", "#8a7a58", "#d8c9d4", "#6b5744"),
}

APARTMENTS = {
    "apartment":       ("#b7a3a0", "#8f7d7a", "#dfe6e4", "#7a6a67", "#5a4b48", 3),
    "apartment_brick": ("#a86b5e", "#7a4a40", "#e0d0c4", "#6a4a42", "#4a3830", 4),
    "apartment_grey":  ("#9a9a98", "#6e6e6c", "#dfe4e6", "#62625f", "#44443f", 3),
    "apartment_blue":  ("#8fa2b0", "#61727e", "#e0e8ec", "#5a6a75", "#3e4a52", 4),
    "apartment_cream": ("#cdbf9e", "#a08c68", "#f0ead6", "#8a7a58", "#6b5a3a", 3),
}

COTTAGES = {
    "cottage_thatch": ("#e0d3ad", "#9c7b46", "#dfeae0", "#7a5a3a"),
    "cottage_stone":  ("#b7b0a4", "#7d7468", "#dce6e2", "#5a4f42"),
    "cottage_rose":   ("#d9a7ad", "#9c6a72", "#f0e2e4", "#6b4a50"),
    "cottage_sage":   ("#93b493", "#5f7d5f", "#dceede", "#55503f"),
}

ROWHOUSES = {
    "rowhouse_brick":  ("#b06a5c", "#7a4438", "#e4d4c6", "#4a3830"),
    "rowhouse_cream":  ("#d8c8a2", "#a68f5f", "#f0e8d2", "#6b5a3a"),
    "rowhouse_blue":   ("#7d94ad", "#516a82", "#e0e8ef", "#3c4a58"),
    "rowhouse_green":  ("#8aa87f", "#5c7a52", "#e0eddb", "#4a4a34"),
}

OFFICES = {
    "office_teal":  ("#8fa8a4", "#cfe2de", "#4a6a66"),
    "office_slate": ("#8b939a", "#d4dde0", "#4c545a"),
    "office_warm":  ("#b7a58a", "#e6ded0", "#6a5a44"),
}

TOWNHALLS = {
    "townhall":         None,   # keep the existing bespoke file, regenerated below
}


def write(name, svg):
    open(os.path.join(OUT, name + ".svg"), "w", encoding="utf-8", newline="\n").write(svg)


def main():
    n = 0
    for k, v in HOUSES.items():       write(k, house(*v)); n += 1
    for k, v in SHOPS.items():        write(k, shop(*v)); n += 1
    for k, v in APARTMENTS.items():   write(k, apartment(*v)); n += 1
    for k, v in COTTAGES.items():     write(k, cottage(*v)); n += 1
    for k, v in ROWHOUSES.items():    write(k, rowhouse(*v)); n += 1
    for k, v in OFFICES.items():      write(k, office(*v)); n += 1
    write("clocktower_stone", clocktower("#c6bfae", "#8a8272", "#efe9d4")); n += 1
    write("clocktower_brick", clocktower("#b07a68", "#7d4f42", "#f0e6d2")); n += 1
    # townhall stays as its own hand-drawn file (assets/sprites/buildings/townhall.svg)
    print(f"wrote {n} building sprites to {OUT}")


if __name__ == "__main__":
    main()
