"""Link actual run owner/adapter into existing fixtures; never changes product conditions."""
from pathlib import Path
import re
ROOT=Path(__file__).resolve().parents[2]

def include(files, original=False):
    for name,text in list(files.items()):
        if not name.endswith('.cs'): continue
        if not original:
            # Existing fixtures used to prime the bool outside the real Process entry.
            # Actual Process now acquires its own lease atomically, as production does.
            text=re.sub(r'_memorySummaryProcessing\s*=\s*true;(?=\s*return (?:Task.Run\(\(\)=>)?ProcessMemorySummaryQueueAsync)', '',text)
            proxy='bool _memorySummaryProcessing { get => _memorySummaryRunOwner.IsRunning; set { if(value) _memorySummaryRunOwner.TryBegin(SaveRuntimeGuard.CaptureGeneration()); else _memorySummaryRunOwner.Reset(); } }'
            text=text.replace('bool _memorySummaryProcessing;',proxy)
            text=text.replace('bool _memorySummaryProcessing,busy;',proxy+' bool busy;')
        if ('MemorySummaryRunOwner' in text or 'RunMemorySummaryRun' in text) and 'using AnimusForge.Refactor.Runtime;' not in text:
            directives=[]
            while text.startswith('#define '):
                first,text=text.split('\n',1);directives.append(first)
            text='\n'.join(directives)+('\n' if directives else '')+'using AnimusForge.Refactor.Runtime;\n'+text
        if not original and name=='Program.cs':text='#define HAS_RUN_OWNER\n'+text
        # Scripted network seam accepts the explicitly forwarded lease but does not
        # implement its production retry checks (run_captured/terminal cover those).
        if name in ('Program.cs','Fixture.cs'):
            text=text.replace('List<MemoryOverviewExecutionResult> overview)', 'List<MemoryOverviewExecutionResult> overview, AnimusForge.Refactor.Runtime.MemorySummaryRunOwner.Lease run = null)')
        files[name]=text
    for path in ['MyBehavior.MemorySummaryRun.cs','Refactor/Runtime/MemorySummaryRunOwner.cs']:
        files[Path(path).name]=(ROOT/path).read_text(encoding='utf-8-sig')
