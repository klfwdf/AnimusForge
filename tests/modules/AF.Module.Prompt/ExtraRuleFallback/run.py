"""Execute the production no-preselection extra-rule worker fallback with fake recall ports."""
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
parser.add_argument("--mutate", choices=["skip-lexical", "lose-sticky-target"])
args = parser.parse_args()
source = (ROOT / "AIConfigHandler.cs").read_text(encoding="utf-8-sig")
method = extract.declaration(source, "internal static List<GuardrailRuleHit> GetMatchedExtraRuleHitsForWorker(")
if args.mutate == "skip-lexical":
    method = method.replace("if (hits == null || hits.Count == 0)", "if (false)", 1)
if args.mutate == "lose-sticky-target":
    method = method.replace('capturedStickyTargetKey ?? ""', '""', 1)
out = ROOT / "artifacts/tests/prompt-j06-extra-rule-fallback" / (args.mutate or "current")
out.mkdir(parents=True, exist_ok=True)
(out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nnamespace AnimusForge { internal static partial class AIConfigHandler {\n" + method + "\n}}\n", encoding="utf-8")
(out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
r = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(r.stdout, end="")
print(r.stderr, end="")
raise SystemExit(r.returncode)
