"""Replay current production relay boundaries, with detached network/agent fixtures."""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment, resolve_dotnet

HERE = Path(__file__).resolve().parent
SCENE = "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneConversationChains.cs"
OWNER = "src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs"

def declaration(text, signature):
    start = text.index(signature)
    opening = text.index("{", start)
    depth = 0
    for m in re.finditer(r'@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/|[{}]', text[opening:]):
        if m.group() == "{": depth += 1
        elif m.group() == "}":
            depth -= 1
            if not depth: return text[start:opening + m.end()]
    raise ValueError("Unterminated declaration: " + signature)

def source(path, ref):
    if ref:
        return subprocess.check_output(["git", "show", ref + ":" + path], cwd=ROOT).decode("utf-8-sig")
    return (ROOT / path).read_text(encoding="utf-8-sig")

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dotnet")
    parser.add_argument("--source-ref")
    parser.add_argument("--request-source-ref", help="Use the actual historical group request callsite as a wiring negative control")
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    scene = source(SCENE, args.source_ref)
    owner = source(OWNER, args.source_ref)
    projection = source("src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs", args.source_ref)
    main_owner = source("src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs", args.source_ref)
    request_source = source(SCENE, args.request_source_ref or args.source_ref)
    request_method = declaration(request_source, "internal async Task HandleGroupResponsePerHeroIndependent(")
    request_lines = request_method.splitlines()
    request_call = next(line for line in request_lines if "List<object> messages = _ports.BuildStrictSceneMessagesForNpc(currentSpeaker.AgentIndex, layeredPrompt" in line)
    request_context = "\n".join(line for line in request_lines if "string relayTurnContext =" in line or "string relayTurnUserSection =" in line)
    record_lines = scene.splitlines()
    record_index = next(i for i, line in enumerate(record_lines) if "if (multiNpcScene && Logger.IsVerboseModLogicEnabled" in line)
    record_gate = "\n".join(record_lines[record_index:record_index + 2])
    method = declaration(scene, "internal async Task HandleGroupResponsePerHeroIndependent(")
    setup_begin = method.index("bool firstTurn = true;")
    loop_open = method.index("{", method.index("while (currentSpeaker != null", setup_begin))
    admission_begin = method.index("if (!IsSceneConversationEpochCurrent(conversationEpoch))", loop_open)
    admission_end = method.index("engagedAgentIndices.Add(currentSpeaker.AgentIndex);", admission_begin)
    eligibility = declaration(method, "if (!suppressBattleSpeechFollowups && !endRequested && remainingTurns > 0)")
    eligibility_end = method.index(";", method.index("relayPostprocessSelected = !suppressBattleSpeechFollowups", admission_end)) + 1
    candidate_begin = method.index("if (!suppressBattleSpeechFollowups && !endRequested && remainingTurns > 0)")
    selection_begin = method.index("if (relayPostprocessSelected)", method.index("Task<ScenePostprocessOutcome> postprocessTask"))
    tail_begin = method.index("if (!string.IsNullOrWhiteSpace(cleaned) && relayRequested)", selection_begin)
    tail_end = method.index("firstTurn = false;", tail_begin) + len("firstTurn = false;")
    snippets = {
        "SETUP": method[setup_begin:loop_open + 1],
        "ADMISSION": method[admission_begin:admission_end],
        "CANDIDATES": method[candidate_begin:eligibility_end],
        "SELECTION": declaration(method[selection_begin:], "if (relayPostprocessSelected)"),
        "TAIL": method[tail_begin:tail_end],
        "NORMALIZER": declaration(owner, "internal static string NormalizeAutoGroupRelayPostprocessTagsForScene("),
        "PROMPT": declaration(owner, "internal static string BuildSceneRelayTargetListForPostprocess("),
        "RULES": declaration(owner, "internal static List<PostprocessRuleEntry> BuildAutoGroupRelayPostprocessRulesForScene("),
        "TURN_CONTEXT": declaration(scene, "internal static string BuildSceneRelayTurnContextForPrompt("),
        "TURN_USER": declaration(scene, "internal static string BuildSceneGroupTurnUserSectionForPrompt("),
        "CONTINUATION_INSTRUCTION": declaration(projection, "internal static string BuildAutoGroupChatReplyInstruction("),
        "MAIN_MESSAGE": declaration(main_owner, "internal static object CreateCourierChatMessage("),
        "MAIN_COMPOSITE": declaration(main_owner, "internal static string BuildSceneCompositeUserBlock("),
        "MAIN_PREFIX": re.search(r"internal enum SceneSingleSpeakerLayout[^\n]+", main_owner).group() + "\n" + declaration(main_owner, "internal static string[] BuildSceneSingleSpeakerPrefixSections("),
        "MAIN_CONTEXT_BINDINGS": request_context,
        "MAIN_REQUEST_CALL": request_call,
        "PRIMARY_RECORD_GATE": record_gate,
        "DIRECT_REPLY_FLAG": next(line for line in method.splitlines() if "bool replyIsDirectPlayerResponse = firstTurn;" in line),
        "RUNTIME_APPEND": declaration(owner, "internal static string AppendPostprocessContextBlockForScene("),
        "POST_CONTEXT_BLOCK": declaration(owner[owner.rfind("if (relayRuleInjected)", 0, owner.index("runtimeContext, BuildSceneRelayTargetListForPostprocess(relayCandidates")):], "if (relayRuleInjected)"),
        "GROUP_DETECTION": declaration(scene, "private static bool IsExplicitSceneGroupAddress("),
        "UNANSWERED_TARGET": declaration(scene, "private static bool TrySelectUnansweredSceneGroupRelayTarget("),
        "GROUP_CALL": next(line for line in method.splitlines() if "bool explicitGroupAddress =" in line),
        "GROUP_FALLBACK": method[method.index('if (!string.IsNullOrWhiteSpace(cleaned))', selection_begin):tail_begin],
        "CONSTANT": re.search(r"internal const int AUTO_GROUP_CHAT_MAX_LINES = \d+;", source("src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs", args.source_ref)).group(),
    }
    output = new_run_root(ROOT, "scene-relay-continuation", args.run_root)
    harness = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
    system_method = re.search(r'internal static string BuildConversationSystemPrompt\(string system\)\s*=>[^;]+;', main_owner)
    harness = harness.replace('@@MAIN_SYSTEM@@', system_method.group())
    config_path = "content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts.json"
    def relay_rows(value):
        if isinstance(value, dict):
            if value.get("Tag") == "[RELAY:接力编号]": yield value
            for child in value.values(): yield from relay_rows(child)
        elif isinstance(value, list):
            for child in value: yield from relay_rows(child)
    configured = list(relay_rows(json.loads(source(config_path, args.source_ref))))
    assert len(configured) == 1, "canonical relay rule must have one configured tag"
    harness = harness.replace("@@CONFIG_GROUP@@", json.dumps(configured[0]["Description"], ensure_ascii=True))
    harness = harness.replace("@@CONFIG_SINGLE@@", json.dumps(configured[0]["SingleFramedNpcDescription"], ensure_ascii=True))
    english = next(row for row in json.loads(source(config_path.replace('.json', '.en.json'), args.source_ref))["RulePrompts"]
                   if row["Id"] == "scene_auto_group_relay")["PostprocessRules"][0]
    for field in ("Tag", "Description", "SingleFramedNpcDescription"):
        harness = harness.replace("@@CONFIG_EN_" + field + "@@", json.dumps(english[field], ensure_ascii=True))
    for key, value in snippets.items(): harness = harness.replace("@@" + key + "@@", value)
    if "@@" in harness: raise ValueError("Unexpanded harness placeholder")
    (output / "Program.cs").write_text(harness, encoding="utf-8")
    assembly_paths = (
        "src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs",
        "src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs",
        "src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs",
        "src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs",
        "src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs")
    for index, path in enumerate(assembly_paths):
        (output / ("ProductionAssembly" + str(index) + ".cs")).write_text(source(path, args.source_ref), encoding="utf-8")
    memory_line = declaration(source("src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs", args.source_ref), "internal sealed class DailyMemoryLine")
    (output / "DailyMemoryLine.cs").write_text("namespace AnimusForge {" + memory_line + "}", encoding="utf-8")
    linked = "".join('<Compile Include="' + str(ROOT / path).replace("\\", "/") + '" />' for path in (
        "AnimusForge.SiegeAftermathIntervention/SiegeNpcResponseEventBudget.cs",
        "AnimusForge.SiegeAftermathIntervention/SiegeNpcResponseLimitProfile.cs"))
    (output / "Relay.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup>' + linked + '</ItemGroup></Project>', encoding="utf-8")
    (output / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
    fingerprints = {key: hashlib.sha256(value.encode()).hexdigest() for key, value in snippets.items()}
    fingerprints[config_path] = hashlib.sha256(source(config_path, args.source_ref).encode()).hexdigest()
    fingerprints.update({path: hashlib.sha256(source(path, args.source_ref).encode()).hexdigest() for path in assembly_paths})
    fingerprints.update(source_ref=args.source_ref or "working-tree", request_source_ref=args.request_source_ref or args.source_ref or "working-tree", boundary_scope="production relay controller/callsite snippets + full real role/history/assembly owners; detached network/agent and identity/config leaves; real GCCZ budget")
    (output / "source-fingerprints.json").write_text(json.dumps(fingerprints, indent=2), encoding="utf-8")
    sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Prompt/Composition'))
    from fixture_resources import add_history_resources
    add_history_resources(ROOT,output,output/'Relay.csproj')
    ui_path='src/AF.GameAdapter.Bannerlord/UI/Conversation/ConversationUiLocalization.cs'
    retry_path='src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs'
    sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Llm/RetryProjectionTests'))
    from fixture_support import retry_members,add_retry_resources
    retry=retry_members(source(retry_path,args.source_ref),declaration,['internal static string ProjectKnownErrorForUi('])
    (output/'ActualRetryProjection.cs').write_text('using System;using System.Text;using System.Runtime.CompilerServices;using TaleWorlds.Localization;namespace AnimusForge;internal static class LlmRetryPrompt {'+retry+'}',encoding='utf-8')
    (output/'ui-source-receipt.json').write_text(json.dumps({'uiWholeSource':ui_path,'uiSha256':hashlib.sha256(source(ui_path,args.source_ref).encode()).hexdigest(),'retryMethod':'ProjectKnownErrorForUi','retrySha256':hashlib.sha256(retry.encode()).hexdigest(),'runtime':'real TaleWorlds.Localization/Library; default English UI fallback; CN packaged Prompt resource'},indent=2),encoding='utf-8')
    linked_ui=''.join('<Compile Include="'+str(ROOT/path)+'" />' for path in [ui_path,'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs'])
    linked_ui+=''.join('<Reference Include="'+name+'"><HintPath>'+str(ROOT/'_deps_auto'/(name+'.dll'))+'</HintPath></Reference>' for name in ['TaleWorlds.Localization','TaleWorlds.Library'])
    linked_ui+='<EmbeddedResource Include="'+str(ROOT/'content/foundation/AF.Foundation.Localization/ModuleData/RuntimeStaleRequestCompatibility.txt')+'" LogicalName="AnimusForge.Foundation.StaleRequestError.txt" />'
    project=output/'Relay.csproj';project.write_text(project.read_text('utf-8').replace('</Project>','<ItemGroup>'+linked_ui+'</ItemGroup></Project>'),encoding='utf-8')
    add_retry_resources(ROOT,project)
    dotnet = resolve_dotnet(ROOT, args.dotnet)
    result = subprocess.run([str(dotnet), "run", "--project", str(output / "Relay.csproj"), "-c", "Release"], cwd=output, env=minimal_test_environment(dotnet, output), capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    log = result.stdout + result.stderr
    (output / "run.log").write_text(log, encoding="utf-8")
    (output / "result.json").write_text(json.dumps({"exitCode": result.returncode, "assertionsPassed": len(re.findall(r"^PASS (?!\d+ production)", log, re.MULTILINE)), "requestSourceRef": args.request_source_ref or args.source_ref or "working-tree", "scope": fingerprints["boundary_scope"]}, indent=2), encoding="utf-8")
    print(log)
    return result.returncode

if __name__ == "__main__": raise SystemExit(main())
