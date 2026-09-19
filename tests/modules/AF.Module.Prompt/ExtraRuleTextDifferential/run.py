"""Compare old synchronous and new worker/owner production extra-rule text."""
from __future__ import annotations

import argparse
import base64
import importlib.util
import os
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["drop-rule-output", "skip-lexical"])
parser.add_argument("--emit-json", action="store_true")
args = parser.parse_args()
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")

shared_markers = (
    "private static string NormalizeSemanticText(",
    "private static bool IsBuiltInRuleTag(",
    "private static HashSet<string> BuildExcludedRuleIdSet(IEnumerable<string> excludedRuleIds)",
    "private static HashSet<string> BuildExcludedRuleIdSet(IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions)",
    "private static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions)",
    "private static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions, out MentionedWorldEntities mentionedEntities)",
    "public static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds)",
    "private static bool TryLexicalRuleKeywordHit(",
    "private static List<GuardrailRuleHit> MergeStickyGuardrailRuleHits(",
    "public static string BuildMatchedExtraRuleInstructions(string input, string secondaryInput, int maxRules, bool hasAnyHero, IEnumerable<string> excludedRuleIds)",
)
outputs = {}
for name in ("old", "current"):
    if name == "old":
        source = subprocess.check_output(["git", "show", "77a3d234:AIConfigHandler.cs"], cwd=ROOT).decode("utf-8-sig")
        ranking = subprocess.check_output(["git", "show", "77a3d234:src/modules/AF.Module.Prompt/Retrieval/PromptRuleRanking.cs"], cwd=ROOT).decode("utf-8-sig")
        sticky = subprocess.check_output(["git", "show", "77a3d234:src/modules/AF.Module.Prompt/Retrieval/PromptStickyRuleStore.cs"], cwd=ROOT).decode("utf-8-sig")
        eval_models = subprocess.check_output(["git", "show", "77a3d234:src/modules/AF.Module.Prompt/Retrieval/PromptRuleEvaluationModels.cs"], cwd=ROOT).decode("utf-8-sig")
        hit_model = subprocess.check_output(["git", "show", "77a3d234:GuardrailRuleHit.cs"], cwd=ROOT).decode("utf-8-sig")
        config_model = subprocess.check_output(["git", "show", "77a3d234:GuardrailRulePromptConfig.cs"], cwd=ROOT).decode("utf-8-sig")
    else:
        source = (ROOT / "AIConfigHandler.cs").read_text(encoding="utf-8-sig")
        ranking = (ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptRuleRanking.cs").read_text(encoding="utf-8-sig")
        sticky = (ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptStickyRuleStore.cs").read_text(encoding="utf-8-sig")
        eval_models = (ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptRuleEvaluationModels.cs").read_text(encoding="utf-8-sig")
        hit_model = (ROOT / "GuardrailRuleHit.cs").read_text(encoding="utf-8-sig")
        config_model = (ROOT / "GuardrailRulePromptConfig.cs").read_text(encoding="utf-8-sig")
    lexical_marker = "private static List<GuardrailRuleHit> GetGuardrailLexicalRuleHits(string input, string secondaryInput, int maxCount = 0, bool includeBuiltInRules = false, IEnumerable<string> excludedRuleIds = null" 
    markers = shared_markers + (lexical_marker,)
    if name == "current":
        markers += ("internal static List<GuardrailRuleHit> GetMatchedExtraRuleHitsForWorker(", "internal static string FormatMatchedExtraRuleInstructions(")
    methods = "\n".join(extract.declaration(source, marker) for marker in markers)
    if name == "current" and args.mutate == "drop-rule-output":
        needle = 'stringBuilder.AppendLine(value);'
        assert methods.count(needle) == 1
        methods = methods.replace(needle, '', 1)
    if name == "current" and args.mutate == "skip-lexical":
        needle = "if (hits == null || hits.Count == 0)"
        assert methods.count(needle) == 1
        methods = methods.replace(needle, "if (false)", 1)
    out = ROOT / "artifacts/tests/j06-extra-rule-text-differential" / (args.mutate or "normal") / str(os.getpid()) / name
    out.mkdir(parents=True, exist_ok=True)
    (out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nnamespace AnimusForge { internal static partial class AIConfigHandler {\n" + methods + "\n}}\n", encoding="utf-8")
    for filename, content in (("Ranking.cs", ranking), ("Sticky.cs", sticky), ("EvalModels.cs", eval_models), ("HitModel.cs", hit_model), ("ConfigModel.cs", config_model)):
        (out / filename).write_text(content, encoding="utf-8")
    (out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
    (out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit>' + ('<DefineConstants>CURRENT</DefineConstants>' if name == "current" else '') + '</PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/><Compile Include="Ranking.cs"/><Compile Include="Sticky.cs"/><Compile Include="EvalModels.cs"/><Compile Include="HitModel.cs"/><Compile Include="ConfigModel.cs"/></ItemGroup></Project>', encoding="utf-8")
    (out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    result = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        print(name, result.stdout, result.stderr, sep="\n")
        raise SystemExit(result.returncode)
    outputs[name] = {}
    for line in result.stdout.splitlines():
        if line.startswith("RESULT_"):
            label, encoded = line.split("=", 1)
            outputs[name][label] = base64.b64decode(encoded, validate=True)
assert outputs["old"] == outputs["current"], "old/current extra-rule text differs"
assert set(outputs["current"]) == {"RESULT_semantic", "RESULT_lexical"}, "one rule branch did not run"
for value in outputs["current"].values():
    assert value and b"RULE_TEXT" in value, "extra-rule branch did not render content"
print("PASS production extra-rule semantic/preselected and lexical/fallback text byte parity")
if args.emit_json:
    print("EXPORT_JSON=" + json.dumps({side: {key: base64.b64encode(value).decode("ascii") for key, value in rows.items()} for side, rows in outputs.items()}, sort_keys=True))
