"""Extract actual host methods; native mutation endpoints are recording fixtures."""
from pathlib import Path
import importlib.util
import argparse
import subprocess
import sys
ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, minimal_test_environment
spec = importlib.util.spec_from_file_location('extractor', ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--run-root', type=Path)
parser.add_argument('--source-file', type=Path)
args = parser.parse_args()
src = (args.source_file or ROOT/'src/AF.GameAdapter.Bannerlord/Encounter/LordEncounterBehavior.cs').read_text(encoding='utf-8-sig')
signatures = [
    'private sealed class MeetingPlayerReleaseRequest',
    'private static MeetingPlayerReleaseRequest CaptureMeetingPlayerReleaseRequest(',
    'private static bool IsMeetingPlayerReleaseRequestCurrent(',
    'internal static bool TryExecuteMeetingPlayerRelease(Hero target, PartyBase expectedEncounterParty, string reason)',
    'private static bool MarkPendingNativeConversationNpcSurrender(',
    'private static bool HasPendingNativeConversationNpcSurrender(',
    'private static void ClearPendingNativeConversationNpcSurrender(',
    'private static bool IsNativeConversationStillActive(',
    'private static void TryForcePendingNativeConversationNpcSurrenderIfReady(',
    'private static bool TryGetNpcSurrenderEncounterParty(',
]
template = Path(__file__).with_name('Harness.cs.txt').read_text(encoding='utf-8')
out = new_run_root(ROOT, 'release-surrender-boundary', args.run_root)
(out/'Program.cs').write_text(template.replace('@@METHODS@@', '\n'.join(extractor.declaration(src,s) for s in signatures)), encoding='utf-8')
links = ''.join(f'<Compile Include="{ROOT.as_posix()}/src/modules/AF.Module.Encounter/{n}.cs" Link="{n}.cs" />' for n in ['EncounterReleaseOwner','EncounterPendingReturnOwner'])
(out/'Boundary.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NoWarn>CS0649;CS0414</NoWarn><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" />'+links+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
dotnet = ROOT/'local/dotnet/8.0.425/dotnet.exe'
result = subprocess.run([str(dotnet),'run','--project',str(out/'Boundary.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'run.log').write_text(result.stdout+result.stderr,encoding='utf-8')
print(result.stdout+result.stderr)
sys.exit(result.returncode)
