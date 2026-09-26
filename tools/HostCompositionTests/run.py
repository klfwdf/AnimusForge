"""Compile the production Tick dispatcher against instrumented engine stubs and reject behavior mutants."""
from pathlib import Path
import argparse
import importlib.util
import json
import os
import re

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
SOURCE = ROOT / 'src/AF.GameAdapter.Bannerlord/Composition/ApplicationTickComposition.cs'


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


extract = load('host_tick_declaration', 'tools/ChannelCutoverBoundaryTests/run.py').declaration
inverse = load('host_source_inverse', 'tools/HostCompositionTests/source_inverse.py')
util = load('host_dotnet', 'tools/ModuleFrameworkApiTests/run.py')


def fixtures(source):
    fast = extract(source, 'private static void RunFastApplicationTickPhases(SubModule host)')
    watched = extract(source, 'private static void RunWatchedApplicationTickPhases(SubModule host)')
    static = {}
    instance = {}
    order = []
    for line in fast.splitlines():
        line = line.strip()
        if line == 'host.ProcessPendingInitialApiGuideNotice();':
            order.append('SubModule.ProcessPendingInitialApiGuideNotice')
            continue
        match = re.fullmatch(r'([A-Za-z]\w*)\.Instance\?\.([A-Za-z]\w*)\(\);', line)
        if match:
            owner, method = match.groups()
            instance.setdefault(owner, set()).add(method)
            order.append(owner + '.' + method)
            continue
        match = re.fullmatch(r'([A-Za-z]\w*)\.([A-Za-z]\w*)\(\);', line)
        if match:
            owner, method = match.groups()
            static.setdefault(owner, set()).add(method)
            order.append(owner + '.' + method)
            continue
    assert len(order) == 36 and len(set(order)) == 36, 'Tick fixture failed to cover every original phase'
    scope_names = re.findall(r'RunWatchedTickPhase\("([^"]+)"', watched)
    assert len(scope_names) == 36 and len(set(scope_names)) == 36, 'Watched fixture failed to cover every phase'
    assert not (set(static) & set(instance)), 'Unexpected mixed static/instance tick owner'
    stubs = ['using System;\nusing System.Collections.Generic;\nnamespace AnimusForge {',
        '''internal static class TickTrace {
 internal static readonly List<string> Events = new();
 internal static bool Freeze, Perf;
 internal static string ThrowOn, DisableScopesOn;
 internal static void Reset(bool freeze, bool perf, string throwOn = null, string disableOn = null) {
   Events.Clear(); Freeze = freeze; Perf = perf; ThrowOn = throwOn; DisableScopesOn = disableOn;
 }
 internal static void Hit(string name) {
   Events.Add("hit:" + name);
   if (name == DisableScopesOn) { Freeze = false; Perf = false; }
   if (name == ThrowOn) throw new InvalidOperationException("original_phase_failure");
 }
 internal static IDisposable Scope(string prefix, string name) {
   Events.Add(prefix + "+:" + name); return new ScopeExit(prefix, name);
 }
 private sealed class ScopeExit : IDisposable {
   private readonly string _prefix, _name;
   internal ScopeExit(string prefix, string name) { _prefix=prefix; _name=name; }
   public void Dispose() { Events.Add(_prefix + "-:" + _name); }
 }
}
internal sealed class SubModule {
 internal void ProcessPendingInitialApiGuideNotice() => TickTrace.Hit("SubModule.ProcessPendingInitialApiGuideNotice");
 internal void TickWarStatsMapButton(float dt) => TickTrace.Hit("WarStats");
}
internal static class FreezeWatchdog {
 internal static void BeginFrame(float dt) => TickTrace.Events.Add("freeze.begin");
 internal static void EndFrame() => TickTrace.Events.Add("freeze.end");
 internal static bool IsScopeRecordingActive() => TickTrace.Freeze;
 internal static IDisposable Scope(string name) => TickTrace.Scope("freeze", name);
 internal static void Mark(string name, string detail, bool immediate) => TickTrace.Events.Add("freeze.mark:" + name);
}
internal static class PerfProbe {
 internal static long BeginFrame(float dt) { TickTrace.Events.Add("perf.begin"); return 7; }
 internal static void EndFrame(long frame, string name) => TickTrace.Events.Add("perf.end");
 internal static bool IsDetailedScopeRecordingActive() => TickTrace.Perf;
 internal static IDisposable Scope(string name) => TickTrace.Scope("perf", name);
}
internal static class CampaignTickDiagnosticsPatch {
 internal static void RefreshCheckpointWriteBudget() => TickTrace.Events.Add("checkpoint");
}''']
    for owner, methods in sorted(static.items()):
        members = ''.join(f' internal static void {method}() => TickTrace.Hit("{owner}.{method}");\n'
                          for method in sorted(methods))
        namespace = 'AnimusForge.PolicyEffects' if owner == 'PolicyEffectModuleManagerPopup' else 'AnimusForge'
        if namespace != 'AnimusForge':
            stubs.append('}\nnamespace AnimusForge.PolicyEffects {')
        stubs.append(f'internal static class {owner} {{\n{members}}}')
        if namespace != 'AnimusForge':
            stubs.append('}\nnamespace AnimusForge {')
    for owner, methods in sorted(instance.items()):
        members = ''.join(f' internal void {method}() => TickTrace.Hit("{owner}.{method}");\n'
                          for method in sorted(methods))
        stubs.append(f'internal sealed class {owner} {{ internal static {owner} Instance {{ get; }} = new();\n{members}}}')
    stubs.append('}\n')
    return '\n'.join(stubs), order, scope_names


def program(order, scope_names):
    expected = ','.join(json.dumps(item) for item in order)
    scopes = ','.join(json.dumps(item) for item in scope_names)
    return '''using System;
using System.Linq;
using AnimusForge;
internal static class Program {
 static readonly string[] Expected = new[] {''' + expected + '''};
 static readonly string[] ScopeNames = new[] {''' + scopes + '''};
 static readonly SubModule Host = new();
 static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); }
 static string[] Hits() => TickTrace.Events.Where(x => x.StartsWith("hit:")).Select(x => x.Substring(4)).ToArray();
 static void HitOrder(bool warStats) {
   var want = warStats ? Expected.Concat(new[]{"WarStats"}) : Expected;
   Check(Hits().SequenceEqual(want), "ordered phases/WarStats");
 }
 static void FrameEnd() {
   var e = TickTrace.Events;
   int begin=e.IndexOf("freeze+:SubModule.PerfProbe.EndFrame");
   int perf=e.IndexOf("perf.end");
   int close=e.IndexOf("freeze-:SubModule.PerfProbe.EndFrame");
   int freeze=e.IndexOf("freeze.end");
   Check(begin>=0 && begin<perf && perf<close && close<freeze, "finally frame-end order");
 }
 static void Normal(bool freeze, bool perf) {
   TickTrace.Reset(freeze,perf); ApplicationTickComposition.Run(Host,0.1f);
   HitOrder(true); FrameEnd();
   int count=TickTrace.Events.Count(x=>x.StartsWith("freeze+:SubModule.") && x!="freeze+:SubModule.PerfProbe.EndFrame");
   int perfCount=TickTrace.Events.Count(x=>x.StartsWith("perf+:SubModule."));
   Check(count==(freeze||perf?36:0) && perfCount==count, "phase scope count");
   if(freeze||perf) {
     foreach(var name in ScopeNames) {
       Check(TickTrace.Events.Count(x=>x=="freeze+:"+name)==1 &&
         TickTrace.Events.Count(x=>x=="perf+:"+name)==1 &&
         TickTrace.Events.Count(x=>x=="perf-:"+name)==1 &&
         TickTrace.Events.Count(x=>x=="freeze-:"+name)==1, "phase scope pairing");
     }
   }
 }
 static void DynamicFallback() {
   TickTrace.Reset(true,false,disableOn:Expected[0]); ApplicationTickComposition.Run(Host,0.1f);
   HitOrder(true); FrameEnd();
   Check(TickTrace.Events.Count(x=>x.StartsWith("freeze+:SubModule.") && x!="freeze+:SubModule.PerfProbe.EndFrame")==1,
     "scope disabled during frame must fall back to direct later phases");
 }
 static void Failure(bool watched) {
   TickTrace.Reset(watched,false,throwOn:Expected[4]);
   try { ApplicationTickComposition.Run(Host,0.1f); throw new Exception("FAIL phase exception swallowed"); }
   catch(InvalidOperationException ex) { Check(ex.Message=="original_phase_failure", "original exception must rethrow"); }
   Check(Hits().SequenceEqual(Expected.Take(5)), "later phases/WarStats executed after failure");
   Check(TickTrace.Events.Contains("freeze.mark:SubModule.OnApplicationTick.exception"), "missing freeze mark");
   FrameEnd();
 }
 static int Main() {
   try { Normal(false,false); Normal(true,false); Normal(false,true); DynamicFallback(); Failure(false); Failure(true);
     Console.WriteLine("PASS source-linked 36-phase fast/watched/scope fallback/exception/finally/WarStats replay"); return 0; }
   catch(Exception ex) { Console.WriteLine(ex.Message.StartsWith("FAIL ")?ex.Message:"FAIL "+ex); return 1; }
 }
}
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', default=os.environ.get('DOTNET_EXE', str(ROOT / 'local/dotnet/8.0.425/dotnet.exe')))
    parser.add_argument('--skip-mutations', action='store_true')
    args = parser.parse_args()
    inverse.verify()
    source = SOURCE.read_text(encoding='utf-8-sig')
    stubs, order, scope_names = fixtures(source)
    out = HERE / '.generated'
    out.mkdir(parents=True, exist_ok=True)
    (out / 'Stubs.cs').write_text(stubs, encoding='utf-8')
    (out / 'Program.cs').write_text(program(order, scope_names), encoding='utf-8')
    (out / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding='utf-8')
    mutations = {
        'reorder_fast': ('ShoutTextInputPopup.ProcessDeferredCloseIfNeeded();\n\t\tShoutTextInputPopup.CloseForSystemInterruptionIfNeeded();',
                         'ShoutTextInputPopup.CloseForSystemInterruptionIfNeeded();\n\t\tShoutTextInputPopup.ProcessDeferredCloseIfNeeded();'),
        'skip_warstats': ('host.TickWarStatsMapButton(dt);', ';'),
        'skip_mark': ('FreezeWatchdog.Mark("SubModule.OnApplicationTick.exception", ex.GetType().Name + ": " + ex.Message, immediate: true);', ';'),
        'skip_perf_end': ('PerfProbe.EndFrame(perfFrame, "SubModule.OnApplicationTick.total");', ';'),
    }
    results = []
    variants = [('current', source)]
    if not args.skip_mutations:
        variants += [(name, source.replace(before, after, 1)) for name, (before, after) in mutations.items()]
        fallback = 'if (!FreezeWatchdog.IsScopeRecordingActive() && !PerfProbe.IsDetailedScopeRecordingActive())'
        assert source.count(fallback) == 2
        last = source.rfind(fallback)
        variants.append(('skip_scope_fallback', source[:last] + source[last:].replace(fallback, 'if (false)', 1)))
    for name, candidate in variants:
        folder = out / name
        folder.mkdir(parents=True, exist_ok=True)
        (folder / 'Tick.cs').write_text(candidate, encoding='utf-8')
        project = util.project(folder, 'HostTick', [folder / 'Tick.cs', out / 'Stubs.cs', out / 'Program.cs'], executable=True)
        status, log = util.run_dotnet(args.dotnet, ['run', '--project', str(project), '-c', 'Release'], out)
        (folder / 'run.log').write_text(log, encoding='utf-8')
        if name == 'current':
            print(log, end='')
            assert status == 0, 'Current host Tick replay failed'
        else:
            assert status != 0 and 'FAIL ' in log and 'error CS' not in log, 'Mutation failed to yield behavioral failure: ' + name + '\n' + log
            print('PASS behavioral mutant rejected: ' + name)
        results.append({'name': name, 'exitCode': status, 'expectedFailure': name != 'current'})
    (out / 'results.json').write_text(json.dumps({'baseline': inverse.BASELINE, 'results': results}, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
