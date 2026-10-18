"""Focused packaging safety/template checks, not a model or game acceptance test."""
from pathlib import Path
import hashlib
import json
import sys
import wave
import package_addon as package

root = Path(sys.argv[1]).resolve()
root.mkdir(parents=True, exist_ok=False)
checks = 0

def check(condition):
    global checks
    assert condition
    checks += 1

def rejected(fn):
    try: fn()
    except (ValueError, FileNotFoundError): check(True); return
    raise AssertionError('Expected input rejection')

for name in ('../escape.wav', '/escape.wav', 'C:/escape.wav', 'folder/../../escape.wav', 'folder\\escape.wav', ''):
    rejected(lambda name=name: package.child(root, name))
check(package.child(root, 'tones/angry.wav') == root/'tones/angry.wav')
recording = root/'reference.wav'
with wave.open(str(recording), 'wb') as stream:
    stream.setnchannels(1); stream.setsampwidth(2); stream.setframerate(16000)
    stream.writeframes(b'\0\0' * (16000 * 4))
example = json.loads((Path(__file__).parent/'manifest.example.json').read_text(encoding='utf-8'))
profile = example['profiles'][0]
rejected(lambda: package.reference(profile, root))
profile['reference_sha256'] = package.sha(recording)
profile['prompt_text'] = '这是结构测试，不是角色语音。'
profile.pop('emotion_variants')
ref, path = package.reference(profile, root)
check(path == recording and ref['reference_sha256'] == hashlib.sha256(recording.read_bytes()).hexdigest())
for values in ({'reference_file':'../outside.wav'}, {'prompt_lang':'ko'}, {'prompt_text':''}, {'prompt_text':'x'*1001}, {'reference_sha256':'0'*64}, {'inference_reference':{}}):
    rejected(lambda values=values: package.reference(dict(profile, **values), root))
(root/'manifest.json').write_bytes(package.dump(example))
check(json.loads((root/'manifest.json').read_text(encoding='utf-8')) == example)
print(f'PASS {checks} package boundary/template assertions; generated manifest uses fixture silence only')
