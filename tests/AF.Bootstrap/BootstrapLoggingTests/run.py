"""Exercise production Bootstrap logging and fatal formatting with a fake engine debug sink."""
from pathlib import Path
import argparse
import importlib.util
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--dotnet", type=Path, required=True)
parser.add_argument("--sdk-version", default="8.0.421")
args = parser.parse_args()
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (ROOT / "src/AF.Bootstrap/BootstrapRuntime.cs").read_text(encoding="utf-8-sig")
messages = "\n".join(extract.declaration(source, signature) for signature in (
    "private string BuildFatalMessage(", "private string BuildFatalDialogMessage(",
    "private static Exception GetInnermostException(", "internal void ReportFatal("))
output = new_run_root(ROOT, "bootstrap-logging", None)
(output / "global.json").write_text(json.dumps({"sdk": {"version": args.sdk_version, "rollForward": "disable"}}))
(output / "BootstrapLog.cs").write_bytes((ROOT / "src/AF.Bootstrap/BootstrapLog.cs").read_bytes())
logger = (ROOT / "src/AF.Bootstrap/BootstrapLog.cs").read_text(encoding="utf-8-sig")
initialize = extract.declaration(logger, "internal static void Initialize(")
(output / "Program.cs").write_text((HERE / "Harness.cs.txt").read_text().replace("@@MESSAGES@@", messages).replace("@@INITIALIZE@@", initialize), encoding="utf-8")
(output / "Tests.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
(output / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet = args.dotnet.resolve()
result = subprocess.run([str(dotnet), "run", "--project", str(output / "Tests.csproj")], cwd=output,
    env=minimal_test_environment(dotnet, output), capture_output=True, text=True, timeout=120)
(output / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
print(result.stdout + result.stderr)
print(output)
sys.exit(result.returncode)
