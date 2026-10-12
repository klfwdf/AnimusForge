"""Link the actual Memory display description with a localization-only fixture."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[4]


def include(files):
    files['MemoryFailureUiText.cs'] = (ROOT / 'src/modules/AF.Module.Memory/Summary/MemoryFailureUiText.cs').read_text(encoding='utf-8-sig')
    if not any('class TextObject' in source for source in files.values()):
        files['TextObjectFixture.cs'] = (Path(__file__).parent / 'TextObjectFixture.cs.txt').read_text(encoding='utf-8')
    # Historical extraction fixtures retain original Error, and must carry the
    # current adapter's additional transient description without changing that oracle.
    for name, source in list(files.items()):
        if not name.endswith('.cs'):
            continue
        source = re.sub(r'(class (?:CapturedMemorySummaryResult|MemorySummaryExecutionResult|MajorActionSummaryExecutionResult|MemoryOverviewExecutionResult)\s*\{)([^}]*)',
                        lambda match: match.group(0) if 'ErrorDisplay' in match.group(2)
                        else match.group(1)+' internal MemoryFailureUiText ErrorDisplay;'+match.group(2), source)
        files[name] = source
