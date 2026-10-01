"""Compile unchanged production method spans with fake storage/game objects; no game or network.

The linked policy and Newtonsoft serializer are real. This does not exercise Bannerlord IDataStore
chunking or actual hourly event dispatch. Generated files stay under ignored repository artifacts.
"""
from pathlib import Path
import re
import subprocess
import tempfile
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent


def method(text, name):
    match = re.search(r"(?m)^\t(?:private|internal) [^\n]*\b" + name + r"\([^\n]*\)\n\t\{", text)
    if not match:
        raise RuntimeError("Missing production method: " + name)
    end = text.index("\n\t}", match.end()) + len("\n\t}")
    return text[match.start():end]


host = (ROOT / "MyBehavior.WorldBulletin.cs").read_text(encoding="utf-8-sig")
npc = (ROOT / "MyBehavior.WorldBulletinNpc.cs").read_text(encoding="utf-8-sig")
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

artifact_root = ROOT / "artifacts" / "world-bulletin-review"
artifact_root.mkdir(parents=True, exist_ok=True)
run = Path(tempfile.mkdtemp(prefix="host-", dir=artifact_root))
(run / "Host.cs").write_text(generated, encoding="utf-8")
links = [HERE / "HostRegression.cs", HERE / "LayoutStub.cs", ROOT / "src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs"]
project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>10</LangVersion></PropertyGroup><ItemGroup>'
project += "".join('<Compile Include="' + escape(str(p), {'"': '&quot;'}) + '" />' for p in links)
project += '<Reference Include="Newtonsoft.Json"><HintPath>' + escape(str(ROOT / "_deps_auto/Newtonsoft.Json.dll")) + '</HintPath></Reference></ItemGroup></Project>'
(run / "HostRegression.csproj").write_text(project, encoding="utf-8")
print("Production method span replay:", run, flush=True)
raise SystemExit(subprocess.call(["dotnet", "run", "--project", str(run / "HostRegression.csproj")], cwd=ROOT))
