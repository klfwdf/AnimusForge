"""Execute the actual J06 MyBehavior Knowledge phase methods with detached fake ports."""
from __future__ import annotations
import argparse
import importlib.util
import os
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (ROOT / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs").read_text(encoding="utf-8-sig")
parser = argparse.ArgumentParser()
parser.add_argument("--run-root",type=Path)
parser.add_argument("--mutate", choices=["no-exclusion-copy", "no-eligibility", "no-result-publish"])
args = parser.parse_args()
markers = (
    "internal static PromptKnowledgeWorkInput CreateSharedKnowledgeWorkInput(",
    "internal static PromptKnowledgeWorkResult RunSharedKnowledgeRetrieval(",
    "internal static void ApplySharedKnowledgeRetrieval(",
)
wrappers = [extract.declaration(source, marker) for marker in markers]
actual_source = (ROOT / "src/AF.GameAdapter.Bannerlord/Prompt/SharedPromptCaptureBannerlordAdapter.cs").read_text(encoding="utf-8-sig")
methods = [extract.declaration(actual_source, marker) for marker in (
    "internal static PromptKnowledgeWorkInput CreateSharedKnowledgeWorkInput(",
    "internal static PromptKnowledgeWorkResult RunSharedKnowledgeRetrieval(",
    "internal static void ApplySharedKnowledgeRetrieval(",
)]
if args.mutate == "no-exclusion-copy":
    assert methods[0].count('new HashSet<string>(phases.Request.ExcludedRuleIds, StringComparer.OrdinalIgnoreCase)') == 1
    methods[0] = methods[0].replace("new HashSet<string>(phases.Request.ExcludedRuleIds, StringComparer.OrdinalIgnoreCase)", "phases.Request.ExcludedRuleIds", 1)
if args.mutate == "no-eligibility":
    assert methods[1].count('AIConfigHandler.ApplyGuardrailRuntimeTarget(input.Target, input.Eligibility);') == 1
    methods[1] = methods[1].replace("AIConfigHandler.ApplyGuardrailRuntimeTarget(input.Target, input.Eligibility);", "", 1)
if args.mutate == "no-result-publish":
    assert methods[2].count('phases.Retrieval.EntityMatches = result.EntityMatches;') == 1
    methods[2] = methods[2].replace("phases.Retrieval.EntityMatches = result.EntityMatches;", "", 1)
import sys,json,hashlib
sys.path.insert(0,str(ROOT/"tests"))
from output_isolation import new_run_root,minimal_test_environment
out = new_run_root(ROOT,"prompt-knowledge-phases",args.run_root)
(out / "Production.cs").write_text("using System;\nusing System.Collections.Generic;\nusing AnimusForge;\nusing AnimusForge.Refactor.Adapters;\nnamespace AnimusForge { internal partial class MyBehavior {\n" + "\n".join(wrappers) + "\n}}\nnamespace AnimusForge.Refactor.Adapters {internal static class SharedPromptCaptureBannerlordAdapter {\n"+"\n".join(methods)+"\n}}",encoding="utf-8")
shutil.copy(HERE / "Program.cs", out / "Program.cs")
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="Production.cs"/><Compile Include="' + str(ROOT / "src/modules/AF.Module.Prompt/Composition/PromptRetrievalCapture.cs") + '" Link="PromptRetrievalCapture.cs"/></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>", encoding="utf-8")
dotnet = Path(os.environ.get("AF_DOTNET") or ROOT / "local/dotnet/8.0.425/dotnet.exe")
env = minimal_test_environment(dotnet,out)
r = subprocess.run([str(dotnet), "run", "--project", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
(out/"run.log").write_text(r.stdout+r.stderr,encoding="utf-8")
(out/"receipt.json").write_text(json.dumps({"exitCode":r.returncode,"layer":"three current full Knowledge owner declarations + exact actual My forwarding; controlled detached retrieval leaves","sourceRawSha256":hashlib.sha256((ROOT/"src/AF.GameAdapter.Bannerlord/Prompt/SharedPromptCaptureBannerlordAdapter.cs").read_bytes()).hexdigest(),"mutation":args.mutate},indent=2),encoding="utf-8")
print(r.stdout, end="")
print(r.stderr, end="")
raise SystemExit(r.returncode)
