"""Execute the actual Native J06 scheduled prompt method against physical-thread fake owners."""
from __future__ import annotations
import argparse
import importlib.util
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["drop-final-admission", "drop-knowledge-generation"])
args = parser.parse_args()
source = (ROOT / "ShoutBehavior.NativePromptBuild.cs").read_text(encoding="utf-8-sig")
method = extract.declaration(source, "private async Task<MyBehavior.ShoutPromptContext> BuildNativePromptContextScheduledAsync(")
if args.mutate == "drop-final-admission":
    needle = "if (!IsNativeConversationAdmissionCurrent(admission, out _))"
    at = method.rfind(needle)
    assert at >= 0
    method = method[:at] + "if (false)" + method[at + len(needle):]
if args.mutate == "drop-knowledge-generation":
    needle = 'SaveRuntimeGuard.IsStale(runtimeGeneration, "native_conversation_knowledge_retrieval")'
    assert method.count(needle) == 1
    method = method.replace(needle, "false", 1)
out = ROOT / "artifacts/tests/prompt-j06-native-knowledge" / (args.mutate or "current")
out.mkdir(parents=True, exist_ok=True)
(out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing TaleWorlds.CampaignSystem;\nnamespace AnimusForge { public partial class ShoutBehavior {\n" + method + "\n}}\n", encoding="utf-8")
(out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
r = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(r.stdout, end="")
print(r.stderr, end="")
raise SystemExit(r.returncode)
