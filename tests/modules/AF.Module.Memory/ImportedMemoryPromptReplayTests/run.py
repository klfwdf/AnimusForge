"""Current-source import-to-Native-request replay; no live game or network."""
import argparse
import hashlib
import importlib.util
import json
import re
import subprocess
import sys
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--memory-file", type=Path)
parser.add_argument("--run-root", type=Path)
parser.add_argument("--mutate", choices=["drop-overview", "wrong-import-store", "drop-prefix"])
args = parser.parse_args()
out = new_run_root(ROOT, "imported-memory-prompt", args.run_root)
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
sources = {}

def read(path):
    source = (ROOT / path).read_text(encoding="utf-8-sig")
    sources[path] = hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
    return source

def declarations(source, markers):
    return "\n".join(extract.declaration(source, marker) for marker in markers)

def write(name, content):
    (out / name).write_text(content, encoding="utf-8")

host = read("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs")
recall = read("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecall.cs")
methods = declarations(host + recall, [
    "private static string NormalizeMemoryHeroId(", "private static string GetMemoryHeroId(",
    "private static bool IsNonHeroMemoryId(", "private static string FormatMemoryHourRange(",
    "private static string FormatCompressedMemoryAgeSuffix(", "private static string StripMemoryTitleDateTime(",
    "private static string BuildMemoryRecallQueryText(", "private string BuildCompressedMemoryContextById(",
    "private string BuildHistoryContextById(", "private MemoryRecallRequest CaptureMemoryRecallRequest(",
    "private void PublishMemoryRecallFailure(", "private static Hero FindHeroById(",
    "private static bool IsMemoryEntityEligibleForCompressedMemory(",
    "private static bool IsHeroNpcEligibleForCompressedMemory(",
    "private List<DailyMemoryDraft> LoadDailyMemoryDraftsById(",
    "private List<CompressedMemoryBlock> LoadCompressedMemoryBlocksById(",
    "private string BuildMemoryOverviewContextById(", "private MemoryImportExportState CaptureMemoryImportExportState(",
    "private bool ApplyCompressedMemoryExportBundle(", "private bool HasCompressedMemoryDataForHero(",
    "public static List<ConversationMessage> BuildUncompressedMemoryRoleMessagesForExternal(",
    "private static int CountDailyMemoryDraftLines(",
])
wrapper = extract.declaration(host, "private List<ConversationMessage> BuildUncompressedMemoryRoleMessages(")
methods += "\n" + wrapper[:wrapper.index(";") + 1]
ui = read("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.ImportExportUi.cs")
methods += "\n" + extract.declaration(ui, "private void ImportSingleNpcDialogueHistoryData(")
for name in ["_dailyMemoryDrafts", "_compressedMemoryBlocks", "_memorySummaryQueue", "_memoryOverviewStates", "_memoryOverviewQueue"]:
    lines = [line.strip() for line in host.splitlines() if re.search(r"\b" + name + r" \{ get =>", line)]
    assert len(lines) == 1, name
    methods += "\n" + lines[0]
recovery = read("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs")
for marker in ["internal static bool IsValidMemoryCommitMarker(", "internal static bool IsMemoryRecoveryHexDigest("]:
    span = extract.declaration(recovery, marker)
    methods += "\n" + span[:span.index(";") + 1]
template = read("tests/modules/AF.Module.Conversation/NativeHistorySnapshotTests/MemoryHarness.cs.txt")
memory = template[:template.index("        public void Seed(")]
memory = memory.replace("@@MODELS@@", "").replace("@@METHODS@@", 'private const string NonHeroMemoryIdPrefix="af_nonhero:";\n' + methods).replace("@@BASELINE_METHODS@@", "")
for start in ["        private List<CompressedMemoryBlock> Blocks=", "        private static bool IsMemoryEntityEligibleForCompressedMemory(",
              "        private List<CompressedMemoryBlock> LoadCompressedMemoryBlocksById(", "        private List<DailyMemoryDraft> LoadDailyMemoryDraftsById(",
              "        private string BuildMemoryOverviewContextById("]:
    memory = re.sub(r"^" + re.escape(start) + r"[^\n]*\n", "", memory, flags=re.M)
memory = memory.replace('return "hero";', 'return Id;').replace('public string Name=>"Hero";',
    'public string Id="hero",NameValue="Hero"; public string Name=>NameValue; public bool IsAlive=true,IsDisabled; public static Hero MainHero; public static List<Hero> Registry=new(); public static Hero FindFirst(Func<Hero,bool> predicate)=>Registry.FirstOrDefault(predicate);')
memory = memory.replace('return null;}}}', 'return Value;}} public Hero Value;}')
memory = memory.replace('public static void Log(string a,string b){}', 'public static void Log(string a,string b){} public static void LogVerbose(string a,string b,Func<string> c,double d){}')
memory = memory.replace('_memoryBusinessState=new() {LoadBlocks=LoadCompressedMemoryBlocksById,LoadDrafts=LoadDailyMemoryDraftsById};', '_memoryBusinessState=new();')
extra = read("tests/modules/AF.Module.Memory/ImportedMemoryPromptReplayTests/Harness.cs.txt").replace('        private readonly MemoryBusinessStateOwner _memoryBusinessState = new();\n', '')
memory = "using System.IO;\nusing Newtonsoft.Json;\n" + memory + extra
memory += 'namespace AnimusForge {public partial class MyBehavior {private MemoryHistoryImportExportAdapter MemoryHistoryFiles=>new(_memoryBusinessState,SaveRuntimeGuard.CaptureGeneration,IsMemorySourceEditorCurrent,MarkMemoryOverviewDirty,ShowDuplicateImportInquiry);private MemoryHistoryCommitBannerlordAdapter _memoryHistoryCommit=>new(_memoryBusinessState);}}'

write("MemoryHarness.cs", memory)

business = read("src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs")
fields = business[business.index("    internal Dictionary<string, List<DailyMemoryDraft>> Drafts"):business.index("    internal List<DailyMemoryDraft> LoadDrafts(")]
queue = read("src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Queues.cs")
business_slice = fields + declarations(business, ["internal List<DailyMemoryDraft> LoadDrafts(", "internal List<CompressedMemoryBlock> LoadBlocks("])
business_slice += declarations(queue, ["internal MemoryOverviewState GetMemoryOverviewState(", "internal bool HasMemoryOverviewPendingBlocks("])
business_slice += declarations(business, ["internal string BuildHistoryContextById(","internal static int CountDailyMemoryDraftLines(","internal bool HasCompressedMemoryBlock("])
business_slice += 'internal int _activeNativeConversationMemorySessionId=-1;internal MemoryQueuePort QueuePort=new(){OverviewStartCount=()=>3};internal int GetCurrentNativeConversationMemorySessionIdForSuppression(Func<bool> active)=>-1;internal string BuildCurrentMemorySessionKey(int scene,int dialogue)=>"";'
queue_port = extract.declaration(queue, "internal sealed class MemoryQueuePort")
business_slice += extract.declaration(business, "internal static bool IsNonHeroMemoryId(")
business_slice += 'private const string NonHeroMemoryIdPrefix="af_nonhero:"; internal static int GetMemoryFinalInjectCountFromSettings()=>MyBehavior.ReadFinal();internal static int GetMemoryCandidateLimitFromSettings()=>MyBehavior.ReadLimit();internal static int GetMemoryPreprocessModeFromSettings()=>MyBehavior.ReadMode();'
if args.mutate == "drop-overview":
    assert 'stringBuilder.AppendLine(memoryOverviewContext);' in business_slice
    business_slice = business_slice.replace('stringBuilder.AppendLine(memoryOverviewContext);','stringBuilder.AppendLine("");',1)
business_slice += 'internal Dictionary<string,List<MyBehavior.DialogueDay>> History;'
write("Business.cs", "using System;using System.Collections.Generic;using System.Diagnostics;using System.Text;using System.Linq;namespace AnimusForge;" + extract.declaration(business, "internal sealed class MemoryHistoryContextReadCapabilities") + "internal sealed class MemoryBusinessStateOwner {" + business_slice + "}"+queue_port)
shared = read("src/AF.GameAdapter.Bannerlord/Prompt/SharedPromptCaptureBannerlordAdapter.cs")
recall_capture = read("src/AF.GameAdapter.Bannerlord/Prompt/MemoryRecallInputCaptureAdapter.cs")
shared_methods = declarations(shared,["internal sealed class HistoryWorkCapturePorts","internal static Func<string> CaptureHistoryContextWorkById(","internal static Func<string> CaptureHistoryContextWorkForHero(","internal static CompressedMemoryBlock CopyHistoryRecallBlock("])
recall_methods = declarations(recall_capture,["internal sealed class CapturePorts","internal static string BuildMemoryRecallQueryText(","internal static string BuildCompressedMemoryContextById(","internal static MemoryRecallRequest CaptureMemoryRecallRequest(","internal static void PublishMemoryRecallFailure(","internal static string ResolveCapturedMemoryLineSceneForPrompt("])
write("CaptureAdapters.cs","using System;using System.Collections.Generic;using System.Linq;using TaleWorlds.CampaignSystem;using TaleWorlds.Library;using HistoryPromptSnapshot=AnimusForge.MyBehavior.HistoryPromptSnapshot;namespace AnimusForge.Refactor.Adapters {internal static class SharedPromptCaptureBannerlordAdapter {"+shared_methods+"} internal static class MemoryRecallInputCaptureAdapter {"+recall_methods+"}}")
summary = read("src/AF.GameAdapter.Bannerlord/Memory/MemorySummaryApplicationAdapter.cs")
imports = read("src/AF.GameAdapter.Bannerlord/ImportExport/MemoryHistoryImportExportAdapter.cs")
import_body = imports[imports.index(' private readonly MemoryBusinessStateOwner _memory;'):imports.index(' internal CompressedMemoryExportBundle BuildCompressedMemoryExportBundle(')]
import_body += declarations(imports,["internal bool HasCompressedMemoryDataForHero(","internal bool ApplyCompressedMemoryExportBundle(","internal void ImportSingleNpcDialogueHistoryData("])
write("ImportAdapter.cs","using System;using System.IO;using System.Collections.Generic;using TaleWorlds.Library;namespace AnimusForge;internal sealed class MemoryHistoryImportExportAdapter {"+import_body+"}internal static class NpcDataIdentityFileAdapter {internal static string FindNpcJsonByHeroId(string path,string id)=>null;}")
commit = read("src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs")
commit_methods = declarations(commit,["internal List<ConversationMessage> BuildUncompressedMemoryRoleMessages(","internal List<ConversationMessage> BuildUncompressedMemoryRoleMessagesById("])
commit_leaves='private readonly MemoryBusinessStateOwner _memory;internal MemoryHistoryCommitBannerlordAdapter(MemoryBusinessStateOwner memory){_memory=memory;}private static int GetCurrentSceneSessionIdForDailyMemorySuppression()=>-1;private static bool IsNonHeroMemoryId(string id)=>MemoryBusinessStateOwner.IsNonHeroMemoryId(id);private static int CountDailyMemoryDraftLines(IEnumerable<DailyMemoryDraft> drafts)=>MemoryBusinessStateOwner.CountDailyMemoryDraftLines(drafts);private static void LogNonHeroMemoryTrace(string text){}'
write("UncompressedCapture.cs","using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;using TaleWorlds.CampaignSystem;using static AnimusForge.MemoryRecordRules;using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;namespace AnimusForge;internal sealed class MemoryHistoryCommitBannerlordAdapter {"+commit_leaves+commit_methods+"}")
write("OverviewRead.cs","using System;using System.Collections.Generic;namespace AnimusForge;internal static class MemorySummaryApplicationAdapter {"+extract.declaration(summary,"internal static string BuildMemoryOverviewContextById(")+"}")
snaproot = read("src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.HistoryPromptSnapshot.cs")
write("Snapshot.cs","using System;using System.Collections.Generic;using TaleWorlds.CampaignSystem;using AnimusForge.Refactor.Adapters;namespace AnimusForge;public partial class MyBehavior {"+declarations(snaproot,["internal sealed class HistoryPromptSnapshot","internal static Func<string> CaptureHistoryContextWorkForHero(","internal static Func<string> CaptureHistoryContextWorkById("])+"}")
owner = read("src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs")
marker_methods = []
for signature in ["internal static bool IsValidMemoryCommitMarker(", "internal static bool IsMemoryRecoveryHexDigest("]:
    start = owner.index(signature)
    marker_methods.append(owner[start:owner.index(";", start) + 1])
write("Recovery.cs", "using System.Linq;namespace AnimusForge;internal static class MemoryRecoveryStateOwner {" + "\n".join(marker_methods) + "}")
store = extract.declaration(read("src/AF.Persistence/PlayerExportsStore.cs"), "internal static T ReadJson<T>(")
write("Store.cs", "using System;using System.IO;using System.Text;using Newtonsoft.Json;namespace AnimusForge;internal static class PlayerExportsStore {" + store + 'internal static string ResolveImportFolderPath(string path)=>path;}')

scene = read("src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs")
history = read("src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneHistoryMessages.cs")
scene_methods = declarations(scene, [
    "internal static object CreateChatMessage(", "private static string BuildStrictSceneMessagesSystemPrompt(",
    "private static void AppendStrictSceneUserSections(",
    "internal static string StripScenePersonaBlocks(", "internal static string ExtractTrustPromptBlock(",
    "private static bool IsSceneWeeklyFullReportHeader(", "private static string FormatSceneRuleSection(",
    "private static string FormatSceneKnowledgeSection(", "internal static void SplitSceneExtraSections(",
    "internal static string BuildSceneSystemRuleBlock(", "internal static Task<string> CallNativeConversationApiAsync(",
    "internal static Func<string> CaptureNativeConversationPersistedHistoryWork(",
])
scene_methods += declarations(history, ["private List<object> BuildStrictSceneMessagesForNpc(",
    "internal SceneHistoryMessageAssemblyInput CaptureStrictSceneMessageInputForNpc(", "private static void AppendConversationMessages("])
native = read("src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPrompt.cs")
capture = extract.declaration(native, "private void CapturePromptMessages(")
start = capture.index('string sceneDynamicUserBlock =')
end = capture.index(';', capture.index('messages = _ports.BuildStrictSceneMessagesForNpc(', start)) + 1
prefix = capture[start:end]
if args.mutate == "drop-prefix":
    prefix = prefix.replace('persistedWithoutRecentWindow,', '"",', 1)
ports = read("src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurn.cs")
assert 'BuildStrictSceneMessagesForNpc = BuildStrictSceneMessagesForNpc,' in ports
assert 'persistedHeroHistory = ((await persistedHeroHistoryTask)' in native
assert 'SplitPersistedHeroHistorySections(persistedHeroHistory, out privateRecentWindowSection, out persistedWithoutRecentWindow)' in native
template = read("tests/modules/AF.Module.Prompt/NativeFinalRequestDifferential/Program.cs")
prompt = template[:template.index("internal static class Program")]
for signature in ["internal static class Logger", "internal static class FreezeWatchdog"]:
    prompt = prompt.replace(extract.declaration(prompt, signature), "")
prompt = prompt.replace('internal static class DuelSettings', 'internal sealed class DuelSettings')
prompt = prompt.replace('internal const int DailyConversationHistoryLineLimitMax=200;', 'internal const int DailyConversationHistoryLineLimitMin=10,DailyConversationHistoryLineLimitMax=500; internal float GetMainApiTemperature()=>0.8f;')
prompt = prompt.replace(extract.declaration(prompt, "internal async Task<object> Replay("), read("tests/modules/AF.Module.Memory/ImportedMemoryPromptReplayTests/PromptHarness.cs.txt").replace("@@NATIVE_PREFIX@@", prefix).replace("private static bool IsBannerlordMainThreadForNativeActions()", "internal static bool IsBannerlordMainThreadForNativeActions()"))
transport = read("src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs")
field = transport[transport.index("    private static readonly AsyncLocal<CancellationToken> OwnerCancellation"):]
lifetime = field[:field.index(";") + 1] + declarations(transport, ["internal static IDisposable PushOwnerCancellation(",
    "private sealed class OwnerCancellationScope", "internal static CancellationTokenSource CreateTimeout("])
write("PromptHarness.cs", "using TaleWorlds.CampaignSystem;using AnimusForge.Refactor.Adapters;\n" + prompt.replace("@@TRANSPORT_LIFETIME@@", lifetime))
capture_leaf = """using System;using System.Collections.Generic;using AnimusForge;namespace AnimusForge.Refactor.Runtime{} namespace AnimusForge.Refactor.Adapters {internal static class LlmRequestConfigurationCaptureAdapter {internal static ConversationSpeechTextOptions CaptureSceneSpeechTextOptions()=>new(false,false);internal static int GetMemoryOverviewStartBlockCountFromSettings()=>3;}internal static class SceneLocationPromptCaptureAdapter {internal static string ResolveCurrentMemorySceneLabel()=>MyBehavior.ReadScene();}} namespace AnimusForge {internal static class MemoryEntityIdentityBannerlordAdapter {internal static int GetCurrentGameDayIndexSafe()=>MyBehavior.ReadDay();internal static bool IsMemoryEntityEligibleForCompressedMemory(string id)=>true;internal static bool IsHeroNpcEligibleForCompressedMemory(TaleWorlds.CampaignSystem.Hero h)=>h!=null&&h.IsAlive&&!h.IsDisabled;internal static bool IsNonSceneNativeConversationActiveForMemory()=>false;internal static TaleWorlds.CampaignSystem.Hero FindHeroById(string id)=>TaleWorlds.CampaignSystem.Hero.FindFirst(h=>h.StringId==id);}internal static class CampaignCharacterRecordCaptureAdapter {internal static string GetMemoryHeroId(TaleWorlds.CampaignSystem.Hero hero)=>hero?.StringId??"";}}"""
capture_leaf = capture_leaf.replace(extract.declaration(capture_leaf,"internal static class MemoryEntityIdentityBannerlordAdapter"), "")
write("LlmCaptureLeaf.cs", capture_leaf)
identity = read("src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs")
identity_methods = declarations(identity,["internal static Hero FindHeroById(","internal static bool IsHeroNpcEligibleForCompressedMemory(","internal static bool IsMemoryEntityEligibleForCompressedMemory("])
write("IdentityRules.cs","using System;using TaleWorlds.CampaignSystem;namespace AnimusForge;internal static class MemoryEntityIdentityBannerlordAdapter {internal static int GetCurrentGameDayIndexSafe()=>MyBehavior.ReadDay();internal static bool IsNonSceneNativeConversationActiveForMemory()=>false;"+identity_methods+"}")
composer = read("src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs")
composer_methods = ["internal static bool TryExtractReplyFormatInstruction(","internal static string InjectSceneMechanismPromptSection(","internal static string BuildNativeConversationStreamingVisibleText(","internal static string StripScenePersonaBlocks(","internal static string ExtractTrustPromptBlock(","internal static bool IsSceneWeeklyFullReportHeader(","internal static string FormatSceneRuleSection(","internal static string FormatSceneKnowledgeSection(","internal static void SplitSceneExtraSections(","internal static string BuildSceneSystemRuleBlock("]
write("StreamingProjection.cs", "using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Text.RegularExpressions;namespace AnimusForge.Refactor.Modules;internal static class ScenePromptMessageProjectionComposer {" + declarations(composer,composer_methods) + "}")
scene_capture = read("src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs")
scene_capture_methods = declarations(scene_capture,["internal static string BuildStrictSceneMessagesSystemPrompt(","internal SceneHistoryMessageAssemblyInput CaptureStrictSceneMessageInputForNpc(","internal static Func<string> CaptureNativeConversationPersistedHistoryWork("])
scene_methods += 'internal static int TryGetCurrentSceneHistorySessionIdForHistoryPersistence()=>-1;private static readonly NativeConversationSessionOwner _nativeSessionOwner=new(()=>0,200);private static readonly SceneAgentIdentityPromptCaptureAdapter NativePromptIdentityCapture=new();'

scene_leaves = "private static bool IsDetailedSceneSpeechPromptEnabled()=>false;private static bool ShouldPreserveSceneAsteriskActions()=>false;private static string GetLatestNativeConversationNpcUtteranceForExternal(NativeConversationSessionOwner sessions,Hero hero,CharacterObject character,int id)=>\"\";private static bool TryExtractReplyFormatInstruction(ref string text,out string instruction){instruction=\"\";return false;}private readonly ScenePendingAfefFactsOwner _pendingFacts=new();private static ConversationMessage StampConversationMessageWithCurrentMemoryContext(ConversationMessage m)=>m;private static string GetStrictScenePlayerDisplayName()=>\"Player\";private static IEnumerable<ConversationMessage> CaptureNpcConversationHistory(int id)=>Array.Empty<ConversationMessage>();private static SceneHistoryMessageContext CaptureSceneHistoryMessageContext(int id,bool distance)=>ShoutBehavior.CaptureSceneHistoryMessageContext(id,distance);"
write("SceneCapture.cs", "using System;using System.Collections.Generic;using System.Linq;using AnimusForge;using AnimusForge.Refactor.Modules;using TaleWorlds.CampaignSystem;using NpcDataPacket=AnimusForge.ShoutBehavior.NpcDataPacket;namespace AnimusForge.Refactor.Adapters;internal sealed class SceneHistoryPromptCaptureAdapter {"+scene_leaves+scene_capture_methods+"}internal sealed class SceneAgentIdentityPromptCaptureAdapter {internal NpcDataPacket BuildNativeConversationNpcData(Hero hero,CharacterObject character)=>null;internal static int TryResolveNativeConversationAgentIndex(Hero hero,CharacterObject character)=>7;internal static bool TryResolveWildernessNonHeroMemory(NpcDataPacket npc,Hero hero,CharacterObject character,int index,out string id,out string name){id=name=\"\";return false;}internal static void LogNonHeroMemoryTrace(string text){}internal static float GetPlayerDistanceToAgentForScenePrompt(int i)=>0;internal static string BuildPlayerCustomPromptRuleBlock()=>\"\";internal static string JoinPromptSections(params string[] parts)=>string.Join(\"\\n\",parts.Where(x=>!string.IsNullOrWhiteSpace(x)));}")
scene_methods += 'internal static string BuildSceneCompositeUserBlock(string block,params string[] extras)=>MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(block,extras);internal static string BuildSystemForReplay(string text,bool suppress)=>BuildStrictSceneMessagesSystemPrompt(text,suppress);'
write("SceneProduction.cs", "using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;using System.Text;using System.Threading;using System.Threading.Tasks;using TaleWorlds.CampaignSystem;using AnimusForge.Refactor.Adapters;using AnimusForge.Refactor.Modules;namespace AnimusForge {public partial class ShoutBehavior {" + scene_methods + "}}")
projection = read("src/modules/AF.Module.Prompt/Composition/HistorySectionProjectionOwner.cs")
write("Sections.cs", "using System;using System.Text;namespace AnimusForge;internal static class HistorySectionProjectionOwner {" + declarations(projection, ["internal static void SplitPersistedHeroHistorySections(", "internal static bool IsPrivateRecentWindowHeader("]) + "}")
network = read("src/modules/AF.Module.Llm/ShoutNetwork.cs")
payload = extract.declaration(network, "private static JObject BuildPrimaryChatPayload(")
payload += '''
private static bool TryApplyPrimaryThinkingControls(JObject payload, DuelSettings settings, string url, string model, bool disabled, out string mode) { mode="plain"; return false; }
internal static JObject CapturePayload(List<object> messages, bool stream) => BuildPrimaryChatPayload(messages, new DuelSettings(), "https://replay.invalid/v1/chat/completions", "replay-model", 5000, stream, out _);
'''
write("Payload.cs", "using System;using System.Collections.Generic;using Newtonsoft.Json.Linq;namespace AnimusForge;internal static class ShoutNetwork {" + payload + "}")
linked = [
    "src/modules/AF.Module.Llm/Application/NativeConversationLlmApplicationAdapter.cs",
    "src/AF.Persistence/NpcDataFileName.cs",
    "src/modules/AF.Module.Conversation/Channels/Scene/ScenePendingAfefFactsOwner.cs",
    "src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs",
    "src/modules/AF.Module.Memory/Records/AnimusForgeDialogueHistoryEntry.cs",
    "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.UncompressedMemoryPrompt.cs",
    "src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs",
    "src/modules/AF.Module.Memory/Records/NpcActionEntry.cs",
    "src/modules/AF.Module.Memory/Records/MemoryRecallCandidate.cs",
    "src/modules/AF.Module.Memory/Recall/MemoryRecallContextOwner.cs",
    "src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs",
    "src/modules/AF.Module.Memory/ImportExport/CompressedMemoryExportBundleReader.cs",
    "src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs",
    "src/modules/AF.Module.Prompt/Composition/PreprocessFormatException.cs",
    "src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs",
    "src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs",
    "src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs",
    "src/modules/AF.Module.Prompt/Composition/UncompressedMemoryMessageAssemblyOwner.cs",
    "src/modules/AF.Module.Prompt/Composition/NativeMemoryHistoryMergeOwner.cs",
    "src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs",
    "src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs",
    "src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs",
    "src/AF.Contracts/Internal/InteractionContracts.cs", "src/AF.Contracts/Internal/LlmContracts.cs",
    "src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs", "src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs",
]
for path in linked:
    read(path)
if args.mutate == "wrong-import-store":
    owner_path = "src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs"
    owner_text = read(owner_path)
    marker = "MemoryImportExportState state = Capture(authority);"
    assert owner_text.count(marker) == 1
    write("WrongImportOwner.cs", owner_text.replace(marker, "authority = new MemoryBusinessStateOwner();\n        " + marker))
    linked.remove(owner_path)
dotnet = resolve_dotnet(ROOT)
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><DefineConstants>CURRENT</DefineConstants><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit><NoWarn>CS0649;CS0169;CS0414</NoWarn></PropertyGroup><ItemGroup>'
project += ''.join('<Compile Include="' + escape(str(ROOT / path)) + '" />' for path in linked)
project += '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll")) + '</HintPath></Reference></ItemGroup></Project>'
write("Proof.csproj", project)
write("NuGet.Config", '<configuration><packageSources><clear/></packageSources></configuration>')
env = minimal_test_environment(dotnet, out)
command = [str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--", str(args.memory_file.resolve()) if args.memory_file else "", str(out)]
result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180)
log = result.stdout + result.stderr
write("run.log", log)
evidence = {"revision": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
            "sources": sources, "mutation": args.mutate, "exitCode": result.returncode,
            "testFiles": {path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in HERE.iterdir() if path.is_file()},
            "generatedSources": {path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in out.glob("*.cs")},
            "directoryDiscovery": "STUBBED; direct file import only",
            "inputSha256": hashlib.sha256(args.memory_file.read_bytes()).hexdigest() if args.memory_file else "synthetic",
            "game": "STUBBED", "network": "STUBBED", "liveModel": "NOT_RUN"}
write("evidence.json", json.dumps(evidence, indent=2))
print(log)
print("EVIDENCE=" + str(out / "evidence.json"))
raise SystemExit(result.returncode)
