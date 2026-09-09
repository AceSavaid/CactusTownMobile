"""Generate the town building sprite set into assets/sprites/buildings/.

Flat vector SVGs matching the game's prop style. Houses / shops / apartments are
one template each in many colourways; cottage / rowhouse / office / clocktower are
bespoke shapes. Every building has the SAME door size (DW x DH) sitting flush on
the base, so doors read consistently whatever the building height.
Run:  python tools/buildings.py   then re-import in Godot.
"""
import os

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                   "assets", "sprites", "buildings")
os.makedirs(OUT, exist_ok=True)

CHIM = "#6b6b66"
KNOB = "#e8c46a"
DW, DH = 66, 120          # standard door
SHADOW_A = 0.13


def door(cx, base, color):
    x = cx - DW / 2
    top = base - DH
    return (
        f'<rect x="{x-9:.0f}" y="{base-8:.0f}" width="{DW+18:.0f}" height="10" rx="2" fill="#3a332c"/>'
        f'<rect x="{x:.0f}" y="{top:.0f}" width="{DW}" height="{DH}" rx="4" fill="{color}"/>'
        f'<rect x="{x:.0f}" y="{top:.0f}" width="{DW}" height="{DH}" rx="4" fill="none" stroke="#00000026" stroke-width="3"/>'
        f'<line x1="{cx:.0f}" y1="{top:.0f}" x2="{cx:.0f}" y2="{base:.0f}" stroke="#00000022" stroke-width="2"/>'
        f'<circle cx="{x+DW-13:.0f}" cy="{base-58:.0f}" r="4.5" fill="{KNOB}"/>'
    )


def win(x, y, glass, frame, w=44, h=50):
    return (
        f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="3" fill="{glass}"/>'
        f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="3" fill="none" stroke="{frame}" stroke-width="4"/>'
        f'<path d="M{x+w/2:.0f} {y} V{y+h} M{x} {y+h/2:.0f} H{x+w}" stroke="{frame}" stroke-width="3"/>'
    )


def svg(w, h, body):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">\n'
            f'  <ellipse cx="{w/2:.0f}" cy="{h-10:.0f}" rx="{w*0.42:.0f}" ry="13" fill="#000" opacity="{SHADOW_A}"/>\n'
            f'  {body}\n</svg>\n')


# ---------------------------------------------------------------- house
def house(wall, roof, glass, door_c):
    W, H = 280, 340
    base = H - 14                # 326
    wtop = 140
    body = (
        f'<rect x="46" y="{wtop}" width="188" height="{base-wtop}" rx="4" fill="{wall}"/>'
        f'<path d="M32 {wtop+2} L140 60 L248 {wtop+2} Z" fill="{roof}"/>'
        f'<rect x="150" y="56" width="20" height="46" rx="3" fill="{CHIM}"/>'
        f'<rect x="40" y="{base-10}" width="200" height="14" rx="3" fill="#0000001f"/>'
        + win(64, wtop + 16, glass, roof, 44, 46) + win(174, wtop + 16, glass, roof, 44, 46)
        + door(140, base, door_c)
    )
    return svg(W, H, body)


# ---------------------------------------------------------------- cottage
def cottage(wall, roof, glass, door_c):
    W, H = 300, 300
    base = H - 12                # 288
    wtop = 150
    body = (
        f'<rect x="44" y="{wtop}" width="212" height="{base-wtop}" rx="8" fill="{wall}"/>'
        f'<path d="M22 {wtop+4} Q150 56 278 {wtop+4} Z" fill="{roof}"/>'
        f'<rect x="196" y="66" width="18" height="40" rx="3" fill="{CHIM}"/>'
        + win(62, wtop + 18, glass, roof, 46, 44) + win(192, wtop + 18, glass, roof, 46, 44)
        + door(150, base, door_c)
    )
    return svg(W, H, body)


# ---------------------------------------------------------------- rowhouse
def rowhouse(wall, roof, glass, door_c):
    W, H = 164, 360
    base = H - 12                # 348
    wtop = 88
    body = (
        f'<rect x="24" y="{wtop}" width="{W-48}" height="{base-wtop}" rx="3" fill="{wall}"/>'
        f'<rect x="16" y="{wtop-16}" width="{W-32}" height="20" rx="3" fill="{roof}"/>'
        f'<rect x="34" y="60" width="18" height="20" fill="{CHIM}"/>'
        + win(30, wtop + 22, glass, roof, W - 60, 44)
        + win(30, wtop + 96, glass, roof, W - 60, 44)
        + door(W / 2, base, door_c)
    )
    return svg(W, H, body)


# ---------------------------------------------------------------- shop (storefront)
def shop(wall, band, awn, awn_lt, frame, glass, door_c):
    W, H = 320, 320
    base = H - 14                # 306
    wtop = 116
    store = base - 104           # storefront band top
    body = (
        f'<rect x="34" y="{wtop}" width="252" height="{base-wtop}" rx="4" fill="{wall}"/>'
        f'<rect x="34" y="{wtop-16}" width="252" height="24" rx="4" fill="{band}"/>'
        f'<rect x="30" y="{store}" width="260" height="{base-store}" fill="#0000001a"/>'
        + win(70, wtop + 20, glass, frame, 40, 44) + win(140, wtop + 20, glass, frame, 40, 44) + win(210, wtop + 20, glass, frame, 40, 44)
        + f'<path d="M28 {store-4} h264 l-14 34 h-236 z" fill="{awn}"/>'
        + f'<path d="M28 {store-4} h264 l-6 15 h-252 z" fill="{awn_lt}"/>'
        + f'<rect x="52" y="{store+40}" width="150" height="{base-store-40}" rx="3" fill="{glass}"/>'
        + f'<rect x="52" y="{store+40}" width="150" height="{base-store-40}" rx="3" fill="none" stroke="{frame}" stroke-width="6"/>'
        + f'<path d="M127 {store+40} V{base} M52 {store+ (base-store-40)//2 +40} H202" stroke="{frame}" stroke-width="5"/>'
        + door(244, base, door_c)
    )
    return svg(W, H, body)


# ---------------------------------------------------------------- apartment
def apartment(wall, trim, glass, frame, door_c, floors=4):
    W, H = 240, 380
    base = H - 12                # 368
    wtop = 64
    ground = base - 96
    rows = ""
    for r in range(floors):
        y = wtop + 26 + r * 60
        if y + 44 > ground - 8:
            break
        for x in (52, 108, 164):
            rows += win(x, y, glass, frame, 34, 44)
    body = (
        f'<rect x="36" y="{wtop}" width="{W-72}" height="{base-wtop}" rx="4" fill="{wall}"/>'
        f'<rect x="28" y="{wtop-16}" width="{W-56}" height="20" rx="4" fill="{trim}"/>'
        f'<rect x="30" y="{ground}" width="{W-60}" height="{base-ground}" fill="#0000001a"/>'
        + rows
        + win(50, ground + 16, glass, frame, 34, 40) + win(156, ground + 16, glass, frame, 34, 40)
        + door(W / 2, base, door_c)
    )
    return svg(W, H, body)


# ---------------------------------------------------------------- office
def office(wall, glass, accent):
    W, H = 260, 372
    base = H - 12                # 360
    wtop = 60
    ground = base - 100
    win_g = ""
    for r in range(4):
        y = wtop + 24 + r * 52
        if y + 36 > ground - 10:
            break
        for c in range(3):
            win_g += f'<rect x="{54+c*58}" y="{y}" width="42" height="38" rx="2" fill="{glass}"/>'
    body = (
        f'<rect x="30" y="{wtop}" width="200" height="{base-wtop}" rx="4" fill="{wall}"/>'
        f'<rect x="24" y="{wtop-14}" width="212" height="18" rx="3" fill="{accent}"/>'
        f'<rect x="26" y="{ground}" width="208" height="{base-ground}" fill="{accent}"/>'
        + win_g
        + f'<rect x="46" y="{ground+16}" width="168" height="{base-ground-16}" rx="2" fill="{glass}"/>'
        + f'<path d="M{W/2:.0f} {ground+16} V{base}" stroke="{accent}" stroke-width="4"/>'
        + door(W / 2, base, accent)
    )
    return svg(W, H, body)


# ---------------------------------------------------------------- townhall
def townhall(wall="#cfc7b6", roof="#9a9084", col="#e8e2d4", face="#efe9d4"):
    W, H = 320, 356
    base = H - 12                # 344
    wtop = 132
    body = (
        f'<rect x="28" y="{base-4}" width="264" height="20" rx="3" fill="#a49c8c"/>'
        f'<rect x="44" y="{base-20}" width="232" height="18" rx="2" fill="#b8b0a0"/>'
        f'<rect x="44" y="{wtop}" width="232" height="{base-20-wtop}" rx="3" fill="{wall}"/>'
        f'<path d="M20 {wtop+4} L160 48 L300 {wtop+4} Z" fill="{roof}"/>'
        f'<rect x="152" y="40" width="16" height="22" fill="{roof}"/>'
        f'<circle cx="160" cy="94" r="20" fill="{face}"/>'
        f'<circle cx="160" cy="94" r="20" fill="none" stroke="{roof}" stroke-width="4"/>'
        f'<path d="M160 94 V80 M160 94 L172 100" stroke="#2a2a28" stroke-width="3.5" stroke-linecap="round"/>'
        + "".join(f'<rect x="{x}" y="{wtop+22}" width="24" height="{base-40-wtop}" fill="{col}"/>' for x in (66, 116, 182, 232))
        + f'<path d="M{160-DW/2:.0f} {base-DH} a{DW/2:.0f} {DW/2:.0f} 0 0 1 {DW:.0f} 0 z" fill="{roof}"/>'
        + door(160, base, "#5c5346")
    )
    return svg(W, H, body)


# ---------------------------------------------------------------- clocktower
def clocktower(wall, roof, face):
    W, H = 172, 404
    base = H - 12                # 392
    wtop = 104
    body = (
        f'<rect x="40" y="{wtop}" width="{W-80}" height="{base-wtop}" rx="3" fill="{wall}"/>'
        f'<path d="M28 {wtop+2} L{W/2:.0f} 40 L{W-28} {wtop+2} Z" fill="{roof}"/>'
        f'<rect x="{W/2-6:.0f}" y="28" width="12" height="16" fill="{roof}"/>'
        f'<circle cx="{W/2:.0f}" cy="150" r="26" fill="{face}"/>'
        f'<circle cx="{W/2:.0f}" cy="150" r="26" fill="none" stroke="{roof}" stroke-width="4"/>'
        f'<path d="M{W/2:.0f} 150 V132 M{W/2:.0f} 150 L{W/2+14:.0f} 158" stroke="#2a2a28" stroke-width="4" stroke-linecap="round"/>'
        + win(W / 2 - 24, 210, face, roof, 48, 40)
        + door(W / 2, base, roof)
    )
    return svg(W, H, body)


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
    "shop":         ("#d9c9a6", "#b8a57e", "#c65b53", "#e0837b", "#8a7a58", "#a9c6cf", "#4a3a2c"),
    "shop_blue":    ("#d9c9a6", "#b8a57e", "#4d7fa8", "#7aa8cc", "#8a7a58", "#c6d9df", "#4a3a2c"),
    "shop_green":   ("#e0d6b8", "#b7a97e", "#5a9161", "#82b98a", "#8a7a58", "#c6d9df", "#3f3226"),
    "shop_orange":  ("#ded0a8", "#bda87c", "#d98a4a", "#e8b07a", "#8a7a58", "#c6d9df", "#4a3a2c"),
    "shop_teal":    ("#ddd0b8", "#b8a980", "#3f8f8a", "#6fb5b0", "#8a7a58", "#cfe0dc", "#3a3428"),
    "shop_plum":    ("#dccdb2", "#b6a37f", "#8a5a7a", "#b085a0", "#8a7a58", "#d8c9d4", "#43303a"),
}
APARTMENTS = {
    "apartment":       ("#b7a3a0", "#8f7d7a", "#dfe6e4", "#7a6a67", "#4a3f3d", 4),
    "apartment_brick": ("#a86b5e", "#7a4a40", "#e0d0c4", "#6a4a42", "#3c2c26", 5),
    "apartment_grey":  ("#9a9a98", "#6e6e6c", "#dfe4e6", "#62625f", "#3a3a37", 4),
    "apartment_blue":  ("#8fa2b0", "#61727e", "#e0e8ec", "#5a6a75", "#33404a", 5),
    "apartment_cream": ("#cdbf9e", "#a08c68", "#f0ead6", "#8a7a58", "#5a4a34", 4),
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


def write(name, s):
    open(os.path.join(OUT, name + ".svg"), "w", encoding="utf-8", newline="\n").write(s)


def main():
    n = 0
    for k, v in HOUSES.items():     write(k, house(*v)); n += 1
    for k, v in SHOPS.items():      write(k, shop(*v)); n += 1
    for k, v in APARTMENTS.items(): write(k, apartment(*v)); n += 1
    for k, v in COTTAGES.items():   write(k, cottage(*v)); n += 1
    for k, v in ROWHOUSES.items():  write(k, rowhouse(*v)); n += 1
    for k, v in OFFICES.items():    write(k, office(*v)); n += 1
    write("clocktower_stone", clocktower("#c6bfae", "#8a8272", "#efe9d4")); n += 1
    write("clocktower_brick", clocktower("#b07a68", "#7d4f42", "#f0e6d2")); n += 1
    write("townhall", townhall()); n += 1
    print(f"wrote {n} building sprites to {OUT}")


if __name__ == "__main__":
    main()
