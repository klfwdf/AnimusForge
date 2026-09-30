"""Execute the nine production record DTOs from pinned old source or new owner.

Output is a caller-owned new directory, never a fixed current/.generated path.
"""
import argparse
import json
import os
import subprocess
from pathlib import Path

from b1a_baseline import BASE, NAMES, ROOT, declaration

parser = argparse.ArgumentParser()
parser.add_argument("--owner", choices=("old", "new"), required=True)
parser.add_argument("--out", type=Path, required=True)
args = parser.parse_args()
out = args.out.resolve()
assert not out.exists(), "output must be new"
assert out.is_relative_to(ROOT / "artifacts/j17b/session-20260930/b1a-tests"), "output outside owned evidence"
out.mkdir(parents=True)
dotnet = ROOT / "local/dotnet/8.0.425/dotnet.exe"
assert dotnet.is_file()
if args.owner == "old":
    source = subprocess.check_output(["git", "show", f"{BASE}:MyBehavior.cs"], cwd=ROOT).decode("utf-8-sig")
    models = "\n".join(declaration(source, "private sealed class " + name) for name in NAMES)
    (out / "Models.cs").write_text("using System; using System.Collections.Generic; using System.Linq; namespace AnimusForge { public partial class MyBehavior {" + models + "}}", encoding="utf-8")
else:
    owner = ROOT / "src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs"
    assert owner.is_file(), "new owner unavailable"
    source = owner.read_text(encoding="utf-8-sig")
    models = "\n".join(declaration(source, "internal sealed class " + name) for name in NAMES)
    (out / "Models.cs").write_text("using System; using System.Collections.Generic; using System.Linq; namespace AnimusForge {" + models + "}", encoding="utf-8")
program = r'''using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge;

class Program {
  static int checks;
  static void Check(bool ok, string text) { checks++; if (!ok) throw new Exception("FAIL " + text); }
  static Type Model(string name) => typeof(MyBehavior).Assembly.GetType("AnimusForge." + name)
      ?? typeof(MyBehavior).GetNestedType(name, BindingFlags.NonPublic)
      ?? throw new Exception("missing " + name);
  static void Main() {
    string[] names = { "DailyMemoryLine", "DailyMemoryDraft", "CompressedMemoryBlock", "WeeklyMemoryMaterialTrigger", "MemorySummaryJob", "MemoryOverviewState", "MemoryOverviewJob", "MajorActionSummaryState", "MajorActionSummaryJob" };
    var shape = new SortedDictionary<string, object>();
    int fields = 0;
    foreach (string name in names) {
      Type type = Model(name);
      object item = Activator.CreateInstance(type, true);
      var fs = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
      fields += fs.Length;
      var defaults = new SortedDictionary<string, object>();
      foreach (var f in fs) defaults[f.Name] = f.GetValue(item);
      var json = JsonConvert.SerializeObject(item);
      var doc = JObject.Parse(json);
      Check(doc.Properties().Count() == fs.Length, name + " JSON field count");
      foreach (var f in fs) Check(doc.Property(f.Name) != null, name + "." + f.Name + " JSON field");
      object missing = JsonConvert.DeserializeObject("{}", type);
      foreach (var f in fs) Check(JsonConvert.SerializeObject(f.GetValue(missing)) == JsonConvert.SerializeObject(f.GetValue(item)), name + "." + f.Name + " missing default");
      object roundTrip = JsonConvert.DeserializeObject(json, type);
      Check(JsonConvert.SerializeObject(roundTrip) == json, name + " JSON roundtrip");
      shape[name] = new { fields = fs.Select(f => new { name = f.Name, type = f.FieldType.IsGenericType ? "List<" + f.FieldType.GetGenericArguments()[0].Name + ">" : f.FieldType.Name, value = JsonConvert.SerializeObject(f.GetValue(item)) }).ToArray(), json };
    }
    Check(fields == 95, "95 fields");
    void Copy(string name, string field) {
      Type type = Model(name); object item = Activator.CreateInstance(type, true);
      var source = (IList)type.GetField(field).GetValue(item); source.Add("before");
      object copy = type.GetMethod("CopyForSummary", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(item, null);
      var detached = (IList)type.GetField(field).GetValue(copy);
      Check(!ReferenceEquals(item, copy) && !ReferenceEquals(source, detached), name + " list identity");
      detached.Add("after"); Check(source.Count == 1, name + " list mutation");
    }
    Copy("WeeklyMemoryMaterialTrigger", "Tags"); Copy("CompressedMemoryBlock", "Scenes");
    Copy("CompressedMemoryBlock", "AfefLines"); Copy("MemoryOverviewState", "IncludedBlockIds");
    Type draftType = Model("DailyMemoryDraft"), lineType = Model("DailyMemoryLine"), triggerType = Model("WeeklyMemoryMaterialTrigger");
    object draft = Activator.CreateInstance(draftType, true);
    object line = Activator.CreateInstance(lineType, true);
    object trigger = Activator.CreateInstance(triggerType, true);
    ((IList)draftType.GetField("Lines").GetValue(draft)).Add(line);
    ((IList)draftType.GetField("WeeklyMaterialTriggers").GetValue(draft)).Add(trigger);
    object draftCopy = draftType.GetMethod("CopyForSummary", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(draft, null);
    Check(!ReferenceEquals(((IList)draftType.GetField("Lines").GetValue(draft))[0], ((IList)draftType.GetField("Lines").GetValue(draftCopy))[0]), "draft line deep copy");
    Check(!ReferenceEquals(((IList)draftType.GetField("WeeklyMaterialTriggers").GetValue(draft))[0], ((IList)draftType.GetField("WeeklyMaterialTriggers").GetValue(draftCopy))[0]), "draft trigger deep copy");
    Console.WriteLine(JsonConvert.SerializeObject(new { checks, fields, shape }));
  }
}
'''
# The old models are nested; a minimal owner marker keeps both variants source-compatible.
if args.owner == "new":
    program = program.replace("using AnimusForge;", "using AnimusForge; namespace AnimusForge { public partial class MyBehavior {} }")
(out / "Program.cs").write_text(program, encoding="utf-8")
# The bundled game-facing Newtonsoft targets .NET Framework and requires
# System.Security.Permissions; use the repository-local SDK's net8-compatible
# Newtonsoft for the detached DTO wire-shape comparison.
(out / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit><Nullable>disable</Nullable></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>' + str(ROOT / "local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll").replace("\\", "/") + '</HintPath></Reference></ItemGroup></Project>', encoding="utf-8")
(out / "NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding="utf-8")
temp = Path("E:/tmp/af-j17-20260930")
temp.mkdir(parents=True, exist_ok=True)
(out / "appdata").mkdir()
(out / "home").mkdir()
os.environ.update({"TEMP": str(temp), "TMP": str(temp), "APPDATA": str(out / "appdata"), "LOCALAPPDATA": str(out / "appdata"), "USERPROFILE": str(out / "home"), "DOTNET_ROOT": str(dotnet.parent), "DOTNET_CLI_HOME": str(out / "home"), "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1", "NUGET_PACKAGES": str(ROOT / ".tmp/nuget-packages")})
build = subprocess.run([str(dotnet), "build", str(out / "Proof.csproj"), "-c", "Release", "--nologo", "-p:RestoreConfigFile=" + str(out / "NuGet.Config")], cwd=out, capture_output=True, text=True, encoding="utf-8", errors="replace")
(out / "build.log").write_text(build.stdout + build.stderr, encoding="utf-8")
if build.returncode: print((build.stdout + build.stderr)[-2000:]); raise SystemExit(build.returncode)
run = subprocess.run([str(dotnet), str(out / "bin/Release/net8.0/Proof.dll")], cwd=out, capture_output=True, text=True, encoding="utf-8", errors="replace")
(out / "run.log").write_text(run.stdout + run.stderr, encoding="utf-8")
if run.returncode: print((run.stdout + run.stderr)[-2000:]); raise SystemExit(run.returncode)
result = json.loads(run.stdout)
(out / "result.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"PASS owner={args.owner} checks={result['checks']} fields={result['fields']} output={out}")
