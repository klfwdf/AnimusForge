"""Execute the production single-rule renderer and prompt disk helpers in isolated fixtures.

Game/ONNX leaf ports are fake; the actual ranking, variant matching and settings
read/write declarations are compiled. All synthetic disk writes stay in a new run root.
"""
from pathlib import Path
import argparse
import importlib.util
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

p = argparse.ArgumentParser()
p.add_argument("--settings-only", action="store_true")
p.add_argument("--prompt-language", choices=["zh-CN", "en"], default="zh-CN")
p.add_argument("--mutate", choices=["empty-resets-default", "take-last", "ignore-variant-conditions"])
p.add_argument("--run-root", type=Path)
p.add_argument("--framework", choices=["net8.0", "net10.0"], default="net8.0")
args = p.parse_args()
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
out = new_run_root(ROOT, "persona-lore-settings", args.run_root)
dotnet = resolve_dotnet(ROOT)
settings = (ROOT / "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs").read_text(encoding="utf-8-sig")
methods = "\n".join(extract.declaration(settings, marker) for marker in (
    "private sealed class CustomPromptTextJson",
    "private static void PersistCustomPromptTextFileUnlocked(",
    "private static void WriteCustomPromptTextJsonFileUnlocked(",
    "private static bool TryReadCustomPromptTextJsonFile(",
    "private static bool TryReadLayeredCustomPromptTextJsonFile(",
    "private static Dictionary<string, HashSet<string>> LoadKnownPackagedCustomPromptHashes(",
    "internal static bool IsKnownPackagedCustomPromptFile(",
    "internal static string GetCustomPromptUserReadPath(",
))
for start,end in [("private static readonly Lazy<Dictionary<string, HashSet<string>>> KnownPackagedCustomPromptHashes","private static Dictionary<string, HashSet<string>> LoadKnownPackagedCustomPromptHashes"),("private static readonly object KnownPackagedPromptHashGate","internal static bool IsKnownPackagedCustomPromptFile")]:
    methods += "\n" + settings[settings.index(start):settings.index(end)]
if args.mutate == "empty-resets-default":
    clause = "WriteCustomPromptTextJsonFileUnlocked(path, text);"
    assert methods.count(clause) == 1
    methods = methods.replace(clause, 'if (string.IsNullOrWhiteSpace(text)) text = "MUTATED_DEFAULT"; ' + clause, 1)
fixture = (HERE / "LoreAndSettings.cs.txt").read_text(encoding="utf-8")
fixture = fixture.replace("@@SETTINGS@@", methods)
files = ["Program.cs", "DataPaths.cs"]
(out/"DataPaths.cs").write_bytes((ROOT/"src/AF.Persistence/AnimusForgeDataPaths.cs").read_bytes())
resources = ""
if not args.settings_only:
    source = (ROOT / "src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs").read_text(encoding="utf-8-sig")
    markers = (
        "internal static LoreRule CollectPersonaLoreRule(",
        "internal static LoreCandidateRules CollectPromptLoreCandidates(",
        "internal string BuildPersonaLoreSource(",
        "public long GetRuleDataVersionForExternal(",
        "private static LoreVariant PickBestVariant(",
        "private static bool IsMatch(",
        "private static string RoleFromOccupation(",
        "private static bool IsPlayerPersonaRule(",
        "private static bool CanInjectKnowledgeRule(",
        "private string ApplyRuleTextMappings(",
    )
    methods = "\n".join(extract.declaration(source, m) for m in markers)
    if args.mutate == "take-last":
        needle = '?.OrderedRules?.FirstOrDefault()'
        assert methods.count(needle) == 1
        methods = methods.replace(needle, '?.OrderedRules?.LastOrDefault()', 1)
    elif args.mutate == "ignore-variant-conditions":
        selected = extract.declaration(source, "private static LoreVariant PickBestVariant(")
        methods = methods.replace(selected, selected[:selected.index('{')] + '{ return rule?.Variants?.FirstOrDefault(); }', 1)
    (out / "Knowledge.cs").write_text("using System;using System.Linq;using System.Collections.Generic;using System.Threading;namespace AnimusForge {public partial class KnowledgeLibraryBehavior {" + methods + "}}", encoding="utf-8")
    leaf = (ROOT / "tests/modules/AF.Module.Knowledge/LoreTextDifferential/Program.cs").read_text(encoding="utf-8").split("internal static class Program")[0]
    leaf = "using System.Threading;\n" + leaf
    leaf = leaf.replace("new FakePorts(), () => _file.Rules", "Ports, () => _file.Rules")
    leaf = leaf.replace("Index.EnsureVectorIndex();", "Index.EnsureVectorIndex(); Index.EnsureOnnxIndex();")
    leaf = leaf.replace('=> "";\n        public string Render(', '=> "mapped-value";\n        public string Render(', 1)
    # This existing differential fixture's Render method is not exercised here.
    leaf = leaf.replace("AIConfigHandler.GetLoreContext(input, npc, \"\", mentions)", "\"unused\"")
    start = leaf.index("    internal sealed class FakePorts")
    leaf = leaf[:start] + "}\n"
    leaf = leaf.replace("private static KnowledgeRuleIndex Index;", "private static KnowledgeRuleIndex Index; internal static FakePorts Ports=new(); private static readonly AsyncLocal<PromptLoreSettings> PromptLoreSettingsScope=new(); internal static void TouchForTest()=>Index.Touch();")
    (out / "Leaves.cs").write_text(leaf, encoding="utf-8")
    for name, relative in (("Index.cs", "Index/KnowledgeRuleIndex.cs"), ("Retriever.cs", "Lore/LoreCandidateRetriever.cs")):
        (out / name).write_bytes((ROOT / "src/modules/AF.Module.Knowledge" / relative).read_bytes())
    (out / "KnowledgePromptTexts.cs").write_bytes((ROOT / "src/modules/AF.Module.Knowledge/Host/KnowledgePromptTexts.cs").read_bytes())
    prompt_file = "KnowledgeRagPromptTexts.en.json" if args.prompt_language == "en" else "KnowledgeRagPromptTexts.json"
    (out / "KnowledgeRagPromptTexts.json").write_bytes((ROOT / "content/modules/AF.Module.Knowledge/ModuleData" / prompt_file).read_bytes())
    resources = '<None Include="KnowledgeRagPromptTexts.json" CopyToOutputDirectory="PreserveNewest"/>'
    files += ["Knowledge.cs", "Leaves.cs", "Index.cs", "Retriever.cs", "KnowledgePromptTexts.cs"]
fixture = fixture.replace("@@RUN_LORE@@", "" if args.settings_only else "LoreCases(args[2]);")
(out / "Program.cs").write_text(fixture, encoding="utf-8")
newtonsoft = dotnet.parent / "sdk/8.0.425/Newtonsoft.Json.dll"
if not newtonsoft.is_file():
    newtonsoft = next(iter(sorted((dotnet.parent / "sdk").glob("*/Newtonsoft.Json.dll"))), ROOT / "_deps_auto/Newtonsoft.Json.dll")
defines = "" if args.settings_only else "<DefineConstants>PERSONA_LORE</DefineConstants>"
items = "".join(f'<Compile Include="{f}"/>' for f in files) + resources
(out / "Proof.csproj").write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>{args.framework}</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit>{defines}</PropertyGroup><ItemGroup>{items}<Reference Include="Newtonsoft.Json"><HintPath>{newtonsoft.as_posix()}</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "-p:RestoreConfigFile=" + str(out / "NuGet.Config"), "--", str(out), str(ROOT / "content/modules/AF.Module.Persona/CustomPrompts/NpcPersonaGenerationRequirements.json"), args.prompt_language], cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding="utf-8", errors="replace")
(out / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
print(result.stdout + result.stderr, end="")
raise SystemExit(result.returncode)
