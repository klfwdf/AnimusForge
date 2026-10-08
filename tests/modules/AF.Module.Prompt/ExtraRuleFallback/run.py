"""Execute the production no-preselection extra-rule worker fallback with fake recall ports."""
from __future__ import annotations
import argparse
import importlib.util
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["skip-lexical", "lose-sticky-target"])
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
source = (ROOT / "src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs").read_text(encoding="utf-8-sig")
method = extract.declaration(source, "internal static List<GuardrailRuleHit> GetMatchedExtraRuleHitsForWorker(")
if args.mutate == "skip-lexical":
    method = method.replace("if (hits == null || hits.Count == 0)", "if (false)", 1)
if args.mutate == "lose-sticky-target":
    method = method.replace('capturedStickyTargetKey ?? ""', '""', 1)
out = new_run_root(ROOT, "prompt-j06-extra-rule-fallback", args.run_root)
(out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nnamespace AnimusForge { internal static partial class AIConfigHandler {\n" + method + "\n}}\n", encoding="utf-8")
(out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = minimal_test_environment(dotnet, out)
r = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(r.stdout, end="")
print(r.stderr, end="")
raise SystemExit(r.returncode)
