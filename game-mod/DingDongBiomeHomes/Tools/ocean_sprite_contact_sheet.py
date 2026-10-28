"""Before/after contact sheet for Tools/remaster_ocean_npc_sprites.py (roadmap item 9).
usage: py -3 Tools/ocean_sprite_contact_sheet.py OUT.png [--src ...] [--dst ...]"""
import argparse, sys
from pathlib import Path
from PIL import Image, ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parent))
from remaster_ocean_npc_sprites import MOD, SHEETS  # noqa: E402

ap = argparse.ArgumentParser(); ap.add_argument("out")
ap.add_argument("--src", default=str(MOD / "Tools/SpriteSources/OceanOriginal"))
ap.add_argument("--dst", default=str(MOD / "OceanExpeditions/Assets"))
o = ap.parse_args()
SEA, PARCH, TXT = (34, 48, 66, 255), (226, 205, 160, 255), (40, 28, 20, 255)
rows = []
for name in SHEETS:
    b = Image.open(Path(o.src) / f"{name}.png").convert("RGBA"); a = Image.open(Path(o.dst) / f"{name}.png").convert("RGBA")
    s = max(2, min(6, 300 // max(b.width, b.height // max(1, SHEETS[name]))))
    rows.append((name, s, b, a))
pad = 12; colw = max(r[2].width * r[1] for r in rows) + pad * 2
H = sum(max(r[2].height * r[1], 20) + pad * 3 + 14 for r in rows) + 40
sheet = Image.new("RGBA", (220 + colw * 4, H), PARCH); d = ImageDraw.Draw(sheet)
d.text((10, 10), "Ding Dong item 9: ocean NPC sheets  before (sea / parchment)  ->  after (sea / parchment)", fill=TXT)
y = 40
for name, s, b, a in rows:
    h = b.height * s
    d.text((10, y + 4), f"{name}\n{b.width}x{b.height}  frames={SHEETS[name]}  x{s}", fill=TXT)
    for i, (img, bg) in enumerate([(b, SEA), (b, PARCH), (a, SEA), (a, PARCH)]):
        x = 220 + i * colw
        cell = Image.new("RGBA", (colw - pad, h + pad), bg)
        cell.alpha_composite(img.resize((img.width * s, img.height * s), Image.NEAREST), (pad // 2, pad // 2))
        sheet.alpha_composite(cell, (x, y))
    d.line([(220 + 2 * colw - pad // 2 - 2, y), (220 + 2 * colw - pad // 2 - 2, y + h + pad)], fill=(150, 30, 30, 255), width=3)
    y += max(h, 20) + pad * 3 + 14
sheet.convert("RGB").save(o.out)
print(o.out, sheet.size)
