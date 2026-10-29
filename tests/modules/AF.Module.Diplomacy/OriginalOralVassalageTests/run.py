"""Replay source-derived oral entry, rule binding, normalization and reward dispatch.

Game effect methods are recording ports. Creation/revision bodies are checked
unchanged from the restoration baseline; this is not Campaign/game acceptance.
"""
from pathlib import Path
import argparse, hashlib, importlib.util, json, subprocess, sys
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    out = new_run_root(ROOT, "original-oral-vassalage", args.out)
    spec = importlib.util.spec_from_file_location("extract",
        ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
    extract = importlib.util.module_from_spec(spec); spec.loader.exec_module(extract)
    source_path = "src/modules/AF.Module.Diplomacy/Vassalage/VassalageBehavior.cs"
    source = (ROOT / source_path).read_text("utf-8-sig")
    baseline = subprocess.check_output(["git", "show", "466607853:" + source_path], cwd=ROOT).decode("utf-8-sig")
    for method in ("private bool TryCreatePlayerVassalage(", "private bool TryRevisePlayerVassalage("):
        assert extract.declaration(source, method) == extract.declaration(baseline, method), method + " unexpectedly rewritten"
    old = subprocess.check_output(["git", "show", "c8e687489^:" + source_path], cwd=ROOT).decode("utf-8-sig")
    gate = "private static bool TryBuildVassalageRuntimeState("
    assert extract.declaration(source, gate) == extract.declaration(old, gate), "Original qualification changed"
    methods = "\n".join(extract.declaration(source, m) for m in (
        "public bool TryApplyVassalageAction(",
        "public static bool CanInjectVassalageRuleForExternal(",
        "internal static bool CanInjectVassalageRuleForPromptCapture(",
        "public static List<PostprocessRuleEntry> BuildRuntimeVassalagePostprocessRulesForExternal(",
        gate))
    owner = (ROOT / "src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs").read_text("utf-8-sig")
    normalization = "\n".join(extract.declaration(owner, m) for m in (
        "internal static string NormalizeVassalagePostprocessTagsForScene(",
        "internal static string NormalizeDiplomacyPostprocessTagsForScene(",
        "internal static string ExtractDiplomacyPostprocessActionName("))
    program = (HERE / "Harness.cs.txt").read_text("utf-8").replace("@@METHODS@@", methods).replace("@@NORMALIZATION@@", normalization)
    (out / "Program.cs").write_text(program, encoding="utf-8")
    political = ROOT / "src/modules/AF.Module.Diplomacy/Application/DiplomacyPoliticalRewardApplication.cs"
    (out / "Test.csproj").write_text(f"""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>
<Compile Include="Program.cs"/><Compile Include="{political.as_posix()}"/></ItemGroup></Project>""", encoding="utf-8")
    dotnet = resolve_dotnet(ROOT); env = minimal_test_environment(dotnet, out)
    run = subprocess.run([str(dotnet), "run", "--project", str(out / "Test.csproj"), "--", str(ROOT)],
                         cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
    (out / "run.log").write_text(run.stdout + run.stderr, encoding="utf-8")
    (out / "receipt.json").write_text(json.dumps({
        "methods_sha256": hashlib.sha256((methods + normalization).encode()).hexdigest(),
        "original_source": "c8e687489^", "unchanged_mechanism_baseline": "466607853",
        "game_and_provider": "NOT_RUN", "exit_code": run.returncode
    }, indent=2), encoding="utf-8")
    print(run.stdout + run.stderr)
    return run.returncode

if __name__ == "__main__":
    raise SystemExit(main())
