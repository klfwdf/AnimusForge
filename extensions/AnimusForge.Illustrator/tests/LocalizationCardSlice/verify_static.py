"""Read-only static closure check for the Illustrator card UI slice; no compiler/API."""
from pathlib import Path
import json
import re
import subprocess
import xml.etree.ElementTree as ET
import verify_status_static as status_slice

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
PACKET = json.loads((HERE / "keys.json").read_text(encoding="utf-8"))
PREFIX = "extensions/AnimusForge.Illustrator/src/UI/"

def old(path):
    return subprocess.check_output(["git", "show", PACKET["baseline"] + ":" + path], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")

path = PREFIX + "Overlays/IllustrationCardPopup.cs"
source = (ROOT / path).read_text(encoding="utf-8-sig")
source = status_slice.restore_popup(source)
edits = [
    ('_dataSource.SetLoading("画卷正在后台生成，可关闭面板，完成后会通知并更新画廊。");', '_dataSource.SetLoading(IllustratorUiText.WeeklyBackground);'),
    ('_dataSource.SetLoading("正在基于本图重绘…");', '_dataSource.SetLoading(IllustratorUiText.WeeklyRedrawLoading);'),
    ('_dataSource.SetReady("重绘完成，已保存为新版本。");', '_dataSource.SetReady(IllustratorUiText.WeeklyRedrawSuccess);'),
    ('_dataSource.SetReady("重绘失败：" + (completion.Result?.ErrorMessage ?? "未能保存或显示图片"));', '_dataSource.SetReady(IllustratorUiText.WeeklyRedrawFailed(completion.Result?.ErrorMessage ?? IllustratorUiText.CardNotSavedOrDisplayed));'),
    ('_dataSource.SetReady("重绘失败：" + error)', '_dataSource.SetReady(IllustratorUiText.WeeklyRedrawFailed(error))'),
    ('_dataSource.SetReady("已显示新设置的默认画卷。");', '_dataSource.SetReady(IllustratorUiText.WeeklyDefaultChanged);'),
    ('_dataSource.SetReady("原会话画卷已存入画廊，当前会话已变化。");', '_dataSource.SetReady(IllustratorUiText.ConversationSessionChanged);'),
    ('popup._dataSource.SetLoading("正在读取缓存画卷…");', 'popup._dataSource.SetLoading(IllustratorUiText.GalleryLoading);'),
    ('popup._dataSource.SetReady("请点击“重新绘制”生成本次对话的第一张场景插画。");', 'popup._dataSource.SetReady(IllustratorUiText.ConversationFirstImageRequired);'),
    ('_dataSource.SetReady("生成准备失败：" + ex.Message);', '_dataSource.SetReady(IllustratorUiText.CardPrepareFailed(ex.Message));'),
    ('_dataSource.SetLoading("AI画师正在细致描摹人物面相骨相与专属构图...");', '_dataSource.SetLoading(IllustratorUiText.PortraitPreparing);'),
    ('_dataSource.SetReady("无法读取百科当前展示装备，已停止生成；不会改用猜测的便服或战斗服。");', '_dataSource.SetReady(IllustratorUiText.PortraitEquipmentUnavailable);'),
    ('_dataSource.SetReady($"绘制失败: {completion.Result?.ErrorMessage ?? "未能保存图像"}");', '_dataSource.SetReady(IllustratorUiText.CardDrawFailed(completion.Result?.ErrorMessage ?? IllustratorUiText.WeeklyNotSaved));'),
    ('_dataSource.SetReady($"生成异常: {error}");', '_dataSource.SetReady(IllustratorUiText.CardGenerationException(error));'),
    ('_dataSource.SetLoading("正在重建附近环境并采集完整全景...");', '_dataSource.SetLoading(IllustratorUiText.SceneProbeLoading);'),
    ('SetSceneProbeFailure("未能显示导出的预览");', 'SetSceneProbeFailure(IllustratorUiText.SceneProbeDisplayFailed);'),
    ('_dataSource.SetReady("试采失败：" + error);', '_dataSource.SetReady(IllustratorUiText.CardProbeFailed(error));'),
    ('_dataSource.SetLoading("AI画师正在分析现场交谈与肢体姿势...");', '_dataSource.SetLoading(IllustratorUiText.ConversationPreparing);'),
    ('_dataSource.SetReady("未能取得双方完整装备快照，已停止生成；不会用兵种模板或另一套服装替代。");', '_dataSource.SetReady(IllustratorUiText.ConversationEquipmentUnavailable);'),
    ('sceneStatus = sceneCapture.StatusText + " · " + IllustratorUiText.PortraitPreparing;', 'sceneStatus = sceneCapture.StatusText;'),
    ('_dataSource.StatusText = sceneStatus;', '_dataSource.StatusText = session != null ? IllustratorUiText.ConversationReferenceReuse : sceneStatus + " · " + IllustratorUiText.PortraitPreparing;'),
    ('_dataSource.StatusText = sceneStatus + " · " + direction.StatusText + " · " + IllustratorUiText.WeeklyDrawing;', '_dataSource.StatusText = (session != null ? IllustratorUiText.ConversationReferenceReuse : sceneStatus + " · " + IllustratorUiText.PortraitPreparing) + " · " + direction.StatusText + " · " + IllustratorUiText.WeeklyDrawing;'),
]
baseline = old(path)
restored = source
for before, after in edits:
    assert restored.count(after) == baseline.count(before) > 0, after
    restored = restored.replace(after, before)
assert restored == baseline, "Unexpected popup behavior/metadata/Prompt/log edit"
assert not re.search(r'(?:SetLoading|SetReady|StatusText\s*=|TitleText\s*=)[^\n]*[\u4e00-\u9fff]', source)
for line in source.splitlines():
    if 'sceneStatus' in line and 'IllustratorUiText.' in line:
        assert 'IllustratorRuntime.Post(() =>' in line, "Worker localization resolution"

text_path = PREFIX + "IllustratorUiText.cs"
catalog = (ROOT / text_path).read_text(encoding="utf-8-sig")
catalog = status_slice.restore_catalog(catalog)
new_lines = []
for key in PACKET["newKeys"]:
    candidates = [line for line in catalog.splitlines(keepends=True) if '"' + key["id"] + '"' in line]
    assert len(candidates) == 1 and '"' + key["en"] + '"' in candidates[0], key["id"]
    assert re.findall(r'\{[A-Z_]+\}', key["en"]) == re.findall(r'\{[A-Z_]+\}', key["zh"])
    assert not re.search(r'[\u4e00-\u9fff]', key["en"])
    new_lines.append(candidates[0])
restored_catalog = catalog
for line in new_lines:
    restored_catalog = restored_catalog.replace(line, '', 1)
assert restored_catalog == old(text_path), "Unexpected existing IllustratorUiText edit"

lang = ROOT / "content/modules/AnimusForge.Illustrator/ModuleData/Languages"
english = {item.get("id"): item.get("text") for item in ET.parse(lang / "illustrator_strings.xml").iter("string")}
chinese = {item.get("id"): item.get("text") for item in ET.parse(lang / "CNs/illustrator_strings-zh-CN.xml").iter("string")}
for method in PACKET["reusedMethods"]:
    declaration = re.search(r'internal static string ' + method + r'\b[^\n]+', catalog).group()
    key = re.search(r'"(AF_ILL_[^"]+)"', declaration).group(1)
    assert key in english and key in chinese, method
print("PASS Illustrator card exact UI inverse; fixed UI Han=0; 11 new fallback/placeholder pairs; 10 reused methods present in EN/CN resources; scene localization remains inside Post; no compiler/API run")
