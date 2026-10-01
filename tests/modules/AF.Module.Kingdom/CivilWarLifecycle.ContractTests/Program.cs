using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;

int checks = 0;
void Check(bool ok, string text) { checks++; if (!ok) throw new Exception(text); }
CivilWarCatalog.Initialize(CivilWarEffects.Ids);

// Real prompt method -> real normalizer -> real shared parser. Only the campaign context is fake.
var f = new Fixture();
f.Home.RulingClan = Clan.PlayerClan;
List<PostprocessRuleEntry> rules = AIConfigHandler.BuildRuntimeKingdomServicePostprocessRules();
Check(rules.Count == 6, "player king gets the six civil faction rules");
var parser = new LegacyActionTagParser();
var context = new PostprocessContext(Array.Empty<string>(), LegacyActionTagCatalog.DefaultAllowedTagFamilies, new CapabilitySet(new[] { "action.parse" }));
foreach (var rule in rules)
{
    string normalized = ShoutBehavior.NormalizeKingdomServicePostprocessTagsForScene(rule.Tag, rules);
    Check(normalized.Contains(rule.Tag), "normalizer preserves " + rule.Tag);
    Check(!parser.HasDisallowedProtocolTag(normalized, context), "shared allowlist accepts " + rule.Tag);
    Check(parser.Parse(normalized, context).Actions.Count == 2, "civil action and mood both parsed");
}
Check(!ShoutBehavior.NormalizeKingdomServicePostprocessTagsForScene("[A:CIVIL_FACTION:ERASE]", rules).Contains("ERASE"), "unknown action is not offered");
Check(parser.HasDisallowedProtocolTag("[A:UNRELATED:DO]", context), "allowlist stays finite");
DuelSettings.Enabled = false;
Check(AIConfigHandler.BuildRuntimeKingdomServicePostprocessRules().Count == 0, "MCM disabled rules empty");
DuelSettings.Enabled = true;
Clan.PlayerClan = null;
Check(AIConfigHandler.BuildRuntimeKingdomServicePostprocessRules().Count == 0, "missing player clan remains ineligible");

// Policy conclusion runs after vanilla applied its decision (both supported API lines).
f = new Fixture();
TeamModuleServices.CivilWar = new CivilWarModuleAdapter();
var behavior = new CivilWarCampaignBehavior();
var policy = new PolicyObject { StringId = "policy", Name = "policy" };
var decision = new KingdomPolicyDecision { Kingdom = f.Home, Policy = policy };
var approved = new KingdomPolicyDecision.PolicyDecisionOutcome { ShouldDecisionBeEnforced = true };
behavior.OnKingdomDecisionConcluded(decision, approved, false);
Check(JsonConvert.DeserializeObject<KingdomCivilWarStorage>(TeamModuleServices.CivilWar.Save()).Kingdoms.Count == 0, "approved repeal does not add grievance");
f.Home.ActivePolicies.Add(policy);
behavior.OnKingdomDecisionConcluded(decision, new KingdomPolicyDecision.PolicyDecisionOutcome(), false);
Check(JsonConvert.DeserializeObject<KingdomCivilWarStorage>(TeamModuleServices.CivilWar.Save()).Kingdoms.Count == 0, "rejected repeal does not add grievance");
// A non-player vassal in the home kingdom receives the actual enacted-policy grievance.
ChangeKingdomAction.Move(f.Follower, f.Home);
behavior.OnKingdomDecisionConcluded(decision, approved, false);
var policyState = JsonConvert.DeserializeObject<KingdomCivilWarStorage>(TeamModuleServices.CivilWar.Save()).Kingdoms[f.Home.StringId];
Check(policyState.LastImposedPolicyId == policy.StringId && policyState.Clans[f.Follower.StringId].Grievance["policy_imposed"] == 8, "enacted policy still records grievance");

// Loyalty starts only for actual combatants, with a repair for marks made by an older build.
f = new Fixture();
Check(f.Owner.GetOppositionLoyaltyDelta(f.PlayerTown) == 0, "pending player is not penalized");
Check(f.Owner.GetOppositionLoyaltyDelta(f.RebelTown) == -1, "actual rebel remains penalized");
var marks = (Dictionary<string, string>)typeof(KingdomCivilWarOwner).GetField("_oppositionMarkFaction", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Owner);
marks[f.PlayerTown.StringId] = f.Faction.Id;
Check(f.Owner.AnswerFollow(f.Faction.Id, false, out _), "player can stay");
Check(f.Owner.GetOppositionLoyaltyDelta(f.PlayerTown) == 0 && f.Owner.GetOppositionLoyaltyDelta(f.RebelTown) == -1, "staying repairs only player's marks");
f.Reload();
Check(f.Owner.GetOppositionLoyaltyDelta(f.PlayerTown) == 0, "loyalty agrees after reload");
f = new Fixture();
Check(f.Owner.AnswerFollow(f.Faction.Id, true, out _) && Clan.PlayerClan.Kingdom == f.Rebel, "player follows");
Check(f.Faction.WarClanIds.Contains(Clan.PlayerClan.StringId) && f.Owner.GetOppositionLoyaltyDelta(f.PlayerTown) == -1, "following records combatant and loyalty");
f.Reload();
Check(f.Owner.GetOppositionLoyaltyDelta(f.PlayerTown) == -1, "following survives reload");

// Failed peace keeps tracking and stability, then resumes the SAME outcome despite a later peace flag.
f = new Fixture();
MakePeaceAction.Fail = true;
f.Tick(101);
Check(f.State.Factions.Count == 1 && f.Owner.IsInOpenCivilWar(f.Home.StringId), "peace failure preserves faction and war block");
Check(MyBehavior.StabilityChanges == 0 && !f.Faction.ResolutionNeedsReview && f.Faction.ResolutionError.Length > 0, "safe failure schedules retry without victory bonus");
Check(f.Home.IsAtWarWith(f.Rebel), "failed peace really left war active");
f.Reload();
MakePeaceAction.Fail = false;
f.Faction.EndedByPeace = true;
f.Tick(102);
Check(f.State.Factions.Count == 0 && !f.Owner.IsInOpenCivilWar(f.Home.StringId), "retry finishes and removes war gate");
Check(f.Leader.Kingdom == null && MyBehavior.StabilityChanges == 6, "locked crown victory did not reroll into concession");
f.Tick(103);
Check(MyBehavior.StabilityChanges == 6, "victory bonus applied once");

// Followers return first; a later failure does not repeat their defection or relation penalty.
f = new Fixture();
ChangeKingdomAction.FailClan = f.Leader.StringId;
f.Tick(101);
Check(f.Follower.Kingdom == f.Home && f.Leader.Kingdom == f.Rebel, "partial return reproduced");
Check(f.State.Factions.Count == 1 && !f.Rebel.IsEliminated && MyBehavior.StabilityChanges == 0, "partial return cannot retire faction/kingdom");
int relationCalls = ChangeRelationAction.Calls;
f.Reload();
ChangeKingdomAction.FailClan = null;
f.Tick(102);
Check(f.State.Factions.Count == 0 && ChangeRelationAction.Calls == relationCalls + 2, "retry only penalizes newly returned leader and exile");

// Cleanup veto is a retryable postcondition; already returned clans are not processed twice.
f = new Fixture();
MyBehavior.CleanupAllowed = false;
f.Tick(101);
Check(f.State.Factions.Count == 1 && f.Leader.Kingdom == f.Home && !f.Faction.ResolutionReturnCompleted, "cleanup veto keeps settlement pending");
int moves = ChangeKingdomAction.Moves;
f.Reload();
MyBehavior.CleanupAllowed = true;
f.Tick(102);
Check(f.State.Factions.Count == 0 && ChangeKingdomAction.Moves == moves + 1, "cleanup retry only exiles leader, no repeated return");

// An additive action that changed the game and then threw must not be replayed after save/load.
f = new Fixture();
f.Faction.ResolutionOutcomeId = CivilWarCatalog.NegotiatedOutcomeId;
GiveGoldAction.ThrowAfterApply = true;
f.Tick(101);
Check(GiveGoldAction.Calls == 1 && f.Faction.ResolutionNeedsReview && f.Faction.ResolutionReturnCompleted, "post-transfer exception retained as uncertain");
Check(f.State.Factions.Count == 1 && MyBehavior.StabilityChanges == 0, "uncertain settlement is not a success");
f.Reload();
GiveGoldAction.ThrowAfterApply = false;
f.Tick(102);
Check(GiveGoldAction.Calls == 1 && f.State.Factions.Count == 1, "uncertain money transfer never automatically repeats");
Check(f.Owner.BuildPlayerKingdomPanel().Factions.Single().Stage.Contains("需检查"), "blocked settlement is visible");

// Previously fulfilled demands can finish after the return phase, rather than getting stuck forever.
f = new Fixture();
f.Faction.DemandId = "revoke_policy";
f.Faction.TargetId = "already_removed";
f.Faction.ResolutionOutcomeId = CivilWarCatalog.NegotiatedOutcomeId;
f.Tick(101);
Check(f.State.Factions.Count == 0, "already-revoked policy concession completes");

f = new Fixture();
f.Faction.ResolutionOutcomeId = "removed_outcome";
f.Tick(101);
Check(f.Faction.ResolutionNeedsReview && MyBehavior.StabilityChanges == 0, "missing outcome is retained without bonus");
f.Reload();
f.Tick(102);
Check(f.State.Factions.Count == 1 && f.Faction.ResolutionOutcomeId == "removed_outcome", "unknown outcome never rerolls after reload");

f = new Fixture();
f.Faction.ResolutionOutcomeId = "rebels_usurp";
f.Leader.Leader.IsAlive = false;
f.Tick(101);
Check(!f.Faction.ResolutionNeedsReview && f.State.Factions.Count == 1 && ChangeKingdomAction.Moves == 0, "precondition failure retries without starting actions");
f.Leader.Leader.IsAlive = true;
f.Tick(102);
Check(f.State.Factions.Count == 0 && f.Home.RulingClan == f.Leader && MyBehavior.StabilityChanges == -4, "repaired precondition finishes chosen usurp outcome");

f = new Fixture();
f.Faction.ResolutionOutcomeId = CivilWarCatalog.SecedeOutcomeId;
MakePeaceAction.Fail = true;
f.Tick(101);
Check(f.State.Factions.Count == 1 && MyBehavior.StabilityChanges == 0, "secession requires actual peace");
MakePeaceAction.Fail = false;
f.Tick(102);
Check(f.State.Factions.Count == 0 && !f.Rebel.IsEliminated && !f.Home.IsAtWarWith(f.Rebel), "secession retry leaves a living independent kingdom");

// Daily decay preserves each source's seven-day retention and cannot repeat on save/load or weekly maintenance.
f = new Fixture();
f.State.LastGrievanceDecayDay = 700;
var grievance = f.State.Clans[f.Follower.StringId].Grievance;
foreach (var source in CivilWarCatalog.Sources) grievance[source.Id] = 100f;
for (int day = 701; day <= 707; day++) { CampaignTime.Day = day; f.Owner.AdvanceDay(day); }
foreach (var source in CivilWarCatalog.Sources)
    Check(Math.Abs(grievance[source.Id] - 100f * (1f - source.DecayPerWeek)) < 0.001f, "seven daily steps preserve weekly rate: " + source.Id);
float before = grievance["lands_raided"];
f.Owner.AdvanceDay(707);
Check(grievance["lands_raided"] == before, "same day callback is idempotent");
f.Reload();
f.Owner.AdvanceDay(707);
Check(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] == before, "reload does not repeat today's decay");
// Keep this test away from any war actions or new factions.
f.State.Factions.Clear();
f.Tick(101);
Check(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] == before, "weekly slice does not charge an extra weekly decay");

f = new Fixture();
f.State.LastGrievanceDecayDay = 700;
f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] = 20f;
CampaignTime.Day = 701;
f.Owner.AdvanceDay(701);
Check(Math.Abs(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] - 19.3725f) < 0.001f, "burned village declines smoothly after one day");

f = new Fixture();
f.State.LastGrievanceDecayDay = -1;
f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] = 20f;
f.Reload();
CampaignTime.Day = 800;
f.Owner.AdvanceDay(800);
Check(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] == 20f, "legacy save starts today without historical catch-up");
CampaignTime.Day = 801;
f.Owner.AdvanceDay(801);
Check(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] < 20f, "legacy save starts decaying the following day");
DuelSettings.Enabled = false;
before = f.State.Clans[f.Follower.StringId].Grievance["lands_raided"];
CampaignTime.Day = 810;
f.Owner.AdvanceDay(810);
Check(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] == before, "disabled days keep grievance");
DuelSettings.Enabled = true;
CampaignTime.Day = 811;
f.Owner.AdvanceDay(811);
Check(Math.Abs(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] - before * Math.Pow(0.8, 1d / 7d)) < 0.001, "re-enable applies one day, not disabled backlog");

f = new Fixture();
f.State.LastGrievanceDecayDay = 700;
f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] = 20f;
ChangeKingdomAction.Move(f.Follower, f.Home);
CampaignTime.Day = 707;
f.Owner.AddGrievance(f.Home, "lands_raided", new[] { f.Follower }, 20f, 101, "raid");
Check(Math.Abs(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] - 36f) < 0.001, "missed days settled before adding fresh points");
f.Owner.AdvanceDay(707);
Check(Math.Abs(f.State.Clans[f.Follower.StringId].Grievance["lands_raided"] - 36f) < 0.001, "fresh points not retroactively decayed");

// Real campaign daily handler -> adapter -> owner.
TeamModuleServices.CivilWar = new CivilWarModuleAdapter();
TeamModuleServices.CivilWar.Load(JsonConvert.SerializeObject(f.Owner.Storage));
CampaignTime.Day = 708;
new CivilWarCampaignBehavior().OnDailyTick();
var dailyStorage = JsonConvert.DeserializeObject<KingdomCivilWarStorage>(TeamModuleServices.CivilWar.Save());
Check(dailyStorage.Kingdoms[f.Home.StringId].LastGrievanceDecayDay == 708 && dailyStorage.Kingdoms[f.Home.StringId].Clans[f.Follower.StringId].Grievance["lands_raided"] < 36, "production daily event reaches stored grievances");

// Synthetic CPU probe only: no game-loop/render/IO cost is represented here.
f = new Fixture(); f.State.Factions.Clear(); f.State.Clans.Clear(); f.State.LastGrievanceDecayDay = 700;
for (int i = 0; i < 100; i++)
{
    var record = new KingdomCivilWarClanState { ClanId = "perf" + i };
    foreach (var source in CivilWarCatalog.Sources) record.Grievance[source.Id] = 100;
    f.State.Clans[record.ClanId] = record;
}
f.Owner.AdvanceDay(701); // warm up
var timer = System.Diagnostics.Stopwatch.StartNew();
for (int day = 702; day < 802; day++) f.Owner.AdvanceDay(day);
timer.Stop();
Console.WriteLine($"Daily decay probe: 100 clans x 9 sources, 100 daily events, total {timer.Elapsed.TotalMilliseconds:F2} ms; mean {timer.Elapsed.TotalMilliseconds / 100:F4} ms/event (fake game context).");
Console.WriteLine($"CivilWar lifecycle contracts passed: {checks} checks (fake game actions, real owner/effects and extracted entry bodies).");

sealed class Fixture
{
    public Kingdom Home, Rebel;
    public Clan Leader, Follower;
    public Settlement PlayerTown, RebelTown;
    public KingdomCivilWarOwner Owner;
    public KingdomCivilWarKingdomState State => Owner.Storage.Kingdoms[Home.StringId];
    public KingdomCivilWarFactionState Faction => State.Factions.Single();
    public Fixture()
    {
        CampaignTime.Day = 700;
        Clan.All.Clear(); Kingdom.All.Clear(); MakePeaceAction.Fail = false; ChangeKingdomAction.FailClan = null; ChangeKingdomAction.Moves = 0;
        GiveGoldAction.Calls = 0; GiveGoldAction.ThrowAfterApply = false; ChangeRelationAction.Calls = 0;
        MyBehavior.StabilityChanges = 0; MyBehavior.CleanupAllowed = true; DuelSettings.Enabled = true;
        Home = new() { StringId = "home" }; Rebel = new() { StringId = "rebel" }; Kingdom.All.AddRange(new[] { Home, Rebel });
        Home.RulingClan = AddClan("crown", Home); Leader = AddClan("leader", Rebel); Rebel.RulingClan = Leader;
        Follower = AddClan("follower", Rebel); Clan.PlayerClan = AddClan("player", Home); Hero.MainHero = Clan.PlayerClan.Leader;
        PlayerTown = new() { StringId = "player_town", OwnerClan = Clan.PlayerClan }; Clan.PlayerClan.Settlements.Add(PlayerTown);
        RebelTown = new() { StringId = "rebel_town", OwnerClan = Follower }; Follower.Settlements.Add(RebelTown);
        Home.FactionsAtWarWith.Add(Rebel); Rebel.FactionsAtWarWith.Add(Home);
        var faction = new KingdomCivilWarFactionState { Id = "faction", DemandId = "redress", LeaderClanId = Leader.StringId,
            Stage = KingdomCivilWarStage.OpenWar, RebelKingdomId = Rebel.StringId, WarStartWeek = 95,
            ResolutionOutcomeId = CivilWarCatalog.CrownVictoryOutcomeId, PlayerFollowPending = true,
            WarClanIds = new() { Leader.StringId, Follower.StringId } };
        var state = new KingdomCivilWarKingdomState { KingdomId = Home.StringId, CooldownUntilWeek = 999, Factions = new() { faction } };
        foreach (var clan in new[] { Leader, Follower, Clan.PlayerClan }) state.Clans[clan.StringId] = new() { ClanId = clan.StringId, Side = KingdomCivilWarSide.Opposition, FactionId = faction.Id };
        Owner = new(); Owner.Replace(new() { Kingdoms = new() { [Home.StringId] = state } });
    }
    static Clan AddClan(string id, Kingdom kingdom)
    {
        var clan = new Clan { StringId = id, Name = id, Kingdom = kingdom }; clan.Leader = new Hero { Clan = clan };
        Clan.All.Add(clan); kingdom.Clans.Add(clan); return clan;
    }
    public void Tick(int week) => Owner.AdvanceWeek(Home, week, 50, (k, d) => MyBehavior.StabilityChanges += d, Array.Empty<string>());
    public void Reload()
    {
        string json = JsonConvert.SerializeObject(Owner.Storage);
        Owner = new(); Owner.Replace(JsonConvert.DeserializeObject<KingdomCivilWarStorage>(json));
    }
}
