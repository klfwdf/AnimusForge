#!/usr/bin/env python3
"""Slice the generated DialogueUI control sheet into runtime sprites.

The generation relay paints the four controls on a solid magenta backdrop; this
script chroma-keys the backdrop, auto-crops each connected component, normalises
nine-slice borders, and writes the afdui_* sprites plus hover/pressed variants.
No network access or image generation occurs in this script.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
SPRITES = ROOT / "GUI" / "SpriteParts"
WORK = ROOT / "work"
SOURCE = ROOT / "outputs" / "image_d53942f109fe49b8b7bb96750a9c9d84.jpg"
RESAMPLE = Image.Resampling.LANCZOS


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def alpha_mask(image: Image.Image) -> Image.Image:
    """Magenta chroma key with 1px erosion and soft edge anti-aliasing."""
    rgb = image.convert("RGB")
    px = rgb.load()
    mask = Image.new("L", rgb.size)
    mp = mask.load()
    for y in range(rgb.height):
        for x in range(rgb.width):
            r, g, b = px[x, y]
            if r > 150 and b > 150 and r - g > 80 and b - g > 80:
                mp[x, y] = 0
            else:
                mp[x, y] = 255
    mask = mask.filter(ImageFilter.MinFilter(3))
    return mask.filter(ImageFilter.GaussianBlur(1.1))


def components(mask: Image.Image):
    """Opaque connected components, largest first; drops specks under 400px."""
    w, h = mask.size
    mp = mask.load()
    seen = bytearray(w * h)
    found = []
    for y in range(h):
        for x in range(w):
            i = y * w + x
            if seen[i] or mp[x, y] < 128:
                continue
            queue = deque([(x, y)])
            seen[i] = 1
            comp = []
            while queue:
                cx, cy = queue.popleft()
                comp.append((cx, cy))
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if 0 <= nx < w and 0 <= ny < h:
                        ni = ny * w + nx
                        if not seen[ni] and mp[nx, ny] >= 128:
                            seen[ni] = 1
                            queue.append((nx, ny))
            found.append(comp)
    found.sort(key=len, reverse=True)
    return [c for c in found if len(c) >= 400]


def crop_component(image: Image.Image, mask: Image.Image, comp) -> Image.Image:
    xs = [p[0] for p in comp]
    ys = [p[1] for p in comp]
    box = (min(xs), min(ys), max(xs) + 1, max(ys) + 1)
    cut = image.convert("RGBA").crop(box)
    cut.putalpha(mask.crop(box))
    return cut


def nine_slice(source: Image.Image, size, source_border: int, output_border: int) -> Image.Image:
    sw, sh = source.size
    w, h = size
    sx = [0, source_border, sw - source_border, sw]
    sy = [0, source_border, sh - source_border, sh]
    dx = [0, output_border, w - output_border, w]
    dy = [0, output_border, h - output_border, h]
    out = Image.new("RGBA", (w, h))
    for row in range(3):
        for col in range(3):
            tile = source.crop((sx[col], sy[row], sx[col + 1], sy[row + 1]))
            tile = tile.resize((dx[col + 1] - dx[col], dy[row + 1] - dy[row]), RESAMPLE)
            out.alpha_composite(tile, (dx[col], dy[row]))
    return out


def save_sprite(name: str, image: Image.Image, descriptions, description: str, border=None):
    path = SPRITES / (name + ".png")
    image.save(path, optimize=True)
    descriptions.append({"name": name, "path": str(path.relative_to(ROOT)).replace("\\", "/"),
                         "width": image.width, "height": image.height,
                         "nine_slice_border": border, "description": description,
                         "sha256": sha256(path)})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=SOURCE)
    args = parser.parse_args()
    SPRITES.mkdir(parents=True, exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)

    sheet = Image.open(args.source)
    mask = alpha_mask(sheet)
    parts = components(mask)
    if len(parts) < 4:
        raise SystemExit("Expected 4 controls on the sheet, found %d" % len(parts))
    crops = [crop_component(sheet, mask, c) for c in parts[:4]]
    for i, c in enumerate(crops):
        c.save(WORK / ("control-%d.png" % i))

    descriptions = []
    # Largest first: parchment button plaque, walnut name plate, scroll handle, bronze pip.
    plate = nine_slice(crops[0], (256, 128), 64, 22)
    save_sprite("afdui_button_plate_normal", plate, descriptions,
                "Parchment answer/button plaque with engraved gold rim.", 22)
    save_sprite("afdui_button_plate_hover", ImageEnhance.Brightness(plate).enhance(1.18), descriptions,
                "Highlighted parchment plaque state.", 22)
    save_sprite("afdui_button_plate_pressed", ImageEnhance.Brightness(plate).enhance(0.75), descriptions,
                "Pressed parchment plaque state.", 22)

    plate_img = crops[1]
    plate_img = plate_img.resize((626, 86), RESAMPLE)
    save_sprite("afdui_nameplate", plate_img, descriptions,
                "Dark walnut speaker-name banner with bronze trim; fixed-size use.")

    handle = crops[2]
    handle = handle.resize((round(handle.width * 112 / handle.height), 112), RESAMPLE)
    save_sprite("afdui_scroll_handle", handle, descriptions,
                "Ornate bronze scrollbar grip; drawn at small fixed sizes.")

    pip = crops[3]
    side = min(pip.width, pip.height)
    cx, cy = pip.width / 2, pip.height / 2
    pip = pip.crop((round(cx - side / 2), round(cy - side / 2), round(cx + side / 2), round(cy + side / 2)))
    pip = pip.resize((44, 44), RESAMPLE)
    save_sprite("afdui_persuasion_dot", pip, descriptions,
                "Bronze persuasion progress pip replacing the tinted white circle.")

    manifest = WORK / "controls-manifest.json"
    manifest.write_text(json.dumps(descriptions, indent=2, ensure_ascii=False), encoding="utf-8")
    for d in descriptions:
        print(d["name"], d["width"], "x", d["height"], "border", d["nine_slice_border"])


if __name__ == "__main__":
    main()
