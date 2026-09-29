#!/usr/bin/env python3
from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def key_neutral_components(image: Image.Image) -> Image.Image:
    rgb = image.convert("RGB")
    pixels = rgb.load()
    alpha = image.getchannel("A")
    out = alpha.load()
    width, height = image.size
    visited = bytearray(width * height)
    queue = deque()

    def neutral(x: int, y: int) -> bool:
        r, g, b = pixels[x, y]
        return max(r, g, b) - min(r, g, b) <= 14

    seeds = [(x, 0) for x in range(width)]
    seeds += [(x, height - 1) for x in range(width)]
    seeds += [(0, y) for y in range(height)]
    seeds += [(width - 1, y) for y in range(height)]
    seeds += [(width // 6, height // 2)]
    for x, y in seeds:
        if neutral(x, y):
            idx = y * width + x
            if not visited[idx]:
                visited[idx] = 1
                queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        out[x, y] = 0
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < width and 0 <= ny < height:
                idx = ny * width + nx
                if not visited[idx] and neutral(nx, ny):
                    visited[idx] = 1
                    queue.append((nx, ny))
    image.putalpha(alpha)
    return image


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    with Image.open(args.source) as opened:
        image = key_neutral_components(opened.convert("RGBA"))
    if image.width != 2432 or image.height != 432:
        image = image.resize((2432, 432), Image.Resampling.LANCZOS)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    image.save(args.output, optimize=True)
    print(f"{args.output} {image.width}x{image.height} alpha={image.getchannel('A').getextrema()} bbox={image.getchannel('A').getbbox()}")


if __name__ == "__main__":
    main()
