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
orchestrator = extract.declaration(source, "private ShoutPromptContext BuildShoutPromptContextForExternalInternal(")
capture_request = extract.declaration(source, "private PromptBuildRequest CapturePromptBuildRequest(")
capture_sections = extract.declaration(source, "private void CapturePromptSections(")
appendices = extract.declaration(source, "private void ApplyPromptRuntimeAppendices(")
if args.mutate == "routing-before-request":
    a = orchestrator.index("CapturePromptBuildRequest("); b = orchestrator.index("PromptTopicRoutingStage.Run(")
    orchestrator = orchestrator[:a] + orchestrator[b:] + orchestrator[a:b]

def ordered(body, *fragments):
    positions = [body.find(f) for f in fragments]
    assert all(p >= 0 for p in positions), [f for f, p in zip(fragments, positions) if p < 0]
    assert positions == sorted(positions), list(zip(fragments, positions))

ordered(orchestrator,
        "CapturePromptBuildRequest(",
        "AIConfigHandler.BeginGuardrailRuntimeScope()",
        "AIConfigHandler.ApplyGuardrailRuntimeTarget(request.Target)",
        "PromptTopicRoutingStage.Run(",
        "CapturePromptSections(",
        "PromptAssemblyStage.Assemble(",
        "ApplyPromptRuntimeAppendices(",
        "AIConfigHandler.ClearGuardrailRuntimeTarget()")

game_services = re.compile(r"\b(RewardSystemBehavior\.Instance|DuelBehavior\.|TeamModuleServices\.|WorldEntityRetrievalService\.|VoteDealBehavior\.|LordEncounterBehavior\.|MobileParty\.MainParty|Clan\.PlayerClan|Hero\.MainHero|RomanceSystemBehavior\.)")
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
