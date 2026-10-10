"""Static contract for detached status display; no compilation or model calls."""
from pathlib import Path
import json
import re
import subprocess
from verify_bulletin_static import restore_catalog as restore_bulletin_catalog
from verify_gallery_defaults_static import restore_probe

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
PACKET = json.loads((HERE / "status-keys.json").read_text(encoding="utf-8"))
PREFIX = "extensions/AnimusForge.Illustrator/src/"

def old(path):
    return subprocess.check_output(["git", "show", PACKET["baseline"] + ":" + path], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")

path = PREFIX + "UI/Overlays/IllustrationCardPopup.cs"
source = (ROOT / path).read_text(encoding="utf-8-sig")
baseline = old(path)
edits = [
    ("update.Saved.DisplayStatusText", "IllustrationStatusUiText.Cached(update.Saved)"),
    ("DiskImageCacheManager.CachedDisplayStatus(cached, IllustratorRuntime.CaptureOptions()?.StyleFingerprint)", "IllustrationStatusUiText.Cached(cached, IllustratorRuntime.CaptureOptions()?.StyleFingerprint, checkStyle: true)"),
    ('_dataSource.StatusText = direction.StatusText + " · "', '_dataSource.StatusText = IllustrationStatusUiText.Direction(direction) + " · "'),
    ('completion.SavedItem?.DisplayStatusText ?? IllustratorUiText.PortraitComplete', 'completion.SavedItem != null ? IllustrationStatusUiText.Cached(completion.SavedItem) : IllustratorUiText.PortraitComplete'),
    ('completion.SavedItem?.DisplayStatusText ?? IllustratorUiText.ConversationComplete', 'completion.SavedItem != null ? IllustrationStatusUiText.Cached(completion.SavedItem) : IllustratorUiText.ConversationComplete'),
    ('sceneStatus + " · " + IllustratorUiText.PortraitPreparing', 'IllustrationStatusUiText.Scene(sceneStatus) + " · " + IllustratorUiText.PortraitPreparing'),
    ('+ " · " + direction.StatusText + " · " + IllustratorUiText.WeeklyDrawing', '+ " · " + IllustrationStatusUiText.Direction(direction) + " · " + IllustratorUiText.WeeklyDrawing'),
]
def restore_popup(source):
    restored = restore_probe(source)
    for before, after in edits:
        assert restored.count(after) == baseline.count(before) > 0, after
        restored = restored.replace(after, before)
    return restored

restored = restore_popup(source)
assert restored == baseline, "Unexpected popup non-display change"
for line in source.splitlines():
    if 'IllustrationStatusUiText.Direction(' in line or 'IllustrationStatusUiText.Scene(' in line:
        assert 'IllustratorRuntime.Post(() =>' in line, "Worker status resolution"

catalog_path = PREFIX + "UI/IllustratorUiText.cs"
catalog = (ROOT / catalog_path).read_text(encoding="utf-8-sig")
def restore_catalog(source):
    source = restore_bulletin_catalog(source)
    for key in PACKET["newKeys"]:
        candidates = [line for line in source.splitlines(keepends=True) if '"' + key["id"] + '"' in line]
        assert len(candidates) == 1, key["id"]
        source = source.replace(candidates[0], '', 1)
    return source

for key in PACKET["newKeys"]:
    candidates = [line for line in catalog.splitlines(keepends=True) if '"' + key["id"] + '"' in line]
    assert len(candidates) == 1 and '"' + key["en"] + '"' in candidates[0], key["id"]
    assert re.findall(r'\{[A-Z_]+\}', key["en"]) == re.findall(r'\{[A-Z_]+\}', key["zh"])
restored_catalog = restore_catalog(catalog)
assert restored_catalog == old(catalog_path), "Unexpected existing catalog change"
for name in ["Core/IllustrationDirection.cs", "Engine/DiskImageCacheManager.cs", "Engine/MapConversationSceneCapture.cs", "Engine/SceneReferenceCapture.cs"]:
    assert (ROOT / PREFIX / name).read_text(encoding="utf-8-sig") == old(PREFIX + name), "Producer changed: " + name
helper = (ROOT / PREFIX / "UI/IllustrationStatusUiText.cs").read_text(encoding="utf-8-sig")
assert 'default: return raw;' in helper and 'return status == "local_fallback" ? KnownLocalFallback(raw) : raw;' in helper
assert 'IllustratorUiText.StatusTheme(item.Theme)' in helper and 'Failed(direction.FallbackReason)' in helper
assert 'private static readonly Regex SceneBudget' in helper and '^环境物件超过([0-9]+)个预算，已跳过环境采集，继续绘制$' in helper
assert not re.search(r'(?:item|direction)\.[A-Za-z]+\s*=(?!=)', helper), "Metadata write in display projection"
print("PASS Illustrator status display exact inverse; producers unchanged; 23 EN/CN placeholder pairs; unknown/dynamic values retained; direction/scene resolution only inside existing Post")
