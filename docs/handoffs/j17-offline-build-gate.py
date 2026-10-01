"""Process-only unified build isolation; leaves tracked scripts and old outputs intact."""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import uuid

ROOT = Path(__file__).resolve().parents[2]
ap = argparse.ArgumentParser()
ap.add_argument('--output-parent',type=Path)
ap.add_argument('--configuration', choices=['Debug', 'Release'], required=True)
args = ap.parse_args()
parent=args.output_parent or ROOT / 'artifacts/j17b/session-20261001/p6-integration'
parent=parent.resolve();parent.relative_to(ROOT/'artifacts')
out = parent / ('build-' + uuid.uuid4().hex)
out.mkdir(parents=True, exist_ok=False)
script_path = ROOT / 'scripts/build/build_single_module.ps1'
source = script_path.read_text(encoding='utf-8-sig')

def replace_once(old, new):
    global source
    assert source.count(old) == 1, old
    source = source.replace(old, new)

replace_once('$artifactRoot = Join-Path $projectRootFull "bin\\$Configuration\\single_module_artifacts"',
             '$artifactRoot = Join-Path $isolationRoot "artifacts"')
replace_once('$intermediateRoot = Join-Path $projectRootFull "obj\\single_module\\$Configuration"',
             '$intermediateRoot = Join-Path $isolationRoot "obj"')
replace_once('Remove-Item -LiteralPath $Path -Recurse -Force',
             'throw "Refusing to overwrite existing isolated build directory: $Path"')
start = source.index('    Get-ChildItem -LiteralPath $OutputDir -Force | Where-Object {')
end = source.index('\n}\n\nfunction Assert-AssemblyName', start)
source = source[:start] + '    # No prune: retain every verification output.\n' + source[end:]
assert '-Stage' not in str(args) and '-Deploy' not in str(args)
game = Path('D:/steam/steamapps/common/Mount & Blade II Bannerlord')
sdk = ROOT / 'local/dotnet/8.0.425'
pwsh = Path('C:/Program Files/PowerShell/7-preview/pwsh.exe')
for item in (sdk / 'dotnet.exe', pwsh, game, ROOT / '_deps_auto', ROOT / 'local/bannerlord-refs/1.4.7.117484'):
    assert item.exists(), item
env = {key: os.environ[key] for key in ('SystemRoot','WINDIR','COMSPEC','PATHEXT','PROCESSOR_ARCHITECTURE',
    'ProgramFiles','ProgramFiles(x86)','ProgramW6432','ProgramData','ALLUSERSPROFILE') if key in os.environ}
env.update({'PATH': str(sdk)+os.pathsep+'C:/Windows/System32', 'DOTNET_ROOT': str(sdk),
    'DOTNET_CLI_HOME': str(out/'home'), 'NUGET_PACKAGES': str(out/'packages'),
    'DOTNET_NOLOGO':'1', 'DOTNET_CLI_TELEMETRY_OPTOUT':'1',
    'USERPROFILE':str(out/'home'),'HOME':str(out/'home'),'APPDATA':str(out/'appdata'),
    'LOCALAPPDATA':str(out/'localappdata'),
    'DefaultItemExcludes':'**/bin/**;**/obj/**;**/local/**;**/artifacts/**'})
head = subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
diff = subprocess.check_output(['git','diff','--binary'],cwd=ROOT)
script = "$ErrorActionPreference='Stop'\n$isolationRoot='" + out.as_posix() + "'\n& ([scriptblock]::Create(@'\n" + source + "\n'@)) -ProjectRoot '" + ROOT.as_posix() + "' -BannerlordRoot '" + game.as_posix() + "' -Bannerlord13ReferenceDir '" + (ROOT/'_deps_auto').as_posix() + "' -Bannerlord14ReferenceDir '" + (ROOT/'local/bannerlord-refs/1.4.7.117484').as_posix() + "' -Configuration " + args.configuration
# Windows command lines cannot hold the full script's UTF-16 base64. Transport
# the verified in-memory invocation over stdin, keeping the encoded launcher tiny.
launcher = "$source=[Console]::In.ReadToEnd(); & ([scriptblock]::Create($source))"
encoded = base64.b64encode(launcher.encode('utf-16le')).decode('ascii')
(out/'invocation.json').write_text(json.dumps({'head':head,'configuration':args.configuration,
    'trackedDiffSha256':hashlib.sha256(diff).hexdigest(),'scriptSha256':hashlib.sha256(script_path.read_bytes()).hexdigest(),
    'gameRoot':str(game),'references13':str(ROOT/'_deps_auto'),'references14':str(ROOT/'local/bannerlord-refs/1.4.7.117484'),
    'processOnlyChanges':['new output/intermediate','reject existing directory','disable prune','exclude old artifacts/bin/obj/local from candidates']},indent=2),encoding='utf-8')
print(out,flush=True)
with (out/'build.log').open('w',encoding='utf-8') as log:
    result = subprocess.run([str(pwsh),'-NoLogo','-NoProfile','-EncodedCommand',encoded],input=script,text=True,encoding='utf-8',cwd=ROOT,env=env,stdout=log,stderr=subprocess.STDOUT)
(out/'results.json').write_text(json.dumps({'exit':result.returncode,'head':head,'configuration':args.configuration},indent=2),encoding='utf-8')
print('build exit',result.returncode,flush=True)
sys.exit(result.returncode)
