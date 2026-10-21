"""Build a local-evaluation U add-on ZIP; no AF DLLs, game writes, downloads or uploads.
Copies trusted, already-built payloads only. Verifies every archived byte after relocation.
"""
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import json
import re
import shutil
import subprocess
import zipfile

CHUNK = 4 * 1024 * 1024

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(CHUNK), b''): h.update(block)
    return h.hexdigest()

def dump(value):
    return (json.dumps(value, ensure_ascii=False, indent=2) + '\n').encode('utf-8')

def child(root, relative):
    p = PurePosixPath(relative)
    if p.is_absolute() or not p.parts or any(x in ('', '.', '..') or ':' in x or '\\' in x for x in p.parts):
        raise ValueError('unsafe_relative_path')
    path = root.joinpath(*p.parts)
    path.resolve().relative_to(root.resolve())
    for node in [path, *path.parents]:
        if node == root.parent: break
        if node.exists() and node.stat().st_file_attributes & 0x400: raise ValueError('reparse_path_rejected')
    return path

def reference(entry, manifest_root):
    # Portable bundle inputs are flattened by build_bundle.py, no inference_reference here.
    if 'inference_reference' in entry: raise ValueError('build_portable_bundle_first')
    result = {k: entry[k] for k in ('reference_file', 'reference_sha256', 'prompt_lang', 'prompt_text')}
    p = child(manifest_root, result['reference_file'])
    if not p.is_file() or not 0 < p.stat().st_size <= 8 * 1024 * 1024: raise ValueError('reference_size')
    if sha(p).lower() != result['reference_sha256'].lower(): raise ValueError('reference_sha256')
    if result['prompt_lang'] not in ('zh','en','ja','yue'): raise ValueError('unsupported_U_reference_language')
    if not 0 < len(result['prompt_text']) <= 1000: raise ValueError('transcript_size')
    import soundfile as sf
    info = sf.info(p)
    if info.format != 'WAV' or info.channels != 1 or not 3 <= info.duration <= 10: raise ValueError('U_mono_WAV_3_to_10_seconds_required')
    return result, p

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--module', type=Path, required=True, help='Trusted existing lab module (read-only)')
    parser.add_argument('--output', type=Path, required=True, help='New local evaluation .zip')
    parser.add_argument('--verify-extract', type=Path, required=True, help='New verification directory, never the game')
    args = parser.parse_args()
    module = args.module.resolve(strict=True)
    output, extract = args.output.resolve(), args.verify_extract.resolve()
    if output.exists() or extract.exists() or module in output.parents or module in extract.parents or extract in output.parents:
        raise ValueError('fresh_separate_output_and_verification_required')
    if output.suffix.lower() != '.zip': raise ValueError('zip_required')
    runtime = child(module, 'AF-GPTSOVITS-U')
    metadata = json.loads((runtime/'runtime.json').read_text(encoding='utf-8-sig'))
    if metadata['package_id'] != 'AF-GPTSOVITS-U' or metadata['af_runtime_api'] != 1 or metadata['engine'] != 'gpt-sovits-v5': raise ValueError('runtime_identity')
    if metadata.get('redistribution_cleared') is not False: raise ValueError('this_tool_is_local_evaluation_only')
    for rel, digest in metadata['files'].items():
        if sha(child(runtime, rel)) != digest.lower(): raise ValueError('runtime_hash:'+rel)
    for name in ('server.py','ultra_engine.py','check_runtime.py'):
        if (runtime/name).read_text(encoding='utf-8-sig') != (Path(__file__).parent/name).read_text(encoding='utf-8-sig'): raise ValueError('runtime_source_mismatch:'+name)
    subprocess.run([str(runtime/'runtime/python.exe'), '-B', str(runtime/'check_runtime.py'), '--bundle', str(runtime)], check=True)
    entries = {}
    for p in sorted(runtime.rglob('*')):
        if p.is_dir(): continue
        rel = p.relative_to(module).as_posix()
        child(module, rel)
        if '__pycache__' in p.parts or p.suffix == '.pyc': continue
        if '.git' in p.parts or p.name.lower() in ('.env','credentials.json'): raise ValueError('unexpected_private_file')
        entries['AnimusForge/'+rel] = p
    voices = child(module, 'VoicePacks')
    profiles = []; aliases = {}; counts = {'male':0,'female':0,'custom':0}; ids = set()
    for category in ('Male','Female','Custom'):
        for manifest in sorted((voices/category).glob('*/manifest.json')):
            document = json.loads(manifest.read_text(encoding='utf-8-sig'))
            normalized = []
            for source in document['profiles']:
                voice = {k:source[k] for k in ('id','label','style_target','category')}
                if source['id'] in ids or not 0 < len(source['id']) <= 128 or source['id'].strip() != source['id']: raise ValueError('voice_id')
                ids.add(source['id'])
                gender = source['speaker_metadata']['gender']
                if gender not in ('male','female') or voice['category'] != category.lower() or (category != 'Custom' and gender != category.lower()): raise ValueError('category_mismatch')
                voice['speaker_metadata'] = {'gender':gender}
                voice['usage'] = 'local_evaluation_redistribution_not_cleared'
                voice['source_notice'] = 'Existing character reference; original recording rights are not cleared for redistribution.'
                ref, path = reference(source, manifest.parent); voice.update(ref)
                entries['AnimusForge/VoicePacks/'+path.relative_to(voices).as_posix()] = path
                if source.get('emotion_variants'):
                    voice['emotion_variants'] = {}
                    for tone, item in source['emotion_variants'].items():
                        if tone not in ('neutral','happy','angry','sad'): raise ValueError('tone_invalid')
                        ref, path = reference(item, manifest.parent); voice['emotion_variants'][tone] = ref
                        entries['AnimusForge/VoicePacks/'+path.relative_to(voices).as_posix()] = path
                normalized.append(voice)
                flat = json.loads(json.dumps(voice)); prefix = manifest.parent.relative_to(voices).as_posix()+'/'
                flat['reference_file'] = prefix+flat['reference_file']
                for item in flat.get('emotion_variants',{}).values(): item['reference_file'] = prefix+item['reference_file']
                profiles.append(flat); counts[category.lower()] += 1
            for alias,target in document.get('aliases',{}).items():
                if alias in aliases and aliases[alias] != target: raise ValueError('alias_conflict')
                aliases[alias] = target
            entries['AnimusForge/VoicePacks/'+manifest.relative_to(voices).as_posix()] = dump({'schema_version':1,'profiles':normalized,'aliases':document.get('aliases',{})})
    if counts != {'male':5,'female':5,'custom':1}: raise ValueError('expected_current_5m5f_1custom')
    entries['AnimusForge/VoicePacks/manifest.json'] = dump({'schema_version':1,'profiles':profiles,'aliases':aliases,'usage':'local_evaluation_redistribution_not_cleared'})
    docs = 'AnimusForge/AF-GPTSOVITS-U/Documentation/'
    entries[docs+'User-Guide.html'] = Path(__file__).parent/'User-Guide.html'
    for name in ('manifest.example.json','AF-GPTSOVITS-U.zh-CN.md','AF-GPTSOVITS-U.en.md'): entries[docs+name] = Path(__file__).resolve().parents[2]/'docs/tts'/name
    entries[docs+'READ-ME-FIRST.txt'] = ('AF-GPTSOVITS-U local evaluation add-on / 本地评估附加包\n\n'
      '解压到游戏 Modules，合并 AnimusForge；不含 AF DLL，不会更新主模组。先备份已有 VoicePacks 中自己的改动；不要盲目覆盖同名自定义包。\n'
      'Extract into Modules, merging AnimusForge. No AF DLLs included. Back up edits in VoicePacks before merging; do not overwrite your own same-ID custom packs blindly.\n'
      'Open Documentation/User-Guide.html. AF must explicitly support U engine gpt-sovits-v5 (runtime API 1); older L-capable AF builds do not. No U public download exists yet.\n'
      '主程序新说明按钮需单独更新本轮 AF 编译候选；本附加包不能提供主程序更新。\n'
      'Windows x64 / compatible NVIDIA CUDA driver required by this bundled torch build; other hardware not verified.\n'
      'U 为 V5 Dev 32步原生48kHz音质优先版，不沿用L的2GiB目标；启动要求至少6GiB空闲显存，8GB游戏同开未验证。\n'
      'U uses V5 Dev / 32 steps / native 48kHz. It requires 6GiB free VRAM at startup; no 8GB concurrent gaming guarantee.\n'
      'LOCAL EVALUATION ONLY. Recording/weight redistribution is not cleared. Do not upload this ZIP to GitHub or Nexus before a separate license review.\n'
      '仅本地评估，角色录音/权重分发授权未核实；许可审核前勿上传 GitHub、N 网。\n').encode('utf-8-sig')
    output.parent.mkdir(parents=True, exist_ok=True)
    checks = {}; total = 0
    with zipfile.ZipFile(output, 'x', zipfile.ZIP_DEFLATED, compresslevel=3, allowZip64=True) as archive:
        for i, (name, value) in enumerate(sorted(entries.items()),1):
            digest = hashlib.sha256(); size = 0
            with archive.open(name, 'w', force_zip64=True) as target:
                if isinstance(value, bytes): target.write(value); digest.update(value); size=len(value)
                else:
                    with value.open('rb') as source:
                        for data in iter(lambda:source.read(CHUNK), b''): target.write(data);digest.update(data);size+=len(data)
            checks[name] = {'sha256':digest.hexdigest(),'bytes':size}; total += size
            if i % 3000 == 0: print('Archived',i,'files;',round(total/1024**3,3),'GiB',flush=True)
        index = {'purpose':'local_evaluation_addon_no_AF_DLL','voice_counts':counts,'files':checks}
        archive.writestr(docs+'archive-files.json',dump(index))
    print('ZIP written; verifying every extracted byte and ZIP CRC...',flush=True)
    extract.mkdir(parents=True)
    with zipfile.ZipFile(output) as archive:
        for entry in archive.infolist():
            target = child(extract, entry.filename); target.parent.mkdir(parents=True, exist_ok=True)
            digest=hashlib.sha256();size=0
            with archive.open(entry) as source, target.open('xb') as dest:
                for data in iter(lambda: source.read(CHUNK),b''):dest.write(data);digest.update(data);size+=len(data)
            if entry.filename in checks and checks[entry.filename] != {'sha256':digest.hexdigest(),'bytes':size}: raise ValueError('archive_roundtrip_mismatch')
    if (extract/'AnimusForge/SubModule.xml').exists() or (extract/'AnimusForge/bin').exists() or (extract/'AnimusForge/ModuleData').exists(): raise ValueError('AF_main_files_forbidden')
    result={'status':'ARCHIVE_ROUNDTRIP_VERIFIED_NOT_GAME_TESTED','zip':str(output),'zip_bytes':output.stat().st_size,'zip_sha256':sha(output),'payload_bytes':total,'files':len(entries),'voice_counts':counts,'extract':str(extract),'redistribution_cleared':False,'game_deployed':False}
    output.with_suffix('.receipt.json').write_bytes(dump(result)); print(json.dumps(result,ensure_ascii=False),flush=True)

if __name__ == '__main__': main()
