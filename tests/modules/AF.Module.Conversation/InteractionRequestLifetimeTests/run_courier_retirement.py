"""Execute current Courier generation terminal paths and the real cancellation registry (game effects stubbed)."""
from pathlib import Path
import argparse
import importlib.util
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--run-root', type=Path)
parser.add_argument('--mutate', choices=['finalize-skips-retire', 'reply-failure-skips-retire', 'inbound-failure-skips-retire'])
args = parser.parse_args()
prefix = 'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.'
generation = (ROOT / (prefix + 'GenerationLifecycle.cs')).read_text(encoding='utf-8-sig')
campaign = (ROOT / (prefix + 'CampaignLifetime.cs')).read_text(encoding='utf-8-sig')
signatures = [
    'private void FinalizeCourierReplyGenerationOnMainThread(',
    'private void FailCourierReplyGenerationOnMainThread(',
    'private void FailInboundLetterGenerationOnMainThread(',
]
methods = [extract.declaration(generation, signature) for signature in signatures]
mutations = ['finalize-skips-retire', 'reply-failure-skips-retire', 'inbound-failure-skips-retire']
if args.mutate:
    index = mutations.index(args.mutate)
    anchor = 'RetireCourierRequestLifetime(session);'
    assert methods[index].count(anchor) == 1, 'retirement mutation anchor changed'
    methods[index] = methods[index].replace(anchor, '/* mutation: skip terminal retirement */')
field = campaign.index('private readonly Dictionary<CourierSession, ConversationRequestLifetime>')
methods.append(campaign[field:campaign.index(';', field) + 1])
methods += [extract.declaration(campaign, signature) for signature in [
    'private ConversationRequestLifetime BeginCourierRequestLifetime(',
    'private void RetireCourierRequestLifetime(',
]]
harness = r'''
using System;
using System.Collections.Generic;
using System.Threading;
using AnimusForge.Refactor.Runtime;
namespace AnimusForge;
static class Logger { internal static void Log(string category, string message) { } }
sealed class Hero { }
static class LegacyActionTagParser { internal static string RemoveProtocolTags(string s, Func<string,bool> f) => s; }
static class CoreCourierAcceptedSteps { internal const int ReplyPrepared=1; }
sealed class Fixture {
    sealed class CourierSession { internal string Id="fixture", ReplyText, ReplyPostprocessedText, LetterText="letter"; internal bool ReplyGenerated, ReplyGenerationStarted=true, Inbound, Terminal; }
    sealed class CourierPromptRun { internal CourierSession Session; }
    sealed class CourierReplyGenerationRequest { internal long RuntimeGeneration; internal string SessionId; internal CourierPromptRun SourceRun; }
    CourierSession current = new();
    internal int processed;
    bool IsCourierReplyRequestCurrent(CourierReplyGenerationRequest request) => ReferenceEquals(request.SourceRun.Session, current);
    CourierSession GetSessionById(string id) => id == current.Id ? current : null;
    bool IsTerminalStage(CourierSession session) => session.Terminal;
    bool IsInboundToPlayer(CourierSession session) => session.Inbound;
    Hero ResolveSender(CourierSession session) => new();
    string NormalizeInboundLetterText(string text, CourierSession session, Hero hero) => text;
    string CleanNpcReply(string text) => text;
    void FailModuleCourierSession(CourierSession session, string reason) { }
    void RecordModuleCourierStep(CourierSession session, int step) { }
    void ProcessSessionById(string id, string reason) {
        Check(!_courierRequestLifetimes.ContainsKey(current), "retired-before-state-machine"); processed++;
    }
    static void Log(string text) { }
    @@METHODS@@
    static int checks;
    static void Check(bool ok, string name) { checks++; if(!ok) throw new Exception("ASSERT " + name); }
    internal void Run(string path, bool stale = false, bool terminal = false) {
        current.Inbound = path == "inbound-failure"; current.Terminal=terminal;
        var lifetime=BeginCourierRequestLifetime(current);
        using var worker=lifetime.Enter();
        var token=lifetime.Token;
        long generation=SaveRuntimeGuard.CaptureGeneration() - (stale ? 1 : 0);
        if(path=="finalize") FinalizeCourierReplyGenerationOnMainThread(new(){RuntimeGeneration=generation,SessionId=current.Id,SourceRun=new(){Session=current}}, "reply", "postprocessed", "");
        else if(path=="reply-failure") FailCourierReplyGenerationOnMainThread(current.Id,generation,"");
        else FailInboundLetterGenerationOnMainThread(current.Id,generation,"fallback","");
        if(stale || terminal) {
            Check(!token.IsCancellationRequested && !current.ReplyGenerated && processed==0, path+"-rejected-no-retirement-or-commit");
            RetireCourierRequestLifetime(current);
        } else {
            Check(token.IsCancellationRequested, path+"-cancels-active-worker");
            Check(!_courierRequestLifetimes.ContainsKey(current), path+"-releases-registry");
            Check(current.ReplyGenerated && !current.ReplyGenerationStarted && processed==1, path+"-terminal-flags-and-progress");
        }
    }
    static void Main() {
        foreach(var path in new[]{"finalize","reply-failure","inbound-failure"}) {
            new Fixture().Run(path); new Fixture().Run(path,stale:true); new Fixture().Run(path,terminal:true);
        }
        Console.WriteLine("PASS CourierGenerationRetirement checks="+checks+" terminalPaths=3 actualRegistry=true actualLifetime=true domainEffects=STUB game=NOT-RUN");
    }
}
'''
out = new_run_root(ROOT, 'courier-generation-retirement', args.run_root)
(out / 'Program.cs').write_text(harness.replace('@@METHODS@@', '\n'.join(methods)), encoding='utf-8')
for relative in ['src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs',
                 'src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs',
                 'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs']:
    (out / Path(relative).name).write_text((ROOT / relative).read_text(encoding='utf-8-sig'), encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>', encoding='utf-8')
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'],
                        cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, text=True,
                        encoding='utf-8', errors='replace', timeout=90)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
raise SystemExit(result.returncode)
