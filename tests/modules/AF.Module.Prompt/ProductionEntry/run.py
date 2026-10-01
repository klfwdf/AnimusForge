"""Compile the actual rule-hit entry and registry methods with deterministic boundaries."""
from __future__ import annotations

import argparse
import importlib.util
import os
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
import sys
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, minimal_test_environment, resolve_dotnet
EXTRACTOR = ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py"
spec = importlib.util.spec_from_file_location("af_extract", EXTRACTOR)
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)

parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
parser.add_argument("--mutate-resource", action="store_true")
parser.add_argument("--mutate-unpin", action="store_true")
args = parser.parse_args()
source = (ROOT / "src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs").read_text(encoding="utf-8-sig")
registry = extractor.declaration(source, "private static Dictionary<string, GuardrailRulePromptConfig> BuildRulePromptRegistry()")
hits = extractor.declaration(source, "private static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions, out MentionedWorldEntities mentionedEntities)")
if args.mutate_unpin:
    old = "using IDisposable configurationScope = _promptConfiguration.BeginCapture();"
    assert old in hits
    hits = hits.replace(old, "using IDisposable configurationScope = new NoopScope();", 1)
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
output = new_run_root(ROOT, "prompt-j03-production-entry", args.run_root)
(output / "Program.cs").write_text(template.replace("@@REGISTRY@@", registry).replace("@@HITS@@", hits), encoding="utf-8")
(output / "Proof.csproj").write_text("""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>
<Compile Include="Program.cs" />
<Compile Include="../../../../tests/modules/AF.Module.Prompt/Configuration/Stubs.cs" Link="Stubs.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/PromptConfigurationSnapshot.cs" Link="PromptConfigurationSnapshot.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/RevisionedPromptConfigurationStore.cs" Link="RevisionedPromptConfigurationStore.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/PromptRevisionedDerivedCache.cs" Link="PromptRevisionedDerivedCache.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/PromptRuleRegistry.cs" Link="PromptRuleRegistry.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/ExecutionPromptConfiguration.cs" Link="ExecutionPromptConfiguration.cs" />
<EmbeddedResource Include="../../../../content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts.json" LogicalName="AnimusForge.Defaults.ExecutionRulePrompts.json" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Retrieval/GuardrailRuleHit.cs" Link="GuardrailRuleHit.cs" />
<Reference Include="Newtonsoft.Json"><HintPath>../../../../local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll</HintPath></Reference>
</ItemGroup></Project>""".replace("../../../../", ROOT.as_posix() + "/").replace("AnimusForge.Defaults.ExecutionRulePrompts.json", "Wrong.ExecutionRulePrompts.json" if args.mutate_resource else "AnimusForge.Defaults.ExecutionRulePrompts.json"), encoding="utf-8")
(output / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = resolve_dotnet(ROOT)
env = minimal_test_environment(dotnet, output)
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
