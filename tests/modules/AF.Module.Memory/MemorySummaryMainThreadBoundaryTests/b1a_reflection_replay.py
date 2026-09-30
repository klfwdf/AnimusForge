"""Run only the three B1a replay sources against an explicit fresh candidate DLL."""
import argparse
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
SOURCES = ROOT / "tests/replay/ProductionOptInEntryReplayTests"
parser = argparse.ArgumentParser()
parser.add_argument("--candidate", type=Path, required=True)
parser.add_argument("--out", type=Path, required=True)
args = parser.parse_args()
candidate = args.candidate.resolve()
assert candidate.is_file() and candidate.name == "AnimusForge.dll"
assert candidate.is_relative_to(ROOT / "artifacts/tests/j17-unified-build"), "only isolated build candidates"
out = args.out.resolve()
assert not out.exists() and out.is_relative_to(ROOT / "artifacts/j17b/session-20260930/b1a-tests")
out.mkdir(parents=True)
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
links = "".join('<Compile Include="' + str(SOURCES / name).replace("\\", "/") + '" Link="' + name + '" />' for name in (
    "MemoryOwnerReadbackReplay.cs", "MemoryRecoveryProductionReplay.cs", "WeeklyActionOutcomeProductionReplay.cs"))
links += '<Compile Include="' + str(Path(__file__).with_name("B1aSanitizerReplay.cs")).replace("\\", "/") + '" Link="B1aSanitizerReplay.cs" />'
(out / "Replay.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs" />' + links + '</ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
program = r'''using System;
using System.IO;
using System.Reflection;
class Program {
  static int Main(string[] args) {
    string candidate = Path.GetFullPath(args[0]);
    string[] roots = { Path.GetDirectoryName(candidate), args[1], args[2], args[3], args[4], args[5], args[6], args[7] };
    AppDomain.CurrentDomain.AssemblyResolve += (_, request) => {
      string filename = new AssemblyName(request.Name).Name + ".dll";
      foreach (string root in roots) {
        string path = Path.Combine(root, filename);
        if (File.Exists(path)) return Assembly.LoadFrom(path);
      }
      return null;
    };
    Assembly production = Assembly.LoadFrom(candidate);
    MemoryOwnerReadbackReplay.Run(production);
    MemoryRecoveryProductionReplay.Run(production);
    WeeklyActionOutcomeProductionReplay.Run(production);
    B1aSanitizerReplay.Run(production);
    Console.WriteLine("PASS B1a reflection replay 4/4");
    return 0;
  }
}
'''
(out / "Program.cs").write_text(program, encoding="utf-8")
temp = Path("E:/tmp/af-j17-20260930"); temp.mkdir(parents=True, exist_ok=True)
synthetic = temp / ("b1a-" + out.name)
assert not synthetic.exists(), "synthetic data root must be new"
synthetic.mkdir()
home = out / "home"; home.mkdir()
os.environ.update({"TEMP": str(temp), "TMP": str(temp), "APPDATA": str(home), "LOCALAPPDATA": str(home), "USERPROFILE": str(home), "DOTNET_ROOT": str(dotnet.parent), "DOTNET_CLI_HOME": str(home), "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1", "ANIMUSFORGE_DATA_ROOT": str(synthetic)})
build = subprocess.run([str(dotnet), "build", str(out / "Replay.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, capture_output=True, text=True, encoding="utf-8", errors="replace")
(out / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode: print((build.stdout + build.stderr)[-3000:]); raise SystemExit(build.returncode)
game = Path("D:/steam/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client")
refs = ROOT / "local/bannerlord-refs/1.4.7.117484"
harmony = Path("D:/steam/steamapps/common/Mount & Blade II Bannerlord/Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client")
mcm = Path("D:/steam/steamapps/common/Mount & Blade II Bannerlord/Modules/Bannerlord.MBOptionScreen/bin/Win64_Shipping_Client")
ui = Path("D:/steam/steamapps/common/Mount & Blade II Bannerlord/Modules/Bannerlord.UIExtenderEx/bin/Win64_Shipping_Client")
net8deps = ROOT / "local/dotnet/8.0.425/sdk/8.0.425/DotnetTools/dotnet-format"
run = subprocess.run([str(dotnet), str(out / "bin/Release/net8.0/Replay.dll"), str(candidate), str(ROOT / "_deps_auto"), str(game), str(refs), str(harmony), str(mcm), str(ui), str(net8deps)], cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
(out / "run.log").write_text(run.stdout + run.stderr, encoding="utf-8")
print((run.stdout + run.stderr)[-3000:])
raise SystemExit(run.returncode)
