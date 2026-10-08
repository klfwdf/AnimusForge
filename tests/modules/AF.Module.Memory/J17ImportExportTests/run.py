from pathlib import Path
import argparse, os, re, subprocess, sys
from xml.sax.saxutils import escape
root = Path(__file__).resolve().parents[4]
here = Path(__file__).resolve().parent
sys.path.insert(0, str(root / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
parser = argparse.ArgumentParser()
parser.add_argument("--dotnet")
parser.add_argument("--run-root", type=Path)
parser.add_argument("--mutate", choices=["accept-unrelated-object"])
args = parser.parse_args()
# This historical owner's test framework remains net10.0; no fallback to net8.
dotnet = resolve_dotnet(root, args.dotnet or os.environ.get("AF_DOTNET10") or r"C:\Program Files\dotnet\dotnet.exe", major=10)
out = new_run_root(root, "j17-import-export", args.run_root)
project = out / "J17ImportExportTests.csproj"
files = [root / "src/modules/AF.Module.Memory/ImportExport/MemoryImportExportOwner.cs",
         root / "src/modules/AF.Module.Memory/ImportExport/MemoryDeveloperEditOwner.cs",
         root / "src/modules/AF.Module.Memory/ImportExport/CompressedMemoryExportBundleReader.cs",
         root / "src/AF.Persistence/PlayerExportsStore.cs",
         root / "src/AF.Persistence/PlayerExportsPackageExport.cs",
         root / "src/AF.Persistence/AnimusForgeDataPaths.cs",
         here / "OwnerChecks.cs", here / "ImportSchemaChecks.cs"]
# Replay the current single-NPC entry without an inverse source projection.
import ast
extract_path = root / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py"
node = next(n for n in ast.parse(extract_path.read_text(encoding="utf-8-sig")).body if isinstance(n, ast.FunctionDef) and n.name == "declaration")
scope = {"re": re}
exec(compile(ast.Module(body=[node], type_ignores=[]), str(extract_path), "exec"), scope)
declaration = scope["declaration"]
source = (root / "src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.ImportExportUi.cs").read_text(encoding="utf-8-sig")
formats = (root / "src/AF.GameAdapter.Bannerlord/ImportExport/MemoryHistoryImportExportAdapter.cs").read_text(encoding="utf-8-sig")
names = ["ImportSingleNpcDialogueHistoryData", "ImportDialogueHistoryData"]
facades = "\n".join(declaration(source, "private void " + name) for name in names)
bodies = "\n".join(declaration(formats, "internal void " + name) for name in names)
capabilities = r'''
internal sealed partial class MyBehavior {
 private MemoryHistoryImportExportAdapter MemoryHistoryFiles => new(
  () => 1, IsMemorySourceEditorCurrent, ShowDuplicateImportInquiry,
  HasCompressedMemoryDataForHero, ApplyCompressedMemoryExportBundle);
}
internal sealed class MemoryHistoryImportExportAdapter {
 private readonly Func<long> _captureGeneration;
 private readonly Func<long,bool> _isCurrent;
 private readonly Action<string,string,Action,Action,Action> _showDuplicate;
 private readonly Func<string,bool> _hasData;
 private readonly Func<string,CompressedMemoryExportBundle,bool,bool> _apply;
 internal MemoryHistoryImportExportAdapter(Func<long> capture,Func<long,bool> current,Action<string,string,Action,Action,Action> show,Func<string,bool> hasData,Func<string,CompressedMemoryExportBundle,bool,bool> apply) {
  _captureGeneration=capture;_isCurrent=current;_showDuplicate=show;_hasData=hasData;_apply=apply;
 }
 private bool HasCompressedMemoryDataForHero(string id)=>_hasData(id);
 private bool ApplyCompressedMemoryExportBundle(string id,CompressedMemoryExportBundle bundle,bool overwriteExisting)=>_apply(id,bundle,overwriteExisting);
'''
header = 'using System;using System.IO;using System.Collections.Generic;using TaleWorlds.Library;namespace AnimusForge {'
host = out / "CurrentSingleImport.cs"
host.write_text(header + 'internal sealed partial class MyBehavior {' + facades + '}\n' + capabilities + bodies + '}}', encoding="utf-8")
files.append(host)
files.append(root / "src/AF.Persistence/NpcDataFileName.cs")
if args.mutate:
    original = root / "src/modules/AF.Module.Memory/ImportExport/CompressedMemoryExportBundleReader.cs"
    text = original.read_text(encoding="utf-8-sig")
    assert text.count("if (!hasMemoryField)") == 1
    mutated = out / original.name
    mutated.write_text(text.replace("if (!hasMemoryField)", "if (false && !hasMemoryField)"), encoding="utf-8")
    files[files.index(original)] = mutated
newtonsoft = Path(os.environ.get("AF_NEWTONSOFT") or root / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll")
assert newtonsoft.is_file(), "Newtonsoft dependency missing"
project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + ''.join('<Compile Include="' + escape(str(f)) + '" />' for f in files) + '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
env = minimal_test_environment(dotnet, out)
result = subprocess.run([str(dotnet), "build", str(project), "-c", "Release", "-p:NuGetAudit=false", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
log = result.stdout + result.stderr
code = result.returncode
if code == 0:
    result = subprocess.run([str(dotnet), str(out / "bin/Release/net10.0/J17ImportExportTests.dll")], cwd=out, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    log += result.stdout + result.stderr
    code = result.returncode
(out / "run.log").write_text(log, encoding="utf-8")
print(log, end="")
raise SystemExit(code)
