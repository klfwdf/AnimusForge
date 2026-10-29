"""Production shared layout + actual native managed widgets; rendering is not exercised."""
from pathlib import Path
import argparse
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet

parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, "hero-action-bar", args.run_root)
dotnet = resolve_dotnet(ROOT)
sdk = next((dotnet.parent / "sdk").glob("*/Roslyn/bincore/csc.dll"))
framework = Path("C:/Windows/Microsoft.NET/Framework64/v4.0.30319")
netstandard = next(Path("C:/Windows/Microsoft.NET/assembly/GAC_MSIL/netstandard").rglob("netstandard.dll"))
helper = ROOT / "src/AF.GameAdapter.Bannerlord/UI/Common/HeroEncyclopediaActionBar.cs"
for api in ("1.3", "1.4"):
    refs = ROOT / ".tmp/build_check" / api
    exe = out / ("Probe-" + api + ".exe")
    command = [str(dotnet), str(sdk), "/nologo", "/noconfig", "/nostdlib+", "/platform:x64", "/out:" + str(exe), str(helper), str(Path(__file__).with_name("Program.cs"))]
    libraries = [framework / (name + ".dll") for name in ("mscorlib", "System", "System.Core")]
    libraries += [netstandard] + [refs / (name + ".dll") for name in ("TaleWorlds.GauntletUI", "TaleWorlds.TwoDimension", "TaleWorlds.Library", "TaleWorlds.Localization", "TaleWorlds.InputSystem", "System.Numerics.Vectors")]
    command += ["/reference:" + str(path) for path in libraries]
    build = subprocess.run(command, capture_output=True)
    (out / ("compile-" + api + ".log")).write_bytes(build.stdout + build.stderr)
    if build.returncode:
        print((build.stdout + build.stderr).decode("utf-8", errors="replace")); raise SystemExit(build.returncode)
    result = subprocess.run([str(exe), str(refs)], capture_output=True)
    (out / ("run-" + api + ".log")).write_bytes(result.stdout + result.stderr)
    print(api, (result.stdout + result.stderr).decode("utf-8", errors="replace"))
    if result.returncode: raise SystemExit(result.returncode)
