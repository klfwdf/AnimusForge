"""Replay the actual audience capture method with production scope and game/LOS fixtures."""
import argparse
import importlib.util
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

spec = importlib.util.spec_from_file_location("cutover", HERE.parent / "ChannelCutoverBoundaryTests/run.py")
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)
parser = argparse.ArgumentParser()
parser.add_argument("--run-root", required=True, type=Path)
args = parser.parse_args()
output = new_run_root(ROOT, "scene-audience-capture", args.run_root)
source = (ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs").read_text(encoding="utf-8-sig")
method = extractor.declaration(source, "internal static bool TryBuildSceneShoutConversationScope(")
(output / "Capture.cs").write_text(
    "using System; using System.Collections.Generic; using System.Linq; using TaleWorlds.Library; "
    "using TaleWorlds.MountAndBlade; namespace AnimusForge { public partial class ShoutBehavior {\n"
    + method + "\n} }", encoding="utf-8")
stubs = (HERE / "Stubs.cs").read_text(encoding="utf-8-sig")
stubs = stubs.replace("public sealed class Mission\n    {", "public sealed class Mission\n    {\n"
    "        public static Mission Current; public List<Agent> Agents = new();")
stubs = stubs.replace("public sealed class Agent\n    {", "public sealed class Agent\n    {\n"
    "        public static Agent Main;")
assert "public static Mission Current;" in stubs and "public static Agent Main;" in stubs
(output / "GameStubs.cs").write_text(stubs, encoding="utf-8")
for name, original in {
    "Scope.cs": ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/SceneShoutConversationScope.cs",
    "Program.cs": HERE / "CaptureHarness.cs.txt",
}.items():
    (output / name).write_bytes(original.read_bytes())
(output / "Capture.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>'
    '<Nullable>disable</Nullable><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(output / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), "run", "--project", str(output / "Capture.csproj"), "-c", "Release"],
    cwd=ROOT, env=minimal_test_environment(dotnet, output), capture_output=True, text=True,
    encoding="utf-8", errors="replace", timeout=90)
log = result.stdout + result.stderr
(output / "run.log").write_text(log, encoding="utf-8")
print(log)
raise SystemExit(result.returncode)
