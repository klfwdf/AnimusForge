"""Execute real IntegratedModuleHost and source-derived module Tick boundaries."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import sys

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet

def load(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

STUBS = '''using System;
using System.Collections.Generic;
namespace TaleWorlds.Core { public interface IGameStarter {} }
namespace TaleWorlds.CampaignSystem { public sealed class CampaignGameStarter:TaleWorlds.Core.IGameStarter{} }
namespace AnimusForge.Refactor.Modules { internal enum InternalModuleRuntimeState { NotInitialized,Ready,Unavailable,Failed } internal static class HostedExtensionCatalog { internal const string Illustrator="af.extension.illustrator",DialogueUi="af.extension.dialogue_ui",Coup="af.extension.coup"; } }
namespace AnimusForge { internal static class ModuleFrameworkRuntime { internal static void ReportHostedExtensionState(string id,AnimusForge.Refactor.Modules.InternalModuleRuntimeState state,string reason,bool requireStarted=false)=>throw new NotSupportedException("non-Tick module lifecycle is outside this fixture"); } }
namespace RichExecutions.Core { public static class VengeanceIntegration { public static bool IsEmbeddedHostActive => true; } }
namespace AnimusForge {
 internal static class Trace {
  internal static readonly List<string> Calls = new();
  internal static string ThrowOn;
  internal static bool Enabled = true, LogThrows;
  internal static readonly Exception Failure = new InvalidOperationException("controlled-original-failure");
  internal static void Hit(string name, float? dt = null) {
   Calls.Add(name); if (dt.HasValue && dt.Value != 0.125f) throw new Exception("FAIL dt forwarded");
   if (name == ThrowOn) throw Failure;
  }
  internal static void Reset(string failure = null) { Calls.Clear(); ThrowOn = failure; Enabled = true; LogThrows = false; }
 }
 internal static class Logger { internal static void Log(string name, string text) { Trace.Calls.Add("host.log:"+name); } }
}
namespace AnimusForge.DialogueUI {
 internal static class DialogueUiRuntime {
  internal static bool Enabled => AnimusForge.Trace.Enabled;
  internal static void LogOnce(string key, string text) { AnimusForge.Trace.Calls.Add("ui.log:"+key); if (AnimusForge.Trace.LogThrows) throw AnimusForge.Trace.Failure; }
 }
 internal static class ShoutUiAdapter { internal static void Tick() => AnimusForge.Trace.Hit("ui.shout"); }
 internal static class NativeUiAdapter { internal static void Tick(float dt) => AnimusForge.Trace.Hit("ui.native",dt); }
 internal static class SceneWheel { internal static void Tick() => AnimusForge.Trace.Hit("ui.wheel"); }
 internal static class SceneSessionPanel { internal static void Tick(float dt) => AnimusForge.Trace.Hit("ui.session",dt); }
}
namespace AnimusForge.Illustrator {
 internal static class IllustratorRuntime { internal static void Tick() => AnimusForge.Trace.Hit("illustrator.runtime"); }
 namespace Engine { internal static class ScreenCaptureHelper { internal static void ObservePanoramaFrame(float dt) => AnimusForge.Trace.Hit("illustrator.panorama",dt); } }
}
namespace AnimusForge.Coup {
 internal sealed class CoupCampaignBehavior {
  internal static CoupCampaignBehavior Instance = new();
  internal void OnEngineTick(float dt) => AnimusForge.Trace.Hit("coup.campaign",dt);
 }
 internal sealed class CoupRebellionBridge {
  internal static CoupRebellionBridge Instance = new();
  internal void OnEngineTick(float dt) => AnimusForge.Trace.Hit("coup.rebellion",dt);
 }
}
'''

PROGRAM = '''using System;
using System.Linq;
using AnimusForge;
internal static class Program {
 static int checks;
 static void Check(bool ok,string name) { if(!ok) throw new Exception("FAIL "+name); checks++; }
 static void Sequence(params string[] expected) => Check(Trace.Calls.SequenceEqual(expected), "sequence: "+string.Join(",",Trace.Calls));
 static void Tick(bool throws = false) {
  Exception seen = null;
  try { IntegratedModuleHost.Tick(0.125f); } catch(Exception error) { seen=error; }
  Check(throws ? ReferenceEquals(seen,Trace.Failure) : seen == null,"original exception identity / expected isolation");
 }
 static void Main() { try { Run(); } catch(Exception e) { Console.Error.WriteLine(e); Environment.ExitCode=1; } }
 static void Run() {
  Trace.Reset(); Tick(); Sequence("ui.shout","ui.native","ui.wheel","ui.session","illustrator.runtime","illustrator.panorama","coup.campaign","coup.rebellion");
  Trace.Reset(); Trace.Enabled=false; Tick(); Sequence("illustrator.runtime","illustrator.panorama","coup.campaign","coup.rebellion");
  Trace.Reset("ui.native"); Tick(); Sequence("ui.shout","ui.native","ui.log:tick-error","illustrator.runtime","illustrator.panorama","coup.campaign","coup.rebellion");
  Trace.Reset("illustrator.runtime"); Tick(); Sequence("ui.shout","ui.native","ui.wheel","ui.session","illustrator.runtime","coup.campaign","coup.rebellion");
  Trace.Reset("illustrator.panorama"); Tick(); Sequence("ui.shout","ui.native","ui.wheel","ui.session","illustrator.runtime","illustrator.panorama","coup.campaign","coup.rebellion");
  Trace.Reset("coup.campaign"); Tick(true); Sequence("ui.shout","ui.native","ui.wheel","ui.session","illustrator.runtime","illustrator.panorama","coup.campaign");
  Trace.Reset("coup.rebellion"); Tick(true); Sequence("ui.shout","ui.native","ui.wheel","ui.session","illustrator.runtime","illustrator.panorama","coup.campaign","coup.rebellion");
  Trace.Reset("ui.shout"); Trace.LogThrows=true; Tick(true); Sequence("ui.shout","ui.log:tick-error");
  Trace.Reset(); AnimusForge.Coup.CoupCampaignBehavior.Instance=null; AnimusForge.Coup.CoupRebellionBridge.Instance=null; Tick(); Sequence("ui.shout","ui.native","ui.wheel","ui.session","illustrator.runtime","illustrator.panorama");
  Console.WriteLine($"PASS {checks} Host Tick / module-boundary assertions; dt, order, optional disable, original failure, logging failure, null owners.");
  Console.WriteLine("NOT TESTED: child gameplay algorithms, live Bannerlord, timing budgets; no diplomacy fixture compiled.");
 }
}
'''

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet")
    parser.add_argument("--run-root", type=Path)
    args = parser.parse_args()
    dotnet = str(resolve_dotnet(ROOT, args.dotnet))
    out = new_run_root(ROOT, "integrated-host-tick", args.run_root)
    util = load("tick_dotnet", "tests/AF.Contracts/ModuleFrameworkApiTests/run.py")
    extract = load("tick_extract", "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py").declaration
    paths = ["src/AF.GameAdapter.Bannerlord/Composition/IntegratedModuleHost.cs"]
    wrappers = STUBS
    for name in ("DialogueUI", "Illustrator", "Coup"):
        path = f"extensions/AnimusForge.{name}/src/SubModule.cs"
        paths.append(path)
        tick = extract((ROOT / path).read_text(encoding="utf-8-sig"), "internal static void Tick(float dt)")
        wrappers += f'\nnamespace AnimusForge.{name} {{ internal static class SubModule {{\n{tick}\ninternal static void Start() {{}} internal static bool TryStart()=>throw new System.NotSupportedException("non-Tick start"); internal static void Shutdown() {{}} internal static bool TryInstallPresentation()=>throw new System.NotSupportedException("non-Tick presentation"); internal static void InstallPresentation() {{}} internal static void RegisterCampaign(TaleWorlds.Core.IGameStarter starter) {{}} }} }}\n'
    (out / "Stubs.cs").write_text(wrappers, encoding="utf-8")
    (out / "Program.cs").write_text(PROGRAM, encoding="utf-8")
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    def execute(name, host):
        project = util.project(out / name, "IntegratedTickContract", [host, out / "Stubs.cs", out / "Program.cs"], executable=True)
        code, log = util.run_dotnet(dotnet, ["run", "--project", str(project), "-c", "Release"], out)
        (out / name / "run.log").write_text(log, encoding="utf-8")
        return code, log
    code, log = execute("base", ROOT / paths[0]); print(log)
    if code: return code
    source = (ROOT / paths[0]).read_text(encoding="utf-8-sig")
    coup = "global::AnimusForge.Coup.SubModule.Tick(dt);"
    mutations = {"skip_coup": (coup, ";"), "swallow_coup_failure": (coup, 'try { global::AnimusForge.Coup.SubModule.Tick(dt); } catch (System.Exception) { }'),
                 "duplicate_ui": ("global::AnimusForge.Illustrator.SubModule.Tick(dt);", "global::AnimusForge.DialogueUI.SubModule.Tick(dt);")}
    for name,(before,after) in mutations.items():
        assert source.count(before)==1, "Mutation anchor drift: "+name
        mutant=out/(name+".cs"); mutant.write_text(source.replace(before,after),encoding="utf-8")
        code,log=execute(name,mutant)
        assert code!=0 and "FAIL " in log and "error CS" not in log,"Mutation not rejected: "+name+"\n"+log
        print("PASS Host behavioral mutation rejected: "+name)
    (out/"source-hashes.txt").write_text("\n".join(p+" "+hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in paths),encoding="utf-8")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
