using AnimusForge;
using AnimusForge.SiegeAftermathIntervention;
using MCM.Abstractions.Base.Global;
int checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
var live = new GcczSettings(); GlobalSettings<GcczSettings>.Instance = live;
var clone = new GcczSettings { InitialCooperation = 5, ResponseLimit = 2, Unlimited = true, DiagnosticLog = false, VerboseLog = true };
Check(DuelSettings.Current.GcczVillageInitialAssistance == 0 && DuelSettings.SaveCount == 0, "Editing clone must not mutate/save owner");
clone.NotifySave();
Check(DuelSettings.SaveCount == 0 && !DuelSettings.Current.GcczNpcResponseUnlimited, "Preset SaveTriggered is not a live commit");
Check(live.InitialCooperation == 0 && clone.InitialCooperation == 5, "Snapshot values are isolated");
live.InitialCooperation = clone.InitialCooperation; live.Unlimited = clone.Unlimited; live.ResponseLimit = clone.ResponseLimit;
live.DiagnosticLog = clone.DiagnosticLog; live.VerboseLog = clone.VerboseLog;
Check(DuelSettings.SaveCount == 0 && DuelSettings.Current.GcczNpcResponseLimit == 10, "Live draft does not prematurely save");
live.NotifySave();
Check(DuelSettings.SaveCount == 1 && DuelSettings.Current.GcczVillageInitialAssistance == 5 && DuelSettings.Current.GcczNpcResponseLimit == 2, "Live save commits original owner exactly once");
Check(DuelSettings.Current.GcczNpcResponseUnlimited && !DuelSettings.Current.EnableGcczDiagnosticLog && DuelSettings.Current.EnableGcczVerboseDiagnosticLog, "All bool fields applied");
DuelSettings.Current.GcczVillageInitialAssistance = 1;
Check(live.InitialCooperation == 1, "Frontend follows terminal owner changes after save");
live.InitialCooperation = 4; live.InitialCooperation = 1; // Undo via the real setter.
Check(DuelSettings.SaveCount == 1 && DuelSettings.Current.GcczVillageInitialAssistance == 1, "Undo/cancel leaves saved owner untouched");
DuelSettings.Current.GcczVillageInitialAssistance = 3;
Check(live.InitialCooperation == 3, "Cancelled draft must not mask later terminal changes");
clone.ResponseLimit = 999; clone.InitialCooperation = -9;
Check(clone.ResponseLimit == 10 && clone.InitialCooperation == 0, "Malformed snapshot values clamped");

// Every restored setting must honor the same copy/save/cancel boundary.
foreach (var test in new (string Name, object Changed, object Later)[] {
    ("GcczDiagnosticLogMaxSizeMegabytes", 32, 48),
    ("EnableSetsDiagnosticLog", false, true),
    ("EnableSetsVerboseDiagnosticLog", true, false),
    ("SetsDiagnosticLogMaxSizeMegabytes", 64, 24),
    ("SetsOtherSettlementFollowerLimit", 50, 20) })
{
    var projected = typeof(GcczSettings).GetProperty(test.Name);
    var owned = typeof(DuelSettings).GetProperty(test.Name);
    var initial = owned.GetValue(DuelSettings.Current);
    var saves = DuelSettings.SaveCount;
    Check(Equals(projected.GetValue(live), initial), test.Name + " reads original owner");
    projected.SetValue(clone, test.Changed); clone.NotifySave();
    Check(Equals(owned.GetValue(DuelSettings.Current), initial) && DuelSettings.SaveCount == saves, test.Name + " preset cannot save owner");
    projected.SetValue(live, test.Changed);
    Check(Equals(owned.GetValue(DuelSettings.Current), initial), test.Name + " live draft isolated");
    projected.SetValue(live, initial); // MCM Undo.
    owned.SetValue(DuelSettings.Current, test.Later);
    Check(Equals(projected.GetValue(live), test.Later), test.Name + " cancelled draft follows terminal");
    projected.SetValue(live, test.Changed); live.NotifySave();
    Check(Equals(owned.GetValue(DuelSettings.Current), test.Changed) && DuelSettings.SaveCount == saves + 1, test.Name + " live save commits once");
    owned.SetValue(DuelSettings.Current, initial);
    Check(Equals(projected.GetValue(live), initial), test.Name + " saved draft released");
}
clone.GcczDiagnosticLogMaxSizeMegabytes = -10; clone.SetsDiagnosticLogMaxSizeMegabytes = 900;
clone.SetsOtherSettlementFollowerLimit = -1;
Check(clone.GcczDiagnosticLogMaxSizeMegabytes == 1 && clone.SetsDiagnosticLogMaxSizeMegabytes == 128 && clone.SetsOtherSettlementFollowerLimit == 1, "Restored integer lower/upper bounds");
clone.GcczDiagnosticLogMaxSizeMegabytes = 900; clone.SetsDiagnosticLogMaxSizeMegabytes = -10;
clone.SetsOtherSettlementFollowerLimit = 900;
Check(clone.GcczDiagnosticLogMaxSizeMegabytes == 128 && clone.SetsDiagnosticLogMaxSizeMegabytes == 1 && clone.SetsOtherSettlementFollowerLimit == 100, "Restored integer opposite bounds");
foreach (var name in new[] { "ExportGcczDebugLog", "ClearGcczDebugLog", "ExportSetsDiagnosticLog", "ClearSetsDiagnosticLog", "ExportNoblePrisonerEscortLog", "ClearNoblePrisonerEscortLog" })
{
    var button = (Action)typeof(GcczSettings).GetProperty(name).GetValue(clone);
    var callback = typeof(DuelSettings).GetProperty(name);
    int calls = 0; var saves = DuelSettings.SaveCount;
    callback.SetValue(DuelSettings.Current, (Action)(() => calls++));
    button(); Check(calls == 1 && DuelSettings.SaveCount == saves, name + " invokes existing action without saving draft");
    DuelSettings.Current = new DuelSettings();
    callback.SetValue(DuelSettings.Current, (Action)(() => calls += 10));
    button(); Check(calls == 11, name + " resolves current owner at click, not snapshot creation");
    DuelSettings.Current = null; button();
    Check(calls == 11, name + " absent owner safe");
    DuelSettings.Current = new DuelSettings();
}
var controls = typeof(GcczSettings).GetProperties().Where(p => p.GetCustomAttributes(typeof(MCM.Abstractions.Attributes.SettingPropertyGroupAttribute), false).Length != 0).ToArray();
Check(controls.Length == 19, "Complete original GCCZ functionality: 10 settings and 9 actions, duplicate town tutorial consolidated");
Check(controls.All(p => p.GetCustomAttributes(typeof(Newtonsoft.Json.JsonIgnoreAttribute), false).Length == 1), "Frontend has no competing persisted values or actions");
Check(live.DisplayName.Contains("{=AF_GCCZ_McmTitle}"), "Panel title uses UI localization, not hardcoded GCCZ");

VillageDispositionEconomy Budget() => new(100, 0, 0, 4, new Dictionary<string,int> { ["iron"] = 100 }, new Dictionary<string,int> { ["cow"] = 20 });
var budget = Budget(); var devastation = budget.Prepare(VillageDispositionActionKind.Destroy);
Check(devastation.Gold == 300 && devastation.Food == 60 && devastation.Goods["iron"] == 42 && devastation.Livestock["cow"] == 12, "Devastation gives exact 60 percent");
Check(budget.Commit(devastation), "Commit devastation");
Check(!budget.Prepare(VillageDispositionActionKind.Plunder).HasRewards, "Destroyed forty percent cannot be looted later");
Check(!budget.Commit(devastation), "Duplicate destruction receipt cannot pay twice");
foreach (var kind in new[] { VillageDispositionActionKind.ForcedRansom, VillageDispositionActionKind.ForcedFood, VillageDispositionActionKind.ForcedGoods, VillageDispositionActionKind.ForcedLivestock, VillageDispositionActionKind.VoluntaryRansom, VillageDispositionActionKind.VoluntaryFood })
    Check(!budget.Prepare(kind).HasRewards, "Destroyed pool inaccessible: " + kind);
var small = new VillageDispositionEconomy(1, 0, 0, 0, null, null);
small.DestroyRemainingResources();
Check(!small.Prepare(VillageDispositionActionKind.Plunder).HasRewards, "No-reward destructive path also consumes leftovers");
var shared = Budget(); Check(shared.Commit(shared.Prepare(VillageDispositionActionKind.ForcedRansom)), "Ordinary levy remains payable");
Check(shared.Prepare(VillageDispositionActionKind.Plunder).Gold == 0 && shared.Prepare(VillageDispositionActionKind.Plunder).Food == 100, "Non-destructive claims preserve other pools");
Check(Budget().Prepare(VillageDispositionActionKind.Plunder).Gold == 500, "Per-visit refresh intentionally preserved by user");
Console.WriteLine($"PASS {checks} GCCZ review checks: real source, fake MCM boundary; no native UI/save/game execution.");
