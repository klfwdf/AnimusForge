"""Production guard + real Harmony; ROT body and game state are explicit fixtures."""
from pathlib import Path
import argparse
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--target-framework', default='net10.0')
args = parser.parse_args()
root = Path(__file__).resolve().parents[3]
source = root / 'src/AF.GameAdapter.Bannerlord/Patches/Safety/RotEncyclopediaCompanionCompatibility.cs'
startup = (root / 'src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs').read_text(encoding='utf-8-sig')
assert startup.count('RotEncyclopediaCompanionCompatibility.EnsurePatched();') == 1
assert 'protected override void OnBeforeInitialModuleScreenSetAsRoot()' in (root / 'src/AF.GameAdapter.Bannerlord/Composition/SubModule.cs').read_text(encoding='utf-8-sig')
program = Path(__file__).with_name('Program.cs')
output = root / 'artifacts/rot-encyclopedia-compat-20261015'
for mode in ('present', 'absent'):
    out = output / mode
    out.mkdir(parents=True, exist_ok=True)
    project = out / 'Replay.csproj'
    define = '<DefineConstants>ROT_PRESENT</DefineConstants>' if mode == 'present' else ''
    project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>{args.target_framework}</TargetFramework>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems><LangVersion>latest</LangVersion>{define}</PropertyGroup>
<ItemGroup><Compile Include="{source.as_posix()}"/><Compile Include="{program.as_posix()}"/>
<PackageReference Include="Lib.Harmony" Version="2.4.2"/></ItemGroup></Project>''', encoding='utf-8')
    subprocess.run(['dotnet', 'run', '--project', str(project), '-c', 'Release'], cwd=root, check=True)
print('PASS startup registration wiring; no Tick registration, game save or third-party DLL changes.')
