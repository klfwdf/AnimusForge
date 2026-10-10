"""Static contract for menus/notices; compilation and game execution are separate."""
import importlib.util
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("inventory", ROOT / "tools/localization/inventory_untranslated.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)
en = {node.get("id"): node.get("text") for node in ET.parse(ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml").iter("string")}
zh = {node.get("id"): node.get("text") for node in ET.parse(ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml").iter("string")}

def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig").replace("\r\n", "\n")

def baseline(path):
    return subprocess.check_output(["git", "show", "37467703e:" + path], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")

def method(source, signature):
    start = source.index(signature)
    code = audit.tokenize(source)[0]
    opening = code.index("{", start)
    depth = 1
    for end in range(opening + 1, len(code)):
        depth += (code[end] == "{") - (code[end] == "}")
        if depth == 0:
            return source[start:end + 1]
    raise AssertionError(signature)

def restore_literals(source):
    def restore(match):
        marker = re.fullmatch(r"\{=([^}]+)\}(.*)", audit.decode(match[0]), re.S)
        if not marker:
            return match[0]
        key, fallback = marker.groups()
        assert en[key] == fallback, key
        assert key in zh and zh[key], key
        return json.dumps(("{=!}" if key == "AF_UI_Developer_Developer_tools" else "") + zh[key], ensure_ascii=False)
    return audit.LEX.sub(lambda match: restore(match) if match.lastgroup == "string" else match[0], source)

host = "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs"
source, old = read(host), baseline(host)
for signature in ("private void OnSessionLaunched(", "private void OnSettlementOverlayQuickTalkPermission("):
    actual = restore_literals(method(source, signature))
    assert actual == method(old, signature), signature
assert not audit.HAN.search(method(source, "private void OnSessionLaunched("))

news = "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.NewsCollection.cs"
source, old = read(news), baseline(news)
start = "        InformationManager.DisplayMessage("
end = "        Logger.Log(\"EventWeeklyReport\", \"[ModeRestart]"
assert source[:source.index(start)].replace("using TaleWorlds.Localization;\n", "") == old[:old.index(start)]
assert source[source.index(end):] == old[old.index(end):]
restore_literals(source)
assert "if (state.CollectionBulletinMode == bulletin && !_newsCollectionNewGame) return;" in source

native = "src/AF.GameAdapter.Bannerlord/Scene/SceneShoutInputController.cs"
actual = restore_literals(method(read(native), "internal static void OpenNativeConversationInput("))
actual = actual.replace('new TextObject("' + zh["AF_NOTIFY_NativeConversation_InputOpened"] + '").ToString()', '"' + zh["AF_NOTIFY_NativeConversation_InputOpened"] + '"')
expected = method(baseline(native), "internal static void OpenNativeConversationInput(")
# The original source encoded this message with Unicode escapes.
expected = audit.LEX.sub(lambda m: json.dumps(audit.decode(m[0]), ensure_ascii=False) if m.lastgroup == "string" else m[0], expected)
assert actual == expected

escort = "src/modules/AF.Module.Encounter/Escort/NoblePrisonerEscortMissionBehavior.cs"
source, old = read(escort), baseline(escort)
new_message = 'new TextObject("{=AF_NOTIFY_Escort_PrisonersSpawned}' + en["AF_NOTIFY_Escort_PrisonersSpawned"] + '").SetTextVariable("COUNT", spawned).ToString()'
assert new_message in source
assert source.replace("using TaleWorlds.Localization;\n", "").replace(new_message, '"【贵族俘虏随行】已带入 " + spawned + " 名英雄俘虏，归入第 8 编队。"') == old
restore_literals(source)
print("PASS menu/notice static contract: fallbacks, unchanged menu actions, news lifecycle and input/escort mechanics")
