"""Run the real admission host against native campaign/object registries, without a game.

Campaign/entity construction is synthetic. Lookup, Hero state, registries, resolver,
ReadAdmissionIdentity, AdmissionSubjectCountry and CaptureCountryAdmission are real.
No model, game launch, save access or installation writes are performed.
"""
from pathlib import Path
import argparse
import hashlib
import json
import subprocess
import sys
from xml.sax.saxutils import escape

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--references', type=Path, required=True)
    parser.add_argument('--shared-references', type=Path, required=True)
    parser.add_argument('--framework', type=Path, required=True)
    parser.add_argument('--run-root', type=Path)
    args = parser.parse_args()
    out = new_run_root(ROOT, 'native-admission-identity', args.run_root)
    base = ROOT / 'src/modules/AF.Module.Diplomacy'
    paths = [base / 'Adapters/DiplomacyIdentityResolver.cs',
             base / 'World/WorldDiplomacyBehavior.CountryAdmission.cs',
             base / 'Domain/DiplomacyCountryAdmission.cs',
             base / 'Application/WorldDiplomacyOrchestration.CountryAdmission.cs']
    texts = [p.read_text(encoding='utf-8-sig') for p in paths]
    # Use the real identity contract; the rest of orchestration has separate replay tests.
    contract = texts[3].split('internal sealed partial class WorldDiplomacyOrchestration')[0]
    contract = contract.replace('using Newtonsoft.Json;','').replace('using Newtonsoft.Json.Linq;','')
    references = {p.name: p for p in args.shared_references.glob('TaleWorlds.*.dll')}
    references.update({p.name: p for p in args.references.glob('TaleWorlds.*.dll')})
    references['netstandard.dll'] = args.framework / 'Facades/netstandard.dll'
    assert 'TaleWorlds.CampaignSystem.dll' in references
    (out / 'sources.json').write_text(json.dumps({str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                                                for p in paths}, indent=2), encoding='utf-8')
    (out / 'references.json').write_text(json.dumps({str(p): hashlib.sha256(p.read_bytes()).hexdigest()
                                                   for p in references.values()}, indent=2), encoding='utf-8')
    dotnet = resolve_dotnet(ROOT)
    for variant in ('current', 'wrong_hero_store', 'wrong_kingdom_store', 'wrong_capture_store', 'wrong_settlement_store'):
        folder = out / variant
        folder.mkdir()
        resolver, host = texts[:2]
        if variant == 'wrong_hero_store':
            host = host.replace('var hero = DiplomacyIdentityResolver.Hero(id);',
                                'var hero = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<Hero>(id);')
        elif variant == 'wrong_kingdom_store':
            host = host.replace('var kingdom = DiplomacyIdentityResolver.Kingdom(id);',
                                'var kingdom = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<Kingdom>(id);')
        elif variant == 'wrong_capture_store':
            host = host.replace('Select(id => DiplomacyIdentityResolver.Hero(id)',
                                'Select(id => TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<Hero>(id)')
        elif variant == 'wrong_settlement_store':
            resolver = resolver.replace('Campaign.Current == null ? null : TaleWorlds.CampaignSystem.Settlements.Settlement.Find(id)',
                                        'Campaign.Current?.CampaignObjectManager?.Find<Settlement>(id)')
        for name, text in [('Resolver.cs', resolver), ('Host.cs', host), ('Domain.cs', texts[2]),
                           ('Contract.cs', contract), ('Program.cs', (HERE / 'Program.cs').read_text(encoding='utf-8'))]:
            (folder / name).write_text(text, encoding='utf-8')
        refs = ''.join('<Reference Include="' + escape(p.stem) + '"><HintPath>' + escape(str(p.resolve()))
                       + '</HintPath><Private>true</Private></Reference>' for p in references.values())
        (folder / 'Native.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion>'
            '<PlatformTarget>x64</PlatformTarget><NoWarn>MSB3277</NoWarn>'
            '<AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages>'
            '<FrameworkPathOverride>' + escape(str(args.framework.resolve())) + '</FrameworkPathOverride></PropertyGroup><ItemGroup>'
            + refs + '</ItemGroup></Project>', encoding='utf-8')
        (folder / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
        env = minimal_test_environment(dotnet, folder)
        build = subprocess.run([str(dotnet), 'build', str(folder / 'Native.csproj'), '-v:q',
            '-p:RestoreConfigFile=' + str(folder / 'NuGet.Config')], env=env, capture_output=True, text=True)
        (folder / 'build.log').write_text(build.stdout + build.stderr, encoding='utf-8')
        assert build.returncode == 0, build.stdout + build.stderr
        run = subprocess.run([str(folder / 'bin/Debug/net472/Native.exe')], env=env,
                             capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=30)
        output = run.stdout + run.stderr
        (folder / 'run.log').write_text(output, encoding='utf-8')
        if variant == 'current':
            print(output)
            assert run.returncode == 0
        else:
            expected = {'wrong_hero_store': 'FAIL registered live hero', 'wrong_kingdom_store': 'FAIL registered kingdom',
                        'wrong_capture_store': 'admission_hero_missing:', 'wrong_settlement_store': 'FAIL native settlement owner'}[variant]
            assert run.returncode != 0 and expected in output, output
            print('PASS behavioral mutation rejected: ' + variant)
    print('OUTPUT', out)


if __name__ == '__main__':
    main()
