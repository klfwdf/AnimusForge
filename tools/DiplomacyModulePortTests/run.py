"""DPL-100: real bridges/adapters against recording engine owners, plus exact caller inverse."""
from pathlib import Path
import argparse, importlib.util, subprocess, re
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
BASELINE='23f4d467'
def read(p): return (ROOT/p).read_text(encoding='utf-8-sig')
def old(p): return subprocess.check_output(['git','show',BASELINE+':'+p],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n').replace('\r','\n')
def load(name,p):
 spec=importlib.util.spec_from_file_location(name,ROOT/p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
util=load('port_util','tools/ModuleFrameworkApiTests/run.py')
declaration=load('port_decl','tools/ChannelCutoverBoundaryTests/run.py').declaration
SOURCES=['Refactor/Contracts/'+n+'.cs' for n in ['DiplomacyModulePorts','AfTributePowerContext','WorldDiplomacyPolicySignalSnapshot','WorldDiplomacyPresentationPort','WorldDiplomacyPresentationContracts','WorldDiplomacyTimelineQueryContracts','WorldDiplomacyTimelineDocumentQueryContracts','WorldDiplomacyDocumentReadCommandContracts']]
SOURCES += ['src/bridges/Diplomacy/'+n+'.cs' for n in ['DiplomacyConversationBridge','DiplomacyPolicyObservationBridge','DiplomacyModuleServices']]
SOURCES += ['src/modules/AF.Module.Diplomacy/Adapters/'+n+'.cs' for n in ['DiplomacyConversationModuleAdapter','WorldDiplomacyModuleAdapter','DiplomacyIdentityResolver']]
SOURCES += ['Refactor/Adapters/'+n+'Adapter.cs' for n in ['WorldDiplomacyTimelineRevisionQuery','WorldDiplomacyTimelineDocumentQuery','WorldDiplomacyDocumentReadCommand']]
def boundaries():
 paths=['AIConfigHandler.cs','ShoutBehavior.cs','ShoutBehavior.NativeTurnCommit.cs','DiplomacyPeaceTermsService.cs','NpcTributeVassalageBehavior.cs','src/modules/AF.Module.Social/Proactive/ProactiveCandidateQualification.cs']
 paths += ['src/modules/AF.Module.Conversation/Channels/'+p for p in ['Scene/ShoutBehavior.ScenePostprocess.cs','Scene/ShoutBehavior.SceneConversationChains.cs','Courier/CourierDeliveryBehavior.DomainCommit.cs','Courier/CourierDeliveryBehavior.DeliveryLifetime.cs']]
 count=0
 for p in paths:
  current=read(p);prior=old(p); count+=current.count('DiplomacyConversationBridge.')
  for name in ['CanDiscussWorldDiplomacyForExternal','TryBuildProactiveDiscussionForExternal']:
   current=current.replace('DiplomacyConversationBridge.'+name,'WorldDiplomacyBehavior.'+name)
  current=current.replace('DiplomacyConversationBridge.','DiplomacyBehavior.')
  if p=='NpcTributeVassalageBehavior.cs':
   block=declaration(prior,'internal readonly struct AfTributePowerContext')
   assert declaration(read('Refactor/Contracts/AfTributePowerContext.cs'),'internal readonly struct AfTributePowerContext')==block
   prior=prior.replace(block+'\n\n','')
  assert current.strip()==prior.strip(), 'Caller guard/order/argument drift: '+p
 world='src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs'
 assert read(world).replace('DiplomacyModuleServices.Policy.','WorldDiplomacyPolicyContext.')==load('retired','tools/DiplomacyArchitectureTests/retired.py').remove_retired(old(world),declaration),'Policy cadence/state owner changed'
 policy='PolicySystem/Context/WorldDiplomacyPolicyContext.cs'
 before=old(policy);after=read(policy)
 ledger=declaration(before,'internal sealed class PublishedPolicyArtifactLedgerEntry')
 assert ledger==declaration(read('Refactor/Contracts/PublishedPolicyArtifactLedgerEntry.cs'),'internal sealed class PublishedPolicyArtifactLedgerEntry')
 before=before[:before.index('/// <summary>')]+before[before.index(ledger)+len(ledger):].lstrip('\n')
 snapshot=declaration(before,'internal sealed class WorldDiplomacyPolicySignalSnapshot')
 before=before.replace(snapshot,'').strip()
 start=before.index('result.Add(new WorldDiplomacyPolicySignalSnapshot')
 end=before.index('});',start)+3
 old_block=before[start:end]
 fields=re.findall(r'^\s*(\w+) = (.*?)(?:,)?$',old_block,re.M)
 new_block='result.Add(new WorldDiplomacyPolicySignalSnapshot(\n'+',\n'.join('\t\t\t\t\t\t'+value.rstrip(',') for _,value in fields)+'\n\t\t\t\t\t));'
 assert before.replace(old_block,new_block)==after.strip(),'Policy producer/cache/ordering drift'
 value_type=read('Refactor/Contracts/WorldDiplomacyPolicySignalSnapshot.cs')
 for name,_ in fields:
  assert name+' = '+name[0].lower()+name[1:]+';' in value_type,'Snapshot value lost: '+name
 for p in ['src/AF.GameAdapter.Bannerlord/Composition/ApplicationTickComposition.cs','src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs']:
  restored=read(p).replace('DiplomacyModuleServices.World.OnEngineTick()', 'WorldDiplomacyBehavior.Instance?.OnEngineTick()').replace('DiplomacyModuleComposition.RegisterPatches(harmony)','WorldDiplomacyBehavior.RegisterHarmonyPatches(harmony)')
  assert restored==old(p),'Lifecycle order/guard drift: '+p
 for p in ['src/bridges/Diplomacy/DiplomacyConversationBridge.cs','src/bridges/Diplomacy/DiplomacyPolicyObservationBridge.cs']:
  assert not any(s in read(p) for s in ['foreach (','Regex','Campaign.Current','_af_world_diplomacy_v1','new Dictionary','lock (']), 'Bridge owns business/state: '+p
 print(f'PASS exact inverse: {len(paths)} caller files / {count} routes; channel guards/ref/out/order unchanged; policy cadence and tick/patch guards unchanged')

def main():
 p=argparse.ArgumentParser();p.add_argument('--dotnet',default='dotnet');a=p.parse_args();boundaries()
 out=HERE/'.generated/current';out.mkdir(parents=True,exist_ok=True)
 (out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
 # Compile the existing immutable ledger DTO verbatim, never a hand-maintained mirror.
 entry=declaration(read('Refactor/Contracts/PublishedPolicyArtifactLedgerEntry.cs'),'internal sealed class PublishedPolicyArtifactLedgerEntry')
 (out/'LedgerEntry.cs').write_text('namespace AnimusForge;\n'+entry,encoding='utf-8')
 common=[HERE/'HostStubs.cs',HERE/'Program.cs',out/'LedgerEntry.cs']
 variants=[
  ('current',None),
  ('wrong_target',('src/bridges/Diplomacy/DiplomacyConversationBridge.cs','(hero ?? character?.HeroObject)?.StringId','character?.HeroObject?.StringId')),
  ('swap_tribute',('src/modules/AF.Module.Diplomacy/Adapters/DiplomacyConversationModuleAdapter.cs','ResolveKingdom(payerId), ResolveKingdom(receiverId)','ResolveKingdom(receiverId), ResolveKingdom(payerId)')),
  ('wrong_executor',('src/modules/AF.Module.Diplomacy/Adapters/DiplomacyConversationModuleAdapter.cs','ProcessDiplomacyTagsDispatch(ResolveHero(heroId), ref text)','ProcessDiplomacyTagsDispatch(null, ref text)')),
  ('drop_tick',('src/modules/AF.Module.Diplomacy/Adapters/WorldDiplomacyModuleAdapter.cs','WorldDiplomacyBehavior.Instance?.OnEngineTick()','System.GC.KeepAlive(null)'))]
 for name,mutation in variants:
  folder=out/name;folder.mkdir(exist_ok=True);sources=[ROOT/s for s in SOURCES]
  if mutation:
   path,before,after=mutation; text=read(path);assert before in text
   target=folder/Path(path).name;target.write_text(text.replace(before,after),encoding='utf-8');sources[sources.index(ROOT/path)]=target
  proj=util.project(folder,'DiplomacyPortChecks',sources+common,executable=True)
  code,log=util.run_dotnet(a.dotnet,['run','--project',str(proj),'-c','Release'],out)
  (folder/'run.log').write_text(log,encoding='utf-8')
  if not mutation: print(log,end='');assert code==0,'Port execution failed'
  else: assert code!=0 and 'FAIL ' in log and 'error CS' not in log,log;print('PASS behavioral mutation rejected: '+name)
if __name__=='__main__':main()
