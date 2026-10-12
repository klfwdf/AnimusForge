"""Compile the exact current record with the production JSON/chunk boundary."""
from pathlib import Path
import hashlib
import importlib.util
import json
import subprocess
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

spec = importlib.util.spec_from_file_location("extract", ROOT / "tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py")
extract = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extract)
source = ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.cs"
record = extract.declaration(source.read_text(encoding="utf-8-sig"), "private sealed class CourierLetterInventoryRecord")
out = new_run_root(ROOT, "courier-receipt-persistence", None)
(out / "Program.cs").write_text((HERE / "ReceiptPersistence.cs.txt").read_text(encoding="utf-8").replace("@@RECORD@@", record), encoding="utf-8")
links = [ROOT / "src/AF.Persistence/CampaignSaveChunkHelper.cs", ROOT / "tests/AF.Persistence/PersistenceChunkReplayTests/ReplayStubs.cs"]
newtonsoft = ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll"
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>'
project += ''.join('<Compile Include="' + escape(str(path)) + '" />' for path in links)
project += '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>'
(out / "Replay.csproj").write_text(project, encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding="utf-8")
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), "run", "--project", str(out / "Replay.csproj"), "-c", "Release"], cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, encoding="utf-8", errors="replace")
(out / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
(out / "receipt.json").write_text(json.dumps({"exitCode": result.returncode, "recordSha256": hashlib.sha256(record.encode()).hexdigest(), "sources": {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in links}}, indent=2), encoding="utf-8")
print(result.stdout + result.stderr, end="")
print("Evidence:", out)
sys.exit(result.returncode)
