"""Source-linked V1 API checks: stub composition host, real contracts and runtime."""
from __future__ import annotations
import argparse
import hashlib
import os
from pathlib import Path
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
SOURCES = ["Api/V1/AfApi.cs", "Api/V1/AfApiContracts.cs",
    "Refactor/Modules/InternalModuleDirectory.cs", "Refactor/Modules/ModuleFrameworkRuntime.cs",
    "Refactor/Contracts/FeatureBridgeContracts.cs"]


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


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=r"G:\AFMOD\.dotnet-sdk\dotnet.exe")
    parser.add_argument("--artifact-root", action="append", default=[], help="Optional actual single_module_artifacts directory; repeat for Debug/Release")
    args = parser.parse_args()
    out = HERE / ".generated/current"
    out.mkdir(parents=True, exist_ok=True)
    (out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
    library = project(out / "Library", "ModuleFrameworkUnderTest", [ROOT / p for p in SOURCES] + [HERE / "HostStubs.cs"])
    control = project(out / "Control", "ModuleFrameworkControl", [HERE / "HostControl.cs"], [library])
    client = project(out / "Client", "ModuleFrameworkExternalClient", [HERE / "ExternalClient.cs"], [library, control], True)
    denied = project(out / "Denied", "ModuleFrameworkDeniedClient", [HERE / "DeniedClient.cs"], [library], True)
    status, log = run_dotnet(args.dotnet, ["run", "--project", str(client), "-c", "Release"], out)
    print(log, end="")
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
