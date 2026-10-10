"""Prove the Courier failure slice changes only its reviewed UI expressions."""
from pathlib import Path
import ast
import re
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
COURIER = "src/modules/AF.Module.Conversation/Channels/Courier/"
BASELINE = "ad623a06d"
CHANGES = {
    "CourierDeliveryBehavior.GenerationLifecycle.cs": [
        ('NonBlockingErrorReport.Show("信使回信生成失败", "信使回信正文生成失败：\\n\\n" + (error ?? "未知错误"));',
         'NonBlockingErrorReport.Show(CourierFailureUiText.ReplyFailedTitle, CourierFailureUiText.ReplyFailedDetail(error));'),
        ('"信使回信生成失败",\n', 'CourierFailureUiText.ReplyFailedTitle,\n'),
        ('NonBlockingErrorReport.Show("信使回信已停止", "本次回信未能安全完成，系统不会自动重试可能已生效的动作。请查看日志并核对实际结果。");',
         'NonBlockingErrorReport.Show(CourierFailureUiText.ReplyStoppedTitle, CourierFailureUiText.ReplyStoppedDetail);'),
        ('NonBlockingErrorReport.Show("信使来信生成失败", "信使来信正文生成失败：\\n\\n" + (error ?? "未知错误"));',
         'NonBlockingErrorReport.Show(CourierFailureUiText.LetterFailedTitle, CourierFailureUiText.LetterFailedDetail(error));'),
        ('"信使来信生成失败",\n', 'CourierFailureUiText.LetterFailedTitle,\n'),
        ('"重试",\n', 'CourierFailureUiText.Retry,\n'),
        ('"放弃",\n', 'CourierFailureUiText.Abort,\n'),
    ],
    "CourierDeliveryBehavior.InboundCompletion.cs": [
        ('"信使来信处理失败，信使已安全终止，未交付信件。",', 'CourierFailureUiText.InboundAborted,'),
    ],
}

for name, changes in CHANGES.items():
    path = COURIER + name
    baseline = subprocess.check_output(["git", "show", BASELINE + ":" + path], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
    current = (ROOT / path).read_text(encoding="utf-8-sig")
    for before, after in changes:
        count = 2 if after in ("CourierFailureUiText.Retry,\n", "CourierFailureUiText.Abort,\n") else 1
        assert current.count(after) == count, (name, after)
        current = current.replace(after, before)
    assert current == baseline, "Unexpected non-UI source change: " + name

helper = (ROOT / COURIER / "CourierFailureUiText.cs").read_text(encoding="utf-8-sig")
texts = re.findall(r"\{=([^}]+)\}([^\"\n]+)", helper)
assert len(texts) == 10 and len({key for key, _ in texts}) == 10
assert all(not re.search(r"[\u4e00-\u9fff]", fallback) for _, fallback in texts)
assert 'error ?? new LocalizedTextObject' in helper and 'error.Trim' not in helper
assert 'using LocalizedTextObject = TaleWorlds.Localization.TextObject;' in helper
runner = (HERE / "run_liveness.py").read_text(encoding="utf-8-sig")
ast.parse(runner)
assert "Channels/Courier/CourierFailureUiText.cs" in runner
assert "HERE/'CourierLocalizationBoundary.cs.txt'" in runner
boundary = (HERE / "CourierLocalizationBoundary.cs.txt").read_text(encoding="utf-8-sig")
assert "AnimusForge.NativeHost.Game();" in boundary
print("PASS Courier failure UI exact inverse; raw diagnostics, retryability, callbacks, session/facts/history unchanged; 10 fallback keys; current fixture closure maintained")
