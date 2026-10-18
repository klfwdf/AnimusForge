"""Exercise the complete current speech-output method at the controlled TTS leaf.

This is not native playback. Exact request lifetime is covered by the existing
TtsRequestLifetimeRegressionTests; primary identity is covered by VoiceTests.
"""
from pathlib import Path
import argparse, hashlib, importlib.util, json, subprocess, sys
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment

p = argparse.ArgumentParser()
p.add_argument("--dotnet", required=True)
a = p.parse_args()
out = new_run_root(ROOT, "GptSoVitsScenePrimaryTests", None)
out.mkdir(parents=True, exist_ok=True)
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
ex = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ex)
source = ROOT / "src/AF.GameAdapter.Bannerlord/Scene/ScenePresentationController.cs"
raw = source.read_text(encoding="utf-8-sig")
method = ex.declaration(raw, "internal SceneSpeechPlaybackInfo ShowNpcSpeechOutput(")
port = ex.declaration(raw, "internal sealed class SceneSpeechOutputPort")
harness = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
harness = harness.replace("@@METHOD@@", method).replace("@@PORT@@", port)
assert "@@" not in harness
(out / "Program.cs").write_text(harness, encoding="utf-8")
(out / "Tests.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>')
digest = hashlib.sha256(source.read_bytes()).hexdigest()
r = subprocess.run([a.dotnet, "run", "--project", str(out / "Tests.csproj")], cwd=out,
    env=minimal_test_environment(Path(a.dotnet), out), capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=90)
(out / "run.log").write_text(r.stdout+r.stderr, encoding="utf-8")
assert hashlib.sha256(source.read_bytes()).hexdigest() == digest
(out / "source.json").write_text(json.dumps({"source":str(source),"sha256":digest,"scope":"exact whole ShowNpcSpeechOutput and exact port; game/audio/network leaves controlled"}, indent=2), encoding="utf-8")
print(r.stdout+r.stderr)
raise SystemExit(r.returncode)
