"""Replay exact owner append/sanitize receipts without the unrelated summary/UI closure."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, re, subprocess, sys
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
sys.path.insert(0, str(ROOT / 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
from business_owner_fixture_support import enable_expression_declarations

p = argparse.ArgumentParser()
p.add_argument('--run-root', type=Path)
p.add_argument('--dotnet')
a = p.parse_args()
out = new_run_root(ROOT, 'conversation-rewrite-turn', a.run_root)
dotnet = resolve_dotnet(ROOT, a.dotnet)
spec = importlib.util.spec_from_file_location('extract', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
ex = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ex)
enable_expression_declarations(ex)
manifest = []

def source(relative):
    text = (ROOT / relative).read_text(encoding='utf-8-sig')
    manifest.append({'file': relative, 'sha256': hashlib.sha256(text.encode()).hexdigest()})
    return text

def members(text, signatures):
    return '\n'.join(ex.declaration(text, signature) for signature in signatures)

imports = 'using System;using System.Linq;using System.Collections.Generic;using System.Text;using System.Text.RegularExpressions;using System.Security.Cryptography;using TaleWorlds.Library;'
business = source('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs')
identity = source('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.IdentityStores.cs')
weekly = source('src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.WeeklyTriggers.cs')
models = source('src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs')
owner = 'internal sealed class MemoryBusinessStateOwner {'
owner += re.search(r'internal Dictionary<string, List<DailyMemoryDraft>> Drafts = [^;]+;', business).group()
owner += re.search(r'internal List<WeeklyMemoryMaterialTrigger> PendingWeeklyTriggers = [^;]+;', business).group()
owner += re.search(r'internal const string NonHeroMemoryIdPrefix = [^;]+;', business).group()
owner += re.search(r'internal string _memoryRuntimeSessionKey = [^;]+;', identity).group()
owner += members(business, ['internal List<DailyMemoryDraft> LoadDrafts(', 'internal void SaveDrafts(', 'internal bool IsDailyMemoryLinePublished(', 'internal bool AppendDailyMemoryLineById(', 'private void CaptureRewriteDailyLine(', 'internal static int CountDailyMemoryDraftLines(', 'internal static bool IsNonHeroMemoryId('])
owner += members(identity, ['internal string BuildCurrentMemorySessionKey('])
owner += members(weekly, ['internal void AddWeeklyTrigger(', 'internal void AttachPendingWeeklyTriggers(', 'internal void PrunePendingWeeklyTriggers(']) + '}'
owner += ex.declaration(business, 'internal sealed class MemoryDailyAppendCapabilities')
model_types = members(models, ['internal sealed class DailyMemoryLine', 'internal sealed class DailyMemoryDraft', 'internal sealed class WeeklyMemoryMaterialTrigger'])
rules = 'internal static class MemoryRecordRules {' + members(models, [
    'internal static List<DailyMemoryDraft> SanitizeDailyMemoryDrafts(',
    'internal static void BindDailyMemoryDraftWeeklyTrigger(',
    'internal static DailyMemoryLine SanitizeDailyMemoryDraftLine(',
    'internal static DailyMemoryDraft SanitizeDailyMemoryDraftEntry(',
    'internal static List<WeeklyMemoryMaterialTrigger> SanitizeWeeklyMemoryMaterialTriggers(',
    'internal static string NormalizeMemoryHeroId(',
    'internal static string NormalizeWeeklyMemoryMaterialTagText(',
    'internal static List<string> ExtractWeeklyMemoryMaterialTags(',
    'internal static List<string> NormalizeWeeklyMemoryMaterialTags(',
    'internal static string BuildWeeklyMemoryMaterialTriggerStableKey(',
    'internal static string ComputeWeeklyMemoryMaterialHash(']) + '}'
host = source('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs')
recovery = source('src/modules/AF.Module.Memory/Summary/MemoryRecoveryStateOwner.cs')
host_types = 'internal sealed class MyBehavior {' + ex.declaration(host, 'internal class DialogueDay')
host_types += members(recovery, ['internal static bool IsValidMemoryCommitMarker(', 'internal static bool IsMemoryRecoveryHexDigest(']) + '}'
ledger = source('src/modules/AF.Module.Memory/Records/DialogueHistoryLedger.cs')
ledger_methods = 'internal static class DialogueHistoryLedger {' + members(ledger, ['internal static List<(int Day, string Date, string Line)> Flatten<TDay>(', 'internal static List<TDay> Regroup<TDay>(']) + '}'
commit = source('src/AF.GameAdapter.Bannerlord/Memory/MemoryHistoryCommitBannerlordAdapter.cs')
assert commit.count('AppendRewriteableDialogueLine(normalizedMemoryId, dayIndex, dialogueDay,') == 2
commit_methods = members(commit, ['private void AppendRewriteableDialogueLine(', 'private void CaptureRewriteableDialogueLine('])
probe = 'internal sealed class RewriteHistoryCommitOwnerProbe {internal Dictionary<string,List<MyBehavior.DialogueDay>> _dialogueHistory=new();internal void Append(string id,int dayIndex,string text,bool player){if(!_dialogueHistory.TryGetValue(id,out var days))_dialogueHistory[id]=days=new();var day=days.FirstOrDefault(x=>x.GameDayIndex==dayIndex);if(day==null){day=new MyBehavior.DialogueDay{GameDayIndex=dayIndex};days.Add(day);}AppendRewriteableDialogueLine(id,dayIndex,day,text,player);}' + commit_methods + '}'
program = ROOT / 'tests/modules/AF.Module.Conversation/ConversationRewriteTurnTests/Program.cs'
source(str(program.relative_to(ROOT)))
(out / 'Probe.cs').write_text(imports + 'using DialogueDay=AnimusForge.MyBehavior.DialogueDay;namespace AnimusForge {' + owner + model_types + rules + host_types + ledger_methods + probe + '}', encoding='utf-8')
(out / 'ControlledEngine.cs').write_text('namespace TaleWorlds.Library { public static class TWParallel { public static bool IsMainThread()=>true; } public static class MBMath { public static int ClampInt(int x,int min,int max)=>System.Math.Clamp(x,min,max); } }', encoding='utf-8')
receipt = ROOT / 'src/modules/AF.Module.Conversation/Internal/History/ConversationRewriteTurn.cs'
items = ''.join('<Compile Include="' + escape(str(path)) + '" />' for path in [out / 'Probe.cs', out / 'ControlledEngine.cs', receipt, program])
(out / 'Tests.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Tests.csproj'), '-c', 'Release'], cwd=out, env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=120)
(out / 'run.log').write_text(result.stdout + result.stderr, encoding='utf-8')
(out / 'receipt.json').write_text(json.dumps({'sources': manifest, 'receipt_sha256': hashlib.sha256(receipt.read_bytes()).hexdigest(), 'exit_code': result.returncode, 'scope': 'exact production daily append, sanitizer, weekly attachment, recent-history append helper and regroup; controlled main-thread clock/input leaves; no live save/game'}, indent=2), encoding='utf-8')
print(result.stdout + result.stderr)
print('OUTPUT', out)
raise SystemExit(result.returncode)
