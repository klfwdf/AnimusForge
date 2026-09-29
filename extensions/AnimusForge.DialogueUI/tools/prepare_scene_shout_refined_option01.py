#!/usr/bin/env python3
"""Generate two refined option-01 scene-shout preview sets.

This is a preview-only layer. It imports the deterministic base renderer and
adds parchment texture, engraved borders, corner filigree, studs, ribbons and
wax seals without touching runtime UI, PEN, or the existing five previews.
"""

from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

import prepare_scene_shout_previews as base


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "outputs"
SIZE = base.SIZE


VARIANTS = [
    {
        "key": "a",
        "name": "象牙羊皮纸 · 花饰金线",
        "seed": 1701,
        "accent": (169, 117, 50, 255),
        "accent2": (226, 181, 94, 255),
        "ornament": (184, 132, 54, 235),
        "wax": (139, 44, 42, 245),
        "ribbon": (111, 67, 32, 240),
        "paper": (238, 224, 186, 150),
        "ink": (57, 39, 24, 240),
    },
    {
        "key": "b",
        "name": "象牙羊皮纸 · 冷铜纹章",
        "seed": 1702,
        "accent": (99, 116, 105, 255),
        "accent2": (193, 159, 87, 255),
        "ornament": (104, 129, 119, 230),
        "wax": (60, 78, 104, 245),
        "ribbon": (57, 75, 70, 242),
        "paper": (229, 224, 199, 148),
        "ink": (42, 46, 41, 240),
    },
]


def rgba(color, alpha=None):
    if len(color) == 4:
        return color if alpha is None else (*color[:3], alpha)
    return (*color, 255 if alpha is None else alpha)


def refined_theme(variant):
    theme = dict(base.THEMES[0])
    theme.update(
        {
            "name": variant["name"],
            "accent": variant["accent"],
            "accent2": variant["accent2"],
            "highlight": (255, 226, 150, 245) if variant["key"] == "a" else (246, 220, 148, 245),
            "panel": (242, 229, 194, 240) if variant["key"] == "a" else (232, 228, 204, 240),
            "panel2": (221, 198, 148, 235) if variant["key"] == "a" else (207, 198, 161, 235),
            "ink": variant["ink"],
            "muted": (105, 77, 46, 255) if variant["key"] == "a" else (76, 89, 79, 255),
        }
    )
    return theme


def draw_filigree(draw, center, scale, color, flip_x=False, flip_y=False):
    """Small symmetric leaf and curl ornament used on parchment corners."""
    cx, cy = center
    sx = -1 if flip_x else 1
    sy = -1 if flip_y else 1
    points = [
        (cx, cy),
        (cx + sx * 28 * scale, cy + sy * 6 * scale),
        (cx + sx * 45 * scale, cy + sy * 30 * scale),
        (cx + sx * 60 * scale, cy + sy * 20 * scale),
    ]
    draw.line(points, fill=color, width=max(1, int(3 * scale)), joint="curve")
    arc_box = (
        cx + sx * 18 * scale,
        cy + sy * 22 * scale,
        cx + sx * 82 * scale,
        cy + sy * 82 * scale,
    )
    draw.arc(
        (min(arc_box[0], arc_box[2]), min(arc_box[1], arc_box[3]), max(arc_box[0], arc_box[2]), max(arc_box[1], arc_box[3])),
        180 if sx > 0 else 0,
        330 if sx > 0 else 150,
        fill=color,
        width=max(1, int(2 * scale)),
    )
    for offset in (0.0, 0.45, 0.9):
        x = cx + sx * (26 + offset * 32) * scale
        y = cy + sy * (13 + offset * 22) * scale
        draw.ellipse((x - 5 * scale, y - 3 * scale, x + 5 * scale, y + 3 * scale), outline=color, width=max(1, int(2 * scale)))


def draw_rosette(draw, center, radius, color, fill=None):
    cx, cy = center
    for n in range(8):
        angle = math.radians(n * 45)
        px = cx + math.cos(angle) * radius * 0.55
        py = cy + math.sin(angle) * radius * 0.55
        draw.ellipse((px - radius * 0.23, py - radius * 0.13, px + radius * 0.23, py + radius * 0.13), outline=color, width=2)
    draw.ellipse((cx - radius * 0.2, cy - radius * 0.2, cx + radius * 0.2, cy + radius * 0.2), fill=fill or rgba(color, 120), outline=color, width=2)


def draw_wax_seal(draw, center, radius, variant):
    cx, cy = center
    color = variant["wax"]
    draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=rgba(color, 215), outline=rgba(variant["accent2"], 220), width=3)
    draw.ellipse((cx - radius + 8, cy - radius + 8, cx + radius - 8, cy + radius - 8), outline=rgba((248, 219, 151, 210), 210), width=1)
    draw_rosette(draw, (cx, cy), radius * 0.62, rgba((245, 222, 161, 210), 225), fill=rgba(color, 130))


def add_paper_texture(image, box, variant, seed, density=1700):
    """Add restrained grain and fibers only inside the scroll body."""
    rng = random.Random(seed)
    overlay = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay, "RGBA")
    x0, y0, x1, y1 = box
    paper = variant["paper"]
    for _ in range(density):
        x = rng.randint(x0 + 12, x1 - 12)
        y = rng.randint(y0 + 12, y1 - 12)
        if rng.random() < 0.62:
            length = rng.randint(3, 20)
            draw.line((x, y, x + length, y + rng.choice((-1, 0, 1))), fill=rgba(paper, rng.randint(10, 28)), width=1)
        else:
            r = rng.choice((1, 1, 2))
            draw.ellipse((x - r, y - r, x + r, y + r), fill=rgba(paper, rng.randint(8, 24)))
    overlay = overlay.filter(ImageFilter.GaussianBlur(0.25))
    image.alpha_composite(overlay)


def add_scroll_details(image, variant, label_text):
    draw = ImageDraw.Draw(image, "RGBA")
    ornament = rgba(variant["ornament"], 205)
    gold = rgba(variant["accent2"], 225)

    # Decorative title ribbon and engraved corner lines on the wheel preview.
    # Cover the base renderer's header completely so the refinement reads as one finished plate.
    draw.rounded_rectangle((48, 34, 925, 124), radius=20, fill=rgba(variant["ribbon"], 238), outline=gold, width=3)
    draw.rounded_rectangle((61, 47, 912, 111), radius=14, outline=rgba((248, 220, 155, 220), 190), width=1)
    base.label(draw, (82, 72), "精修 01", refined_theme(variant), size=20, color=(252, 228, 173, 255), bold=True, anchor="lm")
    base.label(draw, (185, 72), label_text, refined_theme(variant), size=19, color=(246, 222, 170, 255), anchor="lm")
    base.label(draw, (185, 101), "T 框选 · 动作二级轮盘 · 羊皮纸装饰细节", refined_theme(variant), size=14, color=(235, 208, 157, 235), anchor="lm")
    draw_filigree(draw, (545, 135), 0.78, ornament)
    draw_filigree(draw, (1870, 135), 0.78, ornament, flip_x=True)
    draw_filigree(draw, (88, 858), 0.62, ornament, flip_y=True)
    draw_rosette(draw, (1840, 900), 31, gold, fill=rgba(variant["ribbon"], 175))
    draw_wax_seal(draw, (1788, 164), 42, variant)

    # Thin engraved divider between the primary and secondary wheel states.
    draw.line((978, 522, 1052, 522), fill=ornament, width=4)
    draw.ellipse((1000, 516, 1012, 528), fill=gold)


def add_session_details(image, variant):
    draw = ImageDraw.Draw(image, "RGBA")
    theme = refined_theme(variant)
    ornament = rgba(variant["ornament"], 210)
    gold = rgba(variant["accent2"], 225)
    scroll = (150, 708, 1770, 1060)

    add_paper_texture(image, scroll, variant, variant["seed"], density=2300)
    draw = ImageDraw.Draw(image, "RGBA")

    # Embossed botanical corners, geometric border marks, and brass studs.
    draw_filigree(draw, (198, 736), 1.0, ornament)
    draw_filigree(draw, (1722, 736), 1.0, ornament, flip_x=True)
    draw_filigree(draw, (198, 1030), 0.9, ornament, flip_y=True)
    draw_filigree(draw, (1722, 1030), 0.9, ornament, flip_x=True, flip_y=True)
    for x in (171, 1749):
        for y in (728, 1040):
            draw.ellipse((x - 9, y - 9, x + 9, y + 9), fill=rgba(variant["paper"], 220), outline=gold, width=2)
            draw.ellipse((x - 3, y - 3, x + 3, y + 3), fill=ornament)

    # Raised left ribbon tab and a wax seal tucked into the upper scroll edge.
    draw.polygon([(168, 734), (224, 734), (254, 763), (224, 792), (168, 792)], fill=rgba(variant["ribbon"], 220), outline=gold)
    base.label(draw, (199, 764), "喊话", theme, size=14, color=(248, 222, 165, 255), bold=True, anchor="mm")
    draw_wax_seal(draw, (1712, 746), 30, variant)

    # Engraved inset around the main text field, with a small quill mark.
    draw.rounded_rectangle((421, 765, 1394, 1019), radius=16, outline=rgba(variant["accent"], 145), width=2)
    draw.arc((1260, 945, 1350, 1005), 190, 350, fill=ornament, width=2)
    draw.line((1296, 986, 1340, 956), fill=ornament, width=3)
    draw.polygon([(1337, 956), (1349, 951), (1343, 964)], fill=ornament)

    # Medallion rims for History / Participants / Illustration, plus state tags.
    for cx in (1470, 1550, 1630):
        draw.ellipse((cx - 33, 792, cx + 33, 858), outline=gold, width=2)
        draw.arc((cx - 26, 799, cx + 26, 851), 30, 150, fill=ornament, width=2)
    draw.rounded_rectangle((1418, 1021, 1718, 1044), radius=8, fill=rgba(variant["ribbon"], 80), outline=rgba(variant["accent"], 120), width=1)
    base.label(draw, (1568, 1033), "可收起 · 展开后保留快照", theme, size=11, color=rgba(theme["ink"], 210), anchor="mm")

    # Small caption under the quick-choice pills, matching the reference's engraved UI notes.
    base.label(draw, (962, 632), "精修装饰预览 · 底部卷轴可收起为状态条", theme, size=14, color=(231, 207, 164, 235), anchor="mm")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for variant in VARIANTS:
        theme = refined_theme(variant)
        wheel = base.draw_wheel(1, theme).convert("RGBA")
        add_scroll_details(wheel, variant, variant["name"])
        session = base.draw_session(1, theme).convert("RGBA")
        add_session_details(session, variant)
        wheel_path = OUTPUT / f"shout-wheel-option-01-refined-{variant['key']}-preview.png"
        session_path = OUTPUT / f"shout-session-option-01-refined-{variant['key']}-preview.png"
        wheel.convert("RGB").save(wheel_path, optimize=True)
        session.convert("RGB").save(session_path, optimize=True)
        print(f"{wheel_path} {wheel.size}")
        print(f"{session_path} {session.size}")


if __name__ == "__main__":
    main()
