"""Compile unchanged production method spans with fake storage/game objects; no game or network.

The linked policy and Newtonsoft serializer are real. This does not exercise Bannerlord IDataStore
chunking or actual hourly event dispatch. Generated files stay under ignored repository artifacts.
"""
from pathlib import Path
import re
import subprocess
import argparse
import sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "tests"))
from output_isolation import current_source_path, new_run_root, resolve_dotnet, minimal_test_environment
parser = argparse.ArgumentParser()
parser.add_argument("--dotnet")
parser.add_argument("--run-root", type=Path)
parser.add_argument("--newtonsoft", type=Path)
args = parser.parse_args()
dotnet = resolve_dotnet(ROOT, args.dotnet)
# The net8 replay uses the already installed SDK serializer, not the game
# net472 assembly that requires System.Security.Permissions under this host.
newtonsoft = args.newtonsoft or dotnet.parent / "sdk/8.0.425/Newtonsoft.Json.dll"
if not newtonsoft.is_file():
    raise SystemExit("Pass --newtonsoft <existing compatible Newtonsoft.Json.dll>")


import importlib.util,hashlib,json
spec=importlib.util.spec_from_file_location('world_extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
sys.path.insert(0,str(ROOT/'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests'))
from business_owner_fixture_support import enable_expression_declarations
enable_expression_declarations(ex)
inventory=[]
def method(text,name):
    match=re.search(r'(?m)^\s*(?:private|internal) [^\n]*?\b'+name+r'\(',text)
    if not match:raise RuntimeError('Missing production method: '+name)
    value=ex.declaration(text,match.group().strip());inventory.append({'symbol':name,'sha256':hashlib.sha256(value.encode()).hexdigest()});return value


host = current_source_path(ROOT, "MyBehavior.WorldBulletin.cs").read_text(encoding="utf-8-sig")
npc = current_source_path(ROOT, "MyBehavior.WorldBulletinNpc.cs").read_text(encoding="utf-8-sig")
fields = host[host.index("public partial class MyBehavior"):host.index("\tinternal static bool IsWorldBulletinEnabled")]
npc_fields = npc[npc.index("\tprivate List<EventRecordEntry> _worldBulletinCachedRecords"):npc.index("\tprivate WeeklyPromptSnapshot CaptureWorldBulletinNpcSnapshot")]
spans = [method(host, name) for name in (
    "IsWorldBulletinEventId", "EnsureWorldBulletinState", "SyncWorldBulletinData",
    "ResetWorldBulletinTransientState", "ResetWorldBulletinForRuntime")]
spans += [method(npc, name) for name in ("FindLatestWorldBulletinRecord", "GetWorldBulletinRecordSequence")]
generated = """using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;
namespace AnimusForge;
""" + fields + npc_fields + "\n".join(spans) + "\n}\n"

owner=(ROOT/'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs').read_text(encoding='utf-8-sig')
presentation=(ROOT/'src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Presentation.cs').read_text(encoding='utf-8-sig')
owner_fields=owner[owner.index('internal sealed partial class WorldBulletinStateOwner {'):owner.index('internal WorldBulletinSaveState EnsureWorldBulletinState()')]
cache_fields=presentation[presentation.index('internal List<EventRecordEntry> CachedRecords;'):presentation.index('private const int WorldBulletinMaxLayouts')]
actual_methods=[method(owner,n) for n in ['EnsureWorldBulletinState','IsWorldBulletinEventId','ReleasePendingWorldBulletinNotice','ResetTransient','ResetRuntime','ExportJson','ImportJson']]+[method(presentation,n) for n in ['FindLatestWorldBulletinRecord','GetWorldBulletinRecordSequence']]
owned='using System;using System.Linq;using System.Globalization;using System.Collections.Generic;using System.Collections.Concurrent;using static AnimusForge.MyBehavior;namespace AnimusForge;'+owner_fields+cache_fields+'\n'.join(actual_methods)+'}'
# Only calendar, storage/game records and logging are fixture facts; all state/cache/save algorithms above are actual spans.
owned+='internal sealed class WorldBulletinPort {internal Func<int> CurrentDay;internal Func<List<EventRecordEntry>> Records;internal Action<string,string> Log;internal Func<string,MyBehavior.EventRecordEntry> FindRecord;internal Action<string> QueueNotice;}'
generated=generated.replace('public partial class MyBehavior\n{','public partial class MyBehavior\n{ private readonly WorldBulletinStateOwner _worldBulletinOwner=new();private WorldBulletinStateOwner WorldBulletinState {get { _worldBulletinOwner.Bind(new(){CurrentDay=GetCurrentGameDayIndexSafe,Records=()=>_eventRecordEntries,Log=Logger.Log});return _worldBulletinOwner;}}',1)
run = new_run_root(ROOT, "world-bulletin-review", args.run_root)
(run / "Host.cs").write_text(generated, encoding="utf-8")
(run / "Owner.cs").write_text(owned,encoding='utf-8')
(run / "Replay.cs").write_text((HERE/'HostRegression.cs').read_text(encoding='utf-8-sig').replace('private sealed class EventRecordEntry','internal sealed class EventRecordEntry'),encoding='utf-8')
(run / "source-manifest.json").write_text(json.dumps(inventory,indent=2),encoding='utf-8')
links = [ HERE / "LayoutStub.cs", ROOT / "src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs", ROOT / "src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs"]
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>10</LangVersion></PropertyGroup><ItemGroup>'
project += "".join('<Compile Include="' + escape(str(p), {'"': '&quot;'}) + '" />' for p in links)
project += '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>'
(run / "HostRegression.csproj").write_text(project, encoding="utf-8")
(run / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
print("Production method span replay:", run, flush=True)
raise SystemExit(subprocess.call([str(dotnet), "run", "--project", str(run / "HostRegression.csproj")], cwd=run, env=minimal_test_environment(dotnet, run)))
