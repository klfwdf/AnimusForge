"""Replay production range/capture and qualification prefix with both native locator sources.

The qualification method is executed unchanged up to SafeFoodDays; a counter replaces
the expensive snapshot tail. Game objects, positions and route distances are fixtures.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, minimal_test_environment

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--run-root', type=Path)
parser.add_argument('--qualification-ref', help='Historical qualification prefix for a negative control')
args = parser.parse_args()
out = new_run_root(ROOT, 'proactive-nearby', args.run_root)
spec = importlib.util.spec_from_file_location('extract', HERE.parent / 'ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
owner_path = 'src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs'
host_path = 'src/modules/AF.Module.Conversation/Proactive/ProactiveNpcRequestBehavior.cs'
owner = (ROOT / owner_path).read_text(encoding='utf-8-sig')
host = (ROOT / host_path).read_text(encoding='utf-8-sig')
capture = extract.declaration(owner, 'private static List<MobileParty> CaptureNearbyRequestParties(')
radius = extract.declaration(owner, 'private static float GetProactiveContactRange(')
start = extract.declaration(host, 'private void StartCandidateScan(')
find = extract.declaration(owner, 'private ProactiveCandidate FindBestRequestCandidate(')
assert 'CaptureNearbyRequestParties(mainParty)' in start
assert 'sourceParties ?? CaptureNearbyRequestParties(mainParty)' in find
assert 'AllLordParties' not in start + find
for relative in [owner_path, 'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs',
                 'src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsRegistry.cs',
                 'src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsLocalization.cs']:
    assert 'ProactiveNpcRequestDistanceMultiplier' not in (ROOT / relative).read_text(encoding='utf-8-sig'), relative
for relative in ['content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml',
                 'content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml']:
    entries = ET.parse(ROOT / relative).getroot().iter('string')
    assert not any('ProactiveNpcRequestDistanceMultiplier' in e.get('id', '')
                   or e.get('id', '').startswith('AF_MCM_Proactive_Distance') for e in entries), relative
qualification = owner if not args.qualification_ref else subprocess.check_output(
    ['git', 'show', args.qualification_ref + ':' + owner_path], cwd=ROOT).decode('utf-8-sig')
base = extract.declaration(qualification, 'private bool TryBuildBaseCandidate(')
prefix = base[:base.index('int foodDays = SafeFoodDays(party);')]
prefix += '''ExpensiveSnapshots++; candidate = new ProactiveCandidate(); return true;
        } catch { skipReason = "exception"; return false; }
    }
'''
generated = '''using System; using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
internal sealed partial class ProactiveNpcRequestBehavior {
''' + radius + '\n' + capture + '\n' + prefix + '\n}\n'
(out / 'Production.cs').write_text(generated, encoding='utf-8')
hashes = {owner_path: hashlib.sha256(owner.encode()).hexdigest(),
          host_path: hashlib.sha256(host.encode()).hexdigest(),
          'executed-qualification-prefix': hashlib.sha256(prefix.encode()).hexdigest()}
for api, native in [('1.3', ROOT / '原版游戏本体代码1.3.x/TaleWorlds.CampaignSystem/Map'),
                    ('1.4', ROOT / '原版游戏本体代码1.4.5/TaleWorlds.CampaignSystem/TaleWorlds/CampaignSystem/Map')]:
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
    props = ET.SubElement(project, 'PropertyGroup')
    for key, value in {'OutputType': 'Exe', 'TargetFramework': 'net8.0', 'LangVersion': 'latest',
                       'EnableDefaultCompileItems': 'false', 'NuGetAudit': 'false'}.items():
        ET.SubElement(props, key).text = value
    items = ET.SubElement(project, 'ItemGroup')
    sources = [out / 'Production.cs', HERE / 'Replay.cs']
    # The 1.3 decompiler omitted object casts in generic reference comparisons.
    # Restore those casts in the isolated fixture; qualify MathF for a net8 host.
    native_grid = (native / 'LocatorGrid.cs').read_text(encoding='utf-8-sig')
    native_grid = native_grid.replace('this._nodes[locatable.LocatorNodeIndex] == locatable',
                                      '(object)this._nodes[locatable.LocatorNodeIndex] == locatable')
    native_grid = native_grid.replace('locatable2.NextLocatable == locatable',
                                      '(object)locatable2.NextLocatable == locatable')
    grid = out / ('LocatorGrid-' + api + '.cs')
    grid.write_text('using MathF = TaleWorlds.Library.MathF;\n' + native_grid, encoding='utf-8')
    sources += [grid, native / 'LocatableSearchData.cs', native / 'ILocatable.cs']
    for path in sources:
        ET.SubElement(items, 'Compile', Include=str(path))
        hashes[str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
    hashes[str((native / 'LocatorGrid.cs').relative_to(ROOT))] = hashlib.sha256((native / 'LocatorGrid.cs').read_bytes()).hexdigest()
    project_folder = out / api
    project_folder.mkdir()
    path = project_folder / 'Replay.csproj'
    ET.ElementTree(project).write(path, encoding='utf-8', xml_declaration=True)
    result = subprocess.run(['dotnet', 'run', '--project', str(path), '-v:q'], cwd=ROOT,
                            env=minimal_test_environment(ROOT, out), capture_output=True, text=True,
                            encoding='utf-8', errors='replace', timeout=120)
    (out / ('replay-' + api + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(api, 'exit', result.returncode, result.stdout + result.stderr)
    if result.returncode:
        raise SystemExit(result.returncode)
(out / 'sources.json').write_text(json.dumps(hashes, ensure_ascii=False, indent=2), encoding='utf-8')
print('PASS nearby scan: two native locator lines; game/MCM/route and snapshot tail substituted; live=NOT_RUN')
