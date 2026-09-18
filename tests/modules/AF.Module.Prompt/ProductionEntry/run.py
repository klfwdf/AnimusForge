"""Compile the actual rule-hit entry and registry methods with deterministic boundaries."""
from __future__ import annotations

import argparse
import importlib.util
import os
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
EXTRACTOR = ROOT / "tools/ChannelCutoverBoundaryTests/run.py"
spec = importlib.util.spec_from_file_location("af_extract", EXTRACTOR)
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)

parser = argparse.ArgumentParser()
parser.add_argument("--mutate-unpin", action="store_true")
args = parser.parse_args()
source = (ROOT / "AIConfigHandler.cs").read_text(encoding="utf-8-sig")
registry = extractor.declaration(source, "private static Dictionary<string, GuardrailRulePromptConfig> BuildRulePromptRegistry()")
hits = extractor.declaration(source, "private static List<GuardrailRuleHit> GetGuardrailSemanticRuleHits(string input, string secondaryInput, int maxCount, bool includeBuiltInRules, IEnumerable<string> excludedRuleIds, bool applyRuntimeAutoExclusions, out MentionedWorldEntities mentionedEntities)")
if args.mutate_unpin:
    old = "using IDisposable configurationScope = _promptConfiguration.BeginCapture();"
    assert old in hits
    hits = hits.replace(old, "using IDisposable configurationScope = new NoopScope();", 1)
template = (HERE / "Harness.cs.txt").read_text(encoding="utf-8")
output = ROOT / "artifacts/tests/prompt-j03-production-entry" / ("unpin-red" if args.mutate_unpin else "current")
output.mkdir(parents=True, exist_ok=True)
(output / "Program.cs").write_text(template.replace("@@REGISTRY@@", registry).replace("@@HITS@@", hits), encoding="utf-8")
(output / "Proof.csproj").write_text("""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>
<Compile Include="Program.cs" />
<Compile Include="../../../../tests/modules/AF.Module.Prompt/Configuration/Stubs.cs" Link="Stubs.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/PromptConfigurationSnapshot.cs" Link="PromptConfigurationSnapshot.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/RevisionedPromptConfigurationStore.cs" Link="RevisionedPromptConfigurationStore.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/PromptRevisionedDerivedCache.cs" Link="PromptRevisionedDerivedCache.cs" />
<Compile Include="../../../../src/modules/AF.Module.Prompt/Configuration/PromptRuleRegistry.cs" Link="PromptRuleRegistry.cs" />
<Compile Include="../../../../GuardrailRuleHit.cs" Link="GuardrailRuleHit.cs" />
<Reference Include="Newtonsoft.Json"><HintPath>../../../../local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll</HintPath></Reference>
</ItemGroup></Project>""", encoding="utf-8")
(output / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"),
           DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1", DOTNET_GENERATE_ASPNET_CERTIFICATE="false")
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
