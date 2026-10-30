"""Build and verify only this independent submodule. Never writes to the game."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET

EXT = Path(__file__).resolve().parent
ROOT = EXT.parents[1]
MODULE = 'AnimusForge.ReplyTool'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def execute(command, logfile):
    result = subprocess.run(command, cwd=ROOT, capture_output=True)
    logfile.parent.mkdir(parents=True, exist_ok=True)
    logfile.write_bytes(result.stdout + result.stderr)
    if result.returncode:
        raise RuntimeError(f'Exit {result.returncode}; see {logfile}')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-root', required=True, type=Path)
    parser.add_argument('--refs13', type=Path, default=ROOT / '_deps_auto')
    args = parser.parse_args()
    game = args.game_root.resolve(strict=True)
    harmony = game / 'Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client/0Harmony.dll'
    version = ET.parse(game / 'Modules/Native/SubModule.xml').find('Version').get('value')
    if not version.startswith('v1.4.'):
        raise RuntimeError('A verified 1.4 game reference line is required')
    hosts = {api: game / f'Modules/AnimusForge/bin/Win64_Shipping_Client/versions/{api}/AnimusForge.dll'
             for api in ['1.3', '1.4']}
    before = {api: digest(path) for api, path in hosts.items()}
    receipt = {'module': MODULE, 'version': '0.1.2', 'af_sha256_before': before, 'references': {},
               'live_provider': 'NOT-RUN', 'live_game': 'NOT-RUN', 'deployment': 'NOT-RUN'}
    for api, refs in [('1.3', args.refs13.resolve(strict=True)), ('1.4', game / 'bin/Win64_Shipping_Client')]:
        paths = [refs / f'TaleWorlds.{name}.dll' for name in ['MountAndBlade', 'Core', 'Library', 'DotNet']]
        for path in paths:
            if not path.is_file():
                raise FileNotFoundError(path)
        versions = sorted(set(re.findall(r'v1\.\d+\.\d+\.\d+',
                              (refs / 'TaleWorlds.Library.dll').read_bytes().decode('utf-16-le', errors='ignore'))))
        if len(versions) != 1 or not versions[0].startswith('v' + api + '.'):
            raise RuntimeError('Wrong/ambiguous reference line: ' + repr(versions))
        receipt['references'][api] = {'game_version': versions[0], 'sha256': {p.name: digest(p) for p in paths}}
        command = ['dotnet', 'build', str(EXT / f'src/{MODULE}.csproj'), '-c', 'Release', '-t:Rebuild', '-v:q',
                   '-p:BannerlordApi=' + api, '-p:CoreRefs=' + str(refs), '-p:HarmonyCorePath=' + str(harmony)]
        execute(command, EXT / f'artifacts/build-{api}.log')
        print('PASS build', api, versions[0], flush=True)
    outputs = {api: EXT / f'artifacts/{api}/{MODULE}.dll' for api in hosts}
    if outputs['1.3'].read_bytes() != outputs['1.4'].read_bytes():
        raise RuntimeError('API builds differ; refusing a single shared DLL stage')
    test_project = EXT / 'tests/ReplyTool.Tests.csproj'
    execute(['dotnet', 'build', str(test_project), '-c', 'Release', '-v:q', '-p:HarmonyCorePath=' + str(harmony)],
            EXT / 'artifacts/test-build.log')
    exe = EXT / 'tests/bin/Release/net472/ReplyTool.Tests.exe'
    for api, host in hosts.items():
        logfile = EXT / f'artifacts/tests-{api}.log'
        execute([str(exe), str(ROOT), str(host), api, str(game)], logfile)
        result = logfile.read_text(encoding='utf-8').splitlines()[-1]
        receipt.setdefault('tests', {})[api] = result
        print(result, flush=True)
    receipt['af_sha256_after'] = {api: digest(path) for api, path in hosts.items()}
    if before != receipt['af_sha256_after']:
        raise RuntimeError('An installed AF host changed during verification')
    sources = list((EXT / 'src').glob('*.cs')) + list((EXT / 'src').glob('*.csproj'))
    sources += list((EXT / 'content').glob('*.json')) + [EXT / 'SubModule.xml', Path(__file__).resolve()]
    receipt['source_sha256'] = {str(p.relative_to(EXT)).replace('\\', '/'): digest(p) for p in sorted(sources)}
    receipt['dll_sha256'] = digest(outputs['1.3'])
    receipt['dependencies'] = {'0Harmony.dll': digest(harmony), 'Newtonsoft.Json.dll': digest(ROOT / '_deps_auto/Newtonsoft.Json.dll')}
    metadata = (json.dumps(receipt, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
    (EXT / 'artifacts/verification.json').write_bytes(metadata)
    # New stages preserve every prior candidate. Both contain the same shared DLL.
    stage = Path(tempfile.mkdtemp(prefix='stage-', dir=EXT / 'artifacts'))
    for lang in ['CN', 'EN']:
        folder = stage / lang / MODULE
        binary = folder / 'bin/Win64_Shipping_Client'
        data = folder / 'ModuleData'
        binary.mkdir(parents=True); data.mkdir()
        shutil.copyfile(outputs['1.3'], binary / (MODULE + '.dll'))
        shutil.copyfile(EXT / 'SubModule.xml', folder / 'SubModule.xml')
        shutil.copyfile(EXT / f'content/ReplyToolPrompt.{lang}.json', data / 'ReplyToolPrompt.json')
        shutil.copyfile(EXT / f'README.{lang}.md', folder / 'README.md')
        (folder / 'verification.json').write_bytes(metadata)
        expected = {'SubModule.xml', 'README.md', 'verification.json', 'ModuleData/ReplyToolPrompt.json',
                    'bin/Win64_Shipping_Client/' + MODULE + '.dll'}
        actual = {p.relative_to(folder).as_posix() for p in folder.rglob('*') if p.is_file()}
        if actual != expected:
            raise RuntimeError('Unexpected stage closure')
    (EXT / 'artifacts/latest-stage.txt').write_text(str(stage) + '\n', encoding='utf-8')
    print('AF host hashes unchanged. Local stages:', stage, flush=True)


if __name__ == '__main__':
    main()
