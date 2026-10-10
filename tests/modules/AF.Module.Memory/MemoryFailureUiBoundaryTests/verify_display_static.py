"""Read-only localization projection review. This never builds or executes C#."""
from pathlib import Path
import ast
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
BASE = '88f8a7104'


def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig').replace('\r\n', '\n')


def prior(path):
    return subprocess.check_output(['git', 'show', BASE+':'+path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')


recall_path = 'src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs'
recall = read(recall_path)
diagnostics = lambda text: [line.strip() for line in text.splitlines() if line.strip().startswith('snapshot.RecordBlockingFailure(')]
assert diagnostics(recall) == diagnostics(prior(recall_path)), 'Original recall diagnostics changed'
assert len(diagnostics(recall)) == recall.count('snapshot.SetBlockingDisplay(') == 7
# Only a paired display statement and transient fields were added to recall.
without_display = re.sub(r'\n\s*snapshot.SetBlockingDisplay\(.*?\);', '', recall, flags=re.S)
without_display = re.sub(r'\n    internal MemoryFailureDisplay BlockingDisplay;\n    internal void SetBlockingDisplay\(.*?\n    }', '', without_display, flags=re.S)
assert without_display == prior(recall_path), 'Recall Prompt/selection/persistence logic changed'

for path, annotation, count in [
    ('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs', '[JsonIgnore]', 3),
    ('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryInput.cs', '[Newtonsoft.Json.JsonIgnore]', 1),
]:
    source = read(path)
    assert source.count(annotation+' internal MemoryFailureUiText ErrorDisplay;') == count
    restored = source.replace('\n\n\t\t'+annotation+' internal MemoryFailureUiText ErrorDisplay;', '') if count == 3 else source.replace('\n        '+annotation+' internal MemoryFailureUiText ErrorDisplay;', '')
    assert restored == prior(path), 'Unrelated result/host changes: '+path

owner = read('src/modules/AF.Module.Memory/Summary/MemoryFailureNoticeOwner.cs')
assert owner.index('if (!_port.IsMainThread()) return;') < owner.index('notice.Display?.Title.Resolve()')
for guard in ['_port.IsCurrentOwner(notice.Generation)', '_port.IsCurrentCampaign()', 'revision != Interlocked.Read(ref _memoryFailurePopupRevision)', 'Interlocked.Exchange(ref _pendingMemoryFailureNotice, null)']:
    assert guard in owner, 'Missing failure lifecycle guard: '+guard
factory = read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryQueueRun.cs')
assert factory.count('MarkFailure = result =>') == factory.count('FailureDisplayText = result =>') == 3
assert '.Resolve()' not in factory, 'Worker factory resolves game text'
run = read('src/modules/AF.Module.Memory/Summary/MemorySummaryQueueRunRuntime.cs')
assert 'await Task.Run(() => "以下日结压缩任务' in run
assert run.count('Accept(result, failures, displayFailures)') == 3
assert run.count('_ports.DelayAsync(60000)') == 2
assert 'finally { run.Dispose(); }' in run

for path in [
    'tests/modules/AF.Module.Memory/MemoryFailureUiBoundaryTests/run.py',
    'tests/modules/AF.Module.Memory/MemoryFailureUiBoundaryTests/display_fixture_support.py',
    'tests/modules/AF.Module.Memory/MemoryRecallContextTests/run.py',
    'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/verify_queue_run.py',
    'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_business.py',
    'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_terminal.py',
    'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_captured.py',
]:
    ast.parse(read(path), filename=path)

en = ET.parse(ROOT/'content/modules/AF.Module.Memory/ModuleData/Languages/afmemory_ui_strings.xml').getroot()
zh = ET.parse(ROOT/'content/modules/AF.Module.Memory/ModuleData/Languages/CNs/afmemory_ui_strings-zh-CN.xml').getroot()
def rows(root):
    values = root.findall('./strings/string')
    mapping = {row.attrib['id']: row.attrib['text'] for row in values}
    assert len(mapping) == len(values)
    return mapping
english, chinese = rows(en), rows(zh)
assert english.keys() == chinese.keys()
for key, value in english.items():
    assert sorted(re.findall(r'\{[A-Z][A-Z0-9_]*\}', value)) == sorted(re.findall(r'\{[A-Z][A-Z0-9_]*\}', chinese[key]))
display_sources = ['src/modules/AF.Module.Memory/Summary/MemoryFailureUiText.cs',
                   'src/modules/AF.Module.Memory/Summary/MemorySummaryQueueRunRuntime.cs',
                   'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemorySummaryQueueRun.cs', recall_path]
for path in display_sources:
    source = read(path)
    for key, literal in re.findall(r'(?:T|SetBlockingDisplay)\("(AF_MEMORY_UI_[^"]+)",\s*("(?:[^"\\]|\\.)*")', source):
        value = ast.literal_eval(literal)
        assert key in english and value == english[key], 'Display fallback/resource differs: '+key
        assert not re.search(r'[\u3400-\u9fff]', value), 'Chinese display fallback: '+key
print('PASS Memory display static: 7 original recall diagnostics/logic; 4 JsonIgnore transient DTO fields; lifecycle/queue guards; paired '+str(len(english))+' keys; Python test syntax. C# runtime NOT-RUN.')
