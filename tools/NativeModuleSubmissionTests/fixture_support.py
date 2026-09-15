"""Share the actual operation dependency with prior Native owner fixtures; no method substitutions."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def include_operation_sources(out):
    for name in ['CoreDialogueContracts.cs','CoreDialogueOperation.cs']:
        (out/name).write_text((ROOT/'Refactor/Modules'/name).read_text(encoding='utf-8-sig'),encoding='utf-8')
    (out/'CoreDialogueUsing.cs').write_text('global using AnimusForge.Refactor.Modules;\n',encoding='utf-8')
