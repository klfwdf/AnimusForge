"""Execute production Scene/Native role wrappers and verify their candidate handoff."""
from __future__ import annotations

import argparse
import importlib.util
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
from output_isolation import new_run_root, minimal_test_environment
AF2_FIXTURE_METADATA = {"sourceClass": "legacy-oracle-extraction", "currentOwnerReplayProjected": False}
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = historical_source('src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs')
top = extract.declaration(source, "private static string BuildSceneSystemTopPromptIntroForSingle(")
runtime = extract.declaration(source, "private static string BuildSceneUserRuntimeContextForSingle(")
role = extract.declaration(source, "private static string BuildSceneNpcRoleIntroForPrompt(")
assert "BuildFilteredInventorySummaryForAI(hero, inventoryMentions, promptListMax, includePrivateBattleEquipment: includeTradePricing)" in role
assert "BuildFilteredSettlementMerchantInventorySummaryForAI(characterObject, inventoryMentions, promptListMax)" in role
native = extract.declaration(historical_source('src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPrompt.cs'), "private void CapturePromptRules()")
assert native.count("ctx?.MentionedEntities") >= 2
assert "BuildSceneSystemTopPromptIntroForSingle(npc, targetHero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities)" in native
assert "BuildSceneUserRuntimeContextForSingle(npc, targetHero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities)" in native
output = new_run_root(ROOT, "production-scene-native", args.run_root)
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
(output / "Program.cs").write_text(template.replace("@@TOP@@", top).replace("@@RUNTIME@@", runtime), encoding="utf-8")
(output / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>', encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = minimal_test_environment(dotnet, output)
build = subprocess.run([str(dotnet), "build", str(output / "Proof.csproj"), "-c", "Release", "--nologo", "-p:UseAppHost=false", "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")],
                       cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(build.stdout + build.stderr)
if build.returncode:
    raise SystemExit(build.returncode)
run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/Proof.dll")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(run.stdout + run.stderr)
raise SystemExit(run.returncode)
