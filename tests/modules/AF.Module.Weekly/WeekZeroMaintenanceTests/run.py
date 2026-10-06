from pathlib import Path
import argparse,importlib.util,subprocess,sys
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
ap=argparse.ArgumentParser();ap.add_argument('--out',type=Path,required=True);ap.add_argument('--source-ref');ap.add_argument('--records',type=Path);ap.add_argument('--materials',type=Path);ap.add_argument('--opening',type=Path);args=ap.parse_args()
if args.materials and not args.records:ap.error('--materials requires --records')
if args.opening and not (args.records and args.materials):ap.error('--opening requires --records and --materials')
out=new_run_root(ROOT,'week-zero-maintenance',args.out);dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(extract)
def source(path):
 return subprocess.check_output(['git','show',args.source_ref+':'+path],cwd=ROOT).decode('utf-8-sig') if args.source_ref else (ROOT/path).read_text(encoding='utf-8-sig')
def decl(s,t):return extract.declaration(s,t)
host=source('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs');data=source('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.CampaignMaterialRecords.cs');legacy=source('src/modules/AF.Module.Weekly/Models/WeeklyLegacyDtos.cs');rules=source('src/modules/AF.Module.Weekly/Generation/WeeklyGenerationRules.cs');owner=source('src/modules/AF.Module.Memory/Records/CampaignMaterialRecordOwner.cs');imports=source('src/modules/AF.Module.Weekly/ImportExport/WeeklyEventDataImportOwner.cs');ledger=source('src/modules/AF.Module.Memory/Records/NpcActionLedger.cs')
methods=['private void EnsureWeekZeroOpeningSummaryEvents','private bool ProcessWeekZeroOpeningSummaryEventsSlice','private void FinalizeWeekZeroOpeningSummaryMaintenance','private bool UpsertWeekZeroOpeningSummaryEvent','private static string ComputeWeekZeroShortSummarySourceHash','private static string BuildWeekZeroPromptText','private static bool HasWeekZeroLlmShortSummary','private static string BuildPublishedWorldWeeklyProductState','private static void AppendPublishedWorldWeeklyProductField','private void NotifyPublishedWorldWeeklyProductChanged','private void NotifyWorldMessageWeeklyTimelineChanged','private Task<bool> ApplyWeekZeroShortSummaryOnMainThreadAsync','private static HashSet<string> BuildEventSourceMaterialStableKeySet']
prefix='using System;using System.IO;using System.Linq;using System.Text;using System.Text.Json;using System.Text.RegularExpressions;using System.Threading;using System.Threading.Tasks;using System.Security.Cryptography;using System.Collections.Generic;using System.Collections.Concurrent;using System.Diagnostics;namespace AnimusForge;\n'
code=prefix+'public partial class MyBehavior {\n'+ '\n'.join([decl(host,'internal sealed class EventRecordEntry'),decl(data,'internal sealed class EventSourceMaterialEntry'),decl(legacy,'internal sealed class EventMaterialReference')]+[decl(host,t) for t in methods])+'\n}\n'
code+='internal static class WeeklyGenerationRules {\n'+'\n'.join(decl(rules,'internal static string '+x) for x in ['NeutralizeWeeklyReportScenarioName','BuildFallbackWeeklyReportShortSummary','NormalizeWeeklyReportTagText'])+'\n}\n'
code+='internal static class NpcActionLedger { '+decl(ledger,'internal static string NormalizeStableKey')+' }\n'
code+='internal static class CampaignMaterialRecordOwner {\n'+decl(owner,'internal static List<EventSourceMaterialEntry> SanitizeEventSourceMaterials').replace('EventSourceMaterialEntry','MyBehavior.EventSourceMaterialEntry')+'\n'
if 'internal static HashSet<string> BuildStableKeySet' in owner:code+=decl(owner,'internal static HashSet<string> BuildStableKeySet').replace('EventSourceMaterialEntry','MyBehavior.EventSourceMaterialEntry')
code+='}\ninternal static class WeeklyEventDataImportOwner {\n'+decl(imports,'internal static List<EventRecordEntry> SanitizeEventRecordEntries').replace('EventRecordEntry','MyBehavior.EventRecordEntry').replace('EventMaterialReference','MyBehavior.EventMaterialReference')+'\n}\n'
(out/'Production.cs').write_text(code,encoding='utf-8');(out/'Program.cs').write_text((HERE/'Program.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0169;0414</NoWarn><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Production.cs"/><Compile Include="Program.cs"/></ItemGroup></Project>',encoding='utf-8')
cmd=[str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release','--']
if args.records:cmd.append(str(args.records.resolve()))
if args.materials:cmd.append(str(args.materials.resolve()))
if args.opening:cmd.append(str(args.opening.resolve()))
r=subprocess.run(cmd,cwd=ROOT,env=env,text=True,encoding='utf-8',errors='replace',capture_output=True);(out/'run.log').write_text(r.stdout+'\n'+r.stderr,encoding='utf-8');print(r.stdout,r.stderr);print('evidence:',out);raise SystemExit(r.returncode)
