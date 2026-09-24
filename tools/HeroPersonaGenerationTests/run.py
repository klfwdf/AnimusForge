from pathlib import Path
import argparse,importlib.util,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
p=argparse.ArgumentParser();p.add_argument('--dotnet', default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe');p.add_argument('--original',action='store_true');p.add_argument('--mutate',choices=['worker_capture','worker_commit','ignore_edit','stale_lease','release_new','drop_voice','false_queued_success','promoted_worker_commit']);a=p.parse_args()
spec=importlib.util.spec_from_file_location('ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
spec=importlib.util.spec_from_file_location('util',ROOT/'tools/ModuleFrameworkApiTests/run.py');util=importlib.util.module_from_spec(spec);spec.loader.exec_module(util)
s=subprocess.check_output(['git','show','10defeb4:MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig')
if a.original:
 code=code.replace('@@GENERATOR@@','\n'.join(ex.declaration(s,sig) for sig in ['private async Task<string> GenerateNpcPersonaAsync(','private async Task EnsureNpcPersonaGeneratedAsync(','public static async Task EnsureNpcPersonaGeneratedForExternalAsync('])).replace('@@RESET@@','_npcPersonaAutoGenInFlight.Clear();_npcPersonaAutoGenRetryAfterUtcTicks.Clear()').replace('@@ACTIVE@@','return _npcPersonaAutoGenInFlight.Contains(id);').replace('@@COOLING@@','return _npcPersonaAutoGenRetryAfterUtcTicks.ContainsKey(id);')
else:
 code=code.replace('@@GENERATOR@@','').replace('@@RESET@@','_npcPersonaGeneration.Reset()').replace('@@ACTIVE@@','_npcPersonaGeneration.GetState(id,out bool active,out _);return active;').replace('@@COOLING@@','_npcPersonaGeneration.GetState(id,out _,out bool cooling);return cooling;')
ui_source=s if a.original else (ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig')
code=code.replace('@@REROLL_UI@@',ex.declaration(ui_source,'private async Task RunHeroPersonaRerollAsync('))
code=code.replace('@@PROMOTED_RESPONSE@@', 'return response.Task;' if a.original else 'SkillCalls++;return skillResponse.Task;')
code=code.replace('@@PROMOTED_HELPERS@@', '' if a.original else (HERE/'PromotedHelpers.cs.txt').read_text(encoding='utf-8')).replace('@@PROMOTED_TESTS@@', '' if a.original else (HERE/'PromotedTests.cs.txt').read_text(encoding='utf-8')).replace('@@RUN_PROMOTED@@', '' if a.original else 'PromotedCases();')
code=code.replace('@@RESERVATION_TESTS@@','' if a.original else (HERE/'Reservations.cs.txt').read_text(encoding='utf-8'))
out=HERE/'.generated'/('original' if a.original else a.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
(out/'Program.cs').write_text(code,encoding='utf-8');(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
files=[out/'Program.cs',ROOT/'src/modules/AF.Module.Memory/Summary/MemorySummaryDispatcher.cs',ROOT/'src/modules/AF.Module.Memory/Summary/IMemorySummaryDispatchHost.cs']
if not a.original:
 helper=(ROOT/'MyBehavior.PersonaGeneration.cs').read_text(encoding='utf-8-sig');owner=(ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaGenerationOwner.cs').read_text(encoding='utf-8-sig')
 if a.mutate in ('worker_capture','worker_commit'):
  part='bool captured' if a.mutate=='worker_capture' else 'bool accepted'
  begin=helper.index(part);helper=helper[:begin]+helper[begin:].replace('RunMemorySummaryCompletionAsync(generation, () =>','UnsafeDirect(() =>',1)
 policy=(ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaProfilePolicy.cs').read_text(encoding='utf-8-sig')
 if a.mutate=='ignore_edit':policy=policy.replace('if (overwriteExisting && (!string.Equals(currentPersonality','if (false && (!string.Equals(currentPersonality',1)
 if a.mutate=='stale_lease':helper=helper.replace('!_npcPersonaGeneration.IsCurrent(work.Reservation)','false',1)
 if a.mutate=='release_new':owner=owner.replace('|| !ReferenceEquals(current, lease)) return;','|| false) return;',1)
 if a.mutate=='false_queued_success':helper=helper.replace('return overwriteExisting ? "请求已失效，未保存新的人设。" : "";', 'return "";',1).replace('return accepted ? failure ?? "" : overwriteExisting ? "请求已失效，未保存新的人设。" : "";', 'return accepted ? failure ?? "" : "";',1)
 if a.mutate=='drop_voice':helper=helper.replace('VoiceId = (current.VoiceId ?? "").Trim()','VoiceId = ""',1)
 (out/'Persona.cs').write_text(helper,encoding='utf-8');(out/'Owner.cs').write_text(owner,encoding='utf-8');files += [out/'Persona.cs',out/'Owner.cs']
 promoted=(ROOT/'MyBehavior.PromotedPersonaGeneration.cs').read_text(encoding='utf-8-sig')
 if a.mutate=='promoted_worker_commit':
  begin=promoted.index('bool committed');promoted=promoted[:begin]+promoted[begin:].replace('RunMemorySummaryCompletionAsync(runtimeGeneration, () =>','UnsafeDirect(() =>',1)
 (out/'Promoted.cs').write_text(promoted,encoding='utf-8');files.append(out/'Promoted.cs')
if not a.original:
 (out/'Policy.cs').write_text(policy,encoding='utf-8');files.append(out/'Policy.cs')
project=util.project(out,'HeroPersonaProof',files,executable=True)
code,log=util.run_dotnet(a.dotnet,['run','--project',str(project),'-c','Release'],out)
(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');raise SystemExit(code)
