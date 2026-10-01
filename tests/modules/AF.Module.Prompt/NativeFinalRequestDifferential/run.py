"""Build old/current Native production final-message assembly over deterministic port state."""
from __future__ import annotations

import importlib.util
import os
import sys
import argparse
import json
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
    methods = "\n".join(extract.declaration(source, marker) for marker in markers)
    out = output / revision
    out.mkdir(parents=True, exist_ok=True)
    (out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\nusing System.Linq;\nusing System.Text;\nusing System.Threading;\nusing System.Threading.Tasks;\nnamespace AnimusForge { public partial class ShoutBehavior {\n" + methods + "\n}}\n", encoding="utf-8")
    transport=(ROOT / "src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs").read_text(encoding="utf-8-sig")
    field=transport[transport.index("    private static readonly AsyncLocal<CancellationToken> OwnerCancellation"):]
    field=field[:field.index(";")+1]
    lifetime=field+"\n"+"\n".join(extract.declaration(transport, marker) for marker in ["internal static IDisposable PushOwnerCancellation(", "private sealed class OwnerCancellationScope", "internal static CancellationTokenSource CreateTimeout("])
    (out / "Program.cs").write_text((HERE / "Program.cs").read_text(encoding="utf-8-sig").replace("@@TRANSPORT_LIFETIME@@",lifetime),encoding="utf-8")
    (out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs" /><Compile Include="Production.cs" /></ItemGroup></Project>', encoding="utf-8")
    if revision == "current":
        project=(out / "Proof.csproj").read_text(encoding="utf-8")
        extra=[ROOT / "src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs", ROOT / "src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs"]
        project=project.replace("</PropertyGroup>", "<DefineConstants>CURRENT</DefineConstants></PropertyGroup>")
        project=project.replace("</ItemGroup>", "".join('<Compile Include="'+str(path)+'" />' for path in extra)+"</ItemGroup>")
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
