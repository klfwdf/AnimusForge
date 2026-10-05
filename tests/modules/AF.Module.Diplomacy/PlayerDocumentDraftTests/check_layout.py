"""Check current prefab geometry against actual scroll pixels; write a browser preview.
This is an offline layout projection, not Gauntlet/game acceptance.
"""
from pathlib import Path
import base64, json, sys
import xml.etree.ElementTree as ET
from PIL import Image

root = Path(__file__).resolve().parents[4]
out = root / "artifacts/diplomatic-document-drafting-20261005"
prefab = root / "content/modules/AF.Module.Diplomacy/GUI/Prefabs/WorldDiplomacyComposePopup.xml"
background = root / "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_scroll.png"
tree = ET.parse(prefab)
paper = tree.find("./Window/Widget/Children/Widget")
w, h = (int(paper.get(key)) for key in ("SuggestedWidth", "SuggestedHeight"))
row = paper.find("./Children/ListPanel")
children = list(row.find("Children"))
widths = [int(c.get("SuggestedWidth")) for c in children]
x = (w - sum(widths)) / 2
y = h - int(row.get("MarginBottom")) - int(row.get("SuggestedHeight"))
buttons = []
for child, width in zip(children, widths):
    if child.tag == "ButtonWidget":
        buttons.append((child, (x, y, x + width, y + int(child.get("SuggestedHeight")))))
    x += width
assert [b[0].get("Command.Click") for b in buttons] == ["ExecuteCancel", "ExecutePublish", "ExecuteAutoDraft"]
assert (buttons[1][1][0] + buttons[1][1][2]) / 2 == w / 2
assert len({b[1][1] for b in buttons}) == 1
image = Image.open(background).convert("RGB")
red_pixels = [(x, y) for y in range(image.height // 2, image.height)
              for x in range(image.width // 2, image.width)
              if (lambda c: c[0] > 100 and c[1] < 90 and c[0] > c[1] + 50 and c[0] > c[2] + 50)(image.getpixel((x, y)))]
assert red_pixels, "Seal red mask not found in actual background"
seal = ((min(p[0] for p in red_pixels) - 12) * w / image.width,
        (min(p[1] for p in red_pixels) - 12) * h / image.height,
        (max(p[0] for p in red_pixels) + 12) * w / image.width,
        (max(p[1] for p in red_pixels) + 12) * h / image.height)
def overlaps(a, b):
    return a[0] < b[2] and a[2] > b[0] and a[1] < b[3] and a[3] > b[1]
editor = paper.find("./Children/ScrollablePanel")
editor_box = (int(editor.get("MarginLeft")), int(editor.get("MarginTop")),
              w - int(editor.get("MarginRight")), h - int(editor.get("MarginBottom")))
for button, box in buttons:
    assert not overlaps(box, seal), "Button intersects expanded seal mask"
    assert not overlaps(box, editor_box), "Button intersects editable content"
    text = button.find("./Children/TextWidget")
    label = {"@AutoDraftText": "书记官拟稿中…"}.get(text.get("Text"), text.get("Text"))
    assert len(label) * 22 <= box[2] - box[0] - int(text.get("MarginLeft")) - int(text.get("MarginRight")), "Button label cannot fit"
report = {"paper": [w, h], "publish_center": w / 2, "buttons": [b[1] for b in buttons],
          "expanded_seal_bbox": seal, "editor": editor_box, "status": "PASS", "kind": "offline prefab geometry"}
out.mkdir(parents=True, exist_ok=True)
(out / "layout.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
bg_data = base64.b64encode(background.read_bytes()).decode("ascii")
css = "body{margin:0;background:#26302f;display:grid;place-items:center;height:100vh} .paper{position:relative;background-size:100% 100%;font-family:KaiTi,SimKai,serif;color:#15100b} .button{position:absolute;box-sizing:border-box;border:3px ridge #54472e;background:#b39f75aa;text-align:center;line-height:38px;font-size:22px;border-radius:8px} .title{position:absolute;left:235px;right:235px;top:104px;text-align:center;font-size:30px}.note{position:absolute;left:170px;right:430px;top:474px;font-size:18px}.text{position:absolute;left:170px;right:430px;top:166px;font-size:24px;line-height:32px}.label{position:absolute;bottom:12px;color:#ddd;font:14px sans-serif}"
html = '<!doctype html><meta charset="utf-8"><style>' + css + '</style>'
html += f'<div class="paper" style="width:{w}px;height:{h}px;background-image:url(data:image/png;base64,{bg_data})"><div class="title">撰写外交宣言</div>'
html += '<div class="text">瓦兰迪亚，我方愿就和平展开商议。双方可就停战条件继续磋商，望贵国慎重考虑，并给予明确答复。</div>'
for (_, box), label in zip(buttons, ["取消", "发布外交文书", "书记官代笔"]):
    html += f'<div class="button" style="left:{box[0]}px;top:{box[1]}px;width:{box[2]-box[0]}px;height:{box[3]-box[1]}px">{label}</div>'
html += '<div class="note">稿件已回填，可修改后发布；再次代笔将重新拟稿。</div></div><div class="label">依据当前 prefab 的布局示意，按钮外观为近似绘制，非游戏截图</div>'
(out / "layout-preview.html").write_text(html, encoding="utf-8")
print("PASS: publish centered; all 3 buttons aligned and clear of actual seal/editor; labels fit.")
