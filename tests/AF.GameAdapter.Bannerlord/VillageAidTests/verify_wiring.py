"""Focused aid wiring and paired resource checks; not native save/UI/provider execution."""
from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[3]
read=lambda p:(root/p).read_text(encoding='utf-8-sig')
d='src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath/'
gov=read(d+'VillageAftermathBehavior.Governance.cs'); main=read(d+'VillageAftermathBehavior.cs'); aid=read(d+'VillageAftermathBehavior.Aid.cs')
checks=0
def check(ok,label):
 global checks
 assert ok,label
 checks+=1
check('FriendlyVillageActionCatalog.NormalizeInvocations(raw, allowed)' in main,'normalizer preserves arguments behind base eligibility')
check('ExtractInvocations(text)' in gov and 'OpenVillageGift(invocation)' in gov,'executor receives same parsed invocation')
check('if (!IsActive() || !direct || !CanEnterFriendlyVillage(ResolveActiveVillage()))' in gov and 'FriendlyActionEnabled(action, index)' in gov,'dedicated mission, faction, direct reply and speaker gating retained')
check('HasCurrentDispositionPromptSpeaker(null, null, targetAgentIndex)' in main,'prompt/rule target verified before aid context')
check('!_hostileDispositionActive ? "\\n" + GcczTownPromptResourceProvider.GetCatalog().GetRuntimeText("VillageAid_Postprocess")' in main,'argument instructions only in friendly postprocess')
check('GetRuntimeText("VillageAid_Dialogue")' in gov and 'GetRuntimeText("VillageAid_Confirm")' in gov,'main and per-action descriptions consume selected package')
check('_aidLastDay.TryGetValue' not in aid+gov and 'VillageGovernanceRules.AidAssistance(' not in aid+gov,'old cooldown and distinct-type cooperation awards removed from live path')
check('VillageAidRules.RecordDelivery(_aidDeliveryRemainder, village.StringId)' in aid,'actual delivery uses persistent two-payment counter')
check('store.SyncData("_gcczVillageAidDeliveryRemainder_v1", ref _aidDeliveryRemainder)' in gov and '_aidDeliveryRemainder ??=' in gov,'primitive remainder sync and old-save default')
check('_aidDeliveryRemainder.Clear();' in main and '_aidDeliveryRemainder.Clear();' not in main.split('private static void ClearMissionState',1)[1],'new game clears remainder, mission exit does not')
check('store.SyncData("_gcczVillageAidLastDay_v1", ref _aidLastDay)' in gov,'legacy save identity preserved')
check('sequence == _giftInquirySequence' in aid and 'ReferenceEquals(owner, EconomyOwner)' in aid and 'ReferenceEquals(mission, _activeMission)' in aid,'request/owner/mission identity guards native callbacks')
check('ReferenceEquals(roster, party.ItemRoster)' in aid and 'ReferenceEquals(market, village.ItemRoster)' in aid,'confirmed rosters cannot silently change')
check(aid.index('_giftInquiryActive = false; // Consume') < aid.index('GiveGoldAction.ApplyBetweenCharacters'),'callback consumed before native events')
check('ItemValue' in aid and '!e.EquipmentElement.Item.HorseComponent.IsLiveStock' in aid,'native modified value and livestock/mount split')
check('market.AddToCounts(pair.Key, pair.Value)' in aid and 'source.AddToCounts(pair.Key, -pair.Value)' in aid,'real modifier-preserving transfer, not a separate stash')
check('OnMissionTick' not in aid and 'File.' not in aid and 'Task.Run' not in aid,'no tick/background inventory or IO work')
check('CurrentDay - round >= VillageGovernanceRules.AidCooldownDays' in aid and 'before | (1 << kind)' in aid,'different-type growth round remains seven days')
check('if (!_aidBonusExpires.ContainsKey(key))' in aid,'repeats or upgrades cannot renew growth expiry')
base='content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/'
packs={lang:json.loads(read(base+'GcczTownPrompt.'+lang+'.json'))['RuntimeText'] for lang in ['zh-CN','en','en-US']}
keys={'VillageAid_Dialogue','VillageAid_Postprocess','VillageAid_Confirm'}
for lang,body in packs.items():
 check(keys=={k for k in body if k.startswith('VillageAid_')},'three aid-only keys '+lang)
 for key in keys:
  check(set(re.findall(r'\{[a-z_]+\}',body[key]))==set(re.findall(r'\{[a-z_]+\}',packs['zh-CN'][key])),'placeholder parity '+key+lang)
  if lang!='zh-CN':
   natural=re.sub(r'\[VILLAGE_ACTION:[^\]]*\]','',body[key])
   check(not re.search('[\u4e00-\u9fff]',natural),'EN fixed prose; Chinese protocol identity exempt '+key)
 check('[VILLAGE_ACTION:' not in body['VillageAid_Dialogue'],'main aid text has no protocol '+lang)
check(all(packs['en'][k]==packs['en-US'][k] for k in keys),'EN compatibility edition equals canonical')
tutorial=read('AnimusForge.SiegeAftermathIntervention/GcczVillageCastleTutorial.cs')
check('每实际交付两次' in tutorial and 'every two completed deliveries' in tutorial,'paired tutorial explains independent assistance')
check('同一种援助有 7 天冷却' not in tutorial and 'Each aid type has a 7-day cooldown' not in tutorial,'obsolete aid cooldown removed from tutorial')
print(f'PASS {checks} aid wiring/resource checks; source contracts only.')
