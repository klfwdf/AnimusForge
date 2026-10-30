"""Package the already verified standalone ReplyTool stage as a CN/EN pair.

Does not rebuild, bump the module version, call a provider, or deploy game files.
"""
from datetime import datetime, timedelta, timezone
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile

EXT = Path(__file__).resolve().parent
ROOT = EXT.parents[1]
MODULE = 'AnimusForge.ReplyTool'
DLL = MODULE + '.dll'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    source = Path((EXT / 'artifacts/latest-stage.txt').read_text(encoding='utf-8').strip()).resolve(strict=True)
    if not source.is_relative_to((EXT / 'artifacts').resolve()):
        raise RuntimeError('Stage must be within this module artifacts directory')
    receipt = json.loads((EXT / 'artifacts/verification.json').read_text(encoding='utf-8'))
    for name, expected in receipt['source_sha256'].items():
        if digest(EXT / name) != expected:
            raise RuntimeError('Source differs from verified build: ' + name)
    manifest = ET.parse(EXT / 'SubModule.xml').getroot()
    version = manifest.find('Version').get('value')
    if not re.fullmatch(r'v\d+\.\d+\.\d+', version) or version != 'v' + receipt['version']:
        raise RuntimeError('Manifest/build version mismatch')
    if (manifest.find('Id').get('value') != MODULE
            or manifest.find('./SubModules/SubModule/DLLName').get('value') != DLL
            or manifest.find('./SubModules/SubModule/SubModuleClassType').get('value') != MODULE + '.SubModule'
            or {node.get('Id') for node in manifest.findall('./DependedModules/DependedModule')}
            != {'Native', 'Bannerlord.Harmony', 'AnimusForge'}):
        raise RuntimeError('Invalid standalone module identity/dependencies')
    if set(receipt.get('tests', {})) != {'1.3', '1.4'} or any(not value.startswith('PASS ') for value in receipt['tests'].values()):
        raise RuntimeError('Both API test results are required')
    for api in ['1.3', '1.4']:
        if digest(EXT / f'artifacts/{api}/{DLL}') != receipt['dll_sha256']:
            raise RuntimeError('Dual-API binary differs from verified shared DLL')
    allowed_stage = {'SubModule.xml', 'README.md', 'verification.json', 'ModuleData/ReplyToolPrompt.json',
                     'bin/Win64_Shipping_Client/' + DLL}
    for language in ['CN', 'EN']:
        folder = source / language / MODULE
        if {p.relative_to(folder).as_posix() for p in folder.rglob('*') if p.is_file()} != allowed_stage:
            raise RuntimeError('Unexpected complete stage content: ' + language)
        if digest(folder / 'bin/Win64_Shipping_Client' / DLL) != receipt['dll_sha256']:
            raise RuntimeError('Stage DLL differs from verified DLL')
        for staged, original in [('SubModule.xml', EXT / 'SubModule.xml'),
                                 ('README.md', EXT / f'README.{language}.md'),
                                 ('ModuleData/ReplyToolPrompt.json', EXT / f'content/ReplyToolPrompt.{language}.json'),
                                 ('verification.json', EXT / 'artifacts/verification.json')]:
            if (folder / staged).read_bytes() != original.read_bytes():
                raise RuntimeError('Stage projection mismatch: ' + language + '/' + staged)

    stamp = datetime.now(timezone(timedelta(hours=8))).strftime('%m%d_%H%M')
    release = EXT / 'artifacts/packages'
    release.mkdir(exist_ok=True)
    suffix = ''
    collision = 0
    while any((release / f'AnimusForge_ReplyTool_{version}_{lang}_{stamp}{suffix}.zip').exists() for lang in ['CN', 'EN']):
        collision += 1
        suffix = '_' + str(collision)
    stage = Path(tempfile.mkdtemp(prefix='package-' + stamp + '-', dir=EXT / 'artifacts'))
    revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    pending = []
    archives = {}
    for language in ['CN', 'EN']:
        folder = stage / language / MODULE
        for relative in sorted(allowed_stage - {'verification.json'}):
            destination = folder / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source / language / MODULE / relative, destination)
        public_info = {'module': MODULE, 'version': receipt['version'], 'content_language': language,
                       'source_revision': revision, 'dll_sha256': receipt['dll_sha256'],
                       'reference_game_versions': {api: item['game_version'] for api, item in receipt['references'].items()},
                       'offline_tests': receipt['tests'], 'live_game': 'NOT-RUN',
                       'long_reply_anti_truncation_effect': 'NOT-VERIFIED',
                       'image_generation_integration': 'Gemini chat input wrapping; native image/base64 consumed by AF',
                       'image_generation_live_game': 'NOT-RUN'}
        (folder / 'build-info.json').write_text(json.dumps(public_info, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
        files = {MODULE + '/' + p.relative_to(folder).as_posix(): p for p in folder.rglob('*') if p.is_file()}
        expected = {MODULE + '/' + p for p in allowed_stage - {'verification.json'} | {'build-info.json'}}
        if set(files) != expected:
            raise RuntimeError('Unexpected package closure')
        for entry, path in files.items():
            if path.suffix in ['.json', '.md', '.xml'] and re.search(rb'\bsk-[A-Za-z0-9_-]{20,}', path.read_bytes()):
                raise RuntimeError('Credential-like data rejected: ' + entry)
        filename = f'AnimusForge_ReplyTool_{version}_{language}_{stamp}{suffix}.zip'
        archive = stage / filename
        with zipfile.ZipFile(archive, 'x', zipfile.ZIP_DEFLATED) as output:
            for entry, path in sorted(files.items()):
                output.write(path, entry)
        with zipfile.ZipFile(archive) as check:
            if check.testzip() is not None or set(check.namelist()) != expected:
                raise RuntimeError('Archive CRC/closure failure')
            for entry, path in files.items():
                if check.read(entry) != path.read_bytes():
                    raise RuntimeError('Archive byte mismatch: ' + entry)
        destination = release / filename
        pending.append((archive, destination))
        archives[language] = {'path': str(destination), 'sha256': digest(archive), 'bytes': archive.stat().st_size,
                             'files': {entry: digest(path) for entry, path in sorted(files.items())}}
    published = []
    try:
        for archive, destination in pending:
            archive.rename(destination)
            published.append(destination)
    except Exception:
        for destination in published:
            if destination.parent.resolve() != release.resolve():
                raise RuntimeError('Unexpected cleanup target')
            destination.unlink()
        raise
    record = {'module': MODULE, 'version': receipt['version'], 'source_stage': str(source),
              'source_revision': revision, 'package_script_sha256': digest(Path(__file__)),
              'time_zone': 'Asia/Shanghai', 'archives': archives, 'deployment_performed': False,
              'new_model_requests': 0, 'validation': 'CN/EN closure, projections, CRC and every entry byte checked'}
    (stage / 'package-receipt.json').write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    (release / f'package-receipt_{stamp}{suffix}.json').write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for language, item in archives.items():
        print(language, item['path'], item['sha256'], item['bytes'], flush=True)
    print('PASS: both archives verified before publication; no AF/game files changed; no model requests')


if __name__ == '__main__':
    main()
