"""Separate historical owner retirement from current shared public-execution reset."""
from pathlib import Path
import argparse
import importlib.util
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
spec = importlib.util.spec_from_file_location(
    "extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)

# Explicit historical proof, not a fallback for failed current source checks.
HISTORICAL = "4bde450ba2f3e982b9c0f2a21f71442c1ac1cc35"
def historical(path):
    return subprocess.check_output(["git", "show", HISTORICAL + ":" + path], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n")
presentation = historical("src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPresentation.cs")
lifetime = historical("src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.CampaignLifetime.cs")
arm = extract.declaration(presentation, "private void TryQueueCeremonyExecutionOrder(")
clear = extract.declaration(presentation, "internal static void ClearCeremonyExecutionOrder()")
clear_owner = extract.declaration(presentation, "internal static void ClearCeremonyExecutionOrder(ShoutBehavior owner)")
retire = extract.declaration(lifetime, "internal void RetireCampaignRuntime(string reason)")
call = "NativeConversationTurnHost.ClearCeremonyExecutionOrder(this);"
assert retire.index("_pendingMainThreadFunctions.Seal();") < retire.index(call)
assert retire.index(call) < retire.index("ResetInstanceTransientRuntimeForLoadedSave(reason);")
assert "_ceremonyOrderManager.ConversationEndOneShot -= _ceremonyOrderHandler;" in clear
assert arm.index("_ceremonyOrderOwner = _owner;") < arm.index("manager.ConversationEndOneShot += handler;"), "arming did not bind owner before subscription"
assert "ReferenceEquals(_ceremonyOrderOwner, owner)" in clear_owner

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--mutate", choices=["skip-clear", "drop-owner-guard", "skip-public-reset", "drop-public-unsubscribe"])
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
if args.mutate == "skip-clear":
    retire = retire.replace(call, "// mutation: skip clear", 1)
elif args.mutate == "drop-owner-guard":
    clear_owner = clear_owner.replace("if (!ReferenceEquals(_ceremonyOrderOwner, owner)) return;", "// mutation: drop owner guard", 1)

dotnet = resolve_dotnet(ROOT)
run_root = new_run_root(ROOT, "native-ceremony-lifetime", args.run_root)
out = run_root / "historical-owner"
out.mkdir()

template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8-sig")
(out / "Program.cs").write_text(template.replace("@@CLEAR@@", clear)
                           .replace("@@CLEAR_OWNER@@", clear_owner)
                           .replace("@@RETIRE@@", retire), encoding="utf-8")
(out / "Tests.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable>'
    '<ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text(
    "<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
env = minimal_test_environment(dotnet, out)
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Tests.csproj"), "-c", "Release"],
                        cwd=out, env=env, capture_output=True, text=True,
                        encoding="utf-8", errors="replace", timeout=90)
log = result.stdout + result.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
print("Historical Native owner proof:", HISTORICAL)
if result.returncode: raise SystemExit(result.returncode)

# Current production reset and campaign-retirement bodies, with event/storage-only fakes.
public = (ROOT / "src/bridges/Vengeance/Host/PublicExecutionOrderRuntime.cs").read_text(encoding="utf-8-sig")
current_lifetime = (ROOT / "src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.CampaignLifetime.cs").read_text(encoding="utf-8-sig")
current_retire = extract.declaration(current_lifetime, "internal void RetireCampaignRuntime(string reason)")
current_clear = extract.declaration(public, "private static void ClearConversation()")
current_reset = extract.declaration(public, "internal static void Reset()")
assert current_retire.index("_pendingMainThreadFunctions.Seal();") < current_retire.index("PublicExecutionOrderRuntime.Reset();") < current_retire.index("ResetInstanceTransientRuntimeForLoadedSave(reason);")
assert "_manager.ConversationEndOneShot -= _handler;" in current_clear
assert "lock (Gate) Permits.Clear();" in current_reset
if args.mutate == "skip-public-reset":
    current_retire = current_retire.replace("PublicExecutionOrderRuntime.Reset();", "// mutation: skip public reset", 1)
elif args.mutate == "drop-public-unsubscribe":
    current_clear = current_clear.replace("if (_manager != null && _handler != null) _manager.ConversationEndOneShot -= _handler;", "// mutation: retain subscribed handler", 1)
current_template = r'''using System;
using System.Collections.Generic;
namespace TaleWorlds.CampaignSystem {
public sealed class ConversationManager {
 public event Action ConversationEndOneShot;
 public void End() => ConversationEndOneShot?.Invoke();
 public int Subscribers => ConversationEndOneShot?.GetInvocationList().Length ?? 0;
}}
namespace AnimusForge {
internal static class PublicExecutionOrderRuntime {
 private static TaleWorlds.CampaignSystem.ConversationManager _manager;
 private static Action _handler;
 private static readonly object Gate = new();
 private static readonly Dictionary<string, object> Permits = new();
 @@CURRENT_CLEAR@@
 @@CURRENT_RESET@@
 internal static void Arm(TaleWorlds.CampaignSystem.ConversationManager manager, Action callback) {
  _manager = manager; _handler = callback; manager.ConversationEndOneShot += callback; Permits["pending"] = new();
 }
 internal static bool Empty => _manager == null && _handler == null && Permits.Count == 0;
}
public sealed class PendingOperationRegistry { public void Seal() { } }
public partial class ShoutBehavior {
 private readonly PendingOperationRegistry _pendingMainThreadFunctions = new();
 private void RetireChannelRequestLifetimes() { }
 private void ResetInstanceTransientRuntimeForLoadedSave(string reason) { }
 private void CloseNativeConversationInput(bool clearSessionHistory) { }
 @@CURRENT_RETIRE@@
 static void Require(bool ok, string name) { if (!ok) throw new Exception("FAIL " + name); }
 public static void Main() {
  var owner = new ShoutBehavior(); var oldManager = new TaleWorlds.CampaignSystem.ConversationManager(); int oldEffects = 0;
  PublicExecutionOrderRuntime.Arm(oldManager, () => oldEffects++);
  Require(oldManager.Subscribers == 1, "current armed manager");
  owner.RetireCampaignRuntime("campaign_end");
  Require(oldManager.Subscribers == 0 && PublicExecutionOrderRuntime.Empty, "current reset unsubscribes and clears permits");
  oldManager.End(); Require(oldEffects == 0, "current retired callback rejected");
  var nextManager = new TaleWorlds.CampaignSystem.ConversationManager(); int nextEffects = 0;
  PublicExecutionOrderRuntime.Arm(nextManager, () => nextEffects++);
  nextManager.End(); Require(nextEffects == 1 && oldEffects == 0, "current new manager rearm");
  owner.RetireCampaignRuntime("campaign_end");
  Require(nextManager.Subscribers == 0 && PublicExecutionOrderRuntime.Empty, "current new manager cleanup");
  Console.WriteLine("PASS current PublicExecution reset/unsubscribe/permits/rearm campaign proof");
 }
}}
'''
current_code = current_template.replace("@@CURRENT_CLEAR@@", current_clear).replace("@@CURRENT_RESET@@", current_reset).replace("@@CURRENT_RETIRE@@", current_retire)
assert "@@" not in current_code
out = run_root / "current-public-reset"
out.mkdir()
(out / "Program.cs").write_text(current_code, encoding="utf-8")
(out / "Tests.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear/></packageSources></configuration>", encoding="utf-8")
env = minimal_test_environment(dotnet, out)
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Tests.csproj"), "-c", "Release"], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=90)
log = result.stdout + result.stderr
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(result.returncode)
