#!/usr/bin/env python3
from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def remove_neutral_component(image: Image.Image, seeds: list[tuple[int, int]]) -> Image.Image:
    rgb = image.convert("RGB")
    px = rgb.load()
    alpha = image.getchannel("A")
    out = alpha.load()
    width, height = rgb.size
    seen = bytearray(width * height)
    queue = deque()

    def neutral(x: int, y: int) -> bool:
        r, g, b = px[x, y]
        return max(r, g, b) - min(r, g, b) <= 14

    for x, y in seeds:
        if 0 <= x < width and 0 <= y < height and neutral(x, y):
            seen[y * width + x] = 1
            queue.append((x, y))
    while queue:
        x, y = queue.popleft()
        out[x, y] = 0
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < width and 0 <= ny < height:
                idx = ny * width + nx
                if not seen[idx] and neutral(nx, ny):
                    seen[idx] = 1
                    queue.append((nx, ny))
    return alpha


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--clear-right-panel", action="store_true")
    args = parser.parse_args()
    with Image.open(args.source) as opened:
        image = opened.convert("RGBA")
    width, height = image.size
    seeds = [(x, 0) for x in range(width)] + [(x, height - 1) for x in range(width)]
    seeds += [(0, y) for y in range(height)] + [(width - 1, y) for y in range(height)]
    seeds.append((width // 2, height // 2))
    image.putalpha(remove_neutral_component(image, seeds))
    if args.clear_right_panel:
        px = image.load()
        for y in range(height):
            for x in range(int(width * 0.88), width):
                r, g, b, a = px[x, y]
                if a and r > 125 and g > 95 and b > 65 and r - b < 110:
                    px[x, y] = (r, g, b, 0)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    image.save(args.output, optimize=True)
    print(f"{args.output} {width}x{height} alpha={image.getchannel('A').getextrema()}")


if __name__ == "__main__":
    main()
