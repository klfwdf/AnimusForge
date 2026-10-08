"""Current Memory/Weekly persistence: actual adapters, codecs and owner store declarations.
No live campaign, no .sav files and no network. Corruption observations are separate from roundtrip.
"""
import argparse, ast, hashlib, json, re, subprocess, sys
from pathlib import Path
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);a=p.parse_args()
out=new_run_root(ROOT,'current-memory-weekly-owner-persistence',a.run_root)
extract_path=ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py'
node=next(n for n in ast.parse(extract_path.read_text(encoding='utf-8-sig')).body if isinstance(n,ast.FunctionDef) and n.name=='declaration')
scope={'re':re};exec(compile(ast.Module(body=[node],type_ignores=[]),str(extract_path),'exec'),scope);decl=scope['declaration']
sources={}
def read(path):
 q=ROOT/path;data=q.read_bytes();sources[path]=hashlib.sha256(data).hexdigest();return data.decode('utf-8-sig')
def write(name,text):(out/name).write_text(text,encoding='utf-8')
host=read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs')
materials=read('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs')
legacy=read('src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs')
dtos='\n'.join([decl(host,'internal class DialogueDay'),decl(host,'internal sealed class EventRecordEntry'),decl(materials,'internal sealed class EventSourceMaterialEntry'),decl(legacy,'internal sealed class EventMaterialReference')])
recovery=read('src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs')
markers=''.join(decl(recovery,marker) for marker in ['internal static bool IsValidMemoryCommitMarker(','internal static bool IsMemoryRecoveryHexDigest('])
write('Dtos.cs','using System;using System.Linq;using System.Collections.Generic;namespace AnimusForge;public partial class MyBehavior {'+dtos+markers+'}')
fields=[]
for file in sorted((ROOT/'src/modules/AF.Module.Memory/Summary').glob('MemoryBusinessStateOwner*.cs')):
 src=read(file.relative_to(ROOT).as_posix())
 fields.extend(line for line in src.splitlines() if re.match(r'^\s*internal (?:Dictionary<.*>|List<.*>) \w+\s*=',line))
write('MemoryStores.cs','using System;using System.Collections.Generic;namespace AnimusForge;internal sealed class MemoryBusinessStateOwner {'+'\n'.join(fields)+'}')
weekly=read('src/modules/AF.Module.Weekly/Records/WeeklyEventRecordStateOwner.cs')
weekly_fields=[line for line in weekly.splitlines() if re.match(r'^\s*internal (?:Dictionary<.*>|List<.*>|string) (?:KingdomOpenings|Records|WorldOpening)\b',line)]
assert len(weekly_fields)==3
write('WeeklyStores.cs','using System;using System.Collections.Generic;namespace AnimusForge;internal sealed class WeeklyEventRecordStateOwner {'+'\n'.join(weekly_fields)+'}')
material_owner=read('src/modules/AF.Module.Memory/Records/CampaignMaterialRecordOwner.cs')
ledger=read('src/modules/AF.Module.Memory/Records/NpcActionLedger.cs')
write('MaterialStores.cs','using System;using System.Linq;using System.Collections.Generic;using EventSourceMaterialEntry=AnimusForge.MyBehavior.EventSourceMaterialEntry;namespace AnimusForge;internal sealed class CampaignMaterialRecordOwner {'+next(line for line in material_owner.splitlines() if 'internal List<EventSourceMaterialEntry> Materials =' in line)+decl(material_owner,'internal static List<EventSourceMaterialEntry> SanitizeEventSourceMaterials(')+'}internal static class NpcActionLedger {'+decl(ledger,'internal static string NormalizeStableKey(')+'}')
bulletin=read('src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.cs')
layout=read('src/modules/AF.Module.Weekly/Generation/WorldBulletinStateOwner.Presentation.cs')
# The actual persisted owner serialization methods use only the logging port here.
write('BulletinStores.cs','using System;using System.Collections.Generic;namespace AnimusForge;'+decl(layout,'internal sealed class WorldBulletinLayout')+'internal sealed class WorldBulletinStateOwner {internal WorldBulletinSaveState State;internal string CorruptRaw;private WorldBulletinPort _port=new();'+decl(bulletin,'internal string ExportJson(')+decl(bulletin,'internal void ImportJson(')+'}internal sealed class WorldBulletinPort {internal Action<string,string> Log=Logger.Log;}')
linked=['src/AF.GameAdapter.Bannerlord/Persistence/'+x+'.cs' for x in ['CampaignMemoryPersistenceAdapter','CampaignWeeklyRecordPersistenceAdapter','CampaignMaterialPersistenceAdapter','CampaignWorldBulletinPersistenceAdapter']]
linked+=['src/AF.Persistence/CampaignSaveChunkHelper.cs','src/AF.Persistence/OwnerJsonStorageCodec.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs','src/modules/AF.Module.Weekly/Bulletin/WorldBulletinPolicy.cs']
for path in linked:read(path)
write('Harness.cs',(HERE/'Harness.cs.txt').read_text(encoding='utf-8'))
write('Tests.csproj','<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><NoWarn>CS0649;CS0169;CS0414</NoWarn><UseAppHost>false</UseAppHost><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+escape(str(ROOT/path))+'"/>' for path in linked)+'<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll'))+'</HintPath></Reference></ItemGroup></Project>')
write('NuGet.Config','<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);r=subprocess.run([str(dotnet),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(dotnet,out),text=True,encoding='utf-8',errors='replace',capture_output=True,timeout=120)
write('run.log',r.stdout+r.stderr)
write('evidence.json',json.dumps({'revision':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'sources':sources,'testFiles':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in HERE.iterdir() if f.is_file()},'exitCode':r.returncode,'ownerStoreDeclarations':'extracted current exact fields','game':'STUBBED','serializer':'actual Newtonsoft; in-memory IDataStore','liveSav':'NOT_RUN','network':'NOT_RUN'},ensure_ascii=False,indent=2))
print(r.stdout+r.stderr,end='');print('EVIDENCE='+str(out/'evidence.json'));raise SystemExit(r.returncode)
