#!/usr/bin/env python3
from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "outputs"
SIZE = (1920, 1080)

FONT_SERIF = Path(r"C:\Windows\Fonts\NotoSerifSC-VF.ttf")
FONT_SANS = Path(r"C:\Windows\Fonts\Noto Sans SC (TrueType).otf")
FONT_SANS_BOLD = Path(r"C:\Windows\Fonts\Noto Sans SC Bold (TrueType).otf")

THEMES = [
    {
        "name": "象牙羊皮纸与古铜",
        "bg0": (31, 25, 22), "bg1": (73, 54, 39),
        "panel": (239, 226, 190, 238), "panel2": (222, 203, 157, 235),
        "ink": (52, 36, 24, 255), "muted": (108, 78, 45, 255),
        "accent": (180, 133, 62, 255), "accent2": (222, 177, 91, 255),
        "highlight": (255, 224, 142, 245), "danger": (164, 72, 57, 245),
    },
    {
        "name": "胡桃木与浅色羊皮纸",
        "bg0": (24, 18, 15), "bg1": (62, 40, 27),
        "panel": (235, 216, 174, 238), "panel2": (215, 188, 140, 235),
        "ink": (47, 30, 20, 255), "muted": (105, 69, 40, 255),
        "accent": (141, 92, 48, 255), "accent2": (200, 143, 78, 255),
        "highlight": (248, 211, 125, 245), "danger": (157, 67, 49, 245),
    },
    {
        "name": "酒红皮革与古铜",
        "bg0": (39, 17, 22), "bg1": (91, 40, 43),
        "panel": (226, 196, 176, 238), "panel2": (195, 146, 126, 232),
        "ink": (56, 25, 27, 255), "muted": (116, 60, 55, 255),
        "accent": (176, 105, 78, 255), "accent2": (218, 154, 105, 255),
        "highlight": (250, 196, 139, 245), "danger": (154, 55, 62, 245),
    },
    {
        "name": "卷轴边饰与古铜纹饰",
        "bg0": (23, 31, 29), "bg1": (52, 74, 66),
        "panel": (240, 226, 190, 238), "panel2": (220, 201, 157, 235),
        "ink": (43, 48, 39, 255), "muted": (91, 95, 71, 255),
        "accent": (162, 126, 68, 255), "accent2": (218, 175, 92, 255),
        "highlight": (245, 222, 140, 245), "danger": (157, 76, 60, 245),
    },
    {
        "name": "深木纹与柔和铜饰",
        "bg0": (13, 15, 18), "bg1": (43, 38, 34),
        "panel": (229, 216, 185, 238), "panel2": (205, 187, 149, 235),
        "ink": (37, 29, 24, 255), "muted": (91, 71, 53, 255),
        "accent": (164, 131, 78, 255), "accent2": (213, 176, 105, 255),
        "highlight": (245, 214, 132, 245), "danger": (153, 64, 56, 245),
    },
]


def font(path: Path, size: int):
    return ImageFont.truetype(str(path), size=size)


def rgba(color, alpha=None):
    if len(color) == 4:
        return color if alpha is None else (*color[:3], alpha)
    return (*color, 255 if alpha is None else alpha)


def lerp(a, b, t):
    return int(a + (b - a) * t)


def scene_background(theme, variant):
    image = Image.new("RGBA", SIZE, rgba(theme["bg0"]))
    px = image.load()
    for y in range(SIZE[1]):
        t = y / (SIZE[1] - 1)
        row = tuple(lerp(theme["bg0"][i], theme["bg1"][i], t) for i in range(3))
        for x in range(SIZE[0]):
            edge = min(x, SIZE[0] - 1 - x) / 320.0
            shade = max(0.18, min(1.0, edge))
            px[x, y] = (int(row[0] * shade), int(row[1] * shade), int(row[2] * shade), 255)
    draw = ImageDraw.Draw(image, "RGBA")
    # Tavern / street architecture silhouettes.
    draw.rectangle((0, 500, 1920, 1080), fill=(18, 15, 14, 92))
    for x in range(80, 1920, 250):
        draw.rectangle((x, 160 + (x % 3) * 28, x + 54, 720), fill=(12, 12, 13, 88))
        draw.line((x + 27, 160, x + 27, 720), fill=(208, 171, 104, 34), width=3)
    for x in range(0, 1920, 160):
        draw.polygon([(x, 620), (x + 82, 620), (x + 138, 1080), (x - 54, 1080)], fill=(10, 10, 10, 48))
    # Distant banners and lamps.
    for x, y in [(170, 250), (1630, 230), (1020, 180)]:
        draw.line((x, y, x, y + 210), fill=(220, 183, 113, 70), width=5)
        draw.polygon([(x, y + 12), (x + 88, y + 36), (x + 76, y + 126), (x, y + 106)], fill=(112, 63, 53, 92))
        draw.ellipse((x - 22, y - 28, x + 22, y + 16), fill=(247, 204, 115, 92))
    # NPC silhouettes and the player anchor.
    silhouettes = [(280, 625, 0.9), (530, 708, 0.75), (1470, 650, 1.05), (1730, 720, 0.72), (1010, 760, 1.2)]
    for index, (x, y, scale) in enumerate(silhouettes):
        head = 22 * scale
        body = 62 * scale
        draw.ellipse((x - head, y - 122 * scale - head, x + head, y - 122 * scale + head), fill=(8, 8, 9, 170))
        draw.polygon([(x - body, y - 92 * scale), (x + body, y - 92 * scale), (x + body * 1.22, y + 92 * scale), (x - body * 1.22, y + 92 * scale)], fill=(7, 8, 10, 172))
        if index in (0, 2, 4):
            draw.line((x - 34 * scale, y - 64 * scale, x + 34 * scale, y - 64 * scale), fill=rgba(theme["accent2"], 105), width=max(2, int(4 * scale)))
    # Subtle vignette.
    vignette = Image.new("L", SIZE, 0)
    vp = vignette.load()
    cx, cy = SIZE[0] / 2, SIZE[1] / 2
    for y in range(SIZE[1]):
        for x in range(SIZE[0]):
            d = math.sqrt(((x - cx) / cx) ** 2 + ((y - cy) / cy) ** 2)
            vp[x, y] = int(min(175, max(0, (d - 0.30) * 150)))
    shade = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    shade.putalpha(vignette)
    image.alpha_composite(shade)
    return image


def panel(draw, box, theme, radius=18, fill=None, outline=None, width=3):
    fill = fill or theme["panel"]
    outline = outline or rgba(theme["accent"], 235)
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)
    x0, y0, x1, y1 = box
    draw.rounded_rectangle((x0 + 10, y0 + 10, x1 - 10, y1 - 10), radius=max(3, radius - 8), outline=rgba(theme["accent2"], 90), width=1)


def label(draw, xy, text, theme, size=24, color=None, bold=False, anchor=None):
    f = font(FONT_SANS_BOLD if bold else FONT_SERIF, size)
    draw.text(xy, text, font=f, fill=color or theme["ink"], anchor=anchor)


def button(draw, box, text, theme, selected=False, disabled=False, size=24):
    x0, y0, x1, y1 = box
    fill = rgba(theme["highlight"], 235 if selected else 168)
    if disabled:
        fill = (110, 100, 83, 105)
    outline = rgba(theme["accent2"], 255 if selected else 210)
    draw.rounded_rectangle(box, radius=12, fill=fill, outline=outline, width=2)
    label(draw, ((x0 + x1) // 2, (y0 + y1) // 2), text, theme, size=size, color=theme["ink"] if not disabled else (190, 177, 153, 220), bold=selected, anchor="mm")


def checkbox(draw, x, y, theme, checked=True, locked=False):
    box = (x, y, x + 28, y + 28)
    fill = rgba(theme["highlight"], 220 if checked else 80)
    draw.rounded_rectangle(box, radius=6, fill=fill, outline=rgba(theme["accent"], 240), width=2)
    if checked:
        draw.line((x + 6, y + 15, x + 12, y + 21, x + 23, y + 7), fill=theme["ink"], width=3)
    if locked:
        draw.arc((x + 8, y - 8, x + 20, y + 10), 180, 360, fill=theme["ink"], width=2)
        draw.rectangle((x + 6, y + 7, x + 22, y + 21), fill=rgba(theme["accent"], 230))


def npc_marker(draw, x, y, theme, primary=False, cancelled=False, hovered=False):
    radius = 30 if primary else 23
    color = theme["highlight"] if primary else theme["accent2"]
    if cancelled:
        color = theme["danger"]
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(18, 14, 12, 190), outline=rgba(color, 245), width=4 if hovered or primary else 2)
    draw.ellipse((x - 7, y - 11, x + 7, y + 3), fill=rgba(color, 220))
    draw.arc((x - 13, y + 1, x + 13, y + 21), 180, 360, fill=rgba(color, 220), width=3)
    if cancelled:
        draw.line((x - radius + 7, y - radius + 7, x + radius - 7, y + radius - 7), fill=rgba(theme["danger"], 255), width=4)
        draw.line((x + radius - 7, y - radius + 7, x - radius + 7, y + radius - 7), fill=rgba(theme["danger"], 255), width=4)


def multiline_label(draw, xy, text, theme, size=24, color=None, bold=False, anchor=None, spacing=2):
    f = font(FONT_SANS_BOLD if bold else FONT_SERIF, size)
    draw.multiline_text(xy, text, font=f, fill=color or theme["ink"], anchor=anchor, spacing=spacing, align="center")


def radial_menu(draw, center, outer, items, theme, selected_index=0, center_lines=(), label_size=20):
    """Draw a circular menu so the secondary actions remain inside the wheel flow."""
    cx, cy = center
    draw.ellipse((cx - outer, cy - outer, cx + outer, cy + outer), fill=(16, 13, 11, 205), outline=rgba(theme["accent"], 245), width=4)
    draw.ellipse((cx - outer + 18, cy - outer + 18, cx + outer - 18, cy + outer - 18), outline=rgba(theme["accent2"], 145), width=2)
    step = 360.0 / len(items)
    for n, item in enumerate(items):
        start = -90 + n * step + 1.2
        end = -90 + (n + 1) * step - 1.2
        points = [(cx, cy)]
        angle = start
        while angle <= end:
            r = math.radians(angle)
            points.append((cx + math.cos(r) * (outer - 25), cy + math.sin(r) * (outer - 25)))
            angle += 3
        fill = rgba(theme["highlight"], 205) if n == selected_index else (22, 18, 15, 190)
        draw.polygon(points, fill=fill, outline=rgba(theme["accent2"], 180))
        mid = math.radians((start + end) / 2)
        tx = cx + math.cos(mid) * outer * 0.63
        ty = cy + math.sin(mid) * outer * 0.63
        text_size = label_size - 2 if len(item.replace("\n", "")) > 5 else label_size
        multiline_label(
            draw,
            (tx, ty),
            item,
            theme,
            size=text_size,
            color=theme["ink"] if n == selected_index else (236, 218, 181, 255),
            bold=n == selected_index,
            anchor="mm",
            spacing=0,
        )
    draw.ellipse((cx - outer * 0.30, cy - outer * 0.30, cx + outer * 0.30, cy + outer * 0.30), fill=rgba(theme["panel2"], 238), outline=rgba(theme["accent2"], 230), width=3)
    if center_lines:
        multiline_label(draw, (cx, cy), "\n".join(center_lines), theme, size=24 if len(center_lines) == 1 else 21, color=theme["ink"], bold=True, anchor="mm", spacing=3)


def pill_button(draw, box, icon, text, theme, selected=False, size=18):
    x0, y0, x1, y1 = box
    fill = (25, 21, 18, 226)
    outline = rgba(theme["accent2"], 255 if selected else 205)
    draw.rounded_rectangle(box, radius=18, fill=fill, outline=outline, width=3 if selected else 2)
    radius = 17
    cx, cy = x0 + 32, (y0 + y1) // 2
    draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=rgba(theme["panel2"], 220), outline=rgba(theme["accent2"], 225), width=2)
    label(draw, (cx, cy), icon, theme, size=19, color=theme["ink"], bold=True, anchor="mm")
    label(draw, (x0 + 62, cy), text, theme, size=size, color=(239, 222, 185, 255), bold=selected, anchor="lm")


def circle_action(draw, center, icon, text, theme, selected=False, radius=28):
    cx, cy = center
    fill = rgba(theme["highlight"], 205 if selected else 120)
    outline = rgba(theme["accent"], 245)
    draw.ellipse((cx - radius, cy - radius, cx + radius, cy + radius), fill=fill, outline=outline, width=3)
    label(draw, (cx, cy), icon, theme, size=20, color=theme["ink"], bold=selected, anchor="mm")
    label(draw, (cx, cy + radius + 14), text, theme, size=13, color=theme["ink"], anchor="mt")


def draw_wheel(index, theme):
    image = scene_background(theme, index)
    overlay = Image.new("RGBA", SIZE, (5, 5, 7, 116))
    image.alpha_composite(overlay)
    draw = ImageDraw.Draw(image, "RGBA")
    label(draw, (64, 54), f"轮盘方案 {index:02d} · {theme['name']}", theme, size=34, color=(249, 230, 183, 255), bold=True)
    label(draw, (66, 100), "按住 T 框选 NPC，松开进入轮盘 · 动作二级菜单仍在轮盘内 · 鼠标悬停后左键确认", theme, size=21, color=(223, 202, 165, 255))
    # Targeting markers on the live scene.
    marker_data = [(270, 575, False, False, False), (520, 660, False, False, True), (1660, 420, True, False, False), (1705, 695, False, True, False), (1005, 770, False, False, False)]
    for x, y, primary, cancelled, hovered in marker_data:
        npc_marker(draw, x, y, theme, primary=primary, cancelled=cancelled, hovered=hovered)
    label(draw, (198, 625), "路人 01", theme, size=18, color=(239, 218, 177, 240))
    label(draw, (445, 717), "路人 02", theme, size=18, color=(239, 218, 177, 240))
    label(draw, (1565, 360), "学者·哈拉忒奥斯 · 主目标", theme, size=18, color=theme["highlight"], bold=True)
    label(draw, (1645, 750), "路人 03 · 已取消", theme, size=18, color=(225, 152, 131, 240))
    # Main wheel and the action branch are both circular. The action branch no longer lives in a rectangular side panel.
    radial_menu(draw, (700, 520), 286, ["对话", "动作", "离开"], theme, selected_index=1, center_lines=("场景喊话", "已选 4 / 6"), label_size=31)
    label(draw, (700, 844), "一级轮盘", theme, size=20, color=(229, 205, 160, 235), bold=True, anchor="mm")
    action_items = ["给予物品", "展示物品", "给予部队", "给予俘虏", "转移\n固定资产", "演讲", "开发模式\n标签测试"]
    radial_menu(draw, (1285, 520), 238, action_items, theme, selected_index=index % len(action_items), center_lines=("动作", "二级"), label_size=19)
    label(draw, (1285, 804), "动作 · 二级轮盘", theme, size=22, color=(239, 214, 169, 245), bold=True, anchor="mm")
    label(draw, (1285, 838), "内容留在同一轮盘内，悬停后确认", theme, size=17, color=(205, 182, 144, 220), anchor="mm")
    panel(draw, (70, 900, 620, 1018), theme, radius=14, fill=(22, 18, 15, 180))
    label(draw, (102, 938), "T 框选快照已冻结 · 主目标锁定 · 路人 03 可点击取消", theme, size=19, color=(237, 214, 170, 255))
    return image.convert("RGB")


def draw_history(draw, box, theme):
    panel(draw, box, theme, radius=18, fill=rgba(theme["panel"], 238))
    x0, y0, x1, y1 = box
    label(draw, (x0 + 34, y0 + 30), "当前场景喊话历史", theme, size=28, bold=True)
    lines = [
        ("你", "这里的人，请先停一下。"),
        ("学者·哈拉忒奥斯", "如果你在询问城中的消息，我可以回答。"),
        ("路人 01", "他刚才说的是哪一条街？"),
        ("你", "只要愿意听，我会继续说明。"),
    ]
    y = y0 + 93
    for speaker, text in lines:
        speaker_color = theme["accent"] if speaker == "你" else theme["muted"]
        label(draw, (x0 + 38, y), speaker, theme, size=18, color=speaker_color, bold=True)
        label(draw, (x0 + 38, y + 28), text, theme, size=20, color=theme["ink"])
        draw.line((x0 + 34, y + 67, x1 - 34, y + 67), fill=rgba(theme["accent"], 65), width=1)
        y += 87


def draw_session(index, theme):
    image = scene_background(theme, index)
    draw = ImageDraw.Draw(image, "RGBA")

    # Keep the scene readable, then place the interaction in the bottom third like the reference input roll.
    draw.rectangle((0, 620, 1920, 1080), fill=(6, 7, 8, 88))
    npc_marker(draw, 286, 560, theme, primary=False, cancelled=False, hovered=False)
    npc_marker(draw, 1560, 565, theme, primary=True, cancelled=False, hovered=False)
    npc_marker(draw, 1740, 620, theme, primary=False, cancelled=True, hovered=True)
    label(draw, (216, 520), "路人 01", theme, size=18, color=(243, 220, 173, 230))
    label(draw, (1505, 510), "学者·哈拉忒奥斯", theme, size=18, color=theme["highlight"], bold=True)
    label(draw, (1650, 665), "路人 03 · 已取消", theme, size=18, color=(227, 159, 137, 230))

    # Scene badge and the collapsed-state affordance remain above the scroll.
    draw.rounded_rectangle((64, 56, 300, 112), radius=14, fill=(14, 12, 10, 225), outline=rgba(theme["accent2"], 230), width=2)
    label(draw, (88, 84), f"0{index}", theme, size=25, color=theme["highlight"], bold=True, anchor="lm")
    label(draw, (154, 84), f"场景喊话 · {index:02d}", theme, size=19, color=(243, 222, 183, 255), bold=True, anchor="lm")
    draw.rounded_rectangle((1480, 574, 1855, 616), radius=12, fill=(18, 15, 13, 220), outline=rgba(theme["accent"], 190), width=2)
    label(draw, (1500, 595), "收起态 · 场景喊话 4 / 6", theme, size=14, color=(232, 207, 163, 245), anchor="lm")
    button(draw, (1765, 581, 1838, 610), "展开 ▼", theme, selected=True, size=12)

    # Three compact topic / participant pills echo the reference's quick-choice row.
    pill_button(draw, (208, 646, 700, 704), "锁", "主目标锁定 · 参与者 4 / 6", theme, selected=True, size=17)
    pill_button(draw, (722, 646, 1215, 704), "人", "点击无名路人标记即可取消", theme, selected=False, size=17)
    pill_button(draw, (1237, 646, 1728, 704), "话", "T 轮盘 · 对话 / 动作 / 离开", theme, selected=False, size=17)

    # Parchment scroll body.
    scroll = (150, 708, 1770, 1060)
    draw.rounded_rectangle(scroll, radius=20, fill=rgba(theme["panel"], 246), outline=rgba(theme["accent"], 245), width=3)
    draw.rounded_rectangle((164, 720, 1756, 1048), radius=14, outline=rgba(theme["accent2"], 110), width=2)
    # Scroll rods and small ornamental curls.
    draw.rounded_rectangle((150, 732, 188, 1037), radius=18, fill=rgba(theme["panel2"], 238), outline=rgba(theme["accent"], 220), width=2)
    draw.rounded_rectangle((1732, 732, 1770, 1037), radius=18, fill=rgba(theme["panel2"], 238), outline=rgba(theme["accent"], 220), width=2)
    for y in (764, 1008):
        draw.ellipse((158, y, 180, y + 22), outline=rgba(theme["accent"], 180), width=2)
        draw.ellipse((1740, y, 1762, y + 22), outline=rgba(theme["accent"], 180), width=2)
    for offset in (0, 1):
        draw.arc((190 + offset * 8, 744 + offset * 8, 360 - offset * 8, 900 - offset * 8), 100, 270, fill=rgba(theme["accent"], 165), width=2)
        draw.arc((1510 - offset * 8, 866 - offset * 8, 1705 + offset * 8, 1025 + offset * 8), 280, 80, fill=rgba(theme["accent"], 140), width=2)

    # Compact participant drawer on the left of the scroll.
    draw.rounded_rectangle((205, 752, 418, 1018), radius=12, fill=(53, 40, 29, 105), outline=rgba(theme["accent"], 145), width=2)
    label(draw, (226, 770), "参与者 4 / 6", theme, size=18, color=(245, 223, 180, 255), bold=True)
    participants = [
        ("主目标", "锁定", True, True),
        ("路人 01", "参与", True, False),
        ("路人 02", "参与", True, False),
        ("路人 03", "取消", False, False),
        ("守卫", "参与", True, False),
    ]
    for n, (name, status, checked, locked) in enumerate(participants):
        y = 806 + n * 38
        checkbox(draw, 220, y, theme, checked=checked, locked=locked)
        label(draw, (258, y + 1), name, theme, size=14, color=(245, 223, 180, 255) if checked else theme["danger"], bold=locked)
        label(draw, (258, y + 20), status, theme, size=12, color=(218, 190, 146, 245) if checked else theme["danger"])
    label(draw, (220, 994), "点选无名路人取消", theme, size=12, color=theme["muted"])

    # Central wide history and text input, matching the reference's dark inset field.
    label(draw, (452, 748), "当前场景喊话历史", theme, size=19, color=theme["ink"], bold=True)
    draw.rounded_rectangle((432, 776, 1382, 1008), radius=12, fill=(45, 35, 29, 225), outline=rgba(theme["accent"], 220), width=3)
    history_lines = [
        ("你", "这里的人，请先停一下。"),
        ("学者", "如果你在询问城中的消息，我可以回答。"),
        ("路人 01", "他刚才说的是哪一条街？"),
    ]
    for n, (speaker, text) in enumerate(history_lines):
        y = 802 + n * 34
        label(draw, (458, y), f"{speaker}：", theme, size=14, color=theme["accent2"] if speaker == "你" else (216, 186, 137, 245), bold=True)
        label(draw, (560, y), text, theme, size=14, color=(224, 210, 184, 235))
        draw.line((454, y + 27, 1360, y + 27), fill=rgba(theme["accent"], 62), width=1)
    label(draw, (458, 934), "输入下一句……", theme, size=20, color=(228, 211, 181, 185))
    label(draw, (458, 982), "Enter 发送 · Shift+Enter 换行 · 发送后保持同一参与者快照", theme, size=13, color=(202, 181, 150, 215))

    # Right-side icon actions mirror History / Gift / Paint while carrying our session controls.
    circle_action(draw, (1470, 825), "史", "历史", theme, selected=index == 1)
    circle_action(draw, (1550, 825), "人", "参与", theme, selected=False)
    circle_action(draw, (1630, 825), "画", "插画", theme, selected=False)
    label(draw, (1430, 884), "会话状态", theme, size=16, color=theme["ink"], bold=True)
    label(draw, (1430, 914), "目标有效 · 主目标锁定", theme, size=13, color=theme["muted"])
    label(draw, (1430, 938), "历史已加载 · 可重复发送", theme, size=13, color=theme["muted"])
    button(draw, (1430, 966, 1568, 1014), "离开", theme, selected=False, size=17)
    button(draw, (1580, 966, 1718, 1014), "发送", theme, selected=True, size=17)
    label(draw, (1430, 1030), "展开 ▼ / 收起 ▲", theme, size=13, color=theme["muted"])
    return image.convert("RGB")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for index, theme in enumerate(THEMES, start=1):
        wheel = draw_wheel(index, theme)
        session = draw_session(index, theme)
        wheel_path = OUTPUT / f"shout-wheel-option-{index:02d}-preview.png"
        session_path = OUTPUT / f"shout-session-option-{index:02d}-preview.png"
        wheel.save(wheel_path, optimize=True)
        session.save(session_path, optimize=True)
        print(f"{wheel_path} {wheel.size}")
        print(f"{session_path} {session.size}")


if __name__ == "__main__":
    main()
