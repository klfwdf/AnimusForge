"""Fixed-baseline inverse of gallery/conversion/photo UI; no compilation or game API."""
from pathlib import Path
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from verify_gallery_defaults_static import restore_converter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
PACKET = json.loads((HERE / "remaining-keys.json").read_text(encoding="utf-8"))
PREFIX = "extensions/AnimusForge.Illustrator/src/"


def old(path):
    return subprocess.check_output(["git", "show", PACKET["baseline"] + ":" + path], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")


edits = {
    "Engine/GalleryPreviewLoader.cs": [
        ('request.Failed?.Invoke("图片不存在或无法解码，请刷新画廊。")', 'request.Failed?.Invoke(IllustratorRemainingUiText.GalleryPreviewUnavailable)'),
    ],
    "UI/Gallery/IllustratorGalleryPopup.cs": [
        ('_dataSource.StatusText = "正在基于所选画卷重绘，可关闭画廊等待。";', '_dataSource.StatusText = IllustratorRemainingUiText.GalleryRedrawLoading;'),
        ('_dataSource.StatusText = result.Saved != null ? "重绘完成，已保存新画卷。" : "重绘失败：" + result.Result?.ErrorMessage;', '_dataSource.StatusText = result.Saved != null ? IllustratorRemainingUiText.GalleryRedrawComplete : IllustratorUiText.WeeklyRedrawFailed(result.Result?.ErrorMessage);'),
        ('_dataSource.StatusText = "重绘失败：" + error;', '_dataSource.StatusText = IllustratorUiText.WeeklyRedrawFailed(error);'),
    ],
    "UI/Gallery/GalleryRpItemConverter.cs": [
        ('message = "画卷或当前战役已失效，请重新打开画廊。";', 'message = IllustratorRemainingUiText.GalleryConversionUnavailable;'),
        ('message = "原画文件已不存在，未创建物品。";', 'message = IllustratorRemainingUiText.GalleryConversionSourceMissing;'),
        ('message = "请填写画卷介绍，NPC 展示时需要读取这段内容。";', 'message = IllustratorRemainingUiText.GalleryConversionDescriptionRequired;'),
        ('message = "未能创建画卷物品，背包未增加物品。";', 'message = IllustratorRemainingUiText.GalleryConversionCreateFailed;'),
        ('message = "画卷介绍保存失败：" + error;', 'message = IllustratorRemainingUiText.GalleryConversionDescriptionFailed(error);'),
        ('message = "背包中已有" + name + "，已更新介绍，未重复添加。";', 'message = IllustratorRemainingUiText.GalleryConversionAlreadyOwned(DisplayItemName(image));'),
        ('? "已将" + name + "放入背包；可通过对话中的“展示物品”向 NPC 展示。"', '? IllustratorRemainingUiText.GalleryConversionDelivered(DisplayItemName(image))'),
        (': "画卷未能放入背包，请检查背包后再试。";', ': IllustratorRemainingUiText.GalleryConversionDeliveryFailed;'),
    ],
    "UI/Overlays/MissionPhotoOverlay.cs": [
        ('_vm.Set("Enter 回车截取第 " + (count + 1) + " 张 · Esc 取消", false, "", "", null, null);', '_vm.Set(IllustratorRemainingUiText.PhotoAim(count + 1), false, "", "", null, null);'),
        ('_vm.Set("已截取 " + count + " 张，是否开始生成？", true, "生成", count == 1 ? "继续截取" : "重新截取",', '_vm.Set(IllustratorRemainingUiText.PhotoDecision(count), true, IllustratorRemainingUiText.PhotoGenerate, count == 1 ? IllustratorRemainingUiText.PhotoContinue : IllustratorRemainingUiText.PhotoRestart,'),
        ('_vm.Set("已中止本次取景", true, "重新截取", "取消", _vm.Restart, _vm.Cancel);', '_vm.Set(IllustratorRemainingUiText.PhotoStopped, true, IllustratorRemainingUiText.PhotoRestart, IllustratorUiText.Cancel, _vm.Restart, _vm.Cancel);'),
    ],
}
display_helper = '''        private static string DisplayItemName(CachedIllustrationItem item)
        {
            string title = AnimusForgeTextInputSanitizer.SanitizeSingleLine(item?.Title ?? "", 100).Trim();
            return IllustratorRemainingUiText.GalleryConversionItemName(title.Length == 0 ? IllustratorRemainingUiText.GalleryConversionDefaultTitle : title);
        }

'''

for relative, replacements in edits.items():
    path = PREFIX + relative
    source = (ROOT / path).read_text(encoding="utf-8-sig")
    if relative.endswith("GalleryRpItemConverter.cs"):
        source = restore_converter(source)
    restored = source
    baseline = old(path)
    for before, after in replacements:
        assert restored.count(after) == baseline.count(before) == 1, after
        restored = restored.replace(after, before)
    if relative != "UI/Overlays/MissionPhotoOverlay.cs":
        restored = restored.replace("using AnimusForge.Illustrator.UI;\n", "", 1)
    if relative.endswith("GalleryRpItemConverter.cs"):
        assert restored.count(display_helper) == 1
        restored = restored.replace(display_helper, "", 1)
        assert 'string name = ItemName(image);' in source
        assert 'string description = Clean(introduction,' in source
    assert restored == baseline, "Unexpected asset/business/lifecycle change: " + relative
    assert not re.search(r'(?:message\s*=|StatusText\s*=|_vm.Set|request.Failed)[^\n]*[\u4e00-\u9fff]', source), relative

catalog = (ROOT / PREFIX / "UI/IllustratorRemainingUiText.cs").read_text(encoding="utf-8-sig")
ids = set()
for key in PACKET["newKeys"]:
    assert key["id"] not in ids
    ids.add(key["id"])
    candidates = [line for line in catalog.splitlines() if '"' + key["id"] + '"' in line]
    assert len(candidates) == 1 and '"' + key["en"] + '"' in candidates[0], key["id"]
    assert re.findall(r'\{[A-Z_]+\}', key["en"]) == re.findall(r'\{[A-Z_]+\}', key["zh"]), key["id"]
    assert not re.search(r'[\u4e00-\u9fff]', key["en"]), key["id"]
assert len(ids) == 19
for project in ["GalleryRpConversionTests", "MissionScreenshotTests"]:
    path = ROOT / "extensions/AnimusForge.Illustrator/tests" / project / (project + ".csproj")
    includes = [item.get("Include") for item in ET.parse(path).iter("Compile")]
    for name in ["IllustratorUiText.cs", "IllustratorRemainingUiText.cs", "LocalizationTextObject.cs"]:
        resolved = [path.parent / include for include in includes if include.endswith(name)]
        assert len(resolved) == 1 and resolved[0].is_file(), (project, name)

print("PASS earlier Illustrator UI slice inverse after restoring newer editable-default display slice; 19 EN/CN keys; original ItemName/roster/callbacks unchanged; no compiler/API run")
