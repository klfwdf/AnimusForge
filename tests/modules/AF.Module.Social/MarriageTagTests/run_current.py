"""Opt-in real-DLL/synthetic-hero replay, optionally using the actual configured gateway."""
import argparse, hashlib, json, os, shutil, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root

p = argparse.ArgumentParser()
p.add_argument('--dll', required=True, type=Path)
p.add_argument('--references', required=True, type=Path)
p.add_argument('--game-root', required=True, type=Path)
p.add_argument('--run-root', type=Path)
p.add_argument('--live', action='store_true', help='Explicit billable opt-in; synthetic dialogue only')
p.add_argument('--settings', type=Path)
p.add_argument('--model-cache', type=Path)
args = p.parse_args()
dll = args.dll.resolve(strict=True)
assert dll.is_relative_to(ROOT), 'candidate must stay inside workspace'
marker = json.loads(dll.with_suffix('.build.json').read_text('utf-8-sig'))
digest = hashlib.sha256(dll.read_bytes()).hexdigest().upper()
assert marker['Sha256'].upper() == digest and marker['BannerlordApi'] == '1.4', 'candidate identity mismatch'
out = new_run_root(ROOT, 'marriage-current-dll', args.run_root)
game = args.game_root.resolve(strict=True)
directories = [dll.parent, args.references.resolve(strict=True), game/'Modules/AnimusForge/bin/Win64_Shipping_Client']
directories += [game/('Modules/'+name+'/bin/Win64_Shipping_Client') for name in ['Bannerlord.Harmony', 'Bannerlord.MBOptionScreen', 'Bannerlord.UIExtenderEx']]
assert all(d.is_dir() for d in directories), 'explicit dependency directory missing'
csc = Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe')
refs = [r for r in directories[1].glob('TaleWorlds.*.dll') if r.name != 'TaleWorlds.Native.dll'] + [directories[1]/'Newtonsoft.Json.dll', dll, directories[4]/'MCMv5.dll']
refs += [Path('C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.2/Facades/netstandard.dll')]
worker = out/'Worker.dll'
command = [str(csc), '/nologo', '/target:library', '/out:'+str(worker), '/r:System.Net.Http.dll']
command += ['/r:'+str(r) for r in refs] + [str(HERE/'CurrentDllReplay.cs')]
compiled = subprocess.run(command, capture_output=True, text=True, encoding='mbcs', errors='replace')
(out/'build.log').write_text(compiled.stdout+compiled.stderr, 'utf-8')
print(compiled.stdout+compiled.stderr)
if compiled.returncode:
    raise SystemExit(compiled.returncode)
bootstrap = '''using System;using System.IO;using System.Linq;using System.Reflection;
class Bootstrap{static int Main(string[] args){string[] dirs=args.Take(6).ToArray();AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string n=new AssemblyName(e.Name).Name+".dll";foreach(string d in dirs){string p=Path.Combine(d,n);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};try{return (int)Assembly.LoadFrom(args[6]).GetType("CurrentDllReplay").GetMethod("Main",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{args.Skip(7).ToArray()});}catch(Exception e){Console.WriteLine("FAIL bootstrap "+e.GetType().Name+": "+e.Message);return 1;}}}'''
(out/'Bootstrap.cs').write_text(bootstrap, 'utf-8')
subprocess.run([str(csc), '/nologo', '/out:'+str(out/'Replay.exe'), str(out/'Bootstrap.cs')], check=True, capture_output=True)
# csc's unannotated executable otherwise uses the CLR 4.0 TLS defaults. Match
# the product's net472 target without changing machine TLS or certificate policy.
(out/'Replay.exe.config').write_text('<configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2"/></startup><runtime><AppContextSwitchOverrides value="Switch.System.Net.DontEnableSystemDefaultTlsVersions=false;Switch.System.Net.DontEnableSchUseStrongCrypto=false"/></runtime></configuration>', 'utf-8')
command = [str(out/'Replay.exe')] + [str(d) for d in directories] + [str(worker), str(ROOT), str(out)]
if args.live:
    assert args.settings and args.model_cache, 'live requests require explicit settings and model cache'
    command += [str(args.settings.resolve(strict=True)), str(args.model_cache.resolve(strict=True)), '--live']
module = out/'AnimusForge'
module.mkdir()
shutil.copy2(ROOT/'AnimusForge/SubModule.xml', module/'SubModule.xml')
for entry in json.loads((ROOT/'content/content-map.json').read_text('utf-8-sig'))['entries']:
    target = entry.get('target', '')
    if target.startswith('ModuleData/') and target.endswith('.json'):
        destination = module/target
        destination.parent.mkdir(parents=True, exist_ok=True)
        assert not destination.exists(), 'duplicate module fixture resource: '+target
        shutil.copy2(ROOT/entry['source'], destination)
env = dict(os.environ)
env['ANIMUSFORGE_DATA_ROOT'] = str(module)
result = subprocess.run(command, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
(out/'run.log').write_text(result.stdout+result.stderr, 'utf-8')
print(result.stdout+result.stderr)
(out/'receipt.json').write_text(json.dumps({'dll':str(dll), 'sha256':digest, 'liveOptIn':args.live, 'exitCode':result.returncode, 'nativeCampaignStarted':False, 'credentialsCopied':False, 'uncovered':['native clan/marriage effects','actual saves','full main-reply generation']}, indent=2), 'utf-8')
raise SystemExit(result.returncode)
