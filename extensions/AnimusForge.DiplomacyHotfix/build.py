"""Build the standalone hotfix against both API lines; optionally package CN/EN ZIPs.

This entry point never copies files to a game installation.
"""
import argparse
import datetime
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
MODULE = 'AnimusForge.DiplomacyHotfix'
DLL = MODULE + '.dll'
VERSION = '0.1.0'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--harmony', required=True, type=Path)
    parser.add_argument('--refs13', type=Path, default=ROOT / '_deps_auto')
    parser.add_argument('--refs14', type=Path, default=ROOT / '.tmp/build_check/1.4')
    parser.add_argument('--package', action='store_true')
    args = parser.parse_args()
    args.harmony = args.harmony.resolve(strict=True)
    receipt = {'module': MODULE, 'version': VERSION, 'references': {}}
    for api, refs in [('1.3', args.refs13), ('1.4', args.refs14)]:
        refs = refs.resolve(strict=True)
        paths = [refs / ('TaleWorlds.' + name + '.dll')
                 for name in ['MountAndBlade', 'Core', 'Library', 'DotNet']]
        for path in paths:
            if not path.is_file():
                raise FileNotFoundError(path)
        versions = re.findall(r'v1\.\d+\.\d+\.\d+',
                              (refs / 'TaleWorlds.Library.dll').read_bytes().decode('utf-16-le', errors='ignore'))
        versions = sorted(set(versions))
        if len(versions) != 1 or not versions[0].startswith('v' + api + '.'):
            raise RuntimeError('Wrong or ambiguous reference game version: ' + repr(versions))
        receipt['references'][api] = {'game_version': versions[0],
                                      'sha256': {p.name: digest(p) for p in paths}}
        command = ['dotnet', 'build', str(EXT / 'src' / (MODULE + '.csproj')),
                   '-c', 'Release', '-t:Rebuild', '-v:q', '-p:BannerlordApi=' + api,
                   '-p:CoreRefs=' + str(refs), '-p:HarmonyCorePath=' + str(args.harmony),
                   '-p:DebugType=None', '-p:DebugSymbols=false']
        result = subprocess.run(command, cwd=ROOT, capture_output=True)
        log = EXT / 'artifacts' / ('build-' + api + '.log')
        log.parent.mkdir(parents=True, exist_ok=True)
        log.write_bytes(result.stdout + result.stderr)
        print('Build', api, versions[0], 'exit', result.returncode, flush=True)
        if result.returncode:
            raise RuntimeError('Build failed; see ' + str(log))
    outputs = [EXT / 'artifacts' / api / DLL for api in ['1.3', '1.4']]
    if outputs[0].read_bytes() != outputs[1].read_bytes():
        raise RuntimeError('The two API builds differ; a shared DLL cannot be packaged')
    receipt['dll_sha256'] = digest(outputs[0])
    receipt['dependency_sha256'] = {
        '0Harmony.dll': digest(args.harmony),
        'Newtonsoft.Json.dll': digest(ROOT / '_deps_auto/Newtonsoft.Json.dll')}
    receipt['source_sha256'] = {str(p.relative_to(EXT)).replace('\\', '/'): digest(p)
                                for p in sorted((EXT / 'src').glob('*')) if p.is_file()}
    receipt['source_sha256']['SubModule.xml'] = digest(EXT / 'SubModule.xml')
    receipt['verification_scope'] = 'Identical Release DLL compiled against both reference API lines; live game acceptance is separate.'
    metadata = (json.dumps(receipt, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
    (EXT / 'artifacts/build-info.json').write_bytes(metadata)
    print('Shared DLL SHA256:', receipt['dll_sha256'], flush=True)
    if not args.package:
        return
    xml = ET.parse(EXT / 'SubModule.xml').getroot()
    if (xml.find('Id').get('value') != MODULE
            or xml.find('./SubModules/SubModule/DLLName').get('value') != DLL
            or xml.find('Version').get('value') != 'v' + VERSION
            or {n.get('Id') for n in xml.findall('./DependedModules/DependedModule')}
            != {'Native', 'Bannerlord.Harmony', 'AnimusForge'}):
        raise RuntimeError('Invalid module identity or dependencies')
    release = EXT / 'artifacts/packages'
    release.mkdir(parents=True, exist_ok=True)
    stamp = datetime.datetime.now().astimezone().strftime('%m%d_%H%M')
    suffix = ''
    collision = 0
    while any((release / f'AnimusForge_DiplomacyHotfix_v{VERSION}_{lang}_{stamp}{suffix}.zip').exists()
              for lang in ['CN', 'EN']):
        collision += 1
        suffix = '_' + str(collision)
    # Preserve each complete stage. Publish the pair only after both archive checks pass.
    stage = Path(tempfile.mkdtemp(prefix='stage-' + stamp + '-', dir=EXT / 'artifacts'))
    pending = []
    for lang in ['CN', 'EN']:
        folder = stage / lang / MODULE
        binary = folder / 'bin/Win64_Shipping_Client'
        binary.mkdir(parents=True)
        shutil.copyfile(outputs[0], binary / DLL)
        shutil.copyfile(EXT / 'SubModule.xml', folder / 'SubModule.xml')
        shutil.copyfile(EXT / ('README.' + lang + '.md'), folder / 'README.md')
        (folder / 'build-info.json').write_bytes(metadata)
        files = {str(p.relative_to(folder.parent)).replace('\\', '/'): p
                 for p in folder.rglob('*') if p.is_file()}
        expected = {MODULE + '/' + p for p in ['SubModule.xml', 'README.md', 'build-info.json',
                    'bin/Win64_Shipping_Client/' + DLL]}
        if set(files) != expected:
            raise RuntimeError('Unexpected stage content')
        name = f'AnimusForge_DiplomacyHotfix_v{VERSION}_{lang}_{stamp}{suffix}.zip'
        archive = stage / name
        with zipfile.ZipFile(archive, 'x', zipfile.ZIP_DEFLATED) as z:
            for entry, path in sorted(files.items()):
                z.write(path, entry)
        with zipfile.ZipFile(archive) as z:
            if set(z.namelist()) != expected or z.testzip() is not None:
                raise RuntimeError('Invalid archive closure')
            for entry, path in files.items():
                if z.read(entry) != path.read_bytes():
                    raise RuntimeError('Archive bytes differ: ' + entry)
        pending.append((archive, release / name))
    published = []
    try:
        for archive, destination in pending:
            archive.rename(destination)
            published.append(destination)
    except Exception:
        for destination in published:
            destination.unlink()
        raise
    (stage / 'package-receipt.json').write_text(json.dumps(
        {p.name: digest(p) for p in published}, indent=2) + '\n', encoding='utf-8')
    for path in published:
        print(path, digest(path), flush=True)


if __name__ == '__main__':
    main()
