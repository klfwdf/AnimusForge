"""Static exact inverse of notification-only changes; current C#/game NOT-RUN."""
import importlib.util
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "5173ec03e"
spec = importlib.util.spec_from_file_location("inventory", ROOT / "tools/localization/inventory_untranslated.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)

changes = {
    "src/AF.GameAdapter.Bannerlord/SceneActions/SceneMovementController.cs": [
        ('return "那些人";', 'return ConversationUiLocalization.Text("AF_UI_Scene_SummonUnknownTargets", "those people");'),
        ('return string.Join("、", list);', 'return string.Join(ConversationUiLocalization.Text("AF_UI_Scene_NameSeparator", ", "), list);'),
        ('LaunchAnnouncement = (isInitialRequest ? (batch.SpeakerName + " 去叫" + text2 + "了。") : null),', 'LaunchAnnouncement = (isInitialRequest ? ConversationUiLocalization.Text("AF_UI_Scene_SummonStarted", "{SPEAKER} went to summon {TARGET}.", ("SPEAKER", batch.SpeakerName), ("TARGET", text2)) : null),'),
        ('LaunchAnnouncement = (isInitialRequest ? (batch.SpeakerName + " 去帮你叫" + text2 + "了。") : null),', 'LaunchAnnouncement = (isInitialRequest ? ConversationUiLocalization.Text("AF_UI_Scene_SummonStartedAcrossLocations", "{SPEAKER} went to summon {TARGET} for you.", ("SPEAKER", batch.SpeakerName), ("TARGET", text2)) : null),'),
    ],
    "src/AF.GameAdapter.Bannerlord/UI/Common/ExternalBrowserLauncher.cs": [
        ('', 'using TaleWorlds.Localization;\n'),
        ('failureMessage = "链接格式无效，只能打开 HTTP 或 HTTPS 页面。";', 'failureMessage = new TextObject("{=AF_UI_Browser_InvalidUrl}Invalid link. Only HTTP or HTTPS pages can be opened.").ToString();'),
        ('failureMessage = "系统默认浏览器无法启动，且未能启动注册表或常见安装位置中的本机浏览器。请安装或设置 Chrome、Edge、Firefox 等浏览器后重试。原因：" + ex.Message;',
         'failureMessage = DeveloperUiText.Format("AF_UI_Browser_StartFailed", "The default browser could not start, and no local browser could be launched from the registry or common installation paths. Install or configure Chrome, Edge, Firefox, or another browser and try again. Reason: {REASON}", ("REASON", (object)ex.Message));'),
    ],
}
en = {n.get("id"): n.get("text") for n in ET.parse(ROOT / "content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml").iter("string")}
scene_root = ROOT / "content/foundation/AF.Foundation.Localization/ModuleData"
scene_raw = json.loads((scene_root / "AdapterSceneCompatibility.json").read_text(encoding="utf-8"))
scene_cn = json.loads((scene_root / "AdapterSceneTexts.zh-CN.json").read_text(encoding="utf-8"))
scene_en = json.loads((scene_root / "AdapterSceneTexts.en.json").read_text(encoding="utf-8"))
seen = set()
for relative, pairs in changes.items():
    source = (ROOT / relative).read_text(encoding="utf-8-sig")
    restored = source
    for old, new in pairs:
        assert restored.count(new) == 1, (relative, new)
        restored = restored.replace(new, old, 1)
        for call in re.finditer(r'(?:ConversationUiLocalization.Text|DeveloperUiText.Format)\("([^"]+)",\s*("(?:[^"\\]|\\.)*")', new):
            assert en[call[1]] == audit.decode(call[2]), call[1]
            seen.add(call[1])
        for marker in re.finditer(r'"\{=([^}]+)\}([^"\n]+)"', new):
            assert en[marker[1]] == marker[2], marker[1]
            seen.add(marker[1])
    baseline = subprocess.check_output(["git", "show", BASELINE + ":" + relative], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
    if relative.endswith("SceneMovementController.cs"):
        restored = restored.replace('AdapterSceneTexts.ProjectLabelForPrompt(target.DisplayName.Trim())', 'target.DisplayName.Trim()')
        restored = restored.replace('AdapterSceneTexts.ProjectLabelForPrompt(target.LocationCode ?? AdapterSceneTexts.Get("Scene_Fallback_LocationCode"))', '(target.LocationCode ?? AdapterSceneTexts.Get("Scene_Fallback_LocationCode"))')
        arrival = scene_cn["SceneLive_ArrivalSpeech"]
        assert arrival.count("{PLAYER}") == 1
        restored = re.sub(r'AdapterSceneTexts.Format\("SceneLive_ArrivalSpeech", "(?:\\.|[^"\\])*", \("PLAYER", text\)\)', lambda match: json.dumps(arrival.split("{PLAYER}")[0], ensure_ascii=False) + ' + text + ' + json.dumps(arrival.split("{PLAYER}")[1], ensure_ascii=False), restored)
        for reader, values in (("Raw", scene_raw), ("Get", scene_cn)):
            restored = re.sub(r'AdapterSceneTexts\.' + reader + r'\("([^"\n]+)"\)', lambda match: json.dumps(values[match[1]], ensure_ascii=False), restored)
        for key in re.findall(r'AdapterSceneTexts\.(?:Get|Format)\("([^"\n]+)"', source):
            assert key in scene_cn and key in scene_en
            assert re.findall(r"\d+", scene_cn[key]) == re.findall(r"\d+", scene_en[key]), key
            assert re.findall(r"\{[A-Z_]+\}", scene_cn[key]) == re.findall(r"\{[A-Z_]+\}", scene_en[key]), key
    assert restored == baseline, "Non-display algorithm/guard/log/Prompt changed after exact source-resource restoration: " + relative
assert len(seen) == 6, seen
assert audit.SINKS["AnimusForgeQuickInfo.Show"] == {0}
print("PASS static 6-key notices: scene targets/stages/launch guards and browser URL/arguments/fallback/cache preserved, exception details retained; C#/game NOT-RUN")
