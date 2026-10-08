"""Build old/current Native production final-message assembly over deterministic port state."""
from __future__ import annotations

import importlib.util
import os
import sys
import argparse
import json
import re
import hashlib
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument("--run-root", type=Path);args=parser.parse_args()
output=new_run_root(ROOT,"j06-native-final",args.run_root)
outputs={}
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, output)
markers = (
    "private List<object> BuildStrictSceneMessagesForNpc(",
    "private static object CreateChatMessage(",
    "private static string BuildStrictSceneMessagesSystemPrompt(",
    "private static void AppendStrictSceneUserSections(",
    "private static string BuildSceneCompositeUserBlock(",
    "private static string StripScenePersonaBlocks(",
    "private static string ExtractTrustPromptBlock(",
    "private static bool IsSceneWeeklyFullReportHeader(",
    "private static string FormatSceneRuleSection(",
    "private static string FormatSceneKnowledgeSection(",
    "private static void SplitSceneExtraSections(",
    "private static string BuildSceneSystemRuleBlock(",
    "private static bool TryConvertSceneMessageToStrictChatMessage(ConversationMessage msg, int npcAgentIndex, out object chatMessage, HashSet<string>",
    "private static void AppendConversationMessages(",
    "private static List<ConversationMessage> SortConversationMessagesByEventSequence(",
    "private static bool IsAfefConversationMessage(",
    "private static List<ConversationMessage> KeepAfefFactsAndRecentConversationMessages(",
    "private static async Task<string> CallNativeConversationApiAsync(",
)
for revision in ("old", "current"):
    if revision == "old":
        source = subprocess.check_output(["git", "show", "77a3d234:ShoutBehavior.cs"], cwd=ROOT).decode("utf-8-sig")
    else:
        source = (ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs").read_text(encoding="utf-8-sig")
    if revision == "old":
        # Execute the fixed original metadata helpers, not the prior abbreviated [speaker] fixture stub.
        metadata_markers = ("private static string PrefixConversationMessageForPrompt(", "private static string BuildConversationMessageMetadataPrefix(", "private static int ClampMemoryPromptHour(")
        methods = "\n".join(extract.declaration(source, marker) for marker in markers + metadata_markers)
    else:
        # Current capture + detached real assembly owners; never use a historical projection here.
        current_markers = ('internal static object CreateChatMessage(', 'private static string BuildStrictSceneMessagesSystemPrompt(', 'private static void AppendStrictSceneUserSections(', 'internal static string BuildSceneCompositeUserBlock(', 'internal static string StripScenePersonaBlocks(', 'internal static string ExtractTrustPromptBlock(', 'private static bool IsSceneWeeklyFullReportHeader(', 'private static string FormatSceneRuleSection(', 'private static string FormatSceneKnowledgeSection(', 'internal static void SplitSceneExtraSections(', 'internal static string BuildSceneSystemRuleBlock(', 'internal static Task<string> CallNativeConversationApiAsync(')
        history = (ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneHistoryMessages.cs").read_text(encoding="utf-8-sig")
        history_markers = ('private List<object> BuildStrictSceneMessagesForNpc(', 'internal SceneHistoryMessageAssemblyInput CaptureStrictSceneMessageInputForNpc(', 'private static void AppendConversationMessages(')
        methods = "\n".join(extract.declaration(source, marker) for marker in current_markers)
        methods += "\n" + "\n".join(extract.declaration(history, marker) for marker in history_markers)
    out = output / revision
    out.mkdir(parents=True, exist_ok=True)
    (out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\nusing System.Linq;\nusing System.Text;\nusing System.Threading;\nusing System.Threading.Tasks;\n#if CURRENT\nusing AnimusForge.Refactor.Adapters;\nusing AnimusForge.Refactor.Modules;\n#endif\nnamespace AnimusForge { public partial class ShoutBehavior {\n" + methods + "\n}}\n", encoding="utf-8")
    transport=(ROOT / "src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs").read_text(encoding="utf-8-sig")
    field=transport[transport.index("    private static readonly AsyncLocal<CancellationToken> OwnerCancellation"):]
    field=field[:field.index(";")+1]
    lifetime=field+"\n"+"\n".join(extract.declaration(transport, marker) for marker in ["internal static IDisposable PushOwnerCancellation(", "private sealed class OwnerCancellationScope", "internal static CancellationTokenSource CreateTimeout("])
    program=(HERE / "Program.cs").read_text(encoding="utf-8-sig").replace("@@TRANSPORT_LIFETIME@@",lifetime)
    if revision == "current":
        # Two unchanged capture algorithms; all original lower history/game/settings leaves
        # are synthetic. Current Native request mapping is the whole application source.
        capture_path="src/AF.GameAdapter.Bannerlord/Prompt/SceneHistoryPromptCaptureAdapter.cs"
        capture_source=(ROOT/capture_path).read_text(encoding="utf-8-sig")
        capture_markers=['internal static string BuildStrictSceneMessagesSystemPrompt(', 'internal SceneHistoryMessageAssemblyInput CaptureStrictSceneMessageInputForNpc(']
        capture_methods="\n".join(extract.declaration(capture_source,marker) for marker in capture_markers)
        composer_path="src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs"
        composer_source=(ROOT/composer_path).read_text(encoding="utf-8-sig")
        names=['StripScenePersonaBlocks','ExtractTrustPromptBlock','IsSceneWeeklyFullReportHeader','FormatSceneRuleSection','FormatSceneKnowledgeSection','SplitSceneExtraSections','BuildSceneSystemRuleBlock','InjectSceneMechanismPromptSection','TryExtractReplyFormatInstruction','BuildNativeConversationStreamingVisibleText']
        composer_methods=[]
        for name in names:
            matches=re.findall(r'(?:internal|private) static [^\n(]+ '+name+r'\(',composer_source)
            assert len(matches)==1, "composer declaration drift: "+name
            composer_methods.append(extract.declaration(composer_source,matches[0]))
        session_source=(ROOT/'src/modules/AF.Module.Conversation/Channels/Native/NativeConversationSessionOwner.cs').read_text(encoding="utf-8-sig")
        limit=extract.declaration(session_source,'internal static int ResolveHistoryLineLimit(')
        fixture="""
namespace AnimusForge.Refactor.Runtime {internal class FixtureNamespace {}}
namespace AnimusForge {
 internal sealed class NativeConversationSessionOwner { @LIMIT@ }
}
namespace AnimusForge.Refactor.Modules {
 internal static class ScenePromptMessageProjectionComposer { @COMPOSER@ }
}
namespace AnimusForge.Refactor.Adapters {
 internal static class LlmRequestConfigurationCaptureAdapter {internal static ConversationSpeechTextOptions CaptureSceneSpeechTextOptions()=>new(false,false);}
 internal static class SceneAgentIdentityPromptCaptureAdapter {
  internal static string BuildPlayerCustomPromptRuleBlock()=>"";
  internal static string JoinPromptSections(params string[] values)=>string.Join("\\n",values.Where(x=>!string.IsNullOrWhiteSpace(x)));
  internal static float GetPlayerDistanceToAgentForScenePrompt(int index)=>1f;
 }
 internal sealed class SceneHistoryPromptCaptureAdapter {
  internal sealed class PendingFacts {internal List<ConversationMessage> Consume(int index)=>new();}
  readonly PendingFacts _pendingFacts=new();
  static ConversationMessage StampConversationMessageWithCurrentMemoryContext(ConversationMessage m)=>m;
  static string GetStrictScenePlayerDisplayName()=>"Player";
  static List<ConversationMessage> CaptureNpcConversationHistory(int index)=>new();
  static SceneHistoryMessageContext CaptureSceneHistoryMessageContext(int index,bool distance)=>ShoutBehavior.CaptureSceneHistoryMessageContext(index,distance);
  @CAPTURE@
 }
}
""".replace('@LIMIT@',limit).replace('@COMPOSER@',"\n".join(composer_methods)).replace('@CAPTURE@',capture_methods)
        (out/'CurrentCapture.cs').write_text("using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nusing System.Text.RegularExpressions;\nusing AnimusForge.Refactor.Modules;\n"+fixture,encoding="utf-8")
        receipt={"capturePath":capture_path,"captureRawSha256":hashlib.sha256((ROOT/capture_path).read_bytes()).hexdigest(),"captureMarkers":capture_markers,"captureDeclarationSha256":[hashlib.sha256(extract.declaration(capture_source,m).encode()).hexdigest() for m in capture_markers],"composerPath":composer_path,"composerRawSha256":hashlib.sha256((ROOT/composer_path).read_bytes()).hexdigest(),"composerMethods":names,"layer":"Current source-extracted capture/system/pure declarations; original lower settings, history stores/context and network/normalizer seams controlled; whole actual Native application linked. No historical input supplies current behavior."}
        (out/'current-capture-inputs.json').write_text(json.dumps(receipt,indent=2),encoding="utf-8")
    (out / "Program.cs").write_text(program,encoding="utf-8")
    (out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs" /><Compile Include="Production.cs" /></ItemGroup></Project>', encoding="utf-8")
    if revision == "current":
        project=(out / "Proof.csproj").read_text(encoding="utf-8")
        extra=[ROOT / path for path in ['src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs', 'src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs', 'src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs', 'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs', 'src/modules/AF.Module.Conversation/Internal/History/ConversationSpeechTextRules.cs', 'src/modules/AF.Module.Conversation/Internal/History/SceneHistoryProjectionOwner.cs', 'src/AF.Contracts/Internal/InteractionContracts.cs', 'src/AF.Contracts/Internal/LlmContracts.cs', 'src/modules/AF.Module.Llm/Application/NativeConversationLlmApplicationAdapter.cs']]
        project=project.replace("</PropertyGroup>", "<DefineConstants>CURRENT</DefineConstants></PropertyGroup>")
        project=project.replace("</ItemGroup>", '<Compile Include="CurrentCapture.cs" />'+"".join('<Compile Include="'+str(path)+'" />' for path in extra)+"</ItemGroup>")
        (out / "Proof.csproj").write_text(project, encoding="utf-8")
    (out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    result = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        print(revision, result.stdout, result.stderr, sep="\n")
        raise SystemExit(result.returncode)
    print("BUILD", revision, "production Native final message assembly")

    outputs[revision]=str(out / "bin/Release/net8.0/Proof.dll")
(output / "outputs.json").write_text(json.dumps(outputs,indent=2),encoding="utf-8")
print("OUTPUTS="+str(output / "outputs.json"))
