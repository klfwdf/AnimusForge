"""Compile the production evaluation entry with per-call deterministic ports."""
from __future__ import annotations

import importlib.util
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (ROOT / "AIConfigHandler.cs").read_text(encoding="utf-8-sig")
session_source = (ROOT / "MyBehavior.cs").read_text(encoding="utf-8-sig")
mission_source = (ROOT / "ShoutBehavior.cs").read_text(encoding="utf-8-sig")
assert session_source.count('AIConfigHandler.TryStartBackgroundSemanticWarmup("session_launch")') == 1
mission_seed = 'PromptSemanticWarmupSeedBatch semanticWarmupSeeds = AIConfigHandler.CaptureGuardrailSemanticWarmupSeeds();'
mission_rag = 'RagWarmupCoordinator.TryStartBackgroundWarmup("mission_start", semanticWarmupSeeds);'
mission_semantic = 'AIConfigHandler.TryStartBackgroundSemanticWarmup("mission_start", semanticWarmupSeeds);'
assert mission_source.count(mission_seed) == mission_source.count(mission_rag) == mission_source.count(mission_semantic) == 1
assert mission_source.index(mission_seed) < mission_source.index(mission_rag) < mission_source.index(mission_semantic)
ports = extract.declaration(source, "internal sealed class PromptRuleEvaluationPorts")
entry = extract.declaration(source, "private static bool TryGetGuardrailEvalSnapshot(string userText, string secondaryText, out GuardrailEvalSnapshot snapshot, IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions, PromptRuleEvaluationPorts ports = null)")
capture = extract.declaration(source, "internal static PromptSemanticWarmupSeedBatch CaptureGuardrailSemanticWarmupSeeds()")
session_start = extract.declaration(source, "internal static void TryStartBackgroundSemanticWarmup(string source)")
start = extract.declaration(source, "internal static void TryStartBackgroundSemanticWarmup(string source, PromptSemanticWarmupSeedBatch seeds)")
complete = extract.declaration(source, "private static void RunGuardrailSemanticWarmup(string source, PromptSemanticWarmupSeedBatch seeds)")
output = ROOT / "artifacts/tests/prompt-j03-production-evaluation/current"
output.mkdir(parents=True, exist_ok=True)
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
(output / "Program.cs").write_text(template.replace("@@PORTS@@", ports).replace("@@ENTRY@@", entry)
                                  .replace("@@CAPTURE@@", capture).replace("@@SESSION_START@@", session_start)
                                  .replace("@@START@@", start).replace("@@COMPLETE@@", complete), encoding="utf-8")
tree = ET.parse(ROOT / "tests/modules/AF.Module.Prompt/Retrieval/PromptCandidateSelectionTests.csproj")
links = []
for item in tree.iter("Compile"):
    relative = item.attrib["Include"].replace("\\", "/")
    if relative in ("Program.cs", "FacadeStubs.cs") or relative.endswith(("PromptListRetrievalService.cs", "RagWarmupCoordinator.cs")):
        continue
    path = (ROOT / "tests/modules/AF.Module.Prompt/Retrieval" / relative).resolve()
    links.append(f'<Compile Include="{path}" Link="{path.name}" />')
rag = ROOT / "RagWarmupCoordinator.cs"
links.append(f'<Compile Include="{rag}" Link="{rag.name}" />')
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" />' + ''.join(links) + '</ItemGroup></Project>'
(output / "Proof.csproj").write_text(project, encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"),
           DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1")
build = subprocess.run([str(dotnet), "build", str(output / "Proof.csproj"), "-c", "Release", "--nologo",
                        "-p:UseAppHost=false", "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")],
                       cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(build.stdout + build.stderr)
if build.returncode:
    raise SystemExit(build.returncode)
run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/Proof.dll")], cwd=ROOT, env=env,
                     capture_output=True, text=True, encoding="utf-8", errors="replace")
print(run.stdout + run.stderr)
raise SystemExit(run.returncode)
