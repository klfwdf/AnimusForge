from pathlib import Path
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
AF2_FIXTURE_METADATA = {"sourceClass": "current-owner-replay-with-explicit-legacy-option", "currentOwnerReplayProjected": False}
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path
import os
import argparse,importlib.util,subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--run-root',type=Path);p.add_argument('--dotnet', default=(os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[4] / "local/dotnet/8.0.425/dotnet.exe")));p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['worker_capture','worker_commit','ignore_edit','stale_lease','release_new','drop_voice','false_queued_success','promoted_worker_commit']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
spec=importlib.util.spec_from_file_location('util',ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
s=subprocess.check_output(['git','show','10defeb4:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
if a.original:
 code=code.replace('@@GENERATOR@@','\n'.join(ex.declaration(s,sig) for sig in ['private async Task<string> GenerateNpcPersonaAsync(','private async Task EnsureNpcPersonaGeneratedAsync(','public static async Task EnsureNpcPersonaGeneratedForExternalAsync('])).replace('@@RESET@@','_npcPersonaAutoGenInFlight.Clear();_npcPersonaAutoGenRetryAfterUtcTicks.Clear()').replace('@@ACTIVE@@','return _npcPersonaAutoGenInFlight.Contains(id);').replace('@@COOLING@@','return _npcPersonaAutoGenRetryAfterUtcTicks.ContainsKey(id);')
else:
 code=code.replace('@@GENERATOR@@','').replace('@@RESET@@','_npcPersonaGeneration.Reset()').replace('@@ACTIVE@@','_npcPersonaGeneration.GetState(id,out bool active,out _);return active;').replace('@@COOLING@@','_npcPersonaGeneration.GetState(id,out _,out bool cooling);return cooling;')
ui_source=s if a.original else (ROOT/'src/AF.GameAdapter.Bannerlord/UI/Editors/PersonaEditorController.cs').read_text(encoding='utf-8-sig')
ui_marker='private async Task RunHeroPersonaRerollAsync(' if a.original else 'internal async Task RunHeroPersonaRerollAsync('
code=code.replace('@@REROLL_UI@@',ex.declaration(ui_source,ui_marker))
code=code.replace('@@PROMOTED_RESPONSE@@', 'return response.Task;' if a.original else 'SkillCalls++;return skillResponse.Task;')
code=code.replace('@@PROMOTED_HELPERS@@', '' if a.original else (HERE/'PromotedHelpers.cs.txt').read_text(encoding='utf-8')).replace('@@PROMOTED_TESTS@@', '' if a.original else (HERE/'PromotedTests.cs.txt').read_text(encoding='utf-8')).replace('@@RUN_PROMOTED@@', '' if a.original else 'PromotedCases();')
code=code.replace('@@RESERVATION_TESTS@@','' if a.original else (HERE/'Reservations.cs.txt').read_text(encoding='utf-8'))
code=code.replace('@@TEXT_RULE_CASES@@','' if a.original else (HERE/'TextRules.cs.txt').read_text(encoding='utf-8')).replace('@@RUN_TEXT_RULES@@','' if a.original else 'TextRuleCases();')
code=code.replace('@@LORE_TESTS@@','' if a.original else (HERE/'LoreTests.cs.txt').read_text(encoding='utf-8')).replace('@@RUN_LORE@@','' if a.original else 'LoreCases();')
out=util.new_run_root(ROOT,'HeroPersonaGenerationTests',a.run_root)
if not a.original:
 # Keep the original behavioral oracle; seed the current sole profiles owner and real applications.
 code=code.replace('private sealed class ApiCallResult','internal sealed class ApiCallResult').replace('private sealed class NpcPersonaProfile','internal sealed class NpcPersonaProfile')
 code=code.replace('private NpcPersonaProfile profile=new();','private readonly PersonaProfileStateOwner _personaProfiles=new(); private NpcPersonaProfile profile {get=>_personaProfiles.Get("hero",true);set=>_personaProfiles.Profiles["hero"]=value;}')
 code=code.replace('private Task<ApiCallResult> CallAuxiliaryGatewayDetailed(', 'internal Task<ApiCallResult> CallAuxiliaryGatewayDetailed(')
 code=code.replace('private static Hero FindHeroById(', 'internal static Hero FindHeroById(')
 code=code.replace('private static string BuildHeroFactsForPersonaGeneration(', 'internal static string BuildHeroFactsForPersonaGeneration(').replace('private static string BuildPromotedNonHeroCompanionFactsForPersonaGeneration(', 'internal static string BuildPromotedNonHeroCompanionFactsForPersonaGeneration(')
 code=code.replace('private string BuildPromotedHeroSkillSummary(', 'internal string BuildPromotedHeroSkillSummary(').replace('private bool TryApplyPromotedHeroSkillJson(', 'internal bool TryApplyPromotedHeroSkillJson(')
 # The old deterministic network fixture uses P|B; expose those same fields in the real parser's wire schema.
 old='response.TrySetResult(new ApiCallResult{Content=text,Success=success,ErrorMessage="transport failed"})'
 new='response.TrySetResult(new ApiCallResult{Content=PersonaFixtureWire(text),Success=success,ErrorMessage="transport failed"})'
 assert code.count(old)==1;code=code.replace(old,new)
 code=code.replace('private Task<bool> UnsafeDirect(', 'private static string PersonaFixtureWire(string text){var parts=(text??"").Split((char)124);return parts.Length==2?Newtonsoft.Json.JsonConvert.SerializeObject(new {personality=parts[0],background=parts[1]}):text;} private Task<bool> UnsafeDirect(')
 glue='private void StampNpcPersonaProfile(string id,NpcPersonaProfile p){Probe.Read();Saves++;}'
 code=code.replace('@@CURRENT_GLUE@@',glue) if '@@CURRENT_GLUE@@' in code else code.replace('internal void Tick()=>dispatcher.Tick();',glue+' internal void Tick()=>dispatcher.Tick();')
 code+='namespace TaleWorlds.CampaignSystem.Settlements {} namespace AnimusForge {internal static class MemoryEntityIdentityBannerlordAdapter {internal static TaleWorlds.CampaignSystem.Hero FindHeroById(string id)=>MyBehavior.FindHeroById(id);} internal static class CampaignCharacterRecordCaptureAdapter {internal static string BuildPromotedHeroSkillSummary(TaleWorlds.CampaignSystem.Hero hero)=>MyBehavior.Instance.BuildPromotedHeroSkillSummary(hero);internal static bool TryApplyPromotedHeroSkillJson(TaleWorlds.CampaignSystem.Hero hero,string json)=>MyBehavior.Instance.TryApplyPromotedHeroSkillJson(hero,json);}} namespace AnimusForge.Refactor.Adapters {internal static class ConfiguredChatApplicationAdapter {internal static Task<MyBehavior.ApiCallResult> CallAuxiliaryGatewayDetailed(string sys,string user,string source,int n,bool forceThinkingDisabled)=>MyBehavior.Instance.CallAuxiliaryGatewayDetailed(sys,user,source,n,forceThinkingDisabled);} internal static class PersonaGenerationFactCaptureAdapter {internal static string BuildHeroFactsForPersonaGeneration(TaleWorlds.CampaignSystem.Hero h)=>MyBehavior.BuildHeroFactsForPersonaGeneration(h);internal static string BuildPromotedNonHeroCompanionFactsForPersonaGeneration(TaleWorlds.CampaignSystem.Hero h,string n,string f,string t,string id,string c,string s,string j,string e)=>MyBehavior.BuildPromotedNonHeroCompanionFactsForPersonaGeneration(h,n,f,t,id,c,s,j,e);}}'
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
files=[out/'Program.cs',ROOT/'src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs',ROOT/'src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs']
if not a.original:
 helper=(current_source_path(ROOT, 'MyBehavior.PersonaGeneration.cs')).read_text(encoding='utf-8-sig');owner=(ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaGenerationOwner.cs').read_text(encoding='utf-8-sig')
 application=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/NpcPersonaGenerationApplicationAdapter.cs').read_text(encoding='utf-8-sig')
 # A separate sibling class is Kingdom naming capture, not Persona application.
 application=application[:application.index('internal static class RebelNamingPromptSummaryCaptureAdapter')]
 if a.mutate in ('worker_capture','worker_commit'):
  part='bool captured' if a.mutate=='worker_capture' else 'bool accepted'
  begin=application.index(part);old='_dispatch(generation, () =>';assert old in application[begin:];application=application[:begin]+application[begin:].replace(old,'UnsafeDirect(() =>',1)
  application=application.replace('internal sealed class NpcPersonaGenerationApplicationAdapter\n{','internal sealed class NpcPersonaGenerationApplicationAdapter\n{\nprivate static Task<bool> UnsafeDirect(Func<bool> action)=>Task.FromResult(action());',1)
 policy=(ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaProfilePolicy.cs').read_text(encoding='utf-8-sig')
 if a.mutate=='ignore_edit':policy=policy.replace('if (overwriteExisting && (!string.Equals(currentPersonality','if (false && (!string.Equals(currentPersonality',1)
 if a.mutate=='stale_lease':application=application.replace('!_reservations.IsCurrent(work.Reservation)','false',1)
 if a.mutate=='release_new':owner=owner.replace('|| !ReferenceEquals(current, lease)) return;','|| false) return;',1)
 if a.mutate=='false_queued_success':application=application.replace('return overwriteExisting ? "请求已失效，未保存新的人设。" : "";', 'return "";',1).replace('return accepted ? failure ?? "" : overwriteExisting ? "请求已失效，未保存新的人设。" : "";', 'return accepted ? failure ?? "" : "";',1)
 if a.mutate=='drop_voice':application=application.replace('VoiceId = (current.VoiceId ?? "").Trim()','VoiceId = ""',1)
 (out/'PersonaApplication.cs').write_text(application,encoding='utf-8');files.append(out/'PersonaApplication.cs')
 (out/'Persona.cs').write_text(helper,encoding='utf-8');(out/'Owner.cs').write_text(owner,encoding='utf-8');files += [out/'Persona.cs',out/'Owner.cs']
 promoted=(current_source_path(ROOT, 'MyBehavior.PromotedPersonaGeneration.cs')).read_text(encoding='utf-8-sig')
 promoted_application=(ROOT/'src/AF.GameAdapter.Bannerlord/Prompt/PromotedPersonaGenerationApplicationAdapter.cs').read_text(encoding='utf-8-sig')
 if a.mutate=='promoted_worker_commit':
  begin=promoted_application.index('bool committed');old='_dispatch(runtimeGeneration, () =>';assert old in promoted_application[begin:];promoted_application=promoted_application[:begin]+promoted_application[begin:].replace(old,'UnsafeDirect(() =>',1)
  promoted_application=promoted_application.replace('internal sealed class PromotedPersonaGenerationApplicationAdapter\n{','internal sealed class PromotedPersonaGenerationApplicationAdapter\n{\nprivate static Task<bool> UnsafeDirect(Func<bool> action)=>Task.FromResult(action());',1)
 (out/'PromotedApplication.cs').write_text(promoted_application,encoding='utf-8');files += [out/'PromotedApplication.cs',ROOT/'src/modules/AF.Module.Persona/Profiles/PersonaProfileStateOwner.cs']
 (out/'Promoted.cs').write_text(promoted,encoding='utf-8');files.append(out/'Promoted.cs')
if not a.original:
 (out/'Policy.cs').write_text(policy,encoding='utf-8');files.append(out/'Policy.cs')
 files += [ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaTextRules.cs',ROOT/'src/modules/AF.Module.Llm/Protocol/JsonResponseTextCodec.cs']
# Bind exact whole application classes, scalar game/network fixtures, and generated mutation bytes.
import json,hashlib
physical=['src/AF.GameAdapter.Bannerlord/Prompt/NpcPersonaGenerationApplicationAdapter.cs','src/AF.GameAdapter.Bannerlord/Prompt/PromotedPersonaGenerationApplicationAdapter.cs','src/modules/AF.Module.Persona/Profiles/PersonaProfileStateOwner.cs','src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PersonaGeneration.cs','src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.PromotedPersonaGeneration.cs','src/modules/AF.Module.Persona/Generation/NpcPersonaProfilePolicy.cs','src/modules/AF.Module.Persona/Generation/NpcPersonaGenerationOwner.cs','src/modules/AF.Module.Persona/Generation/NpcPersonaTextRules.cs']
physical += [str(path.relative_to(ROOT)).replace('\\','/') for path in HERE.iterdir() if path.name=='run.py' or path.suffix=='.txt']
(out/'source-manifest.json').write_text(json.dumps({'mutation':a.mutate,'actualCurrent':{path:hashlib.sha256((ROOT/path).read_bytes()).hexdigest() for path in physical},'compiled':{str(path):hashlib.sha256(path.read_bytes()).hexdigest() for path in files},'scope':'whole two Persona applications + soleProfile/Reservation/realDispatcher; only game/provider/skills boundaries controlled; sibling Kingdom naming excluded'},indent=2),encoding='utf-8')
project=util.project(out,'HeroPersonaProof',files,executable=True)
if not a.original:
 newtonsoft=Path(os.environ.get('AF_NEWTONSOFT') or str(Path(a.dotnet).parent/'sdk/8.0.425/Newtonsoft.Json.dll'))
 if not newtonsoft.is_file(): raise SystemExit('Missing existing Newtonsoft.Json.dll: '+str(newtonsoft))
 from xml.sax.saxutils import escape
 xml=project.read_text(encoding='utf-8').replace('</ItemGroup>', '<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newtonsoft))+'</HintPath></Reference></ItemGroup>')
 project.write_text(xml,encoding='utf-8')
code,log=util.run_dotnet(a.dotnet,['run','--project',str(project),'-c','Release'],out)
(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
