"""Compile the unchanged production shared completion and section capture from both revisions."""
from __future__ import annotations

import importlib.util
import argparse
import base64
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument("--mutate", choices=["drop-lore", "drop-entity", "drop-rule"])
args = parser.parse_args()
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_NOLOGO="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
composition = [
    "PromptTopicRoutingStage.cs", "PromptBuildRequest.cs", "PromptContextDecisions.cs",
    "PromptAssemblyStage.cs", "PromptRetrievalCapture.cs", "PromptRuleIdPolicy.cs",
    "PromptPreprocessRuleIdAssembler.cs", "PromptExtrasComposer.cs", "PromptRuleBlockText.cs",
    "PromptRuntimeTargetBinding.cs", "PromptRuleEligibility.cs", "BuiltInRuleStickyCarry.cs",
    "PromptBuiltInTopicRouter.cs", "PromptRuleInstructionComposer.cs",
]
markers = (
    "internal ShoutPromptContext CompleteSharedPromptBuild(",
    "private void CapturePromptSections(",
    "private void ApplyPromptRuntimeAppendices(",
)
for revision in ("old", "current"):
    out = ROOT / "artifacts/tests/j06-shared-completion" / revision
    out.mkdir(parents=True, exist_ok=True)
    def read(path: str) -> str:
        if revision == "old":
            return subprocess.check_output(["git", "show", "77a3d234:" + path], cwd=ROOT).decode("utf-8-sig")
        return (ROOT / path).read_text(encoding="utf-8-sig")
    source = read("MyBehavior.cs")
    methods = "\n".join(extract.declaration(source, marker) for marker in markers)
    if revision == "current" and args.mutate:
        needles = {
            "drop-lore": ("extrasSections.LoreContext = loreContext;", "extrasSections.LoreContext = \"\";"),
            "drop-entity": ("MainPromptBlock = entityPromptContext?.MainPromptBlock,", "MainPromptBlock = \"\","),
            "drop-rule": ("extrasSections.TriggeredRuleInstructions = value8;", "extrasSections.TriggeredRuleInstructions = \"\";"),
        }
        before, after = needles[args.mutate]
        assert methods.count(before) == 1, "mutation anchor drift: " + args.mutate
        methods = methods.replace(before, after, 1)
    (out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\nusing System.Linq;\nusing System.Threading;\nnamespace AnimusForge { public partial class MyBehavior {\n" + methods + "\n}}\n", encoding="utf-8")
    for filename in composition:
        (out / filename).write_text(read("src/modules/AF.Module.Prompt/Composition/" + filename), encoding="utf-8")
    for filename in ("GuardrailRuleHit.cs", "PreprocessFormatException.cs"):
        (out / filename).write_text(read(filename), encoding="utf-8")
    (out / "Program.cs").write_bytes((HERE / "Program.cs").read_bytes())
    files = ["Program.cs", "Production.cs", "GuardrailRuleHit.cs", "PreprocessFormatException.cs"] + composition
    (out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit>' + ('<DefineConstants>CURRENT</DefineConstants>' if revision == 'current' else '') + '</PropertyGroup><ItemGroup>' + ''.join(f'<Compile Include="{name}" />' for name in files) + '</ItemGroup></Project>', encoding="utf-8")
    (out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
    result = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        print(revision, result.stdout, result.stderr, sep="\n")
        raise SystemExit(result.returncode)
    print("BUILD", revision, "production CompleteSharedPromptBuild/CapturePromptSections")

def component(path: str) -> dict:
    result = subprocess.run(["python", str(ROOT / path), "--emit-json"], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        print(result.stdout, result.stderr, sep="\n")
        raise SystemExit(result.returncode)
    lines = [line[len("EXPORT_JSON="):] for line in result.stdout.splitlines() if line.startswith("EXPORT_JSON=")]
    assert len(lines) == 1, "missing production component export: " + path
    return json.loads(lines[0])

lore = component("tests/modules/AF.Module.Knowledge/LoreTextDifferential/run.py")
entity = component("tests/modules/AF.Module.Knowledge/EntityTextDifferential/run.py")
rule = component("tests/modules/AF.Module.Prompt/ExtraRuleTextDifferential/run.py")
courier_build = subprocess.run(["python", str(ROOT / "tools/CourierPromptPreparationTests/run.py")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
if courier_build.returncode:
    print(courier_build.stdout, courier_build.stderr, sep="\n")
    raise SystemExit(courier_build.returncode)
courier_dll = ROOT / "tools/CourierPromptPreparationTests/.generated/current/bin/Release/net8.0/CourierPromptChecks.dll"
assert courier_dll.exists(), "Courier production final request runner missing"
native_build = subprocess.run(["python", str(ROOT / "tests/modules/AF.Module.Prompt/NativeFinalRequestDifferential/run.py")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
if native_build.returncode:
    print(native_build.stdout, native_build.stderr, sep="\n")
    raise SystemExit(native_build.returncode)
checked = 0
for lore_case in ("hit", "stale"):
    for entity_case in ("direct", "title", "fallback"):
        for rule_case in ("semantic", "lexical"):
            outputs = {}
            requests = {}
            native_requests = {}
            for revision in ("old", "current"):
                component_env = dict(env)
                component_env.update({
                    "AF_J06_LORE": lore[revision]["FALLBACK" if revision == "current" and lore_case == "stale" else "RESULT"],
                    "AF_J06_ENTITY_MAIN": entity[revision]["RESULT_" + ("title" if entity_case == "title" else "direct") + "_main"],
                    "AF_J06_ENTITY_POST": entity[revision]["RESULT_" + ("title" if entity_case == "title" else "direct") + "_post"],
                    "AF_J06_RULE": rule[revision]["RESULT_" + rule_case],
                    "AF_J06_PRESELECTED": "1" if rule_case == "semantic" else "0",
                    "AF_J06_CAPTURE_FAIL": "1" if entity_case == "fallback" else "0",
                })
                if revision == "current" and args.mutate:
                    component_env["AF_J06_ALLOW_LOSS"] = "1"
                dll = ROOT / "artifacts/tests/j06-shared-completion" / revision / "bin/Release/net8.0/Proof.dll"
                result = subprocess.run([str(dotnet), str(dll)], cwd=ROOT, env=component_env, capture_output=True, text=True, encoding="utf-8", errors="replace")
                if result.returncode:
                    print(revision, lore_case, entity_case, rule_case, result.stdout, result.stderr, sep="\n")
                    raise SystemExit(result.returncode)
                lines = [line[len("CONTEXT="):] for line in result.stdout.splitlines() if line.startswith("CONTEXT=")]
                assert len(lines) == 1, "shared completion omitted context"
                outputs[revision] = base64.b64decode(lines[0], validate=True)
                courier_env = dict(component_env, AF_J06_PRODUCTION_CONTEXT=lines[0])
                courier = subprocess.run([str(dotnet), str(courier_dll)], cwd=ROOT, env=courier_env, capture_output=True, text=True, encoding="utf-8", errors="replace")
                if courier.returncode:
                    print("Courier", revision, lore_case, entity_case, rule_case, courier.stdout, courier.stderr, sep="\n")
                    raise SystemExit(courier.returncode)
                requests[revision] = {label: base64.b64decode(encoded, validate=True) for label, encoded in
                    (line.split("=", 1) for line in courier.stdout.splitlines() if line.startswith("REQUEST_"))}
                assert set(requests[revision]) == {"REQUEST_outbound", "REQUEST_inbound"}, "Courier directions missing"
                native_dll = ROOT / "artifacts/tests/j06-native-final" / revision / "bin/Release/net8.0/Proof.dll"
                native = subprocess.run([str(dotnet), str(native_dll)], cwd=ROOT, env=courier_env, capture_output=True, text=True, encoding="utf-8", errors="replace")
                if native.returncode:
                    print("Native", revision, lore_case, entity_case, rule_case, native.stdout, native.stderr, sep="\n")
                    raise SystemExit(native.returncode)
                native_lines = [line[len("REQUEST="):] for line in native.stdout.splitlines() if line.startswith("REQUEST=")]
                assert len(native_lines) == 1, "Native final request missing"
                native_requests[revision] = base64.b64decode(native_lines[0], validate=True)
            if args.mutate:
                assert requests["old"] != requests["current"], "Courier final request failed to reject " + args.mutate
                assert native_requests["old"] != native_requests["current"], "Native final messages failed to reject " + args.mutate
                print("EXPECTED_REJECT", args.mutate, "Courier and Native final requests both differ")
                raise SystemExit(1)
            assert requests["old"] == requests["current"], "Courier final requests differ: " + "/".join((lore_case, entity_case, rule_case))
            assert native_requests["old"] == native_requests["current"], "Native final messages differ: " + "/".join((lore_case, entity_case, rule_case))
            assert outputs["old"] == outputs["current"], "shared prompt differs: " + "/".join((lore_case, entity_case, rule_case))
            context = json.loads(outputs["current"])
            assert "Praven is a port city." in context["Extras"] and "RULE_TEXT" in context["Extras"] and "Alda" in context["Extras"]
            assert "npc_1" in context["EntityPostprocessContext"] and "trade_context" in context["Extras"]
            if rule_case == "semantic":
                assert "trade_context" in context["PreprocessRuleIds"], "preselected rule id absent"
            checked += 1
print("PASS production shared completion + Courier full requests + Native final messages with production-derived text scenarios=" + str(checked))
