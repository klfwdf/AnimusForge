"""Run old/current production Lore retrieval and rendering over identical fake game state."""
from __future__ import annotations

import importlib.util
import base64
import os
import json
import argparse
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument("--emit-json", action="store_true")
parser.add_argument("--mutate", choices=["ignore-stale-version", "drop-lore-output"])
args = parser.parse_args()
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

markers = (
    "private bool TryFindRuleByExactKeyword(",
    "private static bool IsPlayerPersonaRule(",
    "private static bool CanInjectKnowledgeRule(",
    "private bool TryAppendExactKeywordSlotLore(",
    "private string ApplyRuleTextMappings(",
    "private string BuildLoreContextInternal(",
    "public string BuildLoreContext(string inputText, Hero npcHero, string secondaryInput, MentionedWorldEntities mentionedEntities)",
    "public long GetRuleDataVersionForExternal(",
    "private static string RoleFromOccupation(",
    "private static LoreVariant PickBestVariant(",
    "private static bool IsMatch(",
)

dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
outputs = {}
for name in ("old", "current"):
    if name == "old":
        source = subprocess.check_output(["git", "show", "77a3d234:KnowledgeLibraryBehavior.cs"], cwd=ROOT).decode("utf-8-sig")
        ai_source = subprocess.check_output(["git", "show", "77a3d234:AIConfigHandler.cs"], cwd=ROOT).decode("utf-8-sig")
        index = subprocess.check_output(["git", "show", "77a3d234:src/modules/AF.Module.Knowledge/Index/KnowledgeRuleIndex.cs"], cwd=ROOT).decode("utf-8-sig")
        retriever = subprocess.check_output(["git", "show", "77a3d234:src/modules/AF.Module.Knowledge/Lore/LoreCandidateRetriever.cs"], cwd=ROOT).decode("utf-8-sig")
    else:
        source = (ROOT / "KnowledgeLibraryBehavior.cs").read_text(encoding="utf-8-sig")
        ai_source = (ROOT / "AIConfigHandler.cs").read_text(encoding="utf-8-sig")
        index = (ROOT / "src/modules/AF.Module.Knowledge/Index/KnowledgeRuleIndex.cs").read_text(encoding="utf-8-sig")
        retriever = (ROOT / "src/modules/AF.Module.Knowledge/Lore/LoreCandidateRetriever.cs").read_text(encoding="utf-8-sig")
    out = ROOT / "artifacts/tests/j06-lore-text-differential" / (args.mutate or "normal") / str(os.getpid()) / name
    out.mkdir(parents=True, exist_ok=True)
    selected_markers = markers + (("internal string BuildLoreContextWithCandidates(",) if name == "current" else ())
    methods = "\n".join(extract.declaration(source, marker) for marker in selected_markers)
    if name == "current" and args.mutate == "drop-lore-output":
        needle = "stringBuilder.AppendLine(text3);"
        assert methods.count(needle) == 1
        methods = methods.replace(needle, "", 1)
    (out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nnamespace AnimusForge { public partial class KnowledgeLibraryBehavior {\n" + methods + "\n}}\n", encoding="utf-8")
    ai_marker = "internal static string GetLoreContextWithCandidates(string inputText, Hero npcHero," if name == "current" else "public static string GetLoreContext(string inputText, Hero npcHero, string secondaryInput, MentionedWorldEntities mentionedEntities)"
    ai_method = extract.declaration(ai_source, ai_marker)
    if name == "current" and args.mutate == "ignore-stale-version":
        needle = "instance.GetRuleDataVersionForExternal() == candidateVersion"
        assert ai_method.count(needle) == 1
        ai_method = ai_method.replace(needle, "true", 1)
    (out / "AIConfig.cs").write_text("using System;\nnamespace AnimusForge { public static class AIConfigHandler {\n" + ai_method + "\n}}\n", encoding="utf-8")
    (out / "Index.cs").write_text(index, encoding="utf-8")
    (out / "Retriever.cs").write_text(retriever, encoding="utf-8")
    (out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
    (out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit>' + ('<DefineConstants>CURRENT</DefineConstants>' if name == "current" else '') + '</PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/><Compile Include="AIConfig.cs"/><Compile Include="Index.cs"/><Compile Include="Retriever.cs"/></ItemGroup></Project>', encoding="utf-8")
    (out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    result = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        print(name, result.stdout, result.stderr, sep="\n")
        raise SystemExit(result.returncode)
    outputs[name] = {}
    for line in result.stdout.splitlines():
        for label in ("RESULT", "FALLBACK"):
            if line.startswith(label + "="):
                outputs[name][label] = base64.b64decode(line[len(label) + 1 :], validate=True)
    if "RESULT" not in outputs[name]:
        raise RuntimeError(name + " did not produce a Lore result")
assert outputs["old"]["RESULT"] == outputs["current"]["RESULT"] == outputs["current"]["FALLBACK"], "old/current/stale Lore bytes differ"
print("PASS production Lore candidate retrieval and text formatting: old/current/stale byte parity; nonempty rule and branch counts asserted")
if args.emit_json:
    print("EXPORT_JSON=" + json.dumps({side: {key: base64.b64encode(value).decode("ascii") for key, value in rows.items()} for side, rows in outputs.items()}, sort_keys=True))
