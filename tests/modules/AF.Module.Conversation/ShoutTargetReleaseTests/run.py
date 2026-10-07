"""Production hotkey/release methods; input, targeting and popup endpoints are doubles."""
from pathlib import Path
import importlib.util, subprocess, sys

root = Path(__file__).resolve().parents[4]
out = Path(sys.argv[1]).resolve()
out.mkdir(parents=True, exist_ok=True)
spec = importlib.util.spec_from_file_location('extract', root / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec); spec.loader.exec_module(extract)
source = (root / 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs').read_text(encoding='utf-8-sig')
fixture = (Path(__file__).parent / 'Fixture.cs.in').read_text(encoding='utf-8')
for marker, signature in [('UPDATE', 'private void UpdateShoutHotkeyCharge('), ('BEGIN', 'private void TryBeginShoutHotkeyCharge('), ('RELEASE', 'private void TryStartShoutFromHotkey(')]:
    fixture = fixture.replace('__' + marker + '__', extract.declaration(source, signature))
(out / 'Program.cs').write_text(fixture, encoding='utf-8')
(out / 'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>', encoding='utf-8')
subprocess.run(['dotnet', 'build', str(out / 'Test.csproj'), '-o', str(out / 'bin')], check=True)
subprocess.run([str(out / 'bin/Test.exe')], check=True)
