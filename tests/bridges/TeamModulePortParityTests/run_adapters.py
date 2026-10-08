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
    parser.add_argument("--current-adapters-only", action="store_true", help="Run actual current typed adapters, excluding the separately retained historical whole-source inverse oracle")
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    dotnet = str(ports.resolve_dotnet(ROOT, args.dotnet))
    out = ports.new_run_root(ROOT, "team-module-adapters", args.run_root)
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    if not args.current_adapters_only:
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
    extractor=load("civil_war_extract",ROOT/"tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    current_hook=(ROOT/"src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CivilWarPolitics.cs").read_text(encoding="utf-8-sig")
    hook=extractor.declaration(current_hook,"internal static void RecordCivilWarPoliticalResult(")
    delegate_source=(ROOT/"src/AF.GameAdapter.Bannerlord/Memory/ExecutionWitnessObservationController.cs").read_text(encoding="utf-8-sig")
    bulletin=next(line for line in delegate_source.splitlines() if line.startswith("internal delegate bool ExecutionObservationBulletin("))
    hook_file=out/"CurrentCivilWarHook.cs"
    political=(ROOT/"src/modules/AF.Module.Kingdom/CivilWar/CivilWarPoliticalRules.cs").read_text(encoding="utf-8-sig")
    priority=next(line for line in political.splitlines() if "internal const int PoliticalResultPriority =" in line)
    kind=extractor.declaration(political,"internal static string PoliticalResultKind(")
    hook_file.write_text("using TaleWorlds.CampaignSystem; namespace AnimusForge {"+bulletin+" internal static class CivilWarPoliticalRules {"+priority+kind+"} public partial class MyBehavior {"+hook+"}}",encoding="utf-8")
    record_adapter=(ROOT/'src/AF.GameAdapter.Bannerlord/Memory/ExternalActionObservationBannerlordAdapter.cs').read_text(encoding='utf-8-sig')
    record_fields=record_adapter[record_adapter.index('    private readonly ExternalActionObservationApplication'):record_adapter.index('    internal ExternalActionObservationBannerlordAdapter(')]
    record_ctor=extractor.declaration(record_adapter,'internal ExternalActionObservationBannerlordAdapter(')
    record_rp=extractor.declaration(record_adapter,'internal void RecordExternalPlayerHighValueRpCraft(')
    my=(ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
    rp_hook=extractor.declaration(my,'public static void RecordPlayerHighValueRpCraftForExternal(')+extractor.declaration(my,'private void RecordExternalPlayerHighValueRpCraft(')
    identities=(ROOT/'src/AF.GameAdapter.Bannerlord/Memory/MemoryEntityIdentityBannerlordAdapter.cs').read_text(encoding='utf-8-sig')
    identity=''.join(extractor.declaration(identities,sig) for sig in ['internal static string GetHeroId(', 'internal static string GetSettlementDisplayName(', 'internal static string GetSettlementId(', 'internal static string GetKingdomId(Kingdom', 'internal static string GetKingdomId(IFaction'])
    policy=(ROOT/'src/AF.GameAdapter.Bannerlord/Records/PoliticalDecisionRecordCaptureAdapter.cs').read_text(encoding='utf-8-sig')
    limit=extractor.declaration(policy,'internal static string LimitCustomPolicyWeeklyMaterialText(')
    rp_file=out/'CurrentRpCraftHook.cs';rp_file.write_text('using System;using System.Globalization;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.CampaignSystem.Settlements;namespace AnimusForge {internal sealed class ExternalActionObservationBannerlordAdapter {'+record_fields+record_ctor+record_rp+'} public partial class MyBehavior {'+rp_hook+'} internal static partial class MemoryEntityIdentityBannerlordAdapter {'+identity+'} internal static class PoliticalDecisionRecordCaptureAdapter {'+limit+'}}',encoding='utf-8')
    sources = ports.PORT_SOURCES + ports.ADAPTER_SOURCES + ["src/AF.GameAdapter.Bannerlord/Memory/CivilWarResultObservationAdapter.cs","src/modules/AF.Module.Memory/Records/ExternalActionObservationApplication.cs"]
    def execute(name, override=None, api="1.3"):
        paths = [override[1] if override and p == override[0] else ROOT / p for p in sources]
        project = util.project(out / name, "TeamModuleAdapters", paths + [HERE / "OwnerStubs.cs", HERE / "Program.cs", hook_file, rp_file], executable=True)
        code, log = util.run_dotnet(dotnet, ["run", "--project", str(project), "-c", "Release", "-p:DefineConstants=" + ("ADAPTER_ONLY%3BBANNERLORD_1_4_OR_GREATER" if api == "1.4" else "ADAPTER_ONLY")], out)
        (out / name / "run.log").write_text(log, encoding="utf-8")
        return code, log
    for api in ("1.3", "1.4"):
        code, log = execute("base-" + api, api=api)
        print("CURRENT ADAPTERS " + api + "\n" + log)
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
