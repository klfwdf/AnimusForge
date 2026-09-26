"""Build old/current Native production final-message assembly over deterministic port state."""
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
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
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
        source = (ROOT / "ShoutBehavior.cs").read_text(encoding="utf-8-sig")
    methods = "\n".join(extract.declaration(source, marker) for marker in markers)
    out = ROOT / "artifacts/tests/j06-native-final" / revision
    out.mkdir(parents=True, exist_ok=True)
    (out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\nusing System.Linq;\nusing System.Text;\nusing System.Threading;\nusing System.Threading.Tasks;\nnamespace AnimusForge { public partial class ShoutBehavior {\n" + methods + "\n}}\n", encoding="utf-8")
    (out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
    (out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs" /><Compile Include="Production.cs" /></ItemGroup></Project>', encoding="utf-8")
    (out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    result = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        print(revision, result.stdout, result.stderr, sep="\n")
        raise SystemExit(result.returncode)
    print("BUILD", revision, "production Native final message assembly")
