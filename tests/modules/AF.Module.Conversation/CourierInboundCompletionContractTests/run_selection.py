"""Compile and exercise the production inbound receipt round-robin selector."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess

ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.InboundCompletion.cs"
EXTRACTOR = ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py"

spec = importlib.util.spec_from_file_location("channel_extract", EXTRACTOR)
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--dotnet", default=str(ROOT / "local/dotnet/8.0.425/dotnet.exe"))
parser.add_argument("--run-root", type=Path, required=True)
args = parser.parse_args()
out = args.run_root.resolve()
if not out.is_relative_to(ROOT.resolve() / "artifacts"):
    parser.error("--run-root must be under workspace artifacts")
out.mkdir(parents=True, exist_ok=True)
source = SOURCE.read_text(encoding="utf-8-sig")
method = extract.declaration(source, "private void ProcessOneCourierInboundCompletionReceipt(")
program = r'''
using System;
using System.Collections.Generic;
namespace AnimusForge {
enum CourierInboundCompletionLifecycle { Pending, Quarantined }
sealed class CourierInboundCompletionReceipt {
    public CourierInboundCompletionLifecycle Lifecycle;
    public string DiagnosticCode;
    public static bool TryDeserialize(string wire, out CourierInboundCompletionReceipt receipt, out string error) {
        receipt = new CourierInboundCompletionReceipt(); error = null; return true;
    }
}
sealed partial class CourierDeliveryBehavior {
    internal sealed class CourierSession {
        public string Id, InboundCompletionReceipt;
        public bool ReplyGenerated, ReplyGenerationStarted, Inbound = true, Terminal;
    }
    private readonly object _sessionLock = new object();
    private readonly Dictionary<string, CourierSession> _sessions = new Dictionary<string, CourierSession>();
    private string _courierInboundCompletionScanCursor = string.Empty;
    internal readonly List<string> Selected = new List<string>();
    private static bool IsInboundToPlayer(CourierSession s) => s.Inbound;
    private static bool IsTerminalStage(CourierSession s) => s.Terminal;
    private static void Log(string value) { }
    private void AbortCourierInboundCompletion(CourierSession s, string reason) => Selected.Add("abort:" + s.Id);
    private void TryCompleteCourierInboundCompletionReceipt(CourierSession s, CourierInboundCompletionReceipt r, bool memoryConfirmed, string reason) => Selected.Add(s.Id);
    internal void Add(string id, bool active = true) => _sessions.Add(id, new CourierSession { Id=id, InboundCompletionReceipt=active ? "receipt" : null });
    internal void Tick() => ProcessOneCourierInboundCompletionReceipt();
    internal string Cursor => _courierInboundCompletionScanCursor;
@@METHOD@@
}
static class Program {
    static void Require(bool yes, string why) { if (!yes) throw new Exception(why); }
    static void Main() {
        var c = new CourierDeliveryBehavior();
        c.Add("c"); c.Add("a"); c.Add("b"); c.Add("inactive", false);
        c.Tick(); c.Tick(); c.Tick(); c.Tick();
        Require(string.Join(",", c.Selected) == "a,b,c,a", "round robin order");
        var empty = new CourierDeliveryBehavior();
        empty.Add("inactive", false); empty.Tick();
        Require(empty.Cursor == string.Empty && empty.Selected.Count == 0, "empty candidate reset");
        Console.WriteLine("PASS Courier inbound receipt selector order/wrap/filter/empty");
    }
}
}
'''.replace("@@METHOD@@", method)
(out / "Program.cs").write_text(program, encoding="utf-8")
(out / "Tests.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
env = {
    "DOTNET_ROOT": str(Path(args.dotnet).resolve().parent),
    "DOTNET_CLI_HOME": str(out / "cli"),
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
    "DOTNET_NOLOGO": "1",
    "TEMP": str(out), "TMP": str(out),
    "APPDATA": str(out), "LOCALAPPDATA": str(out), "USERPROFILE": str(out),
    "HOME": str(out),
    "HOMEDRIVE": out.drive, "HOMEPATH": str(out)[len(out.drive):],
    "NUGET_PACKAGES": str(out / "packages"),
    "SystemRoot": os.environ.get("SystemRoot", r"C:\Windows"),
    "ProgramData": os.environ.get("ProgramData", r"C:\ProgramData"),
    "ALLUSERSPROFILE": os.environ.get("ALLUSERSPROFILE", r"C:\ProgramData"),
    "ProgramFiles": os.environ.get("ProgramFiles", r"C:\Program Files"),
    "ProgramFiles(x86)": os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)"),
    "windir": os.environ.get("windir", r"C:\Windows"),
}
result = subprocess.run([args.dotnet, "run", "--project", str(out / "Tests.csproj"), "-c", "Release"], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=90)
(out / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
print(result.stdout + result.stderr, end="")
raise SystemExit(result.returncode)
