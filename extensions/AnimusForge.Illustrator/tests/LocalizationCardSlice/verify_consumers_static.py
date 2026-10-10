"""Read-only closure of gallery/weekly status sinks; no compiler or game API."""
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
BASELINE = "488957016f7765b7fcf3f4da7f69b4f3d546c21f"
PREFIX = "extensions/AnimusForge.Illustrator/src/"


def old(path):
    return subprocess.check_output(
        ["git", "show", BASELINE + ":" + path], cwd=ROOT
    ).decode("utf-8-sig").replace("\r\n", "\n")


edits = {
    "UI/Gallery/IllustratorGalleryPopupVM.cs": [
        ("selected.Item.DisplayStatusText", "IllustrationStatusUiText.Cached(selected.Item)", 2),
    ],
    "UI/Patches/WeeklyReportPopupIllustrationPatch.cs": [
        ("else _sink.StatusText = cached.DisplayStatusText;", "else _sink.StatusText = IllustrationStatusUiText.Cached(cached);", 1),
        ("DiskImageCacheManager.CachedDisplayStatus(cached, cacheOptions?.StyleFingerprint)", "IllustrationStatusUiText.Cached(cached, cacheOptions?.StyleFingerprint, checkStyle: true)", 1),
        ("_sink.StatusText = update.Saved.DisplayStatusText;", "_sink.StatusText = IllustrationStatusUiText.Cached(update.Saved);", 1),
        ("result.Saved?.DisplayStatusText ?? IllustratorUiText.WeeklyComplete", "result.Saved != null ? IllustrationStatusUiText.Cached(result.Saved) : IllustratorUiText.WeeklyComplete", 1),
        ('status?.Invoke(direction.StatusText + " · " + IllustratorUiText.WeeklyDrawing)', 'status?.Invoke((bulletin ? direction.StatusText : IllustrationStatusUiText.Direction(direction)) + " · " + IllustratorUiText.WeeklyDrawing)', 1),
    ],
}

for relative, replacements in edits.items():
    path = PREFIX + relative
    source = (ROOT / path).read_text(encoding="utf-8-sig")
    baseline = old(path)
    restored = source
    for before, after, count in replacements:
        assert baseline.count(before) == restored.count(after) == count, after
        restored = restored.replace(after, before)
    assert restored == baseline, "Non-display edit: " + relative
    if relative.startswith("UI/Gallery/"):
        assert "IllustratorRuntime.AssertMainThread();" in source
        assert "IllustratorRuntime.PostCritical, GauntletTextureLoader.ReadPreparedImageForUi" in source
    else:
        direction_line = next(line for line in source.splitlines() if "IllustrationStatusUiText.Direction(" in line)
        assert "IllustratorRuntime.Post(() =>" in direction_line
        assert "bulletin ? direction.StatusText" in direction_line, "Bulletin raw status changed"
        assert "_sink.StatusText = job.Status;" in source, "Preloader state transformed"
        assert "directorStatusText: direction.StatusText" in source, "Saved metadata transformed"

for relative in ["Core/BulletinIllustrationPreloader.cs", "Engine/DiskImageCacheManager.cs", "Core/IllustrationDirection.cs"]:
    path = PREFIX + relative
    assert (ROOT / path).read_text(encoding="utf-8-sig") == old(path), "Producer edit: " + relative

print("PASS 7 gallery/weekly status sinks; exact inverse; raw bulletin status and metadata retained; direction resolves in existing Post; no compiler/API run")
