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
parser.add_argument("--mutate", choices=["assembly-reads-game", "routing-before-request", "drop-worker-eligibility", "drop-knowledge-worker", "drop-extra-worker"])
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
knowledge_capture = extract.declaration(source, "internal void CaptureSharedKnowledgeSnapshot(")
knowledge_worker = extract.declaration(source, "internal void RunSharedKnowledgeRetrieval(")
if args.mutate == "drop-extra-worker":
    knowledge_worker = knowledge_worker.replace("AIConfigHandler.GetMatchedExtraRuleHitsForWorker(", "AIConfigHandler.X(", 1)
complete = extract.declaration(source, "internal ShoutPromptContext CompleteSharedPromptBuild(")
if args.mutate == "routing-before-request":
    orchestrator = orchestrator.replace("BeginSharedPromptBuild(", "X(", 1).replace("RunSharedPromptRouting(phases)", "BeginSharedPromptBuild(", 1).replace("X(", "RunSharedPromptRouting(", 1)
ordered(orchestrator,
        "BeginSharedPromptBuild(",
        "AIConfigHandler.BeginGuardrailRuntimeScope()",
        "AIConfigHandler.ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility)",
        "RunSharedPromptRouting(phases)",
        "CaptureSharedKnowledgeSnapshot(phases)",
        "RunSharedKnowledgeRetrieval(phases)",
        "CompleteSharedPromptBuild(phases",
        "AIConfigHandler.ClearGuardrailRuntimeTarget()")
ordered(begin, "CapturePromptBuildRequest(", "PromptExclusionSets.AddUnavailableConfiguredRules(", "BuildPreprocessExcludedRuleBlockForExternal(")
ordered(routing, "SetGuardrailSemanticContext(", "PromptTopicRoutingStage.Run(", "GetAuxiliaryMentionedEntitiesForExternal(")
ordered(complete, "CapturePromptSections(", "PromptAssemblyStage.Assemble(", "ApplyPromptRuntimeAppendices(")
assert not game_services.search(routing), "routing step must not read game services: " + (game_services.search(routing).group(0) if game_services.search(routing) else "")
assert "PreparePromptLoreRetrieval(" in knowledge_capture and "CollectPromptLoreCandidates(" in knowledge_worker
assert "PreparePromptLoreRetrieval(" not in knowledge_worker and not game_services.search(knowledge_worker), "knowledge worker must not prepare indexes or read game services"
assert "GetMatchedExtraRuleHitsForWorker(" in knowledge_worker and "phases.Routing?.AuxiliaryRuleHitIds == null" in knowledge_worker, "legacy no-preselection rule retrieval must run on worker"

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
if args.mutate == "drop-worker-eligibility":
    native_schedule = native_schedule.replace("ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility)", "ApplyGuardrailRuntimeTarget(phases.Request.Target)")
if args.mutate == "drop-knowledge-worker":
    native_schedule = native_schedule.replace("owner.RunSharedKnowledgeRetrieval(phases);", "", 1)
assert "ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility)" in native_schedule, "Native worker must publish detached eligibility"
assert "ApplyGuardrailRuntimeTarget(begin.Preprocess.Target, begin.Preprocess.Eligibility)" in courier_schedule and "ApplyGuardrailRuntimeTarget(phases.Request.Target, phases.Request.Eligibility)" in courier_schedule, "Courier workers must publish detached eligibility"
assert native_schedule.index('RunNativeConversationMainThreadFuncAsync("prompt_build_begin"') < native_schedule.index('RunNativeConversationBackgroundPreprocessAsync(') < native_schedule.index('RunNativeConversationMainThreadFuncAsync("prompt_build_complete"'), "Native capture/routing/complete thread order"
assert courier_schedule.index('RunCourierOwnerPhaseAsync(generation, source + "_prompt_begin"') < courier_schedule.index('Task.Run(() =>') < courier_schedule.index('RunCourierOwnerPhaseAsync(generation, source + "_prompt_capture"') < courier_schedule.index('RunCourierOwnerPhaseAsync(generation, source + "_prompt_complete"'), "Courier owner/worker thread order"
assert native_schedule.index('"prompt_build_knowledge_capture"') < native_schedule.index('owner.RunSharedKnowledgeRetrieval(phases);') < native_schedule.index('"prompt_build_complete"'), "Native knowledge capture/worker/complete order"
assert courier_schedule.index('source + "_knowledge_capture"') < courier_schedule.index('owner.RunSharedKnowledgeRetrieval(phases)') < courier_schedule.index('source + "_prompt_complete"'), "Courier knowledge capture/worker/complete order"
assert "GetLoreContextWithCandidates(" in capture_sections and "AIConfigHandler.GetLoreContext(" not in capture_sections, "final section must consume worker Lore candidates"
extra_instructions = extract.declaration(source, "private string BuildExtraRuleInstructions(")
assert "AIConfigHandler.FormatMatchedExtraRuleInstructions(" in extra_instructions and extra_instructions.index("fallbackHits != null") < extra_instructions.index("AIConfigHandler.BuildMatchedExtraRuleInstructions("), "captured fallback hits must format without a second retrieval"
assert "GuardrailStickyTargetKey = AIConfigHandler.CaptureGuardrailStickyTargetKey(runtimeTarget)" in capture_request, "sticky identity must be captured on game thread"
assert "GetMatchedExtraRuleHitsForWorker(" in ai and "capturedStickyTargetKey ?? \"\"" in ai, "worker fallback must not resolve target identity live"
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

print("PASS prompt-build-phases orchestrator=5-phase knowledge=worker captureRequest=game sections=game assembly=pure appendices=game LIVE=NOT_RUN")
