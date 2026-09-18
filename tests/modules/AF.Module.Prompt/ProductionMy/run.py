"""Execute the production My settlement candidate consumer with the real facade."""
from __future__ import annotations

import importlib.util
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("extract", ROOT / "tools/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = (ROOT / "MyBehavior.cs").read_text(encoding="utf-8-sig")
method = extract.declaration(source, "public static string BuildSettlementTransferRuntimeInstructionForExternal(")
stubs = (ROOT / "tests/modules/AF.Module.Prompt/Retrieval/FacadeStubs.cs").read_text(encoding="utf-8")
for before, after in (
    ("public static class MyBehavior", "public static partial class MyBehavior"),
    ("public static class RewardSystemBehavior", "public partial class RewardSystemBehavior"),
    ("public sealed class Hero { public string StringId; }", "public sealed class Hero { public string StringId; public string Name; public CharacterObject CharacterObject; public Clan Clan; }"),
    ("public sealed class Clan { public string Name, StringId; }", "public sealed class Clan { public string Name, StringId; public Hero Leader; }"),
    ("public sealed class SettlementTransferPromptEntry\n        {", "public sealed class SettlementTransferPromptEntry\n        {\n            public SettlementTransferEntrySection Section;"),
    ("public enum SettlementTransferAssetKind", "public enum SettlementTransferEntrySection { NpcFiefs, PlayerFiefs }\n        public enum SettlementTransferAssetKind"),
    ("internal static class AIConfigHandler", "internal static partial class AIConfigHandler"),
):
    assert stubs.count(before) == 1, before
    stubs = stubs.replace(before, after)
output = ROOT / "artifacts/tests/prompt-j03-production-my/current"
output.mkdir(parents=True, exist_ok=True)
(output / "Stubs.cs").write_text(stubs, encoding="utf-8")
(output / "Program.cs").write_text((HERE / "Harness.cs.txt").read_text(encoding="utf-8").replace("@@METHOD@@", method), encoding="utf-8")
links = [ROOT / "PromptListRetrievalService.cs",
         ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptCandidateSelection.cs",
         ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptCandidateSnapshotIndex.cs",
         ROOT / "src/modules/AF.Module.Prompt/Retrieval/PromptSemanticWarmupSeedBatch.cs"]
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" /><Compile Include="Stubs.cs" />' + ''.join(f'<Compile Include="{path}" Link="{path.name}" />' for path in links) + '</ItemGroup></Project>'
(output / "Proof.csproj").write_text(project, encoding="utf-8")
(output / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
env = dict(os.environ, DOTNET_ROOT=str(dotnet.parent), DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"), DOTNET_CLI_TELEMETRY_OPTOUT="1")
build = subprocess.run([str(dotnet), "build", str(output / "Proof.csproj"), "-c", "Release", "--nologo", "-p:UseAppHost=false", "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(output / "NuGet.Config")],
                       cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(build.stdout + build.stderr)
if build.returncode:
    raise SystemExit(build.returncode)
run = subprocess.run([str(dotnet), str(output / "bin/Release/net8.0/Proof.dll")], cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
print(run.stdout + run.stderr)
raise SystemExit(run.returncode)
