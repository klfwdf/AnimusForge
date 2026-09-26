"""Source-linked V1 API checks: stub composition host, real contracts and runtime."""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import os
from pathlib import Path
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
API_SOURCES = {
    "src/AF.Contracts/PublicApi/V1/AfApiContracts.cs",
    "src/modules/AF.Module.PublicApi/V1/AfApi.cs",
    "src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs",
    "src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs",
    "src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs",
}
SOURCES = ["src/modules/AF.Module.PublicApi/V1/AfApi.cs", "src/AF.Contracts/PublicApi/V1/AfApiContracts.cs",
    "src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs", "src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs", "Refactor/Modules/CoreDialogueContracts.cs",
    "Refactor/Modules/CoreDialogueOperation.cs", "Refactor/Modules/CoreDialogueClient.cs",
    "Refactor/Modules/CoreDialogueServices.cs", "tools/ModuleFrameworkApiTests/NativeOwnerStub.cs",
    "src/AF.Foundation.Runtime/ModuleDirectory/InternalModuleDirectory.cs", "src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs",
    "src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs",
    "Refactor/Contracts/FeatureBridgeContracts.cs", "src/AF.GameAdapter.Bannerlord/Composition/TeamModuleRegistration.cs",
    "src/AF.Foundation.Runtime/ModuleDirectory/ModuleFrameworkSnapshot.cs", "src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs"]
assert API_SOURCES <= set(SOURCES)


def environment(dotnet: str) -> dict[str, str]:
    env = os.environ.copy()
    env.update(DOTNET_ROOT=str(Path(dotnet).parent),
        DOTNET_CLI_HOME=str(ROOT / ".tmp/dotnet-cli"),
        NUGET_PACKAGES=str(ROOT / ".tmp/nuget-packages"),
        DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_GENERATE_ASPNET_CERTIFICATE="false",
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1", DOTNET_NOLOGO="1",
        DOTNET_CLI_UI_LANGUAGE="en", PYTHONIOENCODING="utf-8")
    return env


def project(path: Path, name: str, sources: list[Path], references: list[Path] = (), executable=False):
    path.mkdir(parents=True, exist_ok=True)
    items = [f'<Compile Include="{escape(str(source))}" />' for source in sources]
    items += [f'<ProjectReference Include="{escape(str(reference))}" />' for reference in references]
    xml = f"""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<TargetFramework>net8.0</TargetFramework><AssemblyName>{name}</AssemblyName>
<OutputType>{'Exe' if executable else 'Library'}</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
<ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit>
</PropertyGroup><ItemGroup>{''.join(items)}</ItemGroup></Project>"""
    target = path / (name + ".csproj")
    target.write_text(xml, encoding="utf-8")
    return target


def run_dotnet(dotnet: str, args: list[str], cwd: Path):
    result = subprocess.run([dotnet] + args, cwd=cwd, env=environment(dotnet),
        capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180)
    return result.returncode, result.stdout + result.stderr



def snapshot_mutations(dotnet: str, out: Path):
    mutations = {
        "map_ready_as_degraded": ("src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs", "case ModuleFrameworkLifecycleState.Ready: return AfFrameworkState.Ready;", "case ModuleFrameworkLifecycleState.Ready: return AfFrameworkState.Degraded;"),
        "share_snapshot_container": ("src/AF.Foundation.Runtime/ModuleDirectory/ModuleFrameworkSnapshot.cs", "new ReadOnlyCollection<ModuleBindingSnapshot>(modules.ToArray())", "new ReadOnlyCollection<ModuleBindingSnapshot>((IList<ModuleBindingSnapshot>)modules)"),
        "projection_rereads_live_directory": ("src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs", "in snapshot.Modules)", "in ModuleFrameworkRuntime.CaptureSnapshot().Modules)"),
        "skip_shutdown_forward": ("src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs", "ModuleDirectoryLifecycleOwner.Shutdown();", ";"),
        "force_factory_rebuild": ("src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs", "if (_state != ModuleFrameworkLifecycleState.NotInitialized && _state != ModuleFrameworkLifecycleState.Stopped)", "if (false)")
    }
    for name, (path, before, after) in mutations.items():
        folder = out / name; folder.mkdir(exist_ok=True)
        text = (ROOT / path).read_text(encoding="utf-8-sig")
        assert text.count(before) == 1, "Mutation anchor drift: " + name
        mutated = folder / Path(path).name; mutated.write_text(text.replace(before, after), encoding="utf-8")
        sources = [mutated if p == path else ROOT / p for p in SOURCES]
        library = project(folder / "Library", "ModuleFrameworkUnderTest", sources + [HERE / "HostStubs.cs"])
        control = project(folder / "Control", "ModuleFrameworkControl", [HERE / "HostControl.cs", HERE / "SnapshotBoundaryChecks.cs", out / "OriginalRuntime.cs"], [library])
        client = project(folder / "Client", "ModuleFrameworkExternalClient", [HERE / "ExternalClient.cs"], [library, control], True)
        code, log = run_dotnet(dotnet, ["run", "--project", str(client), "-c", "Release"], out)
        (folder / "run.log").write_text(log, encoding="utf-8")
        assert code != 0 and "FAIL " in log and "error CS" not in log, "Mutation not rejected by runtime assertions: " + name + "\n" + log
        print("PASS snapshot mutation rejected: " + name)


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=r"G:\AFMOD\.dotnet-sdk\dotnet.exe")
    parser.add_argument("--artifact-root", action="append", default=[], help="Optional actual single_module_artifacts directory; repeat for Debug/Release")
    args = parser.parse_args()
    out = HERE / ".generated/current"
    out.mkdir(parents=True, exist_ok=True)
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    spec = importlib.util.spec_from_file_location("snapshot_source_inverse", HERE / "source_boundary.py")
    boundary = importlib.util.module_from_spec(spec); spec.loader.exec_module(boundary); boundary.verify()
    original = boundary.old("Refactor/Modules/ModuleFrameworkRuntime.cs")
    original = original.replace("namespace AnimusForge.Refactor.Modules;", "namespace ModuleFramework.TestControl;\nusing AnimusForge.Refactor.Modules;")
    original = original.replace("internal static class ModuleFrameworkRuntime", "internal static class OriginalFrameworkRuntime")
    (out / "OriginalRuntime.cs").write_text(original, encoding="utf-8")
    library = project(out / "Library", "ModuleFrameworkUnderTest", [ROOT / p for p in SOURCES] + [HERE / "HostStubs.cs"])
    control = project(out / "Control", "ModuleFrameworkControl", [HERE / "HostControl.cs", HERE / "SnapshotBoundaryChecks.cs", out / "OriginalRuntime.cs"], [library])
    client = project(out / "Client", "ModuleFrameworkExternalClient", [HERE / "ExternalClient.cs"], [library, control], True)
    denied = project(out / "Denied", "ModuleFrameworkDeniedClient", [HERE / "DeniedClient.cs"], [library], True)
    core = project(out / "CoreOnly", "ModuleFrameworkCoreOnly", [ROOT / p for p in SOURCES if p not in API_SOURCES] + [HERE / "HostStubs.cs"])
    core_status, core_log = run_dotnet(args.dotnet, ["build", str(core), "-c", "Release", "--nologo"], out)
    (out / "core-only.log").write_text(core_log, encoding="utf-8")
    if core_status: print(core_log); return core_status
    print("PASS core-only build: no API sources or reference")
    status, log = run_dotnet(args.dotnet, ["run", "--project", str(client), "-c", "Release"], out)
    print(log, end="")
    if status != 0: return status
    snapshot_mutations(args.dotnet, out)
    code, denied_log = run_dotnet(args.dotnet, ["build", str(denied), "-c", "Release", "--nologo"], out)
    denial_ok = code != 0 and "CS0122" in denied_log and "InternalModuleDirectory" in denied_log
    print("PASS expected external internal-access compiler rejection (CS0122)" if denial_ok else "FAIL expected internal access rejection")
    (out / "denied.log").write_text(denied_log, encoding="utf-8")
    metadata_log = ""
    metadata_ok = True
    if args.artifact_root:
        metadata = project(out / "Metadata", "ModuleFrameworkArtifactMetadata", [HERE / "ArtifactMetadata.cs"], executable=True)
        paths = []
        for folder in args.artifact_root:
            base = Path(folder).resolve()
            paths.extend(str(base / "versions" / api / "AnimusForge.dll") for api in ["1.3", "1.4"])
        meta_code, metadata_log = run_dotnet(args.dotnet, ["run", "--project", str(metadata), "-c", "Release", "--"] + paths, out)
        metadata_ok = meta_code == 0
        print(metadata_log, end="")
    else:
        metadata_log = "NOT CHECKED: actual dual-version DLL API metadata (pass --artifact-root).\n"
        print(metadata_log, end="")
    (out / "metadata.log").write_text(metadata_log, encoding="utf-8")
    fingerprint = "\n".join(f"{p} SHA256={hashlib.sha256((ROOT/p).read_bytes()).hexdigest()}" for p in SOURCES)
    (out / "run.log").write_text(fingerprint + "\n" + log + "\nExpected internal access rejected=" + str(denial_ok) + "\n" + metadata_log, encoding="utf-8")
    return 0 if status == 0 and denial_ok and metadata_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
