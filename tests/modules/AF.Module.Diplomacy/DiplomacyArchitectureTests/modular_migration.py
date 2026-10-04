"""Negative ownership checks for the concurrent/oral migration; no game required."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[4]
DPL = ROOT / "src/modules/AF.Module.Diplomacy"
checks = 0
def check(value, message):
    global checks
    checks += 1
    assert value, message
def read(path):
    return (DPL / path).read_text(encoding="utf-8-sig")

for name in ["WorldDiplomacyBehavior.cs", "WorldDiplomacyBehavior.Dialogue.cs", "WorldDiplomacyBehavior.Scheduling.cs",
             "DiplomacyBehavior.cs", "WorldDiplomacyLlmClient.cs", "DiplomacyDialogue", "DiplomacyRounds"]:
    path = ROOT / name
    check(not path.exists() or (path.is_dir() and not any(path.rglob("*.cs"))), "active predecessor remains: " + name)
for path in [*DPL.glob("Application/WorldDiplomacyOrchestration.*.cs"),
             *DPL.glob("Domain/Dialogue/*.cs"), *DPL.glob("Domain/Rounds/*.cs")]:
    source = path.read_text(encoding="utf-8-sig")
    check(not re.search(r"^using (?:TaleWorlds|SandBox|HarmonyLib)", source, re.M), "live engine dependency: " + path.name)
    check("partial class WorldDiplomacyBehavior" not in source, "business still owned by predecessor: " + path.name)
world = read("World/WorldDiplomacyBehavior.cs")
adapter = read("World/WorldDiplomacyBehavior.DialogueAdapter.cs")
oral = read("Application/WorldDiplomacyOrchestration.Dialogue.cs")
dispatch = read("Application/WorldDiplomacyLlmDispatchApplication.cs")
request = read("World/WorldDiplomacyBehavior.LlmDispatchSource.cs")
client = read("World/WorldDiplomacyLlmClient.cs")
check("_orchestration.CurrentStorage" in world and "_llmBudget" not in world, "host owns a second store or bank")
check("_orchestration.SubmitOralDiplomaticCommitment" in adapter, "oral adapter does not forward")
check("ProcessAnalyzedDocument(document" in oral and "GenerateAsync" not in oral, "oral publication bypasses executor or rewrites consent")
check(dispatch.index("prepareShared?.Invoke(job)") > dispatch.index("if (!EnsureCurrentCanonicalPromptContractBeforeSend("), "shared tail frozen before refresh")
check("scheduler.RequestLeases.TryClaim(job" in dispatch and "source.StartRequest(request, requestMessages)" in dispatch, "real transport bypasses application lease")
check("budget.TryAdmit(player)" in request and "cancellation.IsCancellationRequested" in request, "actual HTTP bank/cancellation bypassed")
check(client.index("if (admitRequest != null && !admitRequest())") < client.index("return DuelSettings.GlobalClient.SendAsync(request, token)"), "send admission comes after HTTP")
all_source = "\n".join(p.read_text(encoding="utf-8-sig") for p in DPL.rglob("*.cs"))
check(len(re.findall(r"public sealed class WorldDiplomacyStorage\b", all_source)) == 1, "duplicate serialized state owner")
save_keys = re.findall(r'"_af_world_diplomacy_v1"', all_source)
check(len(save_keys) == 1, "save key changed or duplicated")
for name in ["WorldDiplomacyBehavior.Dialogue.cs", "WorldDiplomacyBehavior.Scheduling.cs", "WorldDiplomacyBehavior.Treaties.cs",
             "WorldDiplomacyBehavior.PersonalMemory.cs", "WorldDiplomacyBehavior.DialogueIndex.cs"]:
    check(not (DPL / "World" / name).exists(), "copied predecessor algorithm: " + name)
prompt = read("Application/DiplomacyPromptApplication.cs")
postprocess = read("Application/DiplomacyPostprocessContextApplication.cs")
check("oral.OralArrangementContext()" in prompt, "main conversation lost oral source/version context")
check("oral.OralArrangementContext()" in postprocess, "postprocessor lost oral source/version context")
check("DIPLOMACY:COMMIT:action=" in postprocess and "DIPLOMACY:COMMITMENT:arrangement=" in postprocess,
      "oral consent/control tags not injected into real postprocessing")
check("DIPLOMACY:MAKE_TRADE:" not in postprocess, "old immediate bilateral-treaty prompt remains")
check("action.TreatyTerms = document.TreatyTerms" in read("Application/WorldDiplomacyDocumentApplication.cs"),
      "action settlement loses accepted treaty terms")
memory_bridge = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.MemoryRecovery.cs").read_text(encoding="utf-8-sig")
check("commit.CapturedGameDate" in memory_bridge and "?commit.CapturedGameDate:ResolveInteractionMemoryOriginGameDate" in memory_bridge,
      "real memory adapter discards the captured oral diplomacy date")
print(f"Modular migration ownership checks passed: {checks}")
