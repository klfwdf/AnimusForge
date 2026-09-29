#!/usr/bin/env python3
from __future__ import annotations

import argparse
from pathlib import Path

from collections import deque
from PIL import Image
from prepare_controls import alpha_mask


def remove_neutral_border_key(image: Image.Image, mask: Image.Image) -> Image.Image:
    rgb = image.convert("RGB")
    pixels = rgb.load()
    alpha = mask.load()
    width, height = rgb.size
    visited = bytearray(width * height)
    queue = deque()

    def is_background(x: int, y: int) -> bool:
        if alpha[x, y] < 128:
            return False
        r, g, b = pixels[x, y]
        return 80 <= r <= 245 and max(r, g, b) - min(r, g, b) <= 10

    for x in range(width):
        for y in (0, height - 1):
            if is_background(x, y):
                index = y * width + x
                if not visited[index]:
                    visited[index] = 1
                    queue.append((x, y))
    for y in range(height):
        for x in (0, width - 1):
            if is_background(x, y):
                index = y * width + x
                if not visited[index]:
                    visited[index] = 1
                    queue.append((x, y))

    while queue:
        x, y = queue.popleft()
        alpha[x, y] = 0
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < width and 0 <= ny < height:
                index = ny * width + nx
                if not visited[index] and is_background(nx, ny):
                    visited[index] = 1
                    queue.append((nx, ny))
    for y in range(height):
        for x in range(width):
            r, g, b = pixels[x, y]
            if r > 110 and b > 110 and r - g > 40 and b - g > 40:
                alpha[x, y] = 0
    return mask


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    with Image.open(args.source) as opened:
        image = opened.convert("RGBA")
        mask = remove_neutral_border_key(image, alpha_mask(image))
        image.putalpha(mask)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    image.save(args.output, optimize=True)
    print(f"{args.output} {image.width}x{image.height}")


if __name__ == "__main__":
    main()
