"""Exact reviewed additive Native API changes; reject any unrelated change before restoring older proof."""
from pathlib import Path
import hashlib,json,subprocess,importlib.util
ROOT=Path(__file__).resolve().parents[4]
REVIEW=json.loads((Path(__file__).parent/'source-review.json').read_text(encoding='utf-8'))
SCENE_REVIEW=json.loads((Path(__file__).parent/'scene-additive-review.json').read_text(encoding='utf-8'))
COURIER_REVIEW=json.loads((Path(__file__).parent/'courier-additive-review.json').read_text(encoding='utf-8'))
CURRENT_PATHS={
    'Refactor/Modules/CoreDialogueContracts.cs':'src/modules/AF.Module.Conversation/Internal/CoreDialogueContracts.cs',
    'Refactor/Modules/CoreDialogueOperation.cs':'src/modules/AF.Module.Conversation/Internal/CoreDialogueOperation.cs',
    'Refactor/Modules/CoreDialogueClient.cs':'src/modules/AF.Module.Conversation/Internal/CoreDialogueClient.cs',
    'Refactor/Modules/CoreDialogueServices.cs':'src/modules/AF.Module.Conversation/Internal/CoreDialogueServices.cs',
    'Api/V1/AfApiContracts.cs':'src/AF.Contracts/PublicApi/V1/AfApiContracts.cs',
    'Api/V1/AfApi.cs':'src/modules/AF.Module.PublicApi/V1/AfApi.cs',
    'Api/V1/AfDialogueClient.cs':'src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs',
    'Api/Internal/AfV1SnapshotProjection.cs':'src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs',
    'Api/Internal/AfV1DialogueProjection.cs':'src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs',
}
def current_path(old_path):
    return CURRENT_PATHS.get(old_path,old_path)

def restore_scene_additions(path,current):
    path=str(path).replace('\\','/')
    courier=COURIER_REVIEW['files'].get(path)
    if courier is not None:
        old=subprocess.check_output(['git','show',COURIER_REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        assert hashlib.sha256(old.encode()).hexdigest()==courier['beforeSha256'], 'Courier API reviewed baseline mismatch: '+path
        expected=old.splitlines(keepends=True)
        for h in reversed(courier['hunks']):
            assert ''.join(expected[h['oldStart']:h['oldEnd']])==h['before'], 'Courier API reviewed hunk mismatch: '+path
            expected[h['oldStart']:h['oldEnd']]=h['after'].splitlines(keepends=True)
        expected=''.join(expected)
        assert hashlib.sha256(expected.encode()).hexdigest()==courier['afterSha256'], 'Courier API reviewed candidate mismatch: '+path
        # Callers may already have restored the Scene/Native historical form.
        if current==expected:current=old
        elif current!=old:
            prior=subprocess.check_output(['git','show',SCENE_REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
            assert current==prior, 'Unreviewed Courier API live source: '+path
    evidence=SCENE_REVIEW['files'].get(path)
    if evidence is None:return current
    old=subprocess.check_output(['git','show',SCENE_REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    assert hashlib.sha256(old.encode()).hexdigest()==evidence['beforeSha256'], 'Scene API reviewed baseline mismatch: '+path
    if current==old:return old
    expected=old.splitlines(keepends=True)
    for h in reversed(evidence['hunks']):
        assert ''.join(expected[h['oldStart']:h['oldEnd']])==h['before'], 'Scene API reviewed hunk mismatch: '+path
        expected[h['oldStart']:h['oldEnd']]=h['after'].splitlines(keepends=True)
    expected=''.join(expected)
    assert hashlib.sha256(expected.encode()).hexdigest()==evidence['afterSha256'], 'Scene API reviewed candidate mismatch: '+path
    assert current==expected, 'Unreviewed Scene API live source: '+path
    return old
spec_owner=importlib.util.spec_from_file_location('j07b_admission_inverse',ROOT/'tests/modules/AF.Module.Conversation/NativeConversationAdmissionTests/owner_extraction.py');owner_inverse=importlib.util.module_from_spec(spec_owner);spec_owner.loader.exec_module(owner_inverse)

def restore(path,current,verify_dependencies=True,live_current=None):
    path=str(path).replace('\\','/')
    current=owner_inverse.restore(path,restore_scene_additions(path,current))
    if verify_dependencies:
        for dependency,expected_hash in REVIEW.get('dependencies',{}).items():
            source=restore_scene_additions(dependency,(ROOT/current_path(dependency)).read_text(encoding='utf-8-sig'))
            assert hashlib.sha256(owner_inverse.restore(dependency,source).encode()).hexdigest()==expected_hash, 'Unreviewed Native API dependency: '+dependency
    evidence=REVIEW['files'].get(path)
    if evidence is None: return current
    old=subprocess.check_output(['git','show',REVIEW['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    assert hashlib.sha256(old.encode()).hexdigest()==evidence['beforeSha256'], 'Native API baseline mismatch: '+path
    expected=old.splitlines(keepends=True)
    for h in reversed(evidence['hunks']):
        assert ''.join(expected[h['oldStart']:h['oldEnd']])==h['before'], 'Native API reviewed hunk mismatch: '+path
        expected[h['oldStart']:h['oldEnd']]=h['after'].splitlines(keepends=True)
    expected=''.join(expected)
    assert hashlib.sha256(expected.encode()).hexdigest()==evidence['afterSha256'], 'Native API candidate hash mismatch: '+path
    observed = live_current if live_current is not None else (ROOT/current_path(path)).read_text(encoding='utf-8-sig')
    assert owner_inverse.restore(path,restore_scene_additions(path,observed))==expected, 'Unreviewed Native API live source: '+path
    assert current in (old,expected), 'Unexpected Native API proof input: '+path
    return old
if __name__=='__main__':
    for path in COURIER_REVIEW['files']:restore_scene_additions(path,(ROOT/current_path(path)).read_text(encoding='utf-8-sig'))
    for path in REVIEW['files']:restore(path,(ROOT/current_path(path)).read_text(encoding='utf-8-sig'))
    print('PASS exact Native API inverse: '+str(len(REVIEW['files']))+' original files; '+str(len(COURIER_REVIEW['files']))+' reviewed Courier additive files; original dependency hashes retained')
