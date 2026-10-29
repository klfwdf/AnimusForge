"""Exercise the production tick routes, TickSource, application and capture queue.

Only game lifetime/settings and unrelated orchestration operations are leaves.
The mutation must fail behaviorally, not through compilation or a text assertion.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment


def method(text, signature):
    start = text.index(signature)
    brace = text.index('{', start)
    depth = 1
    end = brace + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--run-root', type=Path)
    args = parser.parse_args()
    out = new_run_root(ROOT, 'material-routing', args.run_root)
    base = ROOT / 'src/modules/AF.Module.Diplomacy'
    paths = [base / 'Adapters/WorldDiplomacyModuleAdapter.cs',
             base / 'World/WorldDiplomacyBehavior.cs',
             base / 'World/WorldDiplomacyBehavior.TickSource.cs',
             base / 'Application/WorldDiplomacyTickApplication.cs',
             base / 'Application/WorldDiplomacyMaterialCaptureQueue.cs',
             ROOT / 'src/AF.GameAdapter.Bannerlord/Composition/ApplicationTickComposition.cs']
    texts = [p.read_text(encoding='utf-8-sig') for p in paths]
    assert '() => DiplomacyModuleServices.World.OnEngineTick()' in texts[-1]
    adapter = method(texts[0], 'public void OnEngineTick()')
    legacy = method(texts[1], 'public void OnEngineTick()')
    owner = 'namespace AnimusForge; public sealed partial class WorldDiplomacyBehavior {' + legacy + '}'
    route = 'namespace AnimusForge; internal sealed class WorldDiplomacyModuleAdapter {' + adapter + '}'
    (out / 'sources.json').write_text(json.dumps({str(p): hashlib.sha256(p.read_bytes()).hexdigest()
                                               for p in paths}, indent=2), encoding='utf-8')
    dotnet = resolve_dotnet(ROOT)
    for variant in ('current', 'missing_shared_capture'):
        folder = out / variant
        folder.mkdir()
        app = texts[3]
        if variant != 'current':
            assert 'source.AdvanceMaterialCapture();' in app
            app = app.replace('source.AdvanceMaterialCapture();', '')
        for name, text in [('Owner.cs', owner), ('Adapter.cs', route), ('Source.cs', texts[2]),
                           ('Application.cs', app), ('Queue.cs', texts[4]),
                           ('Program.cs', (HERE / 'Program.cs').read_text(encoding='utf-8'))]:
            (folder / name).write_text(text, encoding='utf-8')
        (folder / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
        (folder / 'Routing.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>'
            '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>')
        env = minimal_test_environment(dotnet, folder)
        build = subprocess.run([str(dotnet), 'build', str(folder / 'Routing.csproj'), '-v:q',
            '-p:RestoreConfigFile=' + str(folder / 'NuGet.Config')], env=env, capture_output=True, text=True)
        (folder / 'build.log').write_text(build.stdout + build.stderr, encoding='utf-8')
        assert build.returncode == 0, build.stdout + build.stderr
        run = subprocess.run([str(dotnet), str(folder / 'bin/Debug/net8.0/Routing.dll')],
                             env=env, capture_output=True, text=True, timeout=25)
        (folder / 'run.log').write_text(run.stdout + run.stderr, encoding='utf-8')
        if variant == 'current':
            print(run.stdout + run.stderr)
            assert run.returncode == 0
        else:
            assert run.returncode != 0 and 'FAIL module capture stalled' in run.stdout + run.stderr
            print('PASS removed shared pump rejected by actual module-entry behavior')
    print('OUTPUT', out)


if __name__ == '__main__':
    main()
