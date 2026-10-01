from pathlib import Path
import sys,importlib.util,subprocess,json,hashlib,argparse
R=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(R/'tests'))
from output_isolation import minimal_test_environment
s=importlib.util.spec_from_file_location('ex',R/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(s);s.loader.exec_module(ex)
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path,required=True);args=parser.parse_args()
out=args.run_root.resolve();out.relative_to(R);out.mkdir(parents=True,exist_ok=True)
main=(R/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs').read_text(encoding='utf-8-sig')
h=(R/'tests/modules/AF.Module.Memory/MemoryDailyDeveloperEditTests/Harness.cs.txt').read_text(encoding='utf-8')
prefix=h[:h.index('internal sealed class MemoryBusinessStateOwner')].replace('@@DAY@@',ex.declaration(main,'internal class DialogueDay'))
program=prefix+'''
internal sealed class MemorySealingOwner { internal MemorySealingOwner(MemoryBusinessStateOwner s) {} }
internal sealed partial class MemoryBusinessStateOwner {
 internal Dictionary<string,List<MyBehavior.DialogueDay>> History;
 internal Dictionary<string,string> HistoryStorage,DraftStorage,BlockStorage,OverviewStorage,MajorStorage;
 internal Dictionary<string,string> MajorActionStorage=new(),RecentActionStorage=new();
 internal List<NpcActionEntry> MajorActions=new(),RecentActions=new();
 internal long MajorActionOrderCounter=88;
}
internal static class Program {
 static int count; static void C(bool ok,string why){count++;if(!ok)throw new Exception(why);}
 static void Main(){
 var s=new MemoryBusinessStateOwner();
 var methods=new[]{"EnsureHistoryAndDailyPersistenceContainers","EnsureOverviewPersistenceContainers","EnsureMajorSummaryPersistenceContainers","ClearHistoryAndDailyForCurrentSave","ClearOverviewForCurrentSave","ClearMajorSummaryForCurrentSave","ClearOverviewDiscoveryForCurrentSave"};
 foreach(var m in methods)C(typeof(MemoryBusinessStateOwner).GetMethod(m,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.DeclaringType==typeof(MemoryBusinessStateOwner),"real declaring type "+m);
 s.History=null;s.HistoryStorage=null;s.Drafts=null;s.DraftStorage=null;s.Blocks=null;s.BlockStorage=null;s.DailyQueue=null;s.Overviews=null;s.OverviewStorage=null;s.OverviewQueue=null;s.MajorSummaries=null;s.MajorStorage=null;s.MajorQueue=null;
 s.EnsureHistoryAndDailyPersistenceContainers();s.EnsureOverviewPersistenceContainers();s.EnsureMajorSummaryPersistenceContainers();
 C(!s.History.Comparer.Equals("A","a")&&!s.HistoryStorage.Comparer.Equals("A","a"),"original Ordinal null ensures");
 C(s.Drafts.Comparer.Equals("A","a")&&s.DraftStorage.Comparer.Equals("A","a")&&s.Blocks.Comparer.Equals("A","a")&&s.BlockStorage.Comparer.Equals("A","a"),"daily IgnoreCase");
 C(s.Overviews.Comparer.Equals("A","a")&&s.OverviewStorage.Comparer.Equals("A","a")&&s.MajorSummaries.Comparer.Equals("A","a")&&s.MajorStorage.Comparer.Equals("A","a"),"summary IgnoreCase");
 C(s.DailyQueue!=null&&s.OverviewQueue!=null&&s.MajorQueue!=null,"queues ensured");
 var history=s.History;var drafts=s.Drafts;var blocks=s.Blocks;var overviews=s.Overviews;var major=s.MajorSummaries;
 s.EnsureHistoryAndDailyPersistenceContainers();s.EnsureOverviewPersistenceContainers();s.EnsureMajorSummaryPersistenceContainers();
 C(ReferenceEquals(history,s.History)&&ReferenceEquals(drafts,s.Drafts)&&ReferenceEquals(blocks,s.Blocks)&&ReferenceEquals(overviews,s.Overviews)&&ReferenceEquals(major,s.MajorSummaries),"existing references preserved");
 var actions=s.MajorActions;var recent=s.RecentActions;var aStore=s.MajorActionStorage;var rStore=s.RecentActionStorage;var weekly=s.PendingWeeklyTriggers;
 s.DirtyOverviewIds.Add("h");s.OverviewCandidateIds.Enqueue("h");s.OverviewCandidateIdSet.Add("h");
 s.ClearHistoryAndDailyForCurrentSave();s.ClearOverviewForCurrentSave();s.ClearMajorSummaryForCurrentSave();s.ClearOverviewDiscoveryForCurrentSave();
 C(!ReferenceEquals(history,s.History)&&!ReferenceEquals(drafts,s.Drafts)&&!ReferenceEquals(blocks,s.Blocks)&&!ReferenceEquals(overviews,s.Overviews)&&!ReferenceEquals(major,s.MajorSummaries),"original replacement semantics");
 C(s.History.Count==0&&s.Drafts.Count==0&&s.Blocks.Count==0&&s.Overviews.Count==0&&s.MajorSummaries.Count==0&&s.DailyQueue.Count==0&&s.OverviewQueue.Count==0&&s.MajorQueue.Count==0,"empty finite memory stores");
 C(s.DirtyOverviewIds.Count==0&&s.OverviewCandidateIds.Count==0&&s.OverviewCandidateIdSet.Count==0,"discovery in place clear");
 C(ReferenceEquals(actions,s.MajorActions)&&ReferenceEquals(recent,s.RecentActions)&&ReferenceEquals(aStore,s.MajorActionStorage)&&ReferenceEquals(rStore,s.RecentActionStorage)&&ReferenceEquals(weekly,s.PendingWeeklyTriggers)&&s.MajorActionOrderCounter==88,"A authority untouched");
 Console.WriteLine("PASS actual state lifecycle checks="+count+" live=NOT_RUN");
 }
}
}
'''
scene=(R/'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.SceneHistoryMessages.cs').read_text(encoding='utf-8-sig')
capture=ex.declaration(scene,'internal List<string> CaptureVisibleSceneHistoryLinesForPrompt(')
probe='''internal sealed class SceneProbe {
 internal object _historyLock=new(); internal List<string> _publicConversationHistory=new(); internal int Calls; internal bool Locked; internal int Viewer; internal string Name; internal bool Distance;
 private List<string> BuildVisibleSceneHistoryLines(List<string> source,int viewer,string name,bool distance){Calls++;Locked=System.Threading.Monitor.IsEntered(_historyLock);Viewer=viewer;Name=name;Distance=distance;return source.ToList();}
'''+capture+'}\n'
program=program.replace('internal static class Program {',probe+'internal static class Program {').replace(' Console.WriteLine("PASS actual state lifecycle', ''' var scene=new SceneProbe();C(scene.CaptureVisibleSceneHistoryLinesForPrompt(7,"viewer",true)==null&&scene.Calls==0,"empty scene history preserves null");
 scene._publicConversationHistory.Add("line");var visible=scene.CaptureVisibleSceneHistoryLinesForPrompt(7,"viewer",true);
 C(scene.Locked&&scene.Viewer==7&&scene.Name=="viewer"&&scene.Distance,"scene scalar projection called under original lock");C(visible.SequenceEqual(new[]{"line"})&&!ReferenceEquals(visible,scene._publicConversationHistory),"detached list result");
 Console.WriteLine("PASS actual state lifecycle''')
(out/'Program.cs').write_text(program,encoding='utf-8')
paths=['src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs','src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs']
(out/'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS8632</NoWarn></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(R/p)+'" />' for p in paths)+'</ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
d=R/'local/dotnet/8.0.425/dotnet.exe';r=subprocess.run([str(d),'run','--project',str(out/'Tests.csproj'),'-c','Release'],cwd=out,env=minimal_test_environment(d,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr,end='')
receipt={'owner_sha256':hashlib.sha256((R/paths[0]).read_bytes()).hexdigest(),'exit_code':r.returncode,'source_link':paths[0],'declaring_type':'AnimusForge.MemoryBusinessStateOwner'}
# Fixed original host segments read before the finite lifecycle move; no artifact dependency.
patch={'main_patches': [{'old': '\t\tif (_dialogueHistory == null)\n'
                          '\t\t{\n'
                          '\t\t\t_dialogueHistory = new Dictionary<string, List<DialogueDay>>();\n'
                          '\t\t}\n'
                          '\t\tif (_dialogueHistoryStorage == null)\n'
                          '\t\t{\n'
                          '\t\t\t_dialogueHistoryStorage = new Dictionary<string, string>();\n'
                          '\t\t}\n'
                          '\t\tif (_dailyMemoryDrafts == null)\n'
                          '\t\t{\n'
                          '\t\t\t_dailyMemoryDrafts = new Dictionary<string, '
                          'List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t}\n'
                          '\t\tif (_dailyMemoryDraftStorage == null)\n'
                          '\t\t{\n'
                          '\t\t\t_dailyMemoryDraftStorage = new Dictionary<string, '
                          'string>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t}\n'
                          '\t\tif (_compressedMemoryBlocks == null)\n'
                          '\t\t{\n'
                          '\t\t\t_compressedMemoryBlocks = new Dictionary<string, '
                          'List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t}\n'
                          '\t\tif (_compressedMemoryBlockStorage == null)\n'
                          '\t\t{\n'
                          '\t\t\t_compressedMemoryBlockStorage = new Dictionary<string, '
                          'string>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t}\n'
                          '\t\tif (_memorySummaryQueue == null)\n'
                          '\t\t{\n'
                          '\t\t\t_memorySummaryQueue = new List<MemorySummaryJob>();\n'
                          '\t\t}\n',
                   'new': '\t\t'
                          '_memoryBusinessState.EnsureHistoryAndDailyPersistenceContainers();\n',
                   'symbol': 'EnsureHistoryAndDailyPersistenceContainers',
                   'old_sha256': '477c8af0838a4167e408adf6d39eaed456c6dc9c202f3468ca4351c40a6b7704'},
                  {'old': '\t\tif (_memoryOverviewStates == null)\n'
                          '\t\t{\n'
                          '\t\t\t_memoryOverviewStates = new Dictionary<string, '
                          'MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t}\n'
                          '\t\tif (_memoryOverviewStateStorage == null)\n'
                          '\t\t{\n'
                          '\t\t\t_memoryOverviewStateStorage = new Dictionary<string, '
                          'string>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t}\n'
                          '\t\tif (_memoryOverviewQueue == null)\n'
                          '\t\t{\n'
                          '\t\t\t_memoryOverviewQueue = new List<MemoryOverviewJob>();\n'
                          '\t\t}\n',
                   'new': '\t\t_memoryBusinessState.EnsureOverviewPersistenceContainers();\n',
                   'symbol': 'EnsureOverviewPersistenceContainers',
                   'old_sha256': '818210267e9c1f2b3ba7e60ffd0c71f3aeb298dc4ffad5d9dcb3141e689055c0'},
                  {'old': '\t\tif (_npcMajorActionSummaries == null)\n'
                          '\t\t{\n'
                          '\t\t\t_npcMajorActionSummaries = new Dictionary<string, '
                          'MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t}\n'
                          '\t\tif (_npcMajorActionSummaryStorage == null)\n'
                          '\t\t{\n'
                          '\t\t\t_npcMajorActionSummaryStorage = new Dictionary<string, '
                          'string>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t}\n'
                          '\t\tif (_npcMajorActionSummaryQueue == null)\n'
                          '\t\t{\n'
                          '\t\t\t_npcMajorActionSummaryQueue = new List<MajorActionSummaryJob>();\n'
                          '\t\t}\n',
                   'new': '\t\t_memoryBusinessState.EnsureMajorSummaryPersistenceContainers();\n',
                   'symbol': 'EnsureMajorSummaryPersistenceContainers',
                   'old_sha256': 'bc1c1733a28b5dfa19128798dfcd2ac2643e2cac8a0d5e178b7460924d346d48'},
                  {'old': '\t\t_dialogueHistory = new Dictionary<string, List<DialogueDay>>();\n'
                          '\t\t_dialogueHistoryStorage = new Dictionary<string, string>();\n'
                          '\t\t_dailyMemoryDrafts = new Dictionary<string, '
                          'List<DailyMemoryDraft>>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t_dailyMemoryDraftStorage = new Dictionary<string, '
                          'string>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t_compressedMemoryBlocks = new Dictionary<string, '
                          'List<CompressedMemoryBlock>>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t_compressedMemoryBlockStorage = new Dictionary<string, '
                          'string>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t_memorySummaryQueue = new List<MemorySummaryJob>();\n',
                   'new': '\t\t_memoryBusinessState.ClearHistoryAndDailyForCurrentSave();\n',
                   'symbol': 'ClearHistoryAndDailyForCurrentSave',
                   'old_sha256': 'b3653ad3c3e636a682bff695f2db76e41979de6946b72b93b59efa9fc9bfe660'},
                  {'old': '\t\t_memoryOverviewStates = new Dictionary<string, '
                          'MemoryOverviewState>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t_memoryOverviewStateStorage = new Dictionary<string, '
                          'string>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t_memoryOverviewQueue = new List<MemoryOverviewJob>();\n',
                   'new': '\t\t_memoryBusinessState.ClearOverviewForCurrentSave();\n',
                   'symbol': 'ClearOverviewForCurrentSave',
                   'old_sha256': '7eadc35baad52f2ddc706fe87780377fe3f8d5922a61f77e605913cbbf25f5ae'},
                  {'old': '\t\t_npcMajorActionSummaries = new Dictionary<string, '
                          'MajorActionSummaryState>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t_npcMajorActionSummaryStorage = new Dictionary<string, '
                          'string>(StringComparer.OrdinalIgnoreCase);\n'
                          '\t\t_npcMajorActionSummaryQueue = new List<MajorActionSummaryJob>();\n',
                   'new': '\t\t_memoryBusinessState.ClearMajorSummaryForCurrentSave();\n',
                   'symbol': 'ClearMajorSummaryForCurrentSave',
                   'old_sha256': '72c84354f197d5424810c2150b93e945253711233a3ac6a1f562edb88ad4a3a5'},
                  {'old': '\t\t_dirtyMemoryOverviewIds.Clear();\n'
                          '\t\t_pendingMemoryOverviewCandidateScanIds.Clear();\n'
                          '\t\t_pendingMemoryOverviewCandidateScanIdSet.Clear();\n',
                   'new': '\t\t_memoryBusinessState.ClearOverviewDiscoveryForCurrentSave();\n',
                   'symbol': 'ClearOverviewDiscoveryForCurrentSave',
                   'old_sha256': '50c9ecc1af72c28af5277559cf43a65c719cb0ef82f2220fc87895019ea4c541'}]}
for p in patch['main_patches']:
 assert main.count(p['new'])==1,p['symbol']
 assert p['old'] not in main,p['symbol']
receipt['seven_production_consumers']='unique_present_old_segments_absent'
clear=ex.declaration(main,'private void ClearAllDataForCurrentSave(')
ordered=['RetireConversationRequestsForDeveloperClear();','ResetMemorySummaryMainThreadActions();','_memoryBusinessState.ClearHistoryAndDailyForCurrentSave();','_memorySummaryQueueJsonStorage = "[]";','_memorySummaryRunOwner.Reset();','ResetMemoryFailureNotices();','_nativeConversationMemorySessionCounter = 0;','_activeNativeConversationMemorySessionId = -1;','_memoryBusinessState.ClearOverviewForCurrentSave();','_memoryOverviewQueueJsonStorage = "[]";','_memoryBusinessState.ClearMajorSummaryForCurrentSave();','_npcMajorActionSummaryQueueJsonStorage = "[]";','ResetDailyMemoryDraftSealSliceState();','_memoryBusinessState.ClearOverviewDiscoveryForCurrentSave();']
positions=[clear.index(x) for x in ordered];assert positions==sorted(positions)
receipt['clear_worker_scratch_native_order']=ordered
sync=ex.declaration(main,'public override void SyncData(')
for call,scratch in [('EnsureHistoryAndDailyPersistenceContainers','_memorySummaryQueueJsonStorage'),('EnsureOverviewPersistenceContainers','_memoryOverviewQueueJsonStorage'),('EnsureMajorSummaryPersistenceContainers','_npcMajorActionSummaryQueueJsonStorage')]:
 assert sync.index('_memoryBusinessState.'+call+'();')<sync.index('if ('+scratch+' == null)')
receipt['sync_scratch_null_order']='three_state_ensure_then_original_scratch_null_boundary'
(out/'receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8');sys.exit(r.returncode)
