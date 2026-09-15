from pathlib import Path
import subprocess,json,hashlib
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent

def restore(source):
    review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
    old=subprocess.check_output(['git','show',review['baseline']+':'+review['path']],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    assert hashlib.sha256(old.encode()).hexdigest()==review['originalNormalizedSha256']
    expected=old
    for before,after in review['edits']:
        assert expected.count(before)==1,'Courier outcome edit is not exact';expected=expected.replace(before,after,1)
    assert source==expected,'Unreviewed Courier outcome or surrounding change'
    return old
if __name__=='__main__':
    restore((ROOT/'CourierDeliveryBehavior.CommitDispatch.cs').read_text(encoding='utf-8-sig'));print('PASS Courier unconfirmed outcome exact whole-file inverse')
