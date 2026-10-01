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
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path, new_run_root, minimal_test_environment, resolve_dotnet
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

source = (ROOT / "src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs").read_text(encoding="utf-8-sig")
capture = extract.declaration(source, "internal static PromptRuleEligibility CapturePromptRuleEligibility(")
parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
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
output = new_run_root(ROOT, "prompt-j06-eligibility-capture", args.run_root)
(output / "Program.cs").write_text((HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig").replace("@@CAPTURE@@", capture), encoding="utf-8")
(output / "Proof.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'
    '<Compile Include="Program.cs" /><Compile Include="' + str(HERE / 'ExecutionRuntimePorts.cs.txt').replace('\\', '/') + '" />'
    + "".join('<Compile Include="' + str(current_source_path(ROOT, path)).replace("\\", "/") + '" />' for path in (
        "src/modules/AF.Module.Prompt/Composition/PromptRuleEligibility.cs",
        "src/modules/AF.Module.Prompt/Composition/PromptRuntimeTargetBinding.cs",
        "src/modules/AF.Module.Prompt/Retrieval/PromptRetrievalContextOwner.cs",
        "src/bridges/Vengeance/Host/PublicExecutionOrderRuntime.cs",
        "src/bridges/Vengeance/Host/PublicExecutionOrderPolicy.cs",
        "Vengeance/Source/Core/ExecutionEnums.cs"))
    + '</ItemGroup></Project>', encoding="utf-8")
(output / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, output)
run = subprocess.run([str(dotnet), "run", "--project", str(output / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
print((run.stdout + run.stderr).strip()[-2500:])
raise SystemExit(run.returncode)
