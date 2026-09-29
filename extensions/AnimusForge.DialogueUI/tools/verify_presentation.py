"""Static resource/interaction contract checks; does not claim in-game rendering coverage."""
import re
import math
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageFilter, ImageChops
from prepare_portrait_background import aperture_mask

ROOT = Path(__file__).resolve().parents[1]
prefabs = {p.stem: ET.parse(p).getroot() for p in (ROOT / "GUI/Prefabs").glob("*.xml")}
sprites_source = (ROOT / "src/DialogueUiSprites.cs").read_text()
registered = set()
for group in ("Names", "SceneNames"):
    registered |= set(re.findall(r'"([^"]+)"', re.search(rf"string\[\] {group} = \{{([^}}]+)", sprites_source).group(1)))
for name in registered:
    assert (ROOT / "GUI/SpriteParts" / (name + ".png")).is_file(), name
for name, prefab in prefabs.items():
    for widget in prefab.iter():
        sprite = widget.get("Sprite", "")
        assert not sprite.startswith("afdui_") or sprite in registered, (name, sprite)

conversation = prefabs["AFDialogueConversation"].find("Window")[0]
# Resolve the actual direct-child path used by Widget.FindChild(BindingPath).
answer = conversation
for part in conversation.get("AnswerList").split("\\"):
    answer = next(n for n in answer.find("Children") if n.get("Id") == part)
assert answer.get("DataSource") == "{AnswerList}"
assert answer.find("ItemTemplate/ConversationItem") is not None
assert conversation.find("Children/ButtonWidget").get("Command.Click") == "ExecuteContinue"

map_root = prefabs["AFDialogueMapConversation"].find("Window")[0]
assert map_root.tag == "MapConversationScreenButtonWidget"
assert map_root.get("Command.Click") == "ExecuteContinue"
assert map_root.get("IsBarterActive") == "@IsBarterActive"
map_content = next(n for n in map_root.find("Children") if n.get("Id") == map_root.get("ConversationParent"))
assert map_content.tag == "AFDialogueConversation" and map_content.get("DataSource") == "{DialogController}"
assert map_root.find(".//MapConversationTableauWidget").get("Data") == "@TableauData"

viewport = conversation.find(".//*[@Id='AFDialoguePortraitViewport']")
layers = list(viewport.find("Children"))
assert viewport.get("CircularClipEnabled") == "true"
assert layers[0].tag == "CharacterTableauWidget"
# The camera projection uses logical texture/crop dimensions, never render resolution.
framing = (ROOT / "src/Native/PortraitFraming.cs").read_text()
def framing_constant(name):
    return float(re.search(rf"const float {name} = (-?[\d.]+)f", framing).group(1))
assert float(layers[0].get("SuggestedHeight")) == framing_constant("TextureHeight")
assert float(viewport.get("SuggestedHeight")) == framing_constant("ViewportHeight")
assert float(layers[0].get("PositionYOffset")) == framing_constant("TextureOffsetY")
assert float(layers[0].get("CustomRenderScale")) == framing_constant("RenderQuality")
assert float(layers[0].get("SuggestedWidth")) / 2 + float(layers[0].get("PositionXOffset")) == float(viewport.get("SuggestedWidth")) / 2
frame_layers = list(conversation.find(".//*[@Id='AFDialogueConsoleFrame']/Children"))
background = frame_layers[0]
assert background.get("Id") == "AFDialoguePortraitBackground"
slot = frame_layers[1]
assert slot.get("Id") == "AFDialoguePortraitSlot" and frame_layers[2].get("Id") == "AFDialogueBackdrop"
assert background.get("DoNotAcceptEvents") == "true"
art = Image.open(ROOT / "GUI/SpriteParts/afdui_console_base_option_02_walnut_original_ratio.png").convert("RGBA")
opening = aperture_mask(art)
box = opening.filter(ImageFilter.MaxFilter(9)).getbbox()
expected = (box[0] * 1440 / art.width, box[1] * 283 / art.height, (box[2] - box[0]) * 1440 / art.width, (box[3] - box[1]) * 283 / art.height)
for attr, value in zip(("MarginLeft", "MarginTop", "SuggestedWidth", "SuggestedHeight"), expected):
    assert abs(float(background.get(attr)) - value) < 0.00001, (attr, value)
with Image.open(ROOT / "GUI/SpriteParts/afdui_portrait_background.png") as image:
    assert image.size == (box[2] - box[0], box[3] - box[1])
    coverage = Image.new("L", art.size)
    coverage.paste(image.getchannel("A"), (box[0], box[1]))
    assert ImageChops.subtract(opening, coverage).getbbox() is None, "Uncovered aperture pixels"

# Check the actual artwork opening against the model's clip geometry, including its
# smoothing band and parent rectangle. The previous 79px radius fails at the shoulders.
slot_x = float(slot.get("MarginLeft"))
slot_y = 283 - float(slot.get("MarginBottom")) - float(slot.get("SuggestedHeight"))
slot_w = float(slot.get("SuggestedWidth"))
slot_h = float(slot.get("SuggestedHeight"))
center_x = slot_x + float(viewport.get("SuggestedWidth")) / 2
center_y = slot_y + float(viewport.get("MarginTop", "0")) + float(viewport.get("SuggestedHeight")) / 2
radius = float(viewport.get("CircularClipRadius"))
smooth = float(viewport.get("CircularClipSmoothingRadius"))
assert (center_x, center_y) == (208.5, 129.5), "Aperture center must be preserved"
for y in range(box[1] - 15, box[3] + 15):
    for x in range(box[0] - 15, box[2] + 15):
        ux, uy = (x + 0.5) * 1440 / art.width, (y + 0.5) * 283 / art.height
        in_slot = slot_x <= ux <= slot_x + slot_w and slot_y <= uy <= slot_y + slot_h
        distance = math.hypot(ux - center_x, uy - center_y)
        if opening.getpixel((x, y)):
            assert in_slot and distance <= radius - smooth, ("Portrait clipped inside frame", x, y)
        elif in_slot and distance <= radius + smooth:
            assert art.getpixel((x, y))[3] >= 128, ("Portrait may leak outside frame", x, y)

# The C# wheel hit test and the hover-wedge art must describe the same ring and spokes.
from wheel_geometry import INNER, OUTER, SECTORS
sectors_cs = (ROOT / "src/Scene/WheelSectors.cs").read_text()
assert f"InnerRatio = {INNER}f / 512f" in sectors_cs and f"OuterRatio = {OUTER}f / 512f" in sectors_cs, "wheel radii drift"
for name, (start, end) in SECTORS.items():
    if end < 360:
        assert f"if (degrees < {end}) return \"{name}\";" in sectors_cs, ("wheel sector drift", name)

for prefab in prefabs.values():
    assert not re.search(r"@(Option[0-5](Text|Visible)|DialogueText|SpeakerName)\b|ExecuteOption[0-5]", ET.tostring(prefab, encoding="unicode"))
print(f"PASS: {len(prefabs)} XML prefabs, {len(registered)} registered sprites, map/barter/continue bindings, native answer path, complete background and portrait aperture coverage, no exterior leaks; camera projection matches UI crop.")
