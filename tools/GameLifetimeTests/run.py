"""Execute actual lifecycle registry, coordinator, adapter and SubModule callbacks; no game deployment."""
from pathlib import Path
import argparse, importlib.util, os, subprocess
ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).parent

def load(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module

util = load('util', 'tools/ModuleFrameworkApiTests/run.py')
ex = load('ex', 'tools/ChannelCutoverBoundaryTests/run.py')
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--original-callbacks', action='store_true')
p.add_argument('--skip-mutations', action='store_true')
p.add_argument('--dotnet', default=os.environ.get('DOTNET_EXE', r'G:\AFMOD\.dotnet-sdk\dotnet.exe'))
args = p.parse_args()

def read(path): return (ROOT/path).read_text(encoding='utf-8-sig')

sources = ['src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs', 'src/AF.Foundation.Runtime/Lifecycle/GameLifetimeCoordinator.cs', 'src/AF.GameAdapter.Bannerlord/Composition/AfCampaignRuntimeLifecycle.cs']
submodule = (subprocess.check_output(['git','show','807bc5b9:SubModule.cs'], cwd=ROOT).decode('utf-8-sig')
             if args.original_callbacks else read('SubModule.cs'))
callbacks = '\n'.join(ex.declaration(submodule, sig) for sig in ['protected override void InitializeGameStarter(', 'public override void OnGameEnd(', 'protected override void OnSubModuleUnloaded('])
harness = read('tools/GameLifetimeTests/Harness.cs.txt').replace('@@CALLBACKS@@', callbacks)
mutations = {
    'stale_registration': (sources[0], ' && version == _version', ''),
    'unsealed_owner': (sources[0], 'if (seal) _sealed = true;', 'if (seal) _sealed = false;'),
    'clear_window_open': (sources[0], 'lock (_sync) _pauseCount++;', 'lock (_sync) _pauseCount += 0;'),
    'unsettled_registration': (sources[0], 'try { registration.Retire(); }', 'try { }'),
    'no_release': (sources[0], 'finally { registration?.Dispose(); }', 'finally { }'),
    'wrong_game_end': (sources[1], 'if (!IsCurrent(game)) return false;', 'if (_game == null) return false;'),
    'duplicate_begin': (sources[1], 'if (game == null || IsCurrent(game)) return false;', 'if (game == null) return false;'),
    'late_generation': (sources[1], '_advance("campaign_end");\n        _retire("campaign_end");', '_retire("campaign_end");'),
    'owner_failure_aborts': (sources[2], 'catch (Exception error)\n            {', 'catch (Exception error)\n            {\n                throw;'),
    'no_capture': ('Program.cs', 'AfCampaignRuntimeLifecycle.CaptureOwners(game, campaignStarter);', ';'),
    'no_end': ('Program.cs', 'AfCampaignRuntimeLifecycle.End(game);', ''),
    'no_unload': ('Program.cs', 'AfCampaignRuntimeLifecycle.Stop();', ''),
}
variants = [('original-callbacks' if args.original_callbacks else 'current', None)]
if not args.original_callbacks and not args.skip_mutations: variants += list(mutations.items())
for name, mutation in variants:
    out = HERE / '.generated' / name; out.mkdir(parents=True, exist_ok=True)
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    selected = []
    for path, code in [(path,read(path)) for path in sources] + [('Program.cs',harness)]:
        if mutation and mutation[0] == path:
            assert mutation[1] in code, 'Mutation anchor missed: '+name
            code = code.replace(mutation[1], mutation[2])
        file = out / Path(path).name; file.write_text(code, encoding='utf-8'); selected.append(file)
    project = util.project(out, 'GameLifetime', selected, executable=True)
    status, log = util.run_dotnet(args.dotnet, ['run','--project',str(project),'-c','Release'], out)
    (out/'run.log').write_text(log,encoding='utf-8')
    if mutation:
        assert status != 0 and 'FAIL ' in log and 'error CS' not in log, 'Mutation failed to produce behavioral failure: '+name+'\n'+log
        print('PASS behavioral mutant rejected: '+name, flush=True)
    else:
        print(log,end='',flush=True)
        if args.original_callbacks: raise SystemExit(status)
        assert status == 0, 'Current lifecycle failed'
