from pathlib import Path
import argparse, os, subprocess, sys
from xml.sax.saxutils import escape
root = Path(__file__).resolve().parents[4]
here = Path(__file__).resolve().parent
sys.path.insert(0, str(root / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser = argparse.ArgumentParser()
parser.add_argument("--dotnet")
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
# This historical owner's test framework remains net10.0; no fallback to net8.
dotnet = resolve_dotnet(root, args.dotnet or os.environ.get("AF_DOTNET10") or r"C:\Program Files\dotnet\dotnet.exe", major=10)
out = new_run_root(root, "j17-import-export", args.run_root)
project = out / "J17ImportExportTests.csproj"
files = [root / "src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs",
         root / "src/modules/AF.Module.Memory/ImportExport/MemoryDeveloperEditOwner.cs", here / "OwnerChecks.cs"]
project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + ''.join('<Compile Include="' + escape(str(f)) + '" />' for f in files) + '</ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
env = minimal_test_environment(dotnet, out)
result = subprocess.run([str(dotnet), "build", str(project), "-c", "Release", "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
log = result.stdout + result.stderr
code = result.returncode
if code == 0:
    result = subprocess.run([str(dotnet), str(out / "bin/Release/net10.0/J17ImportExportTests.dll")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    log += result.stdout + result.stderr
    code = result.returncode
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(code)
