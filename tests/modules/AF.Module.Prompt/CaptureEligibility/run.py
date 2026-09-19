"""Compile the production J06d capture method with fake game-thread ports.

This exercises exception isolation, target scope restoration and worker-only DTO reads;
it does not execute Bannerlord or the actual module eligibility implementations.
"""
from __future__ import annotations

import importlib.util
import argparse
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

source = (ROOT / "AIConfigHandler.cs").read_text(encoding="utf-8-sig")
capture = extract.declaration(source, "internal static PromptRuleEligibility CapturePromptRuleEligibility(")
parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=("shared-catch", "open-exclusion"))
args = parser.parse_args()
if args.mutate == "shared-catch":
    line = "try { result.VassalageEligible = VassalageBehavior.CanInjectVassalageRuleForPromptCapture(hero, targetCharacter); } catch { }"
    assert line in capture
    capture = capture.replace(line, line.removeprefix("try { ").removesuffix(" } catch { }"))
elif args.mutate == "open-exclusion":
    line = "TargetIsPlayerPartyTradeLimited = true,"
    assert line in capture
    capture = capture.replace(line, "TargetIsPlayerPartyTradeLimited = false,", 1)
output = ROOT / "artifacts/tests/prompt-j06-eligibility-capture"
output.mkdir(parents=True, exist_ok=True)
(output / "Program.cs").write_text((HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig").replace("@@CAPTURE@@", capture), encoding="utf-8")
(output / "Proof.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'
    '<Compile Include="Program.cs" />'
    + "".join('<Compile Include="' + str(ROOT / path).replace("\\", "/") + '" />' for path in (
        "src/modules/AF.Module.Prompt/Composition/PromptRuleEligibility.cs",
        "src/modules/AF.Module.Prompt/Composition/PromptRuntimeTargetBinding.cs",
        "src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalContextOwner.cs"))
    + '</ItemGroup></Project>', encoding="utf-8")
(output / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".dotnet-cli-home"), DOTNET_CLI_TELEMETRY_OPTOUT="1")
run = subprocess.run([str(dotnet), "run", "--project", str(output / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
print((run.stdout + run.stderr).strip()[-2500:])
raise SystemExit(run.returncode)
