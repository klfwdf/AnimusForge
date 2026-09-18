"""Execute production Scene/Native role wrappers and verify their candidate handoff."""
from __future__ import annotations

import importlib.util
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (ROOT / "ShoutBehavior.cs").read_text(encoding="utf-8-sig")
top = extract.declaration(source, "private static string BuildSceneSystemTopPromptIntroForSingle(")
runtime = extract.declaration(source, "private static string BuildSceneUserRuntimeContextForSingle(")
role = extract.declaration(source, "private static string BuildSceneNpcRoleIntroForPrompt(")
assert "BuildFilteredInventorySummaryForAI(hero, inventoryMentions, promptListMax, includePrivateBattleEquipment: includeTradePricing)" in role
assert "BuildFilteredSettlementMerchantInventorySummaryForAI(characterObject, inventoryMentions, promptListMax)" in role
native = extract.declaration(source, "private async Task<string> SubmitNativeConversationTextInternalAsync(")
assert native.count("ctx?.MentionedEntities") >= 2
assert "BuildSceneSystemTopPromptIntroForSingle(npc, targetHero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities)" in native
assert "BuildSceneUserRuntimeContextForSingle(npc, targetHero, presentNpcs, includeInventorySummary, includeTradePricing, partyTransferTopicSelected, ctx?.MentionedEntities)" in native
output = ROOT / "artifacts/tests/prompt-j03-production-scene-native/current"
output.mkdir(parents=True, exist_ok=True)
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
(output / "Program.cs").write_text(template.replace("@@TOP@@", top).replace("@@RUNTIME@@", runtime), encoding="utf-8")
(output / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>', encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_CLI_TELEMETRY_OPTOUT="1")
build = subprocess.run([str(dotnet), "build", str(output / "Proof.csproj"), "-c", "Release", "--nologo", "-p:UseAppHost=false", "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")],
                       cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(build.stdout + build.stderr)
if build.returncode:
    raise SystemExit(build.returncode)
run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/Proof.dll")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(run.stdout + run.stderr)
raise SystemExit(run.returncode)
