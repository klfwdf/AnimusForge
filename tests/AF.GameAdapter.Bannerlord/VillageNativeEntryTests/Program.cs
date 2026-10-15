using AnimusForge;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
int checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
VillageAftermathBehavior Reset(bool atWar = true)
{
    var owner = new VillageAftermathBehavior(); Campaign.Current = new Campaign { Owner = owner };
    Clan.PlayerClan = new Clan { MapFaction = new object() }; FactionManager.AtWar = atWar;
    var village = new Settlement { IsVillage = true, StringId = "village_ES3_3", MapFaction = new object(), Village = new Village(), Party = new PartyBase() };
    Settlement.CurrentSettlement = PlayerEncounter.EncounterSettlement = village;
    PlayerEncounter.Current = new PlayerEncounter(); PlayerEncounter.LocationEncounter = new LocationEncounter { Settlement = village };
    MobileParty.MainParty = new MobileParty { CurrentSettlement = village, MemberRoster = new Roster { TotalHealthyCount = 1 } };
    PartyBase.MainParty = new PartyBase { NumberOfHealthyMembers = 1 };
    Hero.MainHero = new Hero { HitPoints = 100 };
    Mission.Current = null; PlayerEncounterCompat.Battle = null;
    PlayerEncounter.Starts = PlayerEncounter.Updates = MenuHelper.ConditionCalls = MenuHelper.AttackCalls = BeHostileAction.Calls = SettlementEntryTroopSelectionBehavior.Queued = SiegeAiInterventionBehavior.FriendlyEntries = 0;
    MenuHelper.ConditionResult = MenuHelper.EnableResult = true; VillageAftermathBehavior.LastFailure = null;
    PlayerEncounter.OnStart = () => PlayerEncounterCompat.Battle = Battle(1, 53);
    VillageAftermathBehavior.InstallHooks(); return owner;
}
MapEvent Battle(int attackers, int defenders) => new() { MapEventSettlement = Settlement.CurrentSettlement, IsForcingSupplies = true, IsPlayerMapEvent = true, AttackerSide = new Side { TroopCount = attackers }, DefenderSide = new Side { TroopCount = defenders } };
foreach (bool atWar in new[] { true, false })
{
    var owner = Reset(atWar); MobileParty.MainParty.MemberRoster.TotalHealthyCount = 0;
    var menuArgs = new MenuCallbackArgs();
    Check(VillageAftermathBehavior.Condition(menuArgs) && !menuArgs.IsEnabled && menuArgs.Tooltip != null, "Full HP but no healthy roster disables hostile button");
    VillageAftermathBehavior.Enter(new MenuCallbackArgs());
    Check(BeHostileAction.Calls == 0 && PlayerEncounter.Starts == 0 && MenuHelper.AttackCalls == 0 && owner.Pending == "", "Click rechecks readiness before war, request or mission");
    Reset(atWar); Hero.MainHero.IsWounded = true;
    menuArgs = new MenuCallbackArgs(); VillageAftermathBehavior.Condition(menuArgs);
    Check(!menuArgs.IsEnabled, "Wounded hero cannot enter even with healthy troops");
}
Reset(); PartyBase.MainParty.NumberOfHealthyMembers = 0;
var disabled = new MenuCallbackArgs(); VillageAftermathBehavior.Condition(disabled); Check(!disabled.IsEnabled, "Party readiness checked independently of roster");
Reset(); PlayerEncounterCompat.Battle = Battle(1, 53);
disabled = new MenuCallbackArgs(); VillageAftermathBehavior.Condition(disabled); Check(!disabled.IsEnabled, "Existing battle cannot be replaced");

var neutral = Reset(false); VillageAftermathBehavior.Enter(new MenuCallbackArgs());
Check(GameMenu.LastMenu == "force_supplies_village_resist_warn_player" && BeHostileAction.Calls == 0, "Neutral warning remains before hostility");
Hero.MainHero.IsWounded = true;
Check(!VillageAftermathBehavior.Confirm(new MenuCallbackArgs()), "Readiness rechecked after warning");
VillageAftermathBehavior.Confirmed(new MenuCallbackArgs());
Check(neutral.Pending == "" && BeHostileAction.Calls == 0 && PlayerEncounter.Starts == 0, "Cancelled confirmation cannot launch through postfix");
neutral = Reset(false); Hero.MainHero.IsWounded = true;
Check(VillageAftermathBehavior.Confirm(new MenuCallbackArgs()), "Unrelated native force-supply confirmation untouched");

var first = Reset(); VillageAftermathBehavior.Enter(new MenuCallbackArgs());
Check(MenuHelper.ConditionCalls == 1 && MenuHelper.AttackCalls == 1 && PlayerEncounter.Starts == 1, "Healthy 1 vs 53 checks native condition then attacks");
var lost = PlayerEncounterCompat.Battle; lost.HasWinner = true; lost.WinningSide = 1;
first.BattleEnded(lost); Check(first.Pending == "" && !first.Victory && SettlementEntryTroopSelectionBehavior.Queued == 0, "Defeat clears request and cannot grant GCCZ victory");
PlayerEncounterCompat.Battle = null; PlayerEncounter.Current = new PlayerEncounter();
Hero.MainHero.HitPoints = 100; MobileParty.MainParty.MemberRoster.TotalHealthyCount = 0;
VillageAftermathBehavior.Enter(new MenuCallbackArgs());
Check(MenuHelper.AttackCalls == 1 && PlayerEncounter.Starts == 1, "Logged defeat-heal-retry sequence no longer launches empty mission");

foreach (int defenders in new[] { 53, 0 })
{
    var owner = Reset(); PlayerEncounter.OnStart = () => PlayerEncounterCompat.Battle = Battle(0, defenders);
    VillageAftermathBehavior.Enter(new MenuCallbackArgs());
    Check(MenuHelper.AttackCalls == 0 && PlayerEncounter.Updates == 0 && !owner.Victory && owner.Pending == "", "Actual zero-attacker snapshot neither attacks nor wins");
    Check(VillageAftermathBehavior.LastFailure != null, "Invalid battle reports recovery path");
}
foreach (bool visible in new[] { true, false })
{
    var owner = Reset(); MenuHelper.ConditionResult = visible; MenuHelper.EnableResult = false;
    VillageAftermathBehavior.Enter(new MenuCallbackArgs());
    Check(MenuHelper.ConditionCalls == 1 && MenuHelper.AttackCalls == 0 && owner.Pending == "", "Native hidden/disabled attack cannot be bypassed");
}
var emptyVillage = Reset(); PlayerEncounter.OnStart = () => PlayerEncounterCompat.Battle = Battle(1, 0);
VillageAftermathBehavior.Enter(new MenuCallbackArgs());
Check(emptyVillage.Victory && PlayerEncounter.Updates == 1 && MenuHelper.AttackCalls == 0, "Healthy attacker retains undefended village path");
var retry = Reset(); VillageAftermathBehavior.Enter(new MenuCallbackArgs());
var won = PlayerEncounterCompat.Battle; won.HasWinner = true; won.WinningSide = won.PlayerSide;
retry.BattleEnded(won); Check(retry.Victory, "Normal native victory retained");
string supplyState = VillageAftermathBehavior.BeforeSupplies();
Check(supplyState == Settlement.CurrentSettlement.StringId, "Victory captured before native supply callback changes flags");
PlayerEncounterCompat.Battle = null; PlayerEncounter.Current.ForceSupplies = false;
VillageAftermathBehavior.AfterSupplies(supplyState);
Check(SettlementEntryTroopSelectionBehavior.Queued == 1 && retry.Pending == "", "Native reward callback queues one completed handoff and clears attack request");
Check(SettlementEntryTroopSelectionBehavior.Rewards.VillageSuppliesPrepared && SettlementEntryTroopSelectionBehavior.Rewards.VillageDamageApplied,
    "Already-paid native supplies and damage are not paid a second time");
int starts = PlayerEncounter.Starts, attacks = MenuHelper.AttackCalls;
SettlementEntryTroopSelectionBehavior.Presented = true;
VillageAftermathBehavior.Enter(new MenuCallbackArgs());
Check(!SettlementEntryTroopSelectionBehavior.Presented && PlayerEncounter.Starts == starts && MenuHelper.AttackCalls == attacks,
    "Click after native rewards requests existing victory menu instead of another zero-defender battle");
Check(VillageAftermathBehavior.BeforeSupplies() == "", "Completed request cannot create a second reward ticket");
SettlementEntryTroopSelectionBehavior.PendingVillage = null;
Reset(); Settlement.CurrentSettlement.MapFaction = Clan.PlayerClan.MapFaction; MobileParty.MainParty.MemberRoster.TotalHealthyCount = 0;
VillageAftermathBehavior.Enter(new MenuCallbackArgs());
Check(SiegeAiInterventionBehavior.FriendlyEntries == 1 && MenuHelper.AttackCalls == 0, "Friendly governance is not a hostile battle and remains accessible");
SettlementEntryTroopSelectionBehavior.Pumps = 0;
SettlementEntryTroopSelectionBehavior.OnEngineTickForExternal();
Check(SettlementEntryTroopSelectionBehavior.Pumps == 0, "Idle engine tick does no pending-flow work");
SettlementEntryTroopSelectionBehavior.PendingVillage = "village_test";
SettlementEntryTroopSelectionBehavior.OnEngineTickForExternal();
Check(SettlementEntryTroopSelectionBehavior.Pumps == 1, "Paused-time handoff is dispatched from engine tick without campaign tick");
SettlementEntryTroopSelectionBehavior.OnEngineTickForExternal();
Check(SettlementEntryTroopSelectionBehavior.Pumps == 1, "Presented menu is not pumped every frame");
SettlementEntryTroopSelectionBehavior.RequestVictoryMenuAgainForExternal("wrong_village");
Check(SettlementEntryTroopSelectionBehavior.Presented, "A different village cannot reopen the reward ticket");
SettlementEntryTroopSelectionBehavior.QueueExitForTest("village_test");
SettlementEntryTroopSelectionBehavior.OnEngineTickForExternal();
Check(VillageAftermathBehavior.PayoutPumps == 1 && SettlementEntryTroopSelectionBehavior.Pumps == 2, "Village exit advances payout before finishing encounter");
Console.WriteLine($"PASS {checks} village native entry checks; real entry source with fake native boundaries, not a live crash replay.");
