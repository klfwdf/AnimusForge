"""Focused source/resource wiring contracts; not a live-game simulation."""
from pathlib import Path
import hashlib
import re
import xml.etree.ElementTree as ET
ROOT = Path(__file__).resolve().parents[3]
D = ROOT / "src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath"
def read(p): return p.read_text(encoding="utf-8-sig")
def check(ok, label):
    if not ok: raise AssertionError(label)
    print("PASS", label)
main = read(D / "VillageAftermathBehavior.cs")
gov = read(D / "VillageAftermathBehavior.Governance.cs")
aid = read(D / "VillageAftermathBehavior.Aid.cs")
core = read(ROOT / "AnimusForge.SiegeAftermathIntervention/VillageGovernanceRules.cs")
economy = read(D / "VillageAftermathBehavior.Economy.cs")
sets = read(ROOT / "src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.cs")
culture = read(D / "VillageAftermathBehavior.Culture.cs")
check("TryProcessFriendlyDisposition" in main and "private static bool ApplyAction(" not in main, "new friendly consumer replaces old policy executor")
check("BuildFriendlyRules" in main and "FriendlyVillageActionCatalog.NormalizeInvocations(raw, allowed)" in main, "postprocess eligibility, normalization and execution use new catalog")
check("OnPlayerVillageLeaveRequest(base.Mission)" in sets and "RequestDefaultPlunder" in gov, "default looting is attached to player leave request")
end = main.split("internal static void EndForSetsMission", 1)[1].split("internal static string", 1)[0]
check("Plunder" not in end and "_dispositionAwaitingExit = true" in end, "generic teardown settles but never invents default plunder")
check("!_activateVillageAftermath && SetsSettlementEntryProfile.IsSupported" in sets, "village aftermath cannot launch a new SETS militia conflict")
check("activateVillageAftermath && villageSelectedRoster != null" in sets and "selected.AddToCounts(c, count)" in sets, "both village entry paths consume actual selected troops and heroes")
check("VillageGovernanceRules.OwnerPenalty" in economy and "VillageGovernanceRules.RulerPenalty" in economy and "AdditionalPenalty" in economy, "friendly politics uses identity targets and worst-severity ledger")
check("_aidBonusExpires[key] = CurrentDay + VillageGovernanceRules.AidDurationDays" in aid and "_aidMultiplier[id]" in aid, "independently expiring stacking aid bonuses have a runtime consumer")
check("_culturePreparedTurn = _villageReplySequence" in culture and "_cultureQuestionTurn = _villageReplySequence" in gov and "CanConfirmCulture(_cultureSpeaker, index, _villageReplySequence, _cultureQuestionTurn)" in culture, "culture confirmation binds speaker and a later reply")
check("ApplyVillageCultureNow" in culture and "VillageGovernanceRules.CultureCost" in culture and "RecordCultureCompletion" in culture, "culture completion reaches real persistence, payment and memory")
check("if (!CanGovern(village))" in main and "_gradualCultureFinishDayByVillageId[villageId] > CurrentDay" in main, "daily culture completion revalidates authority before due time")
growth = read(D / "VillageAftermathBehavior.Growth.cs")
check("__result * aid * PurgeMultiplier(settlement)" in growth and "CalculateHearthChange" in growth and "CalculateDailyProductionAmount" in growth, "native growth hooks multiply aid and purge without replacing models")
friendly = read(D / "SiegeAiInterventionBehavior.FriendlyEntry.cs")
check("_peacefulEntrySettlementId" in friendly and "ResetAftermathRuntimeGuards" in friendly and "ClearCastleLordDefeatProvenance" in friendly, "peaceful fortification entry carries explicit noncombat provenance")
check("if (IsPeacefulGovernanceEntry(settlement)) return;" in read(D / "SiegeAiInterventionBehavior.TownCompletionEffectAdapter.cs"), "peaceful entry bypasses native conquest aftermath")
check('"inspection"' in friendly and '"hostile_action"' in friendly, "live menu ordering uses inspection and native hostile-action anchors")
check("ExpeditionParadeMenuPolicy.EntryId" in friendly and friendly.index("ExpeditionParadeMenuPolicy.EntryId") < friendly.index('"inspection"'), "installed parade is preferred over legacy troop inspection")
main_prompt = gov.split("private static string BuildFriendlyPrompt", 1)[1].split("private static bool TryProcessFriendlyDisposition", 1)[0]
check("CultureRuleContext(false)" in main_prompt and "rule.Tag" not in main_prompt and "BuildFriendlyRules(index)" not in main_prompt, "main village prose never receives the postprocess directive list")
check("CalculateDailyFoodProductionAmount" in growth and "__result *= PurgeMultiplier" in growth, "purge includes native daily food production")
bypass = sets.split("internal bool ShouldBypassNativeEndMissionGuards()", 1)[1].split("private void ReachVictory", 1)[0]
check("VillageAftermathBehavior.IsActiveForMission(base.Mission)" in bypass and "_conflictFeaturesEnabled" in bypass, "only explicit village disposition bypasses nearby-enemy TAB lock; SETS battle guard retained")
adapter = read(D / "SiegeAiInterventionBehavior.cs")
check("peacefulEntry: IsPeacefulGovernanceEntry(activeSettlement)" in adapter and adapter.count("return BuildPeacefulSpeakerContext(") == 2, "peaceful castle replaces conquest main, role and reaction context")
mcm = read(ROOT / "src/AF.GameAdapter.Bannerlord/Configuration/Mcm/GcczSettings.cs")
check('"AnimusForge_GCCZ_v1"' in mcm and "DuelSettings.GetSettings()" in mcm and "[JsonIgnore]" in mcm, "separate MCM entry delegates to the original setting owner")
check(mcm.index("TownTutorial { ") < mcm.index("VillageTutorial { ") < mcm.index("CastleTutorial { "), "tutorial order is town, village, castle")
entry = read(D / "VillageAftermathBehavior.NativeDispositionEntry.cs")
check("prefix: new HarmonyMethod(typeof(VillageAftermathBehavior), nameof(NativeDispositionConfirmedPrefix))" in entry, "native neutral confirmation revalidates only the GCCZ request before hostility")
attack = entry.split("private static void StartNativeDispositionResistance", 1)[1].split("private void OnNativeDispositionBattleEnd", 1)[0]
check(attack.index("GetNativeDispositionAttackBlockReason()") < attack.index("PlayerEncounter.StartBattle()") < attack.index("battle.AttackerSide.TroopCount <= 0") < attack.index("MenuHelper.EncounterAttackCondition(args)") < attack.index("MenuHelper.EncounterAttackConsequence(args)"), "entry health, actual battle roster and native condition precede mission launch")
base = ROOT / "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/Languages"
cn = {n.attrib["id"]: n.attrib["text"] for n in ET.parse(base / "CNs/sets_ui_strings-zh-CN.xml").iter("string")}
en = {n.attrib["id"]: n.attrib["text"] for n in ET.parse(base / "sets_ui_strings.xml").iter("string")}
for key in ["AF_GCCZ_TownAffairs","AF_GCCZ_CastlePrisoners","AF_GCCZ_VillageAffairs","AF_GCCZ_TownTutorial","AF_GCCZ_CastleTutorial","AF_GCCZ_TutorialGroup","AF_GCCZ_CooperationCap","AF_GCCZ_NoHealthyAttackers","AF_GCCZ_InvalidAttackRoster",
            "AF_GCCZ_VillageDispositionEntry","AF_GCCZ_VillageDispositionNotReady","AF_GCCZ_VillageGatherQueued","AF_GCCZ_VillageGatherIssued","AF_GCCZ_VillageSceneFailed"]:
    check(bool(cn.get(key)) and bool(en.get(key)), "paired live UI resource " + key)
scene = read(D / "VillageDispositionSceneMissionBehavior.cs")
handoff = read(ROOT / "src/AF.GameAdapter.Bannerlord/SettlementEntry/SettlementEntryTroopSelectionBehavior.RewardPersistence.cs")
victory = read(D / "SiegeAiInterventionBehavior.SetsVictoryMenu.cs")
check(cn["AF_GCCZ_VillageDispositionEntry"] == "亲自进入村庄处置平民" and "AF_GCCZ_VillageDispositionEntry" in victory, "requested label is wired to actual victory menu")
check("SettlementEntryTroopSelectionBehavior.OnEngineTickForExternal();" in adapter and 'PumpPendingPostMissionFlow("engine_tick")' in handoff, "paused menus advance through existing engine tick")
check("PumpPendingPayoutForExternal" in handoff and "owner.OnVillageEconomyTick(0f)" in economy, "TAB party/item payout advances while campaign is paused")
menu_opened = sets.split("private void OnSetsGameMenuOpened", 1)[1].split("private static void PumpPendingPostMissionFlow", 1)[0]
check("if (_pendingVictoryMenuEntry == null)" in menu_opened, "native menu callback cannot present pending victory reentrantly")
pump = sets.split("private static bool TryPumpPendingSettlementTakenMenu", 1)[1].split("internal static void QueueSameKingdom", 1)[0]
check("PlayerEncounter.Current?.ForceSupplies == true" in pump and "settlement.Party?.MapEvent != null" in pump, "pending victory waits for native battle and force-supply completion")
check("RequestVictoryMenuAgainForExternal(village.StringId)" in entry and "HasQueuedVictoryForExternal" in entry, "repeat village click resumes reward ticket, not another battle")
check("EnsureVillageVictoryLocationEncounter(settlement)" in adapter and "new VillageEncounter(village)" in victory, "village handoff restores only its missing native location")
check("&& VillageAftermathBehavior.HasQueuedDispositionForExternal(settlement.StringId)" in sets and "!admittedVillageVictory" in sets, "exact village admission is allowed past its own pending victory ticket")
check("return !dispositionRequested || (_pendingMissionEntry?.ActivateVillageAftermath == true" in sets, "failed GCCZ preparation cannot fall through to ordinary scene")
check("new VillageDispositionSceneMissionBehavior(entry.SettlementId, entry.HostileVillageDisposition, entry.SelectedRoster)" in sets, "consumed admission installs dedicated scene owner")
check("VillageDispositionSceneMissionBehavior.Admits(_activeMission, _activeVillageId)" in main, "village prompt and action execution require current admitted mission")
check("!_armedCoup && !_activateVillageAftermath && _isOwnSettlement" in sets and "TryGatherDispositionVillagers(speakerAgentIndex)" in economy, "GCCZ gathering is separate from ordinary SETS formation workflow")
check(sets.count("if (_activateVillageAftermath) return;") >= 2 and adapter.count("&& !VillageDispositionSceneMissionBehavior.Controls(Mission.Current)") == 3, "GCCZ damage skips SETS and ordinary taunt/crime callbacks")
check("VillageDispositionSceneMissionBehavior.PrepareResidentDamage(victim);" in adapter and "PrepareResidentDamage(agent)" in scene, "adult mortality and player damage are wired to the village scene")
check('id.IndexOf("teenager"' in scene and "SceneTauntBehavior.IsChildSceneProtectedTarget(character)" in scene and "VillageDispositionSceneMissionBehavior.IsAdult" in read(D / "VillageAftermathBehavior.HostileDialogue.cs"), "youth is filtered from scene and dialogue authorization")
check(".SetTeam(" not in scene and ".Formation =" not in scene and "i < 8" in scene, "village gathering is bounded native navigation, not civilian team/formation reassignment")
# Exit-only resource payout contract: aid and neutral scene operations are separate.
collect = economy.split("private static bool ApplyForeignEconomicAction", 1)[1].split("private static void ApplyForeignRelations", 1)[0]
payout = economy.split("private void OnVillageEconomyTick", 1)[1].split("private void ClearPayout", 1)[0]
check("GiveGoldAction" not in collect and "OpenScreenAsLoot" not in collect, "resource tags neither pay money nor open payout screens inside the scene")
check("MobileParty.MainParty.MemberRoster.AddToCounts" not in collect and "MobileParty.MainParty.ItemRoster.AddToCounts" not in collect and "MobileParty.MainParty.PrisonRoster.AddToCounts" not in collect, "resource tags do not add rewards directly to the player's rosters")
check("owner._dispositionGold += plan.Gold" in collect and "owner._dispositionItems.AddToCounts" in collect, "cash and goods accumulate only in the pending payout")
check("owner._dispositionRecruits.AddToCounts" in collect and "owner._dispositionPrisoners.AddToCounts" in collect, "recruits and captives accumulate only in the pending payout")
check("_dispositionAwaitingExit = true" not in collect, "resource tag cannot unlock settlement before leaving")
check("!_dispositionAwaitingExit || Mission.Current != null" in payout and "ActiveState is not MapState" in payout, "payout requires exit, no mission and actual campaign map")
check(payout.index("!_dispositionAwaitingExit") < payout.index("GiveGoldAction.ApplyBetweenCharacters") < payout.index("PartyScreenHelper.OpenScreenAsLoot"), "cash payout happens only behind the exit guard")
check("PartyScreenHelper.OpenScreenAsLoot(_dispositionRecruits, _dispositionPrisoners" in payout, "personnel and prisoner choices use the pending rosters")
check("InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { { PartyBase.MainParty, _dispositionItems } })" in payout, "item choices use pending loot, not direct inventory credit")
check("_dispositionGoldPaid < _dispositionGold" in payout and "_dispositionGoldPaid += Math.Max(0, Hero.MainHero.Gold - before)" in payout, "cash retry tracks actual amount paid")
check("Math.Min(_dispositionOfferedSlaves" in payout and "GetTroopCount(slaveTroop) - _dispositionSlavesBefore" in payout, "slave hearth loss uses actual accepted captives, not all offered")
check("OpenVillageGift(invocation)" in gov and "owner.RecordVillageGift(village, kind, amount, itemList)" in aid and "_dispositionItems" not in aid, "player donations remain confirmed immediate transfers, not rewards from village")
print("SCOPE source wiring and resources only; no native mission, MCM UI, save loading or provider request executed.")
