"""Prove first-slice port delegation and exact owner receiver-only replacement."""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
import re
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet
HERE = Path(__file__).resolve().parent
PORT_SOURCES = [
    "src/AF.Contracts/Internal/TeamModules/IPolicyModulePort.cs",
    "src/AF.Contracts/Internal/TeamModules/IGatheringModulePort.cs",
    "src/AF.Contracts/Internal/TeamModules/ISiegeModulePort.cs",
]
ADAPTER_SOURCES = [
    "src/bridges/Policy/PolicyModuleAdapter.cs",
    "src/bridges/Gathering/GatheringModuleAdapter.cs",
    "src/bridges/Siege/SiegeModuleAdapter.cs",
]
SERVICE_SOURCE = "src/AF.GameAdapter.Bannerlord/Composition/TeamModuleServices.cs"
SOURCES = PORT_SOURCES + ADAPTER_SOURCES + [SERVICE_SOURCE]
MAP = {
    "Policy": ("KingdomAgendaCustomPolicyBehavior", ["IsEligibleTargetForExternal", "BuildRuntimePostprocessRulesForExternal", "TryProcessAcceptedAgendaTag"]),
    "Gathering": ("NobleGatheringBehavior", ["BuildRuntimePostprocessRulesForExternal", "BuildPostprocessContextForExternal", "NormalizeNobleGatheringPostprocessTagsForExternal", "BuildFeastAttendanceContext", "TryApplyNobleGatheringTagsForExternal"]),
    "Siege": ("AfGcczShoutBridge", ["BuildPostprocessRules", "BuildPostprocessContext", "NormalizePostprocessTags", "TryProcessActionTags"])
}


def read(path):
    return (current_source_path(ROOT, path)).read_text(encoding="utf-8-sig")



def restore_reviewed_nonport_deltas(path, current, prior):
    # Preserve the strict original whole-file proof without freezing unrelated Native evolution.
    # Only these hash-frozen, separately behavior-tested declarations can differ; a future edit fails.
    spec = importlib.util.spec_from_file_location("native_delta_extractor", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extractor = importlib.util.module_from_spec(spec); spec.loader.exec_module(extractor)
    # Restore only the separately reviewed B1 migration first. It verifies production and
    # evidence sources and whole-file equivalence; no removed method is silently skipped.
    spec = importlib.util.spec_from_file_location("port_memory_inverse", ROOT / "tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/source_parity.py")
    memory = importlib.util.module_from_spec(spec); spec.loader.exec_module(memory)
    current = memory.restore_memory_summary_source(path, current)
    if path == "ShoutBehavior.cs":
        spec = importlib.util.spec_from_file_location("channel_persona_inverse", ROOT / "tests/modules/AF.Module.Conversation/ChannelPersonaPreparationTests/source_parity.py")
        persona = importlib.util.module_from_spec(spec); spec.loader.exec_module(persona)
        current = persona.restore(path, current)
    if path == "CourierDeliveryBehavior.cs":
        spec = importlib.util.spec_from_file_location("courier_history_inverse", ROOT / "tests/modules/AF.Module.Conversation/CourierHistoryPreparationTests/source_parity.py")
        courier_history = importlib.util.module_from_spec(spec); spec.loader.exec_module(courier_history)
        current = courier_history.restore(current)
    review = json.loads((HERE / "reviewed-native-admission-deltas.json").read_text(encoding="utf-8"))
    for comment in review.get("commentRewrites", []):
        if comment["path"] == path:
            if not all(line.lstrip().startswith("//") for key in ("current", "original") for line in comment[key].splitlines()):
                raise AssertionError("Only explicit line-comment rewrites are allowed")
            if current.count(comment["current"]) != 1 or prior.count(comment["original"]) != 1:
                raise AssertionError("Unreviewed compatibility comment rewrite")
            current = current.replace(comment["current"], comment["original"], 1)
    for item in review["methods"]:
        if item["path"] != path:
            continue
        declaration = extractor.declaration(current, item["signature"])
        if hashlib.sha256(declaration.encode()).hexdigest() != item["sha256"] or "TeamModuleServices." in declaration:
            raise AssertionError("Unreviewed non-port owner delta: " + path + ":" + item["signature"])
        original = extractor.declaration(prior, item.get("baselineSignature", item["signature"]))
        if current.count(declaration) != 1:
            raise AssertionError("Nonunique reviewed declaration")
        current = current.replace(declaration, original, 1)
    return current


def owner_parity(baseline):
    replacements = {f"TeamModuleServices.{port}.{method}": f"{owner}.{method}"
        for port, (owner, methods) in MAP.items() for method in methods}
    replacements["TeamModuleServices.Policy.BuildActivePolicyDialogueContextForExternal"] = "NpcRulerPolicyBehavior.BuildActivePolicyDialogueContextForExternal"
    def extract_calls(source, qualified_name):
        """Extract complete call expressions while respecting nested arguments."""
        calls = []
        cursor = 0
        while True:
            start = source.find(qualified_name, cursor)
            if start < 0:
                return calls
            opening = start + len(qualified_name)
            while opening < len(source) and source[opening].isspace():
                opening += 1
            if opening >= len(source) or source[opening] != "(":
                cursor = start + 1
                continue
            depth = 0
            quote = None
            escaped = False
            line_comment = False
            block_comment = False
            index = opening
            while index < len(source):
                char = source[index]
                following = source[index + 1] if index + 1 < len(source) else ""
                if line_comment:
                    line_comment = char != "\n"
                elif block_comment:
                    if char == "*" and following == "/":
                        block_comment = False
                        index += 1
                elif quote:
                    if escaped:
                        escaped = False
                    elif char == "\\":
                        escaped = True
                    elif char == quote:
                        quote = None
                elif char == "/" and following == "/":
                    line_comment = True
                    index += 1
                elif char == "/" and following == "*":
                    block_comment = True
                    index += 1
                elif char in ('"', "'"):
                    quote = char
                elif char == "(":
                    depth += 1
                elif char == ")":
                    depth -= 1
                    if depth == 0:
                        calls.append(source[start:index + 1])
                        cursor = index + 1
                        break
                index += 1
            else:
                raise AssertionError("Unterminated owner call: " + qualified_name)

    normalize = lambda value: re.sub(r"\s+", "", value)
    # The historical fallback retirement applies only inside the authoritative
    # recipient-delivery commit, not any same-spelled call elsewhere.
    domain = read("src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs")
    spec = importlib.util.spec_from_file_location("port_courier_commit", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extractor = importlib.util.module_from_spec(spec); spec.loader.exec_module(extractor)
    courier_core = extractor.declaration(domain, "private bool CommitGeneratedReplyActionsAtRecipientCore(")
    current_paths = [current_source_path(ROOT, "MyBehavior.cs")]
    current_paths += sorted((ROOT / "src/modules/AF.Module.Conversation/Channels/Scene").glob("*.cs"))
    current_paths += sorted((ROOT / "src/modules/AF.Module.Conversation/Internal/Postprocess").glob("*.cs"))
    current_paths += sorted((ROOT / "src/modules/AF.Module.Conversation/Channels/Courier").glob("*.cs"))
    current = "\n".join(path.read_text(encoding="utf-8-sig") for path in current_paths)
    baseline_paths = ["MyBehavior.cs", "ShoutBehavior.cs", "ShoutBehavior.ScenePostprocess.cs", "CourierDeliveryBehavior.cs"]
    prior = "\n".join(
        subprocess.check_output(["git", "show", f"{baseline}:{path}"], cwd=ROOT)
        .decode("utf-8-sig").replace("\r\n", "\n")
        for path in baseline_paths)

    seen = {name: 0 for name in replacements}
    # J09 renamed the Scene postprocess work-item locals without changing the
    # ordered values passed to the Siege port. ScenePostprocessParityTests owns
    # the complete old/new behavior proof; this single reviewed alias keeps the
    # port test focused on receiver, argument order and call count.
    reviewed_scene_alias = normalize(
        "AfGcczShoutBridge.TryProcessActionTags(speakingHero, npcCharacter, "
        "targetAgentIndex, ref remaining, out bool siegeActionHandled, "
        "replyIsDirectPlayerResponse, replyIsDirectPlayerResponse ? playerText : string.Empty, replyText)")
    reviewed_scene_original = normalize(
        "AfGcczShoutBridge.TryProcessActionTags(speakingHero, npcCharacter, "
        "runtimeTargetAgentIndex, ref text3, out siegeActionHandled, "
        "replyIsDirectPlayerResponse, replyIsDirectPlayerResponse ? playerText : string.Empty, replySnapshot)")
    alias_seen = 0
    for new, old in replacements.items():
        current_calls = []
        for call in extract_calls(current, new):
            restored = normalize(call.replace(new, old, 1))
            if restored == reviewed_scene_alias:
                restored = reviewed_scene_original
                alias_seen += 1
            current_calls.append(restored)
        prior_calls = [normalize(call) for call in extract_calls(prior, old)]
        if new == "TeamModuleServices.Gathering.TryApplyNobleGatheringTagsForExternal":
            courier = normalize(old + '(recipient, ref text, out var nobleFacts, out var nobleNotifications)')
            if prior_calls.count(courier) != 2 or current_calls.count(courier) != 1:
                raise AssertionError("Reviewed Courier gathering single-commit call drifted")
            if len(extract_calls(courier_core, new)) != 1 or courier_core.index("session.PostprocessConsumed = true") > courier_core.index(new):
                raise AssertionError("Courier gathering call left the one-shot domain owner")
            prior_calls.remove(courier)
        if new == "TeamModuleServices.Policy.TryProcessAcceptedAgendaTag":
            # cd0d942d removed the duplicated fallback executor; f6c95ac3 moved
            # the single recipient-delivery commit into its authoritative owner.
            # Do not accept arbitrary call-count changes: only the identical
            # historical Courier expression may collapse from two to one.
            courier = normalize(old + '(recipient, "courier", session.LetterText, session.ReplyText ?? text, ref text, out string proposalFailure)')
            if prior_calls.count(courier) != 2 or current_calls.count(courier) != 1:
                raise AssertionError("Reviewed Courier single-commit call drifted")
            prior_calls.remove(courier)
            core = courier_core
            markers = ["session.PostprocessConsumed", "!session.DeliveryApplied", "session.PostprocessConsumed = true", new]
            indices = [core.index(marker) for marker in markers]
            if indices != sorted(indices) or len(extract_calls(core, new)) != 1:
                raise AssertionError("Courier authority lost delivery/one-shot guards")
            print("PASS reviewed Courier duplicate fallback retirement; recipient delivery and one-shot guards retained")
        if sorted(current_calls) != sorted(prior_calls):
            raise AssertionError("Owner call parity failed: " + new + "\ncurrent=" + repr(current_calls) + "\nbaseline=" + repr(prior_calls))
        if extract_calls(current, old):
            raise AssertionError("Direct gameplay owner call bypasses typed port: " + old)
        seen[new] = len(current_calls)
        print(f"PASS scoped receiver/argument parity equals {baseline}: {new} calls={len(current_calls)}")
    if alias_seen != 1:
        raise AssertionError("Reviewed Scene postprocess alias count drifted")
    if len(seen) != 13 or any(count == 0 for count in seen.values()):
        raise AssertionError("Every declared method must have a live owner call, not only a descriptor")
    sub = read("SubModule.cs")
    # Campaign composition has its own source/behavior suite.  This test owns
    # only the framework lifetime seam and must not be blocked by unrelated
    # GameLifetime fixture hashes.
    init_block = "\t\t// 只装配同 DLL 的内部接缝与只读 API 目录，不切换任何渠道的默认执行路径。\n\t\tModuleFrameworkRuntime.Initialize(out string moduleFrameworkReason);\n\t\tLogger.LogTrace(\"SubModule\", \">>> Module framework: \" + moduleFrameworkReason);\n"
    if sub.count(init_block) != 1 or sub.count("\t\tModuleFrameworkRuntime.Shutdown();\n") != 1:
        raise AssertionError("Unexpected lifecycle wiring")
    if sub.count("ModuleFrameworkRuntime.") != 3:
        raise AssertionError("Module framework lifecycle has an unreviewed extra call")
    assert sub.index("FeatureBridgeRuntime.Initialize") < sub.index("ModuleFrameworkRuntime.Initialize") < sub.index("SceneActionsIntegrationBoundary.InitializeRuntime") < sub.index("if (_uiExtenderInitialized)")
    unload = sub[sub.index("protected override void OnSubModuleUnloaded()"):sub.index("protected override void OnBeforeInitialModuleScreenSetAsRoot()")]
    assert unload.index("ModuleFrameworkRuntime.Shutdown") < unload.index("base.OnSubModuleUnloaded")
    campaign_start = sub.index("protected override void InitializeGameStarter(")
    campaign = sub[campaign_start:sub.index("protected override void OnApplicationTick", campaign_start)]
    assert campaign.index("AfCampaignRuntimeLifecycle.Begin") < campaign.index("ModuleFrameworkRuntime.RegisterCampaign") < campaign.index("AfCampaignRuntimeLifecycle.CaptureOwners")
    print("PASS scoped module framework initialize/shutdown lifecycle and ordering")
    print(f"PASS 13 routed method names / {sum(seen.values())} live call sites")
    return seen



def owner_signatures():
    stubs = (HERE / "OwnerStubs.cs").read_text(encoding="utf-8")
    declarations = []
    owners = [
        ("KingdomAgendaCustomPolicyBehavior", "PolicySystem/Agenda/KingdomAgendaCustomPolicyBehavior.cs", MAP["Policy"][1]),
        ("NpcRulerPolicyBehavior", "PolicySystem/Npc/NpcRulerPolicyBehavior.cs", ["BuildActivePolicyDialogueContextForExternal"]),
        ("NobleGatheringBehavior", "NobleGatheringBehavior.cs", MAP["Gathering"][1]),
        ("AfGcczShoutBridge", "AfGcczShoutBridge.cs", MAP["Siege"][1])]
    for owner, path, methods in owners:
        owner_source = read(path)
        begin = stubs.index("internal static class " + owner)
        end = stubs.find("internal static class ", begin + 1)
        stub = stubs[begin:] if end < 0 else stubs[begin:end]
        for name in methods:
            pattern = r"(?:public|internal)\s+static\s+([\w<>]+)\s+" + name + r"\s*\(([^)]*)\)\s*(?:=>|\{)"
            live = re.search(pattern, owner_source)
            fake = re.search(pattern, stub)
            if not live or not fake:
                raise AssertionError("Missing static owner signature: " + owner + "." + name)
            normalize = lambda match: re.sub(r"\s+", "", match.group(1) + "(" + match.group(2) + ")")
            if normalize(live) != normalize(fake):
                raise AssertionError("Stub drift from actual owner signature: " + owner + "." + name)
            declarations.append({"owner": path, "method": name,
                "signature": re.sub(r"\s+", " ", live.group(1) + " " + name + "(" + live.group(2) + ")").strip()})
    print("PASS 13 recording-stub signatures match actual gameplay owner declarations")
    return declarations


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--dotnet")
    p.add_argument("--run-root", type=Path)
    p.add_argument("--baseline", default="df6ab928")
    p.add_argument("--skip-mutations", action="store_true")
    args = p.parse_args()
    args.dotnet = str(resolve_dotnet(ROOT, args.dotnet))
    seen = owner_parity(args.baseline)
    signatures = owner_signatures()
    spec = importlib.util.spec_from_file_location("framework_test_util", ROOT / "tests/AF.Contracts/ModuleFrameworkApiTests/run.py")
    util = importlib.util.module_from_spec(spec); spec.loader.exec_module(util)
    out = new_run_root(ROOT, "team-module-port-parity", args.run_root)
    (out / "owner-signatures.json").write_text(json.dumps(signatures, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    target = util.project(out / "Base", "TeamModulePortParity", [current_source_path(ROOT, p) for p in SOURCES] + [HERE / "OwnerStubs.cs", HERE / "Program.cs"], executable=True)
    code, log = util.run_dotnet(args.dotnet, ["run", "--project", str(target), "-c", "Release"], out)
    print(log, end="")
    if code:
        (out/"run.log").write_text(log, encoding="utf-8"); return code
    mutation_log = []
    if not args.skip_mutations:
        mutations = {
            "invert_eligibility": ("=> KingdomAgendaCustomPolicyBehavior.IsEligibleTargetForExternal", "=> !KingdomAgendaCustomPolicyBehavior.IsEligibleTargetForExternal"),
            "invert_siege_selected": ("=> AfGcczShoutBridge.NormalizePostprocessTags(selected, raw, rules);", "=> AfGcczShoutBridge.NormalizePostprocessTags(!selected, raw, rules);"),
            "swap_siege_context_texts": ("ref text, out actionHandled, replyIsDirectPlayerResponse, playerText, speakerReplyText);", "ref text, out actionHandled, replyIsDirectPlayerResponse, speakerReplyText, playerText);")}
        for name, (old, new) in mutations.items():
            matches = [(path, read(path)) for path in ADAPTER_SOURCES if old in read(path)]
            if len(matches) != 1 or matches[0][1].count(old) != 1:
                raise AssertionError("Mutation anchor drift: " + name)
            adapter_path, adapters = matches[0]
            folder = out/name; folder.mkdir(exist_ok=True)
            mutated = folder/Path(adapter_path).name
            mutated.write_text(adapters.replace(old, new), encoding="utf-8")
            sources = [mutated if path == adapter_path else current_source_path(ROOT, path) for path in SOURCES]
            project = util.project(folder, "TeamModulePortParity", sources + [HERE/"OwnerStubs.cs", HERE/"Program.cs"], executable=True)
            result, text = util.run_dotnet(args.dotnet, ["run", "--project", str(project), "-c", "Release"], out)
            (folder/"run.log").write_text(text, encoding="utf-8")
            if result == 0 or "FAIL " not in text or "error CS" in text:
                print(text); raise AssertionError("Mutation not rejected by runtime assertions: " + name)
            mutation_log.append("PASS behavioral mutation rejected: " + name)
            print(mutation_log[-1])
    fingerprint = "\n".join(f"{p} SHA256={hashlib.sha256((current_source_path(ROOT, p)).read_bytes()).hexdigest()}" for p in SOURCES)
    calls = "\n".join(f"{name} calls={count}" for name,count in sorted(seen.items()))
    (out/"run.log").write_text(fingerprint+"\n"+calls+"\n"+log+"\n"+"\n".join(mutation_log)+"\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
