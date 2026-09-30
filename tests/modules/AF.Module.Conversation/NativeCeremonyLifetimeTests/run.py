"""Execute the production Native ceremony unsubscription through campaign retirement."""
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

presentation = (ROOT / "ShoutBehavior.NativeTurnPresentation.cs").read_text(encoding="utf-8-sig")
lifetime = (ROOT / "ShoutBehavior.CampaignLifetime.cs").read_text(encoding="utf-8-sig")
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
parser.add_argument("--mutate", choices=["skip-clear", "drop-owner-guard"])
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
if args.mutate == "skip-clear":
    retire = retire.replace(call, "// mutation: skip clear", 1)
elif args.mutate == "drop-owner-guard":
    clear_owner = clear_owner.replace("if (!ReferenceEquals(_ceremonyOrderOwner, owner)) return;", "// mutation: drop owner guard", 1)

dotnet = resolve_dotnet(ROOT)
out = new_run_root(ROOT, "native-ceremony-lifetime", args.run_root)

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
raise SystemExit(result.returncode)
