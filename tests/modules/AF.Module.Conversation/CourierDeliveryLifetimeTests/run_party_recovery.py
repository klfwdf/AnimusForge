"""Execute production party transfers and dispatch recovery with native-order fault injection."""
from pathlib import Path
import importlib.util
import argparse
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, minimal_test_environment

spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', type=Path, required=True)
parser.add_argument('--sdk-version')
args = parser.parse_args()
folder = ROOT / 'src/modules/AF.Module.Conversation/Channels/Courier'
declarations = []
for filename, signatures in {
    'CourierDeliveryBehavior.cs': ['private sealed class CourierSession', 'private static bool MoveWholeMemberRoster(', 'private static void MoveRosterFromMainParty(', 'private static void MoveCharacterFromMainMembersToParty('],
    'CourierDeliveryBehavior.DeliveryLifetime.cs': ['private bool ReturnCourierContentsToPlayer(', 'private void TryMoveHeroLossesToDestroyer('],
    'CourierDeliveryBehavior.SessionCreation.cs': ['private CourierSession CreateCourierSession('],
}.items():
    source = (folder / filename).read_text(encoding='utf-8-sig')
    declarations.extend(extract.declaration(source, signature) for signature in signatures)
output = new_run_root(ROOT, 'courier-party-recovery', None)
if args.sdk_version:
    import json
    (output / 'global.json').write_text(json.dumps({'sdk': {'version': args.sdk_version, 'rollForward': 'disable'}}))
(output / 'Program.cs').write_text((HERE / 'PartyRecoveryHarness.cs.txt').read_text().replace('@@PRODUCTION@@', '\n'.join(declarations)), encoding='utf-8')
(output / 'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
(output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet = args.dotnet.resolve()
result = subprocess.run([str(dotnet), 'run', '--project', str(output / 'Tests.csproj')], cwd=output,
    env=minimal_test_environment(dotnet, output), capture_output=True, text=True, timeout=120)
(output / 'run.log').write_text(result.stdout + result.stderr, encoding='utf-8')
print(result.stdout + result.stderr)
print(output)
sys.exit(result.returncode)
