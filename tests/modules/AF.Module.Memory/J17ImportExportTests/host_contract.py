from pathlib import Path
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path
import re

root = Path(__file__).resolve().parents[4]
# Responsibility migration: inspect current actual owners, not obsolete host business bodies.
# Original guard/fingerprint/cancel/invalid JSON refusal assertions and mutation probes remain below.
source = (root / "src/AF.GameAdapter.Bannerlord/UI/Editors/MemoryEditorController.cs").read_text(encoding="utf-8-sig")
formats = (root / "src/AF.GameAdapter.Bannerlord/ImportExport/MemoryHistoryImportExportAdapter.cs").read_text(encoding="utf-8-sig")
package = (root / "src/AF.GameAdapter.Bannerlord/UI/Editors/DeveloperPackageImportController.cs").read_text(encoding="utf-8-sig")


def method(name, text=None):
    if text is None:
        text = package if name == "ImportHeroNpcAllData" else formats if name.startswith("Import") else source
    match = re.search(r'^\s*(?:private|internal)\s+[^\n]*\b' + re.escape(name) + r'\s*\(', text, re.M)
    assert match, f'missing production method: {name}'
    following = re.search(r'^\s*(?:private|internal)\s+[^\n]*\(', text[match.end():], re.M)
    body = text[match.start():match.end() + following.start()] if following else text[match.start():]
    if name == "ImportHeroNpcAllData":
        # The actual package invokes this complete format scan; do not count an unrelated unused helper.
        assert "_memoryFiles.PrepareCompressedDirectory(importDir,out num3,out num4,out invalidMemoryFiles)" in body
        body += method("PrepareCompressedDirectory", formats)
    return body


def callback_guard(body, call, fingerprint=False):
    before = body[:body.index(call)]
    callback = before[before.rfind('delegate'):]
    return ('IsMemorySourceEditorCurrent(editorGeneration)' in callback
            and (not fingerprint or 'ComputeMemorySummaryFingerprint' in callback))


def cancel_guard(body):
    return 'IsMemorySourceEditorCurrent(editorGeneration)' in body[body.rfind('}, delegate'):]


def invalid_scan(body):
    return 'ReadJson<CompressedMemoryExportBundle>' in body and 'invalidMemoryFiles++' in body


guarded = [
    ('OpenDevMemoryOverviewEditor', 'ApplyDevMemoryOverviewInput', True),
    ('OpenDevCompressedMemoryBlockTitleEditor', 'ApplyDevCompressedMemoryBlockMutation', True),
    ('OpenDevCompressedMemoryBlockSummaryEditor', 'ApplyDevCompressedMemoryBlockMutation', True),
    ('OpenDevCompressedMemoryBlockScenesEditor', 'ApplyDevCompressedMemoryBlockMutation', True),
    ('OpenDevCompressedMemoryBlockAfefEditor', 'ApplyDevCompressedMemoryBlockMutation', True),
    ('ConfirmDevDeleteCompressedMemoryBlock', 'DeleteDevCompressedMemoryBlock', True),
    ('ConfirmDevClearCompressedMemory', 'ClearDevCompressedMemoryData', False),
]
clear_root = (root / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryDeveloperDailyEdit.cs").read_text(encoding="utf-8-sig")
assert "MemoryDeveloperEditOwner.ClearMemoryWithHistorySync(_memoryBusinessState" in clear_root, 'clear capability disconnected from sole Memory authority'
checks = 0
for name, call, fingerprint in guarded:
    body = method(name)
    assert 'SaveRuntimeGuard.CaptureGeneration()' in body, f'{name}: generation not captured when opening'
    assert callback_guard(body, call, fingerprint), f'{name}: stale callback reaches {call}'
    checks += 1
    # A source mutation that removes the callback guard must be rejected by this contract.
    call_at = body.index(call)
    delegate_at = body.rfind('delegate', 0, call_at)
    mutated = body[:delegate_at] + body[delegate_at:call_at].replace(
        'IsMemorySourceEditorCurrent(editorGeneration)', 'false') + body[call_at:]
    assert not callback_guard(mutated, call, fingerprint), f'{name}: guard mutation survived'
    checks += 1
    assert cancel_guard(body), f'{name}: stale cancel callback reopens editor'
    checks += 1
    assert not cancel_guard(body[:body.rfind('}, delegate')] + body[body.rfind('}, delegate'):].replace(
        'IsMemorySourceEditorCurrent(editorGeneration)', 'false')), f'{name}: cancel guard mutation survived'
    checks += 1

navigation = [
    ('OpenDevCompressedMemoryMenu', 'OnDevCompressedMemoryMenuSelected'),
    ('OpenDevDailyMemoryDraftList', 'OpenDevDailyMemoryDraftEditor'),
    ('OpenDevDailyMemoryDraftEditor', 'OpenDevDailyMemoryLineList'),
    ('OpenDevCompressedMemoryBlockList', 'OpenDevCompressedMemoryBlockEditor'),
    ('OpenDevCompressedMemoryBlockEditor', 'OpenDevCompressedMemoryBlockSummaryEditor'),
]
for name, target in navigation:
    body = method(name)
    assert 'SaveRuntimeGuard.CaptureGeneration()' in body, f'{name}: stale navigation can recapture new generation'
    assert callback_guard(body, target), f'{name}: stale selection can navigate to {target}'
    assert cancel_guard(body), f'{name}: stale cancel can reopen editor'
    checks += 3

for name, target in (('OpenDevDailyMemoryDraftEditor', 'OpenDevDailyMemoryLineList'),
                     ('OpenDevCompressedMemoryBlockEditor', 'OpenDevCompressedMemoryBlockSummaryEditor')):
    body = method(name)
    callback = body[:body.index(target)]
    callback = callback[callback.rfind('delegate'):]
    assert 'ReferenceEquals' in callback and 'ComputeMemorySummaryFingerprint' in callback, f'{name}: stale same-generation selection can retarget replacement'
    checks += 1

view = method('ShowDevCompressedMemoryText')
assert 'SaveRuntimeGuard.CaptureGeneration()' in view and callback_guard(view, 'OpenDevCompressedMemoryMenu'), 'stale read-only view can reopen editor'
checks += 1

for name in ('ImportHeroNpcAllData', 'ImportDialogueHistoryData'):
    body = method(name)
    assert invalid_scan(body), f'{name}: invalid JSON is silently skipped'
    assert '导入部分完成' in body or '部分导入完成' in body, f'{name}: partial import is reported as complete'
    assert not invalid_scan(body.replace('invalidMemoryFiles++', '/* removed */')), f'{name}: invalid-file mutation survived'
    checks += 3

single = method('ImportSingleNpcDialogueHistoryData')
assert re.search(r'if\s*\(bundle\s*==\s*null\)', single), 'single-NPC malformed JSON reaches Apply'
assert re.search(r'if\s*\(ApplyCompressedMemoryExportBundle\(', single), 'single-NPC Apply failure is reported as success'
checks += 2

dedicated = method('ImportDialogueHistoryData')
assert re.search(r'files\.Length\s*>\s*0[^\n]*dict\.Count\s*==\s*0|dict\.Count\s*==\s*0[^\n]*files\.Length\s*>\s*0', dedicated), 'all-invalid dedicated import has no refusal'
checks += 1

print(f'PASS {checks}')
