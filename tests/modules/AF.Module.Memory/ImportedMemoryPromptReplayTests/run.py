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
memory = "using System.IO;\nusing Newtonsoft.Json;\n" + memory + read("tests/modules/AF.Module.Memory/ImportedMemoryPromptReplayTests/Harness.cs.txt")
if args.mutate == "drop-overview":
    memory = memory.replace('stringBuilder.AppendLine(memoryOverviewContext);', 'stringBuilder.AppendLine("");', 1)
if args.mutate == "wrong-import-store":
    memory = memory.replace('Overviews = _memoryOverviewStates,', 'Overviews = new Dictionary<string, MemoryOverviewState>(),', 1)
write("MemoryHarness.cs", memory)

business = read("src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs")
fields = business[business.index("    internal Dictionary<string, List<DailyMemoryDraft>> Drafts"):business.index("    internal List<DailyMemoryDraft> LoadDrafts(")]
queue = read("src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.Queues.cs")
business_slice = fields + declarations(business, ["internal List<DailyMemoryDraft> LoadDrafts(", "internal List<CompressedMemoryBlock> LoadBlocks("])
business_slice += extract.declaration(queue, "internal MemoryOverviewState GetMemoryOverviewState(")
business_slice += 'internal Dictionary<string,List<MyBehavior.DialogueDay>> History;'
write("Business.cs", "using System;using System.Collections.Generic;using System.Linq;namespace AnimusForge;internal sealed class MemoryBusinessStateOwner {" + business_slice + "}")
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
    "private static void AppendStrictSceneUserSections(", "internal static string BuildSceneCompositeUserBlock(",
    "internal static string StripScenePersonaBlocks(", "internal static string ExtractTrustPromptBlock(",
    "private static bool IsSceneWeeklyFullReportHeader(", "private static string FormatSceneRuleSection(",
    "private static string FormatSceneKnowledgeSection(", "internal static void SplitSceneExtraSections(",
    "internal static string BuildSceneSystemRuleBlock(", "internal static async Task<string> CallNativeConversationApiAsync(",
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
prompt = prompt.replace('internal static bool IsBuiltInSceneReplyFormatPromptDisabled()', 'internal const int DailyConversationHistoryLineLimitMin=10,DailyConversationHistoryLineLimitMax=500; internal static int GetDailyConversationHistoryLineLimitForExternal()=>20; internal float GetMainApiTemperature() => 0.8f; internal static bool IsBuiltInSceneReplyFormatPromptDisabled()')
prompt = prompt.replace(extract.declaration(prompt, "internal async Task<object> Replay("), read("tests/modules/AF.Module.Memory/ImportedMemoryPromptReplayTests/PromptHarness.cs.txt").replace("@@NATIVE_PREFIX@@", prefix))
transport = read("src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs")
field = transport[transport.index("    private static readonly AsyncLocal<CancellationToken> OwnerCancellation"):]
lifetime = field[:field.index(";") + 1] + declarations(transport, ["internal static IDisposable PushOwnerCancellation(",
    "private sealed class OwnerCancellationScope", "internal static CancellationTokenSource CreateTimeout("])
write("PromptHarness.cs", "using TaleWorlds.CampaignSystem;\n" + prompt.replace("@@TRANSPORT_LIFETIME@@", lifetime))
write("SceneProduction.cs", "using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;using System.Text;using System.Threading;using System.Threading.Tasks;using TaleWorlds.CampaignSystem;namespace AnimusForge {public partial class ShoutBehavior {" + scene_methods + "}}")
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
    "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.HistoryPromptSnapshot.cs",
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
    "src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs",
    "src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs",
    "src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs",
    "src/AF.Contracts/Internal/InteractionContracts.cs", "src/AF.Contracts/Internal/LlmContracts.cs",
    "src/modules/AF.Module.Llm/Protocol/PrimaryChatMessagePolicy.cs", "src/modules/AF.Module.Llm/Protocol/LlmApiCompat.cs",
]
for path in linked:
    read(path)
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
            "inputSha256": hashlib.sha256(args.memory_file.read_bytes()).hexdigest() if args.memory_file else "synthetic",
            "game": "STUBBED", "network": "STUBBED", "liveModel": "NOT_RUN"}
write("evidence.json", json.dumps(evidence, indent=2))
print(log)
print("EVIDENCE=" + str(out / "evidence.json"))
raise SystemExit(result.returncode)
