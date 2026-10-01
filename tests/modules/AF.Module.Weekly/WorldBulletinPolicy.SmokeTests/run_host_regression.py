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


def method(text, name):
    match = re.search(r"(?m)^\t(?:private|internal) [^\n]*\b" + name + r"\([^\n]*\)\n\t\{", text)
    if not match:
        raise RuntimeError("Missing production method: " + name)
    end = text.index("\n\t}", match.end()) + len("\n\t}")
    return text[match.start():end]


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

run = new_run_root(ROOT, "world-bulletin-review", args.run_root)
(run / "Host.cs").write_text(generated, encoding="utf-8")
links = [HERE / "HostRegression.cs", HERE / "LayoutStub.cs", ROOT / "src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs"]
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>10</LangVersion></PropertyGroup><ItemGroup>'
project += "".join('<Compile Include="' + escape(str(p), {'"': '&quot;'}) + '" />' for p in links)
project += '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(newtonsoft)) + '</HintPath></Reference></ItemGroup></Project>'
(run / "HostRegression.csproj").write_text(project, encoding="utf-8")
(run / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
print("Production method span replay:", run, flush=True)
raise SystemExit(subprocess.call([str(dotnet), "run", "--project", str(run / "HostRegression.csproj")], cwd=run, env=minimal_test_environment(dotnet, run)))
