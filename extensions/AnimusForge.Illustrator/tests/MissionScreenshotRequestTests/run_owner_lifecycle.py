"""Replay the production scene capture gate and late-completion methods; native/network are doubles."""
from pathlib import Path
import importlib.util, subprocess, sys
root = Path(__file__).resolve().parents[4]
out = Path(sys.argv[1]).resolve(); out.mkdir(parents=True, exist_ok=True)
spec = importlib.util.spec_from_file_location('extract', root / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec); spec.loader.exec_module(extract)
source = (root / 'extensions/AnimusForge.Illustrator/src/Core/MissionScreenshotIllustration.cs').read_text(encoding='utf-8-sig')
fixture = Path(__file__).with_name('OwnerLifecycle.cs.in').read_text(encoding='utf-8')
for name in ['TickCapture', 'Finish', 'ScopeClosed', 'TryShowProgress']:
    fixture = fixture.replace('__' + name + '__', extract.declaration(source, 'private static void ' + name + '('))
popup = (root / 'src/AF.GameAdapter.Bannerlord/UI/Conversation/ShoutTextInputPopup.cs').read_text(encoding='utf-8-sig')
fixture = fixture.replace('__ShouldCancelForSystemInterruption__', extract.declaration(popup, 'private bool ShouldCancelForSystemInterruption('))
(out/'Program.cs').write_text(fixture, encoding='utf-8')
(out/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>', encoding='utf-8')
subprocess.run(['dotnet','build',str(out/'Test.csproj'),'-o',str(out/'bin')],check=True)
subprocess.run([str(out/'bin/Test.exe')],check=True)
