#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from PIL import Image, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
SPRITE = ROOT / "GUI" / "SpriteParts" / "afdui_shout_dialog.png"
SIZE = (980, 460)
RESAMPLE = Image.Resampling.LANCZOS


def make_panel(source: Path) -> Image.Image:
    with Image.open(source) as opened:
        rgb = opened.convert("RGB")
        pixels = rgb.load()
        mask = Image.new("L", rgb.size)
        alpha = mask.load()
        for y in range(rgb.height):
            for x in range(rgb.width):
                r, g, b = pixels[x, y]
                is_key = r > 150 and b > 150 and r - g > 80 and b - g > 80
                alpha[x, y] = 0 if is_key else 255
        mask = mask.filter(ImageFilter.MinFilter(5))
        bounds = mask.getbbox()
        if bounds is None:
            raise ValueError("Generated panel contains no non-key pixels")
        panel = opened.convert("RGBA").crop(bounds)
        panel.putalpha(mask.crop(bounds))
    ratio = panel.width / panel.height
    if abs(ratio - SIZE[0] / SIZE[1]) > 0.25:
        raise ValueError(f"Panel aspect ratio is not close to 980x460: {ratio:.4f}")
    return ImageOps.pad(panel, SIZE, method=RESAMPLE, color=(0, 0, 0, 0))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=SPRITE)
    parser.add_argument("--pencil-copy", type=Path, required=True)
    args = parser.parse_args()
    panel = make_panel(args.source)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.pencil_copy.parent.mkdir(parents=True, exist_ok=True)
    panel.save(args.output, optimize=True)
    panel.save(args.pencil_copy, optimize=True)
    digest = hashlib.sha256(args.output.read_bytes()).hexdigest()
    print(f"{args.output} {panel.width}x{panel.height} sha256={digest}")
    print(f"Pencil image: {args.pencil_copy}")


if __name__ == "__main__":
    main()
