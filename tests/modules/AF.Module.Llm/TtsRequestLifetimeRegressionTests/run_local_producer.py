"""Run only the four local-provider producer cases, not the legacy consumer suite.

Compiles the complete current TtsEngine with the existing deterministic seams.
Native APIs remain forbidden. A passing subset does not bypass or validate the
separate historical consumer binding gate used by run.py.
"""
from pathlib import Path
import argparse
import hashlib
import importlib.util
from xml.sax.saxutils import escape
import re
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--dotnet", required=True)
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
output = new_run_root(ROOT, "GptSoVitsLocalProducer", args.run_root)
raw = (ROOT / "src/modules/AF.Module.Llm/Tts/TtsEngine.cs").read_text(encoding="utf-8-sig")
source, count = re.subn(r'\[DllImport\([^\n]+\)\]\s*internal static extern ([^;]+);',
                      r'internal static \1 { throw new InvalidOperationException("Native API forbidden in regression harness"); }', raw)
assert count == 11, count
harness = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
spec = importlib.util.spec_from_file_location("declarations", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
declarations = importlib.util.module_from_spec(spec)
spec.loader.exec_module(declarations)
settings = (ROOT / "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs").read_text(encoding="utf-8-sig")
button = declarations.declaration(settings, "TestTtsVolcDedicatedVoice = delegate") + ";"
helpers = "\n".join(declarations.declaration(settings, x) for x in ["private static string Ui(", "private static string UiFormat("])
harness = harness.replace("@@TTS_TEST_BUTTON@@", button).replace("@@UI_HELPERS@@", helpers)
assert "@@" not in harness

names = ("local provider does not require", "battle cancels local", "battle rejects local", "local failure never")
cases = [line for line in harness.splitlines() if any(line.lstrip().startswith('Check("' + name) for name in names)]
assert len(cases) == 4, "Local producer case selection changed; review the explicit subset."
program = harness.split(" static int Main(){", 1)[0] + " static int Main(){\n" + "\n".join(cases)
program += '\nConsole.WriteLine($"PRODUCER SUBSET {Passed} PASS / {Failed} FAIL; legacy consumer suite NOT-RUN");return Failed==0?0:1;\n}\n}\n'
(output / "TtsEngine.cs").write_text(source, encoding="utf-8")
(output / "Program.cs").write_text(program, encoding="utf-8")
sources = [ROOT / x for x in ["src/AF.Persistence/AnimusForgeDataPaths.cs", "src/modules/AF.Module.Llm/Protocol/LlmSourceTexts.cs", "src/modules/AF.Module.Llm/Tts/VolcTtsGateway.cs", "src/modules/AF.Module.Llm/Tts/LlmUiTexts.cs", "src/AF.GameAdapter.Bannerlord/UI/Errors/TtsTestFeedback.cs", "src/AF.GameAdapter.Bannerlord/UI/Errors/TtsErrorUiText.cs", "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettingsUiLocalization.cs"]]
links = [ROOT / "_deps_auto/Newtonsoft.Json.dll", ROOT / ".tmp/build_check/1.3/TaleWorlds.Localization.dll", ROOT / ".tmp/build_check/1.3/TaleWorlds.Library.dll"]
includes = "".join('<Compile Include="' + escape(str(x)) + '" />' for x in sources)
refs = "".join('<Reference Include="' + x.stem + '"><HintPath>' + escape(str(x)) + '</HintPath></Reference>' for x in links)
(output / "Tests.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + includes + refs + '<EmbeddedResource Include="' + escape(str(ROOT / "content/modules/AF.Module.Llm/ModuleData/LlmSourceTexts.json")) + '" LogicalName="AnimusForge.Llm.RuntimeSources.json" WithCulture="false" /></ItemGroup></Project>', encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
env = minimal_test_environment(Path(args.dotnet), output)
module = output / "module-fixture/AnimusForge"
(module / "ModuleData").mkdir(parents=True)
(module / "SubModule.xml").write_text('<Module><Id value="AnimusForge" /></Module>', encoding="utf-8")
env["ANIMUSFORGE_DATA_ROOT"] = str(module)
result = subprocess.run([args.dotnet, "run", "--project", str(output / "Tests.csproj"), "-c", "Release"],
                        cwd=output, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=150)
log = "Current TtsEngine SHA256=" + hashlib.sha256(raw.encode()).hexdigest() + "\n" + result.stdout + result.stderr
(output / "run.log").write_text(log, encoding="utf-8")
print(log)
raise SystemExit(result.returncode)
