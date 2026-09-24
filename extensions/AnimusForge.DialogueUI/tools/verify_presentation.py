"""Static resource/interaction contract checks; does not claim in-game rendering coverage."""
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageFilter, ImageChops
from prepare_portrait_background import aperture_mask

ROOT = Path(__file__).resolve().parents[1]
prefabs = {p.stem: ET.parse(p).getroot() for p in (ROOT / "GUI/Prefabs").glob("*.xml")}
names = re.search(r"string\[\] Names = \{([^}]+)", (ROOT / "src/DialogueUiSprites.cs").read_text()).group(1)
registered = set(re.findall(r'"([^"]+)"', names))
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
frame_layers = list(conversation.find(".//*[@Id='AFDialogueConsoleFrame']/Children"))
background = frame_layers[0]
assert background.get("Id") == "AFDialoguePortraitBackground" and frame_layers[1].get("Id") == "AFDialogueBackdrop"
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

for prefab in prefabs.values():
    assert not re.search(r"@(Option[0-5](Text|Visible)|DialogueText|SpeakerName)\b|ExecuteOption[0-5]", ET.tostring(prefab, encoding="unicode"))
print(f"PASS: {len(prefabs)} XML prefabs, {len(registered)} registered sprites, map/barter/continue bindings, native answer path, complete oval background coverage; no removed VM bindings.")
