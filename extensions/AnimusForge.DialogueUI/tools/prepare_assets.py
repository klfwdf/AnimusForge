#!/usr/bin/env python3
"""Reproducible offline preparation of the generated AF DialogueUI parchment atlas.

Only the recorded relay output supplies material pixels. Pillow creates transparency,
nine-slice-safe borders, button states, and clearly labelled static layout previews.
No network access or image generation occurs in this script.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[1]
SPRITES = ROOT / "GUI" / "SpriteParts"
WORK = ROOT / "work"
SOURCE = ROOT / "outputs" / "image_c024cff2f9264fda8757b882d6bce9df.png"
MODEL = "gpt-image-2.5-sunburst福利"
RESOLUTIONS = [(1920, 1080), (1366, 768), (2560, 1440), (2560, 1080)]
REFERENCE = Path("C:/Users/29310/AppData/Local/Temp/codex-clipboard-af560617-642a-4c2b-9110-ea3a83f8cc43.png")
FONT = Path("C:/Windows/Fonts/msyh.ttc")
FONT_BOLD = Path("C:/Windows/Fonts/msyhbd.ttc")
RESAMPLE = Image.Resampling.LANCZOS


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def polygon_mask(size, points):
    scale = 4
    mask = Image.new("L", (size[0] * scale, size[1] * scale))
    ImageDraw.Draw(mask).polygon([(round(x * scale), round(y * scale)) for x, y in points], fill=255)
    return mask.resize(size, RESAMPLE)


def isolate(image, box, points=None, ellipse=None):
    result = image.crop(box).convert("RGBA")
    if points:
        mask = polygon_mask(result.size, [(x - box[0], y - box[1]) for x, y in points])
    elif ellipse:
        scale = 4
        mask = Image.new("L", (result.width * scale, result.height * scale))
        ImageDraw.Draw(mask).ellipse(tuple(round(v * scale) for v in ellipse), fill=255)
        mask = mask.resize(result.size, RESAMPLE)
    else:
        mask = Image.new("L", result.size, 255)
    result.putalpha(mask)
    return result


def nine_slice(source, size, source_border, output_border=None):
    """Keep corners undistorted for runtime nine-slicing; normalize oversized atlas corners."""
    border = source_border if output_border is None else output_border
    sw, sh = source.size
    w, h = size
    if min(sw, sh) < source_border * 2 or min(w, h) < border * 2:
        raise ValueError("Nine-slice target cannot fit its corners")
    sx = [0, source_border, sw - source_border, sw]
    sy = [0, source_border, sh - source_border, sh]
    dx = [0, border, w - border, w]
    dy = [0, border, h - border, h]
    out = Image.new("RGBA", (w, h))
    for row in range(3):
        for col in range(3):
            tile = source.crop((sx[col], sy[row], sx[col + 1], sy[row + 1]))
            tile = tile.resize((dx[col + 1] - dx[col], dy[row + 1] - dy[row]), RESAMPLE)
            out.alpha_composite(tile, (dx[col], dy[row]))
    return out


def save_sprite(name, image, descriptions, description, border=None):
    path = SPRITES / (name + ".png")
    image.save(path, optimize=True)
    alpha = image.getchannel("A")
    descriptions.append({"name": name, "path": str(path.relative_to(ROOT)).replace("\\", "/"),
                         "width": image.width, "height": image.height,
                         "nine_slice_border": border, "description": description,
                         "alpha_extrema": list(alpha.getextrema()), "sha256": sha256(path)})


def make_materials(source):
    entries = []
    # Hand traced against the recorded image, avoiding its unwanted brown backdrop.
    scroll_points = [(56, 14), (62, 20), (70, 28), (66, 35), (84, 41), (94, 48),
                     (95, 55), (133, 56), (166, 52), (211, 57), (256, 53), (309, 57),
                     (360, 56), (407, 59), (470, 53), (534, 59), (584, 55), (635, 56),
                     (690, 58), (747, 54), (799, 56), (854, 56), (922, 58), (979, 54),
                     (1047, 57), (1106, 53), (1167, 56), (1224, 53), (1282, 55),
                     (1356, 54), (1416, 56), (1438, 53), (1440, 45), (1457, 38),
                     (1470, 34), (1470, 24), (1478, 14), (1489, 17), (1496, 26),
                     (1491, 34), (1500, 38), (1516, 45), (1518, 50), (1510, 55),
                     (1505, 59), (1507, 105), (1505, 160), (1507, 214), (1506, 269),
                     (1506, 308), (1517, 313), (1516, 320), (1507, 326), (1495, 332),
                     (1492, 342), (1489, 349), (1482, 356), (1473, 350), (1468, 342),
                     (1469, 334), (1458, 331), (1444, 325), (1440, 320), (1404, 321),
                     (1341, 317), (1282, 320), (1231, 315), (1168, 320), (1110, 316),
                     (1055, 321), (997, 316), (938, 320), (882, 315), (825, 320),
                     (766, 316), (703, 320), (640, 315), (579, 321), (519, 316),
                     (465, 320), (411, 315), (361, 320), (310, 316), (254, 320),
                     (197, 315), (146, 319), (95, 320), (87, 326), (75, 331),
                     (64, 334), (62, 345), (57, 356), (48, 351), (43, 341),
                     (45, 333), (31, 329), (18, 323), (17, 315), (24, 310),
                     (26, 266), (26, 222), (25, 179), (26, 137), (24, 95), (25, 58),
                     (19, 54), (18, 46), (32, 40), (46, 36), (47, 31), (43, 27),
                     (45, 22), (51, 15)]
    scroll = isolate(source, (0, 0, 1536, 368), scroll_points)
    scroll.save(WORK / "isolated-scroll.png")
    left = scroll.crop((0, 0, 320, 368)).resize((160, 320), RESAMPLE)
    # A broad center retains actual paper grain even on 21:9 monitors. A 64px
    # strip produced visible horizontal smearing during the first visual QA.
    body = scroll.crop((320, 0, 1216, 368)).resize((1024, 320), RESAMPLE)
    right = scroll.crop((1216, 0, 1536, 368)).resize((160, 320), RESAMPLE)
    # Match seams using the adjacent center sample. Work on RGBA to retain torn edges.
    for side, is_left in [(left, True), (right, False)]:
        px = side.load()
        bp = body.load()
        for y in range(320):
            for offset in range(24):
                x = side.width - 1 - offset if is_left else offset
                blend = (1 - offset / 24) * 0.92
                target = bp[0 if is_left else body.width - 1, y]
                original = px[x, y]
                px[x, y] = tuple(round(original[c] * (1 - blend) + target[c] * blend) for c in range(4))
    save_sprite("afdui_scroll_left", left, entries, "Left roll and antique-gold foliage; keep width fixed.")
    save_sprite("afdui_scroll_body", body, entries, "Blank vellum center; stretch horizontally only.")
    save_sprite("afdui_scroll_right", right, entries, "Right roll and antique-gold foliage; keep width fixed.")

    panel_points = [(33, 376), (68, 375), (109, 377), (151, 374), (190, 376),
                    (232, 374), (277, 376), (321, 374), (365, 376), (412, 375),
                    (457, 376), (500, 374), (542, 376), (589, 374), (625, 376),
                    (628, 386), (627, 429), (628, 474), (625, 521), (628, 566),
                    (625, 612), (627, 657), (625, 706), (628, 748), (625, 795),
                    (628, 843), (625, 889), (627, 933), (626, 968), (586, 970),
                    (539, 968), (490, 970), (445, 968), (400, 970), (354, 968),
                    (311, 970), (264, 968), (219, 970), (169, 968), (125, 970),
                    (81, 968), (31, 969), (28, 960), (28, 916), (30, 871),
                    (28, 823), (30, 778), (28, 734), (30, 686), (28, 641),
                    (30, 598), (28, 551), (29, 506), (28, 460), (30, 420), (28, 388)]
    panel = isolate(source, (24, 372, 632, 974), panel_points)
    panel.save(WORK / "isolated-parchment-panel.png")
    panel = nine_slice(panel, (512, 512), 150, 40)
    save_sprite("afdui_parchment_panel", panel, entries,
                "Light vellum with all corner illumination normalized inside 40px margins.", 40)
    input_panel = isolate(source, (672, 375, 1500, 635))
    input_panel = nine_slice(input_panel, (512, 192), 24, 16)
    save_sprite("afdui_input_panel", input_panel, entries,
                "Blank dark umber inset; bronze rim inside 16px margins.", 16)
    button = isolate(source, (708, 663, 1017, 968), ellipse=(5, 4, 305, 301)).resize((64, 64), RESAMPLE)
    save_sprite("afdui_button_normal", button, entries, "Blank bronze button; control draws its own icon.")
    save_sprite("afdui_button_hover", ImageEnhance.Brightness(button).enhance(1.20), entries,
                "Warm highlighted bronze button state.")
    save_sprite("afdui_button_pressed", ImageEnhance.Brightness(button).enhance(0.75), entries,
                "Dark pressed bronze button state.")
    seal_points = [(1252, 667), (1274, 666), (1299, 675), (1323, 683), (1351, 692),
                   (1371, 708), (1383, 731), (1391, 754), (1408, 784), (1414, 811),
                   (1411, 836), (1399, 859), (1392, 886), (1376, 911), (1356, 927),
                   (1328, 938), (1302, 950), (1272, 956), (1246, 949), (1214, 943),
                   (1183, 930), (1159, 913), (1146, 888), (1137, 860), (1126, 837),
                   (1120, 810), (1124, 784), (1138, 761), (1149, 731), (1170, 712),
                   (1195, 698), (1224, 689)]
    seal = isolate(source, (1115, 660, 1420, 964), seal_points).resize((96, 96), RESAMPLE)
    save_sprite("afdui_wax_seal", seal, entries, "Blank muted red wax decoration; no fictitious crest.")
    return entries


def font(size, bold=False):
    path = FONT_BOLD if bold else FONT
    if not path.exists():
        raise FileNotFoundError("Chinese preview font missing; pass a Windows font path in script configuration")
    return ImageFont.truetype(str(path), max(10, round(size)))


def text(draw, xy, value, size=22, fill="#eadac0", bold=False):
    draw.text(xy, value, font=font(size, bold), fill=fill, stroke_width=0)


def load_sprite(name):
    return Image.open(SPRITES / (name + ".png")).convert("RGBA")


def draw_panel(canvas, rect, name="afdui_parchment_panel", border=40):
    x, y, w, h = map(round, rect)
    sprite = load_sprite(name)
    canvas.alpha_composite(nine_slice(sprite, (w, h), border), (x, y))


def draw_scroll(canvas, rect):
    x, y, w, h = map(round, rect)
    side = round(160 * h / 320)
    left = load_sprite("afdui_scroll_left").resize((side, h), RESAMPLE)
    body = load_sprite("afdui_scroll_body").resize((w - side * 2, h), RESAMPLE)
    right = load_sprite("afdui_scroll_right").resize((side, h), RESAMPLE)
    canvas.alpha_composite(left, (x, y))
    canvas.alpha_composite(body, (x + side, y))
    canvas.alpha_composite(right, (x + w - side, y))


def button(canvas, center, label, scale=1, icon=None):
    x, y = center
    diameter = round(58 * scale)
    sprite = load_sprite("afdui_button_normal").resize((diameter, diameter), RESAMPLE)
    canvas.alpha_composite(sprite, (round(x - diameter / 2), round(y - diameter / 2)))
    draw = ImageDraw.Draw(canvas)
    text(draw, (x - 11 * scale, y - 17 * scale), icon or label[0], 23 * scale, "#e7cf91", True)
    box = draw.textbbox((0, 0), label, font=font(17 * scale))
    text(draw, (x - (box[2] - box[0]) / 2, y + 32 * scale), label, 17 * scale, "#5c3c20")


def background(size):
    w, h = size
    cached = WORK / "reference-scene-crop.png"
    if REFERENCE.exists():
        # Crop away all UI; this is only a reference scene for static previews.
        Image.open(REFERENCE).convert("RGB").crop((45, 125, 1555, 600)).save(cached)
    if cached.exists():
        source = Image.open(cached).convert("RGB")
        image = ImageOps.fit(source, size, method=RESAMPLE, centering=(0.5, 0.50)).convert("RGBA")
        image = ImageEnhance.Brightness(image).enhance(0.58)
    else:
        image = Image.new("RGBA", size, (35, 30, 24, 255))
    # Reserve scene visibility above the preview UI without baking a scene into sprites.
    shade = Image.new("RGBA", size)
    sd = ImageDraw.Draw(shade)
    for y in range(h):
        alpha = int(78 * max(0, (y / h - .48) / .52))
        sd.line((0, y, w, y), fill=(6, 4, 2, alpha))
    image.alpha_composite(shade)
    return image


def preview_shout(size, history=False):
    w, h = size
    s = h / 1080
    canvas = background(size)
    draw = ImageDraw.Draw(canvas)
    text(draw, (36 * s, 28 * s), "场景喊话 · 羊皮卷输入", 25 * s, "#ead4a3", True)
    text(draw, (36 * s, 64 * s), "静态布局预览｜参考场景背景｜文字与按钮由真实控件绘制", 16 * s)
    # Match the layout bands; the wide mode expands the quiet center only.
    margin = max(20, round(w * .035))
    scroll_y = h - round((530 if history else 318) * s)
    scroll_h = round((504 if history else 292) * s)
    draw_scroll(canvas, (margin, scroll_y, w - margin * 2, scroll_h))
    left = margin + round((270 if history else 180) * s)
    right = w - margin - round(80 * s)
    top = scroll_y + round((96 if history else 63) * s)
    draw = ImageDraw.Draw(canvas)
    text(draw, (left, top), "对瓦伦德说话", 23 * s, "#553719", True)
    text(draw, (left + 200 * s, top + 4 * s), "查看百科 ↗", 17 * s, "#765525")
    text(draw, (right - 305 * s, top + 5 * s), "给予／展示：通过 Y 菜单", 17 * s, "#765525")
    inner_y = top + round(44 * s)
    inner_right = right - round(95 * s)
    inner_h = round((255 if history else 100) * s)
    draw_panel(canvas, (left, inner_y, inner_right - left, inner_h), "afdui_input_panel", 16)
    draw = ImageDraw.Draw(canvas)
    if history:
        text(draw, (left + 23 * s, inner_y + 20 * s), "场景记录 · 当前目标", 20 * s, "#e6c58b", True)
        text(draw, (left + 23 * s, inner_y + 58 * s), "你：这座城最近是否平安？", 19 * s)
        text(draw, (left + 23 * s, inner_y + 96 * s), "瓦伦德：大道上的商队已经恢复通行，守卫仍在巡逻。", 19 * s)
        text(draw, (left + 23 * s, inner_y + 166 * s), "[关闭记录后继续编辑保留的草稿]", 18 * s, "#bbaa89")
    else:
        text(draw, (left + 22 * s, inner_y + 18 * s), "你们需要更多粮食，还是需要我护送商队？", 22 * s)
        text(draw, (left + 22 * s, inner_y + 53 * s), "│", 22 * s)
    button(canvas, (right - 31 * s, inner_y + 40 * s), "返回" if history else "记录", s, "卷")
    draw = ImageDraw.Draw(canvas)
    text(draw, (left, inner_y + inner_h + 15 * s), "已选物资：谷物 × 10    ·    物资详情随现有 Y 菜单保持", 16 * s, "#664827")
    text(draw, (inner_right - 460 * s, inner_y + inner_h + 15 * s), "Enter 发送  ·  Shift+Enter 换行  ·  Esc 取消", 16 * s, "#664827")
    return canvas


def preview_conversation(size, ai=True):
    w, h = size
    s = h / 1080
    canvas = background(size)
    draw = ImageDraw.Draw(canvas)
    text(draw, (36 * s, 28 * s), "场景一对一对话 · 羊皮纸三分区", 25 * s, "#ead4a3", True)
    text(draw, (36 * s, 64 * s), "静态布局预览｜左侧为原生肖像控件占位｜普通选项与 AI 输入可切换", 16 * s)
    margin = max(20, round(w * .025))
    gap = round(12 * s)
    panel_h = round(355 * s)
    panel_y = h - panel_h - round(28 * s)
    content_w = w - 2 * margin
    left_w = round(294 * s)
    right_w = round(488 * s)
    center_w = content_w - left_w - right_w - 2 * gap
    xs = [margin, margin + left_w + gap, w - margin - right_w]
    for x, pw in zip(xs, [left_w, center_w, right_w]):
        draw_panel(canvas, (x, panel_y, pw, panel_h))
    # Clearly-labelled native portrait placeholder, never a generated NPC identity.
    draw = ImageDraw.Draw(canvas)
    px = xs[0] + 38 * s
    py = panel_y + 36 * s
    draw.rounded_rectangle((px, py, px + 218 * s, py + 220 * s), radius=6 * s,
                           fill="#b39a74", outline="#8b6b38", width=max(1, round(2 * s)))
    draw.ellipse((px + 78 * s, py + 26 * s, px + 140 * s, py + 96 * s), fill="#76674f")
    draw.pieslice((px + 27 * s, py + 83 * s, px + 191 * s, py + 247 * s), 180, 360, fill="#76674f")
    text(draw, (px + 32 * s, py + 179 * s), "原生人物肖像", 22 * s, "#efe1c2")
    text(draw, (xs[0] + 52 * s, panel_y + 272 * s), "瓦伦德 · 城镇守卫", 20 * s, "#553719", True)
    text(draw, (xs[0] + 71 * s, panel_y + 304 * s), "姓名与纹章来自游戏", 15 * s, "#785630")
    cx = xs[1] + round(34 * s)
    cy = panel_y + round(50 * s)
    text(draw, (cx, cy), "瓦伦德", 24 * s, "#553719", True)
    # Wrap CJK sample by measured width, as the runtime TextWidget would.
    sample = "若你想拜访领主，请先向门口的卫兵出示通行凭证。城门每天日落时关闭，最好不要耽误太久。"
    max_w = center_w - 68 * s
    lines = []
    line = ""
    for char in sample:
        if draw.textlength(line + char, font=font(24 * s)) > max_w and line:
            lines.append(line)
            line = ""
        line += char
    if line:
        lines.append(line)
    for n, value in enumerate(lines):
        text(draw, (cx, cy + (55 + n * 44) * s), value, 24 * s, "#4c331d")
    text(draw, (cx, panel_y + panel_h - 56 * s), "长回复保持滚动 · 流式内容沿用原链路", 15 * s, "#896437")
    for index, label in enumerate(["记录", "给予", "绘图", "更多"]):
        button(canvas, (xs[1] + center_w * (index + 1) / 5, panel_y - 17 * s), label, s)
    # Right-hand region is mutually exclusive AI input / full native option list.
    rx = xs[2] + round(26 * s)
    ry = panel_y + round(36 * s)
    rw = right_w - round(52 * s)
    text(draw, (rx + 9 * s, ry), "AI 自由输入" if ai else "选择回应", 23 * s, "#553719", True)
    text(draw, (rx + rw - 128 * s, ry + 6 * s), "切回原版" if ai else "AI 输入", 17 * s, "#8b5827")
    if ai:
        draw_panel(canvas, (rx, ry + 47 * s, rw, 148 * s), "afdui_input_panel", 16)
        draw = ImageDraw.Draw(canvas)
        text(draw, (rx + 18 * s, ry + 66 * s), "谢谢，我会在日落前回来。", 21 * s)
        text(draw, (rx + 18 * s, ry + 105 * s), "│", 22 * s)
        button(canvas, (rx + rw - 47 * s, ry + 244 * s), "发送", s, "封")
        text(draw, (rx + 5 * s, ry + 233 * s), "Enter 发送  ·  Esc 返回", 16 * s, "#74522c")
    else:
        for n, value in enumerate(["我想打听一些消息。", "请告诉我领主在何处。", "我明白了，再见。"]):
            yy = ry + (54 + n * 65) * s
            draw_panel(canvas, (rx, yy, rw, 53 * s), "afdui_input_panel", 16)
            draw = ImageDraw.Draw(canvas)
            text(draw, (rx + 16 * s, yy + 12 * s), value, 19 * s)
        text(draw, (rx + 4 * s, ry + 277 * s), "原生全部选项 · 禁用原因 / 说服 / 继续", 15 * s, "#74522c")
    return canvas


def contact_sheet(entries):
    canvas = Image.new("RGBA", (1120, 780), "#292722")
    draw = ImageDraw.Draw(canvas)
    text(draw, (28, 20), "AnimusForge.DialogueUI · 透明运行时素材", 24, "#ead4a3", True)
    draw_scroll(canvas, (20, 72, 1080, 215))
    draw_panel(canvas, (24, 335, 370, 390))
    draw_panel(canvas, (429, 347, 659, 224), "afdui_input_panel", 16)
    draw = ImageDraw.Draw(canvas)
    text(draw, (43, 303), "羊皮纸面板 · 九切边距 40 px", 19)
    text(draw, (449, 605), "空白按钮三态与蜡封（图标由控件绘制）", 18)
    for n, name in enumerate(["afdui_button_normal", "afdui_button_hover", "afdui_button_pressed", "afdui_wax_seal"]):
        sprite = load_sprite(name)
        canvas.alpha_composite(sprite, (470 + n * 136, 661))
    return canvas


def validate(entries):
    for entry in entries:
        path = ROOT / entry["path"]
        with Image.open(path) as image:
            assert image.mode == "RGBA", entry["name"]
            assert image.size == (entry["width"], entry["height"])
            assert image.getchannel("A").getextrema()[1] == 255
            if entry["name"] != "afdui_input_panel":
                assert image.getchannel("A").getextrema()[0] == 0, entry["name"]
            assert entry["sha256"] == sha256(path)
    for size in RESOLUTIONS:
        for mode in ["shout", "conversation"]:
            path = WORK / ("preview-%s-%dx%d.png" % (mode, *size))
            with Image.open(path) as image:
                assert image.size == size
    return {"sprite_count": len(entries), "resolutions": RESOLUTIONS,
            "checks": ["RGBA output", "dimensions", "transparent sprite boundaries", "SHA-256", "preview canvas sizes"],
            "not_verified": ["Bannerlord rendering", "runtime widget positions", "focus/IME", "game UI scale", "native portrait rendering"]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=SOURCE)
    args = parser.parse_args()
    SPRITES.mkdir(parents=True, exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    with Image.open(args.source) as raw:
        if raw.size != (1536, 1024):
            raise ValueError("Masks are verified only for the recorded 1536x1024 atlas")
        entries = make_materials(raw.convert("RGBA"))
    for size in RESOLUTIONS:
        preview_shout(size).convert("RGB").save(WORK / ("preview-shout-%dx%d.png" % size), optimize=True)
        preview_conversation(size).convert("RGB").save(WORK / ("preview-conversation-%dx%d.png" % size), optimize=True)
    preview_shout((1920, 1080), history=True).convert("RGB").save(WORK / "preview-shout-history-1920x1080.png", optimize=True)
    preview_conversation((1920, 1080), ai=False).convert("RGB").save(WORK / "preview-conversation-native-1920x1080.png", optimize=True)
    contact_sheet(entries).convert("RGB").save(WORK / "asset-contact-sheet.png", optimize=True)
    report = validate(entries)
    manifest = {"module": "AnimusForge.DialogueUI", "model": MODEL,
                "source": str(args.source.relative_to(ROOT)).replace("\\", "/"),
                "source_sha256": sha256(args.source), "source_dimensions": [1536, 1024],
                "prompt": "work/parchment-atlas.prompt.txt", "generation_requests": 1,
                "processing": "Hand-traced antialiased alpha masks; normalize corner flourishes before runtime nine-slicing.",
                "preview_warning": "Static design previews, not Bannerlord screenshots. Scene crop comes from user reference; native portrait is a labelled placeholder.",
                "runtime_asset_directory": "GUI/SpriteParts", "sprites": entries, "validation": report}
    (WORK / "assets-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
