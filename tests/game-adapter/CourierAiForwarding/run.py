"""Replay actual AF DLL forwarding against explicit matching game references; no game started."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dll', required=True, type=Path)
parser.add_argument('--references', required=True, type=Path)
parser.add_argument('--fallback-references', type=Path, help='Non-versioned dependencies, matching the normal build fallback')
parser.add_argument('--dependency-dir', action='append', type=Path, default=[])
parser.add_argument('--api', required=True, choices=['1.3', '1.4'])
parser.add_argument('--out', required=True, type=Path)
args = parser.parse_args()
dll = args.dll.resolve(strict=True)
references = args.references.resolve(strict=True)
out = args.out.resolve()
root = Path(__file__).resolve().parents[3]
assert out.is_relative_to(root / 'artifacts'), 'test output must stay inside workspace artifacts'
out.mkdir(parents=True, exist_ok=False)
csc = Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe')
refs = sorted(p for p in references.glob('TaleWorlds.*.dll') if p.name != 'TaleWorlds.Native.dll')
fallback = args.fallback_references.resolve(strict=True) if args.fallback_references else references
names = {p.name for p in refs}
refs += sorted(p for p in fallback.glob('TaleWorlds.*.dll') if p.name not in names and p.name != 'TaleWorlds.Native.dll')
refs += [dll, csc.parent / 'netstandard.dll']
worker = out / 'Worker.dll'
command = [str(csc), '/nologo', '/target:library', '/out:' + str(worker)]
if args.api == '1.4':
    command.append('/define:API14')
command += ['/r:' + str(p) for p in refs] + [str(Path(__file__).with_name('Replay.cs'))]

def run(command, name, env=None):
    result = subprocess.run(command, env=env, cwd=out, capture_output=True, text=True, encoding='mbcs', errors='replace')
    (out / name).write_text(result.stdout + result.stderr, 'utf-8')
    print(result.stdout + result.stderr)
    return result.returncode

if run(command, 'compile.log'):
    raise SystemExit(1)
bootstrap = out / 'Bootstrap.cs'
bootstrap.write_text('''using System;using System.IO;using System.Reflection;
class Bootstrap { static int Main(string[] args) {
AppDomain.CurrentDomain.AssemblyResolve += (_, e) => { string n = new AssemblyName(e.Name).Name + ".dll";
for (int i=0;i<args.Length-1;i++) { string p=Path.Combine(args[i],n);if(File.Exists(p))return Assembly.LoadFrom(p); } return null; };
try { return (int)Assembly.LoadFrom(args[args.Length-1]).GetType("Replay").GetMethod("Main").Invoke(null,null); }
catch(Exception e) { Console.WriteLine(e);return 1; } } }''', 'utf-8')
exe = out / 'Replay.exe'
if run([str(csc), '/nologo', '/out:' + str(exe), str(bootstrap)], 'bootstrap-compile.log'):
    raise SystemExit(1)
env = dict(os.environ, ANIMUSFORGE_DATA_ROOT=str(out / 'AnimusForge'))
code = run([str(exe), str(dll.parent), str(references), str(fallback)] +
           [str(p.resolve(strict=True)) for p in args.dependency_dir] + [str(worker)], 'run.log', env)
(out / 'receipt.json').write_text(json.dumps({
    'dll': str(dll), 'sha256': hashlib.sha256(dll.read_bytes()).hexdigest(),
    'api': args.api, 'exitCode': code, 'nativeCampaignStarted': False,
    'uncovered': ['native naval movement', 'existing save recovery', 'shore embark transitions'],
}, indent=2), 'utf-8')
raise SystemExit(code)
