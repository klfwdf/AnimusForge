"""Replay the actual player-prompt disk save helpers with isolated synthetic files."""
from pathlib import Path
from xml.sax.saxutils import escape
import argparse
import hashlib
import json
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument("--run-root", type=Path)
args = parser.parse_args()
out = new_run_root(ROOT, "prompt-save", args.run_root)
dotnet = resolve_dotnet(ROOT)
source_path = ROOT / "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs"
source = source_path.read_text(encoding="utf-8-sig")
methods = [
    "TryPersistCustomPromptTextFile", "PersistCustomPromptTextFileUnlocked",
    "WriteCustomPromptTextJsonFileUnlocked", "TryReadCustomPromptTextJsonFile",
    "IsCustomPromptTextFileTooLarge", "IsWorldDiplomacyPromptPath",
    "GetCustomPromptTextStoreDirectory", "GetCustomPromptTextFilePath",
]
bodies = []
for name in methods:
    start = re.search(r"^\tprivate static [^\n]+\b" + name + r"\(", source, re.M).start()
    end = re.search(r"^\t(?:private|internal|public) ", source[start + 1:], re.M).start() + start + 1
    bodies.append(source[start:end])
(out / "SourceHelpers.cs").write_text(
    "using System; using System.IO; using System.Text; using Newtonsoft.Json; using AnimusForge;\n"
    "internal static partial class PromptSaveProbe {\n" + "\n".join(bodies) + "}\n", encoding="utf-8"
)
retired_names = ["GetLegacyDataPath", "GetLegacyDocumentsPath", "MigrateLegacyDirectoryOnce", "CopyLegacyFileIfMissing", "FailedMigrations"]
paths = [ROOT / "src/AF.Persistence/AnimusForgeDataPaths.cs",
         ROOT / "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/McmSettingsStorage.cs",
         source_path,
         ROOT / "Vengeance/Source/Customization/ExecutionSiteResourceCatalog.cs",
         ROOT / "Vengeance/Source/Customization/ExecutionSitePresetStore.cs"]
# Upstream retains inert shared-source signatures. The runtime storage replay
# verifies they never inspect or import old data; active consumers must not call them.
for path in paths[1:]:
    text = path.read_text(encoding="utf-8-sig")
    assert not any(name in text for name in retired_names), f"Retired migration remains in {path}"
assert "GetFolderPath" not in paths[0].read_text(encoding="utf-8-sig")
assert "FailedMigrations" not in paths[0].read_text(encoding="utf-8-sig")
links = [HERE / "PromptSaveHarness.cs.txt", out / "SourceHelpers.cs", paths[0]]
(out / "Tests.csproj").write_text(
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
    '<TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
    '<UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'
    + "".join('<Compile Include="' + escape(str(path)) + '" />' for path in links)
    + '<Reference Include="Newtonsoft.Json"><HintPath>$(MSBuildSDKsPath)/../Newtonsoft.Json.dll</HintPath></Reference>'
    '</ItemGroup></Project>', encoding="utf-8"
)
command = [str(dotnet), "run", "--project", str(out / "Tests.csproj"), "-c", "Release", "--", str(out / "fixtures")]
result = subprocess.run(command, cwd=out, env=minimal_test_environment(dotnet, out),
                        capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180)
(out / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
(out / "receipt.json").write_text(json.dumps({"exit": result.returncode, "command": command,
    "methods": methods, "source_sha256": hashlib.sha256(source_path.read_bytes()).hexdigest(),
    "coverage": "real player prompt IO plus static retired-caller exclusion; no live game"}, indent=2), encoding="utf-8")
print((result.stdout + result.stderr)[-3500:])
raise SystemExit(result.returncode)
