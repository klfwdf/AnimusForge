"""Prompt assets and host wiring only; live game/provider behavior is tested separately."""
from pathlib import Path
import json,re,subprocess
root=Path(__file__).resolve().parents[3]
checks=0
def check(ok,name):
 global checks
 assert ok,name
 checks+=1
base=root/'content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData'
pack={x:json.loads((base/f'GcczTownPrompt.{x}.json').read_text(encoding='utf-8-sig')) for x in ['zh-CN','en','en-US']}
keys={k for k in pack['zh-CN']['RuntimeText'] if k.startswith('VillageScene_')}
check(len(keys)==21,'21 scene/persona resources')
for language,body in pack.items():
 check(keys=={k for k in body['RuntimeText'] if k.startswith('VillageScene_')},'key parity '+language)
 for key in keys:
  value=body['RuntimeText'][key]
  check(set(re.findall(r'\{\w+\}',value))==set(re.findall(r'\{\w+\}',pack['zh-CN']['RuntimeText'][key])),'placeholder parity '+key)
  check(not re.search(r'\[(?:VILLAGE_ACTION|VILLAGE_DISPOSE|ACTION):',value),'no executable tag in scene prose '+key)
  if language!='zh-CN':check(not re.search('[\u4e00-\u9fff]',value),'EN fixed text '+key)
 relative=(base/f'GcczTownPrompt.{language}.json').relative_to(root).as_posix()
 old=json.loads(subprocess.check_output(['git','show','1db77a52:'+relative],cwd=root).decode('utf-8-sig'))
 unchanged=json.loads(json.dumps(body))
 for key in keys | {k for k in unchanged['RuntimeText'] if k.startswith('VillageAid_')}:unchanged['RuntimeText'].pop(key)
 check(unchanged==old,'town/castle/existing keys unchanged '+language)
D=root/'src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath'
read=lambda path:path.read_text(encoding='utf-8-sig')
main=read(D/'VillageAftermathBehavior.cs');scope=read(D/'VillageAftermathBehavior.PromptScope.cs')
check('VillageDispositionSceneMissionBehavior.Admits(_activeMission, _activeVillageId)' in main and 'ReferenceEquals(_activeMission, Mission.Current)' in main,'current dedicated mission gate')
check('HasCurrentDispositionPromptSpeaker' in scope and 'agentIndex < 0' in scope and 'ReferenceEquals(character, actual)' in scope and 'ReferenceEquals(hero, actual.HeroObject)' in scope,'real speaker/remote target boundary')
check(main.count('HasCurrentDispositionPromptSpeaker(null, null, targetAgentIndex)')==2,'postprocess rules and context use same speaker boundary')
bridge=read(root/'src/bridges/Siege/Host/AfGcczShoutBridge.cs')
check('VillageAftermathBehavior.BuildRuntimePromptForExternal(targetHero, targetCharacter, targetAgentIndex)' in bridge and 'if (string.IsNullOrWhiteSpace(siegePrompt))' in bridge,'shared conversation bridge respects empty context')
host=read(D/'VillageAftermathBehavior.HostileDialogue.cs')
check('GcczTownPromptResourceProvider.GetCatalog()' in host and 'BuildSpeakerPrompt(profile, role, catalog)' in host,'actual selected package forwarded to scene and persona')
check('MyBehavior.GetNpcPersonaForExternal' in host and 'ShoutUtils.TryGetUnnamedNpcPersona' in host,'actual AF source persona has precedence')
session=read(root/'AnimusForge.SiegeAftermathIntervention/VillageDispositionDialogueSession.cs')
check('_ordinaryVoices.GetOrCreateProfile' in session and '_ordinaryVoices.Begin(_villageId)' in session and '_ordinaryVoices.EndScene()' in session and 'role == VillageDispositionSpeakerRole.Villager || role == VillageDispositionSpeakerRole.PlayerTroop' in session,'exact town cache/factory only for ordinary villagers/soldiers')
check('_profiles.Clear()' in session and '_sceneGeneration++' in session and '_textCatalog = _villageId.Length == 0 ? null' in session,'reset erases cache and retired catalog references')
check('ClearHostileDialogue();' in main.split('private static void ClearMissionState',1)[1] and 'HostileDialogue.End();' in host,'end/load/new-game cleanup owns personas')
check('CanGovern(village) && ResolveHostileSpeakerRole(index, out _) == VillageDispositionSpeakerRole.PlayerCompanion' in read(D/'VillageAftermathBehavior.Governance.cs'),'culture instructions only to eligible accompanying hero')
check('DefaultPrompt' not in session and 'SaveChunked' not in session and 'SyncData' not in session,'no persona persistence path added')
for suffix in ['en','zh-CN']:
 check(f'GcczTownPrompt.{suffix}.json' in read(root/'AnimusForge.csproj') and f'GcczTownPrompt.{suffix}.json' in read(root/'content/content-map.json'),'existing embedded/stage map '+suffix)
escort=read(D/'SiegeAiInterventionBehavior.SharedEscorts.cs')
town=read(D/'SiegeAiInterventionBehavior.cs')
sets=read(root/'src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.cs')
scene=read(D/'VillageDispositionSceneMissionBehavior.cs')
check('BuildInterventionEscortData(troop, origin, team, position, spawnDirection' in town and 'SiegeAiInterventionBehavior.BuildInterventionEscortData(troop, origin, team, position, direction' in sets,'town and village invoke the exact same native troop builder')
check('if (_activateVillageAftermath && !asEnemy)' in sets and 'if (!_activateVillageAftermath) MaintainProtectedFollowersFriendlyState();' in sets,'ordinary SETS keeps its own spawn/protection; admitted village uses town escort state')
check('FollowVillageEscortFormationForExternal(mission);' in sets and 'TrySetPlayerFormationFollowOrder(FormationClass.Infantry' in escort,'village follows through real town formation function')
check('if (!IsInterventionEscortScene(mission)) return 0;' in town and '!IsInterventionEscortScene(mission) || mission == null' in town,'both external and inner banner entry accept only current admitted scene')
check('VillageAftermathBehavior.IsActiveForMission(mission) ? selectedRoster : _selectedInterventionRoster' in town,'village banner selection never inherits stale town roster')
check('ReleaseVillageEscortsForExternal(Mission);' in scene and 'SelectedRoster = null;' in scene,'scene end releases shared indices and selected roster')
check('AlliedAgentIndexes.Clear()' not in escort and 'AlliedAgentIndexes.Remove(pair.Key)' in escort,'shared cleanup does not clear unrelated town/castle state')
check('CultureRuleContext()' not in read(D/'VillageAftermathBehavior.Governance.cs').split('private static List<PostprocessRuleEntry> BuildFriendlyRules',1)[1].split('private static string BuildFriendlyPrompt',1)[0] and 'CultureRuleContext()' in main,'culture stage appears once in postprocess context, not each action')
print(f'PASS {checks} prompt resource and host wiring checks; not a live game/LLM acceptance.')
