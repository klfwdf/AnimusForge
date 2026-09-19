"""Source-wiring contract for the shared main-chain prompt build phases (J04).

Checks the real MyBehavior source: the orchestrator delegates to the five phases in order and
the pure stages receive detached inputs; the game-reading phases are the only places that
touch Reward/Duel/Team/Entity services. Not a runtime test; pair with the Composition contract.
"""
from __future__ import annotations
import argparse, importlib.util, re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec); spec.loader.exec_module(extract)
parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["assembly-reads-game", "routing-before-request"])
args = parser.parse_args()

source = (ROOT / "MyBehavior.cs").read_text(encoding="utf-8-sig")
game_services = re.compile(r"\b(RewardSystemBehavior\.Instance|DuelBehavior\.|TeamModuleServices\.|WorldEntityRetrievalService\.|VoteDealBehavior\.|LordEncounterBehavior\.|MobileParty\.MainParty|Clan\.PlayerClan|Hero\.MainHero|RomanceSystemBehavior\.)")
orchestrator = extract.declaration(source, "private ShoutPromptContext BuildShoutPromptContextForExternalInternal(")
capture_request = extract.declaration(source, "private PromptBuildRequest CapturePromptBuildRequest(")
capture_sections = extract.declaration(source, "private void CapturePromptSections(")
appendices = extract.declaration(source, "private void ApplyPromptRuntimeAppendices(")

def ordered(body, *fragments):
    positions = [body.find(f) for f in fragments]
    assert all(p >= 0 for p in positions), [f for f, p in zip(fragments, positions) if p < 0]
    assert positions == sorted(positions), list(zip(fragments, positions))

begin = extract.declaration(source, "internal PromptBuildPhases BeginSharedPromptBuild(")
routing = extract.declaration(source, "internal void RunSharedPromptRouting(")
complete = extract.declaration(source, "internal ShoutPromptContext CompleteSharedPromptBuild(")
if args.mutate == "routing-before-request":
    orchestrator = orchestrator.replace("BeginSharedPromptBuild(", "X(", 1).replace("RunSharedPromptRouting(phases)", "BeginSharedPromptBuild(", 1).replace("X(", "RunSharedPromptRouting(", 1)
ordered(orchestrator,
        "BeginSharedPromptBuild(",
        "AIConfigHandler.BeginGuardrailRuntimeScope()",
        "AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility)",
        "RunSharedPromptRouting(phases)",
        "CompleteSharedPromptBuild(phases",
        "AIConfigHandler.ClearGuardrailRuntimeTarget()")
ordered(begin, "CapturePromptBuildRequest(", "PromptExclusionSets.AddUnavailableConfiguredRules(", "BuildPreprocessExcludedRuleBlockForExternal(")
ordered(routing, "SetGuardrailSemanticContext(", "PromptTopicRoutingStage.Run(", "GetAuxiliaryMentionedEntitiesForExternal(")
ordered(complete, "CapturePromptSections(", "PromptAssemblyStage.Assemble(", "ApplyPromptRuntimeAppendices(")
assert not game_services.search(routing), "routing step must not read game services: " + (game_services.search(routing).group(0) if game_services.search(routing) else "")

# J06d: only game-thread capture may call the live eligibility functions. Both
# worker entries must publish their request's detached facts before retrieval.
ai = (ROOT / "AIConfigHandler.cs").read_text(encoding="utf-8-sig")
capture_eligibility = extract.declaration(ai, "internal static PromptRuleEligibility CapturePromptRuleEligibility(")
rag_gate = extract.declaration(ai, "private static bool IsRuleCurrentlyEligibleForRag(")
preprocess_gate = extract.declaration(ai, "public static bool CanInjectRuleTopicIntoPreprocessForExternal(")
for target_field in ("Kingdom", "Hero", "Character", "Troop", "UnnamedRank", "AgentIndex"):
    setter = extract.declaration(ai, "public static void SetGuardrailRuntimeTarget" + target_field + "(")
    assert "ClearCapturedEligibilityOnTargetMutation();" in setter, "setter-only consumers must not inherit stale captured eligibility: " + target_field
apply_target = extract.declaration(ai, "internal static void ApplyGuardrailRuntimeTarget(PromptRuntimeTargetBinding binding, PromptRuleEligibility eligibility)")
assert apply_target.index("binding.Apply(") < apply_target.index("_guardrailRuntimeEligibility.Value = eligibility"), "binding must invalidate old facts before publishing its new facts"
assert "ApplyGuardrailRuntimeTarget(binding);" in capture_eligibility and "BeginGuardrailRuntimeScope()" in capture_eligibility, "capture must bind and restore the request target before ambient lords-hall read"
assert "CanInjectVassalageRuleForPromptCapture(hero, targetCharacter)" in capture_eligibility and "CanInjectVassalageRuleForExternal(hero, targetCharacter)" not in capture_eligibility, "eager capture must not emit per-topic vassalage diagnostics"
vassalage = (ROOT / "VassalageBehavior.cs").read_text(encoding="utf-8-sig")
silent_vassalage = extract.declaration(vassalage, "internal static bool CanInjectVassalageRuleForPromptCapture(")
assert "TryBuildVassalageRuntimeState(" in silent_vassalage and "VassalageDiagnosticLog.Event(" not in silent_vassalage, "capture predicate must be read-only"
assert "captured.IsRuleEligibleForRag(text)" in rag_gate and rag_gate.index("captured.IsRuleEligibleForRag(text)") < rag_gate.index("ShouldExcludeRuntimeRuleForConversationTarget(text)"), "RAG must consult captured facts before live Hero/Mission fallback"
assert "captured.CanInjectRuleTopicIntoPreprocess(text)" in preprocess_gate and preprocess_gate.index("captured.CanInjectRuleTopicIntoPreprocess(text)") < preprocess_gate.index("VassalageBehavior.CanInjectVassalageRuleForExternal("), "preprocess must consult captured facts before live module fallback"
assert "Eligibility = CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTarget)" in capture_request, "shared request must capture eligibility on game thread"
courier_begin = extract.declaration(source, "internal CourierPreprocessRequest BeginCourierRulePreprocess(")
assert "Eligibility = CapturePromptRuleEligibility(targetHero, targetCharacter, runtimeTarget)" in courier_begin, "courier request must capture eligibility on game thread"
native_schedule = (ROOT / "ShoutBehavior.NativePromptBuild.cs").read_text(encoding="utf-8-sig")
courier_schedule = (ROOT / "CourierDeliveryBehavior.PromptSchedule.cs").read_text(encoding="utf-8-sig")
assert "ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility)" in native_schedule, "Native worker must publish detached eligibility"
assert "ApplyGuardrailRuntimeTarget(begin.Preprocess.Target, begin.Preprocess.Eligibility)" in courier_schedule and "ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility)" in courier_schedule, "Courier workers must publish detached eligibility"
for label, body in (("Native", native_schedule), ("Courier", courier_schedule)):
    for live_call in ("Hero.Find(", "Mission.Current", "CanInjectVassalageRuleForExternal(", "CanInjectDiplomacyRuleForExternal(", "CanDiscussWorldDiplomacyForExternal("):
        assert live_call not in body, label + " worker schedule must not resolve live eligibility: " + live_call

assert not game_services.search(orchestrator), "orchestrator must not read game services directly: " + game_services.search(orchestrator).group(0)
assert game_services.search(capture_sections), "section capture is the game-reading phase"
for name, body in (("CapturePromptBuildRequest", capture_request), ("CapturePromptSections", capture_sections), ("ApplyPromptRuntimeAppendices", appendices)):
    assert "GetGuardrailSemanticRuleHitsForPreprocess" not in body and "IsGuardrailSemanticHit(" not in body, name + " must not run rule retrieval"

composition = "".join(p.read_text(encoding="utf-8-sig") for p in sorted((ROOT / "src/modules/AF.Module.Prompt/Composition").glob("*.cs")))
assert "PromptAssemblyStage" in composition and "PromptTopicRoutingStage" in composition, "expected Composition owners present"
if args.mutate == "assembly-reads-game":
    composition += "\nusing TaleWorlds.CampaignSystem;\n"
forbidden = re.compile(r"\busing TaleWorlds\.|\bHero\b\s+\w+\s*[=;,)]|\bCampaign\.Current\b|\bMission\.Current\b|\bAIConfigHandler\.|\bLogger\.Log\(")
m = forbidden.search(composition)
assert not m, "Composition owners must stay detached from game/config/log: " + m.group(0)

print("PASS prompt-build-phases orchestrator=5-phase captureRequest=game sections=game assembly=pure appendices=game LIVE=NOT_RUN")
