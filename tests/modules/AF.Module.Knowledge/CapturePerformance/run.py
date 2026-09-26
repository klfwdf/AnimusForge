"""Exercise the production candidate/metadata capture methods with deterministic fake game ports."""
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
parser.add_argument("--mutate", choices=["skip-metadata", "skip-budget-check"])
args = parser.parse_args()
source = (ROOT / "WorldEntityRetrievalService.cs").read_text(encoding="utf-8-sig")
parts = [extract.declaration(source, marker) for marker in (
    "internal sealed class DetachedEntityCandidate\n",
    "private static void CaptureCandidates<T>(",
    "private static void CaptureDetachedMetadata(",
)]
if args.mutate == "skip-metadata":
    assert "captureMetadata?.Invoke(value, candidate)" in parts[1]
    parts[1] = parts[1].replace("captureMetadata?.Invoke(value, candidate)", "", 1)
if args.mutate == "skip-budget-check":
    assert "if (++scanned % EntityRetrievalBudgetCheckInterval == 0)" in parts[1]
    parts[1] = parts[1].replace("if (++scanned % EntityRetrievalBudgetCheckInterval == 0)", "if (++scanned < 0)", 1)
out = ROOT / "artifacts/tests/knowledge-j06-capture-performance" / (args.mutate or "current")
out.mkdir(parents=True, exist_ok=True)
(out / "Production.cs").write_text(
    "using System;\nusing System.Collections.Generic;\nusing System.Linq;\n"
    "namespace AnimusForge { internal static partial class WorldEntityRetrievalService {\n"
    + "\n".join(parts) + "\n}}\n", encoding="utf-8")
(out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
(out / "Proof.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
    '<UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup>'
    '<ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/></ItemGroup></Project>',
    encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".dotnet-cli-home"),
           DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release",
                         "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")],
                        cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(result.stdout, end="")
print(result.stderr, end="")
raise SystemExit(result.returncode)
