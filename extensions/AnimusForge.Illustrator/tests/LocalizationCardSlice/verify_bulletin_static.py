"""Bulletin UI projection integration; current C# execution/game NOT-RUN."""
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET
from verify_gallery_defaults_static import restore_probe_catalog

ROOT = Path(__file__).resolve().parents[4]
PREFIX = "extensions/AnimusForge.Illustrator/src/"
BASELINE = "577f29b7a"


def old(relative):
    return subprocess.check_output(["git", "show", BASELINE + ":" + PREFIX + relative], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")


def restore_preloader(source):
    inserts = [
        '            // Display-only state; raw Status remains available to diagnostics.\n'
        '            internal string DisplayCode = "preparing";\n'
        '            internal string DisplayDetail;\n'
        '            internal CachedIllustrationItem DisplayItem;\n',
        '            job.DisplayCode = "discarded";\n',
        '                    job.DisplayCode = "raw";\n',
        '            job.DisplayCode = displayCode ?? (result?.Saved != null ? "cached" : "raw");\n'
        '            job.DisplayDetail = displayDetail;\n'
        '            job.DisplayItem = result?.Saved;\n',
        '            job.DisplayCode = "deleted";\n',
    ]
    for addition in inserts:
        assert source.count(addition) == 1, addition
        source = source.replace(addition, "", 1)
    changes = [
        ('Finish(job, "本期配图已取消，可点击重绘。", null)', 'Finish(job, "本期配图已取消，可点击重绘。", null, "cancelled")'),
        ('Finish(job, "暂无已生成插画，可点击重绘。", null)', 'Finish(job, "暂无已生成插画，可点击重绘。", null, "missing")'),
        ('Finish(job, error + "；可点击重绘。", null)', 'Finish(job, error + "；可点击重绘。", null, "retry", error)'),
        ('Finish(job, "配图准备失败：" + ex.Message + "；可点击重绘。", null)', 'Finish(job, "配图准备失败：" + ex.Message + "；可点击重绘。", null, "prepare_failed", ex.Message)'),
        ('Finish(job, "本图重绘已取消。", null)', 'Finish(job, "本图重绘已取消。", null, "redraw_cancelled")'),
        ('Result = value.Result, Saved = value.Saved, Prompt = value.Result.ResolvedPrompt });', 'Result = value.Result, Saved = value.Saved, Prompt = value.Result.ResolvedPrompt }, "redraw_complete");'),
        ('Finish(job, "重绘失败：" + (value.Result?.ErrorMessage ?? "未能保存图片"), null)', 'Finish(job, "重绘失败：" + (value.Result?.ErrorMessage ?? "未能保存图片"), null, "redraw_failed", value.Result?.ErrorMessage)'),
        ('Finish(job, "重绘失败：" + error, null)', 'Finish(job, "重绘失败：" + error, null, "redraw_failed", error ?? string.Empty)'),
        ('Finish(job, saved ? result.Saved.DisplayStatusText : "配图失败：" + (result.Result?.ErrorMessage ?? "无法保存") + "；可点击重绘。", saved ? result : null)', 'Finish(job, saved ? result.Saved.DisplayStatusText : "配图失败：" + (result.Result?.ErrorMessage ?? "无法保存") + "；可点击重绘。", saved ? result : null, saved ? "cached" : "image_failed", result.Result?.ErrorMessage)'),
        ('GenerationResult result)', 'GenerationResult result, string displayCode = null, string displayDetail = null)'),
    ]
    for before, after in changes:
        assert source.count(after) == (2 if '"retry", error' in after else 1), after
        source = source.replace(after, before)
    return source


def restore_catalog(source):
    lines = [line for line in source.splitlines(keepends=True) if '"AF_ILL_UI_BULLETIN_' in line]
    assert len(lines) == 11, len(lines)
    for line in lines:
        source = source.replace(line, "", 1)
    return restore_probe_catalog(source)


def main():
    producer = (ROOT / PREFIX / "Core/BulletinIllustrationPreloader.cs").read_text(encoding="utf-8-sig")
    assert restore_preloader(producer) == old("Core/BulletinIllustrationPreloader.cs"), "Raw values, queue, cancel/waiter/bytes or generation changed"
    assert "IllustratorUiText" not in producer and "TextObject" not in producer, "Producer resolves UI text"
    helper = (ROOT / PREFIX / "UI/IllustrationStatusUiText.cs").read_text(encoding="utf-8-sig")
    projection = helper.split("internal static string Bulletin(", 1)[1].split("internal static string Direction(", 1)[0]
    assert not re.search(r'job\.\w+\s*=(?!=)', projection), "UI changes job state"
    assert 'default: return job.Status;' in projection, "Unknown raw state lost"
    assert 'Cached(job.DisplayItem)' in projection and 'job.DisplayDetail' in projection
    # Every explicit production state has a display branch; the existing progress callback is raw fallback.
    codes = set(re.findall(r'job.DisplayCode = "([a-z_]+)"', producer)) | {"preparing", "cached", "cancelled", "missing", "retry", "prepare_failed", "redraw_cancelled", "redraw_complete", "redraw_failed", "image_failed"}
    assert codes - {"raw"} == set(re.findall(r'case "([a-z_]+)"', projection)), codes
    consumer = (ROOT / PREFIX / "UI/Patches/WeeklyReportPopupIllustrationPatch.cs").read_text(encoding="utf-8-sig")
    restored = consumer.replace('IllustrationStatusUiText.Bulletin(job)', 'job.Status').replace('status?.Invoke(IllustrationStatusUiText.Direction(direction) +', 'status?.Invoke((bulletin ? direction.StatusText : IllustrationStatusUiText.Direction(direction)) +')
    assert restored == old("UI/Patches/WeeklyReportPopupIllustrationPatch.cs"), "Lifecycle/storage/Prompt consumer changed"
    assert 'directorStatusText: direction.StatusText' in consumer
    assert 'IllustratorRuntime.Post(() =>' in next(line for line in consumer.splitlines() if 'status?.Invoke(IllustrationStatusUiText.Direction(' in line)
    catalog = (ROOT / PREFIX / "UI/IllustratorUiText.cs").read_text(encoding="utf-8-sig")
    assert restore_catalog(catalog) == old("UI/IllustratorUiText.cs"), "Existing catalog changed"
    lang = ROOT / "content/modules/AnimusForge.Illustrator/ModuleData/Languages"
    en = {s.get("id"): s.get("text") for s in ET.parse(lang / "illustrator_strings.xml").iter("string")}
    zh = {s.get("id"): s.get("text") for s in ET.parse(lang / "CNs/illustrator_strings-zh-CN.xml").iter("string")}
    calls = re.findall(r'(?:Resolve|Format)\("(AF_ILL_UI_BULLETIN_[^"]+)", "([^"]*)"', catalog)
    assert len(calls) == 11
    for key, fallback in calls:
        assert en[key] == fallback and zh[key].strip(), key
        assert not re.search(r'[\u4e00-\u9fff]', fallback), key
        assert sorted(re.findall(r'\{[A-Z_]+\}', en[key])) == sorted(re.findall(r'\{[A-Z_]+\}', zh[key])), key
    print("PASS bulletin static UI integration: 11 keys, known states covered, unknown errors retained, exact raw/lifecycle inverse, metadata unchanged; C#/game NOT-RUN")


if __name__ == "__main__":
    main()
