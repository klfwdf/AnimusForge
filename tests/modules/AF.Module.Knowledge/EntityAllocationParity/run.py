"""Compare actual sync and detached global-allocation wrappers over equivalent fake game metadata."""
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
parser.add_argument("--mutate", choices=["drop-detached-scope", "drop-detached-distance"])
args = parser.parse_args()
source = (ROOT / "WorldEntityRetrievalService.cs").read_text(encoding="utf-8-sig")
markers = [
    "internal sealed class EntityMatch<T>", "internal sealed class DetachedEntityCandidate\n",
    "private static void ApplyGlobalInjectionLimit(", "private static void ApplyDetachedGlobalInjectionLimit(",
    "private static void AddGlobalLimitItems<T>(", "private static List<EntityMatch<T>> ExtractGlobalLimitMatches<T>(",
    "private static void SortEntityMatches<T>(",
]
parts = [extract.declaration(source, m) for m in markers]
if args.mutate == "drop-detached-scope":
    parts[4] = parts[4].replace("candidate.HeroClanId = detached.HeroClanId;", 'candidate.HeroClanId = "";', 1)
if args.mutate == "drop-detached-distance":
    parts[4] = parts[4].replace("candidate.HeroDistanceBonus = detached.HeroDistanceBonus;", "candidate.HeroDistanceBonus = 0f;", 1)
out = ROOT / "artifacts/tests/knowledge-j06-entity-allocation" / (args.mutate or "current")
out.mkdir(parents=True, exist_ok=True)
(out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Linq;\nnamespace AnimusForge { internal static partial class WorldEntityRetrievalService {\n" + "\n".join(parts) + "\n}}\n", encoding="utf-8")
(out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/><Compile Include="../../../../src/modules/AF.Module.Knowledge/Entities/EntityInjectionAllocator.cs" Link="EntityInjectionAllocator.cs"/><Compile Include="../../../../src/modules/AF.Module.Knowledge/Entities/EntityNameMatcher.cs" Link="EntityNameMatcher.cs"/></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
r = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(r.stdout, end="")
print(r.stderr, end="")
raise SystemExit(r.returncode)
