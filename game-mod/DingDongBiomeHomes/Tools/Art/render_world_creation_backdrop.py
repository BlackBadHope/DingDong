"""Procedural 'First Word' world-creation backdrop for Ding Dong.
Outputs (into argv[1]): Backdrop.png (opaque, the unspoken world), Spoken.png (RGBA light layer
revealed with generation progress), Ring.png (bell toll ring), Glow.png (soft glow sprite)."""
import sys, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

out = Path(sys.argv[1]); out.mkdir(parents=True, exist_ok=True)
W, H = 1920, 1080
rng = np.random.default_rng(1977)
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)

def noise(scale, octaves=5, seed=0):
    r = np.random.default_rng(seed); acc = np.zeros((H, W), np.float32); amp = 1.0; tot = 0
    for o in range(octaves):
        gw, gh = max(2, int(W / scale * 2 ** o)), max(2, int(H / scale * 2 ** o))
        g = (r.random((gh, gw)) * 255).astype(np.uint8)
        acc += amp * np.asarray(Image.fromarray(g).resize((W, H), Image.BICUBIC), np.float32) / 255
        tot += amp; amp *= 0.5
    return acc / tot

def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1); return t * t * (3 - 2 * t)

def col(*c): return np.array(c, np.float32) / 255

def blur(a, r):
    if a.ndim == 2:
        return np.asarray(Image.fromarray(np.clip(a * 255, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(r)), np.float32) / 255
    return np.stack([blur(a[..., i], r) for i in range(a.shape[2])], -1)

def mask_from(draw_fn, r=0):
    m = Image.new('L', (W, H), 0); draw_fn(ImageDraw.Draw(m))
    if r: m = m.filter(ImageFilter.GaussianBlur(r))
    return np.asarray(m, np.float32) / 255

CX, CY = 960, 392                     # the First Word (bell core)
rad = np.hypot(xx - CX, (yy - CY) * 1.08)

# ---------- sky ----------
t = yy / H
sky = (col(4, 5, 18)[None, None] * (1 - t[..., None]) + col(20, 14, 46)[None, None] * t[..., None])
n1, n2, n3 = noise(520, 6, 1), noise(300, 5, 2), noise(900, 4, 3)
neb = smooth(0.42, 0.78, n1 * 0.7 + n3 * 0.5 - 0.1)
hue = n2[..., None]
nebc = col(92, 38, 150) * (1 - hue) + col(24, 120, 150) * hue
nebc = nebc * (1 - smooth(0.6, 0.9, n3)[..., None]) + col(210, 140, 70) * smooth(0.6, 0.9, n3)[..., None]
core_bias = np.exp(-rad / 700)
sky += nebc * (neb * (0.35 + 0.5 * core_bias))[..., None]
dust = smooth(0.55, 0.85, noise(140, 5, 4)) * 0.10
sky += col(150, 120, 200) * dust[..., None] * core_bias[..., None]

# stars
stars = np.zeros((H, W), np.float32)
n = 5200
sx, sy = rng.integers(0, W, n), rng.integers(0, int(H * 0.82), n)
sb = rng.random(n) ** 3
stars[sy, sx] = 0.25 + sb * 0.9
stars = np.maximum(stars, blur(stars, 1.2) * 2.5)
big = rng.choice(n, 70, replace=False)
flare = Image.new('L', (W, H), 0); fd = ImageDraw.Draw(flare)
for i in big:
    x, y, L = int(sx[i]), int(sy[i]), int(6 + sb[i] * 22)
    fd.line([(x - L, y), (x + L, y)], fill=170); fd.line([(x, y - L), (x, y + L)], fill=170)
stars += np.asarray(flare.filter(ImageFilter.GaussianBlur(1.1)), np.float32) / 255 * 0.8
sky += col(225, 230, 255) * stars[..., None] * 0.85

# faction constellations: Gardeners (sprout, left), Tide (wave, right), Archivists (open book, top)
const = Image.new('L', (W, H), 0); cd = ImageDraw.Draw(const)
sprout = [(250, 330), (262, 280), (282, 236), (306, 206), (262, 280), (222, 250), (196, 214), (282, 236), (330, 238), (366, 222)]
wave = [(1500, 300), (1552, 262), (1612, 246), (1668, 262), (1700, 300), (1688, 334), (1652, 336), (1636, 306), (1700, 300), (1760, 318), (1816, 300)]
book = [(760, 118), (842, 96), (930, 112), (960, 140), (990, 112), (1078, 96), (1160, 118), (1160, 176), (1078, 158), (990, 172), (960, 196), (930, 172), (842, 158), (760, 176), (760, 118)]
for pts in (sprout, wave, book):
    cd.line(pts, fill=60, width=1)
    for (x, y) in pts:
        cd.ellipse([x - 3, y - 3, x + 3, y + 3], fill=255)
cm = np.asarray(const.filter(ImageFilter.GaussianBlur(0.8)), np.float32) / 255
cm = np.maximum(cm, blur(cm, 6) * 1.6)
sky += col(150, 210, 255) * cm[..., None] * 0.55

img = sky

# ---------- planet / ocean / continent ----------
PR = 6200; PCY = 800 + PR
hor = PCY - np.sqrt(np.maximum(PR ** 2 - (np.arange(W) - CX) ** 2, 0))
xs = np.arange(W, dtype=np.float32)
def fbm1(seed, base, octs=6, ridged=False):
    r = np.random.default_rng(seed); acc = np.zeros(W, np.float32); amp, tot = 1.0, 0.0
    for o in range(octs):
        n = int(W / base * 2 ** o) + 2
        v = np.interp(xs, np.linspace(0, W, n), r.random(n)).astype(np.float32)
        if ridged: v = 1 - np.abs(2 * v - 1)
        acc += amp * v; tot += amp; amp *= 0.52
    return acc / tot
def taper(x0, x1, soft):
    return smooth(x0, x0 + soft, xs) * (1 - smooth(x1 - soft, x1, xs))
rim_d = yy - hor[None, :]
below = smooth(-2, 4, rim_d)
# ocean everywhere below the horizon
depth = smooth(0, 300, rim_d)
oceanc = col(14, 48, 70) * (1 - depth[..., None]) + col(3, 8, 20) * depth[..., None]
img = img * (1 - below[..., None]) + oceanc * below[..., None]
on = noise(36, 4, 11)
waves = (np.sin(rim_d * 0.5 + on * 10) * 0.5 + 0.5) ** 8
draft = waves * np.exp(-np.maximum(rim_d, 0) / 240) * below * (0.25 + 0.75 * smooth(1300, 1900, xx))
img += col(60, 190, 215) * draft[..., None] * 0.45
# atmosphere rim
rim = np.exp(-np.abs(rim_d) / 9) * 0.9 + np.exp(-np.maximum(-rim_d, 0) / 70) * (rim_d < 0) * 0.4
img = img + col(90, 170, 235) * rim[..., None] * 0.5
# Abyss trench on the far left: the ocean falls out of the world
tr_w = 90 + 0 * yy
trench = smooth(0, 60, rim_d) * np.exp(-((xx - (190 - (yy - 800) * 0.18)) / (40 + (yy - 800).clip(0) * 0.35)) ** 2)
img = img * (1 - trench[..., None] * 0.95)
bio = np.zeros((H, W), np.float32)
bx, by = rng.integers(40, 400, 520), rng.integers(810, 1078, 520)
bio[by, bx] = rng.random(520) * 0.9 + 0.2
bio = (np.maximum(bio, blur(bio, 1.5) * 4)) * np.clip(trench * 1.6, 0, 1)
img += col(70, 150, 255) * bio[..., None] * 0.9
# three mountain ranges with aerial perspective; heights taper to the shores (no hard cuts)
ranges = []
specs = [  # seed, base, height, lift, span, colour, haze
    (21, 260, 150, 10, (330, 1650), col(30, 33, 62), 0.22),
    (22, 180, 100, -50, (260, 1720), col(16, 18, 36), 0.10),
    (23, 120, 70, -150, (200, 1800), col(7, 8, 16), 0.03)]
for i, (seed, base, h, lift, (x0, x1), c, haze) in enumerate(specs):
    prof = fbm1(seed, base, 7, ridged=True) ** 1.6
    massif = np.exp(-((xs - 1010) / 230) ** 2) * (0.55 if i == 0 else 0.25)
    t_ = taper(x0, x1, 260)
    top = hor + 0 - lift - (prof * h + massif * h * 0.9) * t_ + (1 - t_) * 60
    m = (yy >= top[None, :]).astype(np.float32) * t_[None, :] ** 0.3
    m = blur(m, 0.8)
    shade = smooth(0, 160, yy - top[None, :])
    lc = c * (1 - shade[..., None] * 0.5)
    lc = lc + col(70, 110, 170) * haze * (1 - shade[..., None])
    img = img * (1 - m[..., None]) + lc * m[..., None]
    edge = np.clip(m - blur(m, 2.5), 0, 1) * 4
    img += col(110, 140, 200) * np.clip(edge, 0, 1)[..., None] * (0.25 - i * 0.06)
    ranges.append((top, m))
landf = np.clip(sum(m for _, m in ranges), 0, 1)
land = landf > 0.5
top = ranges[0][0]
ocean = below * (1 - landf)

# ---------- the First Word: bell glyph + light ----------
glow = np.exp(-rad / 70) * 1.2 + np.exp(-rad / 210) * 0.55 + np.exp(-rad / 620) * 0.28
occl = 1 - landf * 0.85
img += col(255, 214, 150) * (glow * occl)[..., None] * 0.9
# sound rings (bell toll) — slightly elliptic, gold/cyan
rings = np.zeros((H, W), np.float32); ringc = np.zeros((H, W, 3), np.float32)
for k in range(1, 9):
    rr = 120 * 1.38 ** k; w = 1.5 + k * 0.9; a = 0.55 * 0.82 ** k
    ell = np.hypot(xx - CX, (yy - CY) * 1.65)
    ring = np.exp(-((ell - rr) / w) ** 2) * a * (1 - below * 0.85) * occl
    c = col(255, 200, 110) if k % 2 else col(120, 220, 255)
    ringc += c * ring[..., None]
img += ringc
# bell silhouette (luminous)
bell = Image.new('L', (W, H), 0); bd = ImageDraw.Draw(bell)
left = []
for i in range(0, 201):
    u = i / 200.0
    yb = CY - 70 + u * 150
    half = 30 + 14 * u + 46 * u ** 5
    left.append((CX - half, yb))
bd.polygon(left + [(2 * CX - x, y) for (x, y) in reversed(left)], fill=255)
bd.ellipse([CX - 31, CY - 100, CX + 31, CY - 40], fill=255)          # dome
bd.rounded_rectangle([CX - 96, CY + 72, CX + 96, CY + 90], radius=9, fill=255)  # lip
bd.ellipse([CX - 14, CY + 88, CX + 14, CY + 116], fill=255)          # clapper
bd.ellipse([CX - 11, CY - 122, CX + 11, CY - 96], outline=255, width=6)  # crown loop
bm = np.asarray(bell, np.float32) / 255
inner = np.asarray(bell.filter(ImageFilter.MinFilter(9)), np.float32) / 255
bellglow = blur(bm, 10) * 1.0 + blur(bm, 30) * 0.8
img += col(255, 225, 170) * bellglow[..., None] * 0.95
img = img * (1 - bm[..., None]) + col(255, 250, 238) * bm[..., None] * 1.25
shadeb = smooth(CX - 90, CX + 90, xx)  # light from the left
img = img * (1 - inner[..., None] * (0.08 + 0.30 * shadeb)[..., None]) + col(235, 170, 90) * (inner * (0.08 + 0.30 * shadeb))[..., None]

# vignette + tone map
vig = 1 - smooth(0.55, 1.25, np.hypot((xx - W / 2) / (W / 2), (yy - H / 2) / (H / 2))) * 0.75
img = img * vig[..., None]
img = 1 - np.exp(-img * 1.35)          # soft filmic shoulder
img = np.clip(img, 0, 1)
body = col(255, 246, 222) * (1 - shadeb[..., None] * 0.18) + col(255, 196, 100) * (shadeb * inner * 0.35)[..., None]
img = img * (1 - bm[..., None]) + body * bm[..., None]
grain = (rng.random((H, W)) - 0.5)[..., None] * 0.012
Image.fromarray((np.clip(img + grain, 0, 1) * 255).astype(np.uint8), 'RGB').save(out / 'Backdrop.png', optimize=True)

# ---------- Spoken layer (RGBA): the world lit by the Word ----------
sp = np.zeros((H, W, 3), np.float32); sa = np.zeros((H, W), np.float32)
def add(c, a):
    global sp, sa
    sp = sp + np.asarray(c, np.float32) * a[..., None]; sa = sa + a
# god rays from the bell down to the land
rays = Image.new('L', (W, H), 0); rd = ImageDraw.Draw(rays)
for k in range(16):
    ang = math.radians(60 + k * 4 + rng.random() * 2)
    L = 1400; spread = math.radians(0.6 + rng.random() * 1.4)
    p1 = (CX + L * math.cos(ang - spread), CY + L * math.sin(ang - spread))
    p2 = (CX + L * math.cos(ang + spread), CY + L * math.sin(ang + spread))
    rd.polygon([(CX, CY), p1, p2], fill=int(60 + rng.random() * 90))
ry = np.asarray(rays.filter(ImageFilter.GaussianBlur(9)), np.float32) / 255 * np.exp(-rad / 900) * (yy > CY)
add(col(255, 210, 140), ry * 0.30)
# golden rim on ridges + warm wash over the continent
for i, (tp, mm) in enumerate(ranges):
    ridge = np.clip(mm - blur(mm, 3), 0, 1) * 5
    add(col(255, 205, 130), np.clip(ridge, 0, 1) * (0.95 - i * 0.25))
wash = landf * np.exp(-np.maximum(yy - top[None, :], 0) / 90) * 0.28
add(col(255, 150, 70), wash)
# 61 residences
lights = np.zeros((H, W), np.float32)
placed = 0
while placed < 61:
    x = int(rng.integers(420, 1620))
    y = int(ranges[1 + placed % 2][0][x] + rng.integers(10, 120))
    if y >= H - 8 or not land[y, x]: continue
    lights[y - 1:y + 2, x - 1:x + 2] = 1.0; placed += 1
lg = np.maximum(lights, blur(lights, 3) * 6) + blur(lights, 12) * 10
add(col(255, 200, 110), np.clip(lg, 0, 1.4))
# the drafts in the ocean glow brighter once spoken
add(col(90, 230, 240), np.clip(draft * ocean * 1.2, 0, 1))
sa = np.clip(sa, 0, 1)
rgb = np.where(sa[..., None] > 1e-4, sp / np.maximum(sa[..., None], 1e-4), 0)
rgb = np.clip(1 - np.exp(-rgb * 1.2), 0, 1)
rgba = np.dstack([rgb, sa])
Image.fromarray((rgba * 255).astype(np.uint8), 'RGBA').save(out / 'Spoken.png', optimize=True)

# ---------- sprites ----------
S = 512; y2, x2 = np.mgrid[0:S, 0:S].astype(np.float32); r2 = np.hypot(x2 - S / 2, y2 - S / 2)
ring = np.exp(-((r2 - 236) / 7) ** 2) + np.exp(-((r2 - 236) / 22) ** 2) * 0.35
Image.fromarray((np.dstack([np.ones((S, S, 3)), np.clip(ring, 0, 1)]) * 255).astype(np.uint8), 'RGBA').save(out / 'Ring.png')
G = 256; y3, x3 = np.mgrid[0:G, 0:G].astype(np.float32); r3 = np.hypot(x3 - G / 2, y3 - G / 2) / (G / 2)
g = np.clip(np.exp(-r3 * 4) * 1.2 * (1 - smooth(0.85, 1.0, r3)), 0, 1)
Image.fromarray((np.dstack([np.ones((G, G, 3)), g]) * 255).astype(np.uint8), 'RGBA').save(out / 'Glow.png')

# ---------- preview composite (60% spoken) ----------
base = Image.open(out / 'Backdrop.png').convert('RGBA'); spk = Image.open(out / 'Spoken.png')
half = int(W * 0.30)
m = Image.new('L', (W, H), 0); ImageDraw.Draw(m).rectangle([CX - half, 0, CX + half, H], fill=255)
m = m.filter(ImageFilter.GaussianBlur(18))
spk.putalpha(Image.fromarray((np.asarray(spk.getchannel('A'), np.float32) * np.asarray(m, np.float32) / 255).astype(np.uint8)))
base.alpha_composite(spk)
base.convert('RGB').save(out / 'preview-60.png')
print('ok')
