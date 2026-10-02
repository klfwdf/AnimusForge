"""Offline local ornament repair. No runtime image processing or new art.

Uses the intact left-hand ornament, reflected to the existing right edge.
Pixels outside ROI and the complete alpha channel stay byte-for-byte unchanged.
"""
import argparse
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps

INK_ROI = (1390, 980, 1685, 1068)
VERTICAL_ROI = (1588, 790, 1645, 1025)
ROI = (1390, 790, 1685, 1068)
REFLECTION_SUM = 1758


def repair(source):
    if source.mode != 'RGBA' or source.size != (1730, 1100):
        raise ValueError('Expected the original 1730x1100 RGBA parchment')
    left, top, right, bottom = INK_ROI
    patch = ImageOps.mirror(source.crop((REFLECTION_SUM-right, top,
                                         REFLECTION_SUM-left, bottom))).convert('RGB')
    target = source.crop(INK_ROI).convert('RGB')
    # Darken-only keeps the existing paper highlights and right-side ink;
    # feathered support joins the missing bottom edge without a square seam.
    mask = Image.new('L', patch.size)
    draw = ImageDraw.Draw(mask)
    draw.rectangle((16, 22, 278, 72), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(5))
    merged = Image.composite(ImageChops.darker(target, patch), target, mask)
    result = source.copy()
    result.paste(merged, (left, top))
    result.putalpha(source.getchannel('A'))
    # Continue the missing inner rule and foliage from the intact left border.
    left, top, right, bottom = VERTICAL_ROI
    patch = ImageOps.mirror(source.crop((1722-right, top, 1722-left, bottom))).convert('RGB')
    target = result.crop(VERTICAL_ROI).convert('RGB')
    mask = Image.new('L', patch.size)
    ImageDraw.Draw(mask).rectangle((10, 18, 48, 222), fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(4))
    merged = Image.composite(ImageChops.darker(target, patch), target, mask)
    result.paste(merged, (left, top))
    result.putalpha(source.getchannel('A'))
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    with Image.open(args.source) as source:
        repair(source).save(args.output)
