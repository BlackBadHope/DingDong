"""Ding Dong: reference-style remaster of the mod's own ocean NPC sheets (roadmap item 9).

Style target = journal portraits Assets/Ui/Residents/CorePortraits.png (owner reference
ideal-candidate-book-and-sprites-20261027): crisp pixel clusters, binary alpha, limited
shared palette, dark ink outline, readable top-left light.

The pass is deterministic and never moves pixels between frames: every output keeps the exact
sheet size, frame count/order (vertical strips), silhouette (+-edge cleanup) and RGBA PNG format
loaded by OceanFauna/OceanCreatures/OceanCrustaceanRig/OceanSegmentAnimator.
PelagicTitan and HadalMaw (the two ocean giants the owner liked) are excluded by design.

usage: py -3 Tools/remaster_ocean_npc_sprites.py [--src Tools/SpriteSources/OceanOriginal]
                                                 [--dst OceanExpeditions/Assets] [--report out.json]
Requires Pillow + numpy (dev machine only; Tools/* is in buildIgnore).
The committed PNGs were produced with Python 3.14, Pillow 12.1.1 and numpy 2.4.6. The palette seed
uses Image.quantize(MEDIANCUT), so other Pillow/numpy versions may give byte-different (but
equivalent) output; the script prints a warning when the versions differ.

Report metrics: silhouette_iou_cut_per_frame compares the source mask alpha >= ALPHA_CUT (110) with
the output (alpha > 0), i.e. it measures only edge cleanup. silhouette_iou_visible_per_frame compares
the full visible source silhouette (alpha > 0) with the output, and removed_semi_alpha_px counts the
source pixels with alpha 1..109 that were dropped (anti-alias fringe, but also thin tentacle/tail tips).
"""
from __future__ import annotations
import argparse, json
from pathlib import Path
import numpy as np
from PIL import Image

MOD = Path(__file__).resolve().parent.parent
EXCLUDED = {"PelagicTitan", "HadalMaw"}  # owner: keep the two ocean giants as they are
# name -> frames (must match Main.npcFrameCount in OceanFauna/OceanCreatures; rig parts are single images)
SHEETS = {
    "BrinePuffer": 4, "LanternJelly": 4, "RibbonSkate": 4, "SiltRibbon": 4, "VentNautilus": 4,
    "GlasswingSkimmer": 4, "GlassfinDarter": 1, "SailfinHunter": 1,
    "CoralPincher": 1, "CoralPincherTorso": 1, "CoralPincherClaw": 1, "CoralPincherLeg": 1,
    "SaltbackCustodian": 1, "SaltbackCustodianTorso": 1, "SaltbackCustodianClaw": 1, "SaltbackCustodianLeg": 1,
}
INK = np.array([30, 22, 36], np.float32)  # warm dark ink used by the book art
ALPHA_CUT = 110
PINNED = {"Pillow": "12.1.1", "numpy": "2.4.6"}  # versions the committed PNGs were generated with


def lum(c: np.ndarray) -> np.ndarray:
    return c[..., 0] * .299 + c[..., 1] * .587 + c[..., 2] * .114


def grade(rgb: np.ndarray) -> np.ndarray:
    """Slight saturation/contrast lift toward the saturated book palette."""
    l = lum(rgb)[..., None]
    rgb = l + (rgb - l) * 1.16
    rgb = (rgb - 128) * 1.07 + 128
    return np.clip(rgb, 0, 255)


def to_lab(rgb: np.ndarray) -> np.ndarray:
    c = rgb / 255.0
    c = np.where(c > .04045, ((c + .055) / 1.055) ** 2.4, c / 12.92)
    m = np.array([[.4124, .3576, .1805], [.2126, .7152, .0722], [.0193, .1192, .9505]], np.float32)
    xyz = c @ m.T / np.array([.9505, 1.0, 1.089], np.float32)
    f = np.where(xyz > .008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def kmeans(px: np.ndarray, k: int, extra: int) -> np.ndarray:
    """Deterministic palette in CIE Lab: median-cut seed, Lloyd refinement, then extra centres for
    badly represented accents (eyes, glow spots) so small bright details survive."""
    w = int(np.ceil(np.sqrt(len(px))))
    tile = np.zeros((w * w, 3), np.uint8); tile[:len(px)] = px.astype(np.uint8); tile[len(px):] = px[0].astype(np.uint8)
    seed = Image.fromarray(tile.reshape(w, w, 3)).quantize(colors=k, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    pal = np.array(seed.getpalette()[:k * 3], np.float32).reshape(-1, 3)
    lab = to_lab(px)

    def assign(p):
        d = ((lab[:, None, :] - to_lab(p)[None]) ** 2).sum(-1)
        return d.argmin(1), d.min(1)

    def lloyd(p, it):
        for _ in range(it):
            a, _ = assign(p)
            for i in range(len(p)):
                m = a == i
                if m.any(): p[i] = px[m].mean(0)
        return p

    pal = lloyd(pal, 10)
    for _ in range(extra):
        a, d = assign(pal)
        if d.max() < 12 ** 2: break
        pal = np.vstack([pal, px[d.argmax()]])
        pal = lloyd(pal, 3)
    return pal


def neighbours(mask: np.ndarray) -> np.ndarray:
    p = np.pad(mask, 1)
    return np.stack([p[:-2, 1:-1], p[2:, 1:-1], p[1:-1, :-2], p[1:-1, 2:]])  # up, down, left, right


def remaster(img: Image.Image, frames: int) -> Image.Image:
    a = np.array(img.convert("RGBA")).astype(np.float32)
    H, W = a.shape[:2]; fh = H // frames
    out = np.zeros_like(a)
    solid = a[..., 3] >= ALPHA_CUT
    # per-frame cleanup so no pixel ever bleeds across a frame boundary
    for f in range(frames):
        s = solid[f * fh:(f + 1) * fh]
        n = neighbours(s)
        s = s & (n.sum(0) > 0)                       # drop orphan pixels
        s = s | (~s & (n.sum(0) == 4))               # close one-pixel pin holes
        solid[f * fh:(f + 1) * fh] = s
    rgb = a[..., :3].copy()
    # recover colour for pin-hole pixels from neighbours
    holes = solid & (a[..., 3] < ALPHA_CUT)
    if holes.any():
        p = np.pad(rgb, ((1, 1), (1, 1), (0, 0)), mode="edge")
        avg = (p[:-2, 1:-1] + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]) / 4
        rgb[holes] = avg[holes]
    rgb = grade(rgb)
    px = rgb[solid]
    k = int(np.clip(round(np.sqrt(len(px)) / 2.4), 10, 40))
    pal = kmeans(px, k, extra=10)
    np_ = len(pal)
    # ink twins for the outline, softer twins for one/two-pixel strands (tentacles, whiskers, fin rays)
    pal = np.vstack([pal, np.clip(pal * .32 + INK * .55, 0, 255), np.clip(pal * .70 + INK * .12, 0, 255)])
    idx = np.full((H, W), -1, np.int32)
    plab = to_lab(pal)
    idx[solid] = ((to_lab(px)[:, None, :] - plab[None, :np_]) ** 2).sum(-1).argmin(1)
    for f in range(frames):
        sl = slice(f * fh, (f + 1) * fh)
        fi = idx[sl]; fs = solid[sl]
        # cluster cleanup: an interior pixel that matches none of its 8 neighbours takes the most
        # common neighbour colour only when that colour is perceptually close (Delta E < 14), so
        # accents (eye glints, glow spots, spiral seams) survive while dither noise is removed
        p = np.pad(fi, 1, constant_values=-1)
        nb = np.stack([p[1 + dy:fh + 1 + dy, 1 + dx:W + 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx])
        same = (nb == fi[None]).sum(0)
        inner = fs & (nb >= 0).all(0)
        new = fi.copy()
        for y, x in zip(*np.nonzero(inner & (same == 0))):
            v = nb[:, y, x]; vals, cnt = np.unique(v, return_counts=True)
            best = vals[cnt.argmax()]
            if np.sqrt(((plab[fi[y, x]] - plab[best]) ** 2).sum()) < 14: new[y, x] = best
        # ink outline on the silhouette edge (hue-preserving dark twin of the local colour)
        n = neighbours(fs)
        edge = fs & ~n.all(0)
        thin = edge & ((~n[0] & ~n[1]) | (~n[2] & ~n[3]))
        new[edge & ~thin] += np_
        new[thin] += 2 * np_
        # top-left rim light one pixel inside the outline, only on bodies at least 3px thick
        up_edge, left_edge = np.zeros_like(fs), np.zeros_like(fs)
        up_edge[1:] = edge[:-1]; left_edge[:, 1:] = edge[:, :-1]
        thick = fs & ~edge & n.all(0)
        rim = thick & (up_edge | left_edge)
        if rim.any():
            q = np.pad(fs, 2)
            deep = np.ones_like(fs)
            for dy in (-2, 0, 2):
                for dx in (-2, 0, 2):
                    deep &= q[2 + dy:fh + 2 + dy, 2 + dx:W + 2 + dx]
            rim &= deep
        frgb = pal[np.maximum(new, 0)]
        frgb[rim] = np.clip(frgb[rim] * 1.12 + 14, 0, 255)
        o = out[sl]
        o[..., :3] = np.where(fs[..., None], frgb, 0)
        o[..., 3] = np.where(fs, 255, 0)
    return Image.fromarray(out.round().astype(np.uint8), "RGBA")


def iou(a: np.ndarray, b: np.ndarray) -> float:
    u = (a | b).sum()
    return float((a & b).sum() / u) if u else 1.0


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", default=str(MOD / "Tools/SpriteSources/OceanOriginal"))
    ap.add_argument("--dst", default=str(MOD / "OceanExpeditions/Assets"))
    ap.add_argument("--report", default="")
    o = ap.parse_args()
    import PIL
    have = {"Pillow": PIL.__version__, "numpy": np.__version__}
    if have != PINNED:
        print(f"WARNING: versions {have} differ from {PINNED}; output may not be byte-identical to the committed PNGs")
    src, dst = Path(o.src), Path(o.dst)
    rep = {}
    for name, frames in SHEETS.items():
        assert name not in EXCLUDED
        before = Image.open(src / f"{name}.png").convert("RGBA")
        assert before.height % frames == 0, name
        after = remaster(before, frames)
        assert after.size == before.size
        after.save(dst / f"{name}.png", optimize=True)
        b, aa = np.array(before), np.array(after)
        fh = before.height // frames
        rep[name] = dict(size=list(before.size), frames=frames, frame_h=fh,
                         colors_before=len(np.unique(b[b[..., 3] > 0].reshape(-1, 4), axis=0)),
                         colors_after=len(np.unique(aa[aa[..., 3] > 0].reshape(-1, 4), axis=0)),
                         semi_alpha_before=int(((b[..., 3] > 0) & (b[..., 3] < 255)).sum()),
                         semi_alpha_after=int(((aa[..., 3] > 0) & (aa[..., 3] < 255)).sum()),
                         removed_semi_alpha_px=int(((b[..., 3] > 0) & (b[..., 3] < ALPHA_CUT) & (aa[..., 3] == 0)).sum()),
                         removed_solid_px=int(((b[..., 3] >= ALPHA_CUT) & (aa[..., 3] == 0)).sum()),
                         added_px=int(((b[..., 3] == 0) & (aa[..., 3] > 0)).sum()),
                         silhouette_iou_cut_per_frame=[round(iou(b[f * fh:(f + 1) * fh, :, 3] >= ALPHA_CUT, aa[f * fh:(f + 1) * fh, :, 3] > 0), 4) for f in range(frames)],
                         silhouette_iou_visible_per_frame=[round(iou(b[f * fh:(f + 1) * fh, :, 3] > 0, aa[f * fh:(f + 1) * fh, :, 3] > 0), 4) for f in range(frames)])
    if o.report: Path(o.report).write_text(json.dumps(rep, indent=1), "utf-8")
    print(json.dumps(rep, indent=1))


if __name__ == "__main__":
    main()
