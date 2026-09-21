"""Execute extracted Reward candidate consumers with the real PromptList facade."""
from __future__ import annotations

import argparse
import importlib.util
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--dotnet", default=os.environ.get("AF_DOTNET") or os.environ.get("DOTNET_EXE")
                    or r"G:\AFMOD\.dotnet-sdk\dotnet.exe")
args = parser.parse_args()
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (ROOT / "RewardSystemBehavior.cs").read_text(encoding="utf-8-sig")
methods = "\n\n".join(extract.declaration(source, marker) for marker in (
    "public string BuildVisibleEquipmentPostprocessListForAI(Hero hero, MentionedWorldEntities mentions",
    "public string BuildFilteredInventorySummaryForAI(",
    "public string BuildFilteredSettlementMerchantInventorySummaryForAI("))
scene_role = extract.declaration((ROOT / "ShoutBehavior.cs").read_text(encoding="utf-8-sig"),
                                 "private static string BuildSceneNpcRoleIntroForPrompt(")
scene_hero = extract.declaration(scene_role, "if (includeInventorySummary && RewardSystemBehavior.Instance != null)")
scene_merchant = extract.declaration(scene_role, "if (includeInventorySummary && RewardSystemBehavior.Instance != null && characterObject != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind")
stubs = (ROOT / "tests/modules/AF.Module.Prompt/Retrieval/FacadeStubs.cs").read_text(encoding="utf-8")
assert stubs.count("public static class RewardSystemBehavior") == 1
stubs = stubs.replace("public static class RewardSystemBehavior", "public partial class RewardSystemBehavior")
assert stubs.count("public sealed class Hero { public string StringId; }") == 1
stubs = stubs.replace("public sealed class Hero { public string StringId; }", "public sealed class Hero { public string StringId; public CharacterObject CharacterObject; }")
output = ROOT / "artifacts/tests/prompt-j03-production-reward/current"
output.mkdir(parents=True, exist_ok=True)
(output / "Stubs.cs").write_text(stubs, encoding="utf-8")
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
(output / "Program.cs").write_text(template.replace("@@METHODS@@", methods)
                                  .replace("@@SCENE_HERO@@", scene_hero).replace("@@SCENE_MERCHANT@@", scene_merchant), encoding="utf-8")
links = [ROOT / "PromptListRetrievalService.cs",
         ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptCandidateSelection.cs",
         ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptCandidateSnapshotIndex.cs",
         ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptSemanticWarmupSeedBatch.cs"]
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" /><Compile Include="Stubs.cs" />' + ''.join(f'<Compile Include="{path}" Link="{path.name}" />' for path in links) + '</ItemGroup></Project>'
(output / "Proof.csproj").write_text(project, encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
dotnet = Path(args.dotnet)
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_CLI_TELEMETRY_OPTOUT="1")
build = subprocess.run([str(dotnet), "build", str(output / "Proof.csproj"), "-c", "Release", "--nologo", "-p:UseAppHost=false", "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")],
                       cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(build.stdout + build.stderr)
if build.returncode:
    raise SystemExit(build.returncode)
run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/Proof.dll")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(run.stdout + run.stderr)
raise SystemExit(run.returncode)
