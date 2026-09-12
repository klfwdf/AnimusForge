"""Keep older whole-owner proofs strict while accepting the verified Native memory capture."""
import hashlib, json, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def restore_uncompressed_source(path,source):
    if path!='ShoutBehavior.cs': return source
    review=json.loads((Path(__file__).parent/'source-review.json').read_text(encoding='utf-8'))
    prior=subprocess.check_output(['git','show',review['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig')
    def fragment(s):
        start=s.index('\t\tbool useSharedDailyMemoryForNpcOpening = npcInitiatedOpening;')
        end=s.index('\t\tstring taskSystemBlock =',start)
        return s[start:end]
    current=fragment(source)
    assert hashlib.sha256(current.encode()).hexdigest()==review['sha256'],'Unreviewed Native uncompressed call site'
    restored=source.replace(current,fragment(prior),1)
    assert restored==prior,'Unreviewed surrounding Native owner changes'
    return restored
