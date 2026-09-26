"""Execute KnowledgeLibraryBehavior's actual versioned Lore publication methods."""
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
source = (ROOT / "KnowledgeLibraryBehavior.cs").read_text(encoding="utf-8-sig")
parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["no-deep-copy", "no-version-invalidation"])
args = parser.parse_args()
methods = [extract.declaration(source, marker) for marker in (
    "private static IReadOnlyList<LoreRule> GetPublishedRulesForIndex(",
    "private static void PublishPromptRules(",
    "private static void TouchRuleData(",
)]
if args.mutate == "no-deep-copy":
    methods[1] = methods[1].replace("new List<string>(rule.Keywords)", "rule.Keywords", 1)
if args.mutate == "no-version-invalidation":
    methods[2] = methods[2].replace("Index.Touch();", "", 1)
out = ROOT / "artifacts/tests/knowledge-j06-published-snapshot" / ((args.mutate or "current") + "-safe")
out.mkdir(parents=True, exist_ok=True)
(out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Linq;\nnamespace AnimusForge { public partial class KnowledgeLibraryBehavior {\n" + "\n".join(methods) + "\n}}\n", encoding="utf-8")
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
(out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
r = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(r.stdout, end="")
print(r.stderr, end="")
raise SystemExit(r.returncode)
