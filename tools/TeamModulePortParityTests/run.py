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

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
SOURCES = ["Refactor/Modules/TeamModulePorts.cs", "Refactor/Modules/TeamModuleAdapters.cs", "Refactor/Modules/TeamModuleServices.cs"]
MAP = {
    "Policy": ("KingdomAgendaCustomPolicyBehavior", ["IsEligibleTargetForExternal", "BuildRuntimePostprocessRulesForExternal", "TryProcessAcceptedAgendaTag"]),
    "Gathering": ("NobleGatheringBehavior", ["BuildRuntimePostprocessRulesForExternal", "BuildPostprocessContextForExternal", "NormalizeNobleGatheringPostprocessTagsForExternal", "BuildFeastAttendanceContext", "TryApplyNobleGatheringTagsForExternal"]),
    "Siege": ("AfGcczShoutBridge", ["BuildPostprocessRules", "BuildPostprocessContext", "NormalizePostprocessTags", "TryProcessActionTags"])
}


def read(path):
    return (ROOT/path).read_text(encoding="utf-8-sig")



def restore_reviewed_nonport_deltas(path, current, prior):
    # Preserve the strict original whole-file proof without freezing unrelated Native evolution.
    # Only these hash-frozen, separately behavior-tested declarations can differ; a future edit fails.
    spec = importlib.util.spec_from_file_location("native_delta_extractor", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
    extractor = importlib.util.module_from_spec(spec); spec.loader.exec_module(extractor)
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
        original = extractor.declaration(prior, item["signature"])
        if current.count(declaration) != 1:
            raise AssertionError("Nonunique reviewed declaration")
        current = current.replace(declaration, original, 1)
    return current


def owner_parity(baseline):
    replacements = {f"TeamModuleServices.{port}.{method}": f"{owner}.{method}"
        for port, (owner, methods) in MAP.items() for method in methods}
    replacements["TeamModuleServices.Policy.BuildActivePolicyDialogueContextForExternal"] = "NpcRulerPolicyBehavior.BuildActivePolicyDialogueContextForExternal"
    seen = {name: 0 for name in replacements}
    for path in ["MyBehavior.cs", "ShoutBehavior.cs", "ShoutBehavior.ScenePostprocess.cs", "CourierDeliveryBehavior.cs"]:
        current = read(path)
        prior = subprocess.check_output(["git", "show", f"{baseline}:{path}"], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
        restored = restore_reviewed_nonport_deltas(path, current, prior).replace("using AnimusForge.Refactor.Modules;\n", "")
        for new, old in replacements.items():
            seen[new] += restored.count(new)
            restored = restored.replace(new, old)
        prior = subprocess.check_output(["git", "show", f"{baseline}:{path}"], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
        if restored != prior:
            raise AssertionError(f"Owner parity failed: {path} differs beyond declared receiver/using changes")
        print(f"PASS full-file reviewed-native/receiver inverse equals {baseline}: {path}")
    if len(seen) != 13 or any(count == 0 for count in seen.values()):
        raise AssertionError("Every declared method must have a live owner call, not only a descriptor")
    sub = read("SubModule.cs")
    init_block = "\t\t// 只装配同 DLL 的内部接缝与只读 API 目录，不切换任何渠道的默认执行路径。\n\t\tModuleFrameworkRuntime.Initialize(out string moduleFrameworkReason);\n\t\tLogger.LogTrace(\"SubModule\", \">>> Module framework: \" + moduleFrameworkReason);\n"
    if sub.count(init_block) != 1 or sub.count("\t\tModuleFrameworkRuntime.Shutdown();\n") != 1:
        raise AssertionError("Unexpected lifecycle wiring")
    restored = sub.replace("using AnimusForge.Refactor.Modules;\n", "").replace(init_block, "").replace("\t\tModuleFrameworkRuntime.Shutdown();\n", "")
    prior = subprocess.check_output(["git", "show", f"{baseline}:SubModule.cs"], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
    if restored != prior:
        raise AssertionError("SubModule differs beyond the reviewed load/unload hooks")
    assert sub.index("FeatureBridgeRuntime.Initialize") < sub.index("ModuleFrameworkRuntime.Initialize") < sub.index("SceneActionsIntegrationBoundary.InitializeRuntime") < sub.index("if (_uiExtenderInitialized)")
    unload = sub[sub.index("protected override void OnSubModuleUnloaded()"):sub.index("protected override void OnBeforeInitialModuleScreenSetAsRoot()")]
    assert unload.index("ModuleFrameworkRuntime.Shutdown") < unload.index("base.OnSubModuleUnloaded")
    print(f"PASS full-file lifecycle inverse equals {baseline}: SubModule.cs")
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
    p.add_argument("--dotnet", default=r"G:\AFMOD\.dotnet-sdk\dotnet.exe")
    p.add_argument("--baseline", default="df6ab928")
    p.add_argument("--skip-mutations", action="store_true")
    args = p.parse_args()
    seen = owner_parity(args.baseline)
    signatures = owner_signatures()
    spec = importlib.util.spec_from_file_location("framework_test_util", ROOT / "tools/ModuleFrameworkApiTests/run.py")
    util = importlib.util.module_from_spec(spec); spec.loader.exec_module(util)
    out = HERE / ".generated/current"; out.mkdir(parents=True, exist_ok=True)
    (out / "owner-signatures.json").write_text(json.dumps(signatures, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    target = util.project(out / "Base", "TeamModulePortParity", [ROOT/p for p in SOURCES] + [HERE / "OwnerStubs.cs", HERE / "Program.cs"], executable=True)
    code, log = util.run_dotnet(args.dotnet, ["run", "--project", str(target), "-c", "Release"], out)
    print(log, end="")
    if code:
        (out/"run.log").write_text(log, encoding="utf-8"); return code
    mutation_log = []
    if not args.skip_mutations:
        adapters = read("Refactor/Modules/TeamModuleAdapters.cs")
        mutations = {
            "invert_eligibility": ("=> KingdomAgendaCustomPolicyBehavior.IsEligibleTargetForExternal", "=> !KingdomAgendaCustomPolicyBehavior.IsEligibleTargetForExternal"),
            "invert_siege_selected": ("=> AfGcczShoutBridge.NormalizePostprocessTags(selected, raw, rules);", "=> AfGcczShoutBridge.NormalizePostprocessTags(!selected, raw, rules);"),
            "swap_siege_context_texts": ("ref text, out actionHandled, replyIsDirectPlayerResponse, playerText, speakerReplyText);", "ref text, out actionHandled, replyIsDirectPlayerResponse, speakerReplyText, playerText);")}
        for name, (old, new) in mutations.items():
            if adapters.count(old) != 1: raise AssertionError("Mutation anchor drift: " + name)
            folder = out/name; folder.mkdir(exist_ok=True)
            mutated = folder/"Adapters.cs"; mutated.write_text(adapters.replace(old, new), encoding="utf-8")
            project = util.project(folder, "TeamModulePortParity", [ROOT/SOURCES[0], mutated, ROOT/SOURCES[2], HERE/"OwnerStubs.cs", HERE/"Program.cs"], executable=True)
            result, text = util.run_dotnet(args.dotnet, ["run", "--project", str(project), "-c", "Release"], out)
            (folder/"run.log").write_text(text, encoding="utf-8")
            if result == 0 or "FAIL " not in text or "error CS" in text:
                print(text); raise AssertionError("Mutation not rejected by runtime assertions: " + name)
            mutation_log.append("PASS behavioral mutation rejected: " + name)
            print(mutation_log[-1])
    fingerprint = "\n".join(f"{p} SHA256={hashlib.sha256((ROOT/p).read_bytes()).hexdigest()}" for p in SOURCES)
    calls = "\n".join(f"{name} calls={count}" for name,count in sorted(seen.items()))
    (out/"run.log").write_text(fingerprint+"\n"+calls+"\n"+log+"\n"+"\n".join(mutation_log)+"\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
