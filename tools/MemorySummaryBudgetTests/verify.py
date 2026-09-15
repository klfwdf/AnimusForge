"""Validate exact reviewed writer diff and independent baseline/current source hashes."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent

def main():
    review=json.loads((HERE/'writer-review.json').read_text(encoding='utf-8'))
    for path,hunks in review['paths'].items():
        old=subprocess.check_output(['git','show',review['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        lines=old.splitlines(keepends=True)
        for h in reversed(hunks):
            assert ''.join(lines[h['start']:h['end']])==h['before'],'Before hunk differs'
            lines[h['start']:h['end']]=[h['after']]
        actual=(ROOT/path).read_text(encoding='utf-8-sig')
        assert actual==''.join(lines),'Unreviewed writer changes'
        assert hashlib.sha256(old.encode()).hexdigest()==review['beforeSha256']
        assert hashlib.sha256(actual.encode()).hexdigest()==review['afterSha256']
    gen=HERE/'.generated'
    old_hashes=json.loads((gen/('writer-'+review['baseline'])/'source-hashes.json').read_text())
    new_hashes=json.loads((gen/'current/source-hashes.json').read_text())
    assert len(old_hashes)==13 and old_hashes==new_hashes,'Real source digest parity failed'
    for name in [('writer-'+review['baseline']),'current']:
        assert 'BUDGET_RESULT pass=65808 fail=0' in (gen/name/'run.log').read_text(encoding='utf-8')
    print('BUDGET_REVIEW_PASS exact_hunks=4 real_source_digests=13 checks_per_variant=65808')
if __name__=='__main__':main()
