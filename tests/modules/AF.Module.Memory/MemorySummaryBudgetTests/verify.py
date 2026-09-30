"""Validate exact reviewed writer diff and independent baseline/current source hashes."""
from pathlib import Path
import argparse,hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
HISTORICAL_WRITER='Refactor/Runtime/MemorySourceFingerprintWriter.cs'

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--current-run',type=Path,required=True,help='Fresh current Budget runner output directory')
    parser.add_argument('--baseline-run',type=Path,required=True,help='Fresh historical-writer Budget runner output directory')
    args=parser.parse_args()
    current=args.current_run.resolve(strict=True)
    baseline=args.baseline_run.resolve(strict=True)
    if not current.is_relative_to(ROOT) or not baseline.is_relative_to(ROOT) or current==baseline:
        parser.error('Both runs must be distinct repository-local directories')
    review=json.loads((HERE/'writer-review.json').read_text(encoding='utf-8'))
    for path,hunks in review['paths'].items():
        assert path=='src/modules/AF.Module.Memory/Summary/MemorySourceFingerprintWriter.cs','Unexpected writer path'
        old=subprocess.check_output(['git','show',review['baseline']+':'+HISTORICAL_WRITER],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        lines=old.splitlines(keepends=True)
        for h in reversed(hunks):
            assert ''.join(lines[h['start']:h['end']])==h['before'],'Before hunk differs'
            lines[h['start']:h['end']]=[h['after']]
        actual=(ROOT/path).read_text(encoding='utf-8-sig')
        assert actual==''.join(lines),'Unreviewed writer changes'
        assert hashlib.sha256(old.encode()).hexdigest()==review['beforeSha256']
        assert hashlib.sha256(actual.encode()).hexdigest()==review['afterSha256']
    current_manifest=json.loads((current/'manifest.json').read_text(encoding='utf-8'))
    baseline_manifest=json.loads((baseline/'manifest.json').read_text(encoding='utf-8'))
    assert current_manifest['writer_baseline'] is None and current_manifest['mutation'] is None
    assert baseline_manifest['writer_baseline']==review['baseline'] and baseline_manifest['mutation'] is None
    assert current_manifest['head']==baseline_manifest['head'] and current_manifest['source_sha256']==baseline_manifest['source_sha256'],'Run inputs differ'
    old_hashes=json.loads((baseline/'source-hashes.json').read_text(encoding='utf-8'))
    new_hashes=json.loads((current/'source-hashes.json').read_text(encoding='utf-8'))
    assert len(old_hashes)==13 and old_hashes==new_hashes,'Real source digest parity failed'
    for run in (baseline,current):
        assert 'BUDGET_RESULT pass=65808 fail=0' in (run/'run.log').read_text(encoding='utf-8')
    print('BUDGET_REVIEW_PASS exact_hunks=4 real_source_digests=13 checks_per_variant=65808')
if __name__=='__main__':main()
