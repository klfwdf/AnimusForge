"""Non-diplomacy slice: real 13 typed adapters, not whole TeamModuleServices."""
from pathlib import Path
import argparse
import importlib.util
import hashlib
import contextlib
import io
from unittest.mock import patch

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]

def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def main():
    ports = load("port_suite", HERE / "run.py")
    util = load("port_dotnet", ROOT / "tests/AF.Contracts/ModuleFrameworkApiTests/run.py")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet")
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    dotnet = str(ports.resolve_dotnet(ROOT, args.dotnet))
    out = ports.new_run_root(ROOT, "team-module-adapters", args.run_root)
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    ports.owner_parity("df6ab928")
    ports.owner_signatures()
    # Negative controls change only in-memory source reads, never product files.
    original_read = Path.read_text
    parity_mutants = {
        "drop_scene_eligibility": ("src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs", "TeamModuleServices.Policy.IsEligibleTargetForExternal", "RemovedPolicyEligibility"),
        "duplicate_courier_commit": ("src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs", 'TeamModuleServices.Policy.TryProcessAcceptedAgendaTag(recipient, "courier", session.LetterText, session.ReplyText ?? text, ref text, out string proposalFailure);', 'TeamModuleServices.Policy.TryProcessAcceptedAgendaTag(recipient, "courier", session.LetterText, session.ReplyText ?? text, ref text, out string proposalFailure); TeamModuleServices.Policy.TryProcessAcceptedAgendaTag(recipient, "courier", session.LetterText, session.ReplyText ?? text, ref text, out string proposalFailure);'),
        "swap_courier_text": ("src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DomainCommit.cs", 'session.LetterText, session.ReplyText ?? text, ref text, out string proposalFailure', 'session.ReplyText ?? text, session.LetterText, ref text, out string proposalFailure'),
    }
    for name, (path, before, after) in parity_mutants.items():
        target = ROOT / path
        assert before in original_read(target, encoding="utf-8-sig"), "Parity mutation anchor drift: " + name
        def mutated_read(self, *args, **kwargs):
            value = original_read(self, *args, **kwargs)
            return value.replace(before, after, 1) if self == target else value
        rejected = False
        with patch.object(Path, "read_text", mutated_read), contextlib.redirect_stdout(io.StringIO()):
            try: ports.owner_parity("df6ab928")
            except AssertionError: rejected = True
        assert rejected, "Source parity mutation not rejected: " + name
        print("PASS source parity mutation rejected: " + name)
    sources = ports.PORT_SOURCES + ports.ADAPTER_SOURCES
    def execute(name, override=None):
        paths = [override[1] if override and p == override[0] else ROOT / p for p in sources]
        project = util.project(out / name, "TeamModuleAdapters", paths + [HERE / "OwnerStubs.cs", HERE / "Program.cs"], executable=True)
        code, log = util.run_dotnet(dotnet, ["run", "--project", str(project), "-c", "Release", "-p:DefineConstants=ADAPTER_ONLY"], out)
        (out / name / "run.log").write_text(log, encoding="utf-8")
        return code, log
    code, log = execute("base")
    print(log)
    if code: return code
    mutations = {
        "invert_eligibility": ("=> KingdomAgendaCustomPolicyBehavior.IsEligibleTargetForExternal", "=> !KingdomAgendaCustomPolicyBehavior.IsEligibleTargetForExternal"),
        "invert_siege_selected": ("=> AfGcczShoutBridge.NormalizePostprocessTags(selected, raw, rules);", "=> AfGcczShoutBridge.NormalizePostprocessTags(!selected, raw, rules);"),
        "swap_siege_context_texts": ("ref text, out actionHandled, replyIsDirectPlayerResponse, playerText, speakerReplyText);", "ref text, out actionHandled, replyIsDirectPlayerResponse, speakerReplyText, playerText);")}
    for name, (before, after) in mutations.items():
        matches = [(p, ports.read(p)) for p in ports.ADAPTER_SOURCES if before in ports.read(p)]
        assert len(matches) == 1 and matches[0][1].count(before) == 1, "Mutation anchor drift: " + name
        path, source = matches[0]
        changed = out / (name + ".cs")
        changed.write_text(source.replace(before, after), encoding="utf-8")
        code, log = execute(name, (path, changed))
        assert code != 0 and "FAIL " in log and "error CS" not in log, "Mutation not rejected: " + name + "\n" + log
        print("PASS adapter behavioral mutation rejected: " + name)
    (out / "source-hashes.txt").write_text("\n".join(p + " " + hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in sources), encoding="utf-8")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
