"""Run the real Debug implementation with validated dependencies and synthetic senders."""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import minimal_test_environment, new_run_root, resolve_dotnet

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--candidate', type=Path, required=True)
p.add_argument('--suite', choices=('PrimaryStreamingOptionReplayTests', 'PrimaryLlmGatewayReplayTests'), default='PrimaryStreamingOptionReplayTests')
p.add_argument('--game-root', type=Path, required=True)
p.add_argument('--reference-dir', type=Path, required=True)
p.add_argument('--harmony-module', type=Path, required=True)
p.add_argument('--mcm-module', type=Path, required=True)
p.add_argument('--ui-module', type=Path, required=True)
p.add_argument('--private-runtime', type=Path, required=True)
p.add_argument('--run-root', type=Path)
a = p.parse_args()
for path in (a.candidate, a.game_root, a.reference_dir, a.harmony_module, a.mcm_module, a.ui_module, a.private_runtime):
    if not path.is_absolute() or not path.exists():
        p.error('Input must be an existing absolute path: ' + str(path))
output = new_run_root(ROOT, 'primary-streaming-option', a.run_root)
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, output)
env['AF_REPLAY_REPO_ROOT'] = str(ROOT)
project = ROOT / 'tests/replay' / a.suite / (a.suite + '.csproj')
props = {
    'GameRoot': a.game_root,
    'Bannerlord14ReferencePath': a.reference_dir,
    'ReplayHarmonyModulePath': a.harmony_module,
    'ReplayMcmModulePath': a.mcm_module,
    'ReplayUiExtenderModulePath': a.ui_module,
    'ReplayPrivateRuntimePath': a.private_runtime,
    'ReplayCandidateDll': a.candidate,
    'ReplayOutputRoot': output,
    'OutputPath': output / 'bin/Release/net8.0',
    'BaseIntermediateOutputPath': output / 'obj',
}
# Output paths are explicit fresh directories, never the runner's old bin/obj.
properties = ['-p:' + name + '=' + str(value).replace('\\', '/') + ('/' if name in ('OutputPath', 'BaseIntermediateOutputPath') else '') for name, value in props.items()]
properties += ['-p:NuGetAudit=false', '-p:DefaultItemExcludes=**/bin/**%3B**/obj/**']
print('OUTPUT=' + str(output), flush=True)
command = [str(dotnet), 'build', str(project), '-c', 'Release', *properties]
(output / 'invocation.json').write_text(json.dumps({'build': command, 'candidate': str(a.candidate)}, indent=2), encoding='utf-8')
with (output / 'build.log').open('w', encoding='utf-8') as log:
    result = subprocess.run(command, cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT, timeout=180)
if result.returncode:
    print((output / 'build.log').read_text(encoding='utf-8'), flush=True)
    raise SystemExit(result.returncode)
executable = output / 'bin/Release/net8.0' / (a.suite + '.dll')
hash_value = hashlib.sha256(a.candidate.read_bytes()).hexdigest()
result = subprocess.run([str(dotnet), str(executable), str(a.candidate), hash_value], cwd=output, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
log = result.stdout + result.stderr
(output / 'run.log').write_text(log, encoding='utf-8')
(output / 'result.json').write_text(json.dumps({'exit': result.returncode, 'candidateSha256': hash_value}, indent=2), encoding='utf-8')
print(log, flush=True)
raise SystemExit(result.returncode)
