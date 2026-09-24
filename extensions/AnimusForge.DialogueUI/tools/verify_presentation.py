"""Static resource/interaction contract checks; does not claim in-game rendering coverage."""
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image

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
assert layers[0].get("Id") == "AFDialoguePortraitBackground" and layers[1].tag == "CharacterTableauWidget"
assert layers[0].get("DoNotAcceptEvents") == "true"
with Image.open(ROOT / "GUI/SpriteParts/afdui_portrait_background.png") as image:
    assert image.size == (256, 256) and image.getchannel("A").getextrema() == (255, 255)

for prefab in prefabs.values():
    assert not re.search(r"@(Option[0-5](Text|Visible)|DialogueText|SpeakerName)\b|ExecuteOption[0-5]", ET.tostring(prefab, encoding="unicode"))
print(f"PASS: {len(prefabs)} XML prefabs, {len(registered)} registered sprites, map/barter/continue bindings, native answer path, opaque clipped portrait background; no removed VM bindings.")
