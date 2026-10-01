"""Execute production draft UI callbacks; only game UI and dispatch effects are fixtures."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', required=True)
    parser.add_argument('--ref')
    parser.add_argument('--lifecycle', action='store_true')
    parser.add_argument('--public-api', action='store_true')
    parser.add_argument('--probe', action='store_true', help='Run a compiled reflection probe before adding the public surface')
    parser.add_argument('--expect-disabled', action='store_true')
    parser.add_argument('--reorder-core-enums', action='store_true')
    parser.add_argument('--mutate', choices=['ignore-revision', 'ignore-generation', 'ignore-owner', 'ignore-client', 'ignore-ticket-capacity', 'ignore-stock', 'ignore-cancel', 'ignore-operation-capacity', 'release-undrained', 'ignore-completion-receipts', 'late-action-claim', 'ignore-history-receipt', 'ignore-letter-record', 'ignore-source-run'])
    args = parser.parse_args()
    if args.public_api: args.lifecycle = True
    spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
    extract = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extract)

    def read(path):
        if args.ref:
            return subprocess.check_output(['git', 'show', args.ref + ':' + path], cwd=ROOT).decode('utf-8-sig')
        return (ROOT / path).read_text(encoding='utf-8-sig')

    creation = read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionCreation.cs')
    host = read('CourierDeliveryBehavior.cs')
    mutations = {
        'ignore-revision': ('flow.Revision == revision', 'true'),
        'ignore-generation': ('SaveRuntimeGuard.IsCurrentGeneration(flow.RuntimeGeneration)', 'true'),
        'ignore-owner': ('ReferenceEquals(Instance, this)', 'true'),
    }
    if args.mutate in mutations:
        before, after = mutations[args.mutate]
        guard = extract.declaration(creation, 'private bool IsPendingCourierFlowCurrent(')
        assert guard.count(before) == 1, 'Mutation anchor drift: ' + args.mutate
        creation = creation.replace(guard, guard.replace(before, after), 1)
    declarations = [extract.declaration(creation, s) for s in (
        'private void ShowLetterInput(', 'private void OnLetterConfirmed(')]
    declarations += [extract.declaration(host, s) for s in (
        'private sealed class PendingCourierFlow', 'private void ResetPendingFlow(')]
    for signature in ('private bool IsPendingCourierFlowCurrent(', 'private long BeginCourierDraftStep(',
                      'private void ResetPendingCourierFlow(', 'private CourierSession DispatchCourierDraft('):
        if signature in creation:
            declarations.append(extract.declaration(creation, signature))
    admission_path = 'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DraftAdmission.cs'
    if not args.ref:
        admission = extract.declaration(read(admission_path), 'public sealed partial class CourierDeliveryBehavior')
        ticket_mutations = {
            'ignore-client': ('!string.Equals(ticket.ClientId, clientId, StringComparison.Ordinal)', 'false'),
            'ignore-ticket-capacity': ('owner._courierDraftTickets.Count >= MaximumCourierDraftTickets', 'false'),
            'ignore-stock': ('remaining < entry.Amount', 'false'),
        }
        if args.mutate in ticket_mutations:
            before, after = ticket_mutations[args.mutate]
            assert admission.count(before) == 1, 'Mutation anchor drift: ' + args.mutate
            admission = admission.replace(before, after)
        declarations.append(admission[admission.index('{') + 1:admission.rfind('}')])
        module = extract.declaration(read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.ModuleSubmission.cs'),
                                     'public sealed partial class CourierDeliveryBehavior')
        if args.mutate == 'ignore-operation-capacity':
            before = 'owner._moduleCourierRequests.Count >= MaximumModuleCourierOperations'
            assert module.count(before) == 1
            module = module.replace(before, 'false')
        if args.mutate == 'release-undrained':
            before = 'if (request.AdmissionReleased) _moduleCourierRequests.Remove(operation);'
            assert module.count(before) == 1
            module = module.replace(before, '_moduleCourierRequests.Remove(operation);')
        declarations.append(module[module.index('{') + 1:module.rfind('}')])
        declarations.append(extract.declaration(read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DetachedPostprocess.cs'),
                                               'private async Task<T> RunCourierOwnerPhaseAsync<T>('))
        delivery = read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.DeliveryLifetime.cs')
        declarations.extend(extract.declaration(delivery, signature) for signature in
                            ('private void CompleteAndDestroyCourier(', 'private void HandleCourierMissing('))
        declarations.append(extract.declaration(read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.SessionRegistry.cs'),
                                               'private CourierSession GetSessionById('))
        # cb045840: terminal transport retires the per-session request lifetime. Link the real
        # owner members only; the file's registry/retirement members stay harness fixtures.
        campaign = read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.CampaignLifetime.cs')
        field_start = campaign.index('private readonly Dictionary<CourierSession, ConversationRequestLifetime> _courierRequestLifetimes')
        declarations.append(campaign[field_start:campaign.index(';', field_start) + 1])
        declarations.extend(extract.declaration(campaign, signature) for signature in (
            'private ConversationRequestLifetime BeginCourierRequestLifetime(', 'private void RetireCourierRequestLifetime(',
            'private void RetireCourierRequestLifetimes('))
    if args.lifecycle:
        assert not args.ref, 'Lifecycle requires current receipt owners.'
        sources_by_file = {
            'GenerationLifecycle': ['private void DeliverToRecipient(', 'private void FinalizeCourierReplyGenerationOnMainThread('],
            'DomainCommit': ['private bool CommitGeneratedReplyActionsAtRecipientCore(', 'private static bool CommitCourierDialogueHistory(', 'private bool PersistCourierReplyToHistories('],
            'DeliveryLifetime': ['private void CompleteReturn(', 'private bool ReturnCourierContentsToPlayer(', 'private void OnMobilePartyDestroyed('],
            'LetterInventory': ['private void EnsureCourierLetterInventoryData(', 'private CourierLetterInventoryRecord NormalizeCourierLetterInventoryRecord(', 'private bool RememberCourierLetterInventoryRecord(', 'private static bool AddCourierLetterToPlayerInventory(', 'private static int CountItemInRoster(ItemRoster roster, string itemStringId, out ItemObject firstItem)'],
            'PromptPreparation': ['private sealed class CourierPromptRun', 'private CourierPromptRun BeginCourierPromptRun(', 'private bool IsCourierPromptRunCurrent(', 'private bool IsCourierReplyRequestCurrent('],
        }
        for name, signatures in sources_by_file.items():
            source = read('src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.' + name + '.cs')
            declarations.extend(extract.declaration(source, signature) for signature in signatures)
        declarations.extend(extract.declaration(host, signature) for signature in
                            ('private sealed class CourierReplyGenerationRequest', 'private sealed class CourierLetterInventoryRecord'))
        lifecycle_mutations = {
            'ignore-completion-receipts': ('if (receipt.Complete)', 'if (true)'),
            'late-action-claim': ('// Consume before domain handlers: reentrant arrivals must not replay any accepted action.\n\t\tsession.PostprocessConsumed = true;', '// Mutant omits the pre-handler claim.'),
            'ignore-history-receipt': ('?.HistoryWritten == true', '!= null'),
            'ignore-letter-record': ('return remembered;', 'return true;'),
            'ignore-source-run': ('&& _courierPromptRuns.TryGetValue(run.Session, out CourierPromptRun current) && ReferenceEquals(current, run)', ''),
        }
        if args.mutate in lifecycle_mutations:
            before, after = lifecycle_mutations[args.mutate]
            assert sum(part.count(before) for part in declarations) == 1, 'Mutation anchor drift: ' + args.mutate
            declarations = [part.replace(before, after) for part in declarations]

    output = HERE / '.generated' / ('public-' + ('reordered' if args.reorder_core_enums else 'current') if args.public_api else 'lifecycle-' + (args.mutate or 'current') if args.lifecycle else 'admission-baseline' if args.ref else 'admission-' + (args.mutate or 'current'))
    output.mkdir(parents=True, exist_ok=True)
    harness = (HERE / 'AdmissionHarness.cs.txt').read_text(encoding='utf-8')
    (output / 'Program.cs').write_text(harness.replace('@@DECLARATIONS@@', '\n'.join(declarations)) + ((HERE / 'LifecycleHarness.cs.txt').read_text(encoding='utf-8') if args.lifecycle else ''), encoding='utf-8')
    defines = '' if args.ref else '<DefineConstants>COURIER_DRAFT_TICKETS' + (';COURIER_LIFECYCLE' if args.lifecycle else '') + '</DefineConstants>'
    sources = ['src/modules/AF.Module.Conversation/Internal/CoreDialogueContracts.cs', 'src/modules/AF.Module.Conversation/Internal/CoreDialogueOperation.cs', 'src/modules/AF.Module.Conversation/Internal/CoreDialogueClient.cs',
               'src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs']
    if not args.ref:
        sources += ['src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs',
                    'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs']
    from xml.sax.saxutils import escape
    includes = ''
    for path in sources:
        target = ROOT / path
        if args.mutate == 'ignore-cancel' and path.endswith('/CoreDialogueOperation.cs'):
            text = target.read_text(encoding='utf-8-sig')
            before = 'if (_snapshot.State != CoreDialogueState.Queued) return false;'
            assert text.count(before) == 1
            target = output / 'CoreDialogueOperation.cs'
            target.write_text(text.replace(before, 'if (_snapshot.State == CoreDialogueState.Running) return false;'), encoding='utf-8')
            # Default compile items already include this generated mutant.
            continue
        includes += '<Compile Include="' + escape(str(target)) + '" />'
    if args.public_api:
        return run_public_consumer(args, output, extract, defines)
    (output / 'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit>' + defines + '</PropertyGroup><ItemGroup>' + includes + '</ItemGroup></Project>', encoding='utf-8')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    env = os.environ.copy()
    env.update(DOTNET_ROOT=str(Path(args.dotnet).resolve().parent), DOTNET_CLI_HOME=str(ROOT / '.tmp/dotnet-cli'),
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_NOLOGO='1')
    result = subprocess.run([args.dotnet, 'run', '--project', str(output / 'Tests.csproj'), '-c', 'Release'],
                            cwd=ROOT, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
    log = result.stdout + result.stderr
    (output / 'run.log').write_text(log, encoding='utf-8')
    print(log, end='')
    return result.returncode


def run_public_consumer(args, output, extract, defines):
    # Reuse exactly the preceding production-owner extraction and existing lifecycle fixtures.
    # Only the control assembly's surface manipulates fixture game state; the consumer is unrelated.
    spec = importlib.util.spec_from_file_location('api_suite', ROOT / 'tests/AF.Contracts/ModuleFrameworkApiTests/run.py')
    api = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(api)
    host = (ROOT / 'tests/AF.Contracts/ModuleFrameworkApiTests/HostStubs.cs').read_text(encoding='utf-8-sig')
    courier_stub = extract.declaration(host, 'internal static class CourierDeliveryBehavior')
    host = host.replace(courier_stub, '') # Never let the directory-only stub shadow the actual owner.
    (output / 'DirectoryStubs.cs').write_text(host, encoding='utf-8')
    paths = [p for p in api.SOURCES if p != 'tests/AF.Contracts/ModuleFrameworkApiTests/NativeOwnerStub.cs']
    paths += ['src/AF.Foundation.Runtime/Scheduling/PendingOperationRegistry.cs']
    sources = [output / 'Program.cs', output / 'DirectoryStubs.cs', HERE / 'PublicControl.cs.txt']
    for path in paths:
        target = ROOT / path
        if args.reorder_core_enums and path == 'src/modules/AF.Module.Conversation/Internal/CoreDialogueContracts.cs':
            text = target.read_text(encoding='utf-8-sig')
            changes = {
                'Queued, Running, Completed, Rejected, Cancelled, Failed': 'Queued=41, Running=12, Completed=8, Rejected=3, Cancelled=79, Failed=20',
                'NoConfirmedEffect, UnknownAfterStart, CompletedByOwner': 'NoConfirmedEffect=28, UnknownAfterStart=91, CompletedByOwner=53',
                'CancelledBeforeStart, AlreadyTerminal, TooLate': 'CancelledBeforeStart=56, AlreadyTerminal=17, TooLate=99',
                'NotStarted, InTransit, Returned, Destroyed, Missing, Unconfirmed': 'NotStarted=55, InTransit=91, Returned=32, Destroyed=11, Missing=8, Unconfirmed=23',
                'None = 0, Dispatched = 1, ReplyPrepared = 2, Arrived = 4, Payload = 8,': 'None = 0, Dispatched = 512, ReplyPrepared = 1024, Arrived = 2048, Payload = 4096,',
                'DeliveryHistory = 16, Actions = 32, ReplyHistory = 64, ReplyDelivered = 128, ContentsReturned = 256': 'DeliveryHistory = 8192, Actions = 16384, ReplyHistory = 32768, ReplyDelivered = 65536, ContentsReturned = 131072',
            }
            for before, after in changes.items():
                assert text.count(before) == 1, 'Enum reordering anchor drift: ' + before
                text = text.replace(before, after)
            target = output / 'ReorderedContracts.cs'
            target.write_text(text, encoding='utf-8')
        sources.append(target)
    # Explicit Compile items and separate project directories prevent consumer/denied sources
    # from being accidentally compiled into the host under test.
    library = api.project(output / 'Library', 'CourierApiUnderTest', [p.resolve() for p in sources])
    project_text = library.read_text(encoding='utf-8').replace('</PropertyGroup>', defines + '</PropertyGroup>', 1)
    library.write_text(project_text, encoding='utf-8')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
    source = HERE / 'PublicClient.cs.txt'
    if args.probe:
        source = output / 'Probe.cs'
        source.write_text('using AnimusForge.Api.V1; class Probe { static int Main() { '
            'bool ready=typeof(AfDialogueClient).GetMethod("CaptureCourierContextTicket")!=null '
            '&&typeof(AfDialogueClient).GetMethod("SubmitCourier")!=null '
            '&&typeof(AfDialogueResult).GetProperty("Courier")!=null; '
            'System.Console.WriteLine(ready?"PASS public Courier surface exists":"FAIL public Courier surface missing");return ready?0:1;} }', encoding='utf-8')
    client = api.project(output / 'Client', 'CourierIndependentConsumer', [source.resolve()], [library.resolve()], True)
    dotnet = str(Path(args.dotnet).resolve())
    command = ['run', '--project', str(client.resolve()), '-c', 'Release']
    if args.expect_disabled: command += ['--', '--expect-disabled']
    status, log = api.run_dotnet(dotnet, command, ROOT)
    (output / 'public.log').write_text(log, encoding='utf-8')
    print(log, end='')
    if status or args.probe: return status
    denied_source = output / 'Denied.cs'
    denied_source.write_text('class Denied { static void Main(){ AnimusForge.Refactor.Modules.CoreDialogueServices.CreateClient(); } }', encoding='utf-8')
    denied = api.project(output / 'Denied', 'CourierUnrelatedDenied', [denied_source.resolve()], [library.resolve()], True)
    status, log = api.run_dotnet(dotnet, ['build', str(denied.resolve()), '-c', 'Release'], ROOT)
    (output / 'denied.log').write_text(log, encoding='utf-8')
    assert status != 0 and 'CS0122' in log and 'CoreDialogueServices' in log, log
    print('PASS unrelated Courier consumer cannot use internal CoreDialogueServices (CS0122)')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
