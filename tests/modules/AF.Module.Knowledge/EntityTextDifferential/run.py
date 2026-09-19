"""Execute old/current production entity Hero fact rendering over the same fake game object."""
from __future__ import annotations

import base64
import argparse
import importlib.util
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["drop-hero-main", "drop-hero-post", "drop-capture-fallback"])
args = parser.parse_args()
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
markers = (
    "private static string BuildMainPromptBlock(",
    "private static string BuildPostprocessPromptBlock(",
    "private static string ResolvePlayerDisplayNameForPrompt(",
    "private static string StripEntityIdsFromMainPromptBlock(",
    "private static string FormatPostprocessEntityId(",
    "private static string FormatPostprocessMentionHint<T>(",
    "private static bool IsPostprocessHeroMatchEligible(",
    "private static bool IsPostprocessClanMatchEligible(",
    "private static bool IsPostprocessKingdomMatchEligible(",
    "private static void AppendPlayerPostprocessFacts(",
    "private static void AppendHeroMainFacts(",
    "private static string FormatHeroRelationshipForMainPrompt(",
    "private static string FormatHeroRelationshipToReference(",
    "private static bool TryGetRelationValueForPrompt(",
    "private static bool IsSameHero(",
    "private static void AddKinshipRelationship(",
    "private static void AddPoliticalRelationship(",
    "private static string FormatRelationBand(",
    "private static string FormatHeroKingdom(",
    "private static string FormatHeroTraits(",
    "private static void AddTrait(",
    "private static string FormatHeroRelatives(",
    "private static void AddRelative(",
    "private static void AddHeroCollection(",
    "private static string FormatHeroLocation(",
    "private static string FormatHeroStatus(",
    "private static Kingdom ResolveCurrentActiveRulerKingdom(",
    "private static string FormatHeroOccupation(",
    "private static string SafeName(",
    "private static string SafeTextOrEmpty(",
    "private static string SafeStringId(",
    "private static string FormatAge(",
    "private static string FormatGender(",
    "private static string FormatBool(",
    "private static string FormatScore(",
    "private static void ApplyGlobalInjectionLimit(",
    "private static void AddGlobalLimitItems<T>(",
    "private static List<EntityMatch<T>> ExtractGlobalLimitMatches<T>(",
    "private static List<EntityMatch<T>> ConcatEntityMatchCandidates<T>(",
    "private static List<EntityMatch<T>> MergeEntityMatches<T>(",
    "private static List<EntityMatch<T>> CloneEntityMatches<T>(",
    "private static void PopulateGlobalEntityScopeIds(",
    "private static void PopulateGlobalEntityDistanceMetadata(",
    "private static Kingdom ResolveHeroKingdomForResidentEntity(",
    "private static string NormalizeScopeEntityId(",
    "private static IEnumerable<string> GetSettlementAliases(",
    "private static IEnumerable<string> GetClanAliases(",
    "private static IEnumerable<string> GetKingdomAliases(",
    "private static List<EntityMatch<T>> FindMatches<T>(",
    "private static List<EntityCandidateSnapshot<T>> BuildCandidateSnapshots<T>(",
    "private static string SafeSelectorValue<T>(",
    "private static IEnumerable<string> SafeAliases<T>(",
    "private static IEnumerable<string> GetHeroAliases(",
    "private static IEnumerable<string> NonEmpty(",
    "private static IEnumerable<Hero> GetHeroCandidates(",
    "private static bool IsHardBudgetExceeded(",
    "private static void LogSoftBudgetOnceIfNeeded(",
    "private static void LogWorldEntityBudgetStop(",
    "private static string FormatBudgetForLog(",
    "private static void AddOrUpdateEntityMatch<T>(",
    "private static string MergeEntityMention(",
    "private static void SortEntityMatches<T>(",
    "private static void AddAlias(",
    "private static bool RawTextContainsEntityPhrase(",
    "private static List<Tuple<int, int>> FindRawEntityPhraseSpans(",
    "private static int GetLongestContainedRulerTitleLength(",
    "private static bool IsRulerTitleShadowedByLongerContainedTitle(",
    "private static float CalculateRulerTitleEvidenceScore(",
    "private static float CalculateRulerTitlePhraseEvidenceScore(",
    "private static float CalculateBestRulerTitleAliasScore(",
    "private static bool MentionContainsRulerTitleQualifier(",
)
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
outputs = {}
for name in ("old", "current"):
    source = subprocess.check_output(["git", "show", "77a3d234:WorldEntityRetrievalService.cs"], cwd=ROOT).decode("utf-8-sig") if name == "old" else (ROOT / "WorldEntityRetrievalService.cs").read_text(encoding="utf-8-sig")
    out = ROOT / "artifacts/tests/j06-entity-text-differential" / (args.mutate or "normal") / str(os.getpid()) / name
    out.mkdir(parents=True, exist_ok=True)
    entity = extract.declaration(source, "private sealed class EntityMatch<T>" if name == "old" else "internal sealed class EntityMatch<T>")
    visible = extract.declaration(source, "private sealed class VisiblePartyCandidate" if name == "old" else "internal sealed class VisiblePartyCandidate")
    budget = extract.declaration(source, "private sealed class WorldEntityRetrievalBudget")
    snapshot = extract.declaration(source, "private sealed class EntityCandidateSnapshot<T>")
    ruler = extract.declaration(source, "private sealed class RulerTitleCandidate" if name == "old" else "internal sealed class RulerTitleCandidate")
    ruler_scored = extract.declaration(source, "private sealed class RulerTitleScoredMatch")
    variant_markers = (
        (
            "private static List<EntityMatch<Hero>> FindRulerTitleMatches(",
            "private static List<RulerTitleCandidate> BuildRulerTitleCandidates(IEnumerable<Kingdom> kingdoms)",
            "public static WorldEntityPromptContext BuildPromptContext(MentionedWorldEntities mentions, string playerDisplayName, Hero contextHero, bool includeResidentKingdoms, IEnumerable<string> activeRuleIds, string latestInput, bool includeResidentPlayerEntities = false)",
        )
        if name == "old" else
        (
            "private static List<EntityMatch<DetachedEntityCandidate>> FindRulerTitleMatches(",
            "private static List<RulerTitleCandidate> BuildRulerTitleCandidates(IEnumerable<Kingdom> kingdoms, Dictionary<DetachedEntityCandidate, Hero> liveHeroes)",
            "internal static WorldEntityPromptContext BuildPromptContext(MentionedWorldEntities mentions, string playerDisplayName, Hero contextHero, bool includeResidentKingdoms, IEnumerable<string> activeRuleIds, string latestInput, bool includeResidentPlayerEntities, EntityCapture capture, DetachedEntityMatches detachedMatches)",
            "internal static EntityCapture CaptureEntityCandidates(",
            "private static void CaptureCandidates<T>(",
            "private static void CaptureDetachedMetadata(",
            "internal static DetachedEntityMatches MatchDetachedCandidates(",
            "private static void ApplyDetachedGlobalInjectionLimit(",
            "private static List<EntityMatch<DetachedEntityCandidate>> FindDetachedMatches(",
            "private static List<T> RestoreCandidates<T>(",
            "private static List<EntityMatch<T>> RestoreMatches<T>(",
        )
    )
    methods = "\n".join(extract.declaration(source, marker) for marker in markers + variant_markers)
    if name == "current" and args.mutate == "drop-hero-main":
        needle = 'sb.AppendLine("【人物】");'
        assert methods.count(needle) == 2
        before, after = methods.rsplit(needle, 1)
        methods = before + after
    if name == "current" and args.mutate == "drop-hero-post":
        needle = 'sb.AppendLine("【人物】");'
        assert methods.count(needle) == 2
        methods = methods.replace(needle, '', 1)
    if name == "current" and args.mutate == "drop-capture-fallback":
        needle = "if (capture == null) detachedMatches = null;"
        assert methods.count(needle) == 1
        methods = methods.replace(needle, "if (capture == null) return new WorldEntityPromptContext();", 1)
    detached = extract.declaration(source, "internal sealed class DetachedEntityCandidate\n") if name == "current" else ""
    detached_classes = "\n".join(extract.declaration(source, marker) for marker in (
        "internal sealed class DetachedEntityCandidates\n", "internal sealed class EntityCapture\n", "internal sealed class DetachedEntityMatches\n", "private sealed class RawRulerTitleMatchResult\n"
    )) if name == "current" else ""
    mentions = extract.declaration(source, "public sealed class MentionedWorldEntities")
    context = extract.declaration(source, "public sealed class WorldEntityPromptContext")
    raw_result = extract.declaration(source, "private sealed class RawRulerTitleMatchResult\n") if name == "old" else ""
    (out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\nusing System.Globalization;\nusing System.Linq;\nusing System.Text;\nusing System.Text.RegularExpressions;\nnamespace AnimusForge {\n" + mentions + "\n" + context + "\npublic static partial class WorldEntityRetrievalService {\n" + entity + "\n" + visible + "\n" + budget + "\n" + snapshot + "\n" + ruler + "\n" + ruler_scored + "\n" + raw_result + "\n" + detached + "\n" + detached_classes + "\n" + methods + "\n}}\n", encoding="utf-8")
    for filename, relative in (("Matcher.cs", "src/modules/AF.Module.Knowledge/Entities/EntityNameMatcher.cs"), ("Mentions.cs", "src/modules/AF.Module.Knowledge/Entities/EntityMentionList.cs"), ("Allocator.cs", "src/modules/AF.Module.Knowledge/Entities/EntityInjectionAllocator.cs")):
        content = subprocess.check_output(["git", "show", "77a3d234:" + relative], cwd=ROOT).decode("utf-8-sig") if name == "old" else (ROOT / relative).read_text(encoding="utf-8-sig")
        (out / filename).write_text(content, encoding="utf-8")
    (out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
    (out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit>' + ('<DefineConstants>CURRENT</DefineConstants>' if name == "current" else '') + '</PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/><Compile Include="Matcher.cs"/><Compile Include="Mentions.cs"/><Compile Include="Allocator.cs"/></ItemGroup></Project>', encoding="utf-8")
    (out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    result = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        print(name, result.stdout, result.stderr, sep="\n")
        raise SystemExit(result.returncode)
    outputs[name] = {line.split("=", 1)[0]: base64.b64decode(line.split("=", 1)[1], validate=True) for line in result.stdout.splitlines() if line.startswith("RESULT_")}
assert outputs["old"] == outputs["current"] and set(outputs["old"]) == {"RESULT_direct_main", "RESULT_direct_post", "RESULT_direct_meta", "RESULT_title_main", "RESULT_title_post", "RESULT_title_meta"}, "entity Hero facts differ"
print("PASS production Hero direct/title capture, matching, fallback and full entity context old/current byte parity")
